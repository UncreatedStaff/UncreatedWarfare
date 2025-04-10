using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ReflectionTools;
using Pty.Net;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Uncreated.Warfare.Services;

namespace Uncreated.Warfare.Util;

public struct WorkshopUploadParameters
{
    public ulong ModId;
    public string SteamCmdPath;
    public string Username, Password;
    public string ContentFolder;
    public string ImageFile;
    public ESteamWorkshopVisibility Visibility;
    public string Title, Description, ChangeNote;
}

public enum ESteamWorkshopVisibility : byte
{
    Public = 0,
    FriendsOnly = 1,
    Hidden = 2,
    Unlisted = 3
}

/// <summary>
/// Handles programmatically uploading workshop files.
/// </summary>
/// <remarks>
/// <para>
/// This uses the SteamCMD CLI to upload the mod to the workshop using predefined credentials in the config.
/// </para>
/// <para>
/// This uses a library called Pty.Net which acts as a virtual terminal tricking SteamCMD into thinking its displaying on a console window.
/// Windows recently added support for 'ConPTY' which is what the library uses on Windows. On Linux and OSX it just uses the built-in PTY implementation.
/// </para>
/// <para>
/// Usually you could just use the Process API but SteamCMD is written in a way that makes it impossible to read from the output buffer normally,
/// Pty.Net is a workaround.
/// </para>
///</remarks>
[Priority(100)]
public class WorkshopUploader : IHostedService
{
    private readonly ILogger<WorkshopUploader> _logger;
    private readonly SemaphoreSlim _sempahore = new SemaphoreSlim(1, 1);
    private IPtyConnection? _connection;

    internal static string? SteamCode;

    public WorkshopUploader(ILogger<WorkshopUploader> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public UniTask StartAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }

    /// <inheritdoc />
    public UniTask StopAsync(CancellationToken token)
    {
        try
        {
            _connection?.Kill();
            _connection?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing connection.");
        }
        finally
        {
            _connection = null;
        }

        return UniTask.CompletedTask;
    }

    // Steam format for uploading workshop items. If 'publishedfileid' is 0, a new mod is created, otherwise it will upload to whatever mod ID you put there
    private const string WorkshopItemVcfFormat = """
                                                 "workshopitem"
                                                 {{
                                                 	"appid" "304930"
                                                 	"publishedfileid" "{0}"
                                                 	"contentfolder" "{1}"
                                                 	"previewfile" "{2}"
                                                 	"visibility" "{3}"
                                                 	"title" "{4}"
                                                 	"description" "{5}"
                                                 	"changenote" "{6}"
                                                 }}
                                                 """;

    /// <summary>
    /// Returns the path to the .vdf file relating to content.
    /// </summary>
    public string GetVdfPath(string contentFolder)
    {
        return Path.GetFullPath(Path.Combine(contentFolder, "..", "mod_" + Path.GetFileName(contentFolder) + ".vdf"));
    }

    public ulong ReadModIdFromFile(string fileName)
    {
        return File.Exists(fileName) ? ReadModId(File.ReadAllText(fileName)) : 0ul;
    }

    /// <summary>
    /// Uploads a mod to the Steam Workshop using SteamCMD.
    /// </summary>
    public async Task<ulong?> UploadMod(WorkshopUploadParameters parameters, ILogger? logger, CancellationToken token = default)
    {
        logger ??= _logger;
        if (GameThread.IsCurrent)
        {
            await UniTask.SwitchToThreadPool();
        }

        await _sempahore.WaitAsync(token);
        try
        {
            string vdfPath = GetVdfPath(parameters.ContentFolder);

            if (parameters.ModId == 0)
            {
                // read mod id from file to be sure.
                if (File.Exists(vdfPath))
                {
                    parameters.ModId = ReadModId(File.ReadAllText(vdfPath));
                }
            }

            File.WriteAllText(vdfPath, string.Format(CultureInfo.InvariantCulture, WorkshopItemVcfFormat,
                parameters.ModId,
                parameters.ContentFolder.Replace(@"\", @"\\"),
                parameters.ImageFile.Replace(@"\", @"\\"),
                (int)parameters.Visibility,
                parameters.Title.Replace("\"", "\\\""),
                parameters.Description.Replace("\"", "\\\""),
                parameters.ChangeNote.Replace("\"", "\\\"")
            ));

            // StrongBox is basically just a reference type wrapper for a variable
            StrongBox<bool> wasExitFailure = new StrongBox<bool>(false);
            TaskCompletionSource<int> exitCodeSrc = new TaskCompletionSource<int>();
            CancellationTokenSource cts = new CancellationTokenSource();
            int? exitCode = null;
            try
            {
                string? workingDirectory = Path.GetDirectoryName(parameters.SteamCmdPath);

                string app = parameters.SteamCmdPath;

                string[] cl = [ app, $"+login {parameters.Username} {parameters.Password}", $"+workshop_build_item \"{vdfPath}\"", "+quit" ];
                int offset = 1;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    app = "/bin/bash";
                    offset = 0;
                }

                string commandLine = app + " " + string.Join(' ', cl, offset, cl.Length - offset);

                logger.LogInformation("Starting SteamCMD. Command line: {0}.", commandLine.Replace(parameters.Password, "[redacted]"));

                _connection = await PtyProvider.SpawnAsync(new PtyOptions
                {
                    App = app,
                    Cwd = workingDirectory ?? string.Empty,
                    Cols = 128,
                    Rows = 64,
                    CommandLine = cl
                }, cts.Token);

                logger.LogInformation("SteamCMD started. PID: {0}.", _connection.Pid);

                _connection.ProcessExited += (_, e) =>
                {
                    exitCode = e.ExitCode;
                    // ReSharper disable once AccessToDisposedClosure
                    cts.Cancel();
                    Thread.Sleep(5);
                    exitCodeSrc.TrySetResult(e.ExitCode);

                    // this should cancel the Read call (maybe)

                    // ReSharper disable once AccessToDisposedClosure
                    _connection?.Dispose();
                    _connection = null;
                };

                string fileLogPath = Path.Combine(UnturnedPaths.RootDirectory.FullName, "Logs", "steamcmd.ansi");

                Directory.CreateDirectory(Path.GetDirectoryName(fileLogPath)!);

                using FileStream fileLog = new FileStream(fileLogPath, FileMode.Create, FileAccess.Write, FileShare.Read, 1024, FileOptions.SequentialScan);

                LoggingEventStream outStream = new LoggingEventStream(fileLog);
                outStream.OutputReady += text => ProcessLineOutput(text, _connection, logger, wasExitFailure);

                Thread readerThread = new Thread(_ =>
                {
                    try
                    {
                        byte[] numArray = new byte[1024];
                        int count;
                        while (_connection?.ReaderStream != null && (count = _connection.ReaderStream.Read(numArray, 0, numArray.Length)) != 0)
                            outStream.Write(numArray.AsSpan(0, count));
                    }
                    catch (ThreadAbortException) { throw; }
                    catch (OperationCanceledException) when (cts.IsCancellationRequested) { }
                    catch (ObjectDisposedException)
                    {

                    }
                    catch (IOException ex)
                    {
                        // file can throw IOException after process exits
                        if (!exitCode.HasValue)
                            logger.LogError(ex, "Exception reading SteamCMD output.");
                    }
                });

                readerThread.Start();

                try
                {
                    await exitCodeSrc.Task;
                }
                finally
                {
                    readerThread.Abort();
                }

                logger.LogInformation("SteamCMD exited: {0}.", exitCode);
            }
            catch (IOException ex)
            {
                logger.LogError(ex, "IOException uploading a mod. Exit code: {0}.", exitCode);
                wasExitFailure.Value = true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unable to upload mod. Exit code: {0}.", exitCode);
                wasExitFailure.Value = true;
            }
            finally
            {
                _connection?.Dispose();
                _connection = null;
                cts.Dispose();
            }

            if (exitCode == 0 && !wasExitFailure.Value)
            {
                return ReadModId(File.ReadAllText(vdfPath));
            }

            return null;
        }
        finally
        {
            _connection = null;
            _sempahore.Release();
        }
    }

    /// <summary>
    /// Invoked with output from SteamCMD.
    /// </summary>
    private void ProcessLineOutput(string text, IPtyConnection connection, ILogger logger, StrongBox<bool> wasExitFailure)
    {
        //Console.Write(text);

        // SteamCMD may ask for a Steam Guard code which will have to be provided by me. I have the discord bot set up to DM me when this happens.
        if (text.Contains("Steam Guard code:", StringComparison.Ordinal))
        {
            WaitForSteamGuardCode(connection, logger, wasExitFailure);
        }
        // This usually occurs when the login details are incorrect.
        else if (text.Contains("to Steam Public...FAILED", StringComparison.Ordinal))
        {
            logger.LogError("SteamCMD login failed: invalid credentials.");

            // this is equivalent to pressing Ctrl + C in console.
            connection.WriterStream.Write("\x3"u8);
            connection.WriterStream.Flush();
            wasExitFailure.Value = true;
        }
    }

    private void WaitForSteamGuardCode(IPtyConnection connection, ILogger logger, StrongBox<bool> wasExitFailure)
    {
        Task.Factory.StartNew(async () =>
        {
            try
            {
                SendSteamGuardRequired();
            }
            catch (RpcException) { }

            logger.LogWarning("SteamCMD requires the Steam Guard code for the user uploading a mod. Run \"/wdev steamguard <code>\" to supply a code.");

            // default timeout is pretty high to give time for me to see the message
            TimeSpan delay = TimeSpan.FromMinutes(15d);

            Console.WriteLine("Waiting for steam guard code");
            
            DateTime start = DateTime.UtcNow;

            SteamCode = null;
            do
            {
                await Task.Delay(250);
            } while (DateTime.UtcNow - start < delay && SteamCode == null);

            try
            {
                if (SteamCode != null)
                {
                    logger.LogInformation("Received steam guard code.");
                    connection.WriterStream.Write(Encoding.ASCII.GetBytes(SteamCode + Environment.NewLine));
                }
                else
                {
                    logger.LogInformation("Steam Guard Code expired.");
                    // this is equivalent to pressing Ctrl + C in console.
                    connection.WriterStream.Write("\x3"u8);
                    wasExitFailure.Value = true;
                }
            }
            catch (ObjectDisposedException)
            {
                logger.LogInformation("Already exited.");
            }

            connection.WriterStream.Flush();

            SteamCode = null;

        }, TaskCreationOptions.LongRunning);
    }

    [RpcSend]
    protected virtual void SendSteamGuardRequired() => _ = RpcTask.NotImplemented;

    [RpcReceive]
    protected bool ReceiveSteamGuardRequired(string code)
    {
        SteamCode = code;
        return true;
    }

    /// <summary>
    /// Reads the 'publishedfileid' field from a vdf file.
    /// </summary>
    public static ulong ReadModId(string text)
    {
        Regex regex = new Regex("""
                                \"publishedfileid\"\s+\"(\d+)\"
                                """, RegexOptions.IgnoreCase);
        Match match = regex.Match(text);
        if (match.Success)
        {
            Group? grp = match.Groups.LastOrDefault<Group>();
            if (grp is { Value: { Length: > 0 } modIdStr } && ulong.TryParse(modIdStr, NumberStyles.Number, CultureInfo.InvariantCulture, out ulong modId))
                return modId;
        }

        return 0;
    }

    private class LoggingEventStream : Stream
    {
        private readonly FileStream _fs;

        private long _position;

        /// <inheritdoc />
        public override bool CanRead => false;

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override bool CanWrite => true;

        /// <inheritdoc />
        public override long Length => _position;

        /// <inheritdoc />
        public override long Position
        {
            get => _position;
            set => throw new NotSupportedException();
        }

        public event Action<string>? OutputReady;

        public LoggingEventStream(FileStream fs)
        {
            _fs = fs;
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override int Read(Span<byte> buffer)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override void Flush()
        {
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            Write(buffer.AsSpan(0, count));
        }

        /// <inheritdoc />
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            _fs.Write(buffer);
            _fs.Flush();

            _position += buffer.Length;

            string str = Encoding.ASCII.GetString(buffer);
            OutputReady?.Invoke(str);
        }
    }
}

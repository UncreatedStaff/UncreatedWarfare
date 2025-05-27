using Pty.Net;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Uncreated.Warfare.Steam;

public struct WorkshopUploadParameters
{
    [JsonInclude]
    public ulong ModId;
    [JsonInclude]
    public string SteamCmdPath;
    [JsonInclude]
    public string Username, Password;
    [JsonInclude]
    public string ContentFolder;
    [JsonInclude]
    public string ImageFile;
    [JsonInclude, JsonConverter(typeof(JsonStringEnumConverter<ESteamWorkshopVisibility>))]
    public ESteamWorkshopVisibility Visibility;
    [JsonInclude]
    public string Title, Description, ChangeNote;
    [JsonInclude]
    public string LogFileOutput;
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
///
/// <para>
/// Pty.Net does not work on Ubuntu when running Mono as it turns out.. I added a small hotfix below it that continuously checks the mod until it actually updates
/// using Steamworks API to notice when it finishes updating. Only downside is this doesn't look for the steam guard code.
/// </para>
///</remarks>
public static class WorkshopUploader
{
    private static IPtyConnection? _connection;

    internal static string? SteamCode;
    
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
    public static string GetVdfPath(string contentFolder)
    {
        return Path.GetFullPath(Path.Combine(contentFolder, "..", "mod_" + Path.GetFileName(contentFolder) + ".vdf"));
    }

    public static ulong ReadModIdFromFile(string fileName)
    {
        return File.Exists(fileName) ? ReadModId(File.ReadAllText(fileName)) : 0ul;
    }

    /// <summary>
    /// Uploads a mod to the Steam Workshop using SteamCMD.
    /// </summary>
    public static async Task<ulong?> UploadMod(WorkshopUploadParameters parameters, CancellationToken token = default)
    {
        try
        {
            string vdfPath = GetVdfPath(parameters.ContentFolder);

            if (parameters.ModId == 0)
            {
                // read mod id from file to be sure.
                parameters.ModId = ReadModIdFromFile(vdfPath);
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

                string[] cl = [ $"+login {parameters.Username} {parameters.Password}", $"+workshop_build_item \"{vdfPath}\"", "+quit" ];

                string commandLine = app + " " + string.Join(' ', cl);

                Console.WriteLine("Starting SteamCMD. Command line: {0}.", commandLine.Replace(parameters.Password, "[redacted]"));

                _connection = await PtyProvider.SpawnAsync(new PtyOptions
                {
                    App = app,
                    Cwd = workingDirectory ?? string.Empty,
                    Cols = 128,
                    Rows = 64,
                    CommandLine = cl
                }, cts.Token);

                Console.WriteLine("SteamCMD started. PID: {0}.", _connection.Pid);

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

                string fileLogPath = parameters.LogFileOutput;

                Directory.CreateDirectory(Path.GetDirectoryName(fileLogPath)!);

                using FileStream fileLog = new FileStream(fileLogPath, FileMode.Create, FileAccess.Write, FileShare.Read, 1024, FileOptions.SequentialScan);

                LoggingEventStream outStream = new LoggingEventStream(fileLog);
                outStream.OutputReady += text => ProcessLineOutput(text, _connection, wasExitFailure);

                Thread readerThread = new Thread(_ =>
                {
                    try
                    {
                        byte[] numArray = new byte[1024];
                        try
                        {
                            int count;
                            while (_connection?.ReaderStream != null && (count = _connection.ReaderStream.Read(numArray, 0, numArray.Length)) != 0)
                                outStream.Write(numArray.AsSpan(0, count));
                        }
                        catch { /* stream ended */ }
                    }
                    catch (ThreadAbortException) { throw; }
                    // ReSharper disable once AccessToDisposedClosure
                    catch (OperationCanceledException) when (cts.IsCancellationRequested) { }
                    catch (ObjectDisposedException)
                    {

                    }
                    catch (IOException ex)
                    {
                        // file can throw IOException after process exits
                        if (!exitCode.HasValue)
                        {
                            Console.WriteLine("Exception reading SteamCMD output.");
                            Console.WriteLine(ex);
                        }
                    }
                });

                readerThread.Start();

                await exitCodeSrc.Task;

                Console.WriteLine("SteamCMD exited: {0}.", exitCode);
            }
            catch (IOException ex)
            {
                Console.WriteLine("IOException uploading a mod. Exit code: {0}.", exitCode);
                Console.WriteLine(ex);
                wasExitFailure.Value = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to upload mod. Exit code: {0}.", exitCode);
                Console.WriteLine(ex);
                wasExitFailure.Value = true;
            }
            finally
            {
                try
                {
                    _connection?.Dispose();
                }
                catch (InvalidOperationException)
                {
                    // already disposed (thrown on linux)
                }
                finally
                {
                    _connection = null;
                }
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
        }
    }

    /// <summary>
    /// Invoked with output from SteamCMD.
    /// </summary>
    private static void ProcessLineOutput(string text, IPtyConnection connection, StrongBox<bool> wasExitFailure)
    {
        Console.Write(text);

        // SteamCMD may ask for a Steam Guard code which will have to be provided by me. I have the discord bot set up to DM me when this happens.
        if (text.Contains("Steam Guard code:", StringComparison.Ordinal))
        {
            WaitForSteamGuardCode(connection, wasExitFailure);
        }
        // This usually occurs when the login details are incorrect.
        else if (text.Contains("to Steam Public...FAILED", StringComparison.Ordinal))
        {
            Console.WriteLine("Login failed: invalid credentials.");

            // this is equivalent to pressing Ctrl + C in console.
            connection.WriterStream.Write("\x3"u8);
            connection.WriterStream.Flush();
            wasExitFailure.Value = true;
        }
    }

    private static void WaitForSteamGuardCode(IPtyConnection connection, StrongBox<bool> wasExitFailure)
    {
        Task.Factory.StartNew(() =>
        {
            Console.WriteLine("SteamCMD requires the Steam Guard code for the user uploading a mod. Run \"/wdev steamguard <code>\" to supply a code.");
            Console.WriteLine("Waiting for steam guard code...");
            
            SteamCode = Console.ReadLine();

            try
            {
                if (SteamCode != null)
                {
                    Console.WriteLine("Received steam guard code.");
                    connection.WriterStream.Write(Encoding.ASCII.GetBytes(SteamCode + Environment.NewLine));
                }
                else
                {
                    Console.WriteLine("Steam Guard Code expired.");
                    // this is equivalent to pressing Ctrl + C in console.
                    connection.WriterStream.Write("\x3"u8);
                    wasExitFailure.Value = true;
                }
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("Already exited.");
            }

            connection.WriterStream.Flush();

            SteamCode = null;

        }, TaskCreationOptions.LongRunning);
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
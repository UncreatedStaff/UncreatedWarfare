using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ReflectionTools;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Steam;

/// <summary>
/// Uploads workshop content using a process started locally.
/// </summary>
[Priority(100)]
public class LocalWorkshopUploader : IWorkshopUploader
{
    private readonly ILogger<LocalWorkshopUploader> _logger;
    private readonly WarfareModule _module;
    private readonly SemaphoreSlim _sempahore = new SemaphoreSlim(1, 1);

    /// <inheritdoc />
    public string? SteamCode
    {
        get => _steamCode;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                value = null;

            _steamCode = value;
            if (value == null)
                return;

            try
            {
                SteamCodeReceived?.Invoke(value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invoking SteamCodeReceived.");
            }
        }
    }

    /// <inheritdoc />
    public event Action<string?>? SteamCodeReceived;

    private ulong? _modId;
    private string? _steamCode;

    public LocalWorkshopUploader(ILogger<LocalWorkshopUploader> logger, WarfareModule module)
    {
        _logger = logger;
        _module = module;
    }

    private class UploadState
    {
        public bool WasExitFailure;
    }

    /// <summary>
    /// Uploads a mod to the Steam Workshop using SteamCMD.
    /// </summary>
    public async Task<ulong?> UploadMod(WorkshopUploadParameters parameters, CancellationToken token = default)
    {
        using IDisposable? scope = _logger.BeginScope("Upload");

        parameters.SteamCmdPath = Path.GetFullPath(parameters.SteamCmdPath);

        if (GameThread.IsCurrent)
        {
            await UniTask.SwitchToThreadPool();
        }

        string fileName, folderName;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            fileName = "WorkshopUploader.exe";
            folderName = "win64";
        }
        else
        {
            fileName = "WorkshopUploader";
            folderName = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx64" : "linux64";
        }

        string dllPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "..", "WorkshopUploader", folderName, fileName);
        if (!File.Exists(dllPath))
        {
            _logger.LogError($"WorkshopUploader not found at \"{dllPath}\".");
            return null;
        }

        await _sempahore.WaitAsync(token);
        try
        {
            _modId = null;

            int exitCode = -1;
            Process? process = null;
            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationTokenRegistration reg = token.Register(() =>
            {
                // ReSharper disable AccessToDisposedClosure
                if (process == null)
                    return;

                // this is equivalent to pressing Ctrl + C in console.
                lock (process)
                {
                    process.StandardInput.Write("\x3");
                    process.StandardInput.Flush();
                }

                Task.Run(async () =>
                {
                    await Task.Delay(1000, cts.Token);
                    try
                    {
                        if (!process.HasExited)
                            process.Kill();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to kill process after cancel.");
                    }
                }, CancellationToken.None);
                // ReSharper restore AccessToDisposedClosure
            });

            UploadState state = new UploadState();
            try
            {
                parameters.LogFileOutput = Path.Combine(UnturnedPaths.RootDirectory.FullName, "Logs", "steamcmd.ansi");

                string parametersFile = Path.Combine(_module.HomeDirectory, "Quests", "mod_upload_params.json");
                using (FileStream fs = new FileStream(parametersFile, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    JsonSerializer.Serialize(fs, parameters, typeof(WorkshopUploadParameters), WorkshopUploadParametersContext.Default);
                }

                ProcessStartInfo startInfo = new ProcessStartInfo(dllPath, $"\"{parametersFile}\"")
                {
                    // needed for Pty.NET on Ubuntu (see https://github.com/microsoft/vs-pty.net/issues/32) on .NET 7+
                    Environment =
                    {
                        { "DOTNET_EnableWriteXorExecute", "0" }
                    },
                    CreateNoWindow = true,
                    ErrorDialog = false,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true
                };

                process = Process.Start(startInfo);
                if (process == null)
                {
                    _logger.LogError($"Failed to start process {dllPath}.");
                    return null;
                }

                process.EnableRaisingEvents = true;
                _logger.LogTrace($"Started process: {process.Id}.");

                TaskCompletionSource<int> exitCodeSrc = new TaskCompletionSource<int>();

                process.BeginOutputReadLine();
                process.OutputDataReceived += (sender, args) =>
                {
                    _logger.LogDebug("Output: \"" + args.Data.Replace("\e", "[ESC]") + "\"");
                    ProcessLineOutput(args.Data, (Process)sender, state);
                };

                process.Exited += (_, _) =>
                {
                    _logger.LogTrace("Process exited.");
                    // ReSharper disable AccessToDisposedClosure
                    if (process == null)
                        return;

                    int exitCode = process.ExitCode;
                    _logger.LogTrace($"Process exited: {exitCode}.");
                    cts.Cancel();
                    exitCodeSrc.TrySetResult(exitCode);
                    // ReSharper restore AccessToDisposedClosure
                };

                await Task.WhenAny(Task.Delay(TimeSpan.FromMinutes(15), token), exitCodeSrc.Task);

                if (!exitCodeSrc.Task.IsCompleted)
                {
                    throw new TimeoutException("WorkshopUploader timed out after 15 mins.");
                }

                exitCode = exitCodeSrc.Task.Result;

                _logger.LogTrace("Waiting for exit... ({0})", exitCode);
                process.WaitForExit();
                _logger.LogTrace("WorkshopUploader exited: {0}.", exitCode);
                process.Dispose();
                process = null;
            }
            catch (Exception ex)
            {
                try
                {
                    if (process != null)
                        exitCode = process.ExitCode;
                }
                catch { /* ignored */ }

                if (exitCode == -1)
                    _logger.LogError(ex, "Unable to upload mod. Unknown exit code.");
                else
                    _logger.LogError(ex, "Unable to upload mod. Exit code: {0}.", exitCode);

                state.WasExitFailure = true;
            }
            finally
            {
                cts.Dispose();
                process?.Dispose();
                reg.Dispose();
            }

            return exitCode == 0 && !state.WasExitFailure ? _modId : null;
        }
        finally
        {
            _sempahore.Release();
        }
    }

    /// <summary>
    /// Invoked with output from the WorkshopUploader program.
    /// </summary>
    private void ProcessLineOutput(string text, Process process, UploadState state)
    {
        if (text.Equals("Waiting for steam guard code...", StringComparison.Ordinal))
        {
            WaitForSteamGuardCode(process, _logger, state);
        }
        else if (text.Equals("Login failed: invalid credentials.", StringComparison.Ordinal))
        {
            _logger.LogError("SteamCMD encountered an error logging into a Steam account.");
        }
        else if (text.Equals("Received steam guard code.", StringComparison.Ordinal))
        {
            _logger.LogInformation("Supplied Steam Guard code to SteamCMD.");
        }
        else if (text.Equals("Steam Guard Code expired.", StringComparison.Ordinal))
        {
            _logger.LogError("Failed to supply a Steam Guard code to SteamCMD in time.");
        }
        else if (text.StartsWith("Complete. Mod ID: ", StringComparison.Ordinal)
                 && ulong.TryParse(text.AsSpan(18), NumberStyles.Number, CultureInfo.InvariantCulture, out ulong modId))
        {
            _logger.LogDebug($"Mod upload completed. ID: {modId}.");
            _modId = modId;
        }
    }

    private class SteamCodeListener(CancellationTokenSource cancellationTokenSource)
    {
        public void OnReceived(string? steamCode)
        {
            cancellationTokenSource.Cancel();
        }
    }

    private void WaitForSteamGuardCode(Process process, ILogger logger, UploadState state)
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
            
            CancellationTokenSource src = new CancellationTokenSource(delay);
            SteamCodeListener listener = new SteamCodeListener(src);
            SteamCodeReceived += listener.OnReceived;
            try
            {
                await Task.Delay(delay, src.Token);
            }
            catch (OperationCanceledException) { }
            finally
            {
                SteamCodeReceived -= listener.OnReceived;
                src.Dispose();
            }

            lock (process)
            {
                try
                {
                    if (SteamCode != null)
                    {
                        logger.LogInformation("Received steam guard code.");
                        process.StandardInput.WriteLine(SteamCode);
                    }
                    else
                    {
                        logger.LogWarning("Steam Guard Code expired.");

                        // this is equivalent to pressing Ctrl + C in console.
                        process.StandardInput.Write("\x3");
                        state.WasExitFailure = true;
                    }
                }
                catch (ObjectDisposedException)
                {
                    logger.LogInformation("Already exited.");
                }

                process.StandardInput.Flush();
            }
            
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
            Group? grp = match.Groups.AsEnumerable<Group>().LastOrDefault();
            if (grp is { Value: { Length: > 0 } modIdStr } && ulong.TryParse(modIdStr, NumberStyles.Number, CultureInfo.InvariantCulture, out ulong modId))
                return modId;
        }

        return 0;
    }
}

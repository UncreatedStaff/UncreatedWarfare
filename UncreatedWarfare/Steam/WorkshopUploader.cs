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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Steam;

public class WorkshopUploadParameters
{
    public required ulong ModId { get; set; }
    public required string SteamCmdPath { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
    public required string ContentFolder { get; set; }
    public required string ImageFile { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter<ESteamWorkshopVisibility>))]
    public required ESteamWorkshopVisibility Visibility { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string ChangeNote { get; set; }
    public string? LogFileOutput { get; set; }
}

public enum ESteamWorkshopVisibility : byte
{
    Public = 0,
    FriendsOnly = 1,
    Hidden = 2,
    Unlisted = 3
}

[JsonSerializable(typeof(ESteamWorkshopVisibility), GenerationMode = JsonSourceGenerationMode.Serialization | JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(WorkshopUploadParameters), GenerationMode = JsonSourceGenerationMode.Serialization | JsonSourceGenerationMode.Metadata)]
internal partial class WorkshopUploadParametersContext : JsonSerializerContext;

/// <summary>
/// Handles programmatically uploading workshop files.
/// </summary>
/// <remarks>
/// <para>
/// This uses the SteamCMD CLI to upload the mod to the workshop using predefined credentials in the config.
/// </para>
///</remarks>
[Priority(100)]
public class WorkshopUploader
{
    private readonly ILogger<WorkshopUploader> _logger;
    private readonly WarfareModule _module;
    private readonly SemaphoreSlim _sempahore = new SemaphoreSlim(1, 1);

    internal static string? SteamCode;

    private ulong? _modId;

    public WorkshopUploader(ILogger<WorkshopUploader> logger, WarfareModule module)
    {
        _logger = logger;
        _module = module;
    }

    /// <summary>
    /// Uploads a mod to the Steam Workshop using SteamCMD.
    /// </summary>
    public async Task<ulong?> UploadMod(WorkshopUploadParameters parameters, ILogger? logger, CancellationToken token = default)
    {
        logger ??= _logger;

        using IDisposable? scope = logger.BeginScope("Upload");

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

        Console.WriteLine("2");
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
                        logger.LogWarning(ex, "Failed to kill process after cancel.");
                    }
                }, CancellationToken.None);
                // ReSharper restore AccessToDisposedClosure
            });

            StrongBox<bool> wasExitFailure = new StrongBox<bool>();
            try
            {
                parameters.LogFileOutput = Path.Combine(UnturnedPaths.RootDirectory.FullName, "Logs", "steamcmd.ansi");

                string parametersFile = Path.Combine(_module.HomeDirectory, "Quests", "mod_upload_params.json");
                using (FileStream fs = new FileStream(parametersFile, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    JsonSerializer.Serialize(fs, parameters, typeof(WorkshopUploadParameters), WorkshopUploadParametersContext.Default);
                }

                Console.WriteLine("3");
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

                Console.WriteLine("4");
                process = Process.Start(startInfo);
                if (process == null)
                {
                    logger.LogError($"Failed to start process {dllPath}.");
                    return null;
                }

                Console.WriteLine("5");
                process.EnableRaisingEvents = true;
                logger.LogTrace($"Started process: {process.Id}.");

                TaskCompletionSource<int> exitCodeSrc = new TaskCompletionSource<int>();

                process.BeginOutputReadLine();
                process.OutputDataReceived += (sender, args) =>
                {
                    logger.LogDebug("Output: \"" + args.Data.Replace("\e", "[ESC]") + "\"");
                    ProcessLineOutput(args.Data, (Process)sender, logger, wasExitFailure);
                };

                process.Exited += (_, _) =>
                {
                    logger.LogTrace("Process exited.");
                    // ReSharper disable AccessToDisposedClosure
                    if (process == null)
                        return;

                    int exitCode = process.ExitCode;
                    logger.LogTrace($"Process exited: {exitCode}.");
                    cts.Cancel();
                    exitCodeSrc.TrySetResult(exitCode);
                    // ReSharper restore AccessToDisposedClosure
                };

                Console.WriteLine("6");
                await Task.WhenAny(Task.Delay(TimeSpan.FromMinutes(15), token), exitCodeSrc.Task);

                if (!exitCodeSrc.Task.IsCompleted)
                {
                    throw new TimeoutException("WorkshopUploader timed out after 15 mins.");
                }

                exitCode = exitCodeSrc.Task.Result;

                logger.LogTrace("Waiting for exit... ({0})", exitCode);
                process.WaitForExit();
                logger.LogTrace("WorkshopUploader exited: {0}.", exitCode);
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
                    logger.LogError(ex, "Unable to upload mod. Unknown exit code.");
                else
                    logger.LogError(ex, "Unable to upload mod. Exit code: {0}.", exitCode);

                wasExitFailure.Value = true;
            }
            finally
            {
                cts.Dispose();
                process?.Dispose();
                reg.Dispose();
            }

            return exitCode == 0 && !wasExitFailure.Value ? _modId : null;
        }
        finally
        {
            _sempahore.Release();
        }
    }

    /// <summary>
    /// Invoked with output from the WorkshopUploader program.
    /// </summary>
    private void ProcessLineOutput(string text, Process process, ILogger logger, StrongBox<bool> wasExitFailure)
    {
        if (text.Equals("Waiting for steam guard code...", StringComparison.Ordinal))
        {
            WaitForSteamGuardCode(process, logger, wasExitFailure);
        }
        else if (text.Equals("Login failed: invalid credentials.", StringComparison.Ordinal))
        {
            logger.LogError("SteamCMD encountered an error logging into a Steam account.");
        }
        else if (text.Equals("Received steam guard code.", StringComparison.Ordinal))
        {
            logger.LogInformation("Supplied Steam Guard code to SteamCMD.");
        }
        else if (text.Equals("Steam Guard Code expired.", StringComparison.Ordinal))
        {
            logger.LogError("Failed to supply a Steam Guard code to SteamCMD in time.");
        }
        else if (text.StartsWith("Complete. Mod ID: ", StringComparison.Ordinal)
                 && ulong.TryParse(text.AsSpan(18), NumberStyles.Number, CultureInfo.InvariantCulture, out ulong modId))
        {
            logger.LogDebug($"Mod upload completed. ID: {modId}.");
            _modId = modId;
        }
    }

    private void WaitForSteamGuardCode(Process process, ILogger logger, StrongBox<bool> wasExitFailure)
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

            DateTime start = DateTime.UtcNow;

            SteamCode = null;
            do
            {
                await Task.Delay(250);
            } while (DateTime.UtcNow - start < delay && SteamCode == null);

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
                        wasExitFailure.Value = true;
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

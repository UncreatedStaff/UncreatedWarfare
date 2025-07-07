using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Routing;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Uncreated.Warfare.Steam;

/// <summary>
/// Uploads workshop items using homebase.
/// </summary>
[GenerateRpcSource]
public partial class RemoteWorkshopUploader : IWorkshopUploader
{
    private readonly ILogger<RemoteWorkshopUploader> _logger;
    private readonly IRpcConnectionLifetime _connectionLifetime;
    private readonly SemaphoreSlim _semaphore;

    private readonly string? _username;
    private readonly string? _password;
    private readonly string? _steamCmdPath;
    private string? _steamCode;

    /// <summary>
    /// A steam code that just got received.
    /// </summary>
    public string? SteamCode
    {
        get => _steamCode;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                value = null;

            if (WarfareModule.IsActive)
            {
                SendSteamCode(value);
            }

            ReceiveSteamCode(value);
        }
    }

    public event Action<string?>? SteamCodeReceived;

    public RemoteWorkshopUploader(ILogger<RemoteWorkshopUploader> logger, IRpcConnectionLifetime connectionLifetime, IConfiguration configuration)
    {
        _logger = logger;
        _connectionLifetime = connectionLifetime;
        _semaphore = new SemaphoreSlim(1, 1);

        IConfigurationSection section = configuration.GetSection("Steam");
        _username = section["Username"];
        _password = section["Password"];
        _steamCmdPath = section["SteamCMDPath"];
        if (!string.IsNullOrEmpty(_password))
            _password = Encoding.UTF8.GetString(Convert.FromBase64String(_password));
    }

    [RpcSend(nameof(ReceiveSteamCode))]
    protected partial void SendSteamCode(string? code);

    [RpcReceive]
    private void ReceiveSteamCode(string? code)
    {
        _steamCode = code;
        if (code == null)
            return;

        try
        {
            SteamCodeReceived?.Invoke(code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking SteamCodeReceived.");
        }
    }

    [RpcSend(nameof(ReceiveUploadMod)), RpcTimeout(Timeouts.Minutes * 15)]
    protected partial RpcTask<ulong?> RemoteUploadMod(
        ulong modId,
        SteamWorkshopVisibility visibility,
        string title,
        string description,
        string changeNote,
        byte[]? image,
        string? imageExt,
        byte[] zipData,
        CancellationToken token
    );

    /// <inheritdoc />
    public async Task<ulong?> UploadMod(WorkshopUploadParameters parameters, CancellationToken token = default)
    {
        if (_connectionLifetime.ForEachRemoteConnection(_ => false) < 1)
        {
            _logger.LogWarning("No connection to server, try again later.");
            return null;
        }

        byte[] zipData;

        using (MemoryStream ms = new MemoryStream(524288))
        {
            using (ZipArchive archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true, Encoding.UTF8))
            {
                foreach (string file in Directory.EnumerateFiles(parameters.ContentFolder, "*.*", SearchOption.AllDirectories))
                {
                    archive.CreateEntryFromFile(file, Path.GetRelativePath(parameters.ContentFolder, file));
                }
            }

            zipData = ms.ToArray();
        }

        try
        {
            byte[]? image = File.Exists(parameters.ImageFile) ? File.ReadAllBytes(parameters.ImageFile) : null;
            return await RemoteUploadMod(
                parameters.ModId,
                parameters.Visibility,
                parameters.Title,
                parameters.Description,
                parameters.ChangeNote,
                image,
                image == null ? null : Path.GetExtension(parameters.ImageFile),
                zipData,
                token
            );
        }
        catch (RpcInvocationException ex)
        {
            _logger.LogError(ex, "Failed to upload workshop mod.");
            return null;
        }
    }

    [RpcReceive]
    private async Task<ulong?> ReceiveUploadMod(ulong modId,
        SteamWorkshopVisibility visibility,
        string title,
        string description,
        string changeNote,
        byte[]? image,
        string? imageExt,
        byte[] zipData, // todo use array segment after fixing rpc lib
        CancellationToken token)
    {
        if (string.IsNullOrEmpty(_password))
            throw new InvalidOperationException("Password not configured.");
        if (string.IsNullOrEmpty(_username))
            throw new InvalidOperationException("Username not configured.");
        if (string.IsNullOrEmpty(_steamCmdPath))
            throw new InvalidOperationException("SteamCMD path not configured.");

        await _semaphore.WaitAsync(token);
        try
        {
            string contentFolder = Path.GetFullPath(Path.Combine("Steam", "Temp_Workshop"));
            string? imagePath = image == null ? null : Path.GetFullPath(Path.Combine("Steam", "icon" + imageExt));

            if (Directory.Exists(contentFolder))
            {
                Directory.Delete(contentFolder, recursive: true);
            }

            Directory.CreateDirectory(contentFolder);

            if (imagePath != null)
            {
                File.WriteAllBytes(imagePath, image!);
            }

            using (MemoryStream ms = new MemoryStream(zipData))
            using (ZipArchive archive = new ZipArchive(ms, ZipArchiveMode.Read, leaveOpen: true, Encoding.UTF8))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string fullPath = Path.Combine(contentFolder, entry.FullName);
                    string? directory = Path.GetDirectoryName(fullPath);
                    if (directory != null)
                        Directory.CreateDirectory(directory);
                    await using FileStream fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.Read);
                    await using Stream stream = entry.Open();
                    await stream.CopyToAsync(fs, token);
                }
            }

            WorkshopUploadParameters p = new WorkshopUploadParameters
            {
                Visibility = visibility,
                ChangeNote = changeNote,
                ContentFolder = contentFolder,
                ImageFile = imagePath ?? string.Empty,
                Description = description,
                ModId = modId,
                Title = title,
                SteamCmdPath = _steamCmdPath,
                Password = _password,
                Username = _username
            };

            return await UploadModImpl(p, token).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    protected virtual Task<ulong?> UploadModImpl(WorkshopUploadParameters parameters, CancellationToken token) =>
        throw new NotSupportedException();
}

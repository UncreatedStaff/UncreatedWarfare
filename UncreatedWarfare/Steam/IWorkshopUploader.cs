using System;
using System.Text.Json.Serialization;

namespace Uncreated.Warfare.Steam;

/// <summary>
/// Handles programmatically uploading workshop files.
/// </summary>
/// <remarks>
/// <para>
/// This uses the SteamCMD CLI to upload the mod to the workshop using predefined credentials in the config.
/// </para>
///</remarks>
public interface IWorkshopUploader
{
    string? SteamCode { get; set; }

    event Action<string?>? SteamCodeReceived;

    Task<ulong?> UploadMod(WorkshopUploadParameters parameters, CancellationToken token = default);
}

public class WorkshopUploadParameters
{
    public required ulong ModId { get; set; }
    public required string SteamCmdPath { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
    public required string ContentFolder { get; set; }
    public required string ImageFile { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter<SteamWorkshopVisibility>))]
    public required SteamWorkshopVisibility Visibility { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string ChangeNote { get; set; }
    public string? LogFileOutput { get; set; }
}

public enum SteamWorkshopVisibility : byte
{
    Public = 0,
    FriendsOnly = 1,
    Hidden = 2,
    Unlisted = 3
}

[JsonSerializable(typeof(SteamWorkshopVisibility), GenerationMode = JsonSourceGenerationMode.Serialization | JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(WorkshopUploadParameters), GenerationMode = JsonSourceGenerationMode.Serialization | JsonSourceGenerationMode.Metadata)]
internal partial class WorkshopUploadParametersContext : JsonSerializerContext;
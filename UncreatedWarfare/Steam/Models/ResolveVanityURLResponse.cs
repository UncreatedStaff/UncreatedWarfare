using System.Text.Json.Serialization;

namespace Uncreated.Warfare.Steam.Models;

internal class ResolveVanityURLResponse
{
    [JsonPropertyName("response")]
    public required ResolveVanityURLResponseInfo Response { get; set; }
}

internal class ResolveVanityURLResponseInfo
{
    [JsonPropertyName("success")]
    public required int StatusCode { get; set; }

    [JsonPropertyName("steamid")]
    public string? SteamId { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
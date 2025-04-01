using System.Text.Json.Serialization;
using Uncreated.Warfare.Configuration.JsonConverters;

namespace Uncreated.Warfare.Steam.Models;

#nullable disable
public class PlayerSummariesResponse
{
    [JsonPropertyName("response")]
    public PlayerSummariesResponseData Data { get; set; }
}

public class PlayerSummariesResponseData
{
    [JsonPropertyName("players")]
    public PlayerSummary[] Results { get; set; }
}

public class PlayerSummary
{
    [JsonPropertyName("steamid")]
    [JsonConverter(typeof(UInt64StringConverter))]
    public ulong Steam64 { get; set; }

    [JsonPropertyName("communityvisibilitystate")]
    public int Visibility { get; set; }

    [JsonPropertyName("profilestate")]
    public int ProfileState { get; set; }

    [JsonPropertyName("personaname")]
    public string PlayerName { get; set; }

    [JsonPropertyName("commentpermission")]
    public int CommentPermissionLevel { get; set; }

    [JsonPropertyName("profileurl")]
    public string ProfileUrl { get; set; }

    [JsonPropertyName("avatar")]
    public string AvatarUrlSmall { get; set; }

    [JsonPropertyName("avatarmedium")]
    public string AvatarUrlMedium { get; set; }

    [JsonPropertyName("avatarfull")]
    public string AvatarUrlFull { get; set; }

    [JsonPropertyName("avatarhash")]
    public string AvatarHash { get; set; }

    [JsonPropertyName("lastlogoff")]
    public long LastLogOff { get; set; }

    [JsonPropertyName("personastate")]
    public int PlayerState { get; set; }

    [JsonPropertyName("realname")]
    public string RealName { get; set; }

    [JsonPropertyName("primaryclanid")]
    [JsonConverter(typeof(UInt64StringConverter))]
    public ulong PrimaryGroupId { get; set; }

    [JsonPropertyName("timecreated")]
    public long TimeCreated { get; set; }

    [JsonPropertyName("personastateflags")]
    public int PlayerStateFlags { get; set; }

    [JsonPropertyName("loccountrycode")]
    public string CountryCode { get; set; }

    [JsonPropertyName("locstatecode")]
    public string RegionCode { get; set; }
}
#nullable restore
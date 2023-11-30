using System;
using System.Globalization;
using System.Text.Json.Serialization;
using Uncreated.SQL;

namespace Uncreated.Warfare.Moderation;
public class PlayerHWID : IListItem
{
    [JsonPropertyName("id")]
    public PrimaryKey PrimaryKey { get; set; }

    [JsonPropertyName("hwid")]
    public HWID HWID { get; set; }

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("login_count")]
    public int LoginCount { get; set; }

    [JsonPropertyName("steam64")]
    public ulong Steam64 { get; set; }

    [JsonPropertyName("first_login")]
    public DateTimeOffset? FirstLogin { get; set; }

    [JsonPropertyName("last_login")]
    public DateTimeOffset LastLogin { get; set; }
    
    public PlayerHWID() { }
    public PlayerHWID(PrimaryKey primaryKey, int index, ulong steam64, HWID hwid, int loginCount, DateTimeOffset? firstLogin, DateTimeOffset lastLogin)
    {
        PrimaryKey = primaryKey;
        Index = index;
        Steam64 = steam64;
        HWID = hwid;
        LoginCount = loginCount;
        FirstLogin = firstLogin;
        LastLogin = lastLogin;
    }

    public override string ToString() => "# " + Index.ToString(CultureInfo.InvariantCulture) + ", HWID: " + HWID;
}
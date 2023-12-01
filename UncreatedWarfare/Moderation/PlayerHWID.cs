using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Database.Automation;
using Uncreated.Warfare.Models.Users;

namespace Uncreated.Warfare.Moderation;

[Table("hwids")]
public class PlayerHWID
{
    [Key]
    [JsonPropertyName("id")]
    public uint Id { get; set; }

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [Required]
    [Column("Steam64")]
    [JsonPropertyName("steam64")]
    [ForeignKey(nameof(PlayerData))]
    public ulong Steam64 { get; set; }
    public WarfareUserData PlayerData { get; set; }

    [Index]
    [JsonPropertyName("hwid")]
    public HWID HWID { get; set; }

    [JsonPropertyName("login_count")]
    public int LoginCount { get; set; }

    [JsonPropertyName("last_login")]
    public DateTimeOffset LastLogin { get; set; }

    [JsonPropertyName("first_login")]
    [DefaultValue(null)]
    public DateTimeOffset? FirstLogin { get; set; }
    
    public PlayerHWID() { }
    public PlayerHWID(uint id, int index, ulong steam64, HWID hwid, int loginCount, DateTimeOffset? firstLogin, DateTimeOffset lastLogin)
    {
        Id = id;
        Index = index;
        Steam64 = steam64;
        HWID = hwid;
        LoginCount = loginCount;
        FirstLogin = firstLogin;
        LastLogin = lastLogin;
    }

    public override string ToString() => "# " + Index.ToString(CultureInfo.InvariantCulture) + ", HWID: " + HWID;
}
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Models.Users;

namespace Uncreated.Warfare.Moderation;

[Table(DatabaseInterface.TableHWIDs), Index(nameof(HWID))]
public class PlayerHWID
{
    [Key]
    [JsonPropertyName("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column(DatabaseInterface.ColumnHWIDsPrimaryKey)]
    public uint Id { get; set; }

    [JsonPropertyName("index")]
    [Column(DatabaseInterface.ColumnHWIDsIndex)]
    public int Index { get; set; }

    [Required]
    [JsonPropertyName("steam64")]
    [ForeignKey(nameof(PlayerData))]
    [Column(DatabaseInterface.ColumnHWIDsSteam64)]
    public ulong Steam64 { get; set; }
    public WarfareUserData PlayerData { get; set; }

    [JsonPropertyName("hwid")]
    [Column(DatabaseInterface.ColumnHWIDsHWID)]
    public HWID HWID { get; set; }

    [JsonPropertyName("login_count")]
    [Column(DatabaseInterface.ColumnHWIDsLoginCount)]
    public int LoginCount { get; set; }

    [JsonPropertyName("last_login")]
    [Column(DatabaseInterface.ColumnHWIDsLastLogin)]
    public DateTimeOffset LastLogin { get; set; }

    [JsonPropertyName("first_login")]
    [DefaultValue(null)]
    [Column(DatabaseInterface.ColumnHWIDsFirstLogin)]
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
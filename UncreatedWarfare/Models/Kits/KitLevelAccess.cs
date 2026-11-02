using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Database.Automation;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Models.Seasons;
using Uncreated.Warfare.Models.Users;

namespace Uncreated.Warfare.Models.Kits;

#nullable disable

/// <summary>
/// Designates access to a public kit purchased with credits.
/// </summary>
[Table("kits_level_access")]
public class KitLevelAccess
{
    [Required]
    [ForeignKey(nameof(PlayerData))]
    [Column("Steam64")]
    public ulong Steam64 { get; set; }

    [Required]
    [ExcludedEnum(Class.None)]
    [ExcludedEnum(Class.Unarmed)]
    public Class Class { get; set; }

    [Required]
    public byte Level { get; set; }

    [Required]
    [ForeignKey(nameof(Season))]
    [Column("Season")]
    public int SeasonId { get; set; }

    [Required, JsonIgnore]
    public WarfareUserData PlayerData { get; set; }

    [Required, JsonIgnore]
    public SeasonData Season { get; set; }

    [Column("GivenAt")]
    public DateTimeOffset Timestamp { get; set; }
}
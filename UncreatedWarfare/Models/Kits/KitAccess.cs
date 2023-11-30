using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Models.Users;

namespace Uncreated.Warfare.Models.Kits;

[Table("kits_access")]
public class KitAccess
{
    [Required]
    public Kit Kit { get; set; }

    [Required]
    [ForeignKey(nameof(Kit))]
    [Column("Kit")]
    public uint KitId { get; set; }

    [Required]
    [ForeignKey(nameof(PlayerData))]
    [Column("Steam64")]
    public ulong Steam64 { get; set; }

    [Required]
    public WarfareUserData PlayerData { get; set; }
    public KitAccessType AccessType { get; set; }

    [Column("GivenAt")]
    public DateTimeOffset Timestamp { get; set; }
}
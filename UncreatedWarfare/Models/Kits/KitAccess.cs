using System;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Kits;

namespace Uncreated.Warfare.Models.Kits;

[Table("kits_access")]
public class KitAccess
{
    public Kit Kit { get; set; }

    [ForeignKey(nameof(Kit))]
    public uint KitId { get; set; }
    public ulong Steam64 { get; set; }
    public KitAccessType AccessType { get; set; }

    [Column("GivenAt")]
    public DateTimeOffset Timestamp { get; set; }
}
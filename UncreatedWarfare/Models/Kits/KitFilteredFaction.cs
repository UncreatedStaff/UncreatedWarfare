using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Models.Factions;

namespace Uncreated.Warfare.Models.Kits;

[Table("kits_faction_filters")]
public class KitFilteredFaction : ICloneable
{
    [Required]
    public Kit Kit { get; set; }

    [Required]
    [ForeignKey(nameof(Kit))]
    [Column("Kit")]
    public uint KitId { get; set; }

    [Required]
    public Faction Faction { get; set; }

    [ForeignKey(nameof(Faction))]
    [Required]
    [Column("Faction")]
    public uint FactionId { get; set; }

    public object Clone()
    {
        return new KitFilteredFaction
        {
            Faction = Faction,
            Kit = Kit
        };
    }
}

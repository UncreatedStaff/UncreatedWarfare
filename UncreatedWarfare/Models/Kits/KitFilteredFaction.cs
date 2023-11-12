using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Models.Factions;

namespace Uncreated.Warfare.Models.Kits;

[Table("kits_faction_filters")]
public class KitFilteredFaction : ICloneable
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("pk")]
    public uint Id { get; set; }

    [Required]
    public Kit Kit { get; set; }

    [Required]
    public Faction Faction { get; set; }

    public object Clone()
    {
        return new KitFilteredFaction
        {
            Faction = Faction,
            Kit = Kit
        };
    }
}

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Uncreated.Warfare.Models.Kits;

[Table("kits_faction_filters")]
public class KitFilteredFaction : ICloneable
{
    [Required, Column("Kit")]
    public uint KitId { get; set; }

    [Required, Column("Faction")]
    public uint FactionId { get; set; }

    public object Clone()
    {
        return new KitFilteredFaction
        {
            FactionId = FactionId,
            KitId = KitId
        };
    }
}

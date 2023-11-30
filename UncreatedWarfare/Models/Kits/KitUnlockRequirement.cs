using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Models.Base;

namespace Uncreated.Warfare.Models.Kits;

[Table("kits_unlock_requirements")]
public class KitUnlockRequirement : BaseUnlockRequirement, ICloneable
{
    [Required]
    public Kit Kit { get; set; }
    
    [Required]
    [ForeignKey(nameof(Kit))]
    [Column("Kit", Order = 1)]
    public uint KitId { get; set; }

    public object Clone()
    {
        return new KitUnlockRequirement
        {
            KitId = KitId,
            Kit = Kit,
            Json = Json
        };
    }
}

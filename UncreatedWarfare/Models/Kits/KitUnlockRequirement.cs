using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Models.Base;

namespace Uncreated.Warfare.Models.Kits;

[Table("kits_unlock_requirements")]
public class KitUnlockRequirement : BaseUnlockRequirement, ICloneable
{
    [Required, Column("Kit", Order = 1)]
    public uint KitId { get; set; }

    public KitUnlockRequirement() { }

    public KitUnlockRequirement(KitUnlockRequirement other)
    {
        KitId = other.KitId;
    }

    public override object Clone()
    {
        return new KitUnlockRequirement(this)
        {
            Data = Data,
            Type = Type,
            Id = Id
        };
    }
}

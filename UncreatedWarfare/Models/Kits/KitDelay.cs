using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Uncreated.Warfare.Models.Base;

namespace Uncreated.Warfare.Models.Kits;

#nullable disable

[Table("kits_delays")]
public class KitDelay : BaseDelay
{
    [Required, Column("Kit")]
    public uint KitId { get; set; }

    public KitDelay() { }

    [SetsRequiredMembers]
    public KitDelay(KitDelay other) : base(other)
    {
        KitId = other.KitId;
    }

    public override object Clone()
    {
        return new KitDelay(this);
    }
}
#nullable restore
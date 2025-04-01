using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Models.Base;

namespace Uncreated.Warfare.Models.Kits;

[Table("kits_sign_text")]
public class KitTranslation : BaseTranslation
{
    [Required, Column("Kit", Order = 1)]
    public uint KitId { get; set; }

    public KitTranslation() { }
    public KitTranslation(KitTranslation other) : base(other)
    {
        KitId = other.KitId;
    }

    public override object Clone() => new KitTranslation(this);
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Models.Base;

namespace Uncreated.Warfare.Models.Kits;

[Table("kits_skillsets")]
public class KitSkillset : BaseSkillset
{
    [Required, Column("Kit", Order = 1)]
    public uint KitId { get; set; }
    public KitSkillset() { }
    public KitSkillset(KitSkillset other) : base(other)
    {
        KitId = other.KitId;
    }
    public override object Clone() => new KitSkillset(this);
}

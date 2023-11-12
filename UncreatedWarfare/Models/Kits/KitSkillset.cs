using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Models.Base;

namespace Uncreated.Warfare.Models.Kits;

[Table("kits_skillsets")]
public class KitSkillset : BaseSkillset
{
    public Kit Kit { get; set; }
    public KitSkillset() { }
    public KitSkillset(KitSkillset other) : base(other)
    {
        Kit = other.Kit;
    }
    public override object Clone() => new KitSkillset(this);
}

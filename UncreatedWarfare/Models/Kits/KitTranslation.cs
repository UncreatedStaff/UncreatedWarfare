using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Models.Base;

namespace Uncreated.Warfare.Models.Kits;

[Table("kits_sign_text")]
public class KitTranslation : BaseTranslation
{
    public Kit Kit { get; set; }
}

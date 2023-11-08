using System.ComponentModel.DataAnnotations.Schema;

namespace Uncreated.Warfare.Models.Localization;

[Table("lang_credits")]
public class LanguageContributor
{
    public int Language { get; set; }
    public ulong Contributor { get; set; }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Uncreated.Warfare.Models.Localization;

[Table("lang_aliases")]
public class LanguageAlias
{
    public int Language { get; set; }

    [MaxLength(64)]
    [Required]
    public string Alias { get; set; }
}

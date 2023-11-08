using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Uncreated.Warfare.Models.Localization;

[Table("lang_cultures")]
public class LanguageCulture
{
    public int Language { get; set; }

    [MaxLength(16)]
    [Required]
    public string CultureCode { get; set; }
}

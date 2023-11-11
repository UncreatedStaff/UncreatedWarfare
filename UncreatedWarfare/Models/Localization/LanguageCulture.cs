using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Uncreated.Warfare.Models.Localization;

[Table("lang_cultures")]
public class LanguageCulture
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("pk")]
    public uint Id { get; set; }

    [Column("Langauge")]
    public LanguageInfo Language { get; set; } = null!;

    [MaxLength(16)]
    [Required]
    public string CultureCode { get; set; } = null!;
}
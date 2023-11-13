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

    [Required]
    public LanguageInfo Language { get; set; } = null!;

    [Required]
    [Column("Langauge")]
    [ForeignKey(nameof(Language))]
    public uint LanguageId { get; set; }

    [MaxLength(16)]
    [Required]
    public string CultureCode { get; set; } = null!;
}
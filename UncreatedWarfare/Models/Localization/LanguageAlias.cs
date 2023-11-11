using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Uncreated.Warfare.Models.Localization;

[Table("lang_aliases")]
public class LanguageAlias
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("pk")]
    public uint Id { get; set; }

    [Column("Langauge")]
    public LanguageInfo Language { get; set; } = null!;

    [MaxLength(64)]
    [Required]
    public string Alias { get; set; } = null!;
}
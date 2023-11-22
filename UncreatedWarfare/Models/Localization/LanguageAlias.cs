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

    [Required]
    public LanguageInfo Language { get; set; } = null!;

    [Required]
    [Column("Language")]
    [ForeignKey(nameof(Language))]
    public uint LanguageId { get; set; }

    [MaxLength(64)]
    [Required]
    public string Alias { get; set; } = null!;
}
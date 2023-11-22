using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Uncreated.Warfare.Models.Localization;

[Table("lang_credits")]
public class LanguageContributor
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
    public ulong Contributor { get; set; }
}
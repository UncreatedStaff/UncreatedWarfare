using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Uncreated.Warfare.Models.Base;
public abstract class BaseTranslation
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("pk")]
    public uint Id { get; set; }

    [Required]
    [ForeignKey(nameof(LanguageId))]
    public Localization.LanguageInfo Language { get; set; } = null!;

    [Required]
    public uint LanguageId { get; set; }

    [MaxLength(32)]
    public string Value { get; set; } = null!;
}

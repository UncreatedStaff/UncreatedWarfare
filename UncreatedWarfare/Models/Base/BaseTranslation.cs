using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Uncreated.Warfare.Models.Base;
public abstract class BaseTranslation
{
    [Required]
    public Localization.LanguageInfo Language { get; set; } = null!;

    [Required]
    [ForeignKey(nameof(Language))]
    [Column("Language")]
    public uint LanguageId { get; set; }

    [Required]
    [StringLength(32)]
    public string Value { get; set; } = null!;
}

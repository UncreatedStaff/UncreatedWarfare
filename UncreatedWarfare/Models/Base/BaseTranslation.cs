using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Uncreated.Warfare.Models.Base;
public abstract class BaseTranslation : ICloneable
{
    [Required]
    [Column("Language")]
    public uint LanguageId { get; set; }

    [Required]
    [StringLength(32)]
    public string Value { get; set; } = null!;

    protected BaseTranslation() { }
    protected BaseTranslation(BaseTranslation other)
    {
        LanguageId = other.LanguageId;
        Value = other.Value;
    }

    /// <inheritdoc />
    public abstract object Clone();
}

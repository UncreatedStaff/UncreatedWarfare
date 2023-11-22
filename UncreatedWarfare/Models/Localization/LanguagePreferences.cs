using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Uncreated.Warfare.Models.Localization;

[Table("lang_preferences")]
public class LanguagePreferences
{
    [Required]
    public LanguageInfo Language { get; set; } = null!;

    [Required]
    [Column("Language")]
    [ForeignKey(nameof(Language))]
    public uint LanguageId { get; set; }

    [Key]
    public ulong Steam64 { get; set; }

    [MaxLength(16)]
    public string? Culture { get; set; }

    [Column("UseCultureForCmdInput")]
    public bool UseCultureForCommandInput { get; set; }

    [Required]
    public DateTimeOffset LastUpdated { get; set; }
}
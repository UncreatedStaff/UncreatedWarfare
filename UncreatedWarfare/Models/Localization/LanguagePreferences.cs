using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Uncreated.Warfare.Models.Localization;

[Table("lang_preferences")]
public class LanguagePreferences
{
    [Key]
    public ulong Steam64 { get; set; }
    public LanguageInfo? Language { get; set; }

    [MaxLength(16)]
    public string? Culture { get; set; }

    [Column("UseCultureForCmdInput")]
    public bool UseCultureForCommandInput { get; set; }

    [Required]
    public DateTime LastUpdated { get; set; }
}

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Models.Users;

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

    [Required]
    [ForeignKey(nameof(PlayerData))]
    [Column("Steam64")]
    [Key]
    public ulong Steam64 { get; set; }

    [Required]
    public WarfareUserData PlayerData { get; set; }

    [MaxLength(16)]
    public string? Culture { get; set; }

    /// <summary>
    /// Corresponds to <see cref="TimeZoneInfo.Id"/>.
    /// </summary>
    [MaxLength(32)]
    public string? TimeZone { get; set; }

    [Column("UseCultureForCmdInput")]
    public bool UseCultureForCommandInput { get; set; }

    [Required]
    public DateTimeOffset LastUpdated { get; set; }
}
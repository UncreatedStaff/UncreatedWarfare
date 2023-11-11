using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Uncreated.Warfare.Models.Factions;

[Table("faction_translations")]
public class FactionLocalization
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("pk")]
    public uint Id { get; set; }

    [Required]
    public Faction Faction { get; set; } = null!;

    [Required]
    [ForeignKey(nameof(LanguageId))]
    public Localization.LanguageInfo Language { get; set; } = null!;

    [Required]
    public uint LanguageId { get; set; }

    [MaxLength(32)]
    public string? Name { get; set; } = null!;

    [MaxLength(24)]
    public string? ShortName { get; set; } = null!;

    [MaxLength(8)]
    public string? Abbreviation { get; set; } = null!;
}
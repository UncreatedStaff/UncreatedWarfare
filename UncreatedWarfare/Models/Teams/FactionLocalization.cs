using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Models.Localization;

namespace Uncreated.Warfare.Models.Teams;

[Table("faction_translations")]
public class FactionLocalization
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("pk")]
    public int Id { get; set; }

    [Required]
    public Faction Faction { get; set; } = null!;

    [Required]
    public LanguageInfo Language { get; set; } = null!;

    [MaxLength(32)]
    public string? Name { get; set; } = null!;

    [MaxLength(24)]
    public string? ShortName { get; set; } = null!;

    [MaxLength(8)]
    public string? Abbreviation { get; set; } = null!;
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Models.Assets;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Models.Factions;

[Table("faction_assets")]
public class FactionAsset
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("pk")]
    public uint Id { get; set; }

    [Required]
    public Faction Faction { get; set; } = null!;

    [Required]
    [Column("Faction")]
    [ForeignKey(nameof(Faction))]
    public uint FactionId { get; set; }

    public RedirectType Redirect { get; set; }

    public UnturnedAssetReference Asset { get; set; }

    [MaxLength(32)]
    public string? VariantKey { get; set; }
}
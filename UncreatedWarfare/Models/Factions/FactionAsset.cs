using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Database.Automation;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Models.Assets;

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

    [ExcludedEnum(RedirectType.None)]
    [ExcludedEnum(RedirectType.StandardAmmoIcon)]
    [ExcludedEnum(RedirectType.StandardGrenadeIcon)]
    [ExcludedEnum(RedirectType.StandardMeleeIcon)]
    [ExcludedEnum(RedirectType.StandardSmokeGrenadeIcon)]
    [ExcludedEnum(RedirectType.VehicleBay)]
    public RedirectType Redirect { get; set; }

    public UnturnedAssetReference Asset { get; set; }

    [MaxLength(32)]
    public string? VariantKey { get; set; }
}
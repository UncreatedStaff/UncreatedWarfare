using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SDG.Unturned;
using Uncreated.Warfare.API.Items;

namespace Uncreated.Warfare.Models.Teams;

[Table("faction_assets")]
public class FactionAsset
{
    [Column("Faction")]
    public int FactionKey { get; set; }

    [ForeignKey(nameof(Faction))]
    public Faction Faction { get; set; }

    public ItemRedirect Redirect { get; set; }
    
    public AssetReference<ItemAsset> Asset { get; set; }

    [MaxLength(32)]
    public string? VariantKey { get; set; }
}

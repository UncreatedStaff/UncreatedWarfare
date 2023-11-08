using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.API.Items;
using Uncreated.Warfare.Models.Assets;

namespace Uncreated.Warfare.Models.Teams;

[Table("faction_assets")]
public class FactionAsset
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("pk")]
    public int Id { get; set; }

    [Required]
    public Faction Faction { get; set; } = null!;

    public ItemRedirect Redirect { get; set; }
    
    public UnturnedAssetReference Asset { get; set; }

    [MaxLength(32)]
    public string? VariantKey { get; set; }
}

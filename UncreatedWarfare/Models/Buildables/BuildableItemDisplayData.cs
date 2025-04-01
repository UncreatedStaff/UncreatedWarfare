using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Models.Assets;

namespace Uncreated.Warfare.Models.Buildables;

[Table("buildables_display_data")]
public class BuildableItemDisplayData
{
    [Key]
    [Column("pk")]
    [ForeignKey(nameof(Save))]
    public int SaveId { get; set; }
    public BuildableSave? Save { get; set; }
    public UnturnedAssetReference Skin { get; set; }
    public UnturnedAssetReference Mythic { get; set; }
    public byte Rotation { get; set; }

    [StringLength(255)]
    public string? Tags { get; set; }

    [StringLength(255)]
    public string? DynamicProps { get; set; }
}

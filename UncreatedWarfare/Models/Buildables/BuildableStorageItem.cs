using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Models.Assets;

namespace Uncreated.Warfare.Models.Buildables;

[Table("buildables_stored_items")]
public class BuildableStorageItem
{
    [Column("Save")]
    [ForeignKey(nameof(Save))]
    public int SaveId { get; set; }

    public BuildableSave? Save { get; set; }
    public UnturnedAssetReference Item { get; set; }
    public byte Amount { get; set; }
    public byte Quality { get; set; }
    public byte PositionX { get; set; }
    public byte PositionY { get; set; }
    public byte Rotation { get; set; }

    [MaxLength(255)]
    [Column(TypeName = "varbinary(255)")]
    public byte[] State { get; set; }
}

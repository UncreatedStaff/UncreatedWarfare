using System.ComponentModel.DataAnnotations.Schema;

namespace Uncreated.Warfare.Models.Buildables;

[Table("buildables_instance_ids")]
public class BuildableInstanceId
{
    [Column("pk")]
    [ForeignKey(nameof(Save))]
    public int SaveId { get; set; }
    public BuildableSave? Save { get; set; }
    public byte RegionId { get; set; }
    public uint InstanceId { get; set; }
}

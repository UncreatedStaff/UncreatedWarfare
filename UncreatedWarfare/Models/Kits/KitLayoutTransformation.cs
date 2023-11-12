using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Kits.Items;

namespace Uncreated.Warfare.Models.Kits;

[Table("kits_layouts")]
public class KitLayoutTransformation
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("pk")]
    public uint Id { get; set; }
    public ulong Steam64 { get; set; }
    public Kit Kit { get; set; }
    [ForeignKey(nameof(Kit))]
    public uint KitId { get; set; }
    public Page OldPage { get; set; }
    public byte OldX { get; set; }
    public byte OldY { get; set; }
    public Page NewPage { get; set; }
    public byte NewX { get; set; }
    public byte NewY { get; set; }
    public byte NewRotation { get; set; }
}

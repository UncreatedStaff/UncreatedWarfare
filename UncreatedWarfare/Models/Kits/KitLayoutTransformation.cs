using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Models.Users;

namespace Uncreated.Warfare.Models.Kits;

[Table("kits_layouts")]
public class KitLayoutTransformation
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("pk")]
    public uint Id { get; set; }

    [Required]
    [ForeignKey(nameof(PlayerData))]
    [Column("Steam64")]
    public ulong Steam64 { get; set; }

    [Required]
    public WarfareUserData PlayerData { get; set; }

    [Required]
    public Kit Kit { get; set; }

    [ForeignKey(nameof(Kit))]
    [Required]
    [Column("Kit")]
    public uint KitId { get; set; }
    public Page OldPage { get; set; }
    public byte OldX { get; set; }
    public byte OldY { get; set; }
    public Page NewPage { get; set; }
    public byte NewX { get; set; }
    public byte NewY { get; set; }
    public byte NewRotation { get; set; }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Uncreated.Warfare.Models.Kits;

[Table("kits_favorites")]
public class KitFavorite
{

    [Required]
    public Kit Kit { get; set; }

    [Required]
    [ForeignKey(nameof(Kit))]
    [Column("Kit")]
    public uint KitId { get; set; }
    public ulong Steam64 { get; set; }
}

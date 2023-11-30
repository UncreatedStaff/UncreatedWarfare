using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Uncreated.Warfare.Models.Seasons;

[Table("maps_dependencies")]
public class MapWorkshopDependency
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public ulong WorkshopId { get; set; }

    [Required]
    [ForeignKey(nameof(Map))]
    [Column("Map")]
    public int MapId { get; set; }

    [Required]
    public MapData Map { get; set; }

    [Required]
    public bool IsRemoved { get; set; }
}

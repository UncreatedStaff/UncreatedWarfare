using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Uncreated.Warfare.Models.Seasons;

[Table("maps")]
public class MapData
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string DisplayName { get; set; }
    public ulong? WorkshopId { get; set; }

    [ForeignKey(nameof(SeasonReleased))]
    [Column("SeasonReleased")]
    [Required]
    public int ReleasedSeasonId { get; set; }

    [Required]
    public SeasonData SeasonReleased { get; set; }

    public ICollection<MapWorkshopDependency> Dependencies { get; set; }
}
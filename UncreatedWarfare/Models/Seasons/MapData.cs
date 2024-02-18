using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Models.Factions;

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

    [ForeignKey(nameof(Team1Faction))]
    [Column("Team1Faction")]
    [Required]
    public uint Team1FactionId { get; set; }

    [ForeignKey(nameof(Team2Faction))]
    [Column("Team2Faction")]
    [Required]
    public uint Team2FactionId { get; set; }

    [Required]
    public Faction Team1Faction { get; set; }

    [Required]
    public Faction Team2Faction { get; set; }

    [Required]
    public SeasonData SeasonReleased { get; set; }

    public ICollection<MapWorkshopDependency> Dependencies { get; set; }
}
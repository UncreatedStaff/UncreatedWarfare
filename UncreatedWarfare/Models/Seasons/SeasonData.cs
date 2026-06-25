using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Uncreated.Warfare.Models.Seasons;

#nullable disable

[Table("seasons")]
public class SeasonData
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("ReleaseTimestampUTC")]
    public DateTimeOffset? ReleaseTimestamp { get; set; }

    public IList<MapData> Maps { get; set; }
}

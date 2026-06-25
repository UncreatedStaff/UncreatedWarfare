using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Uncreated.Warfare.Models.Stats;

#nullable disable

[Table("stats_fob_items_builders")]
public class FobItemBuilderRecord : BasePlayerRecord
{
    [Required]
    [ForeignKey(nameof(FobItem))]
    [Column("FobItem")]
    public ulong FobItemId { get; set; }

    [Required]
    public FobItemRecord FobItem { get; set; }

    public float Hits { get; set; }
    public double Responsibility { get; set; }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Models.Stats.Base;

namespace Uncreated.Warfare.Models.Stats.Records;

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
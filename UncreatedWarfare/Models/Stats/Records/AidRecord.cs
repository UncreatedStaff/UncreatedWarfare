using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Models.Assets;

namespace Uncreated.Warfare.Models.Stats;

[Table("stats_aid_records")]
public class AidRecord : InstigatedPlayerRecord
{
    public UnturnedAssetReference Item { get; set; }

    [DefaultValue("00000000000000000000000000000000"), Required]
    [StringLength(48)]
    public string ItemName { get; set; }

    public float Health { get; set; }
    public bool IsRevive { get; set; }
}

using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Database.Automation;
using Uncreated.Warfare.Models.Assets;
using Uncreated.Warfare.Models.Stats.Base;

namespace Uncreated.Warfare.Models.Stats.Records;

[Table("stats_aid_records")]
public class AidRecord : InstigatedPlayerRecord
{
    [AddName]
    public UnturnedAssetReference Item { get; set; }

    public float Health { get; set; }
    public bool IsRevive { get; set; }
}

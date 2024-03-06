using SDG.Unturned;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Models.Assets;
using Uncreated.Warfare.Models.Stats.Base;

namespace Uncreated.Warfare.Models.Stats.Records;

[Table("stats_deaths")]
public class DeathRecord : RelatedPlayerRecord
{
    [MaxLength(256)]
    public string DeathMessage { get; set; }

    [DefaultValue(nameof(EDeathCause.KILL))]
    public EDeathCause DeathCause { get; set; }

    [DefaultValue(0f)]
    public float TimeDeployedSeconds { get; set; }

    [DefaultValue(null)]
    public float? Distance { get; set; }

    [DefaultValue(false)]
    public bool IsTeamkill { get; set; }

    [DefaultValue(false)]
    public bool IsSuicide { get; set; }

    [DefaultValue(null)]
    public UnturnedAssetReference? PrimaryAsset { get; set; }

    [DefaultValue(null)]
    public UnturnedAssetReference? SecondaryAsset { get; set; }

    [DefaultValue(null)]
    public UnturnedAssetReference? Vehicle { get; set; }
}
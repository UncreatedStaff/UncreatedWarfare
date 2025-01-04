using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Database.Automation;
using Uncreated.Warfare.Models.Assets;

namespace Uncreated.Warfare.Models.Stats;

[Table("stats_damage")]
public class DamageRecord : RelatedPlayerRecord
{
    [DefaultValue(null)]
    [AddName]
    public UnturnedAssetReference? PrimaryAsset { get; set; }

    [DefaultValue(null)]
    [AddName]
    public UnturnedAssetReference? SecondaryAsset { get; set; }

    [DefaultValue(null)]
    [AddName]
    public UnturnedAssetReference? Vehicle { get; set; }

    public EDeathCause Cause { get; set; }

    [DefaultValue(null)]
    public float? Distance { get; set; }
    public float Damage { get; set; }

    [DefaultValue(0f)]
    public float TimeDeployedSeconds { get; set; }

    [DefaultValue(false)]
    public bool IsTeamkill { get; set; }

    [DefaultValue(false)]
    public bool IsSuicide { get; set; }

    [DefaultValue(false)]
    public bool IsInjure { get; set; }

    [DefaultValue(false)]
    public bool IsInjured { get; set; }

    [DefaultValue(nameof(ELimb.SKULL))]
    public ELimb Limb { get; set; }
}

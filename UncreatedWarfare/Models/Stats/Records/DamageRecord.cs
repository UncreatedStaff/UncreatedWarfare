using SDG.Unturned;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Database.Automation;
using Uncreated.Warfare.Models.Assets;
using Uncreated.Warfare.Models.Stats.Base;

namespace Uncreated.Warfare.Models.Stats.Records;

[Table("stats_damage")]
public class DamageRecord : RelatedPlayerRecord
{
    [DefaultValue(null)]
    [AddName]
    public UnturnedAssetReference? PrimaryAsset { get; set; }

    [DefaultValue(null)]
    [AddName]
    public UnturnedAssetReference? SecondaryAsset { get; set; }

    [DefaultValue(nameof(EDamageOrigin.Unknown))]
    public EDamageOrigin Origin { get; set; }

    [DefaultValue(null)]
    public float? Distance { get; set; }
    public float Damage { get; set; }

    [DefaultValue(false)]
    public bool IsTeamkill { get; set; }

    [DefaultValue(false)]
    public bool IsSuicide { get; set; }

    [DefaultValue(false)]
    public bool IsInjure { get; set; }

    [DefaultValue(nameof(ELimb.SKULL))]
    public ELimb Limb { get; set; }
}

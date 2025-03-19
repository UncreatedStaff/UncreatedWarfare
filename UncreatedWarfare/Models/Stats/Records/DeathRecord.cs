using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Database.Automation;
using Uncreated.Warfare.Deaths;
using Uncreated.Warfare.Models.Assets;

namespace Uncreated.Warfare.Models.Stats;

[Table("stats_deaths")]
public class DeathRecord : RelatedPlayerRecord
{
    [MaxLength(256)]
    public string DeathMessage { get; set; }

    [DefaultValue(nameof(EDeathCause.KILL))]
    [IncludedEnum(DeathTracker.InEnemyMainDeathCause)]
    public EDeathCause DeathCause { get; set; }

    [DefaultValue(0f)]
    public float TimeDeployedSeconds { get; set; }

    [DefaultValue(null)]
    public float? Distance { get; set; }

    [DefaultValue(false)]
    public bool IsTeamkill { get; set; }

    [DefaultValue(false)]
    public bool IsSuicide { get; set; }

    [DefaultValue(false)]
    public bool IsBleedout { get; set; }

    [ForeignKey(nameof(KillShot))]
    [Column("KillShot")]
    public ulong? KillShotId { get; set; }

    public DamageRecord? KillShot { get; set; }

    [DefaultValue(null)]
    public UnturnedAssetReference? PrimaryAsset { get; set; }

    [DefaultValue("00000000000000000000000000000000")]
    [StringLength(48)]
    public string? PrimaryAssetName { get; set; }

    [DefaultValue(null)]
    public UnturnedAssetReference? SecondaryAsset { get; set; }

    [DefaultValue("00000000000000000000000000000000")]
    [StringLength(48)]
    public string? SecondaryAssetName { get; set; }

    [DefaultValue(null)]
    public UnturnedAssetReference? Vehicle { get; set; }

    [DefaultValue("00000000000000000000000000000000")]
    [StringLength(48)]
    public string? VehicleName { get; set; }
}
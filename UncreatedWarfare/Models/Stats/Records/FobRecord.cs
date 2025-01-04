using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Database.Automation;
using Uncreated.Warfare.Models.Assets;

namespace Uncreated.Warfare.Models.Stats;

[Table("stats_fobs")]
public class FobRecord : InstigatedPlayerRecord
{
    private Vector3 _fobAngle;

    public FobType FobType { get; set; }

    [DefaultValue(null)]
    [AddName]
    public UnturnedAssetReference? PrimaryAsset { get; set; }

    [DefaultValue(null)]
    [AddName]
    public UnturnedAssetReference? SecondaryAsset { get; set; }

    [Required]
    [StringLength(32)]
    public string FobName { get; set; }

    /// <summary>
    /// Teleporting to FOB.
    /// </summary>
    [DefaultValue(0)]
    public int DeploymentCount { get; set; }

    /// <summary>
    /// Teleporting from FOB.
    /// </summary>
    [DefaultValue(0)]
    public int TeleportCount { get; set; }

    [DefaultValue(0)]
    public int FortificationsBuilt { get; set; }

    [DefaultValue(0)]
    public int FortificationsDestroyed { get; set; }

    [DefaultValue(0)]
    public int EmplacementsBuilt { get; set; }

    [DefaultValue(0)]
    public int EmplacementsDestroyed { get; set; }

    [DefaultValue(0)]
    public int BunkersBuilt { get; set; }

    [DefaultValue(0)]
    public int BunkersDestroyed { get; set; }

    [DefaultValue(0)]
    public int AmmoCratesBuilt { get; set; }

    [DefaultValue(0)]
    public int AmmoCratesDestroyed { get; set; }

    [DefaultValue(0)]
    public int RepairStationsBuilt { get; set; }

    [DefaultValue(0)]
    public int RepairStationsDestroyed { get; set; }

    [DefaultValue(0)]
    public int EmplacementPlayerKills { get; set; }

    [DefaultValue(0)]
    public int EmplacementVehicleKills { get; set; }

    [DefaultValue(0)]
    public int AmmoSpent { get; set; }

    [DefaultValue(0)]
    public int BuildSpent { get; set; }

    [DefaultValue(0)]
    public int AmmoLoaded { get; set; }

    [DefaultValue(0)]
    public int BuildLoaded { get; set; }

    [DefaultValue(false)]
    public bool DestroyedByRoundEnd { get; set; }

    [DefaultValue(false)]
    public bool Teamkilled { get; set; }

    [DefaultValue(null)]
    [Column("DestroyedAtUTC")]
    public DateTimeOffset? DestroyedAt { get; set; }

    [NotMapped]
    public Vector3 FobAngle
    {
        get => _fobAngle;
        set => _fobAngle = value;
    }

    public float FobAngleX
    {
        get => _fobAngle.x;
        set => _fobAngle.x = value;
    }
    public float FobAngleY
    {
        get => _fobAngle.y;
        set => _fobAngle.y = value;
    }
    public float FobAngleZ
    {
        get => _fobAngle.z;
        set => _fobAngle.z = value;
    }
    public IList<FobItemRecord> Items { get; set; }
}
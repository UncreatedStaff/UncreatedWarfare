using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using Uncreated.Warfare.Database.Automation;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.Models.Assets;
using Uncreated.Warfare.Models.Stats.Base;

namespace Uncreated.Warfare.Models.Stats.Records;

[Table("stats_fob_items")]
public class FobItemRecord : InstigatedPlayerRecord
{
    private Vector3 _fobItemPosition;
    private Vector3 _fobItemAngle;

    [ForeignKey(nameof(Fob))]
    [Column("Fob")]
    public ulong? FobId { get; set; }

    public FobRecord Fob { get; set; }

    [ExcludedEnum(BuildableType.Radio)]
    public BuildableType Type { get; set; }

    [DefaultValue(0)]
    public int PlayerKills { get; set; }

    [DefaultValue(0)]
    public int VehicleKills { get; set; }

    [DefaultValue(0)]
    public double UseTimeSeconds { get; set; }

    [Column("BuiltAtUTC")]
    public DateTimeOffset? BuiltAt { get; set; }

    [Column("DestroyedAtUTC")]
    public DateTimeOffset? DestroyedAt { get; set; }

    [DefaultValue(null)]
    [AddName]
    public UnturnedAssetReference? Item { get; set; }

    [DefaultValue(null)]
    [AddName]
    public UnturnedAssetReference? PrimaryAsset { get; set; }

    [DefaultValue(null)]
    [AddName]
    public UnturnedAssetReference? SecondaryAsset { get; set; }

    [DefaultValue(false)]
    public bool DestroyedByRoundEnd { get; set; }

    [DefaultValue(false)]
    public bool Teamkilled { get; set; }

    [NotMapped]
    public Vector3 FobItemPosition
    {
        get => _fobItemPosition;
        set => _fobItemPosition = value;
    }

    public float FobItemPositionX
    {
        get => _fobItemPosition.x;
        set => _fobItemPosition.x = value;
    }
    public float FobItemPositionY
    {
        get => _fobItemPosition.y;
        set => _fobItemPosition.y = value;
    }
    public float FobItemPositionZ
    {
        get => _fobItemPosition.z;
        set => _fobItemPosition.z = value;
    }

    [NotMapped]
    public Vector3 FobItemAngle
    {
        get => _fobItemAngle;
        set => _fobItemAngle = value;
    }

    public float FobItemAngleX
    {
        get => _fobItemAngle.x;
        set => _fobItemAngle.x = value;
    }
    public float FobItemAngleY
    {
        get => _fobItemAngle.y;
        set => _fobItemAngle.y = value;
    }
    public float FobItemAngleZ
    {
        get => _fobItemAngle.z;
        set => _fobItemAngle.z = value;
    }

    public IList<FobItemBuilderRecord> Builders { get; set; }
}
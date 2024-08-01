using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Models.Assets;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Models.Seasons;

namespace Uncreated.Warfare.Models.Buildables;

[Table("buildables")]
public class BuildableSave : ITranslationArgument
{
    private Vector3 _position;
    private Vector3 _rotation;

    [Key]
    [Column("pk")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("Map")]
    [ForeignKey(nameof(Map))]
    public int? MapId { get; set; }
    public MapData? Map { get; set; }
    public UnturnedAssetReference Item { get; set; }
    public bool IsStructure { get; set; }

    [NotMapped]
    public Vector3 Position
    {
        get => _position;
        set => _position = value;
    }

    [NotMapped]
    public Vector3 Rotation
    {
        get => _rotation;
        set => _rotation = value;
    }

    public float PositionX
    {
        get => _position.x;
        set => _position.x = value;
    }
    public float PositionY
    {
        get => _position.y;
        set => _position.y = value;
    }
    public float PositionZ
    {
        get => _position.z;
        set => _position.z = value;
    }

    public byte RotationX
    {
        get => MeasurementTool.angleToByte(_rotation.x);
        set => _rotation.x = MeasurementTool.byteToAngle(value);
    }
    public byte RotationY
    {
        get => MeasurementTool.angleToByte(_rotation.y);
        set => _rotation.y = MeasurementTool.byteToAngle(value);
    }
    public byte RotationZ
    {
        get => MeasurementTool.angleToByte(_rotation.z);
        set => _rotation.z = MeasurementTool.byteToAngle(value);
    }

    public ulong Owner { get; set; }
    public ulong Group { get; set; }

    [MaxLength(255)]
    [Column(TypeName = "varbinary(255)")]
    public byte[] State { get; set; }

    [NotMapped]
    public uint InstanceId { get; set; }

    [NotMapped]
    public IBuildable Buildable { get; set; }

    public BuildableItemDisplayData? DisplayData { get; set; }
    public IList<BuildableStorageItem>? Items { get; set; }
    public IList<BuildableInstanceId>? InstanceIds { get; set; }

    string ITranslationArgument.Translate(LanguageInfo language, string? format, UCPlayer? target, CultureInfo? culture,
        ref TranslationFlags flags)
    {
        return Item.GetAsset<ItemAsset>()?.itemName ?? Item.ToString();
    }
}

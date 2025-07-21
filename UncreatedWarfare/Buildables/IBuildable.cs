using DanielWillett.ReflectionTools;
using System;
using System.Runtime.CompilerServices;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Buildables;

/// <summary>
/// Any structure or barricade. Can be <see cref="BuildableBarricade"/> or <see cref="BuildableStructure"/>.
/// </summary>
[CannotApplyEqualityOperator]
public interface IBuildable :
    IEquatable<IBuildable>,
    IEquatable<BuildableBarricade>,
    IEquatable<BarricadeDrop>,
    IEquatable<BarricadeData>,
    IEquatable<BuildableStructure>,
    IEquatable<StructureDrop>,
    IEquatable<StructureData>,
    ITransformObject
{
    /// <summary>
    /// The instance ID of this buildable specific to it's buildable type.
    /// </summary>
    uint InstanceId { get; }

    /// <summary>
    /// If the buildable is a structure instead of a barricade.
    /// </summary>
    bool IsStructure { get; }

    /// <summary>
    /// If the buildable is planted on a vehicle.
    /// </summary>
    bool IsOnVehicle { get; }

    /// <summary>
    /// The health of this buildable.
    /// </summary>
    ushort Health { get; }

    /// <summary>
    /// The maximum amount of health this buildable could have.
    /// </summary>
    ushort MaxHealth { get; }

    /// <summary>
    /// The vehicle this barricade is attached to, if any.
    /// </summary>
    InteractableVehicle? VehicleParent { get; }

    /// <summary>
    /// If the buildable has been destroyed by its health dropping to 0.
    /// </summary>
    /// <remarks>This is not the same as not spawned.</remarks>
    bool IsDead { get; }

    /// <summary>
    /// The asset for the buildable.
    /// </summary>
    ItemPlaceableAsset Asset { get; }

    /// <summary>
    /// The transform of the buildable.
    /// </summary>
    Transform Model { get; }

    /// <summary>
    /// The player that placed the buildable.
    /// </summary>
    CSteamID Owner { get; }

    /// <summary>
    /// The group of the player that placed the buildable.
    /// </summary>
    CSteamID Group { get; }

    /// <summary>
    /// The <see cref="BarricadeDrop"/> or <see cref="StructureDrop"/> of the buildable.
    /// </summary>
    object Drop { get; }

    /// <summary>
    /// The <see cref="BarricadeData"/> or <see cref="StructureData"/> of the buildable.
    /// </summary>
    object Data { get; }

    /// <summary>
    /// The <see cref="Barricade"/> or <see cref="Structure"/> of the buildable.
    /// </summary>
    object Item { get; }

    /// <summary>
    /// The ID used to refer to networked objects.
    /// </summary>
    NetId NetId { get; }

    /// <summary>
    /// Get <see cref="Drop"/> as either a <see cref="BarricadeDrop"/> or <see cref="StructureDrop"/>.
    /// </summary>
    /// <exception cref="InvalidCastException"/>
    TDrop GetDrop<TDrop>() where TDrop : class
    {
        return Drop as TDrop ?? throw new InvalidCastException($"This buildable's drop is not a {Accessor.ExceptionFormatter.Format<TDrop>()}.");
    }

    /// <summary>
    /// Get <see cref="Data"/> as either a <see cref="BarricadeData"/> or <see cref="StructureData"/>.
    /// </summary>
    /// <exception cref="InvalidCastException"/>
    TData GetData<TData>() where TData : class
    {
        return Data as TData ?? throw new InvalidCastException($"This buildable's data is not a {Accessor.ExceptionFormatter.Format<TData>()}.");
    }

    /// <summary>
    /// Get <see cref="Item"/> as either a <see cref="Barricade"/> or <see cref="Structure"/>.
    /// </summary>
    /// <exception cref="InvalidCastException"/>
    TData GetItem<TData>() where TData : class
    {
        return Item as TData ?? throw new InvalidCastException($"This buildable's item is not a {Accessor.ExceptionFormatter.Format<TData>()}.");
    }
}

[CannotApplyEqualityOperator]
public class BuildableBarricade : IBuildable
{
    public uint InstanceId => Drop.instanceID;
    public bool IsStructure => false;
    public bool IsOnVehicle { get; }
    public InteractableVehicle? VehicleParent { get; }
    public bool IsDead => Data.barricade.isDead;
    public ushort Health => Data.barricade.health;
    public ushort MaxHealth => Drop.asset.health;
    public ItemPlaceableAsset Asset => Drop.asset;
    public Transform Model => Drop.model == null || Drop.GetNetId().id == 0 ? null! : Drop.model; // so you can use ? on it
    public CSteamID Owner => Unsafe.As<ulong, CSteamID>(ref Data.owner);
    public CSteamID Group => Unsafe.As<ulong, CSteamID>(ref Data.group);
    public NetId NetId => Drop.GetNetId();
    public Vector3 Position
    {
        get => Data.point;
        set
        {
            GameThread.AssertCurrent();

            BarricadeManager.ServerSetBarricadeTransform(Drop.model, value, Data.rotation);
        }
    }

    public Quaternion Rotation
    {
        get => Data.rotation;
        set
        {
            GameThread.AssertCurrent();

            BarricadeManager.ServerSetBarricadeTransform(Drop.model, Data.point, value);
        }
    }


    public BarricadeDrop Drop { get; }
    public BarricadeData Data { get; }
    public BuildableBarricade(BarricadeDrop drop)
    {
        Drop = drop;
        Data = drop.GetServersideData();
        Transform parent = drop.model.parent;
        if (!BarricadeManager.tryGetPlant(parent, out _, out _, out _, out BarricadeRegion region) || region is not VehicleBarricadeRegion vehRegion)
        {
            return;
        }

        IsOnVehicle = true;
        VehicleParent = vehRegion.vehicle;
    }

    public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
    {
        GameThread.AssertCurrent();

        BarricadeManager.ServerSetBarricadeTransform(Drop.model, position, rotation);
    }

    public bool Alive => Drop.GetNetId().id != 0;
    object IBuildable.Drop => Drop;
    object IBuildable.Data => Data;
    object IBuildable.Item => Data.barricade;
    Vector3 ITransformObject.Scale
    {
        get => Vector3.one;
        set => throw new NotSupportedException();
    }

    public bool Equals(BarricadeData? other) => other is not null && other.instanceID == Drop.instanceID;
    public bool Equals(BarricadeDrop? other) => other is not null && other.instanceID == Drop.instanceID;
    public bool Equals(BuildableBarricade? other) => other is not null && other.Drop.instanceID == Drop.instanceID;
    public bool Equals(IBuildable? other) => other is not null && !other.IsStructure && other.InstanceId == Drop.instanceID;
    bool IEquatable<StructureData>.Equals(StructureData? other) => false;
    bool IEquatable<StructureDrop>.Equals(StructureDrop? other) => false;
    bool IEquatable<BuildableStructure>.Equals(BuildableStructure? other) => false;
    public override bool Equals(object? obj) => obj is IBuildable b && Equals(b);
    public override int GetHashCode() => unchecked ( (int)Drop.instanceID );
    public override string ToString() =>  $"BuildableBarricade[InstanceId: {InstanceId} Asset itemName: {Asset.itemName} Asset GUID: {Asset.GUID:N} Owner: {Owner} Group: {Group} Alive: {Alive}]";
}

[CannotApplyEqualityOperator]
public class BuildableStructure : IBuildable
{
    public uint InstanceId => Drop.instanceID;
    public bool IsStructure => true;
    public bool IsDead => Data.structure.isDead;
    public ItemPlaceableAsset Asset => Drop.asset;
    public ushort Health => Data.structure.health;
    public ushort MaxHealth => Drop.asset.health;
    public Transform Model => Drop.model == null || Drop.GetNetId().id == 0 ? null! : Drop.model; // so you can use ? on it
    public CSteamID Owner => Unsafe.As<ulong, CSteamID>(ref Data.owner);
    public CSteamID Group => Unsafe.As<ulong, CSteamID>(ref Data.group);
    public NetId NetId => Drop.GetNetId();
    public Vector3 Position
    {
        get => Data.point;
        set
        {
            GameThread.AssertCurrent();

            StructureManager.ServerSetStructureTransform(Drop.model, value, Data.rotation);
        }
    }

    public Quaternion Rotation
    {
        get => Data.rotation;
        set
        {
            GameThread.AssertCurrent();

            StructureManager.ServerSetStructureTransform(Drop.model, Data.point, value);
        }
    }
    public StructureDrop Drop { get; }
    public StructureData Data { get; }
    public Structure Structure => Data.structure;
    public BuildableStructure(StructureDrop drop)
    {
        Drop = drop;
        Data = drop.GetServersideData();
    }

    public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
    {
        GameThread.AssertCurrent();

        StructureManager.ServerSetStructureTransform(Drop.model, position, rotation);
    }

    public bool Alive => Drop.GetNetId().id != 0;
    object IBuildable.Drop => Drop;
    object IBuildable.Data => Data;
    object IBuildable.Item => Data.structure;
    bool IBuildable.IsOnVehicle => false;
    InteractableVehicle? IBuildable.VehicleParent => null;
    Vector3 ITransformObject.Scale
    {
        get => Vector3.one;
        set => throw new NotSupportedException();
    }

    public bool Equals(StructureData? other) => other is not null && other.instanceID == Drop.instanceID;
    public bool Equals(StructureDrop? other) => other is not null && other.instanceID == Drop.instanceID;
    public bool Equals(BuildableStructure? other) => other is not null && other.Drop.instanceID == Drop.instanceID;
    bool IEquatable<BarricadeData>.Equals(BarricadeData? other) => false;
    bool IEquatable<BarricadeDrop>.Equals(BarricadeDrop? other) => false;
    bool IEquatable<BuildableBarricade>.Equals(BuildableBarricade? other) => false;
    public bool Equals(IBuildable? other) => other is not null && other.IsStructure && other.InstanceId == Drop.instanceID;
    public override bool Equals(object? obj) => obj is IBuildable b && Equals(b);
    public override int GetHashCode() => unchecked ( (int)Drop.instanceID );
    public override string ToString() => $"BuildableStructure[InstanceId: {InstanceId} Asset itemName: {Asset.itemName} Asset GUID: {Asset.GUID:N} Owner: {Owner} Group: {Group} Alive: {Alive}]";
}
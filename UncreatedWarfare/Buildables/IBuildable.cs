using System;
using System.Runtime.CompilerServices;
using DanielWillett.ReflectionTools;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Buildables;

/// <summary>
/// Any structure or barricade. Can be <see cref="BuildableBarricade"/> or <see cref="BuildableStructure"/>.
/// </summary>
[CannotApplyEqualityOperator]
public interface IBuildable : IEquatable<IBuildable>, ITransformObject
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
    /// If the buildable has been destroyed.
    /// </summary>
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
public class BuildableBarricade : IBuildable, IEquatable<BuildableBarricade>, IEquatable<BarricadeDrop>
{
    public uint InstanceId => Drop.instanceID;
    public bool IsStructure => false;
    public bool IsDead => Data.barricade.isDead;
    public ItemPlaceableAsset Asset => Drop.asset;
    public Transform Model => Drop.model == null || Data.barricade.isDead ? null! : Drop.model; // so you can use ? on it
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
    }

    public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
    {
        GameThread.AssertCurrent();

        BarricadeManager.ServerSetBarricadeTransform(Drop.model, position, rotation);
    }

    object IBuildable.Drop => Drop;
    object IBuildable.Data => Data;
    object IBuildable.Item => Data.barricade;
    Vector3 ITransformObject.Scale
    {
        get => Vector3.one;
        set => throw new NotSupportedException();
    }

    public bool Equals(BarricadeDrop? other) => other is not null && other.instanceID == Drop.instanceID;
    public bool Equals(BuildableBarricade? other) => other is not null && other.Drop.instanceID == Drop.instanceID;
    public bool Equals(IBuildable? other) => other is not null && !other.IsStructure && other.InstanceId == Drop.instanceID;
    public override bool Equals(object? obj) => obj is IBuildable b && Equals(b);
    public override int GetHashCode() => unchecked ( (int)Drop.instanceID );
    public override string ToString() =>  $"BuildableBarricade[InstanceId: {InstanceId} Asset itemName: {Asset.itemName} Asset GUID: {Asset.GUID:N} Owner: {Owner} Group: {Group} IsDead: {IsDead}]";
}

[CannotApplyEqualityOperator]
public class BuildableStructure : IBuildable, IEquatable<BuildableStructure>, IEquatable<StructureDrop>
{
    public uint InstanceId => Drop.instanceID;
    public bool IsStructure => true;
    public bool IsDead => Data.structure.isDead;
    public ItemPlaceableAsset Asset => Drop.asset;
    public Transform Model => Drop.model == null || Data.structure.isDead ? null! : Drop.model; // so you can use ? on it
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

    object IBuildable.Drop => Drop;
    object IBuildable.Data => Data;
    object IBuildable.Item => Data.structure;
    Vector3 ITransformObject.Scale
    {
        get => Vector3.one;
        set => throw new NotSupportedException();
    }

    public bool Equals(StructureDrop? other) => other is not null && other.instanceID == Drop.instanceID;
    public bool Equals(BuildableStructure? other) => other is not null && other.Drop.instanceID == Drop.instanceID;
    public bool Equals(IBuildable? other) => other is not null && other.IsStructure && other.InstanceId == Drop.instanceID;
    public override bool Equals(object? obj) => obj is IBuildable b && Equals(b);
    public override int GetHashCode() => unchecked ( (int)Drop.instanceID );
    public override string ToString() => $"BuildableStructure[InstanceId: {InstanceId} Asset itemName: {Asset.itemName} Asset GUID: {Asset.GUID:N} Owner: {Owner} Group: {Group} IsDead: {IsDead}]";
}
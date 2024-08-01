using System;

namespace Uncreated.Warfare.Buildables;

[CannotApplyEqualityOperator]
public interface IBuildable : IEquatable<IBuildable>
{
    uint InstanceId { get; }
    bool IsStructure { get; }
    ItemPlaceableAsset Asset { get; }
    Transform Model { get; }
    ulong Owner { get; }
    ulong Group { get; }
    object Drop { get; }
    object Data { get; }
    NetId NetId { get; }
    Vector3 Position { get; }
    Quaternion Rotation { get; }
}

[CannotApplyEqualityOperator]
public class BuildableBarricade : IBuildable, IEquatable<BuildableBarricade>, IEquatable<BarricadeDrop>
{
    public uint InstanceId => Drop.instanceID;
    public bool IsStructure => false;
    public ItemPlaceableAsset Asset => Drop.asset;
    public Transform Model => Drop.model == null || Data.barricade.isDead ? null! : Drop.model; // so you can use ? on it
    public ulong Owner => Data.owner;
    public ulong Group => Data.group;
    public NetId NetId => Drop.GetNetId();
    public Vector3 Position => Data.point;
    public Quaternion Rotation => Data.rotation;
    public BarricadeDrop Drop { get; }
    public BarricadeData Data { get; }
    public BuildableBarricade(BarricadeDrop drop)
    {
        Drop = drop;
        Data = drop.GetServersideData();
    }

    object IBuildable.Drop => Drop;
    object IBuildable.Data => Data;

    public bool Equals(BarricadeDrop? other) => other is not null && other.instanceID == Drop.instanceID;
    public bool Equals(BuildableBarricade? other) => other is not null && other.Drop.instanceID == Drop.instanceID;
    public bool Equals(IBuildable? other) => other is not null && !other.IsStructure && other.InstanceId == Drop.instanceID;
    public override bool Equals(object? obj) => obj is IBuildable b && Equals(b);
    public override int GetHashCode() => unchecked ( (int)Drop.instanceID );
}

[CannotApplyEqualityOperator]
public class BuildableStructure : IBuildable, IEquatable<BuildableStructure>, IEquatable<StructureDrop>
{
    public uint InstanceId => Drop.instanceID;
    public bool IsStructure => true;
    public ItemPlaceableAsset Asset => Drop.asset;
    public Transform Model => Drop.model == null || Data.structure.isDead ? null! : Drop.model; // so you can use ? on it
    public ulong Owner => Data.owner;
    public ulong Group => Data.group;
    public NetId NetId => Drop.GetNetId();
    public Vector3 Position => Data.point;
    public Quaternion Rotation => Data.rotation;
    public StructureDrop Drop { get; }
    public StructureData Data { get; }
    public BuildableStructure(StructureDrop drop)
    {
        Drop = drop;
        Data = drop.GetServersideData();
    }

    object IBuildable.Drop => Drop;
    object IBuildable.Data => Data;

    public bool Equals(StructureDrop? other) => other is not null && other.instanceID == Drop.instanceID;
    public bool Equals(BuildableStructure? other) => other is not null && other.Drop.instanceID == Drop.instanceID;
    public bool Equals(IBuildable? other) => other is not null && other.IsStructure && other.InstanceId == Drop.instanceID;
    public override bool Equals(object? obj) => obj is IBuildable b && Equals(b);
    public override int GetHashCode() => unchecked ( (int)Drop.instanceID );
}
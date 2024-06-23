using SDG.Unturned;
using UnityEngine;

namespace Uncreated.Warfare.Buildables;

public interface IBuildable
{
    uint InstanceId { get; }
    bool IsStructure { get; }
    ItemAsset Asset { get; }
    Transform Model { get; }
    ulong Owner { get; }
    ulong Group { get; }
    object Drop { get; }
    object Data { get; }
    NetId NetId { get; }
}
public class BuildableBarricade : IBuildable
{
    public uint InstanceId => Drop.instanceID;
    public bool IsStructure => false;
    public ItemAsset Asset => Drop.asset;
    public Transform Model => Drop.model == null || Data.barricade.isDead ? null! : Drop.model; // so you can use ? on it
    public ulong Owner => Data.owner;
    public ulong Group => Data.group;
    public NetId NetId => Drop.GetNetId();
    public BarricadeDrop Drop { get; internal set; }
    public BarricadeData Data { get; internal set; }
    public BuildableBarricade(BarricadeDrop drop)
    {
        Drop = drop;
        Data = drop.GetServersideData();
    }

    object IBuildable.Drop => Drop;
    object IBuildable.Data => Data;
}
public class BuildableStructure : IBuildable
{
    public uint InstanceId => Drop.instanceID;
    public bool IsStructure => true;
    public ItemAsset Asset => Drop.asset;
    public Transform Model => Drop.model == null || Data.structure.isDead ? null! : Drop.model; // so you can use ? on it
    public ulong Owner => Data.owner;
    public ulong Group => Data.group;
    public NetId NetId => Drop.GetNetId();
    public StructureDrop Drop { get; }
    public StructureData Data { get; }
    public BuildableStructure(StructureDrop drop)
    {
        Drop = drop;
        Data = drop.GetServersideData();
    }

    object IBuildable.Drop => Drop;
    object IBuildable.Data => Data;
}
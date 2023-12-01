using SDG.Unturned;
using Uncreated.SQL;
using Uncreated.Warfare.Structures;
using UnityEngine;

namespace Uncreated.Warfare.Events;
public abstract class SalvageRequested : BreakablePlayerEvent, IBuildableDestroyedEvent
{
    protected IBuildable? BuildableCache;
    protected readonly object RegionObj;
    protected SqlItem<SavedStructure>? StructureSave;
    protected bool IsStructureSaved;

    private readonly byte _x;
    private readonly byte _y;
    private readonly uint _instanceId;
    public byte RegionPosX => _x;
    public byte RegionPosY => _y;
    public uint InstanceID => _instanceId;
    public bool IsSaved => IsStructureSaved;
    public abstract IBuildable Buildable { get; }
    public SqlItem<SavedStructure>? Save => StructureSave;
    public Transform Transform { get; protected set; }
    UCPlayer? IBuildableDestroyedEvent.Instigator => Player;
    object IBuildableDestroyedEvent.Region => RegionObj;
    ulong IBuildableDestroyedEvent.InstigatorId => Player.Steam64;
    public EDamageOrigin DamageOrigin => EDamageOrigin.Unknown;
    protected SalvageRequested(UCPlayer player, object region, byte x, byte y, uint instanceId) : base(player)
    {
        RegionObj = region;
        _x = x;
        _y = y;
        _instanceId = instanceId;
    }
}

using SDG.Unturned;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Models.Assets;
using Uncreated.Warfare.Models.Buildables;
using UnityEngine;

namespace Uncreated.Warfare.Events;
public abstract class SalvageRequested : BreakablePlayerEvent, IBuildableDestroyedEvent
{
    protected IBuildable? BuildableCache;
    protected readonly object RegionObj;
    protected BuildableSave? BuildableSave;
    protected bool IsStructureSaved;

    private readonly byte _x;
    private readonly byte _y;
    private readonly uint _instanceId;
    public byte RegionPosX => _x;
    public byte RegionPosY => _y;
    public uint InstanceID => _instanceId;
    public bool IsSaved => IsStructureSaved;
    public abstract IBuildable Buildable { get; }
    public BuildableSave? Save => BuildableSave;
    public Transform Transform { get; protected set; }
    UCPlayer? IBuildableDestroyedEvent.Instigator => Player;
    object IBuildableDestroyedEvent.Region => RegionObj;
    ulong IBuildableDestroyedEvent.InstigatorId => Player.Steam64;
    public EDamageOrigin DamageOrigin => EDamageOrigin.Unknown;
    public UnturnedAssetReference PrimaryAsset { get; }
    public UnturnedAssetReference SecondaryAsset { get; }
    protected SalvageRequested(UCPlayer player, object region, byte x, byte y, uint instanceId, UnturnedAssetReference primaryAsset, UnturnedAssetReference secondaryAsset) : base(player)
    {
        RegionObj = region;
        _x = x;
        _y = y;
        _instanceId = instanceId;
        PrimaryAsset = primaryAsset;
        SecondaryAsset = secondaryAsset;
    }
}

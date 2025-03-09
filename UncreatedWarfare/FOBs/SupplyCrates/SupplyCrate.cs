using System;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.FOBs.Entities;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Timing;

namespace Uncreated.Warfare.FOBs.SupplyCrates;

public class SupplyCrate : IBuildableFobEntity, IDisposable
{
    private readonly byte[]? _originalBarricadeState;
    private readonly ILoopTicker _refillLoop;
    public SupplyType Type { get; }
    public float SupplyCount { get; set; }
    public float MaxSupplyCount { get; }
    public float SupplyRadius { get; set; }

    public IBuildable Buildable {  get; }

    public Vector3 Position => Buildable.Position;

    public Quaternion Rotation => Buildable.Rotation;
    public bool WipeStorageOnDestroy => true;

    public IAssetLink<Asset> IdentifyingAsset { get; }

    public SupplyCrate(SupplyCrateInfo info, IBuildable buildable, ILoopTickerFactory loopTickerFactory, bool enableAutoRefill = false)
    {
        Buildable = buildable;
        Type = info.Type;
        SupplyCount = info.StartingSupplies;
        MaxSupplyCount = info.StartingSupplies;
        SupplyRadius = info.SupplyRadius;
        IdentifyingAsset = info.SupplyItemAsset;
        if (!buildable.IsStructure)
            _originalBarricadeState = buildable.GetItem<Barricade>().state;
        
        if (enableAutoRefill)
            _refillLoop = loopTickerFactory.CreateTicker(TimeSpan.FromSeconds(60), false, true, OnRefillTick);
    }

    // Supply crates that are barricades reset to their original barricade state every 60 seconds or so.
    // This is done to refill all the items inside which are important for building fobs and may get used up over time.
    private void OnRefillTick(ILoopTicker ticker, TimeSpan timesincestart, TimeSpan deltatime)
    {
        if (Buildable.IsStructure || _originalBarricadeState == null)
            return;

        BarricadeDrop drop = Buildable.GetDrop<BarricadeDrop>();
        BarricadeUtility.WriteOwnerAndGroup(_originalBarricadeState, drop, Buildable.Owner.m_SteamID, Buildable.Group.m_SteamID);
        BarricadeUtility.SetState(drop, _originalBarricadeState);
    }

    public bool IsWithinRadius(Vector3 point) => MathUtility.WithinRange(Buildable.Position, point, SupplyRadius);

    public override bool Equals(object? obj)
    {
        return obj is SupplyCrate crate && Buildable.Equals(crate.Buildable);
    }

    public override int GetHashCode()
    {
        return Buildable.GetHashCode();
    }

    public void Dispose()
    {
        _refillLoop.Dispose();
    }
}
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.FOBs.Construction;
using Uncreated.Warfare.FOBs.Entities;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Timing;

namespace Uncreated.Warfare.FOBs.SupplyCrates;

public class SupplyCrate : RestockableBuildableFobEntity<SupplyCrateInfo>
{
    private readonly ILoopTicker? _despawnTicker;
    private SemaphoreSlim? _supplyAccessSync;

    internal SemaphoreSlim SupplyAccessSync
    {
        get
        {
            if (_supplyAccessSync != null)
                return _supplyAccessSync;

            SemaphoreSlim @new = new SemaphoreSlim(1, 1);
            SemaphoreSlim? old = Interlocked.CompareExchange(ref _supplyAccessSync, @new, null);
            if (old != null)
                @new.Dispose();

            return _supplyAccessSync;
        }
    }
    
    public CrateType Type { get; }

    public float SupplyCount
    {
        get;
        set
        {
            if (Mathf.Approximately(field, value))
                return;

            field = value;
            OnSupplyCountUpdated?.Invoke();
        }
    }

    public float MaxSupplyCount { get; }
    public float Radius { get; set; }
    
    public SupplyCrateStack Stack { get; set; } // techdebt: since we removed long-lasting depletable supply crates, we probably don't need to worry about stacking anymore
    public StackedSupplyCrate StackInfo { get; set; }

    public event Action? OnSupplyCountUpdated;

    public SupplyCrate(SupplyCrateInfo info, IBuildable buildable, IServiceProvider serviceProvider, Team team, SupplyCrateStack? stack, int level, int index, bool enableAutoRestock = false)
        : base(buildable, serviceProvider, enableAutoRestock, info, team, iconVisible: stack == null)
    {
        Type = info.Type;
        SupplyCount = info.StartingSupplies;
        MaxSupplyCount = info.StartingSupplies;
        Radius = info.SupplyRadius;
        if (info.DespawnAfter.HasValue)
        {
            _despawnTicker = serviceProvider.GetRequiredService<ILoopTickerFactory>()
                .CreateTicker(
                    info.DespawnAfter.Value,
                    TimeSpan.FromSeconds(20),
                    queueOnGameThread: true,
                    onTick: (_, _, _) => TryDespawnIfNoPlayersAround(serviceProvider)
                );
        }

        Stack = stack ?? new SupplyCrateStack(this, serviceProvider);
        StackedSupplyCrate? crate = Stack.Crates.FirstOrDefault(x => ReferenceEquals(x.Crate, this));
        crate ??= Stack.AddCrate(this, level, index);

        StackInfo = crate;
    }

    private void TryDespawnIfNoPlayersAround(IServiceProvider serviceProvider)
    {
        IPlayerService? playerService = serviceProvider.GetService<IPlayerService>();
        if (playerService == null || !playerService.OnlinePlayersOnTeam(Team).Any(p => IsWithinRadius(p.Position)))
        {
            Buildable.Destroy();
        }
    }

    // called when a nearby buildable is destroyed to try to fall to the ground if needed
    internal void RecheckSupport()
    {
        if (!BuildableExtensions.TryGetBuildableBounds(Buildable.Asset, out Bounds localBounds))
        {
            return;
        }

        Quaternion rotation = Buildable.Rotation * BarricadeUtility.InverseDefaultBarricadeRotation;

        Vector3 boxCenter = Position;
        Vector3 boundsExtents = Stack.ColliderObject.transform.TransformVector(localBounds.extents);
        float supportBoxSize = boundsExtents.y;

        // test for support below. basically checks a rectangle stretching the bottom face of the buildable
        Vector3 supportCenter = new Vector3(boxCenter.x, boxCenter.y - boundsExtents.y, boxCenter.z);
        Vector3 supprtExtents = new Vector3(boundsExtents.x * 0.9f, supportBoxSize, boundsExtents.z * 0.9f);
        if (Physics.CheckBox(supportCenter, supprtExtents, rotation, SupplyCrateStack.RayMaskBlockSupplyCrate, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        if (!Physics.Raycast(
                boxCenter,
                direction: Vector3.down,
                out RaycastHit hit,
                maxDistance: 1024f,
                RayMasks.BLOCK_COLLISION & ~RayMasks.VEHICLE,
                QueryTriggerInteraction.Ignore
            ))
        {
            // somehow floating above nothing, won't happen normally
            return;
        }

        // attempt to lock the barricade to the ground's normal
        Vector3 normal = hit.normal;
        Vector3 position = hit.point;
        rotation = Quaternion.LookRotation(Vector3.Cross(Buildable.Rotation * Vector3.right, normal), normal);
        if (Buildable.Asset is ItemBarricadeAsset barricade)
        {
            position += rotation * new Vector3(0f, barricade.offset, 0f);
        }

        Buildable.SetPositionAndRotation(position, rotation * BarricadeUtility.DefaultBarricadeRotation);
        Icon.Update();
    }

    public bool IsWithinRadius(Vector3 point) => MathUtility.WithinRange(Buildable.Position, point, Radius);

    public override bool Equals(object? obj)
    {
        return obj is SupplyCrate crate && Buildable.Equals(crate.Buildable);
    }

    public override int GetHashCode()
    {
        return Buildable.GetHashCode();
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        _despawnTicker?.Dispose();

        _supplyAccessSync?.Dispose();

        if (!StackInfo.IsRemoved)
            Stack.RemoveCrate(StackInfo);

        base.Dispose();
    }

    /// <inheritdoc />
    public override void UpdateConfiguration(FobConfiguration configuration)
    {
        base.UpdateConfiguration(configuration);

        // update each stack only once (check if this is the first crate)
        if (Stack.Crates.Count > 0 && (object)Stack.Crates[0].Crate == this)
        {
            Stack.UpdateIconDisplay();
        }
    }
}
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Fobs.Entities;
using Uncreated.Warfare.FOBs.Construction;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Timing;

namespace Uncreated.Warfare.FOBs.SupplyCrates;

public class SupplyCrate : RestockableBuildableFobEntity<SupplyCrateInfo>
{
    private ILoopTicker? _despawnTicker;
    
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
        : base(buildable, serviceProvider, enableAutoRestock, info, team)
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

        Stack = stack ?? new SupplyCrateStack(this);
        StackedSupplyCrate? crate = Stack.Crates.FirstOrDefault(x => ReferenceEquals(x.Crate, this));
        crate ??= Stack.AddCrate(this, level, index);

        StackInfo = crate;
    }

    private void TryDespawnIfNoPlayersAround(IServiceProvider serviceProvider)
    {
        PlayerService? playerService = serviceProvider.GetService<PlayerService>();
        if (playerService == null || !playerService
                .OnlinePlayersOnTeam(Team).Any(p => IsWithinRadius(p.Position)))
        {
            Buildable.Destroy();
        }
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
        
        if (!StackInfo.IsRemoved)
            Stack.RemoveCrate(StackInfo);

        base.Dispose();
    }
}
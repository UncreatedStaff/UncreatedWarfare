using System;
using System.Linq;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Fobs.Entities;
using Uncreated.Warfare.FOBs.Construction;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.FOBs.SupplyCrates;

public class SupplyCrate : RestockableBuildableFobEntity<SupplyCrateInfo>
{
    public SupplyType Type { get; }
    public float SupplyCount { get; set; }
    public float MaxSupplyCount { get; }
    public float SupplyRadius { get; set; }
    
    public SupplyCrateStack Stack { get; set; }
    public StackedSupplyCrate StackInfo { get; set; }

    public SupplyCrate(SupplyCrateInfo info, IBuildable buildable, IServiceProvider serviceProvider, Team team, SupplyCrateStack? stack, int level, int index, bool enableAutoRestock = false)
        : base(buildable, serviceProvider, enableAutoRestock, info, team)
    {
        Type = info.Type;
        SupplyCount = info.StartingSupplies;
        MaxSupplyCount = info.StartingSupplies;
        SupplyRadius = info.SupplyRadius;

        Stack = stack ?? new SupplyCrateStack(this);
        StackedSupplyCrate? crate = Stack.Crates.FirstOrDefault(x => ReferenceEquals(x.Crate, this));
        crate ??= Stack.AddCrate(this, level, index);

        StackInfo = crate;
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

    /// <inheritdoc />
    public override void Dispose()
    {
        if (!StackInfo.IsRemoved)
            Stack.RemoveCrate(StackInfo);

        base.Dispose();
    }
}
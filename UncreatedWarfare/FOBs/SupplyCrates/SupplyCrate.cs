using System;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Fobs.Entities;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.FOBs.SupplyCrates;

public class SupplyCrate : RestockableBuildableFobEntity
{
    public SupplyType Type { get; }
    public float SupplyCount { get; set; }
    public float MaxSupplyCount { get; }
    public float SupplyRadius { get; set; }

    public SupplyCrate(SupplyCrateInfo info, IBuildable buildable, IServiceProvider serviceProvider, bool enableAutoRestock = false)
        : base(buildable, serviceProvider, enableAutoRestock)
    {
        Type = info.Type;
        SupplyCount = info.StartingSupplies;
        MaxSupplyCount = info.StartingSupplies;
        SupplyRadius = info.SupplyRadius;
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
}
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.FOBs.SupplyCrates;
public class SupplyCrate : IFobItem
{
    public IBuildable Buildable { get; private set; }
    public SupplyType Type { get; }
    public int SupplyCount { get; set; }
    public int SupplyRadius { get; set; }

    public SupplyCrate(SupplyCrateInfo info, IBuildable buildable)
    {
        Type = info.Type;
        Buildable = buildable;
        SupplyCount = info.StartingSupplies;
        SupplyRadius = info.SupplyRadius;
    }
    public bool IsWithinRadius(Vector3 point) => MathUtility.WithinRange(Buildable.Position, point, SupplyRadius);
}

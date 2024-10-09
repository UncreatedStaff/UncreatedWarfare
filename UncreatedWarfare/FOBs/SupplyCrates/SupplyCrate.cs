using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Fobs;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.FOBs.SupplyCrates;
internal class SupplyCrate : IFobItem
{
    public IBuildable Buildable { get; private set; }
    public SupplyType Type { get; }
    public int SupplyCount { get; set; }

    public SupplyCrate(SupplyCrateInfo info, IBuildable buildable)
    {
        SupplyCount = info.StartingSupplies;
        Buildable = buildable;
        Type = info.Type;
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.FOBs.SupplyCrates;
internal class SupplyCrateInfo
{
    public IAssetLink<ItemBarricadeAsset> SupplyItemAsset { get; set; }
    public IAssetLink<EffectAsset> PlacementEffect { get; set; }
    public int StartingSupplies { get ; set; }
    public SupplyType Type { get ; set; }
}

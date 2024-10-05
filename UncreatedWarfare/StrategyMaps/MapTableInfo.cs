using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.StrategyMaps;
public class MapTableInfo
{
    public IAssetLink<ItemBarricadeAsset> BuildableAsset { get; set; }
    public float MapTableSquareWidth { get; set; }
    public float VerticalSurfaceOffset { get; set; }
}

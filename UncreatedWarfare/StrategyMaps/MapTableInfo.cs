using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.StrategyMaps;

public class MapTableInfo
{
    public required IAssetLink<ItemBarricadeAsset> BuildableAsset { get; set; }
    public required float MapTableSquareWidth { get; set; }
    public required float VerticalSurfaceOffset { get; set; }
}

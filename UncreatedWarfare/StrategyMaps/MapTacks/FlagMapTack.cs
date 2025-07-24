using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Layouts.Flags;

namespace Uncreated.Warfare.StrategyMaps.MapTacks;

public class FlagMapTack : MapTack
{
    public FlagObjective Flag { get; }
    public override Vector3 FeatureWorldPosition => Flag.Center;

    public FlagMapTack(IAssetLink<ItemBarricadeAsset> markerAsset, FlagObjective flag)
        : base(markerAsset, flag.Center)
    {
        Flag = flag;
    }
}
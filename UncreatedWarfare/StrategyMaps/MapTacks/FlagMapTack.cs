using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Layouts.Flags;

namespace Uncreated.Warfare.StrategyMaps.MapTacks;

public class FlagMapTack : MapTack
{
    public FlagObjective Flag { get; }
    public override Vector3 FeatureWorldPosition => Flag.Center;

    public FlagMapTack(StrategyMapManager manager, StrategyMap map, IAssetLink<ItemPlaceableAsset> markerAsset, FlagObjective flag, IMapTackUIHandler? uiHandler = null, bool leaveUiHandlerOpen = false)
        : base(manager, map, markerAsset, flag.Center, uiHandler, leaveUiHandlerOpen)
    {
        Flag = flag;
    }
}
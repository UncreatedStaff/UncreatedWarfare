using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.StrategyMaps.MapTacks;

/// <summary>
/// Allows fobs to implement custom strategy map tack logic when they're registered.
/// </summary>
public interface IFobStrategyMapTackHandler
{
    MapTack? CreateMapTack(StrategyMap map, AssetConfiguration assetConfiguration);
}

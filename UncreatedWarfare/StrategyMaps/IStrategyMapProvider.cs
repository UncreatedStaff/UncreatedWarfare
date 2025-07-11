using System;
using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.StrategyMaps;

/// <summary>
/// Allows a service to provide map tacks for a strategy map.
/// </summary>
public interface IStrategyMapProvider
{
    void RepopulateStrategyMap(StrategyMap strategyMap, AssetConfiguration assetConfiguration, IServiceProvider serviceProvider);
}
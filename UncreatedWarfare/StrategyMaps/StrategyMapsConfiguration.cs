using Microsoft.Extensions.Configuration;
using System;
using System.Collections.ObjectModel;
using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.StrategyMaps;

public sealed class StrategyMapsConfiguration : BaseAlternateConfigurationFile
{
    public IReadOnlyList<MapTableInfo> MapTables { get; private set; } = null!;

    public StrategyMapsConfiguration(IServiceProvider serviceProvider) : base(serviceProvider, "StrategyMaps.yml", mapSpecific: false)
    {
        HandleChange();
    }

    protected override void HandleChange()
    {
        List<MapTableInfo> mapTables = GetSection("MapTables").Get<List<MapTableInfo>>() ?? new List<MapTableInfo>(0);

        mapTables.ForEach(x =>
        {
            x.BuildableAsset ??= AssetLink.Empty<ItemBarricadeAsset>();
        });

        MapTables = new ReadOnlyCollection<MapTableInfo>(mapTables);
    }
}
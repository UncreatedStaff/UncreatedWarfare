using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.StrategyMaps;
using Uncreated.Warfare.StrategyMaps.MapTacks;

namespace Uncreated.Warfare.Commands;

[Command("addtack"), SubCommandOf(typeof(DebugStrategyMaps))]
internal sealed class DebugSquadUI : IExecutableCommand
{
    private readonly StrategyMapManager _strategyMapManager;

    public required CommandContext Context { get; init; }

    public DebugSquadUI(IServiceProvider serviceProvider)
    {
        _strategyMapManager = serviceProvider.GetRequiredService<StrategyMapManager>();
    }

    public UniTask ExecuteAsync(CancellationToken token)
    {
        if (!Context.TryGet(0, out ItemBarricadeAsset? markerAsset, out _))
        {
            throw Context.ReplyString("Could not find the specified Map Tack barricade asset.");
        }

        if (!Context.TryGet(1, out float featureWorldX))
        {
            throw Context.SendHelp();
        }

        if (!Context.TryGet(2, out float featureWorldZ))
        {
            throw Context.SendHelp();
        }

        if (!Context.TryGetBarricadeTarget(out BarricadeDrop? drop))
        {
            throw Context.ReplyString("You must be looking at a registered Strategy Map barricade.");
        }
        StrategyMap? strategyMap = _strategyMapManager.GetStrategyMap(drop.instanceID);
        if (strategyMap == null)
        {
            throw Context.ReplyString("That barricade is not a registered Strategy Map.");
        }

        strategyMap.AddMapTack(new MapTack(AssetLink.Create(markerAsset), new Vector3(featureWorldX, 0, featureWorldZ)));
        
        return UniTask.CompletedTask;
    }
}

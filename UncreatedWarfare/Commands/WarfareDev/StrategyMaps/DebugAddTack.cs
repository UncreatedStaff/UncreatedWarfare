using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.StrategyMaps;
using Uncreated.Warfare.StrategyMaps.MapTacks;

namespace Uncreated.Warfare.Commands.WarfareDev.StrategyMaps;

[Command("addtack"), SubCommandOf(typeof(DebugStrategyMaps))]
internal class DebugAddTack : IExecutableCommand
{
    private readonly StrategyMapManager _strategyMapManager;
    private readonly ILogger _logger;

    public CommandContext Context { get; set; }

    public DebugAddTack(IServiceProvider serviceProvider, ILogger<DebugAddTack> logger)
    {
        _strategyMapManager = serviceProvider.GetRequiredService<StrategyMapManager>();
        _logger = logger;
    }

    public UniTask ExecuteAsync(CancellationToken token)
    {
        _logger.LogInformation("sss");
        if (!Context.TryGet(0, out ItemBarricadeAsset? markerAsset, out bool multipleFound))
        {
            throw Context.ReplyString("Could not find the specified Map Tack barricade asset.");
        }

        _logger.LogInformation("yuh");
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

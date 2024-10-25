using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Commands.WarfareDev.StrategyMaps;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Squads.UI;
using Uncreated.Warfare.StrategyMaps;
using Uncreated.Warfare.StrategyMaps.MapTacks;

namespace Uncreated.Warfare.Commands.WarfareDev.Squads;

[Command("menu"), SubCommandOf(typeof(DebugSquads))]
internal class DebugSquadMenuUI : IExecutableCommand
{
    private readonly SquadManager _squadManager;
    private readonly SquadMenuUI _squadMenuUI;
    private readonly ILogger _logger;

    public CommandContext Context { get; set; }

    public DebugSquadMenuUI(IServiceProvider serviceProvider, ILogger<DebugSquadMenuUI> logger)
    {
        _squadManager = serviceProvider.GetRequiredService<SquadManager>();
        _squadMenuUI = serviceProvider.GetRequiredService<SquadMenuUI>();
        _logger = logger;
    }

    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        _squadMenuUI.OpenUI(Context.Player);

        return UniTask.CompletedTask;
    }
}

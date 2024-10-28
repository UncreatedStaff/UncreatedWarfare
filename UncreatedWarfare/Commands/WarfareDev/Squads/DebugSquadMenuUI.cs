using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Squads.UI;

namespace Uncreated.Warfare.Commands;

[Command("menu"), SubCommandOf(typeof(DebugSquads))]
internal class DebugSquadMenuUI : IExecutableCommand
{
    private readonly SquadMenuUI _squadMenuUI;

    public CommandContext Context { get; set; }

    public DebugSquadMenuUI(IServiceProvider serviceProvider)
    {
        _squadMenuUI = serviceProvider.GetRequiredService<SquadMenuUI>();
    }

    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        _squadMenuUI.OpenUI(Context.Player);

        return UniTask.CompletedTask;
    }
}

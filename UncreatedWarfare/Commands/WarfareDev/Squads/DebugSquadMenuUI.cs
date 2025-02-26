using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Squads.UI;

namespace Uncreated.Warfare.Commands;

[Command("menu"), SubCommandOf(typeof(DebugSquads))]
internal sealed class DebugSquadMenuUI : IExecutableCommand
{
    private readonly SquadMenuUI _squadMenuUI;

    public required CommandContext Context { get; init; }

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

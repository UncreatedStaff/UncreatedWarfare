using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.FOBs.StateStorage;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("savestate", "save"), SubCommandOf(typeof(DevBuildables))]
internal sealed class DevBarricadeSaveState : IExecutableCommand
{
    private readonly BarricadeStateStore? _buildableStateStore;
    private readonly IFactionDataStore? _factionStore;
    private readonly DevBuildablesTranslations _translations;

    public required CommandContext Context { get; init; }

    public DevBarricadeSaveState(IServiceProvider serviceProvider)
    {
        _buildableStateStore = serviceProvider.GetService<BarricadeStateStore>();
        _factionStore = serviceProvider.GetService<IFactionDataStore>();
        _translations = serviceProvider.GetRequiredService<TranslationInjection<DevBuildablesTranslations>>().Value;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        if (_buildableStateStore == null)
        {
            throw Context.Reply(_translations.StateStorageNotSupported);
        }
        if (!Context.TryGetBarricadeTarget(out BarricadeDrop? buildable))
        {
            throw Context.Reply(_translations.NotLookingAtBarricade);
        }

        FactionInfo? factionInfo = null;
        if (Context.TryGet(0, out string? factionId))
        {
            if (_factionStore == null)
            {
                throw Context.Reply(_translations.FactionsNotSupported);
            }
            factionInfo = _factionStore.FindFaction(factionId);
            if (factionInfo == null)
            {
                throw Context.Reply(_translations.FactionsNotFound, factionId);
            }
        }

        byte[] state = buildable.GetServersideData().barricade.state;

        await _buildableStateStore.SaveAsync(buildable.asset, state, factionInfo, token);

        throw Context.Reply(_translations.BarricadeSaveStateSuccess, buildable.asset, factionInfo ?? FactionInfo.NoFaction);
    }
}

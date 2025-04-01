using System;
using Uncreated.Warfare.FOBs.StateStorage;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Commands;

[Command("apply"), SubCommandOf(typeof(DevBarricadeSaveState))]
internal sealed class DevBarricadeSaveStateApply : IExecutableCommand
{
    private readonly BarricadeStateStore? _buildableStateStore;
    private readonly DevBuildablesTranslations _translations;

    public required CommandContext Context { get; init; }

    public DevBarricadeSaveStateApply(BarricadeStateStore buildableStateStore, TranslationInjection<DevBuildablesTranslations> translations)
    {
        _buildableStateStore = buildableStateStore;
        _translations = translations.Value;
    }

    public UniTask ExecuteAsync(CancellationToken token)
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

        BarricadeStateSave? save = _buildableStateStore.FindBarricadeSave(buildable.asset, Context.Player.Team.Faction);

        if (save == null)
        {
            throw Context.Reply(_translations.NotLookingAtSavedBarricade);
        }

        byte[] state = Convert.FromBase64String(save.Base64State);

        BarricadeData data = buildable.GetServersideData();
        BarricadeUtility.WriteOwnerAndGroup(state, buildable, data.owner, data.group);
        BarricadeUtility.SetState(buildable, state);
        Context.Reply(_translations.StateRestocked);

        return UniTask.CompletedTask;
    }
}

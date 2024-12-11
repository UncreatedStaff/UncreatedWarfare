using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("remove", "delete"), SubCommandOf(typeof(StructureCommand))]
internal sealed class StructureRemoveCommand : IExecutableCommand
{
    private readonly BuildableSaver _saver;
    private readonly StructureTranslations _translations;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public StructureRemoveCommand(BuildableSaver saver, TranslationInjection<StructureTranslations> translations)
    {
        _saver = saver;
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        if (Context.TryGetStructureTarget(out StructureDrop? structure))
        {
            if (!await _saver.DiscardStructureAsync(structure.instanceID, token))
            {
                throw Context.Reply(_translations.StructureAlreadyUnsaved, structure.asset);
            }

            await UniTask.SwitchToMainThread(token);

            Context.LogAction(ActionLogType.UnsaveStructure, $"{structure.asset.itemName} / {structure.asset.id} / {structure.asset.GUID:N} " +
                                                             $"at {structure.GetServersideData().point} ({structure.instanceID})");
            Context.Reply(_translations.StructureUnsaved, structure.asset);
        }
        else if (Context.TryGetBarricadeTarget(out BarricadeDrop? barricade))
        {
            if (!await _saver.DiscardBarricadeAsync(barricade.instanceID, token))
            {
                throw Context.Reply(_translations.StructureAlreadyUnsaved, barricade.asset);
            }

            await UniTask.SwitchToMainThread(token);

            Context.LogAction(ActionLogType.UnsaveStructure, $"{barricade.asset.itemName} / {barricade.asset.id} / {barricade.asset.GUID:N} " +
                                                             $"at {barricade.GetServersideData().point} ({barricade.instanceID})");
            Context.Reply(_translations.StructureUnsaved, barricade.asset);
        }
        else throw Context.Reply(_translations.StructureNoTarget);
    }
}
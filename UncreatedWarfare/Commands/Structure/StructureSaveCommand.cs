using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("save"), SubCommandOf(typeof(StructureCommand))]
internal sealed class StructureSaveCommand : IExecutableCommand
{
    private readonly BuildableSaver _saver;
    private readonly StructureTranslations _translations;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public StructureSaveCommand(BuildableSaver saver, TranslationInjection<StructureTranslations> translations)
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
            if (!await _saver.SaveStructureAsync(structure, token))
            {
                throw Context.Reply(_translations.StructureAlreadySaved, structure.asset);
            }

            await UniTask.SwitchToMainThread(token);

            Context.Reply(_translations.StructureSaved, structure.asset);
            // todo: Context.LogAction(ActionLogType.SaveStructure, $"{structure.asset.itemName} / {structure.asset.id} / {structure.asset.GUID:N} " +
            //                                                $"at {structure.GetServersideData().point:0:##} ({structure.instanceID})");
        }
        else if (Context.TryGetBarricadeTarget(out BarricadeDrop? barricade))
        {
            if (!await _saver.SaveBarricadeAsync(barricade, token))
            {
                throw Context.Reply(_translations.StructureAlreadySaved, barricade.asset);
            }

            await UniTask.SwitchToMainThread(token);

            Context.Reply(_translations.StructureSaved, barricade.asset);
            // todo: Context.LogAction(ActionLogType.SaveStructure, $"{barricade.asset.itemName} / {barricade.asset.id} / {barricade.asset.GUID:N} " +
            //                                                $"at {barricade.GetServersideData().point:0:##} ({barricade.instanceID})");
        }
        else throw Context.Reply(_translations.StructureNoTarget);
    }
}
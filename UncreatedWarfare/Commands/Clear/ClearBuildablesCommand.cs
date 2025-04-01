using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Commands;

[Command("buildables", "structures", "barricades", "buildable", "structure", "barricade", "struct", "b", "s"), SubCommandOf(typeof(ClearCommand))]
internal sealed class ClearBuildablesCommand : IExecutableCommand
{
    private readonly BuildableSaver _buildableSaver;
    private readonly ClearTranslations _translations;

    public required CommandContext Context { get; init; }
    public ClearBuildablesCommand(TranslationInjection<ClearTranslations> translations, BuildableSaver buildableSaver)
    {
        _buildableSaver = buildableSaver;
        _translations = translations.Value;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        await _buildableSaver.DestroyUnsavedBuildables(Context.CallerId, token).ConfigureAwait(false);

        await UniTask.SwitchToMainThread(token);

        // todo: Context.LogAction(ActionLogType.ClearStructures);
        Context.Reply(_translations.ClearStructures);
    }
}
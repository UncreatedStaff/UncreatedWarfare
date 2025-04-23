using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("buildables", "structures", "barricades", "buildable", "structure", "barricade", "struct", "b", "s"), SubCommandOf(typeof(ClearCommand))]
internal sealed class ClearBuildablesCommand : IExecutableCommand
{
    private readonly MainBaseBuildables _mainBaseBuildables;
    private readonly ClearTranslations _translations;

    public required CommandContext Context { get; init; }
    public ClearBuildablesCommand(TranslationInjection<ClearTranslations> translations, MainBaseBuildables mainBaseBuildables)
    {
        _mainBaseBuildables = mainBaseBuildables;
        _translations = translations.Value;
    }

    public UniTask ExecuteAsync(CancellationToken token)
    {
        _mainBaseBuildables.ClearOtherBuildables();

        // todo: Context.LogAction(ActionLogType.ClearStructures);
        Context.Reply(_translations.ClearStructures);
        return UniTask.CompletedTask;
    }
}
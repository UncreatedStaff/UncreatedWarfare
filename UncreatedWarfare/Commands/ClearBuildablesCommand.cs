using Uncreated.Warfare.Commands.Dispatch;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("buildables", "structures", "barricades", "buildable", "structure", "barricade", "struct", "b", "s"), SubCommandOf(typeof(ClearCommand))]
public class ClearBuildablesCommand : IExecutableCommand
{
    private readonly ClearTranslations _translations;

    public CommandContext Context { get; set; }
    public ClearBuildablesCommand(TranslationInjection<ClearTranslations> translations)
    {
        _translations = translations.Value;
    }

    public UniTask ExecuteAsync(CancellationToken token)
    {
        // todo
        Data.Gamemode.ReplaceBarricadesAndStructures();
        Context.LogAction(ActionLogType.ClearStructures);
        Context.Reply(_translations.ClearStructures);

        return UniTask.CompletedTask;
    }
}
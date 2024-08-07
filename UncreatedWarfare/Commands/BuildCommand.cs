using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[HideFromHelp, Command("build")]
public class BuildCommand : IExecutableCommand
{
    private readonly FobTranslations _translations;

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    public BuildCommand(TranslationInjection<FobTranslations> translations)
    {
        _translations = translations.Value;
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        throw Context.Reply(_translations.BuildLegacyExplanation);
    }
}
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("translations", "lang"), SubCommandOf(typeof(DebugReload))]
internal sealed class DebugReloadTranslations : IExecutableCommand
{
    private readonly ITranslationService _translationService;
    public required CommandContext Context { get; init; }

    public DebugReloadTranslations(ITranslationService translationService)
    {
        _translationService = translationService;
    }

    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        _translationService.ReloadAll();

        return UniTask.CompletedTask;
    }
}
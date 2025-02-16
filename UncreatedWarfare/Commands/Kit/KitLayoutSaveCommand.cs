using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("save", "confirm", "keep"), SubCommandOf(typeof(KitLayoutCommand))]
internal sealed class KitLayoutSaveCommand : IExecutableCommand
{
    private readonly KitLayoutService _layoutService;
    private readonly KitCommandTranslations _translations;

    public required CommandContext Context { get; init; }

    public KitLayoutSaveCommand(TranslationInjection<KitCommandTranslations> translations, KitLayoutService layoutService)
    {
        _layoutService = layoutService;

        _translations = translations.Value;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        if (!await _layoutService.SaveLayoutAsync(Context.Player, token).ConfigureAwait(false))
        {
            throw Context.Reply(_translations.KitLayoutNoKit);
        }

        Kit? kit = await Context.Player
            .Component<KitPlayerComponent>()
            .GetActiveKitAsync(KitInclude.Translations, token)
            .ConfigureAwait(false);

        Context.Reply(_translations.KitLayoutSaved, kit!);
    }
}
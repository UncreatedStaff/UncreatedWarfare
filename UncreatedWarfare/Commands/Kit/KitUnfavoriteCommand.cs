using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Loadouts;
using Uncreated.Warfare.Signs;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("unfavorite", "unfavourite", "unfavour", "unfavor", "unfav", "unstar"), SubCommandOf(typeof(KitCommand))]
internal sealed class KitUnfavoriteCommand : IExecutableCommand
{
    private readonly KitCommandLookResolver _lookResolver;
    private readonly IKitFavoriteService _kitFavoriteService;
    private readonly KitCommandTranslations _translations;

    public required CommandContext Context { get; init; }

    public KitUnfavoriteCommand(
        TranslationInjection<KitCommandTranslations> translations,
        KitCommandLookResolver lookResolver,
        IKitFavoriteService kitFavoriteService)
    {
        _lookResolver = lookResolver;
        _kitFavoriteService = kitFavoriteService;
        _translations = translations.Value;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        KitCommandLookResult lookResult = await _lookResolver.ResolveFromArgumentsOrLook(Context, 0, 0, KitInclude.Default, token).ConfigureAwait(false);

        await _kitFavoriteService.RemoveFavorite(Context.CallerId, lookResult.Kit.Key, token).ConfigureAwait(false);
        Context.Reply(_translations.KitUnfavorited, lookResult.Kit);
    }
}
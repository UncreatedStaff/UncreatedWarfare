using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("favorite", "favourite", "favour", "favor", "fav", "star"), SubCommandOf(typeof(KitCommand))]
internal sealed class KitFavoriteCommand : IExecutableCommand
{
    private readonly KitCommandLookResolver _lookResolver;
    private readonly IKitFavoriteService _kitFavoriteService;
    private readonly KitCommandTranslations _translations;

    public required CommandContext Context { get; init; }

    public KitFavoriteCommand(
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

        if (!await _kitFavoriteService.AddFavorite(Context.CallerId, lookResult.Kit.Key, token).ConfigureAwait(false))
        {
            throw Context.Reply(_translations.KitFavoriteAlreadyFavorited, lookResult.Kit);
        }

        Context.Reply(_translations.KitFavorited, lookResult.Kit);
    }
}

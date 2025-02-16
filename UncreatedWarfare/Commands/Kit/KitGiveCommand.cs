using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Requests;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("give", "g"), SubCommandOf(typeof(KitCommand))]
internal sealed class KitGiveCommand : IExecutableCommand
{
    private readonly KitCommandTranslations _translations;
    private readonly KitRequestService _kitRequestService;
    private readonly KitCommandLookResolver _lookResolver;

    public required CommandContext Context { get; init; }

    public KitGiveCommand(TranslationInjection<KitCommandTranslations> translations,
        KitRequestService kitRequestService,
        KitCommandLookResolver lookResolver)
    {
        _kitRequestService = kitRequestService;
        _lookResolver = lookResolver;
        _translations = translations.Value;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        KitCommandLookResult kitArg = await _lookResolver.ResolveFromArgumentsOrLook(Context, 0, 0, KitInclude.Giveable, token).ConfigureAwait(false);

        WarfarePlayer? player = null;

        if (Context.HasArgument(kitArg.OptionalArgumentStart))
        {
            (_, player) = await Context.TryGetPlayer(kitArg.OptionalArgumentStart).ConfigureAwait(false);
            if (player == null)
                throw Context.SendPlayerNotFound();
        }

        if (Equals(Context.Player, player))
        {
            player = null;
        }
        
        if (player == null)
        {
            Context.AssertRanByPlayer();
        }

        Kit kit = kitArg.Kit;
        await _kitRequestService.GiveKitAsync(player ?? Context.Player, new KitBestowData(kit), token).ConfigureAwait(false);

        Context.LogAction(ActionLogType.GiveKit, kit.Id);

        if (player == null)
        {
            Context.Reply(_translations.KitGiveSuccess, kit);
        }
        else
        {
            Context.Reply(_translations.KitGiveSuccessToPlayer, kit, player);
        }
    }
}

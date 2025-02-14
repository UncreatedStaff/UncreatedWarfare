using System.Collections.Generic;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Loadouts;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util.Inventory;

namespace Uncreated.Warfare.Commands;

[Command("loadout", "l"), SubCommandOf(typeof(KitGiveCommand))]
internal sealed class KitGiveLoadoutCommand : IExecutableCommand
{
    private readonly KitCommandTranslations _translations;
    private readonly DefaultLoadoutItemsConfiguration _defaultLoadoutItemsConfiguration;
    private readonly IItemDistributionService _itemDistributionService;

    public required CommandContext Context { get; init; }

    public KitGiveLoadoutCommand(
        TranslationInjection<KitCommandTranslations> translations,
        DefaultLoadoutItemsConfiguration defaultLoadoutItemsConfiguration,
        IItemDistributionService itemDistributionService)
    {
        _defaultLoadoutItemsConfiguration = defaultLoadoutItemsConfiguration;
        _itemDistributionService = itemDistributionService;
        _translations = translations.Value;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        if (!Context.TryGet(0, out Class @class))
        {
            if (!Context.HasArgs(1))
                throw Context.SendHelp();

            throw Context.Reply(_translations.ClassNotFound);
        }

        (_, WarfarePlayer? player) = await Context.TryGetPlayer(1).ConfigureAwait(false);

        await UniTask.SwitchToMainThread(token);

        player ??= Context.HasArgument(1) ? throw Context.SendPlayerNotFound() : Context.Player;

        IReadOnlyList<IItem> items = _defaultLoadoutItemsConfiguration.GetDefaultsForClass(@class);

        Context.Player.Component<KitPlayerComponent>().UpdateKit(null);

        _itemDistributionService.GiveItems(items, player);

        Context.Reply(_translations.RequestDefaultLoadoutGiven, @class);
    }
}
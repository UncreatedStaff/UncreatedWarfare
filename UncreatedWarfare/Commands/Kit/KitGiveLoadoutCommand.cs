using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Kits.Loadouts;
using Uncreated.Warfare.Kits.Requests;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Inventory;

namespace Uncreated.Warfare.Commands;

[Command("loadout", "l"), SubCommandOf(typeof(KitGiveCommand))]
internal sealed class KitGiveLoadoutCommand : IExecutableCommand
{
    private readonly KitCommandTranslations _translations;
    private readonly DefaultLoadoutItemsConfiguration _defaultLoadoutItemsConfiguration;
    private readonly IItemDistributionService _itemDistributionService;
    private readonly IKitDataStore _kitDataStore;
    private readonly CommandDispatcher _commandDispatcher;

    public required CommandContext Context { get; init; }

    public KitGiveLoadoutCommand(
        TranslationInjection<KitCommandTranslations> translations,
        DefaultLoadoutItemsConfiguration defaultLoadoutItemsConfiguration,
        IItemDistributionService itemDistributionService,
        IKitDataStore kitDataStore,
        CommandDispatcher commandDispatcher)
    {
        _defaultLoadoutItemsConfiguration = defaultLoadoutItemsConfiguration;
        _itemDistributionService = itemDistributionService;
        _kitDataStore = kitDataStore;
        _commandDispatcher = commandDispatcher;
        _translations = translations.Value;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        if (!Context.TryGet(0, out Class @class))
        {
            if (!Context.HasArgs(1))
                throw Context.SendHelp();

            throw Context.Reply(_translations.ClassNotFound, Context.Get(0)!);
        }

        (_, WarfarePlayer? player) = await Context.TryGetPlayer(1).ConfigureAwait(false);

        Kit? defaultKit = await _kitDataStore.QueryKitAsync(KitRequestService.DefaultKitId, KitInclude.Giveable, token);

        await UniTask.SwitchToMainThread(token);

        player ??= Context.HasArgument(1) ? throw Context.SendPlayerNotFound() : Context.Player;

        IReadOnlyList<IItem> items = _defaultLoadoutItemsConfiguration.GetDefaultsForClass(@class);

        KitPlayerComponent component = Context.Player.Component<KitPlayerComponent>();

        if (defaultKit == null)
        {
            component.UpdateKit(null);
            Context.Player.Save.KitState = null;
        }
        else
        {
            // trick the system into thinking the player is previewing
            // the default kit so they can go back to their old kit afterwards
            CurrentKitState? fallback = component.GetUnderlyingPreviewFallback();
            fallback?.ItemsFallback = ItemUtility.ItemsFromInventory(player).ToArray();

            CurrentKitState newState = new CurrentKitState(defaultKit, true, false, fallback);

            Context.Player.Save.KitState = newState;
            component.UpdateKit(newState);
        }

        _itemDistributionService.GiveItems(items, player);

        Context.Reply(
            _translations.RequestDefaultLoadoutGiven,
            @class,
            _commandDispatcher.FindCommand(typeof(KitBackCommand))
        );
    }
}
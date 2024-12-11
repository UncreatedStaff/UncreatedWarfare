using System.Collections.Generic;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Commands;

[Command("add", "create", "new"), SubCommandOf(typeof(KitHotkeyCommand))]
internal sealed class KitHotkeyAddCommand : IExecutableCommand
{
    private readonly KitCommandTranslations _translations;
    private readonly KitManager _kitManager;
    private readonly AssetRedirectService _assetRedirectService;
    private readonly IFactionDataStore _factionDataStore;

    public required CommandContext Context { get; init; }

    public KitHotkeyAddCommand(TranslationInjection<KitCommandTranslations> translations, KitManager kitManager, AssetRedirectService assetRedirectService, IFactionDataStore factionDataStore)
    {
        _kitManager = kitManager;
        _assetRedirectService = assetRedirectService;
        _factionDataStore = factionDataStore;
        _translations = translations.Value;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        if (!Context.TryGet(Context.ArgumentCount - 1, out byte slot))
        {
            throw Context.SendHelp();
        }

        if (!KitEx.ValidSlot(slot))
        {
            throw Context.Reply(_translations.KitHotkeyInvalidSlot);
        }

        await Context.Player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
        try
        {
            Kit? kit = await Context.Player.Component<KitPlayerComponent>().GetActiveKitAsync(token).ConfigureAwait(false);

            if (kit == null)
            {
                throw Context.Reply(_translations.KitHotkeyNoKit);
            }

            IPageKitItem? item = await _kitManager.GetHeldItemFromKit(Context.Player, token).ConfigureAwait(false);
            
            await UniTask.SwitchToMainThread(token);
            
            if (item == null)
            {
                throw Context.Reply(_translations.KitHotkeyNotHoldingItem);
            }

            ItemAsset? asset = item is ISpecificKitItem i2 ? i2.Item.GetAsset<ItemAsset>() : item.GetItem(kit, Context.Player.Team, out _, out _, _assetRedirectService, _factionDataStore);
            if (asset == null)
                throw Context.Reply(_translations.KitHotkeyNotHoldingItem);

            if (!KitEx.CanBindHotkeyTo(asset, item.Page))
                throw Context.Reply(_translations.KitHotkeyNotHoldingValidItem, asset);

            await _kitManager.AddHotkey(kit.PrimaryKey, Context.CallerId.m_SteamID, slot, item, token).ConfigureAwait(false);
            await UniTask.SwitchToMainThread(token);

            HotkeyPlayerComponent hotkeyComponent = Context.Player.Component<HotkeyPlayerComponent>();
            if (hotkeyComponent.HotkeyBindings != null)
            {
                // remove duplicates / conflicts
                hotkeyComponent.HotkeyBindings.RemoveAll(x =>
                    x.Kit == kit.PrimaryKey && (x.Slot == slot ||
                                                              x.Item.X == item.X &&
                                                              x.Item.Y == item.Y &&
                                                              x.Item.Page == item.Page));
            }
            else hotkeyComponent.HotkeyBindings = new List<HotkeyBinding>(32);

            hotkeyComponent.HotkeyBindings.Add(new HotkeyBinding(kit.PrimaryKey, slot, item, new KitHotkey
            {
                Steam64 = Context.CallerId.m_SteamID,
                KitId = kit.PrimaryKey,
                Item = item is ISpecificKitItem item2 ? item2.Item : null,
                Redirect = item is IAssetRedirectKitItem redir ? redir.RedirectType : null,
                X = item.X,
                Y = item.Y,
                Page = item.Page,
                Slot = slot
            }));

            byte index = KitEx.GetHotkeyIndex(slot);

            PlayerEquipment equipment = Context.Player.UnturnedPlayer.equipment;
            if (KitEx.CanBindHotkeyTo(asset, (Page)equipment.equippedPage))
            {
                equipment.ServerBindItemHotkey(index, asset, equipment.equippedPage, equipment.equipped_x, equipment.equipped_y);
            }

            Context.Reply(_translations.KitHotkeyBinded, asset, slot, kit);
        }
        finally
        {
            Context.Player.PurchaseSync.Release();
        }
    }
}
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Kits.Items;
using Uncreated.Warfare.Models.Assets;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Inventory;

namespace Uncreated.Warfare.Commands;

[Command("add", "create", "new"), SubCommandOf(typeof(KitHotkeyCommand))]
internal sealed class KitHotkeyAddCommand : IExecutableCommand
{
    private readonly IKitsDbContext _dbContext;
    private readonly IKitItemResolver _itemResolver;
    private readonly KitCommandTranslations _translations;

    public required CommandContext Context { get; init; }

    public KitHotkeyAddCommand(TranslationInjection<KitCommandTranslations> translations, IKitsDbContext dbContext, IKitItemResolver itemResolver)
    {
        _dbContext = dbContext;
        _itemResolver = itemResolver;
        _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
        _translations = translations.Value;
    }

    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        if (!Context.TryGet(Context.ArgumentCount - 1, out byte slot))
        {
            throw Context.SendHelp();
        }

        if (!KitItemUtility.ValidSlot(slot))
        {
            throw Context.Reply(_translations.KitHotkeyInvalidSlot);
        }

        Kit? kit = await Context.Player.Component<KitPlayerComponent>().GetActiveKitAsync(KitInclude.Translations | KitInclude.Items, token).ConfigureAwait(false);

        await UniTask.SwitchToMainThread(token);

        if (kit == null)
        {
            throw Context.Reply(_translations.KitHotkeyNoKit);
        }

        ItemJar? heldItem = Context.Player.GetHeldItem(out Page currentPage);
        if (heldItem == null)
        {
            throw Context.Reply(_translations.KitHotkeyNotHoldingItem);
        }

        Context.Player.Component<ItemTrackingPlayerComponent>().GetOriginalItemPosition(currentPage, heldItem.x, heldItem.y, out Page page, out byte x, out byte y);

        IPageItem? kitItem = (IPageItem?)Array.Find(kit.Items, i => i is IPageItem pgItem && pgItem.Page == page && pgItem.X == x && pgItem.Y == y);
        
        if (kitItem == null)
        {
            throw Context.Reply(_translations.KitHotkeyNotHoldingItem);
        }

        KitItemResolutionResult asset = _itemResolver.ResolveKitItem(kitItem, kit, Context.Player.Team);
        if (asset.Asset == null)
            throw Context.Reply(_translations.KitHotkeyNotHoldingItem);

        if (!KitItemUtility.CanBindHotkeyTo(asset.Asset, currentPage))
            throw Context.Reply(_translations.KitHotkeyNotHoldingValidItem, asset.Asset);

        KitHotkey hotkey;
        try
        {
            hotkey = new KitHotkey
            {
                X = kitItem.X,
                Y = kitItem.Y,
                Page = kitItem.Page,
                KitId = kit.Key,
                Steam64 = Context.CallerId.m_SteamID,
                Slot = slot
            };

            if (kitItem is IRedirectedItem r)
            {
                hotkey.Redirect = r.Item;
            }
            else if (kitItem is IConcreteItem c)
            {
                hotkey.Item = new UnturnedAssetReference(c.Item);
            }

            _dbContext.KitHotkeys.Add(hotkey);

            await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);

            _dbContext.ChangeTracker.Clear();
        }
        catch (DbUpdateException ex) when (ex.GetBaseException() is MySqlException { ErrorCode: MySqlErrorCode.DuplicateKeyEntry })
        {
            throw Context.Reply(_translations.KitHotkeyAlreadyBound, slot, kit);
        }

        await UniTask.SwitchToMainThread(token);

        HotkeyPlayerComponent hotkeyComponent = Context.Player.Component<HotkeyPlayerComponent>();
        if (hotkeyComponent.HotkeyBindings != null)
        {
            // remove duplicates / conflicts
            hotkeyComponent.HotkeyBindings.RemoveAll(x => x.KitId == kit.Key && (x.Slot == slot || x.X == kitItem.X && x.Y == kitItem.Y && x.Page == kitItem.Page));
        }
        else
        {
            hotkeyComponent.HotkeyBindings = new List<KitHotkey>(32);
        }

        hotkeyComponent.HotkeyBindings.Add(hotkey);

        byte index = KitItemUtility.GetHotkeyIndex(slot);

        if (KitItemUtility.CanBindHotkeyTo(asset.Asset, currentPage))
        {
            Context.Player.UnturnedPlayer.equipment.ServerBindItemHotkey(index, asset.Asset, (byte)currentPage, heldItem.x, heldItem.y);
        }

        Context.Reply(_translations.KitHotkeyBinded, asset.Asset, slot, kit);
    }
}
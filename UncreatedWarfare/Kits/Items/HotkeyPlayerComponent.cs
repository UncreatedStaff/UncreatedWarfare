using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Items;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Kits.Items;

[PlayerComponent]
internal class HotkeyPlayerComponent : IPlayerComponent, IEventListener<ItemDropped>
{
    private KitManager _kitManager = null!;
    private WarfareModule _module = null!;
    private ILogger<HotkeyPlayerComponent> _logger = null!;
    public WarfarePlayer Player { get; private set; }

    // used to trace items back to their original position in the kit
    internal List<HotkeyBinding>? HotkeyBindings;

    void IPlayerComponent.Init(IServiceProvider serviceProvider, bool isOnJoin)
    {
        _kitManager = serviceProvider.GetRequiredService<KitManager>();
        _module = serviceProvider.GetRequiredService<WarfareModule>();
        _logger = serviceProvider.GetRequiredService<ILogger<HotkeyPlayerComponent>>();
    }

    void IEventListener<ItemDropped>.HandleEvent(ItemDropped e, IServiceProvider serviceProvider)
    {
        if (HotkeyBindings is not { Count: > 0 })
            return;

        if (e.Item == null)
            return;

        CancellationToken tkn = _module.UnloadToken;
        CombinedTokenSources tokens = tkn.CombineTokensIfNeeded(Player.DisconnectToken);

        // move hotkey to a different item of the same type
        Task.Run(async () =>
        {
            try
            {
                tokens.Token.ThrowIfCancellationRequested();
                await ApplyHotkeyAfterDroppingItemAsync(e, tokens.Token);
            }
            catch (OperationCanceledException) when (tokens.Token.IsCancellationRequested) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying hotkey change after dropping item.");
            }
            finally
            {
                tokens.Dispose();
            }
        }, CancellationToken.None);
    }

    internal void HandleItemPickedUpAfterTransformed(ItemDestroyed e, byte origX, byte origY, Page origPage)
    {
        // resend hotkeys from picked up item
        if (!Provider.isInitialized || HotkeyBindings == null || origX >= byte.MaxValue)
            return;

        CancellationToken tkn = _module.UnloadToken;
        CombinedTokenSources tokens = tkn.CombineTokensIfNeeded(Player.DisconnectToken);

        // move hotkey to a different item of the same type
        Task.Run(async () =>
        {
            try
            {
                tokens.Token.ThrowIfCancellationRequested();
                await ApplyHotkeyAfterPickingUpItemAsync(e, origX, origY, origPage, tokens.Token);
            }
            catch (OperationCanceledException) when (tokens.Token.IsCancellationRequested) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying hotkey change after dropping item.");
            }
            finally
            {
                tokens.Dispose();
            }
        }, CancellationToken.None);
    }

    private async Task ApplyHotkeyAfterPickingUpItemAsync(ItemDestroyed e, byte origX, byte origY, Page origPage, CancellationToken token = default)
    {
        if (e.PickUpPlayer == null)
            return;

        await e.PickUpPlayer.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (HotkeyBindings == null)
                return;

            Kit? activeKit = await e.PickUpPlayer.Component<KitPlayerComponent>().GetActiveKitAsync(token).ConfigureAwait(false);
            if (activeKit == null)
                return;

            await UniTask.SwitchToMainThread(token);

            foreach (HotkeyBinding binding in HotkeyBindings)
            {
                if (binding.Kit != activeKit.PrimaryKey || binding.Item.X != origX || binding.Item.Y != origY || binding.Item.Page != origPage)
                    continue;

                ItemAsset? asset = binding.GetAsset(activeKit, 0ul /* todo e.PickUpPlayer.Team */);
                if (asset == null)
                    continue;

                byte index = KitEx.GetHotkeyIndex(binding.Slot);
                if (index == byte.MaxValue || !KitEx.CanBindHotkeyTo(asset, e.PickUpPage))
                    continue;

                e.PickUpPlayer.UnturnedPlayer.equipment.ServerBindItemHotkey(index, asset, (byte)e.PickUpPage, e.PickUpX, e.PickUpY);
#if DEBUG
                _logger.LogTrace("Updating old hotkey (picked up): {0} at {1}, ({2}, {3}).", asset.itemName, e.PickUpPage, e.PickUpX, e.PickUpY);
#endif
                break;
            }
        }
        finally
        {
            e.PickUpPlayer.PurchaseSync.Release();
        }
    }

    internal void HandleItemMovedAfterTransformed(ItemMoved e, byte origX, byte origY, Page origPage, byte swapOrigX, byte swapOrigY, Page swapOrigPage)
    {
        // resend hotkeys from moved item(s)
        if (!Provider.isInitialized || HotkeyBindings == null || (origX >= byte.MaxValue && swapOrigX >= byte.MaxValue))
            return;

        CancellationToken tkn = _module.UnloadToken;
        CombinedTokenSources tokens = tkn.CombineTokensIfNeeded(Player.DisconnectToken);

        Task.Run(async () =>
        {
            try
            {
                tokens.Token.ThrowIfCancellationRequested();
                await ApplyHotkeyAfterMovingItemAsync(e, origX, origY, origPage, swapOrigX, swapOrigY, swapOrigPage, tokens.Token);
            }
            catch (OperationCanceledException) when (tokens.Token.IsCancellationRequested) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying hotkey change after moving item.");
            }
            finally
            {
                tokens.Dispose();
            }
        }, CancellationToken.None);
    }

    private async Task ApplyHotkeyAfterDroppingItemAsync(ItemDropped e, CancellationToken token)
    {
        IPageKitItem? jar2 = await _kitManager.GetItemFromKit(Player, e.OldX, e.OldY, e.Item!, e.OldPage, token).ConfigureAwait(false);
        if (jar2 == null || !Player.IsOnline)
            return;

        await Player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (!Player.IsOnline || HotkeyBindings is not { Count: > 0 })
                return;

            Kit? activeKit = await Player.Component<KitPlayerComponent>().GetActiveKitAsync(token).ConfigureAwait(false);

            await UniTask.SwitchToMainThread(token);

            if (!Player.IsOnline || HotkeyBindings is not { Count: > 0 })
                return;

            for (int i = 0; i < HotkeyBindings.Count; ++i)
            {
                HotkeyBinding b = HotkeyBindings[i];

                if ((b.Item is not ISpecificKitItem item || jar2 is not ISpecificKitItem item2 || item.Item != item2.Item) &&
                    (b.Item is not IAssetRedirectKitItem redir || jar2 is not IAssetRedirectKitItem redir2 || redir.RedirectType != redir2.RedirectType))
                {
                    continue;
                }

                // found a binding for that item
                if (b.Item.X != jar2.X || b.Item.Y != jar2.Y || b.Item.Page != jar2.Page)
                    continue;

                ItemAsset? asset = b.Item switch
                {
                    ISpecificKitItem item3 => item3.Item.GetAsset<ItemAsset>(),
                    IKitItem ki => ki.GetItem(activeKit, Player.Team.Faction, out _, out _),
                    _ => null
                };

                if (asset == null)
                    return;

                int hotkeyIndex = KitEx.GetHotkeyIndex(b.Slot);
                if (hotkeyIndex == byte.MaxValue)
                    return;

                PlayerInventory inv = Player.UnturnedPlayer.inventory;

                // find new item to bind the item to
                for (int p = PlayerInventory.SLOTS; p < PlayerInventory.STORAGE; ++p)
                {
                    SDG.Unturned.Items page = inv.items[p];
                    int c = page.getItemCount();
                    for (int index = 0; index < c; ++index)
                    {
                        ItemJar jar = page.getItem((byte)index);
                        if (jar.x == jar2.X && jar.y == jar2.Y && p == (int)jar2.Page)
                            continue;

                        if (jar.GetAsset() is not { } asset2 || asset2.GUID != asset.GUID || !KitEx.CanBindHotkeyTo(asset2, (Page)p))
                            continue;

                        Player.UnturnedPlayer.equipment.ServerBindItemHotkey((byte)hotkeyIndex, asset, (byte)p, jar.x, jar.y);
#if DEBUG
                        _logger.LogTrace("Updating dropped hotkey: {0} at {1}, ({2}, {3}).", asset.itemName, (byte)p, jar.x, jar.y);
#endif
                        return;
                    }
                }

                break;
            }
        }
        finally
        {
            Player.PurchaseSync.Release();
        }
    }

    private async Task ApplyHotkeyAfterMovingItemAsync(ItemMoved e, byte origX, byte origY, Page origPage, byte swapOrigX, byte swapOrigY, Page swapOrigPage, CancellationToken token)
    {
        await Player.PurchaseSync.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (HotkeyBindings == null)
                return;

            Kit? kit = await Player.Component<KitPlayerComponent>().GetActiveKitAsync(token).ConfigureAwait(false);
            if (kit is null)
                return;

            await UniTask.SwitchToMainThread(token);

            foreach (HotkeyBinding binding in HotkeyBindings)
            {
                if (binding.Kit != kit.PrimaryKey)
                    continue;

                byte index = KitEx.GetHotkeyIndex(binding.Slot);
                if (index == byte.MaxValue)
                    continue;

                if (binding.Item.X == origX && binding.Item.Y == origY && binding.Item.Page == origPage)
                {
                    ItemAsset? asset = binding.GetAsset(kit, 0/* todo Player.Team */);
                    if (asset != null && KitEx.CanBindHotkeyTo(asset, e.NewPage))
                    {
                        Player.UnturnedPlayer.equipment.ServerBindItemHotkey(index, asset, (byte)e.NewPage, e.NewX, e.NewY);
#if DEBUG
                        _logger.LogTrace("Updating old hotkey: {0} at {1}, ({2}, {3}).", asset.itemName, e.NewPage, e.NewX, e.NewY);
#endif
                    }
                    if (!e.IsSwap)
                        break;
                }
                else if (binding.Item.X == swapOrigX && binding.Item.Y == swapOrigY && binding.Item.Page == swapOrigPage)
                {
                    ItemAsset? asset = binding.GetAsset(kit, 0/* todo Player.Team */);
                    if (asset != null && !KitEx.CanBindHotkeyTo(asset, e.OldPage))
                    {
                        Player.UnturnedPlayer.equipment.ServerBindItemHotkey(index, asset, (byte)e.OldPage, e.OldX, e.OldY);
#if DEBUG
                        _logger.LogTrace("Updating old swap hotkey: {0} at {1}, ({2}, {3}).", asset.itemName, e.OldPage, e.OldX, e.OldY);
#endif
                    }
                }
            }
        }
        finally
        {
            Player.PurchaseSync.Release();
        }
    }

    WarfarePlayer IPlayerComponent.Player { get => Player; set => Player = value; }
}

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models.Items;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Kits.Items;
internal class HotkeyPlayerComponent : IPlayerComponent, IEventListener<ItemDropped>
{
    private KitManager _kitManager = null!;
    private WarfareModule _module = null!;
    public WarfarePlayer Player { get; private set; }

    // used to trace items back to their original position in the kit
    internal List<HotkeyBinding>? HotkeyBindings;

    void IPlayerComponent.Init(IServiceProvider serviceProvider)
    {
        _kitManager = serviceProvider.GetRequiredService<KitManager>();
        _module = serviceProvider.GetRequiredService<WarfareModule>();
    }

    void IEventListener<ItemDropped>.HandleEvent(ItemDropped e, IServiceProvider serviceProvider)
    {
        if (HotkeyBindings is not { Count: > 0 })
            return;

        if (e.Item == null)
            return;

        // move hotkey to a different item of the same type
        Task.Run(async () =>
        {
            try
            {
                await ApplyHotkeyAfterDroppingItemAsync(e);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        });
    }

    private async Task ApplyHotkeyAfterDroppingItemAsync(ItemDropped e)
    {
        IPageKitItem? jar2 = await _kitManager.GetItemFromKit(e.Player, e.OldX, e.OldY, e.Item!, e.OldPage, tokens.Token).ConfigureAwait(false);
        if (jar2 == null || !e.Player.IsOnline)
            return;

        CancellationToken tkn = _module.UnloadToken;
        CombinedTokenSources tokens = tkn.CombineTokensIfNeeded(e.Player.DisconnectToken);

        await e.Player.PurchaseSync.WaitAsync(tokens.Token).ConfigureAwait(false);
        try
        {
            if (!e.Player.IsOnline || e.Player.HotkeyBindings is not { Count: > 0 })
                return;

            Kit? activeKit = await e.Player.GetActiveKit(tokens.Token).ConfigureAwait(false);

            await UCWarfare.ToUpdate(tokens.Token);
            if (!e.Player.IsOnline || e.Player.HotkeyBindings is not { Count: > 0 })
                return;

            for (int i = 0; i < e.Player.HotkeyBindings.Count; ++i)
            {
                HotkeyBinding b = e.Player.HotkeyBindings[i];

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
                    IKitItem ki => ki.GetItem(activeKit, TeamManager.GetFactionSafe(e.Player.GetTeam()), out _, out _),
                    _ => null
                };
                if (asset == null)
                    return;
                int hotkeyIndex = KitEx.GetHotkeyIndex(b.Slot);
                if (hotkeyIndex == byte.MaxValue)
                    return;
                PlayerInventory inv = e.Player.Player.inventory;
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

                        e.Player.Player.equipment.ServerBindItemHotkey((byte)hotkeyIndex, asset, (byte)p, jar.x, jar.y);
                        return;
                    }
                }

                break;
            }
        }
        finally
        {
            tokens.Dispose();
            e.Player.PurchaseSync.Release();
        }
    }

    WarfarePlayer IPlayerComponent.Player { get => Player; set => Player = value; }
}

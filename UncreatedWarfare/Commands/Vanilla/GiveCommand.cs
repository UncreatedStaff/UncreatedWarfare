using DanielWillett.ReflectionTools;
using System;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Commands;

[Command("give", "i", "item"), Priority(1), MetadataFile]
internal sealed class GiveCommand : IExecutableCommand
{
    private readonly AssetRedirectService _assetRedirectService;
    
    private const int MaxItems = 100;

    /// <inheritdoc />
    public required CommandContext Context { get; init; }

    public GiveCommand(AssetRedirectService assetRedirectService)
    {
        _assetRedirectService = assetRedirectService;
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        Context.AssertArgs(1);

        int amount = 1;
        int itemAmt = -1;
        byte[]? itemSt = null;
        string itemName;
        ushort shortID;
        ItemAsset? asset;

        bool amountWasValid = true;

        int similarNamesCount = 0;

        if (Context.MatchParameter(0, "ammo") && Context.Player.UnturnedPlayer.equipment.useable is UseableGun)
        {
            // held item
            ItemGunAsset? gunAsset = null;
            byte[]? state = null;
            if (!Context.Player.UnturnedPlayer.equipment.isTurret)
            {
                ItemJar item = Context.Player.UnturnedPlayer.inventory.items[Context.Player.UnturnedPlayer.equipment.equippedPage].getItem(
                    Context.Player.UnturnedPlayer.inventory.items[Context.Player.UnturnedPlayer.equipment.equippedPage]
                        .getIndex(Context.Player.UnturnedPlayer.equipment.equipped_x, Context.Player.UnturnedPlayer.equipment.equipped_y));
                if (item != null && item.item.GetAsset() is ItemGunAsset gun2)
                {
                    gunAsset = gun2;
                    state = item.item.state;
                }
            }
            // turret
            else
            {
                InteractableVehicle? vehicle = Context.Player.UnturnedPlayer.movement.getVehicle();
                byte seat = Context.Player.UnturnedPlayer.movement.getSeat();
                if (vehicle != null && vehicle.passengers[seat].turret != null)
                {
                    state = vehicle.passengers[seat].state;
                    gunAsset = Context.Player.UnturnedPlayer.equipment.asset as ItemGunAsset;
                }
            }
            if (state != null && gunAsset != null)
            {
                asset = ItemUtility.FindMagazineAsset(gunAsset, state);
                itemSt = Array.Empty<byte>();
                if (asset != null)
                    itemAmt = asset.amount;
                if (Context.TryGet(1, out int amt) && amt is <= MaxItems and > 0)
                    amount = amt;
                else if (Context.HasArgs(2))
                    amountWasValid = false;
                goto foundItem;
            }
        }
        if (Context.HasArgsExact(1)) // if there is only one argument, we only have to check a single word
        {
            if (Context.TryGet(0, out RedirectType type))
            {
                FactionInfo? kitFaction = (await Context.Player.Component<KitPlayerComponent>().GetActiveKitAsync(KitInclude.Base, token))?.Faction;

                await UniTask.SwitchToMainThread(token);
                
                asset = _assetRedirectService.ResolveRedirect(type, string.Empty, kitFaction, Context.Player.Team, out byte[] state, out byte amt);
                if (asset != null)
                {
                    itemAmt = amt;
                    itemSt = state;
                    goto foundItem;
                }
            }
            if (Guid.TryParse(Context.Get(0)!, out Guid guid))
            {
                asset = Assets.find<ItemAsset>(guid);
                if (asset is not null)
                    goto foundItem;
            }
            // first check if single-word argument is a short ID
            if (Context.TryGet(0, out shortID) && Assets.find(EAssetType.ITEM, shortID) is ItemAsset asset2)
            {
                // success
                asset = asset2;
                goto foundItem;
            }
            itemName = Context.Get(0)!;
            asset = AssetUtility.FindAsset<ItemAsset>(itemName, out similarNamesCount, true);
            if (asset == null)
                throw Context.ReplyString($"No item found by the name or ID of '<color=#cca69d>{itemName}</color>'", "8f9494");
        }
        else // there are at least 2 arguments
        {
            // first try treat 1st argument as a short ID, and 2nd argument as an amount
            if (Context.HasArgsExact(2))
            {
                if (!Context.TryGet(1, out amount) || amount > MaxItems || amount < 0)
                {
                    amount = 1;
                    amountWasValid = false;
                }
                if (Context.TryGet(0, out RedirectType type))
                {
                    FactionInfo? kitFaction = (await Context.Player.Component<KitPlayerComponent>().GetActiveKitAsync(KitInclude.Base, token))?.Faction;

                    await UniTask.SwitchToMainThread(token);

                    asset = _assetRedirectService.ResolveRedirect(type, amountWasValid ? string.Empty : (Context.Get(1) ?? string.Empty), kitFaction, Context.Player.Team, out byte[] state, out byte amt);
                    if (asset != null)
                    {
                        itemAmt = amt;
                        itemSt = state;
                        goto foundItem;
                    }
                }
                if (Context.TryGet(0, out shortID) && Assets.find(EAssetType.ITEM, shortID) is ItemAsset asset2)
                {
                    // success
                    asset = asset2;
                    goto foundItem;
                }
                if (Guid.TryParse(Context.Get(0)!, out Guid guid))
                {
                    asset = Assets.find<ItemAsset>(guid);
                    if (asset is not null)
                        goto foundItem;
                }
            }
            // now try find an asset using all arguments except the last, which could be treated as a number.
            if (Context.TryGet(Context.ArgumentCount - 1, out amount) && amount is <= MaxItems and > 0) // if this runs, then the last ID is treated as an amount
            {
                itemName = Context.GetRange(0, Context.ArgumentCount - 1)!;
                if (Enum.TryParse(itemName, true, out RedirectType type))
                {
                    FactionInfo? kitFaction = (await Context.Player.Component<KitPlayerComponent>().GetActiveKitAsync(KitInclude.Base, token))?.Faction;

                    await UniTask.SwitchToMainThread(token);

                    asset = _assetRedirectService.ResolveRedirect(type, Context.GetRange(1, Context.ArgumentCount - 2) ?? string.Empty, kitFaction, Context.Player.Team, out byte[] state, out byte amt);
                    if (asset != null)
                    {
                        itemAmt = amt;
                        itemSt = state;
                        goto foundItem;
                    }
                }
                asset = AssetUtility.FindAsset<ItemAsset>(itemName, out similarNamesCount, true);
                if (asset == null)
                    throw Context.ReplyString($"No item found by the name or ID of '<color=#cca69d>{itemName}</color>'", "8f9494");
            }
            else
            {
                amount = 1;
                itemName = Context.GetRange(0)!;
                if (Enum.TryParse(itemName, true, out RedirectType type))
                {
                    FactionInfo? kitFaction = (await Context.Player.Component<KitPlayerComponent>().GetActiveKitAsync(KitInclude.Base, token))?.Faction;

                    await UniTask.SwitchToMainThread(token);

                    asset = _assetRedirectService.ResolveRedirect(type, Context.GetRange(1) ?? string.Empty, kitFaction, Context.Player.Team, out byte[] state, out byte amt);
                    if (asset != null)
                    {
                        itemAmt = amt;
                        itemSt = state;
                        goto foundItem;
                    }
                }
                asset = AssetUtility.FindAsset<ItemAsset>(itemName.Trim(), out similarNamesCount, true);
                if (asset == null)
                    throw Context.ReplyString($"No item found by the name or ID of '<color=#cca69d>{itemName}</color>'", "8f9494");
            }
        }
        if (asset == null)
            throw Context.ReplyString("No item found.", "8f9494");

        foundItem:

        amount = Math.Min(amount, 250);

        for (int i = 0; i < amount; i++)
        {
            Item itemInstance = new Item(asset!.id, itemAmt is <= 0 or > byte.MaxValue ? asset.amount : (byte)itemAmt, 100, itemSt ?? asset.getState(true));
            if (!Context.Player.UnturnedPlayer.inventory.tryAddItem(itemInstance, true))
                ItemManager.dropItem(itemInstance, Context.Player.Position, true, true, true);
        }

        string message = TranslationFormattingUtility.Colorize($"Giving you {(amount == 1 ? "a" : TranslationFormattingUtility.Colorize(amount.ToString(Context.Culture) + "x", "9dc9f5"))} <color=#ffdf91>{asset.itemName}</color> - <color=#a7b6c4>{asset.id}</color>", "bfb9ac");

        if (!amountWasValid)
            message += TranslationFormattingUtility.Colorize(" (the amount your tried to put was invalid)", "7f8182");
        else if (similarNamesCount > 0)
            message += TranslationFormattingUtility.Colorize($" ({similarNamesCount} similarly named items exist)", "7f8182");

        Context.ReplyString(message);
        Context.LogAction(ActionLogType.GiveItem, $"GAVE {amount}x {AssetLink.Create(asset).ToDisplayString()}");
    }
}
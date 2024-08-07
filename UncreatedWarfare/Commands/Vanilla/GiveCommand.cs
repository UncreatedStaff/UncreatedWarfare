using DanielWillett.ReflectionTools;
using SDG.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Commands;

[Command("give", "i", "item"), Priority(1)]
[MetadataFile(nameof(GetHelpMetadata))]
public class GiveCommand : IExecutableCommand
{
    private const string Syntax = "/i <item ...> [count = 1]";
    private const string Help = "Gives the player [count] amount of an item.";

    private const int MaxItems = 100;

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = "Gives the caller a specificed amount of an item.",
            Parameters =
            [
                new CommandParameter("Item", typeof(ItemAsset), "Ammo"),
                new CommandParameter("Redirect", typeof(RedirectType))
            ]
        };
    }

    /// <inheritdoc />
    public async UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByPlayer();

        Context.AssertArgs(1, Syntax + " - " + Help);
        Context.AssertHelpCheck(0, Syntax + " - " + Help);

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
                ushort oldItem = BitConverter.ToUInt16(state, (int)AttachmentType.Magazine);
                if (Assets.find(EAssetType.ITEM, oldItem) is ItemMagazineAsset magAsset)
                    asset = magAsset;
                else if (Assets.find(EAssetType.ITEM, gunAsset.getMagazineID()) is ItemMagazineAsset magAsset2)
                    asset = magAsset2;
                else
                {
                    List<ItemMagazineAsset> mags = ListPool<ItemMagazineAsset>.claim();
                    try
                    {
                        Assets.find(mags);
                        asset = mags.FirstOrDefault(x => x.calibers.Any(y => gunAsset.magazineCalibers.Contains(y)));
                    }
                    finally
                    {
                        ListPool<ItemMagazineAsset>.release(mags);
                    }
                }
                itemSt = Array.Empty<byte>();
                if (asset != null)
                    itemAmt = asset.amount;
                if (Context.TryGet(1, out int amt) && amount <= MaxItems && amount > 0)
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
                FactionInfo? kitFaction = (await Context.Player.GetActiveKit(token))?.FactionInfo;

                await UniTask.SwitchToMainThread(token);

                asset = TeamManager.GetRedirectInfo(type, string.Empty, kitFaction, Context.Player.Faction, out byte[] state, out byte amt);
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
            asset = UCAssetManager.FindItemAsset(itemName, out similarNamesCount, true);
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
                    FactionInfo? kitFaction = (await Context.Player.GetActiveKit(token))?.FactionInfo;

                    await UniTask.SwitchToMainThread(token);

                    asset = TeamManager.GetRedirectInfo(type, string.Empty, kitFaction, Context.Player.Faction, out byte[] state, out byte amt);
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
            if (Context.TryGet(Context.ArgumentCount - 1, out amount) && amount <= MaxItems && amount > 0) // if this runs, then the last ID is treated as an amount
            {
                itemName = Context.GetRange(0, Context.ArgumentCount - 1)!;
                if (Enum.TryParse(itemName, true, out RedirectType type))
                {
                    FactionInfo? kitFaction = (await Context.Player.GetActiveKit(token))?.FactionInfo;

                    await UniTask.SwitchToMainThread(token);

                    asset = TeamManager.GetRedirectInfo(type, string.Empty, kitFaction, Context.Player.Faction, out byte[] state, out byte amt);
                    if (asset != null)
                    {
                        itemAmt = amt;
                        itemSt = state;
                        goto foundItem;
                    }
                }
                asset = UCAssetManager.FindItemAsset(itemName, out similarNamesCount, true);
                if (asset == null)
                    throw Context.ReplyString($"No item found by the name or ID of '<color=#cca69d>{itemName}</color>'", "8f9494");
            }
            else
            {
                amount = 1;
                itemName = Context.GetRange(0)!;
                if (Enum.TryParse(itemName, true, out RedirectType type))
                {
                    FactionInfo? kitFaction = (await Context.Player.GetActiveKit(token))?.FactionInfo;

                    await UniTask.SwitchToMainThread(token);

                    asset = TeamManager.GetRedirectInfo(type, string.Empty, kitFaction, Context.Player.Faction, out byte[] state, out byte amt);
                    if (asset != null)
                    {
                        itemAmt = amt;
                        itemSt = state;
                        goto foundItem;
                    }
                }
                asset = UCAssetManager.FindItemAsset(itemName.Trim(), out similarNamesCount, true);
                if (asset == null)
                    throw Context.ReplyString($"No item found by the name or ID of '<color=#cca69d>{itemName}</color>'", "8f9494");
            }
        }
        if (asset == null)
            throw Context.ReplyString("No item found.", "8f9494");

        foundItem:

        amount = Math.Min(amount, 250);

        Item itemFromID = new Item(asset!.id, itemAmt is <= 0 or > byte.MaxValue ? asset.amount : (byte)itemAmt, 100, itemSt ?? asset.getState(true));
        for (int i = 0; i < amount; i++)
        {
            if (!Context.Player.UnturnedPlayer.inventory.tryAddItem(itemFromID, true))
                ItemManager.dropItem(itemFromID, Context.Player.Position, true, true, true);
        }

        string message = $"Giving you {(amount == 1 ? "a" : (amount.ToString(Data.LocalLocale) + "x").Colorize("9dc9f5"))} <color=#ffdf91>{asset.itemName}</color> - <color=#a7b6c4>{asset.id}</color>".Colorize("bfb9ac");

        if (!amountWasValid)
            message += " (the amount your tried to put was invalid)".Colorize("7f8182");
        else if (similarNamesCount > 0)
            message += $" ({similarNamesCount} similarly named items exist)".Colorize("7f8182");

        Context.ReplyString(message);
        Context.LogAction(ActionLogType.GiveItem, $"GAVE {amount}x {asset.ActionLogDisplay()}");
    }
}

using SDG.Framework.Utilities;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Commands.VanillaRework;
// ReSharper disable once InconsistentNaming
public class ICommand : AsyncCommand
{
    private const string Syntax = "/i <item ...> [count = 1]";
    private const string Help = "Gives the player [count] amount of an item.";

    private const int MaxItems = 100;

    public ICommand() : base("give", EAdminType.MODERATOR, 1)
    {
        AddAlias("i");
        AddAlias("item");
        Structure = new CommandStructure
        {
            Description = "Gives the caller a specificed amount of an item.",
            Parameters = new CommandParameter[]
            {
                new CommandParameter("Item", typeof(ItemAsset), "Ammo"),
                new CommandParameter("Redirect", typeof(RedirectType))
            }
        };
    }
    public override async Task Execute(CommandInteraction ctx, CancellationToken token)
    {
        ctx.AssertRanByPlayer();

        ctx.AssertArgs(1, Syntax + " - " + Help);
        ctx.AssertHelpCheck(0, Syntax + " - " + Help);

        int amount = 1;
        int itemAmt = -1;
        byte[]? itemSt = null;
        string itemName;
        ushort shortID;
        ItemAsset? asset;

        bool amountWasValid = true;

        int similarNamesCount = 0;

        if (ctx.MatchParameter(0, "ammo") && ctx.Caller.Player.equipment.useable is UseableGun)
        {
            // held item
            ItemGunAsset? gunAsset = null;
            byte[]? state = null;
            if (!ctx.Caller.Player.equipment.isTurret)
            {
                ItemJar item = ctx.Caller.Player.inventory.items[ctx.Caller.Player.equipment.equippedPage].getItem(
                    ctx.Caller.Player.inventory.items[ctx.Caller.Player.equipment.equippedPage]
                        .getIndex(ctx.Caller.Player.equipment.equipped_x, ctx.Caller.Player.equipment.equipped_y));
                if (item != null && item.item.GetAsset() is ItemGunAsset gun2)
                {
                    gunAsset = gun2;
                    state = item.item.state;
                }
            }
            // turret
            else
            {
                InteractableVehicle? vehicle = ctx.Caller.Player.movement.getVehicle();
                byte seat = ctx.Caller.Player.movement.getSeat();
                if (vehicle != null && vehicle.passengers[seat].turret != null)
                {
                    state = vehicle.passengers[seat].state;
                    gunAsset = ctx.Caller.Player.equipment.asset as ItemGunAsset;
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
                if (ctx.TryGet(1, out int amt) && amount <= MaxItems && amount > 0)
                    amount = amt;
                else if (ctx.HasArg(1))
                    amountWasValid = false;
                goto foundItem;
            }
        }
        if (ctx.HasArgsExact(1)) // if there is only one argument, we only have to check a single word
        {
            if (ctx.TryGet(0, out RedirectType type))
            {
                FactionInfo? kitFaction = (await ctx.Caller.GetActiveKit(token))?.FactionInfo;

                await UCWarfare.ToUpdate(token);

                asset = TeamManager.GetRedirectInfo(type, string.Empty, kitFaction, ctx.Caller.Faction, out byte[] state, out byte amt);
                if (asset != null)
                {
                    itemAmt = amt;
                    itemSt = state;
                    goto foundItem;
                }
            }
            if (Guid.TryParse(ctx.Get(0)!, out Guid guid))
            {
                asset = Assets.find<ItemAsset>(guid);
                if (asset is not null)
                    goto foundItem;
            }
            // first check if single-word argument is a short ID
            if (ctx.TryGet(0, out shortID) && Assets.find(EAssetType.ITEM, shortID) is ItemAsset asset2)
            {
                // success
                asset = asset2;
                goto foundItem;
            }
            itemName = ctx.Get(0)!;
            asset = UCAssetManager.FindItemAsset(itemName, out similarNamesCount, true);
            if (asset == null)
                throw ctx.ReplyString($"No item found by the name or ID of '<color=#cca69d>{itemName}</color>'", "8f9494");
        }
        else // there are at least 2 arguments
        {
            // first try treat 1st argument as a short ID, and 2nd argument as an amount
            if (ctx.HasArgsExact(2))
            {
                if (!ctx.TryGet(1, out amount) || amount > MaxItems || amount < 0)
                {
                    amount = 1;
                    amountWasValid = false;
                }
                if (ctx.TryGet(0, out RedirectType type))
                {
                    FactionInfo? kitFaction = (await ctx.Caller.GetActiveKit(token))?.FactionInfo;

                    await UCWarfare.ToUpdate(token);

                    asset = TeamManager.GetRedirectInfo(type, string.Empty, kitFaction, ctx.Caller.Faction, out byte[] state, out byte amt);
                    if (asset != null)
                    {
                        itemAmt = amt;
                        itemSt = state;
                        goto foundItem;
                    }
                }
                if (ctx.TryGet(0, out shortID) && Assets.find(EAssetType.ITEM, shortID) is ItemAsset asset2)
                {
                    // success
                    asset = asset2;
                    goto foundItem;
                }
                if (Guid.TryParse(ctx.Get(0)!, out Guid guid))
                {
                    asset = Assets.find<ItemAsset>(guid);
                    if (asset is not null)
                        goto foundItem;
                }
            }
            // now try find an asset using all arguments except the last, which could be treated as a number.
            if (ctx.TryGet(ctx.ArgumentCount - 1, out amount) && amount <= MaxItems && amount > 0) // if this runs, then the last ID is treated as an amount
            {
                itemName = ctx.GetRange(0, ctx.ArgumentCount - 1)!;
                if (Enum.TryParse(itemName, true, out RedirectType type))
                {
                    FactionInfo? kitFaction = (await ctx.Caller.GetActiveKit(token))?.FactionInfo;

                    await UCWarfare.ToUpdate(token);

                    asset = TeamManager.GetRedirectInfo(type, string.Empty, kitFaction, ctx.Caller.Faction, out byte[] state, out byte amt);
                    if (asset != null)
                    {
                        itemAmt = amt;
                        itemSt = state;
                        goto foundItem;
                    }
                }
                asset = UCAssetManager.FindItemAsset(itemName, out similarNamesCount, true);
                if (asset == null)
                    throw ctx.ReplyString($"No item found by the name or ID of '<color=#cca69d>{itemName}</color>'", "8f9494");
            }
            else
            {
                amount = 1;
                itemName = ctx.GetRange(0)!;
                if (Enum.TryParse(itemName, true, out RedirectType type))
                {
                    FactionInfo? kitFaction = (await ctx.Caller.GetActiveKit(token))?.FactionInfo;

                    await UCWarfare.ToUpdate(token);

                    asset = TeamManager.GetRedirectInfo(type, string.Empty, kitFaction, ctx.Caller.Faction, out byte[] state, out byte amt);
                    if (asset != null)
                    {
                        itemAmt = amt;
                        itemSt = state;
                        goto foundItem;
                    }
                }
                asset = UCAssetManager.FindItemAsset(itemName.Trim(), out similarNamesCount, true);
                if (asset == null)
                    throw ctx.ReplyString($"No item found by the name or ID of '<color=#cca69d>{itemName}</color>'", "8f9494");
            }
        }
        if (asset == null)
            throw ctx.ReplyString("No item found.", "8f9494");

        foundItem:

        amount = Math.Min(amount, 250);

        Item itemFromID = new Item(asset!.id, itemAmt is <= 0 or > byte.MaxValue ? asset.amount : (byte)itemAmt, 100, itemSt ?? asset.getState(true));
        for (int i = 0; i < amount; i++)
        {
            if (!ctx.Caller.Player.inventory.tryAddItem(itemFromID, true))
                ItemManager.dropItem(itemFromID, ctx.Caller.Position, true, true, true);
        }

        string message = $"Giving you {(amount == 1 ? "a" : (amount.ToString(Data.LocalLocale) + "x").Colorize("9dc9f5"))} <color=#ffdf91>{asset.itemName}</color> - <color=#a7b6c4>{asset.id}</color>".Colorize("bfb9ac");

        if (!amountWasValid)
            message += " (the amount your tried to put was invalid)".Colorize("7f8182");
        else if (similarNamesCount > 0)
            message += $" ({similarNamesCount} similarly named items exist)".Colorize("7f8182");

        ctx.ReplyString(message);
        ctx.LogAction(ActionLogType.GiveItem, $"GAVE {amount}x {asset.ActionLogDisplay()}");
    }
}

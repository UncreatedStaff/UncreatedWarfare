using SDG.Unturned;
using System;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands.VanillaRework;
public class ICommand : Command
{
    private const string SYNTAX = "/i <item ...> [count = 1]";
    private const string HELP = "Gives the player [count] amount of an item.";
    private const int MAX_ITEMS = 100;

    public ICommand() : base("give", EAdminType.MODERATOR)
    {
        AddAlias("i");
        AddAlias("item");
    }

    public override void Execute(CommandInteraction ctx)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ctx.AssertRanByPlayer();

        ctx.AssertArgs(1, SYNTAX + " - " + HELP);
        ctx.AssertHelpCheck(1, SYNTAX + " - " + HELP);

        int amount = 1;
        string itemName = "";
        ushort shortID;
        ItemAsset? asset = null;

        bool amountWasValid = true;

        int similarNamesCount = 0;

        if (ctx.HasArgsExact(1)) // if there is only one argument, we only have to check a single word
        {
            // first check if single-word argument is a short ID
            if (ctx.TryGet(0, out shortID) && Assets.find(EAssetType.ITEM, shortID) is ItemAsset asset2)
            {
                // success
                asset = asset2;
                goto foundItem;
            }
            else
            {
                itemName = ctx.Get(0)!;
                asset = UCAssetManager.FindItemAsset(itemName, out similarNamesCount, true);
                if (asset != null)
                {
                    itemName = asset.itemName;
                    // success
                    goto foundItem;
                }
                else throw ctx.Reply($"No item found by the name or ID of '<color=#cca69d>{itemName}</color>'".Colorize("8f9494"));
            }
        }
        else // there are at least 2 arguments
        {
            // first try treat 1st argument as a short ID, and 2nd argument as an amount
            if (ctx.HasArgsExact(2) && ctx.TryGet(0, out shortID) && Assets.find(EAssetType.ITEM, shortID) is ItemAsset asset2)
            {
                if (!ctx.TryGet(1, out amount) || amount > MAX_ITEMS || amount < 0)
                {
                    amount = 1;
                    amountWasValid = false;
                }

                // success
                asset = asset2;
                goto foundItem;
            }
            else
            {
                // now try find an asset using all arguments except the last, which could be treated as a number.

                if (ctx.TryGet(ctx.ArgumentCount - 1, out amount) && amount <= MAX_ITEMS && amount > 0) // if this runs, then the last ID is treated as an amount
                {
                    itemName = ctx.GetRange(0, ctx.ArgumentCount - 1)!;

                    asset = UCAssetManager.FindItemAsset(itemName, out similarNamesCount, true);
                    if (asset != null)
                    {
                        // success
                        goto foundItem;
                    }
                    else throw ctx.Reply($"No item found by the name or ID of '<color=#cca69d>{itemName}</color>'".Colorize("8f9494"));
                }
                else
                {
                    amount = 1;
                    itemName = ctx.GetRange(0)!;

                    asset = UCAssetManager.FindItemAsset(itemName.Trim(), out similarNamesCount, true);
                    if (asset != null)
                    {
                        // success
                        goto foundItem;
                    }
                    else throw ctx.Reply($"No item found by the name or ID of '<color=#cca69d>{itemName}</color>'".Colorize("8f9494"));
                }
            }
        }

    foundItem:
        if (asset == null) return;
        Item itemFromID = new Item(asset.id, true);
        for (int i = 0; i < amount; i++)
        {
            if (!ctx.Caller.Player.inventory.tryAddItem(itemFromID, true))
                ItemManager.dropItem(itemFromID, ctx.Caller.Position, true, true, true);
        }

        string message = $"Giving you {(amount == 1 ? "a" : (amount.ToString() + "x").Colorize("9dc9f5"))} <color=#ffdf91>{asset.itemName}</color> - <color=#a7b6c4>{asset.id}</color>".Colorize("bfb9ac");

        if (!amountWasValid)
            message += $" (the amount your tried to put was invalid)".Colorize("7f8182");
        else if (similarNamesCount > 0)
            message += $" ({similarNamesCount} similarly named items exist)".Colorize("7f8182");

        ctx.Reply(message);
        ctx.LogAction(EActionLogType.GIVE_ITEM, $"GAVE {amount}x {asset.itemName} / {asset.id} / {asset.GUID}");
    }
}

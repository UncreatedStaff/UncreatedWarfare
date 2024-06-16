using SDG.Unturned;
using System;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public class WhitelistCommand : Command
{
    private const string SYNTAX = "/whitelist <add|remove|set amount>";
    private const string SET_AMOUNT_SYNTAX = "/whitelist set amount <item> <amount>";
    private const string HELP = "Add or remove items from the global whitelist.";

    public WhitelistCommand() : base("whitelist", EAdminType.STAFF)
    {
        AddAlias("wh");
        Structure = new CommandStructure
        {
            Description = "Add or remove items from the global whitelist.",
            Parameters = new CommandParameter[]
            {
                new CommandParameter("Add")
                {
                    Aliases = new string[] { "whitelist", "create" },
                    Description = "Add an item to the global whitelist.",
                    ChainDisplayCount = 3,
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Item", typeof(ItemAsset))
                        {
                            Parameters = new CommandParameter[]
                            {
                                new CommandParameter("Amount", typeof(int))
                                {
                                    IsOptional = true
                                }
                            }
                        }
                    }
                },
                new CommandParameter("Remove")
                {
                    Aliases = new string[] { "delete", "rem" },
                    Description = "Remove an item from the global whitelist.",
                    ChainDisplayCount = 2,
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Item", typeof(ItemAsset))
                        {
                            IsRemainder = true
                        }
                    }
                },
                new CommandParameter("Set")
                {
                    Description = "Set the whitelist amount for an item.",
                    ChainDisplayCount = 3,
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Amount")
                        {
                            Aliases = new string[] { "maxamount", "amt" },
                            IsRemainder = true,
                            Parameters = new CommandParameter[]
                            {
                                new CommandParameter("Amount", typeof(byte))
                            }
                        }
                    }
                }
            }
        };
    }

    public override void Execute(CommandInteraction ctx)
    {
        if (!Data.Gamemode.UseWhitelist || !Whitelister.Loaded) throw ctx.SendGamemodeError();

        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);

        ctx.AssertArgs(2, SYNTAX);

        if (ctx.MatchParameter(0, "add", "whitelist", "create"))
        {
            ctx.AssertHelpCheck(1, "/whitelist add <item> [amount]");
            if (!ctx.TryGet(ctx.ArgumentCount - 1, out byte amount))
                amount = 255;
            if (ctx.TryGet(1, out ItemAsset asset, out bool multiple, amount == 255, ctx.ArgumentCount - (amount == 255 ? 1 : 2), false))
            {
                if (!Whitelister.IsWhitelisted(asset.GUID, out _))
                {
                    Whitelister.AddItem(asset.GUID, amount);
                    ctx.LogAction(ActionLogType.AddWhitelist, $"{asset.itemName} / {asset.id} / {asset.GUID:N}");
                    if (amount != 255)
                        ctx.LogAction(ActionLogType.SetWhitelistMaxAmount, $"{asset.itemName} / {asset.id} / {asset.GUID:N} set to {amount}");
                    ctx.Reply(T.WhitelistAdded, asset);
                }
                else
                    throw ctx.Reply(T.WhitelistAlreadyAdded, asset);
            }
            else if (multiple)
                throw ctx.Reply(T.WhitelistMultipleResults, ctx.Get(1)!);
            else
                throw ctx.Reply(T.WhitelistItemNotID, ctx.Get(1)!);
        }
        else if (ctx.MatchParameter(0, "remove", "delete", "rem"))
        {
            ctx.AssertHelpCheck(1, "/whitelist remove <item>");
            if (ctx.TryGet(1, out ItemAsset asset, out bool multiple, true, selector: x => Whitelister.IsWhitelistedFast(x.GUID)))
            {
                Whitelister.RemoveItem(asset.GUID);
                ctx.LogAction(ActionLogType.RemoveWhitelist, $"{asset.itemName} / {asset.id} / {asset.GUID:N}");
                ctx.Reply(T.WhitelistRemoved, asset);
            }
            else if (multiple)
                throw ctx.Reply(T.WhitelistMultipleResults, ctx.Get(1)!);
            else
                throw ctx.Reply(T.WhitelistItemNotID, ctx.Get(1)!);
        }
        else if (ctx.MatchParameter(0, "set"))
        {
            ctx.AssertArgs(4, SET_AMOUNT_SYNTAX);
            ctx.AssertHelpCheck(1, SET_AMOUNT_SYNTAX);

            if (!ctx.TryGet(ctx.ArgumentCount - 1, out byte amount))
                throw ctx.SendCorrectUsage(SET_AMOUNT_SYNTAX);

            if (ctx.MatchParameter(1, "maxamount", "amount", "amt"))
            {
                ctx.AssertHelpCheck(2, SET_AMOUNT_SYNTAX);

                if (ctx.TryGet(2, out ItemAsset asset, out bool multiple, false, ctx.ArgumentCount - 2, false))
                {
                    if (!Whitelister.IsWhitelisted(asset.GUID, out _))
                    {
                        Whitelister.AddItem(asset.GUID, amount);
                        ctx.LogAction(ActionLogType.AddWhitelist, $"{asset.itemName} / {asset.id} / {asset.GUID:N}");
                        if (amount != 255)
                            ctx.LogAction(ActionLogType.SetWhitelistMaxAmount, $"{asset.itemName} / {asset.id} / {asset.GUID:N} set to {amount}");
                        ctx.Reply(T.WhitelistAdded, asset);
                        ctx.Reply(T.WhitelistSetAmount, asset, amount);
                    }
                    else
                    {
                        if (amount != 255)
                            ctx.LogAction(ActionLogType.SetWhitelistMaxAmount, $"{asset.itemName} / {asset.id} / {asset.GUID:N} set to {amount}");
                        Whitelister.SetAmount(asset.GUID, amount);
                        ctx.Reply(T.WhitelistSetAmount, asset, amount);
                    }
                }
                else if (multiple)
                    throw ctx.Reply(T.WhitelistMultipleResults, ctx.Get(1)!);
                else
                    throw ctx.Reply(T.WhitelistItemNotID, ctx.Get(1)!);
            }
        }
        else throw ctx.SendCorrectUsage(SYNTAX);
    }
}
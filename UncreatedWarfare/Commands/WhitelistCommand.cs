using SDG.Unturned;
using System;
using System.Threading.Tasks;
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
    }

    public override void Execute(CommandInteraction ctx)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
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
                    ctx.LogAction(EActionLogType.ADD_WHITELIST, $"{asset.itemName} / {asset.id} / {asset.GUID:N}");
                    if (amount != 255)
                        ctx.LogAction(EActionLogType.SET_WHITELIST_MAX_AMOUNT, $"{asset.itemName} / {asset.id} / {asset.GUID:N} set to {amount}");
                    ctx.Reply("whitelist_added", asset.itemName);
                }
                else
                    throw ctx.Reply("whitelist_e_exist", ctx.Get(1)!);
            }
            else if (multiple)
                throw ctx.Reply("whitelist_e_multiple_results");
            else
                throw ctx.Reply("whitelist_e_item_not_found");
        }
        else if (ctx.MatchParameter(1, "remove", "delete", "rem"))
        {
            ctx.AssertHelpCheck(1, "/whitelist remove <item>");
            if (ctx.TryGet(1, out ItemAsset asset, out bool multiple, true, selector: x => Whitelister.IsWhitelistedFast(x.GUID)))
            {
                Whitelister.RemoveItem(asset.GUID);
                ctx.LogAction(EActionLogType.REMOVE_WHITELIST, $"{asset.itemName} / {asset.id} / {asset.GUID:N}");
                ctx.Reply("whitelist_removed", ctx.Get(1)!);
            }
            else if (multiple)
                throw ctx.Reply("whitelist_e_multiple_results");
            else
                throw ctx.Reply("whitelist_e_noexist", ctx.Get(1)!);
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
                        ctx.LogAction(EActionLogType.ADD_WHITELIST, $"{asset.itemName} / {asset.id} / {asset.GUID:N}");
                        if (amount != 255)
                            ctx.LogAction(EActionLogType.SET_WHITELIST_MAX_AMOUNT, $"{asset.itemName} / {asset.id} / {asset.GUID:N} set to {amount}");
                        ctx.Reply("whitelist_added", asset.itemName);
                    }
                    else
                        throw ctx.Reply("whitelist_e_exist", ctx.Get(2)!);
                }
                else if (multiple)
                    throw ctx.Reply("whitelist_e_multiple_results");
                else
                    throw ctx.Reply("whitelist_e_item_not_found");
            }
        }
        else throw ctx.SendCorrectUsage(SYNTAX);
    }
}
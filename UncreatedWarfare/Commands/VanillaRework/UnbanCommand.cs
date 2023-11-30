using SDG.Unturned;
using Steamworks;
using System;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Players;
using Uncreated.Warfare.Commands.CommandSystem;

namespace Uncreated.Warfare.Commands.VanillaRework;

public class UnbanCommand : AsyncCommand
{
    private const string Syntax = "/unban <player>";
    private const string Help = "Unban players who have served their time.";

    public UnbanCommand() : base("unban", Framework.EAdminType.MODERATOR, 1)
    {
        Structure = new CommandStructure
        {
            Description = "Unban players who have served their time.",
            Parameters = new CommandParameter[]
            {
                new CommandParameter("Player", typeof(IPlayer))
            }
        };
    }
    public override Task Execute(CommandInteraction ctx, CancellationToken token)
    {
        throw ctx.SendNotImplemented();
#if false
        ctx.AssertHelpCheck(0, Syntax + " - " + Help);

        if (!ctx.HasArgs(1))
            throw ctx.SendCorrectUsage(Syntax);

        if (!ctx.TryGet(0, out ulong targetId, out UCPlayer? target))
            throw ctx.Reply(T.PlayerNotFound);

        PlayerNames targetNames = await F.GetPlayerOriginalNamesAsync(targetId, token);
        await UCWarfare.ToUpdate(token);
        if (target is not null || !Provider.requestUnbanPlayer(ctx.CallerCSteamID, new CSteamID(targetId)))
        {
            ctx.Reply(T.UnbanNotBanned, targetNames);
            return;
        }

        OffenseManager.LogUnbanPlayer(targetId, ctx.CallerID, DateTime.Now);

        string tid = targetId.ToString(Data.AdminLocale);
        ActionLog.Add(ActionLogType.UnbanPlayer, $"UNBANNED {tid}", ctx.CallerID);
        if (ctx.IsConsole)
        {
            L.Log($"{targetNames.PlayerName} ({tid}) was successfully unbanned.", ConsoleColor.Cyan);
            Chat.Broadcast(T.UnbanSuccessBroadcastOperator, targetNames);
        }
        else
        {
            L.Log($"{targetNames.PlayerName} ({tid}) was unbanned by {ctx.Caller.Name.PlayerName} ({ctx.CallerID.ToString(Data.AdminLocale)}).", ConsoleColor.Cyan);
            ctx.Reply(T.UnbanSuccessFeedback, targetNames);
            Chat.Broadcast(LanguageSet.AllBut(ctx.CallerID), T.UnbanSuccessBroadcast, targetNames, ctx.Caller);
        }
#endif
    }
}
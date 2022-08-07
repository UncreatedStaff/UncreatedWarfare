using SDG.Unturned;
using Steamworks;
using System;
using Uncreated.Players;
using Uncreated.Warfare.Commands.CommandSystem;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands.VanillaRework;

public class UnbanCommand : Command
{
    private const string SYNTAX = "/unban <player>";
    private const string HELP = "Unban players who have served their time.";
    public UnbanCommand() : base("unban", Framework.EAdminType.MODERATOR, 1) { }
    public override void Execute(CommandInteraction ctx)
    {
        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);

        if (!ctx.HasArgs(1))
            throw ctx.SendCorrectUsage(SYNTAX);

        if (!ctx.TryGet(0, out ulong targetId, out UCPlayer? target))
            throw ctx.Reply(T.PlayerNotFound);

        FPlayerName targetNames = F.GetPlayerOriginalNames(targetId);
        if (target is not null || !Provider.requestUnbanPlayer(ctx.CallerCSteamID, new CSteamID(targetId)))
        {
            ctx.Reply(T.UnbanNotBanned, targetNames);
            return;
        }

        OffenseManager.LogUnbanPlayer(targetId, ctx.CallerID, DateTime.Now);

        string tid = targetId.ToString(Data.Locale);
        ActionLogger.Add(EActionLogType.UNBAN_PLAYER, $"UNBANNED {tid}", ctx.CallerID);
        if (ctx.IsConsole)
        {
            L.Log($"{targetNames.PlayerName} ({tid.ToString(Data.Locale)}) was successfully unbanned.", ConsoleColor.Cyan);
            Chat.Broadcast(T.UnbanSuccessBroadcastOperator, targetNames);
        }
        else
        {
            FPlayerName callerNames = F.GetPlayerOriginalNames(ctx.CallerID);
            L.Log($"{targetNames.PlayerName} ({tid}) was unbanned by {callerNames.PlayerName} ({ctx.CallerID.ToString(Data.Locale)}).", ConsoleColor.Cyan);
            ctx.Reply(T.UnbanSuccessFeedback, targetNames);
            Chat.Broadcast(LanguageSet.AllBut(ctx.CallerID), T.UnbanSuccessBroadcast, targetNames, ctx.Caller);
        }
    }
}
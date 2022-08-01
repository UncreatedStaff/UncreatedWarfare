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
        {
            ctx.Reply("unban_syntax");
        }
        else if (!ctx.TryGet(0, out ulong targetId, out UCPlayer? target))
        {
            ctx.Reply("unban_no_player_found", ctx.Parameters[0]);
        }
        else
        {
            FPlayerName targetNames = F.GetPlayerOriginalNames(targetId);
            if (target is not null || !Provider.requestUnbanPlayer(ctx.CallerCSteamID, new CSteamID(targetId)))
            {
                ctx.Reply("unban_player_not_banned_console", ctx.IsConsole ? targetNames.PlayerName : targetNames.CharacterName);
                return;
            }

            OffenseManager.LogUnbanPlayer(targetId, ctx.CallerID, DateTime.Now);

            string tid = targetId.ToString(Data.Locale);
            ActionLogger.Add(EActionLogType.UNBAN_PLAYER, $"UNBANNED {tid}", ctx.CallerID);
            if (ctx.IsConsole)
            {
                if (tid.Equals(targetNames.PlayerName, StringComparison.Ordinal))
                {
                    L.Log(Localization.Translate("unban_unbanned_console_id_operator", 0, out _, tid), ConsoleColor.Cyan);
                    Chat.Broadcast("unban_unbanned_broadcast_id_operator", tid);
                }
                else
                {
                    L.Log(Localization.Translate("unban_unbanned_console_name_operator", 0, out _, targetNames.PlayerName, tid.ToString(Data.Locale)), ConsoleColor.Cyan);
                    Chat.Broadcast("unban_unbanned_broadcast_name_operator", targetNames.CharacterName);
                }
            }
            else
            {
                FPlayerName callerNames = F.GetPlayerOriginalNames(ctx.CallerID);
                if (tid.Equals(targetNames.PlayerName, StringComparison.Ordinal))
                {
                    L.Log(Localization.Translate("unban_unbanned_console_id", 0, out _, tid, callerNames.PlayerName, ctx.CallerID.ToString(Data.Locale)), ConsoleColor.Cyan);
                    ctx.Reply("unban_unbanned_feedback_id", tid);
                    Chat.BroadcastToAllExcept(ctx.CallerID, "unban_unbanned_broadcast_id", tid, callerNames.CharacterName);
                }
                else
                {
                    L.Log(Localization.Translate("unban_unbanned_console_name", 0, out _, targetNames.PlayerName, tid, callerNames.PlayerName, ctx.CallerID.ToString(Data.Locale)), ConsoleColor.Cyan);
                    ctx.Reply("unban_unbanned_feedback_name", targetNames.CharacterName);
                    Chat.BroadcastToAllExcept(ctx.CallerID, "unban_unbanned_broadcast_name", targetNames.CharacterName, callerNames.CharacterName);
                }
            }
        }
    }
}
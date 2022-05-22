using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using Uncreated.Networking;
using Uncreated.Players;
using Uncreated.Warfare.Networking;

namespace Uncreated.Warfare.Commands;

public class UnbanOverrideCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller => AllowedCaller.Both;
    public string Name => "unban";
    public string Help => "Unban players who have served their time.";
    public string Syntax => "/unban <player ID>";
    private readonly List<string> _aliases = new List<string>(0);
    public List<string> Aliases => _aliases;
    private readonly List<string> _permissions = new List<string>(1) { "uc.unban" };
	public List<string> Permissions => _permissions;
    public void Execute(IRocketPlayer caller, string[] command)
    {
        CommandContext ctx = new CommandContext(caller, command);
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
            if (UCWarfare.Config.AdminLoggerSettings.LogUnBans)
            {
                Data.DatabaseManager.AddUnban(targetId, ctx.CallerID);
                OffenseManager.NetCalls.SendPlayerUnbanned.NetInvoke(targetId, ctx.CallerID, DateTime.Now);
            }

            string tid = targetId.ToString(Data.Locale);
            ActionLog.Add(EActionLogType.UNBAN_PLAYER, $"UNBANNED {tid}", ctx.CallerID);
            if (ctx.IsConsole)
            {
                if (tid.Equals(targetNames.PlayerName, StringComparison.Ordinal))
                {
                    L.Log(Translation.Translate("unban_unbanned_console_id_operator", 0, out _, tid), ConsoleColor.Cyan);
                    Chat.Broadcast("unban_unbanned_broadcast_id_operator", tid);
                }
                else
                {
                    L.Log(Translation.Translate("unban_unbanned_console_name_operator", 0, out _, targetNames.PlayerName, tid.ToString(Data.Locale)), ConsoleColor.Cyan);
                    Chat.Broadcast("unban_unbanned_broadcast_name_operator", targetNames.CharacterName);
                }
            }
            else
            {
                FPlayerName callerNames = F.GetPlayerOriginalNames(ctx.CallerID);
                if (tid.Equals(targetNames.PlayerName, StringComparison.Ordinal))
                {
                    L.Log(Translation.Translate("unban_unbanned_console_id", 0, out _, tid, callerNames.PlayerName, ctx.CallerID.ToString(Data.Locale)), ConsoleColor.Cyan);
                    ctx.Reply("unban_unbanned_feedback_id", tid);
                    Chat.BroadcastToAllExcept(ctx.CallerID, "unban_unbanned_broadcast_id", tid, callerNames.CharacterName);
                }
                else
                {
                    L.Log(Translation.Translate("unban_unbanned_console_name", 0, out _, targetNames.PlayerName, tid, callerNames.PlayerName, ctx.CallerID.ToString(Data.Locale)), ConsoleColor.Cyan);
                    ctx.Reply("unban_unbanned_feedback_name", targetNames.CharacterName);
                    Chat.BroadcastToAllExcept(ctx.CallerID, "unban_unbanned_broadcast_name", targetNames.CharacterName, callerNames.CharacterName);
                }
            }
        }
    }
}
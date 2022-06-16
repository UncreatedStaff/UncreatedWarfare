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

public class KickOverrideCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller => AllowedCaller.Both;
    public string Name => "kick";
    public string Help => "Kick players who are misbehaving.";
    public string Syntax => "/kick <player> <reason>";
    private readonly List<string> _aliases = new List<string>(0);
    public List<string> Aliases => _aliases;
    private readonly List<string> _permissions = new List<string>(1) { "uc.kick" };
	public List<string> Permissions => _permissions;
    public void Execute(IRocketPlayer caller, string[] command)
    {
        UCCommandContext ctx = new UCCommandContext(caller, command);
        if (!ctx.HasArgs(2))
        {
            ctx.Reply("kick_syntax");
        }
        else if (!ctx.TryGet(0, out ulong targetId, out UCPlayer? target) || target is null)
        {
            ctx.Reply("kick_no_player_found", ctx.Parameters[0]);
        }
        else
        {
            string? reason = ctx.GetRange(1);
            if (string.IsNullOrEmpty(reason))
            {
                ctx.Reply("kick_no_reason_provided", ctx.Parameters[1]);
            }
            else
            {
                FPlayerName names = F.GetPlayerOriginalNames(target);
                Provider.kick(target.Player.channel.owner.playerID.steamID, reason!);
                if (UCWarfare.Config.AdminLoggerSettings.LogKicks)
                {
                    OffenseManager.NetCalls.SendPlayerKicked.NetInvoke(targetId, ctx.CallerID, reason!, DateTime.Now);
                    Data.DatabaseManager.AddKick(targetId, ctx.CallerID, reason!);
                }
                ActionLog.Add(EActionLogType.KICK_PLAYER, $"KICKED {targetId.ToString(Data.Locale)} FOR \"{reason}\"", ctx.CallerID);
                if (ctx.IsConsole)
                {
                    L.Log(Translation.Translate("kick_kicked_console_operator", 0, out _, names.PlayerName, targetId.ToString(Data.Locale), reason!), ConsoleColor.Cyan);
                    Chat.Broadcast("kick_kicked_broadcast_operator", names.CharacterName);
                }
                else
                {
                    FPlayerName callerNames = ctx.Caller is null ? FPlayerName.Console : F.GetPlayerOriginalNames(ctx.Caller);
                    L.Log(Translation.Translate("kick_kicked_console", 0, out _, names.PlayerName, targetId.ToString(Data.Locale),
                        callerNames.PlayerName, ctx.CallerID.ToString(Data.Locale), reason!), ConsoleColor.Cyan);
                    Chat.BroadcastToAllExcept(ctx.CallerID, "kick_kicked_broadcast", names.CharacterName, callerNames.CharacterName);
                    ctx.Reply("kick_kicked_feedback", names.CharacterName);
                }
            }
        }
    }
}
using Rocket.API;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using Uncreated.Players;
using Uncreated.Warfare.Commands.CommandSystem;

namespace Uncreated.Warfare.Commands;

public class WarnCommand : IRocketCommand
{
    public AllowedCaller AllowedCaller => AllowedCaller.Both;
    public string Name => "warn";
    public string Help => "Warn players who are misbehaving.";
    public string Syntax => "/warn <player> <reason>";
    private readonly List<string> _aliases = new List<string>(0);
    public List<string> Aliases => _aliases;
    private readonly List<string> _permissions = new List<string>(1) { "uc.warn" };
	public List<string> Permissions => _permissions;
    public void Execute(IRocketPlayer caller, string[] command)
    {
        WarfareContext ctx = new WarfareContext(caller, command);
        if (!ctx.HasArgs(2))
        {
            ctx.Reply("warn_syntax");
        }
        else if (!ctx.TryGet(0, out ulong targetId, out UCPlayer? target) || target is null)
        {
            ctx.Reply("warn_no_player_found", ctx.Parameters[0]);
        }
        else
        {
            string? reason = ctx.GetRange(1);
            if (string.IsNullOrEmpty(reason))
            {
                ctx.Reply("warn_no_reason_provided", ctx.Parameters[1]);
            }
            else
            {
                FPlayerName targetNames = F.GetPlayerOriginalNames(target);
                if (UCWarfare.Config.ModerationSettings.LogWarning)
                {
                    Data.DatabaseManager.AddWarning(targetId, ctx.CallerID, reason!);
                    OffenseManager.NetCalls.SendPlayerWarned.NetInvoke(targetId, ctx.CallerID, reason!, DateTime.Now);
                }

                string tid = targetId.ToString(Data.Locale);
                ActionLog.Add(EActionLogType.WARN_PLAYER, $"WARNED {tid} FOR \"{reason}\"", ctx.CallerID);
                if (ctx.IsConsole)
                {
                    L.Log(Translation.Translate("warn_warned_console_operator", 0, out _, targetNames.PlayerName, tid, reason!), ConsoleColor.Cyan);
                    Chat.BroadcastToAllExcept(targetId, "warn_warned_broadcast_operator", targetNames.CharacterName);
                    ToastMessage.QueueMessage(target, new ToastMessage(Translation.Translate("warn_warned_private_operator", target, out _, reason!), EToastMessageSeverity.WARNING));
                    target.SendChat("warn_warned_private_operator", reason!);
                }
                else
                {
                    FPlayerName callerNames = F.GetPlayerOriginalNames(ctx.CallerID);
                    L.Log(Translation.Translate("warn_warned_console", 0, out _, targetNames.PlayerName, tid, callerNames.PlayerName, ctx.CallerID.ToString(Data.Locale), reason!), ConsoleColor.Cyan);
                    Chat.BroadcastToAllExcept(new ulong[2] { targetId, ctx.CallerID }, "warn_warned_broadcast", targetNames.CharacterName, callerNames.CharacterName);
                    ctx.Reply("warn_warned_feedback", targetNames.CharacterName);
                    ToastMessage.QueueMessage(target, new ToastMessage(Translation.Translate("warn_warned_private", target, out _, callerNames.CharacterName, reason!), EToastMessageSeverity.WARNING));
                    target.SendChat("warn_warned_private", callerNames.CharacterName, reason!);
                }
            }
        }
    }
}
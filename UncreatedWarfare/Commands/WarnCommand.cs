using System;
using Uncreated.Framework;
using Uncreated.Players;
using Uncreated.Warfare.Commands.CommandSystem;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public class WarnCommand : Command
{
    private const string SYNTAX = "/warn";
    private const string HELP = "Does nothing.";

    public WarnCommand() : base("warn", EAdminType.MEMBER) { }

    public override void Execute(CommandInteraction ctx)
    {
        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);

        ctx.AssertArgs(2, "warn_syntax");

        if (!ctx.TryGet(0, out ulong targetId, out UCPlayer? target) || target is null)
            throw ctx.Reply("warn_no_player_found", ctx.Parameters[0]);

        string? reason = ctx.GetRange(1);
        if (string.IsNullOrEmpty(reason))
            throw ctx.Reply("warn_no_reason_provided", ctx.Parameters[1]);

        FPlayerName targetNames = F.GetPlayerOriginalNames(target);

        OffenseManager.LogWarnPlayer(targetId, ctx.CallerID, reason!, DateTime.Now);

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
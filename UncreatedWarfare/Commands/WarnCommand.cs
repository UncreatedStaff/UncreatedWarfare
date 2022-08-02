using System;
using Uncreated.Framework;
using Uncreated.Players;
using Uncreated.Warfare.Commands.CommandSystem;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public class WarnCommand : Command
{
    private const string SYNTAX = "/warn";
    private const string HELP = "Warn misbehaving players.";

    public WarnCommand() : base("warn", EAdminType.MODERATOR) { }

    public override void Execute(CommandInteraction ctx)
    {
        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);

        ctx.AssertArgs(2, "warn_syntax");

        if (!ctx.TryGet(0, out ulong targetId, out UCPlayer? target) || target is null)
            throw ctx.Reply(T.PlayerNotFound);

        string? reason = ctx.GetRange(1);
        if (string.IsNullOrEmpty(reason))
            throw ctx.Reply(T.NoReasonProvided);

        FPlayerName targetNames = F.GetPlayerOriginalNames(target);

        OffenseManager.LogWarnPlayer(targetId, ctx.CallerID, reason!, DateTime.Now);

        string tid = targetId.ToString(Data.Locale);
        ActionLogger.Add(EActionLogType.WARN_PLAYER, $"WARNED {tid} FOR \"{reason}\"", ctx.CallerID);
        if (ctx.IsConsole)
        {
            L.Log($"{targetNames.PlayerName} ({tid}) was warned for: {reason}.", ConsoleColor.Cyan);
            Chat.Broadcast(LanguageSet.AllBut(targetId), T.WarnSuccessBroadcastOperator, targetNames);
            ToastMessage.QueueMessage(target, new ToastMessage(T.WarnSuccessDMOperator.Translate(Data.Languages.TryGetValue(ctx.CallerID, out string lang) ? lang : L.DEFAULT, reason!, ctx.Caller, ctx.Caller is null ? 0 : ctx.Caller.GetTeam()), EToastMessageSeverity.WARNING));
            target.SendChat(T.WarnSuccessDMOperator, reason!);
        }
        else
        {
            FPlayerName callerNames = F.GetPlayerOriginalNames(ctx.CallerID);
            L.Log($"{targetNames.PlayerName} ({tid}) was warned by {callerNames.PlayerName} ({ctx.CallerID}) for: {reason}.", ConsoleColor.Cyan);
            Chat.Broadcast(LanguageSet.AllBut(targetId, ctx.CallerID), T.WarnSuccessBroadcast, targetNames, ctx.Caller);
            ctx.Reply(T.WarnSuccessFeedback, targetNames);
            ToastMessage.QueueMessage(target, new ToastMessage(T.WarnSuccessDM.Translate(Data.Languages.TryGetValue(ctx.CallerID, out string lang) ? lang : L.DEFAULT, callerNames, reason!, ctx.Caller, ctx.Caller is null ? 0 : ctx.Caller.GetTeam()), EToastMessageSeverity.WARNING));
            target.SendChat(T.WarnSuccessDM, callerNames, reason!);
        }
    }
}
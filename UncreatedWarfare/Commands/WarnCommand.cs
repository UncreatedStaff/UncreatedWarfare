using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;

namespace Uncreated.Warfare.Commands;
public class WarnCommand : Command
{
    private const string SYNTAX = "/warn";
    private const string HELP = "Warn misbehaving players.";

    public WarnCommand() : base("warn", EAdminType.MODERATOR)
    {
        Structure = new CommandStructure
        {
            Description = "Send a warning to a player.",
            Parameters = new CommandParameter[]
            {
                new CommandParameter("Player", typeof(IPlayer))
                {
                    Parameters = new CommandParameter[]
                    {
                        new CommandParameter("Reason", typeof(string))
                        {
                            IsRemainder = true
                        }
                    }
                }
            }
        };
    }

    public override void Execute(CommandContext ctx)
    {
        throw ctx.SendNotImplemented();
#if false
        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);

        ctx.AssertArgs(2, "warn_syntax");

        if (!ctx.TryGet(0, out ulong targetId, out UCPlayer? target) || target is null)
            throw ctx.Reply(T.PlayerNotFound);

        string? reason = ctx.GetRange(1);
        if (string.IsNullOrEmpty(reason))
            throw ctx.Reply(T.NoReasonProvided);

        PlayerNames targetNames = target.Name;

        OffenseManager.LogWarnPlayer(targetId, ctx.CallerID, reason!, DateTime.Now);

        string tid = targetId.ToString(Data.AdminLocale);
        ActionLog.Add(ActionLogType.WarnPlayer, $"WARNED {tid} FOR \"{reason}\"", ctx.CallerID);
        if (ctx.IsConsole)
        {
            L.Log($"{targetNames.PlayerName} ({tid}) was warned for: {reason}.", ConsoleColor.Cyan);
            Chat.Broadcast(LanguageSet.AllBut(targetId), T.WarnSuccessBroadcastOperator, targetNames);
            ToastMessage.QueueMessage(target, ToastMessage.Popup(T.WarnSuccessTitle.Translate(ctx.Caller), T.WarnSuccessDMOperator.Translate(ctx.Caller, false, reason!)));
            target.SendChat(T.WarnSuccessDMOperator, reason!);
        }
        else
        {
            PlayerNames callerNames = ctx.Caller.Name;
            L.Log($"{targetNames.PlayerName} ({tid}) was warned by {callerNames.PlayerName} ({ctx.CallerID}) for: {reason}.", ConsoleColor.Cyan);
            Chat.Broadcast(LanguageSet.AllBut(targetId, ctx.CallerID), T.WarnSuccessBroadcast, targetNames, ctx.Caller);
            ctx.Reply(T.WarnSuccessFeedback, targetNames);
            ToastMessage.QueueMessage(target, ToastMessage.Popup(T.WarnSuccessTitle.Translate(ctx.Caller), T.WarnSuccessDM.Translate(ctx.Caller, false, callerNames, reason!)));
            target.SendChat(T.WarnSuccessDM, callerNames, reason!);
        }
#endif
    }
}
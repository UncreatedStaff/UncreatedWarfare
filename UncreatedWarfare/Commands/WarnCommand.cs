using Uncreated.Warfare.Interaction.Commands;

namespace Uncreated.Warfare.Commands;

[Command("warn")]
[MetadataFile(nameof(GetHelpMetadata))]
public class WarnCommand : IExecutableCommand
{
#if false
    private const string Syntax = "/warn";
    private const string Help = "Warn misbehaving players.";
#endif

    /// <inheritdoc />
    public CommandContext Context { get; set; }

    /// <summary>
    /// Get /help metadata about this command.
    /// </summary>
    public static CommandStructure GetHelpMetadata()
    {
        return new CommandStructure
        {
            Description = "Send a warning to a player.",
            Parameters =
            [
                new CommandParameter("Player", typeof(IPlayer))
                {
                    Parameters =
                    [
                        new CommandParameter("Reason", typeof(string))
                        {
                            IsRemainder = true
                        }
                    ]
                }
            ]
        };
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        throw Context.SendNotImplemented();
#if false
        Context.AssertHelpCheck(0, Syntax + " - " + Help);

        Context.AssertArgs(2, "warn_syntax");

        if (!Context.TryGet(0, out ulong targetId, out UCPlayer? target) || target is null)
            throw Context.Reply(T.PlayerNotFound);

        string? reason = Context.GetRange(1);
        if (string.IsNullOrEmpty(reason))
            throw Context.Reply(T.NoReasonProvided);

        PlayerNames targetNames = target.Name;

        OffenseManager.LogWarnPlayer(targetId, Context.CallerID, reason!, DateTime.Now);

        string tid = targetId.ToString(Data.AdminLocale);
        ActionLog.Add(ActionLogType.WarnPlayer, $"WARNED {tid} FOR \"{reason}\"", Context.CallerID);
        if (Context.IsConsole)
        {
            L.Log($"{targetNames.PlayerName} ({tid}) was warned for: {reason}.", ConsoleColor.Cyan);
            Chat.Broadcast(LanguageSet.AllBut(targetId), T.WarnSuccessBroadcastOperator, targetNames);
            ToastMessage.QueueMessage(target, ToastMessage.Popup(T.WarnSuccessTitle.Translate(Context.Caller), T.WarnSuccessDMOperator.Translate(Context.Caller, false, reason!)));
            target.SendChat(T.WarnSuccessDMOperator, reason!);
        }
        else
        {
            PlayerNames callerNames = Context.Caller.Name;
            L.Log($"{targetNames.PlayerName} ({tid}) was warned by {callerNames.PlayerName} ({Context.CallerID}) for: {reason}.", ConsoleColor.Cyan);
            Chat.Broadcast(LanguageSet.AllBut(targetId, Context.CallerID), T.WarnSuccessBroadcast, targetNames, Context.Caller);
            Context.Reply(T.WarnSuccessFeedback, targetNames);
            ToastMessage.QueueMessage(target, ToastMessage.Popup(T.WarnSuccessTitle.Translate(Context.Caller), T.WarnSuccessDM.Translate(Context.Caller, false, callerNames, reason!)));
            target.SendChat(T.WarnSuccessDM, callerNames, reason!);
        }
#endif
    }
}
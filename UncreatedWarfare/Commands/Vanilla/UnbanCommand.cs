using DanielWillett.ReflectionTools;
using Uncreated.Warfare.Interaction.Commands;

namespace Uncreated.Warfare.Commands;

[Command("unban"), Priority(1)]
[MetadataFile(nameof(GetHelpMetadata))]
public class UnbanCommand : IExecutableCommand
{
#if false
    private const string Syntax = "/unban <player>";
    private const string Help = "Unban players who have served their time.";
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
            Description = "Unban players who have served their time.",
            Parameters =
            [
                new CommandParameter("Player", typeof(IPlayer))
            ]
        };
    }

    /// <inheritdoc />
    public UniTask ExecuteAsync(CancellationToken token)
    {
        throw Context.SendNotImplemented();
#if false
        Context.AssertHelpCheck(0, Syntax + " - " + Help);

        if (!Context.HasArgs(1))
            throw Context.SendCorrectUsage(Syntax);

        if (!Context.TryGet(0, out ulong targetId, out UCPlayer? target))
            throw Context.Reply(T.PlayerNotFound);

        PlayerNames targetNames = await F.GetPlayerOriginalNamesAsync(targetId, token);
        await UniTask.SwitchToMainThread(token);
        if (target is not null || !Provider.requestUnbanPlayer(Context.CallerCSteamID, new CSteamID(targetId)))
        {
            Context.Reply(T.UnbanNotBanned, targetNames);
            return;
        }

        OffenseManager.LogUnbanPlayer(targetId, Context.CallerID, DateTime.Now);

        string tid = targetId.ToString(Data.AdminLocale);
        ActionLog.Add(ActionLogType.UnbanPlayer, $"UNBANNED {tid}", Context.CallerID);
        if (Context.IsConsole)
        {
            L.Log($"{targetNames.PlayerName} ({tid}) was successfully unbanned.", ConsoleColor.Cyan);
            Chat.Broadcast(T.UnbanSuccessBroadcastOperator, targetNames);
        }
        else
        {
            L.Log($"{targetNames.PlayerName} ({tid}) was unbanned by {Context.Caller.Name.PlayerName} ({Context.CallerID.ToString(Data.AdminLocale)}).", ConsoleColor.Cyan);
            Context.Reply(T.UnbanSuccessFeedback, targetNames);
            Chat.Broadcast(LanguageSet.AllBut(Context.CallerID), T.UnbanSuccessBroadcast, targetNames, Context.Caller);
        }
#endif
    }
}
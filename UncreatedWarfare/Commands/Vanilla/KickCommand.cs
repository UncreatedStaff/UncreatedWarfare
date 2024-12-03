﻿using DanielWillett.ReflectionTools;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Commands;

[Command("kick"), Priority(1)]
[MetadataFile(nameof(GetHelpMetadata))]
public class KickCommand : IExecutableCommand
{
#if false
    private const string Syntax = "/kick <player> <reason>";
    private const string Help = "Kick players who are misbehaving.";
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
            Description = "Temporarily removes a player from the server.",
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
        ctx.AssertHelpCheck(0, Syntax + " - " + Help);

        if (!ctx.HasArgs(2))
            throw ctx.SendCorrectUsage(Syntax);

        if (!ctx.TryGet(0, out ulong targetId, out UCPlayer? target) || target is null)
            throw ctx.Reply(T.PlayerNotFound);

        if (!ctx.TryGetRange(1, out string reason))
            throw ctx.Reply(T.NoReasonProvided);

        PlayerNames names = target.Name;
        Provider.kick(target.Player.channel.owner.playerID.steamID, reason!);

        OffenseManager.LogKickPlayer(targetId, ctx.CallerID, reason!, DateTime.Now);

        ctx.LogAction(ActionLogType.KickPlayer, $"KICKED {targetId.ToString(CultureInfo.InvariantCulture)} FOR \"{reason}\"");
        if (ctx.IsConsole)
        {
            ctx.ReplyString($"{names.PlayerName} ({targetId.ToString(Data.LocalLocale)}) was kicked by an operator because: {reason}.", ConsoleColor.Cyan);
            Chat.Broadcast(T.KickSuccessBroadcastOperator, names);
        }
        else
        {
            PlayerNames callerNames = ctx.Caller is null ? PlayerNames.Console : ctx.Caller.Name;
            L.Log($"{names.PlayerName} ({targetId.ToString(CultureInfo.InvariantCulture)}) was kicked by {callerNames.PlayerName} ({ctx.CallerID.ToString(CultureInfo.InvariantCulture)}) because: {reason}.", ConsoleColor.Cyan);
            Chat.Broadcast(LanguageSet.AllBut(ctx.CallerID), T.KickSuccessBroadcast, names, callerNames);
            ctx.Reply(T.KickSuccessFeedback, names);
        }
#endif
    }
}
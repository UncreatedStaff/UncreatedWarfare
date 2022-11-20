using SDG.Unturned;
using System;
using Uncreated.Framework;
using Uncreated.Players;
using Uncreated.Warfare.Commands.CommandSystem;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands.VanillaRework;
public class KickCommand : Command
{
    private const string SYNTAX = "/kick <player> <reason>";
    private const string HELP = "Kick players who are misbehaving.";

    public KickCommand() : base("kick", EAdminType.MODERATOR, 1) { }

    public override void Execute(CommandInteraction ctx)
    {
        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);

        if (!ctx.HasArgs(2))
            throw ctx.SendCorrectUsage(SYNTAX);

        if (!ctx.TryGet(0, out ulong targetId, out UCPlayer? target) || target is null)
            throw ctx.Reply(T.PlayerNotFound);

        if (!ctx.TryGet(1, out string reason))
            throw ctx.Reply(T.NoReasonProvided);

        PlayerNames names = target.Name;
        Provider.kick(target.Player.channel.owner.playerID.steamID, reason!);

        OffenseManager.LogKickPlayer(targetId, ctx.CallerID, reason!, DateTime.Now);

        ctx.LogAction(EActionLogType.KICK_PLAYER, $"KICKED {targetId.ToString(Data.Locale)} FOR \"{reason}\"");
        if (ctx.IsConsole)
        {
            ctx.ReplyString($"{names.PlayerName} ({targetId.ToString(Data.Locale)}) was kicked by an operator because: {reason}.", ConsoleColor.Cyan);
            Chat.Broadcast(T.KickSuccessBroadcastOperator, names);
        }
        else
        {
            PlayerNames callerNames = ctx.Caller is null ? PlayerNames.Console : ctx.Caller.Name;
            L.Log($"{names.PlayerName} ({targetId.ToString(Data.Locale)}) was kicked by {callerNames.PlayerName} ({ctx.CallerID.ToString(Data.Locale)}) because: {reason}.", ConsoleColor.Cyan);
            Chat.Broadcast(LanguageSet.AllBut(ctx.CallerID), T.KickSuccessBroadcast, names, callerNames);
            ctx.Reply(T.KickSuccessFeedback, names);
        }
    }
}
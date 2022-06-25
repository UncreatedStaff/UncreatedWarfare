using SDG.Unturned;
using System;
using System.Threading.Tasks;
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
            throw ctx.Reply("kick_syntax");

        if (!ctx.TryGet(0, out ulong targetId, out UCPlayer? target) || target is null)
            throw ctx.Reply("kick_no_player_found", ctx.Parameters[0]);

        string? reason = ctx.GetRange(1);
        if (string.IsNullOrEmpty(reason))
            throw ctx.Reply("kick_no_reason_provided", ctx.Parameters[1]);

        FPlayerName names = F.GetPlayerOriginalNames(target);
        Provider.kick(target.Player.channel.owner.playerID.steamID, reason!);

        OffenseManager.NetCalls.SendPlayerKicked.NetInvoke(targetId, ctx.CallerID, reason!, DateTime.Now);
        Data.DatabaseManager.AddKick(targetId, ctx.CallerID, reason!);

        ctx.LogAction(EActionLogType.KICK_PLAYER, $"KICKED {targetId.ToString(Data.Locale)} FOR \"{reason}\"");
        if (ctx.IsConsole)
        {
            L.Log(Translation.Translate("kick_kicked_console_operator", 0, out _, names.PlayerName, targetId.ToString(Data.Locale), reason!), ConsoleColor.Cyan);
            Chat.Broadcast("kick_kicked_broadcast_operator", names.CharacterName);
            ctx.Defer();
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
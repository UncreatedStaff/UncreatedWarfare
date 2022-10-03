using System;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public class UnmuteCommand : Command
{
    private const string SYNTAX = "/unmute";
    private const string HELP = "Does nothing.";

    public UnmuteCommand() : base("unmute", EAdminType.MODERATOR) { }

    public override void Execute(CommandInteraction ctx)
    {
        ctx.AssertArgs(1, SYNTAX);

        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);

        if (ctx.TryGet(0, out ulong playerId, out UCPlayer? onlinePlayer))
        {
            if (onlinePlayer is not null)
            {
                if (onlinePlayer.MuteType == EMuteType.NONE || onlinePlayer.TimeUnmuted < DateTime.Now)
                {
                    ctx.Reply(T.UnmuteNotMuted, onlinePlayer);
                    return;
                }
            }

            OffenseManager.UnmutePlayer(playerId, ctx.CallerID);
            ctx.Defer();
        }
        else ctx.Reply(T.PlayerNotFound);
    }
}

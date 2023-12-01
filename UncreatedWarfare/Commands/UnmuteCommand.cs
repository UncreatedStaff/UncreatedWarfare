using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public class UnmuteCommand : Command
{
    private const string SYNTAX = "/unmute";
    private const string HELP = "Lift a mute offense from a player.";

    public UnmuteCommand() : base("unmute", EAdminType.MODERATOR)
    {
        Structure = new CommandStructure
        {
            Description = HELP,
            Parameters = new CommandParameter[]
            {
                new CommandParameter("Player", typeof(IPlayer))
            }
        };
    }

    public override void Execute(CommandInteraction ctx)
    {
        throw ctx.SendNotImplemented();
#if false
        ctx.AssertArgs(1, SYNTAX);

        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);

        if (ctx.TryGet(0, out ulong playerId, out UCPlayer? onlinePlayer))
        {
            if (onlinePlayer is not null)
            {
                if (onlinePlayer.MuteType == MuteType.None || onlinePlayer.TimeUnmuted < DateTime.Now)
                {
                    ctx.Reply(T.UnmuteNotMuted, onlinePlayer);
                    return;
                }
            }

            Task.Run(async () =>
            {
                try
                {
                    await OffenseManager.UnmutePlayerAsync(playerId, ctx.CallerID, DateTimeOffset.UtcNow);
                }
                catch (Exception ex)
                {
                    L.LogError("Error unmuting " + playerId + ".");
                    L.LogError(ex);
                }
            });
            ctx.Defer();
        }
        else ctx.Reply(T.PlayerNotFound);
#endif
    }
}

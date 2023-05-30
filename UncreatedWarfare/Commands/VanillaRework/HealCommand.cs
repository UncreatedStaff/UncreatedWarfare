using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public class HealCommand : Command
{
    private const string SYNTAX = "/heal [player]";
    private const string HELP = "Heal yourself or someone else to max health and revive them if they're injured.";

    public HealCommand() : base("heal", EAdminType.ADMIN)
    {
        Structure = new CommandStructure
        {
            Description = HELP,
            Parameters = new CommandParameter[]
            {
                new CommandParameter("Player", typeof(IPlayer))
                {
                    IsOptional = true,
                    IsRemainder = true
                }
            }
        };
    }

    public override void Execute(CommandInteraction ctx)
    {
        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);

        if (!ctx.HasPermission(EAdminType.VANILLA_ADMIN, PermissionComparison.Exact))
            ctx.AssertOnDuty();

        if (ctx.TryGet(0, out _, out UCPlayer? onlinePlayer) && onlinePlayer is not null)
        {
            onlinePlayer.Player.life.sendRevive();

            if (Data.Is(out IRevives rev))
                rev.ReviveManager.RevivePlayer(onlinePlayer);

            ctx.Reply(T.HealPlayer, onlinePlayer);

            if (onlinePlayer.Steam64 != ctx.CallerID)
                onlinePlayer.SendChat(T.HealSelf);
        }
        else
        {
            ctx.AssertRanByPlayer();

            ctx.Caller.Player.life.sendRevive();

            if (Data.Is(out IRevives rev))
                rev.ReviveManager.RevivePlayer(ctx.Caller);

            ctx.Reply(T.HealSelf);
        }
    }
}
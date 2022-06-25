using SDG.Unturned;
using System;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Revives;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public class HealCommand : Command
{
    private const string SYNTAX = "/heal [player]";
    private const string HELP = "Heal yourself or someone else to max health and revive them if they're injured.";

    public HealCommand() : base("heal", EAdminType.ADMIN) { }

    public override void Execute(CommandInteraction ctx)
    {
        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);

        ctx.AssertRanByPlayer();

        if (!ctx.HasPermission(EAdminType.VANILLA_ADMIN, PermissionComparison.Exact))
            ctx.AssertOnDuty();

        if (ctx.TryGet(0, out _, out UCPlayer? onlinePlayer) && onlinePlayer is not null)
        {
            onlinePlayer.Player.life.sendRevive();

            if (Data.Is(out IRevives rev))
                rev.ReviveManager.RevivePlayer(ctx.Caller);

            ctx.Reply("heal_player", onlinePlayer.CharacterName, Teams.TeamManager.GetTeamHexColor(onlinePlayer.GetTeam()));
        }
        else
        {
            ctx.Caller.Player.life.sendRevive();

            if (Data.Is(out IRevives rev))
                rev.ReviveManager.RevivePlayer(ctx.Caller);

            ctx.Reply("heal_self");
        }
    }
}
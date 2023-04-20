using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Teams;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public class UnstuckCommand : Command
{
    private const string SYNTAX = "/unstuck";
    private const string HELP = "Run this command if you're somehow stuck in the lobby.";

    public UnstuckCommand() : base("unstuck", EAdminType.MEMBER)
    {
        Structure = new CommandStructure
        {
            Description = HELP
        };
    }

    public override void Execute(CommandInteraction ctx)
    {
        ctx.AssertRanByPlayer();

        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);

        ctx.AssertGamemode(out ITeams t);

        if (!t.UseTeamSelector) throw ctx.SendGamemodeError();

        if (TeamManager.LobbyZone.IsInside(ctx.Caller.Position))
        {
            t.TeamSelector?.ResetState(ctx.Caller);
            ctx.ReplyString("Reset lobby state.");
        }
        else throw ctx.SendUnknownError();

        ctx.Defer();
    }
}

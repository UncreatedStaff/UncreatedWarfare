using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public class VanishCommand : Command
{
    private const string SYNTAX = "/vanish";
    private const string HELP = "Toggle your visibility to other players.";

    public VanishCommand() : base("vanish", EAdminType.ADMIN)
    {
        Structure = new CommandStructure
        {
            Description = "Toggle your visibility to other players."
        };
    }

    public override void Execute(CommandInteraction ctx)
    {
        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);

        ctx.AssertRanByPlayer();

        if (!ctx.HasPermission(EAdminType.VANILLA_ADMIN, PermissionComparison.Exact))
            ctx.AssertOnDuty();
        bool v = ctx.Caller.Player.movement.canAddSimulationResultsToUpdates;
        DutyCommand.SetVanishMode(ctx.Caller.Player, v);
        if (v)
        {
            ctx.Reply(T.VanishModeEnabled);
        }
        else
        {
            ctx.Reply(T.VanishModeDisabled);
        }
    }
}
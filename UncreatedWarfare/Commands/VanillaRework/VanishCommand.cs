using Uncreated.Framework;
using Uncreated.Warfare.Commands.Dispatch;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public class VanishCommand : Command
{
    private const string SYNTAX = "/vanish";
    private const string HELP = "Toggle your visibility to other players.";

    public VanishCommand() : base("vanish", EAdminType.TRIAL_ADMIN_ON_DUTY)
    {
        Structure = new CommandStructure
        {
            Description = "Toggle your visibility to other players."
        };
    }

    public override void Execute(CommandContext ctx)
    {
        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);

        ctx.AssertRanByPlayer();

        if (!ctx.HasPermission(EAdminType.VANILLA_ADMIN, PermissionComparison.Exact))
            ctx.AssertOnDuty();
        bool isUnvanished = ctx.Player.Player.movement.canAddSimulationResultsToUpdates;
        ctx.Player.VanishMode = isUnvanished;
        if (isUnvanished)
        {
            ctx.Reply(T.VanishModeEnabled);
        }
        else
        {
            ctx.Reply(T.VanishModeDisabled);
        }
    }
}
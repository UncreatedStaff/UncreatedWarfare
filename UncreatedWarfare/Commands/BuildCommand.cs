using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public class BuildCommand : Command
{
    public BuildCommand() : base("build", EAdminType.MEMBER)
    {
        Structure = new CommandStructure
        {
            Description = "Legacy command, tells you to use your shovel."
        };
    }
    public override void Execute(CommandContext ctx) => ctx.Reply(T.BuildLegacyExplanation);
}

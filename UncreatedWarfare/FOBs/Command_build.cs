using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public class BuildCommand : Command
{
    public BuildCommand() : base("build", EAdminType.MEMBER) { }
    public override void Execute(CommandInteraction ctx) => ctx.Reply(T.BuildLegacyExplanation);
}

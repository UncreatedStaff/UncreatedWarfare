using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;

namespace Uncreated.Warfare.Commands;

public class ConfirmCommand : Command
{
    public ConfirmCommand() : base("confirm", EAdminType.MEMBER) { }
    public override void Execute(CommandInteraction cts) { }
}

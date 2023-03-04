using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;

namespace Uncreated.Warfare.Commands;

public class ConfirmCommand : Command
{
    public ConfirmCommand() : base("confirm", EAdminType.MEMBER, priority: -1)
    {
        AddAlias("c");
    }

    public override void Execute(CommandInteraction ctx)
    {
        ctx.Defer();
    }
}

public class DenyCommand : Command
{
    public DenyCommand() : base("deny", EAdminType.MEMBER, priority: -1)
    {
        AddAlias("d");
    }

    public override void Execute(CommandInteraction ctx)
    {
        ctx.Defer();
    }
}

using SDG.Unturned;
using System;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public class TraitCommand : Command
{
    private const string SYNTAX = "/trait";
    private const string HELP = "Manage properties of traits.";

    public TraitCommand() : base("trait", EAdminType.ADMIN_ON_DUTY) { }

    public override void Execute(CommandInteraction ctx)
    {
        throw ctx.SendNotImplemented();
    }
}

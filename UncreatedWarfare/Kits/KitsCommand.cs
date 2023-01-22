using SDG.Unturned;
using System;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Kits;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public sealed class KitsCommand : Command
{
    private const string Syntax = "/kits";
    private const string Help = "Open the kit menu.";

    public KitsCommand() : base("kits", EAdminType.MEMBER) { }

    public override void Execute(CommandInteraction ctx)
    {
        ctx.AssertHelpCheck(0, Syntax + " - " + Help);
        
        KitManager.MenuUI.OpenUI(ctx.Caller);
    }
}
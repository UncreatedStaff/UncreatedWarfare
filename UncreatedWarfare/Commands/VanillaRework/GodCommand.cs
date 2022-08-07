using SDG.Unturned;
using System;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Revives;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public class GodCommand : Command
{
    private const string SYNTAX = "/god";
    private const string HELP = "Toggle your ability to take damage.";

    public GodCommand() : base("god", EAdminType.ADMIN) { }

    public override void Execute(CommandInteraction ctx)
    {
        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);

        ctx.AssertRanByPlayer();

        if (!ctx.HasPermission(EAdminType.VANILLA_ADMIN, PermissionComparison.Exact))
            ctx.AssertOnDuty();

        ctx.Caller.GodMode = !ctx.Caller.GodMode;

        if (ctx.Caller.GodMode)
        {
            ctx.Caller.Player.life.sendRevive();
            if (Data.Is(out IRevives rev))
                rev.ReviveManager.RevivePlayer(ctx.Caller);
            ctx.Reply(T.GodModeEnabled);
        }
        else
        {
            ctx.Reply(T.GodModeDisabled);
        }
    }
}
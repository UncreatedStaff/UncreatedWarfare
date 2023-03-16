using System;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Squads;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public class RallyCommand : Command
{
    private const string SYNTAX = "/rally";
    private const string HELP = "Deploys you to a rallypoint.";

    public RallyCommand() : base("rally", EAdminType.MEMBER)
    {
        Structure = new CommandStructure
        {
            Description = HELP,
            Parameters = new CommandParameter[]
            {
                new CommandParameter("Cancel")
                {
                    Aliases = new string[] { "c", "abort" },
                    Description = "Cancels pending deployment to a rallypoint for your squadmembers.",
                    IsOptional = true
                },
                /*new CommandParameter("Deny")
                {
                    Description = "Cancels pending deployment to a rallypoint for just you.",
                    IsOptional = true
                }*/
            }
        };
    }

    public override void Execute(CommandInteraction ctx)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif

        ctx.AssertGamemode<ISquads>();

        ctx.AssertRanByPlayer();

        ctx.AssertHelpCheck(0, SYNTAX + " - " + HELP);

        if (!UCWarfare.Config.EnableSquads)
        {
            ctx.Reply(T.SquadsDisabled);
            return;
        }

        if (ctx.Caller.Squad is null)
            throw ctx.Reply(T.RallyNotSquadleader);

        RallyPoint? rallypoint = ctx.Caller.Squad.RallyPoint;

        if (rallypoint == null || !rallypoint.IsActive)
            throw ctx.Reply(T.RallyNotActiveSL);

        if (ctx.MatchParameter(0, "cancel", "c", "abort"))
        {
            if (rallypoint.IsDeploying)
            {
                if (ctx.Caller.IsSquadLeader())
                {
                    rallypoint.AwaitingPlayers.Clear();
                    rallypoint.ShowUIForPlayer(ctx.Caller);
                    ctx.Reply(T.RallyCancel);
                }
                else
                {
                    rallypoint.AwaitingPlayers.RemoveAll(p => p.Steam64 == ctx.CallerID);
                    rallypoint.ShowUIForPlayer(ctx.Caller);
                    ctx.Reply(T.RallyCancel);
                }
            }
            else throw ctx.Reply(T.RallyNoDeny);
        }
        else if (ctx.HasArgsExact(0))
        {
            if (ctx.Caller.Squad is null || !ctx.Caller.IsSquadLeader())
                throw ctx.Reply(T.RallyNotSquadleader);

            if (!rallypoint.IsDeploying)
            {
                if (CooldownManager.HasCooldown(ctx.Caller, CooldownType.Rally, out Cooldown cooldown))
                    throw ctx.Reply(T.RallyCooldown, cooldown);

                rallypoint.StartDeployment();
                CooldownManager.StartCooldown(ctx.Caller, CooldownType.Rally, SquadManager.Config.RallyCooldown);
                ctx.Defer();
            }
            else throw ctx.Reply(T.RallyAlreadyDeploying);
        }
        else throw ctx.SendCorrectUsage(SYNTAX);
    }
}

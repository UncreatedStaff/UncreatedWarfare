using System;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Insurgency;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Teams;
using Command = Uncreated.Warfare.Commands.CommandSystem.Command;

namespace Uncreated.Warfare.Commands;
public class DeployCommand : Command
{
    private const string Syntax = "/deploy main -OR- /deploy <fob name>";
    private const string Help = "Deploy to a point of interest such as a main base, FOB, VCP, or cache.";

    public DeployCommand() : base("deploy", EAdminType.MEMBER)
    {
        AddAlias("dep");
        AddAlias("warp");
        AddAlias("warps");
        AddAlias("tpa");
        AddAlias("go");
        AddAlias("goto");
        AddAlias("fob");
        AddAlias("deployfob");
        AddAlias("df");
        AddAlias("dp");
        Structure = new CommandStructure
        {
            Description = "Deploy to a point of interest such as a main base, FOB, VCP, or cache.",
            Parameters = new CommandParameter[]
            {
                new CommandParameter("Location", typeof(IDeployable), "Lobby", "Main"),
                new CommandParameter("Cancel")
                {
                    Aliases = new string[] { "stop" }
                }
            }
        };
    }

    public override void Execute(CommandInteraction ctx)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ctx.AssertRanByPlayer();

        ctx.AssertArgs(1, Syntax + " - " + Help);

        if (ctx.MatchParameter(0, "cancel", "stop") && ctx.Caller.Player.TryGetPlayerData(out UCPlayerData comp) && comp.CurrentTeleportRequest != null)
        {
            comp.CancelDeployment();
            throw ctx.Reply(T.DeployCancelled);
        }

        if (Data.Is(out IRevives r) && r.ReviveManager.IsInjured(ctx.CallerID))
            throw ctx.Reply(T.DeployInjured);

        string input = ctx.GetRange(0)!;

        UCPlayerData? c = ctx.Caller.Player.GetPlayerData(out _);
        if (c is null) throw ctx.SendUnknownError();

        ulong team = ctx.Caller.GetTeam();
        if (team is not 1 and not 2)
            throw ctx.Reply(T.NotOnCaptureTeam);

        bool inMain = ctx.Caller.Player.IsInMain();
        bool inLobby = !inMain && TeamManager.LobbyZone.IsInside(ctx.Caller.Position);
        bool shouldCancelOnMove = !inMain;
        bool shouldCancelOnDamage = !inMain;

        if (CooldownManager.HasCooldown(ctx.Caller, CooldownType.Deploy, out Cooldown cooldown))
            throw ctx.Reply(T.DeployCooldown, cooldown);

        if (!(inMain || inLobby))
        {
            if (CooldownManager.HasCooldown(ctx.Caller, CooldownType.Combat, out Cooldown combatlog))
                throw ctx.Reply(T.DeployInCombat, combatlog);

            if (!Gamemode.Config.BarricadeInsurgencyCache.ValidReference(out Guid guid) ||
                !(ctx.Caller.IsOnFOB(out IFOB fob) && (fob is not FOB f2 || !f2.Bleeding) || UCBarricadeManager.IsBarricadeNearby(guid, 10, ctx.Caller.Position, team, out _)))
                throw ctx.Reply(Data.Is<Insurgency>() ? T.DeployNotNearFOBInsurgency : T.DeployNotNearFOB);
        }

        IDeployable? destination = null;
        if (!FOBManager.Loaded || !FOBManager.TryFindFOB(input, team, out destination))
        {
            if (input.Equals("lobby", StringComparison.InvariantCultureIgnoreCase))
                throw ctx.Reply(T.DeployLobbyRemoved);

            if (input.Equals("main", StringComparison.InvariantCultureIgnoreCase) ||
                input.Equals("base", StringComparison.InvariantCultureIgnoreCase) ||
                input.Equals("home", StringComparison.InvariantCultureIgnoreCase) ||
                input.Equals("mainbase", StringComparison.InvariantCultureIgnoreCase) ||
                input.Equals("main base", StringComparison.InvariantCultureIgnoreCase) ||
                input.Equals("homebase", StringComparison.InvariantCultureIgnoreCase) ||
                input.Equals("home base", StringComparison.InvariantCultureIgnoreCase))
            {
                destination = TeamManager.GetMain(team);
            }
        }

        if (destination == null)
            throw ctx.Reply(T.DeployableNotFound, input);
        
        Deployment.DeployTo(ctx.Caller, destination, ctx, shouldCancelOnMove, shouldCancelOnDamage, startCooldown: true);
        ctx.Defer();
    }
}
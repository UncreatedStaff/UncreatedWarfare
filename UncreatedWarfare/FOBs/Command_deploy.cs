﻿using System;
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
    private const string SYNTAX = "/deploy main -OR- /deploy <fob name>";
    private const string HELP = "Deploy to a point of interest such as a main base, FOB, VCP, or cache.";

    public DeployCommand() : base("deploy", EAdminType.MEMBER)
    {
        AddAlias("dep");
    }

    // todo use Deployment instead
    public override void Execute(CommandInteraction ctx)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ctx.AssertRanByPlayer();

        ctx.AssertGamemode<IFOBs>();

        ctx.AssertArgs(1, SYNTAX + " - " + HELP);

        if (ctx.MatchParameter(0, "cancel") && ctx.Caller.Player.TryGetPlayerData(out UCPlayerData comp) && comp.CurrentTeleportRequest != null)
        {
            comp.CancelTeleport();
            throw ctx.Reply(T.DeployCancelled);
        }

        if (Data.Is(out IRevives r) && r.ReviveManager.IsInjured(ctx.CallerID))
            throw ctx.Reply(T.DeployInjured);

        string destination = ctx.GetRange(0)!;

        UCPlayerData? c = ctx.Caller.Player.GetPlayerData(out _);
        if (c is null) throw ctx.SendUnknownError();

        ulong team = ctx.Caller.GetTeam();
        bool IsInMain = ctx.Caller.Player.IsInMain();
        bool IsInLobby = !IsInMain && TeamManager.LobbyZone.IsInside(ctx.Caller.Position);
        bool shouldCancelOnMove = !IsInMain;
        bool shouldCancelOnDamage = !IsInMain;

        if (CooldownManager.HasCooldown(ctx.Caller, ECooldownType.DEPLOY, out Cooldown cooldown))
            throw ctx.Reply(T.DeployCooldown, cooldown);

        if (!(IsInMain || IsInLobby))
        {
            if (CooldownManager.HasCooldown(ctx.Caller, ECooldownType.COMBAT, out Cooldown combatlog))
                throw ctx.Reply(T.DeployInCombat, combatlog);

            if (!Gamemode.Config.BarricadeInsurgencyCache.ValidReference(out Guid guid) || !(ctx.Caller.IsOnFOB(out _) || UCBarricadeManager.CountNearbyBarricades(guid, 10, ctx.Caller.Position, team) != 0))
                throw ctx.Reply(Data.Is<Insurgency>() ? T.DeployNotNearFOBInsurgency : T.DeployNotNearFOB);
        }

        if (!FOBManager.FindFOBByName(destination, team, out object? deployable))
        {
            if (destination.Equals("main", StringComparison.OrdinalIgnoreCase))
            {
                c.TeleportTo(team.GetBaseSpawnFromTeam(), FOBManager.Config.DeloyMainDelay, shouldCancelOnMove, false, team.GetBaseAngle());
                throw ctx.Defer();
            }
            if (destination.Equals("lobby", StringComparison.OrdinalIgnoreCase))
                throw ctx.Reply(T.DeployLobbyRemoved);
            else
                throw ctx.Reply(T.DeployableNotFound, destination);
        }

        if (deployable is FOB FOB)
        {
            if (FOB.Bunker == null)
            {
                ctx.Reply(T.DeployNoBunker, FOB);
                return;
            }
            if (FOB.IsBleeding)
            {
                ctx.Reply(T.DeployRadioDamaged, FOB);
                return;
            }
            if (FOB.NearbyEnemies.Count != 0)
            {
                ctx.Reply(T.DeployEnemiesNearby, FOB);
                return;
            }

            c.TeleportTo(FOB, FOBManager.Config.DeloyFOBDelay, shouldCancelOnMove);
            throw ctx.Defer();

        }
        else if (deployable is SpecialFOB special)
        {
            c.TeleportTo(special, FOBManager.Config.DeloyFOBDelay, shouldCancelOnMove);
            throw ctx.Defer();
        }
        else if (deployable is Cache cache)
        {
            if (cache.NearbyAttackers.Count != 0)
                throw ctx.Reply(T.DeployEnemiesNearby, cache);

            c.TeleportTo(cache, FOBManager.Config.DeloyFOBDelay, shouldCancelOnMove);
        }
        else
            throw ctx.Reply(T.DeployableNotFound, destination);
    }
}
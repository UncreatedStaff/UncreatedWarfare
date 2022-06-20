using SDG.Unturned;
using System;
using System.Threading.Tasks;
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

    public override void Execute(CommandInteraction ctx)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ctx.AssertRanByPlayer();

        ctx.AssertGamemode<IFOBs>();

        ctx.AssertArgs(1, SYNTAX + " - " + HELP);

        if (Data.Is(out IRevives r) && r.ReviveManager.DownedPlayers.ContainsKey(ctx.CallerID))
            throw ctx.Reply("deploy_e_injured");

        string destination = ctx.GetRange(0)!;

        UCPlayerData? c = ctx.Caller.Player.GetPlayerData(out _);
        if (c is null) throw ctx.SendUnknownError();

        ulong team = ctx.Caller.GetTeam();
        bool IsInMain = ctx.Caller.Player.IsInMain();
        bool IsInLobby = !IsInMain && TeamManager.LobbyZone.IsInside(ctx.Caller.Position);
        bool shouldCancelOnMove = !IsInMain;
        bool shouldCancelOnDamage = !IsInMain;

        if (CooldownManager.HasCooldown(ctx.Caller, ECooldownType.DEPLOY, out Cooldown cooldown))
            throw ctx.Reply("deploy_e_cooldown", cooldown.ToString());

        if (!(IsInMain || IsInLobby))
        {
            if (CooldownManager.HasCooldown(ctx.Caller, ECooldownType.COMBAT, out Cooldown combatlog))
                throw ctx.Reply("deploy_e_incombat", combatlog.ToString());

            if (!(ctx.Caller.IsOnFOB(out _) || UCBarricadeManager.CountNearbyBarricades(Gamemode.Config.Barricades.InsurgencyCacheGUID, 10, ctx.Caller.Position, team) != 0))
                throw ctx.Reply(Data.Is<Insurgency>() ? "deploy_e_notnearfob_ins" : "deploy_e_notnearfob");
        }

        if (!FOBManager.FindFOBByName(destination, team, out object? deployable))
        {
            if (destination.Equals("main", StringComparison.OrdinalIgnoreCase))
            {
                c.TeleportTo(team.GetBaseSpawnFromTeam(), FOBManager.Config.DeloyMainDelay, shouldCancelOnMove, false, team.GetBaseAngle());
                throw ctx.Defer();
            }
            if (destination.Equals("lobby", StringComparison.OrdinalIgnoreCase))
                throw ctx.Reply("deploy_lobby_removed");
            else
                throw ctx.Reply("deploy_e_fobnotfound", destination);
        }

        if (deployable is FOB FOB)
        {
            if (FOB.Bunker == null)
            {
                ctx.Reply("deploy_e_nobunker", FOB.Name);
                return;
            }
            if (FOB.IsBleeding)
            {
                ctx.Reply("deploy_e_damaged", FOB.Name);
                return;
            }
            if (FOB.NearbyEnemies.Count != 0)
            {
                ctx.Reply("deploy_e_enemiesnearby", FOB.Name);
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
                throw ctx.Reply("deploy_e_enemiesnearby", cache.Name);

            c.TeleportTo(cache, FOBManager.Config.DeloyFOBDelay, shouldCancelOnMove);
        }
        else
            throw ctx.Reply("deploy_e_fobnotfound", destination);
    }
}
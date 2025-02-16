using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Players.Tweaks;
public class PlayerChooseSpawnPointTweaks :
    IEventListener<PlayerChooseSpawnAfterLogin>,
    IEventListener<PlayerChooseSpawnAfterDeath>
{
    [EventListener(RequireActiveLayout = true)]
    public void HandleEvent(PlayerChooseSpawnAfterLogin e, IServiceProvider serviceProvider)
    {
        ZoneStore? zones = serviceProvider.GetService<ZoneStore>();
        ITeamManager<Team>? teamManager = serviceProvider.GetService<ITeamManager<Team>>();

        if (zones == null || teamManager == null) // do not modify the player's spawn if zones or teams are not supported
            return;

        Team lastPlayedTeam = teamManager.GetTeam(new CSteamID((ulong)e.PlayerSave.TeamId));

        // spawn back on the battlefield where they last logged off if they are rejoining a round and do not need a new spawn point
        bool shouldSpawnInTheField = !e.JoiningIntoNewRound && !e.NeedsNewSpawnPoint;
        if (shouldSpawnInTheField)
        {
            return;
        }

        // spawn at War Room if the player has a team and is rejoining the game after being killed
        // this is a relatively niche case - players usually be dead after logging in
        bool shouldSpawnAtMain = lastPlayedTeam != Team.NoTeam && !e.JoiningIntoNewRound && e.NeedsNewSpawnPoint;
        if (shouldSpawnAtMain)
        {
            Zone? warRoom = zones.SearchZone(ZoneType.WarRoom, lastPlayedTeam.Faction);
            if (warRoom == null) // ignore if we don't know what the player's war room is
                return;

            e.SpawnPoint = warRoom.Spawn;
            e.Yaw = warRoom.SpawnYaw;
            return;
        }

        // spawn at main if it's the player's first time joining the server or if they're joining into a new round
        bool shouldSpawnInLobby = e.FirstTimeJoiningServer || e.JoiningIntoNewRound;
        if (shouldSpawnInLobby)
        {
            Zone? lobby = zones.SearchZone(ZoneType.Lobby);
            if (lobby == null) // ignore if there is no apparent lobby
                return;

            e.SpawnPoint = lobby.Spawn;
            e.Yaw = lobby.SpawnYaw;
        }
    }

    [EventListener(RequireActiveLayout = true)]
    public void HandleEvent(PlayerChooseSpawnAfterDeath e, IServiceProvider serviceProvider)
    {
        ZoneStore? zones = serviceProvider.GetService<ZoneStore>();
        ITeamManager<Team>? teamManager = serviceProvider.GetService<ITeamManager<Team>>();

        if (zones == null || teamManager == null) // do not modify the player's spawn if zones or teams are not supported
            return;

        // respawn in main if the player has a team
        if (e.Player.Team != Team.NoTeam)
        {
            Zone? main = zones.SearchZone(ZoneType.MainBase, e.Player.Team.Faction);
            if (main == null) // ignore if we don't know what the player's main base is
                return;

            e.SpawnPoint = main.Spawn;
            e.Yaw = main.SpawnYaw;
            return;
        }
        else
        {
            // otherwise, respawn in lobby

            Zone? lobby = zones.SearchZone(ZoneType.Lobby);
            if (lobby == null) // ignore if there is no apparent lobby
                return;

            e.SpawnPoint = lobby.Spawn;
            e.Yaw = lobby.SpawnYaw;
            return;
        }
        
    }
}

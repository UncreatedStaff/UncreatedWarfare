using System;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Players.Tweaks;

public class PlayerChooseSpawnPointTweaks :
    IEventListener<PlayerChooseSpawnAfterLogin>,
    IEventListener<PlayerChooseSpawnAfterDeath>
{
    private readonly Layout _layout;
    private readonly ZoneStore _zoneStore;

    public PlayerChooseSpawnPointTweaks(Layout layout, ZoneStore zoneStore)
    {
        _layout = layout;
        _zoneStore = zoneStore;
    }

    [EventListener(RequireActiveLayout = true)]
    public void HandleEvent(PlayerChooseSpawnAfterLogin e, IServiceProvider serviceProvider)
    {
        Team lastPlayedTeam = _layout.TeamManager.GetTeam(new CSteamID(e.PlayerSave.TeamId));

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
            Zone? warRoom = _zoneStore.SearchZone(ZoneType.WarRoom, lastPlayedTeam.Faction);
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
            Zone? lobby = _zoneStore.SearchZone(ZoneType.Lobby);
            if (lobby == null) // ignore if there is no apparent lobby
                return;

            e.SpawnPoint = lobby.Spawn;
            e.Yaw = lobby.SpawnYaw;
        }
    }

    [EventListener(RequireActiveLayout = true)]
    public void HandleEvent(PlayerChooseSpawnAfterDeath e, IServiceProvider serviceProvider)
    {
        // respawn in main if the player has a team
        if (e.Player.Team != Team.NoTeam)
        {
            Zone? warRoom = _zoneStore.SearchZone(ZoneType.WarRoom, e.Player.Team.Faction);
            if (warRoom == null) // ignore if we don't know what the player's main base is
                return;

            e.SpawnPoint = warRoom.Spawn;
            e.Yaw = warRoom.SpawnYaw;
            return;
        }
        else
        {
            // otherwise, respawn in lobby

            Zone? lobby = _zoneStore.SearchZone(ZoneType.Lobby);
            if (lobby == null) // ignore if there is no apparent lobby
                return;

            e.SpawnPoint = lobby.Spawn;
            e.Yaw = lobby.SpawnYaw;
            return;
        }
        
    }
}

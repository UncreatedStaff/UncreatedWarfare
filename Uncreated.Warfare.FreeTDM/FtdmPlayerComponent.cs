using System;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.FreeTeamDeathmatch;

[PlayerComponent]
internal sealed class FtdmPlayerComponent : IPlayerComponent, IEventListener<PlayerDied>
{
    public required WarfarePlayer Player { get; init; }

    public float LastInPlayArea { get; internal set; }
    public float LastInFriendlySpawn { get; internal set; }
    public float LastOutOfBoundsUIUpdate { get; internal set; }
    public bool HasExitedSpawnSinceRespawned { get; internal set; }

    void IPlayerComponent.Init(IServiceProvider serviceProvider, bool isOnJoin)
    {
        ResetOnDeath();
        HasExitedSpawnSinceRespawned = false;
    }

    internal void ResetOnDeath()
    {
        HasExitedSpawnSinceRespawned = false;
        LastInPlayArea = float.NaN;
        LastOutOfBoundsUIUpdate = float.NaN;
        LastInFriendlySpawn = float.NaN;
    }

    void IEventListener<PlayerDied>.HandleEvent(PlayerDied e, IServiceProvider serviceProvider)
    {
        ResetOnDeath();
    }
}

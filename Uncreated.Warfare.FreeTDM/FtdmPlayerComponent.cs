using System;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.FreeTeamDeathmatch;

[PlayerComponent]
internal sealed class FtdmPlayerComponent : IPlayerComponent, IEventListener<PlayerDied>
{
    public float LastInPlayArea { get; internal set; }
    public float LastInFriendlySpawn { get; internal set; }
    public float LastOutOfBoundsUIUpdate { get; internal set; }
    public bool HasExitedSpawnSinceRespawned { get; internal set; }
#nullable disable

    public WarfarePlayer Player { get; private set; }

#nullable restore

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

    WarfarePlayer IPlayerComponent.Player { get => Player; set => Player = value; }

    void IEventListener<PlayerDied>.HandleEvent(PlayerDied e, IServiceProvider serviceProvider)
    {
        ResetOnDeath();
    }
}

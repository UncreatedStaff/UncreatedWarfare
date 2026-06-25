using System;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Squads;

[PlayerComponent]
internal class SquadPlayerComponent : IPlayerComponent
{
    public required WarfarePlayer Player { get; init; }
    public Squad? Squad { get; private set; }
    public void ChangeSquad(Squad newSquad)
    {
        Squad = newSquad;
    }
    public void ClearSquad()
    {
        Squad = null;
    }

    void IPlayerComponent.Init(IServiceProvider serviceProvider, bool isOnJoin) { }
}
using System;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Squads;

internal class SquadPlayerComponent : IPlayerComponent
{
    public WarfarePlayer Player { get; private set; }
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
    WarfarePlayer IPlayerComponent.Player { get => Player; set => Player = value; }
}
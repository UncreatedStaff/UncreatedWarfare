using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Squads;
internal class SquadPlayerComponent : IPlayerComponent
{
    private SquadManager _squadManager = null!;
    public WarfarePlayer Player { get; private set; }

    public Squad? Squad { get; private set; }

    void IPlayerComponent.Init(IServiceProvider serviceProvider, bool isOnJoin)
    {
        _squadManager = serviceProvider.GetRequiredService<SquadManager>();
    }

    WarfarePlayer IPlayerComponent.Player { get => Player; set => Player = value; }
}

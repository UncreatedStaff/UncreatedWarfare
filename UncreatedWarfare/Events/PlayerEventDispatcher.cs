using Microsoft.Extensions.DependencyInjection;
using System;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Events;

/// <summary>
/// Handles dispatching instance events from players.
/// </summary>
[PlayerComponent]
internal class PlayerEventDispatcher : IPlayerComponent, IDisposable
{
    private DroppedItemTracker _droppedItemTracker = null!;
    public WarfarePlayer Player { get; private set; }

    void IPlayerComponent.Init(IServiceProvider serviceProvider, bool isOnJoin)
    {
        _droppedItemTracker = serviceProvider.GetRequiredService<DroppedItemTracker>();

        if (!isOnJoin)
            return;

        Player player = Player.UnturnedPlayer;

        player.inventory.onDropItemRequested += _droppedItemTracker.InvokeDropItemRequested;
    }

    void IDisposable.Dispose()
    {
        Player player = Player.UnturnedPlayer;

        player.inventory.onDropItemRequested -= _droppedItemTracker.InvokeDropItemRequested;
    }

    WarfarePlayer IPlayerComponent.Player { get => Player; set => Player = value; }
}

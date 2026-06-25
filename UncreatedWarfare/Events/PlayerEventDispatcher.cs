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
    private DroppedItemTracker? _droppedItemTracker;

    public required WarfarePlayer Player { get; init; }

    void IPlayerComponent.Init(IServiceProvider serviceProvider, bool isOnJoin)
    {
        Player player = Player.UnturnedPlayer;
        if (_droppedItemTracker != null)
        {
            player.inventory.onDropItemRequested -= _droppedItemTracker.InvokeDropItemRequested;
        }

        _droppedItemTracker = serviceProvider.GetRequiredService<DroppedItemTracker>();
        player.inventory.onDropItemRequested += _droppedItemTracker.InvokeDropItemRequested;
    }

    void IDisposable.Dispose()
    {
        if (_droppedItemTracker == null)
            return;
        
        Player.UnturnedPlayer.inventory.onDropItemRequested -= _droppedItemTracker.InvokeDropItemRequested;
        _droppedItemTracker = null;
    }
}
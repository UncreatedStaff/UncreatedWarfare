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

#nullable disable
    public WarfarePlayer Player { get; private set; }
#nullable restore

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

    WarfarePlayer IPlayerComponent.Player { get => Player; set => Player = value; }
}
using System.Collections.Generic;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Services;

namespace Uncreated.Warfare.Events.ListenerProviders;

/// <summary>
/// Includes <see cref="IPlayerComponent"/>'s as event listeners.
/// </summary>
public class PlayerComponentListenerProvider : IEventListenerProvider
{
    private readonly IPlayerService _playerService;

    public PlayerComponentListenerProvider(IPlayerService playerService)
    {
        _playerService = playerService;
    }

    public void AppendListeners<TEventArgs>(TEventArgs args, List<object> listeners) where TEventArgs : class
    {
        if (args is IPlayerEvent playerEvent)
        {
            WarfarePlayer player = playerEvent.Player;
            if (!player.IsOnline)
                return;

            foreach (IPlayerComponent component in player.Components)
            {
                if (component is IEventListener<TEventArgs> el)
                    listeners.Add(el);
                if (component is IAsyncEventListener<TEventArgs> ael)
                    listeners.Add(ael);
            }

            return;
        }

        foreach (WarfarePlayer player in _playerService.OnlinePlayers)
        {
            foreach (IPlayerComponent component in player.Components)
            {
                if (component is IEventListener<TEventArgs> el)
                    listeners.Add(el);
                if (component is IAsyncEventListener<TEventArgs> ael)
                    listeners.Add(ael);
            }
        }
    }
}

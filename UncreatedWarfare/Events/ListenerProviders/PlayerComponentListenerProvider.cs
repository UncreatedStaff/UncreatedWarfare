using System.Collections.Generic;
using System.Linq;
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

    public IEnumerable<IAsyncEventListener<TEventArgs>> EnumerateAsyncListeners<TEventArgs>(TEventArgs args)
    {
        if (args is IPlayerEvent playerEvent)
        {
            return _playerService.OnlinePlayers
                .Where(x => x.Equals(playerEvent.Player))
                .SelectMany(x => x.Components.OfType<IAsyncEventListener<TEventArgs>>());
        }

        return _playerService.OnlinePlayers
            .SelectMany(x => x.Components.OfType<IAsyncEventListener<TEventArgs>>());
    }

    public IEnumerable<IEventListener<TEventArgs>> EnumerateNormalListeners<TEventArgs>(TEventArgs args)
    {
        if (args is IPlayerEvent playerEvent)
        {
            return _playerService.OnlinePlayers
                .Where(x => x.Equals(playerEvent.Player))
                .SelectMany(x => x.Components.OfType<IEventListener<TEventArgs>>());
        }

        return _playerService.OnlinePlayers.SelectMany(x => x.Components.OfType<IEventListener<TEventArgs>>());
    }
}

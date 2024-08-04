using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Services;

namespace Uncreated.Warfare.Events.ListenerProviders;

/// <summary>
/// Includes <see cref="IPlayerComponent"/>'s as event listeners.
/// </summary>
public class PlayerComponentListenerProvider : IEventListenerProvider
{
    private readonly PlayerService _playerService;

    public PlayerComponentListenerProvider(PlayerService playerService)
    {
        _playerService = playerService;
    }

    public IEnumerable<IAsyncEventListener<TEventArgs>> EnumerateAsyncListeners<TEventArgs>()
    {
        return _playerService.OnlinePlayers.SelectMany(x => x.Components.OfType<IAsyncEventListener<TEventArgs>>());
    }

    public IEnumerable<IEventListener<TEventArgs>> EnumerateNormalListeners<TEventArgs>()
    {
        return _playerService.OnlinePlayers.SelectMany(x => x.Components.OfType<IEventListener<TEventArgs>>());
    }
}

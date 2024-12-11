using System.Collections.Generic;
using Uncreated.Warfare.Events.Models;

namespace Uncreated.Warfare.Services;

/// <summary>
/// Acts as an extra 'service provider' for events to be dispatched to.
/// </summary>
public interface IEventListenerProvider
{
    IEnumerable<IAsyncEventListener<TEventArgs>> EnumerateAsyncListeners<TEventArgs>(TEventArgs args) where TEventArgs : class;
    IEnumerable<IEventListener<TEventArgs>> EnumerateNormalListeners<TEventArgs>(TEventArgs args) where TEventArgs : class;
}
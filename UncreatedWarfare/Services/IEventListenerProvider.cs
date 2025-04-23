using System.Collections.Generic;

namespace Uncreated.Warfare.Services;

/// <summary>
/// Acts as an extra 'service provider' for events to be dispatched to.
/// </summary>
public interface IEventListenerProvider
{
    void AppendListeners<TEventArgs>(TEventArgs args, List<object> listeners) where TEventArgs : class;
}
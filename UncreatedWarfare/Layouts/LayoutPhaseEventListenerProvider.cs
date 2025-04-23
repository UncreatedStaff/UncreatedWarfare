using System.Collections.Generic;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Layouts.Phases;
using Uncreated.Warfare.Services;

namespace Uncreated.Warfare.Layouts;
internal class LayoutPhaseEventListenerProvider : IEventListenerProvider
{
    private readonly Layout _layout;

    public LayoutPhaseEventListenerProvider(Layout layout)
    {
        _layout = layout;
    }

    // allows the current phase to handle events
    void IEventListenerProvider.AppendListeners<TEventArgs>(TEventArgs args, List<object> listeners)
    {
        foreach (ILayoutPhase phase in _layout.Phases)
        {
            if (phase is IEventListener<TEventArgs> el)
                listeners.Add(el);
            if (phase is IAsyncEventListener<TEventArgs> ael)
                listeners.Add(ael);
        }
    }
}

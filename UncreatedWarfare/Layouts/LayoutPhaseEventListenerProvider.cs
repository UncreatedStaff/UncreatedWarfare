using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Events.Models;
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
    IEnumerable<IEventListener<TEventArgs>> IEventListenerProvider.EnumerateNormalListeners<TEventArgs>(TEventArgs args)
    {
        return _layout.Phases.OfType<IEventListener<TEventArgs>>();
    }

    IEnumerable<IAsyncEventListener<TEventArgs>> IEventListenerProvider.EnumerateAsyncListeners<TEventArgs>(TEventArgs args)
    {
        return _layout.Phases.OfType<IAsyncEventListener<TEventArgs>>();
    }
}

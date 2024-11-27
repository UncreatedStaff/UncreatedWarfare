using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Services;

namespace Uncreated.Warfare.Layouts;
internal class LayoutPhaseEventListenerProvider : IEventListenerProvider
{
    private readonly WarfareModule _module;
    public LayoutPhaseEventListenerProvider(WarfareModule module)
    {
        _module = module;
    }

    // allows the current phase to handle events
    IEnumerable<IEventListener<TEventArgs>> IEventListenerProvider.EnumerateNormalListeners<TEventArgs>(TEventArgs args)
    {
        return _module.IsLayoutActive()
            ? _module.GetActiveLayout().Phases.OfType<IEventListener<TEventArgs>>()
            : Enumerable.Empty<IEventListener<TEventArgs>>();
    }

    IEnumerable<IAsyncEventListener<TEventArgs>> IEventListenerProvider.EnumerateAsyncListeners<TEventArgs>(TEventArgs args)
    {
        return _module.IsLayoutActive()
            ? _module.GetActiveLayout().Phases.OfType<IAsyncEventListener<TEventArgs>>()
            : Enumerable.Empty<IAsyncEventListener<TEventArgs>>();
    }
}

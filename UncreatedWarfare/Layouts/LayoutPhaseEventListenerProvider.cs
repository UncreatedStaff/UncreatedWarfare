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
        return _module.IsLayoutActive() && _module.GetActiveLayout().ActivePhase is IEventListener<TEventArgs> phase
            ? Enumerable.Repeat(phase, 1)
            : Enumerable.Empty<IEventListener<TEventArgs>>();
    }

    IEnumerable<IAsyncEventListener<TEventArgs>> IEventListenerProvider.EnumerateAsyncListeners<TEventArgs>(TEventArgs args)
    {
        return _module.IsLayoutActive() && _module.GetActiveLayout().ActivePhase is IAsyncEventListener<TEventArgs> phase
            ? Enumerable.Repeat(phase, 1)
            : Enumerable.Empty<IAsyncEventListener<TEventArgs>>();
    }
}

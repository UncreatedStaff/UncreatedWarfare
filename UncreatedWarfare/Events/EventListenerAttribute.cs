using System;

namespace Uncreated.Warfare.Events;

[AttributeUsage(AttributeTargets.Method)]
public sealed class EventListenerAttribute : Attribute
{
    internal bool HasRequiredMainThread;
    private bool _requiresMainThread;

    /// <summary>
    /// Highest priority possible, ensures the event listener runs without switching contexts from the original event.
    /// </summary>
    /// <remarks>Not supported on <see cref="IAsyncEventListener{TEventArgs}"/>. An error will be thrown.</remarks>
    public bool MustRunInstantly { get; set; }

    /// <summary>
    /// Positive values run before negative values. 0 is neutral.
    /// </summary>
    /// <remarks>Defaults to 0.</remarks>
    public int Priority { get; set; }

    /// <summary>
    /// If this listener must be invoked on the main thread.
    /// </summary>
    /// <remarks>Defaults to <see langword="true"/> for <see cref="IEventListener{TEventArgs}"/>'s and <see langword="false"/> for <see cref="IAsyncEventListener{TEventArgs}"/>'s.</remarks>
    public bool RequiresMainThread
    {
        get => _requiresMainThread;
        set
        {
            _requiresMainThread = value;
            HasRequiredMainThread = true;
        }
    }
}

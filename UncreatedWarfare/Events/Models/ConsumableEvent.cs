using System;

namespace Uncreated.Warfare.Events.Models;

/// <summary>
/// A type of event that can stop other event listeners from running but doesn't exactly 'cancel' the event.
/// </summary>
public class ConsumableEvent : ICancellable
{
    public ConsumableEvent() { }

    public ConsumableEvent(bool isConsumed)
    {
        IsCancelled = isConsumed;
    }

    /// <summary>
    /// Keeps future events from running.
    /// </summary>
    public void Consume()
    {
        IsCancelled = true;
    }

    /// <inheritdoc />
    public bool IsCancelled { get; set; }

    /// <inheritdoc />
    bool ICancellable.IsActionCancelled => false;
    void ICancellable.Cancel(bool cancelAction) => throw new NotSupportedException();
    void ICancellable.CancelAction() => throw new NotSupportedException();
    void ICancellable.ResumeAction() => throw new NotSupportedException();
}

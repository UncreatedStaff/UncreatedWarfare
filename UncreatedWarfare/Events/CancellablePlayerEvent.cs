namespace Uncreated.Warfare.Events;
public class CancellablePlayerEvent : PlayerEvent, ICancellable
{
    /// <inheritdoc />
    public virtual bool IsCancelled { get; private set; }

    /// <inheritdoc />
    public virtual bool IsActionCancelled { get; private set; }

    public CancellablePlayerEvent() { }
    public CancellablePlayerEvent(bool shouldAllow)
    {
        if (shouldAllow)
            return;

        IsCancelled = true;
        IsActionCancelled = true;
    }

    /// <inheritdoc />
    public virtual void Cancel(bool cancelAction = true)
    {
        IsCancelled = true;
        IsActionCancelled = cancelAction;
    }
}

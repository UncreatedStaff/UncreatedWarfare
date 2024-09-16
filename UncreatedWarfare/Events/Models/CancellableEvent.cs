namespace Uncreated.Warfare.Events.Models;
public class CancellableEvent : ICancellable
{
    /// <inheritdoc />
    public virtual bool IsCancelled { get; private set; }

    /// <inheritdoc />
    public virtual bool IsActionCancelled { get; private set; }

    public CancellableEvent() { }

    public CancellableEvent(bool shouldAllow)
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

    /// <inheritdoc />
    public virtual void CancelAction()
    {
        IsActionCancelled = true;
    }

    /// <inheritdoc />
    public virtual void ResumeAction()
    {
        IsActionCancelled = true;
    }
}

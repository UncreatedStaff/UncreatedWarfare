namespace Uncreated.Warfare.Events;
public class BreakableEvent : EventState
{
    private bool _hasBroken = false;
    public override bool CanContinue => !_hasBroken;
    public BreakableEvent() { }
    public BreakableEvent(bool shouldAllow)
    {
        if (!shouldAllow) Break();
    }
    public virtual void Break()
    {
        _hasBroken = true;
    }
}

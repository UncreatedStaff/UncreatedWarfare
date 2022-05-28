using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

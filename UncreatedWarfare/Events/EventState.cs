using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Events;
public abstract class EventState
{
    public virtual bool CanContinue { get => true; }
}

using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Layouts;

namespace Uncreated.Warfare.Vehicles.Spawners.Delays;
public interface ILayoutDelay
{
    TimeSpan GetTimeLeft(Layout currentLayout);
}

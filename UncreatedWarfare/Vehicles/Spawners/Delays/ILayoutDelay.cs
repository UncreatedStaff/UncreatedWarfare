using System;

namespace Uncreated.Warfare.Vehicles.Spawners.Delays;

public interface ILayoutDelay<in TContext> where TContext : LayoutDelayContext
{
    TimeSpan GetTimeLeft(TContext context);
}
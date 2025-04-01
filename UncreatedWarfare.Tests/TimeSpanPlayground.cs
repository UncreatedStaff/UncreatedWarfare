using NUnit.Framework;
using System;

namespace Uncreated.Warfare.Tests;

internal class TimeSpanPlayground
{
    [Test]
    public void Test()
    {
        DateTime timeDestroyed = DateTime.Now.AddSeconds(-15);
        DateTime timeIdleStarted = DateTime.Now.AddSeconds(-16);
        TimeSpan timeSpentDestroyed = DateTime.Now - timeDestroyed;
        TimeSpan timeSpentIdle = DateTime.Now - timeIdleStarted;


        if (timeSpentIdle >= TimeSpan.Zero)
        {
            if (timeSpentIdle <= timeSpentDestroyed)
                timeSpentDestroyed = timeSpentDestroyed.Subtract(timeSpentIdle);
            else
                timeSpentDestroyed = TimeSpan.Zero;
        }

        Console.WriteLine($"Time spent destroyed: {timeSpentDestroyed}");
    }
}
using NUnit.Framework;
using System;
using System.Threading;
using Uncreated.Warfare.Profiling;

namespace Uncreated.Warfare.Tests;

[TestFixture]
public class ProfilerUtilTest
{
    [Test]
    public void TestProfile()
    {
        ProfilerUtil.Enabled = true;

        ProfileMethod(1000);
        ProfileMethod(2000);

        ProfilerUtil.Flush();
    }

    private static void ProfileMethod(int sleep)
    {
        using IDisposable profiler = ProfilerUtil.Profile();

        Thread.Sleep(sleep);
    }
}

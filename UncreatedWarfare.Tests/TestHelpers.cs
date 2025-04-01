using SDG.Unturned;
using System.Reflection;
using System.Threading;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Tests;
internal class TestHelpers
{
    public static void SetupMainThread()
    {
        typeof(ThreadUtil).GetProperty("gameThread", BindingFlags.Static | BindingFlags.Public)!
            .GetSetMethod(true)!.Invoke(null, [ Thread.CurrentThread ]);

        GameThread.Setup();
    }
}
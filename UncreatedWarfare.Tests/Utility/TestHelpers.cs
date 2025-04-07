extern alias JetBrains;
using JetBrains::JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SDG.Unturned;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Tests.Utility;
internal class TestHelpers
{
    public static void SetupMainThread()
    {
        typeof(ThreadUtil).GetProperty("gameThread", BindingFlags.Static | BindingFlags.Public)!
            .GetSetMethod(true)!.Invoke(null, [ Thread.CurrentThread ]);

        GameThread.Setup();
    }

    public static async Task<WarfarePlayer> AddPlayer(uint id, IServiceProvider serviceProvider, [CanBeNull] Action<WarfarePlayer> modification = null)
    {
        TestPlayerService playerService = serviceProvider.GetRequiredService<TestPlayerService>();

        WarfarePlayer player = new WarfarePlayer(
            id,
            serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger($"Player {id}"),
            serviceProvider,
            modification
        );

        await playerService.AddPlayer(player);

        return player;
    }

}
using System;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Services;

namespace Uncreated.Warfare.Layouts.Seeding;

internal class SeedingPlayerCountMonitor : IEventListener<PlayerJoined>, IEventListener<PlayerLeft>, ILayoutHostedService
{
    public UniTask StartAsync(CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public UniTask StopAsync(CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public void HandleEvent(PlayerJoined e, IServiceProvider serviceProvider)
    {
        throw new NotImplementedException();
    }

    public void HandleEvent(PlayerLeft e, IServiceProvider serviceProvider)
    {
        throw new NotImplementedException();
    }
}

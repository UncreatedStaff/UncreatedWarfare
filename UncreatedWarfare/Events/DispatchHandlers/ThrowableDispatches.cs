using Uncreated.Warfare.Events.Models.Throwables;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Events;

partial class EventDispatcher
{
    /// <summary>
    /// Invoked when a throwable is spawned.
    /// </summary>
    private void OnThrowableSpawned(UseableThrowable useable, GameObject throwable)
    {
        using IDisposable? profiler = ProfilerUtil.Profile();

        WarfarePlayer warfarePlayer = _playerService.GetOnlinePlayer(useable.player);

        ThrowableSpawned args = new ThrowableSpawned {Player = warfarePlayer, UseableThrowable = useable, Object = throwable};

        _ = DispatchEventAsync(args, CancellationToken.None, allowAsync: false);
    }
}

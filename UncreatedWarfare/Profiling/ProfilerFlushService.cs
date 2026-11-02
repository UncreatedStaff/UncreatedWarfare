#if PROFILING
using System;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.Timing;

namespace Uncreated.Warfare.Profiling;

internal sealed class ProfilerFlushService : IHostedService
{
    private readonly ILoopTickerFactory _loopTickerFactory;
    private readonly ILogger<ProfilerFlushService> _logger;
    private ILoopTicker? _loopTicker;

    public ProfilerFlushService(ILoopTickerFactory loopTickerFactory, ILogger<ProfilerFlushService> logger)
    {
        _loopTickerFactory = loopTickerFactory;
        _logger = logger;
    }

    UniTask IHostedService.StartAsync(CancellationToken token)
    {
        Interlocked.Exchange(
            ref _loopTicker,
            _loopTickerFactory.CreateTicker(TimeSpan.FromMinutes(5), false, false, OnTick)
        )?.Dispose();
        return UniTask.CompletedTask;
    }

    UniTask IHostedService.StopAsync(CancellationToken token)
    {
        Interlocked.Exchange(ref _loopTicker, null)?.Dispose();
        return UniTask.CompletedTask;
    }

    private void OnTick(ILoopTicker ticker, TimeSpan timeSinceStart, TimeSpan deltaTime)
    {
        _logger.LogTrace("Flushing profiler data from ticker...");

        if (GameThread.IsCurrent)
        {
            Task.Run(ProfilerUtil.Flush);
        }
        else
        {
            ProfilerUtil.Flush();
        }
    }
}
#endif
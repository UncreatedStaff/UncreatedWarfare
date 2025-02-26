using System;
using System.Collections.Generic;
using System.Text;
using Uncreated.Warfare.Services;

namespace Uncreated.Warfare.Tweaks;
internal class QueueShutdownOnUnturnedUpdate : IHostedService
{
    private readonly ILogger<QueueShutdownOnUnturnedUpdate> _logger;
    private readonly WarfareModule _module;

    public QueueShutdownOnUnturnedUpdate(ILogger<QueueShutdownOnUnturnedUpdate> logger, WarfareModule module)
    {
        _logger = logger;
        _module = module;
    }

    /// <inheritdoc />
    public UniTask StartAsync(CancellationToken token)
    {
        GameUpdateMonitor.OnGameUpdateDetected += OnUpdateDetected;
        return UniTask.CompletedTask;
    }

    /// <inheritdoc />
    public UniTask StopAsync(CancellationToken token)
    {
        GameUpdateMonitor.OnGameUpdateDetected -= OnUpdateDetected;
        return UniTask.CompletedTask;
    }

    private void OnUpdateDetected(string newVersion, ref bool shouldShutdown)
    {
        _logger.LogInformation($"New Unturned version detected: {newVersion}.");

        
    }
}

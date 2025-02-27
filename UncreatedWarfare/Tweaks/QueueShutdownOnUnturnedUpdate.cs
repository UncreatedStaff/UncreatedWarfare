using Uncreated.Warfare.Services;

namespace Uncreated.Warfare.Tweaks;

internal class QueueShutdownOnUnturnedUpdate : IHostedService
{
    private readonly ILogger<QueueShutdownOnUnturnedUpdate> _logger;
    private readonly WarfareLifetimeComponent _appLifetime;

    public QueueShutdownOnUnturnedUpdate(ILogger<QueueShutdownOnUnturnedUpdate> logger, WarfareLifetimeComponent appLifetime)
    {
        _logger = logger;
        _appLifetime = appLifetime;
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

        _appLifetime.QueueShutdownAtLayoutEnd($"update to Unturned v{newVersion}");
        shouldShutdown = false;
    }
}
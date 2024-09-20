namespace Uncreated.Warfare.Services;

/// <summary>
/// Runs at module startup and shutdown.
/// </summary>
public interface IHostedService
{
    /// <summary>
    /// Executes at module startup.
    /// </summary>
    UniTask StartAsync(CancellationToken token);

    /// <summary>
    /// Executes at module shutdown.
    /// </summary>
    UniTask StopAsync(CancellationToken token);
}

public interface ILevelHostedService : IHostedService
{
    /// <summary>
    /// Executes when the level finishes loading and all assets are loaded but before the first session starts.
    /// </summary>
    UniTask LoadLevelAsync(CancellationToken token);
}
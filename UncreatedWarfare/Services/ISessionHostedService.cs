namespace Uncreated.Warfare.Services;

/// <summary>
/// Runs at game startup and shutdown.
/// </summary>
public interface ISessionHostedService
{
    /// <summary>
    /// Executes at the beginning of every game.
    /// </summary>
    UniTask StartAsync(CancellationToken token);

    /// <summary>
    /// Executes at the end of every game.
    /// </summary>
    UniTask StopAsync(CancellationToken token);
}
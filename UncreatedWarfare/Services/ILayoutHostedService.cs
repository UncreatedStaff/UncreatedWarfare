namespace Uncreated.Warfare.Services;

/// <summary>
/// Runs at layout session startup and shutdown.
/// </summary>
public interface ILayoutHostedService
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
using Cysharp.Threading.Tasks;
using System.Threading;

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

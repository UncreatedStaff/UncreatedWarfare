using Microsoft.Extensions.Configuration;

namespace Uncreated.Warfare.Layouts.Phases;

/// <summary>
/// Instantly skipped phase.
/// </summary>
public class NullPhase : ILayoutPhase
{
    /// <inheritdoc />
    public bool IsActive => false;

    /// <inheritdoc />
    public IConfiguration Configuration { get; }

    public NullPhase(IConfiguration config)
    {
        Configuration = config;
    }

    /// <inheritdoc />
    public UniTask InitializePhaseAsync(CancellationToken token = default) => UniTask.CompletedTask;

    /// <inheritdoc />
    public UniTask BeginPhaseAsync(CancellationToken token = default) => UniTask.CompletedTask;

    /// <inheritdoc />
    public UniTask EndPhaseAsync(CancellationToken token = default) => UniTask.CompletedTask;
}
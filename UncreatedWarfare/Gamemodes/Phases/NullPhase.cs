using Cysharp.Threading.Tasks;
using System.Threading;

namespace Uncreated.Warfare.Gamemodes.Phases;

/// <summary>
/// Instantly skipped phase.
/// </summary>
public class NullPhase : ILayoutPhase
{
    /// <inheritdoc />
    public bool IsActive => false;

    /// <inheritdoc />
    public UniTask InitializePhaseAsync(CancellationToken token = default) => UniTask.CompletedTask;

    /// <inheritdoc />
    public UniTask BeginPhaseAsync(CancellationToken token = default) => UniTask.CompletedTask;

    /// <inheritdoc />
    public UniTask EndPhaseAsync(CancellationToken token = default) => UniTask.CompletedTask;
}
using Uncreated.Warfare.Layouts.Phases;

namespace Uncreated.Warfare.Services;

/// <summary>
/// Allows a service to listen for a specific phase to start and/or end.
/// </summary>
/// <typeparam name="TPhaseType">The base type of the phase.</typeparam>
public interface ILayoutPhaseListener<in TPhaseType> where TPhaseType : ILayoutPhase
{
    /// <summary>
    /// Runs after <see cref="ILayoutPhase.BeginPhaseAsync"/>.
    /// </summary>
    UniTask OnPhaseStarted(TPhaseType phase, CancellationToken token);

    /// <summary>
    /// Runs after <see cref="ILayoutPhase.EndPhaseAsync"/>.
    /// </summary>
    UniTask OnPhaseEnded(TPhaseType phase, CancellationToken token);
}
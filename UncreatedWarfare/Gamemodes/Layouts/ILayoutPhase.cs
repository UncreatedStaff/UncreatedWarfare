using Cysharp.Threading.Tasks;
using System.Threading;

namespace Uncreated.Warfare.Gamemodes.Layouts;

/// <summary>
/// Represents a phase of a <see cref="GameSession"/>.
/// </summary>
public interface ILayoutPhase
{
    /// <summary>
    /// If this phase is currently activated.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Activates the phase. This should happen just after the old phase ended if there was one.
    /// </summary>
    /// <remarks>Must set <see cref="IsActive"/> to <see langword="true"/> or the phase will be skipped.</remarks>
    UniTask BeginPhaseAsync(CancellationToken token = default);

    /// <summary>
    /// Deactivates the phase.
    /// </summary>
    UniTask EndPhaseAsync(CancellationToken token = default);
}

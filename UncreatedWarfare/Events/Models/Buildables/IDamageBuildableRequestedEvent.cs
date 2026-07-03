using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Events.Models.Structures;

namespace Uncreated.Warfare.Events.Models.Buildables;

/// <summary>
/// Invoked when a barricade or structure is about to be damaged (<see cref="DamageBarricadeRequested"/> and <see cref="DamageStructureRequested"/>).
/// </summary>
public interface IDamageBuildableRequestedEvent : ICancellable, IBaseBuildableDestroyedEvent
{
    /// <summary>
    /// The amount of damage to be done to the buildable.
    /// </summary>
    ushort PendingDamage { get; }
}
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Events.Models.Structures;

namespace Uncreated.Warfare.Events.Models.Buildables;

/// <summary>
/// Invoked after a barricade or structure is damaged (<see cref="BarricadeDamaged"/> and <see cref="StructureDamaged"/>).
/// </summary>
public interface IBuildableDamagedEvent : IBaseBuildableDestroyedEvent
{
    /// <summary>
    /// The damage done to the buildable.
    /// </summary>
    ushort Damage { get; }
}
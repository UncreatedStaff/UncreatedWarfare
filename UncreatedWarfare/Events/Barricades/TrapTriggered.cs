using Uncreated.Warfare.Events.Components;

namespace Uncreated.Warfare.Events.Barricades;

/// <summary>
/// Event listener args which handles a patch after trap triggered.
/// </summary>
public class TrapTriggered
{
    /// <summary>
    /// Player that placed the barricade.
    /// </summary>
    public required UCPlayer? BarricadeOwner { get; init; }

    /// <summary>
    /// The barricade's object and model data.
    /// </summary>
    public required BarricadeDrop Barricade { get; init; }

    /// <summary>
    /// The barricade's server-side data.
    /// </summary>
    public required BarricadeData ServersideData { get; init; }

    /// <summary>
    /// The trap component of the barricade.
    /// </summary>
    public required InteractableTrap Trap { get; init; }

    /// <summary>
    /// Object that triggered the trap.
    /// </summary>
    public required GameObject TriggerObject { get; init; }

    /// <summary>
    /// Collider that triggered the trap.
    /// </summary>
    public required Collider TriggerCollider { get; init; }

    /// <summary>
    /// Player that instsigated the trap triggering.
    /// </summary>
    public required UCPlayer? TriggeringPlayer { get; init; }

    /// <summary>
    /// Throwable that triggered the trap.
    /// </summary>
    public required ThrowableComponent? TriggeringThrowable { get; init; }

    /// <summary>
    /// Asset of the throwable that triggered the trap.
    /// </summary>
    public required ItemThrowableAsset? TriggeringThrowableAssset { get; init; }

    /// <summary>
    /// Zombie that triggered the trap.
    /// </summary>
    public required Zombie? TriggeringZombie { get; init; }

    /// <summary>
    /// Animal that triggered the trap.
    /// </summary>
    public required Animal? TriggeringAnimal { get; init; }
}
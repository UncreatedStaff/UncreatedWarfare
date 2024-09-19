using Uncreated.Warfare.Events.Components;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Barricades;

/// <summary>
/// Event listener args which handles a patch on trap triggered.
/// </summary>
[EventModel(SynchronizationContext = EventSynchronizationContext.Global, SynchronizedModelTags = [ "modify_inventory", "modify_world" ])]
public class TriggerTrapRequested : CancellableEvent
{
    /// <summary>
    /// Player that placed the barricade.
    /// </summary>
    public required WarfarePlayer? BarricadeOwner { get; init; }

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
    /// The team that triggered the landmine.
    /// </summary>
    public required Team? TriggeringTeam { get; init; }

    /// <summary>
    /// Player that instsigated the trap triggering.
    /// </summary>
    public required WarfarePlayer? TriggeringPlayer { get; init; }

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

    /// <summary>
    /// Damage done to players by the trap.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public required float PlayerDamage { get; set; }

    /// <summary>
    /// Damage done to zombies by the trap.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public required float ZombieDamage { get; set; }

    /// <summary>
    /// Damage done to animals by the trap.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public required float AnimalDamage { get; set; }

    /// <summary>
    /// If the trap explodes.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public required bool IsExplosive { get; set; }

    /// <summary>
    /// Distance in meters the explosion reaches.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public float ExplosiveRange { get; set; }

    /// <summary>
    /// Damage done to barricades by the explosion.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public float ExplosiveBarricadeDamage { get; set; }

    /// <summary>
    /// Damage done to structures by the explosion.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public float ExplosiveStructureDamage { get; set; }

    /// <summary>
    /// Damage done to vehicles by the explosion.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public float ExplosiveVehicleDamage { get; set; }

    /// <summary>
    /// Damage done to resource by the explosion.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public float ExplosiveResourceDamage { get; set; }

    /// <summary>
    /// Damage done to object by the explosion.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public float ExplosiveObjectDamage { get; set; }

    /// <summary>
    /// Speed at which entities are launched by an explosive.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public float ExplosiveLaunchSpeed { get; set; }

    /// <summary>
    /// Effect triggered when the trap explodes.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public EffectAsset? ExplosionEffect { get; set; }

    /// <summary>
    /// If a non-explosive trap should break the target's legs.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public bool ShouldBreakLegs { get; set; }

    /// <summary>
    /// Damage done to the non-explosive trap barricade by wear and tear.
    /// </summary>
    /// <remarks>This can be changed.</remarks>
    public float WearAndTearDamage { get; set; }
}
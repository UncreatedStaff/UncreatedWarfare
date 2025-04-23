using System;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events.Components;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Events.Models.Barricades;

/// <summary>
/// Event listener args which handles a patch after trap triggered.
/// </summary>
[EventModel(EventSynchronizationContext.Pure)]
public class TrapTriggered : IActionLoggableEvent
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

    /// <inheritdoc />
    public ActionLogEntry GetActionLogEntry(IServiceProvider serviceProvider, ref ActionLogEntry[]? multipleEntries)
    {
        BarricadeData serversideData = Barricade.GetServersideData();
        if (TriggeringThrowableAssset != null)
        {
            return new ActionLogEntry(ActionLogTypes.TrapTriggered,
                $"Barricade {AssetLink.ToDisplayString(Barricade.asset)} owned by {serversideData.owner} ({serversideData.group}) # {Barricade.instanceID} " +
                $"Triggered by throwable {AssetLink.ToDisplayString(TriggeringThrowableAssset)}",
                TriggeringPlayer
            );
        }
        if (TriggeringZombie != null)
        {
            return new ActionLogEntry(ActionLogTypes.BuildableSignChanged,
                $"Barricade {AssetLink.ToDisplayString(Barricade.asset)} owned by {serversideData.owner} ({serversideData.group}) # {Barricade.instanceID} triggered by zombie",
                TriggeringPlayer
            );
        }
        if (TriggeringAnimal != null)
        {
            return new ActionLogEntry(ActionLogTypes.BuildableSignChanged,
                $"Barricade {AssetLink.ToDisplayString(Barricade.asset)} owned by {serversideData.owner} ({serversideData.group}) # {Barricade.instanceID} " +
                $"triggered by animal {AssetLink.ToDisplayString(TriggeringAnimal.asset)}",
                TriggeringPlayer
            );
        }

        return new ActionLogEntry(ActionLogTypes.BuildableSignChanged,
            $"Barricade {AssetLink.ToDisplayString(Barricade.asset)} owned by {serversideData.owner} ({serversideData.group}) # {Barricade.instanceID}",
            TriggeringPlayer
        );
    }
}
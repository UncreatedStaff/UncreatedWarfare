using System;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Events.Logging;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Events.Models.Players;

/// <summary>
/// Handles <see cref="PlayerEquipment.OnPunch_Global"/>.
/// </summary>
[EventModel(EventSynchronizationContext.Pure)]
public class PlayerPunched : PlayerEvent, IActionLoggableEvent
{
    public required EPlayerPunch PunchType { get; init; }

    /// <summary>
    /// The input information about the punch's hit, or <see langword="null"/> if the player didn't hit anything.
    /// </summary>
    public required InputInfo? InputInfo { get; init; }

    /// <inheritdoc />
    public ActionLogEntry GetActionLogEntry(IServiceProvider serviceProvider, ref ActionLogEntry[]? multipleEntries)
    {
        if (InputInfo == null)
            return new ActionLogEntry(ActionLogTypes.Punch, PunchType == EPlayerPunch.LEFT ? "Left hand" : "Right hand", Player.Steam64.m_SteamID);

        return new ActionLogEntry(ActionLogTypes.Punch,
            $"{(PunchType == EPlayerPunch.LEFT ? "Left hand" : "Right hand")} - {ActionLoggerService.DescribeInput(InputInfo)}",
            Player.Steam64.m_SteamID
        );
    }
    
    /// <summary>
    /// Attempts to get a reference to the buildable that the player punched, if they punched one. Works for barricades and structures.
    /// </summary>
    public bool TryGetTargetBuildable([NotNullWhen(true)] out IBuildable? buildable)
    {
        GameThread.AssertCurrent();

        if (InputInfo == null || InputInfo.transform == null)
        {
            buildable = null;
            return false;
        }

        BarricadeDrop? barricade = BarricadeManager.FindBarricadeByRootTransform(InputInfo.transform);
        if (barricade != null)
        {
            buildable = new BuildableBarricade(barricade);
            return true;
        }

        StructureDrop? structure = StructureManager.FindStructureByRootTransform(InputInfo.transform);
        if (structure != null)
        {
            buildable = new BuildableStructure(structure);
            return true;
        }

        buildable = null;
        return false;
    }
}
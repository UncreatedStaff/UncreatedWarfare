using System.Collections.Generic;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Flags;
using Uncreated.Warfare.Layouts.Flags;
using Uncreated.Warfare.Patches;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Zones;

/// <summary>
/// Grid objects apply power to objects related to flags in the rotation.
/// <para>
/// For example, lights, gates in a parking lot, vending machines, etc may be enabled when the zone is objective or in rotation.
/// </para>
/// </summary>
public class ElectricalGridService : ILevelHostedService
{
    private readonly ILogger<ElectricalGridService> _logger;

    public bool Enabled { get; internal set; }

    public ElectricalGridService(ILogger<ElectricalGridService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public UniTask LoadLevelAsync(CancellationToken token)
    {
        if (ElectricalGridCalculationPatches.Failed)
        {
            Enabled = false;
            return UniTask.CompletedTask;
        }

        if (!Level.info.configData.Has_Global_Electricity)
        {
            _logger.LogWarning("Level does not have global electricity enabled, electrical grid effects will not work!");
            Enabled = false;
        }
        else
        {
            Enabled = true;
        }

        return UniTask.CompletedTask;
    }

    internal bool IsInteractableEnabled(IFlagRotationService flagRotation, Interactable interactable)
    {
        if (flagRotation.GridBehaivor == ElectricalGridBehaivor.AllEnabled)
            return true;
        if (flagRotation.GridBehaivor == ElectricalGridBehaivor.Disabled)
            return false;

        if (interactable is not InteractableObject powerObject)
        {
            return IsPointInRotation(flagRotation, interactable.transform.position);
        }

        ObjectInfo obj = LevelObjectUtility.FindObject(powerObject.transform);
        if (!obj.HasValue)
            return false;

        uint instId = obj.Object.instanceID;
        return IsRegisteredGridObject(flagRotation, instId);
    }

    private static bool IsRegisteredGridObject(IFlagRotationService flagRotation, uint instId)
    {
        IEnumerable<FlagObjective> rotation = flagRotation.GridBehaivor == ElectricalGridBehaivor.EnabledWhenInRotation
            ? flagRotation.EnumerateObjectives()
            : flagRotation.ActiveFlags;

        foreach (FlagObjective zoneCluster in rotation)
        {
            if (zoneCluster.Region.Primary.Zone.GridObjects.Contains(instId))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPointInRotation(IFlagRotationService flagRotation, Vector3 position)
    {
        IEnumerable<FlagObjective> rotation = flagRotation.GridBehaivor == ElectricalGridBehaivor.EnabledWhenInRotation
            ? flagRotation.EnumerateObjectives()
            : flagRotation.ActiveFlags;

        foreach (FlagObjective zoneCluster in rotation)
        {
            if (zoneCluster.Region.TestPoint(position))
            {
                return true;
            }
        }

        return false;
    }
}
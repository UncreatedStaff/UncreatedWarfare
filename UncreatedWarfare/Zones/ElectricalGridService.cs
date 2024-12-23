using DanielWillett.ReflectionTools;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Flags;
using Uncreated.Warfare.Layouts;
using Uncreated.Warfare.Layouts.Flags;
using Uncreated.Warfare.Layouts.Teams;
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
public class ElectricalGridService : ILevelHostedService, IEventListener<FlagObjectiveChanged>, ILayoutHostedService
{
    private readonly ILogger<ElectricalGridService> _logger;
    private readonly WarfareModule _module;
    private readonly ZoneStore _zoneStore;

    private static readonly Action<InteractablePower>? RefreshIsConnectedToPower =
        Accessor.GenerateInstanceCaller<InteractablePower, Action<InteractablePower>>("RefreshIsConnectedToPower");

    public bool Enabled { get; internal set; }

    public ElectricalGridService(ILogger<ElectricalGridService> logger, WarfareModule module, ZoneStore zoneStore)
    {
        _logger = logger;
        _module = module;
        _zoneStore = zoneStore;
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

    void IEventListener<FlagObjectiveChanged>.HandleEvent(FlagObjectiveChanged e, IServiceProvider serviceProvider)
    {
        if (!Enabled || !_module.IsLayoutActive())
            return;

        IFlagRotationService? rotationService = _module.GetActiveLayout().ServiceProvider.ResolveOptional<IFlagRotationService>();

        if (rotationService is not { GridBehaivor: ElectricalGridBehaivor.EnabledWhenObjective })
        {
            return;
        }

        if (e.OldObjective != null)
        {
            SetPowerForAllGrid(e.OldObjective.Region.Primary.Zone, false);
        }
        if (e.NewObjective != null)
        {
            SetPowerForAllGrid(e.NewObjective.Region.Primary.Zone, true);
        }

        CheckPowerForAllBarricades();
    }

    private static void CheckPowerForAllBarricades()
    {
        if (RefreshIsConnectedToPower == null)
            return;

        foreach (BarricadeInfo barricade in BarricadeUtility.EnumerateBarricades())
        {
            if (barricade.Drop.interactable is InteractablePower power)
                RefreshIsConnectedToPower(power);
        }
    }

    private static void SetPowerForAllGrid(Zone zone, bool state)
    {
        Vector3 c = zone.Center;
        foreach (uint gridObject in zone.GridObjects)
        {
            ObjectInfo obj = LevelObjectUtility.FindObject(gridObject, c);

            if (!obj.HasValue)
            {
                continue;
            }

            InteractableObject intx = obj.Object.interactable;
            if (intx is null)
                continue;

            if (intx.objectAsset.interactabilityHint is EObjectInteractabilityHint.FIRE or EObjectInteractabilityHint.GENERATOR or EObjectInteractabilityHint.SWITCH)
            {
                ObjectManager.forceObjectBinaryState(obj.Object.transform, state);
            }

            RefreshIsConnectedToPower?.Invoke(intx);
        }
    }

    UniTask ILayoutHostedService.StartAsync(CancellationToken token)
    {
        Layout layout = _module.GetActiveLayout();

        IFlagRotationService? rotationService = layout.ServiceProvider.ResolveOptional<IFlagRotationService>();

        foreach (Team team in layout.TeamManager.AllTeams)
        {
            Zone? zone = _zoneStore.SearchZone(ZoneType.MainBase, team.Faction);
            if (zone == null)
                continue;

            SetPowerForAllGrid(zone, true);
        }

        foreach (Zone zone in _zoneStore.Zones.Where(x => x.Type == ZoneType.Flag))
        {
            if (!zone.IsPrimary)
                continue;

            bool isEnabled = false;
            if (rotationService != null)
            {
                isEnabled = rotationService.GridBehaivor == ElectricalGridBehaivor.AllEnabled;
                if (!isEnabled && rotationService.GridBehaivor == ElectricalGridBehaivor.EnabledWhenInRotation)
                {
                    isEnabled = rotationService.ActiveFlags.Any(x => x.Region.Primary.Zone == zone);
                }
            }

            SetPowerForAllGrid(zone, isEnabled);
        }

        return UniTask.CompletedTask;
    }

    UniTask ILayoutHostedService.StopAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }
}
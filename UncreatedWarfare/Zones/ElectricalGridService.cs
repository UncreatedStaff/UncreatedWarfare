using DanielWillett.ReflectionTools;
using System;
using Uncreated.Warfare.Layouts;
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
[Priority(-1000 /* after IFlagRotationService implementations */)]
public class ElectricalGridService : ILevelHostedService, ILayoutHostedService
{
    private readonly ILogger<ElectricalGridService> _logger;
    private readonly WarfareModule _module;

    private IElectricalGridHandler? _handler;

    // key: instance ID, value: number of zones needing the object to be enabled
    private readonly Dictionary<uint, int> _objectHandles = new Dictionary<uint, int>(256);

    private static readonly Action<InteractablePower>? RefreshIsConnectedToPower =
        Accessor.GenerateInstanceCaller<InteractablePower, Action<InteractablePower>>("RefreshIsConnectedToPower");

    public bool Enabled { get; internal set; }

    public ElectricalGridService(ILogger<ElectricalGridService> logger, WarfareModule module)
    {
        _logger = logger;
        _module = module;
    }

    /// <summary>
    /// Defines whether or not an object should be powered.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public bool IsPowered(LevelObject @object)
    {
        GameThread.AssertCurrent();

        if (_handler is { IsEnabled: true })
        {
            return _handler.IsPowered(@object);
        }

        return true;
    }

    /// <summary>
    /// Defines whether or not a barricade should be powered.
    /// </summary>
    /// <exception cref="GameThreadException"/>
    public bool IsPowered(InteractablePower otherInteractable)
    {
        GameThread.AssertCurrent();

        if (_handler is { IsEnabled: true })
        {
            return _handler.IsPowered(otherInteractable);
        }

        return true;
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

    public void CheckPowerForAllBarricades()
    {
        if (RefreshIsConnectedToPower == null)
            return;

        foreach (BarricadeInfo barricade in BarricadeUtility.EnumerateBarricades())
        {
            if (barricade.Drop.interactable is InteractablePower power)
                RefreshIsConnectedToPower(power);
        }
    }

    public void SetPowerForZoneObjects(Zone zone, bool state, bool addHandles)
    {
#if DEBUG
        _logger.LogConditional("Setting all objects in {0} to state {1}.", zone.Name, state);
        using IDisposable? scope = _logger.BeginScope(zone.Name);
#endif
        Vector3 c = zone.Center;
        foreach (uint gridObject in zone.GridObjects)
        {
            ObjectInfo obj = LevelObjectUtility.FindObject(gridObject, c);
            
            if (!obj.HasValue)
            {
                continue;
            }

            InteractableObject? intx = obj.Object.interactable;
            if (intx is null)
                continue;

            if (addHandles)
            {
                if (state)
                    AddHandle(obj.Object.instanceID);
                else
                    RemoveHandle(obj.Object.instanceID);
            }
            else if (GetHandleCount(obj.Object.instanceID) > 0)
            {
                _logger.LogConditional("Skipping disabling {0} - {1}, already enabled from another zone.", obj.Object.instanceID, obj.Object.asset);
                continue;
            }

            bool checkWire = intx.isWired;
            if (!checkWire)
                RefreshIsConnectedToPower?.Invoke(intx);

            if (intx.objectAsset is { interactability: EObjectInteractability.BINARY_STATE, interactabilityPower: not EObjectInteractabilityPower.NONE })
            {
                _logger.LogConditional("Setting state of {0} - {1} to {2}.", obj.Object.instanceID, obj.Object.asset, state);
                if (obj.Object.interactable is InteractableObjectBinaryState s && s.isUsed != state)
                {
                    ObjectManager.forceObjectBinaryState(obj.Object.transform, state);
                }
            }

            if (checkWire)
                RefreshIsConnectedToPower?.Invoke(intx);
        }
    }

    private void AddHandle(uint obj)
    {
        if (_objectHandles.TryGetValue(obj, out int v))
            _objectHandles[obj] = v + 1;
        else
            _objectHandles.Add(obj, 1);
    }

    private void RemoveHandle(uint obj)
    {
        if (!_objectHandles.TryGetValue(obj, out int v))
            return;

        if (v <= 1)
            _objectHandles.Remove(obj);
        else
            _objectHandles[obj] = v - 1;
    }
    
    private int GetHandleCount(uint obj)
    {
        _objectHandles.TryGetValue(obj, out int value);
        return value;
    }

    UniTask ILayoutHostedService.StartAsync(CancellationToken token)
    {
        _objectHandles.Clear();

        Layout layout = _module.GetActiveLayout();

        _handler = layout.ServiceProvider.ResolveOptional<IElectricalGridHandler>();
        _handler?.Start();
        return UniTask.CompletedTask;
    }

    UniTask ILayoutHostedService.StopAsync(CancellationToken token)
    {
        _handler?.Stop();
        _handler = null;
        return UniTask.CompletedTask;
    }
}
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics.CodeAnalysis;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles.Spawners;
using Uncreated.Warfare.Vehicles.UI;
using Uncreated.Warfare.Vehicles.WarfareVehicles.Damage;
using Uncreated.Warfare.Vehicles.WarfareVehicles.Flares;
using Uncreated.Warfare.Vehicles.WarfareVehicles.Transport;

namespace Uncreated.Warfare.Vehicles.WarfareVehicles;

[CannotApplyEqualityOperator]
public class WarfareVehicle : IDisposable, ITransformObject, IEquatable<WarfareVehicle>, IEquatable<InteractableVehicle>, IComparable<WarfareVehicle>, IComparable<InteractableVehicle>
{
    private bool _isSettingUpComponent;

    public InteractableVehicle Vehicle { get; }
    public WarfareVehicleInfo Info { get; }
    public VehicleSpawner? Spawn { get; private set; }
    public VehicleDamageTracker DamageTracker { get; }
    public VehicleAsset Asset { get; }
    public AdvancedVehicleDamageApplier AdvancedDamageApplier { get; }
    public TranportTracker TranportTracker { get; }
    public VehicleHUD? VehicleHUD { get; }
    public FlareEmitter? FlareEmitter { get; private set; }

    [field: MaybeNull]
    public WarfareVehicleComponent Component
    {
        get
        {
            if (_isSettingUpComponent)
            {
                if (!GameThread.IsCurrent)
                    throw new InvalidOperationException("WarfareVehicle was created on a non-game thread, so the Component won't be initialized until the next Update tick.");

                SetupComponents(WarfareModule.Singleton.ServiceProvider.Resolve<IServiceProvider>());
            }

            return field!;
        }
        private set;
    }

    public Vector3 Position
    {
        get => Vehicle.transform.position;
        set => SetPosition(value);
    }

    public Quaternion Rotation
    {
        get => Vehicle.transform.rotation;
        set => SetRotation(value);
    }

    public bool NeedsAutoResupply { get; internal set; }
    public uint InstanceId { get; }

    /// <summary>
    /// If the vehicle is driveable or submerged.
    /// </summary>
    public bool Alive => Vehicle is { isDead: false, isExploded: false, isActiveAndEnabled: true };

    public WarfareVehicle(InteractableVehicle interactableVehicle, WarfareVehicleInfo info, IServiceProvider serviceProvider)
    {
        Vehicle = interactableVehicle;
        InstanceId = interactableVehicle.instanceID;
        Asset = interactableVehicle.asset;
        Info = info;
        VehicleHUD = serviceProvider.GetService<VehicleHUD>();
        DamageTracker = new VehicleDamageTracker();
        TranportTracker = new TranportTracker();
        AdvancedDamageApplier = new AdvancedVehicleDamageApplier();
        NeedsAutoResupply = false;

        // allow thread-safe initialization so GetVehicle can add the component if it doesn't already exist
        if (GameThread.IsCurrent)
        {
            SetupComponents(serviceProvider);
        }
        else
        {
            _isSettingUpComponent = true;
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                if (Vehicle == null)
                    return;

                SetupComponents(serviceProvider);
            });
        }
    }

    private void SetupComponents(IServiceProvider serviceProvider)
    {
        if (!_isSettingUpComponent)
            return;

        Component = Vehicle.transform.GetOrAddComponent<WarfareVehicleComponent>().Init(this);
        _isSettingUpComponent = false;
        if (!Info.Type.IsAircraft())
            return;

        FlareEmitter = Vehicle.transform.GetOrAddComponent<FlareEmitter>();
        FlareEmitter.Init(this, serviceProvider.GetRequiredService<AssetConfiguration>());
    }

    public void Dispose()
    {
        Object.Destroy(Component);
    }

    internal void UnlinkFromSpawn(VehicleSpawner spawn)
    {
        GameThread.AssertCurrent();

        if (spawn == null)
            throw new ArgumentNullException(nameof(spawn));

        if (!Equals(Spawn, spawn))
        {
            throw new InvalidOperationException("The given spawn is not linked to this vehicle.");
        }

        if (Spawn?.LinkedVehicle == Vehicle)
        {
            throw new InvalidOperationException("The old linked spawn is still linked to this vehicle.");
        }

        Spawn = null;
    }

    internal void LinkToSpawn(VehicleSpawner spawn)
    {
        GameThread.AssertCurrent();

        if (spawn == null)
            throw new ArgumentNullException(nameof(spawn));

        if (spawn.LinkedVehicle != Vehicle)
        {
            throw new InvalidOperationException("The given spawn is not linked to this vehicle.");
        }

        Spawn = spawn;
    }

    /// <inheritdoc />
    public bool Equals(WarfareVehicle other)
    {
        return other.InstanceId == InstanceId;
    }

    /// <inheritdoc />
    public bool Equals(InteractableVehicle other)
    {
        return other.instanceID == InstanceId;
    }

    /// <inheritdoc />
    public int CompareTo(WarfareVehicle other)
    {
        return InstanceId.CompareTo(other.InstanceId);
    }

    /// <inheritdoc />
    public int CompareTo(InteractableVehicle other)
    {
        return InstanceId.CompareTo(other.instanceID);
    }

    public override bool Equals(object? obj)
    {
        return obj switch
        {
            WarfareVehicle v => Equals(v),
            InteractableVehicle v => Equals(v),
            _ => false
        };
    }

    public override int GetHashCode()
    {
        return unchecked ( (int)InstanceId );
    }

    public void SetPosition(Vector3 position)
    {
        GameThread.AssertCurrent();

        if (Vehicle.isDriven)
            throw new InvalidOperationException("Vehicle is driven, unable to set position.");

        Vehicle.transform.position = position;
    }

    public void SetRotation(Quaternion rotation)
    {
        GameThread.AssertCurrent();

        if (Vehicle.isDriven)
            throw new InvalidOperationException("Vehicle is driven, unable to set rotation.");

        Vehicle.transform.rotation = rotation;
    }

    public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
    {
        GameThread.AssertCurrent();

        if (Vehicle.isDriven)
            throw new InvalidOperationException("Vehicle is driven, unable to set position and rotation.");
        
        Vehicle.transform.position = position;
        Vehicle.transform.rotation = rotation;
    }

    Vector3 ITransformObject.Scale
    {
        get => Vector3.one;
        set => throw new NotSupportedException();
    }
}
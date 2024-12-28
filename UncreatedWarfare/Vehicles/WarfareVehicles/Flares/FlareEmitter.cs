using System;
using System.Collections.Generic;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Vehicles.UI;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Uncreated.Warfare.Vehicles.WarfareVehicles.Flares;

public class FlareEmitter : MonoBehaviour
{
    public WarfareVehicle Vehicle { get; private set; }
    
    private const int StartingFlaresAttackHeli = 30;
    private const int StartingFlaresTransportHeli = 50;
    private const int StartingFlaresJet = 30;
    private const int FlareBurstCount = 10;
    private const int FlareCooldown = 11;
    
    private VehicleAsset _flareAsset;
    private EffectAsset _dropFlaresSound;
    
    private float _timeLastFlareSpawned;
    private float _timeLastFlareDrop;
    private int _flareBurst;
    public int TotalFlaresLeft { get; private set; }
    private Coroutine? _warningRoutine;

    public FlareEmitter Init(WarfareVehicle vehicle, AssetConfiguration assetConfiguration)
    {
        Vehicle = vehicle;
        _flareAsset = assetConfiguration.GetAssetLink<VehicleAsset>("llalllalalalala").GetAssetOrFail();
        _dropFlaresSound = assetConfiguration.GetAssetLink<EffectAsset>("llalllalalalala").GetAssetOrFail();
        ReloadCountermeasures();
        PlayerKeys.PressedPluginKey1 += OnFlareKeyPressed;
        return this;
    }
    private void OnDestroy()
    {
        PlayerKeys.PressedPluginKey1 -= OnFlareKeyPressed;
    }

    private void OnFlareKeyPressed(WarfarePlayer player, ref bool handled)
    {
        TryDropFlares();
        handled = true;
    }
    public void ReloadCountermeasures()
    {
        TotalFlaresLeft = Vehicle.Info.Type switch
        {
            VehicleType.AttackHeli => StartingFlaresAttackHeli,
            VehicleType.TransportAir => StartingFlaresTransportHeli,
            VehicleType.Jet => StartingFlaresJet,
            _ => TotalFlaresLeft
        };

        Vehicle.VehicleHUD?.UpdateFlaresForRelevantPassengers(Vehicle);
    }
    
    public void TryDropFlares()
    {
        if (Time.time - _timeLastFlareDrop < FlareCooldown || TotalFlaresLeft < 0)
            return;

        _flareBurst = FlareBurstCount;
        _timeLastFlareDrop = Time.time;
        
        for (byte seat = 0; seat < Vehicle.Vehicle.passengers.Length; seat++)
        {
            Passenger passenger = Vehicle.Vehicle.passengers[seat];
            if (passenger.player == null)
                continue;
            
            EffectManager.sendUIEffect(_dropFlaresSound.id, (short)_dropFlaresSound.id, passenger.player.transportConnection, true);
        }
    }
    public void ReceiveMissileWarning()
    {
        if (Vehicle.VehicleHUD == null)
            return;
        
        if (_warningRoutine != null)
            Vehicle.Vehicle.StopCoroutine(_warningRoutine);

        _warningRoutine = StartCoroutine(MissileWarningRoutine(Vehicle.VehicleHUD));
    }

    private IEnumerator<WaitForSeconds> MissileWarningRoutine(VehicleHUD vehicleHUD)
    {
        vehicleHUD.ToggleMissileWarning(Vehicle, true);
        yield return new WaitForSeconds(1);
        vehicleHUD.ToggleMissileWarning(Vehicle, false);
    }
    
    private void FixedUpdate()
    {
        if (TotalFlaresLeft <= 0 || _flareBurst <= 0 || Time.time - _timeLastFlareSpawned < 0.2f)
            return;
        
        _timeLastFlareSpawned = Time.time;

        
        Vector3 flarePosition = Vehicle.Vehicle.transform.TransformPoint(0, -4, 0);
        Quaternion flareRotation = Vehicle.Vehicle.transform.rotation;
        InteractableVehicle? countermeasureVehicle = VehicleManager.spawnVehicleV2(_flareAsset, flarePosition, flareRotation);

        float sideforce = Random.Range(20, 30);

        if (_flareBurst % 2 == 0)
            sideforce = -sideforce;

        Rigidbody? rigidbody = countermeasureVehicle.transform.GetComponent<Rigidbody>();
            
        Vector3 velocity = 0.9f * Vehicle.Vehicle.ReplicatedSpeed * Vehicle.Vehicle.transform.forward - (15 * Vehicle.Vehicle.transform.up) + (sideforce * Vehicle.Vehicle.transform.right);
        rigidbody.velocity = velocity;

        FlareCountermeasure flareCountermeasure = countermeasureVehicle.gameObject.AddComponent<FlareCountermeasure>();

        FlareCountermeasure.ActiveCountermeasures.Add(flareCountermeasure);

        TotalFlaresLeft--;
        _flareBurst--;
        Vehicle.VehicleHUD?.UpdateFlaresForRelevantPassengers(Vehicle);
    }
}
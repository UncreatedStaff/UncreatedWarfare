using SDG.Unturned;
using System;
using System.Collections.Generic;
using Uncreated.SQL;
using Uncreated.Warfare.Deaths;
using Uncreated.Warfare.Events.Components;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.FOBs;
using UnityEngine;
using VehicleSpawn = Uncreated.Warfare.Vehicles.VehicleSpawn;

namespace Uncreated.Warfare.Components;

public struct LandmineData
{
    public static LandmineData Nil = new LandmineData(null, null);
    public Guid Barricade;
    public Player? Owner;
    public ulong OwnerId;
    public int InstanceId;
    public LandmineData(InteractableTrap? trap, BarricadeComponent? owner)
    {
        if (trap == null || owner == null)
        {
            Barricade = Guid.Empty;
            Owner = null;
            OwnerId = owner != null ? owner.Owner : 0ul;
            InstanceId = 0;
        }
        else
        {
            InstanceId = trap.GetInstanceID();
            Barricade = owner.BarricadeGUID;
            Owner = owner.Player;
            OwnerId = owner.Owner;
        }
    }

}
public class UCPlayerData : MonoBehaviour
{
    internal const int PingBufferSize = 256;
    internal BarricadeDrop? ExplodingLandmine;
    internal BarricadeDrop? TriggeringLandmine;
    internal ItemMagazineAsset LastProjectedAmmoType;
    internal Coroutine? CurrentTeleportRequest;
    internal PlayerDied? LastBleedingEvent;
    internal IDeployable? PendingDeploy;
    internal float[] PingBuffer = new float[PingBufferSize];
    internal int PingBufferIndex = -1;
    internal float LastAvgPingDifference;
    internal List<ThrowableComponent> ActiveThrownItems = new List<ThrowableComponent>(4);
    internal SqlItem<VehicleSpawn>? Currentlylinking;
    internal VehicleComponent? ExplodingVehicle;
    internal ThrowableComponent? TriggeringThrowable;
    internal KeyValuePair<ulong, DateTime> SecondLastAttacker;
    internal DeathMessageArgs LastBleedingArgs;
    internal Guid LastExplodedVehicle;
    internal Guid LastVehicleHitBy;
    internal Guid LastInfectableConsumed;
    internal Guid LastExplosiveConsumed;
    internal Guid LastChargeDetonated;
    internal Guid LastShreddedBy;
    internal Guid LastRocketShot;
    internal Guid LastRocketShotVehicle;
    internal Guid LastGunShot; // used for amc
    internal ulong LastAttacker;
    private float _currentTimeSeconds;
    public Player Player { get; private set; }
    public Gamemodes.Interfaces.IStats Stats { get; internal set; }
    public float JoinTime { get; private set; }
    
    public void StartTracking(Player player)
    {
        Player = player;
        _currentTimeSeconds = 0.0f;
        JoinTime = Time.realtimeSinceStartup;
    }
    public void AddPing(float value)
    {
        ++PingBufferIndex;
        PingBuffer[PingBufferIndex % PingBufferSize] = value;
    }
    public void TryUpdateAttackers(ulong newLastAttacker)
    {
        if (newLastAttacker == LastAttacker) return;

        SecondLastAttacker = new KeyValuePair<ulong, DateTime>(LastAttacker, DateTime.Now);
        LastAttacker = newLastAttacker;
    }
    public void ResetAttackers()
    {
        LastAttacker = 0;
        SecondLastAttacker = new KeyValuePair<ulong, DateTime>(0, DateTime.Now);
    }
    public void Update()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        float dt = Time.deltaTime;
        _currentTimeSeconds += dt;
    }
    public void CancelDeployment()
    {
        if (CurrentTeleportRequest != null)
        {
            StopCoroutine(CurrentTeleportRequest);
            CurrentTeleportRequest = null;
            PendingDeploy = null;
        }
    }
}

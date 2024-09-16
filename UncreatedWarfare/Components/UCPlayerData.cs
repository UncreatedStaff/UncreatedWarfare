using System;
using System.Collections.Generic;

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
    internal float[] PingBuffer = new float[PingBufferSize];
    internal int PingBufferIndex = -1;
    internal float LastAvgPingDifference;
    internal KeyValuePair<ulong, DateTime> SecondLastAttacker;
    internal InteractableVehicle? LastRocketShotVehicle;
    internal Guid LastGunShot; // used for amc
    internal ulong LastAttacker;
    public Player Player { get; private set; }
    public float JoinTime { get; private set; }
    
    public void StartTracking(Player player)
    {
        Player = player;
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
}

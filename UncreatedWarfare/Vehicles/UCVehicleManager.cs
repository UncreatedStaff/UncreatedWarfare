using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Harmony;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Vehicles;

public static class UCVehicleManager
{
    public static bool TryPutPlayerInVehicle(InteractableVehicle vehicle, Player player, byte? seat = null, bool callEvent = true)
    {
        ThreadUtil.assertIsGameThread();

        if (callEvent)
        {
            bool shouldAllow = false;
            EventDispatcher.InvokeVehicleManagerOnEnterVehicleRequested(player, vehicle, ref shouldAllow);
            if (!shouldAllow)
                return false;
        }
        if (!seat.HasValue)
            return VehicleManager.ServerForcePassengerIntoVehicle(player, vehicle);

        Patches.VehiclePatches.DesiredSeat = seat.Value;
        try
        {
            return VehicleManager.ServerForcePassengerIntoVehicle(player, vehicle);
        }
        finally
        {
            Patches.VehiclePatches.DesiredSeat = -1;
        }
    }
    public static bool TryMovePlayerToEmptySeat(Player player)
    {
        ThreadUtil.assertIsGameThread();

        if (Data.SendSwapVehicleSeats == null)
            return false;

        InteractableVehicle? vehicle = player.movement.getVehicle();
        if (vehicle == null)
            return false;
        byte fromSeat = player.movement.getSeat();

        int freeSeat = -1;
        Passenger currentSeat = vehicle.passengers[fromSeat];
        currentSeat.player = Data.NilSteamPlayer;
        try
        {
            if (vehicle.tryAddPlayer(out byte freeSeat2, player))
                freeSeat = freeSeat2;
        }
        finally
        {
            currentSeat.player = player.channel.owner;
        }

        if (freeSeat is >= 0 and <= byte.MaxValue && vehicle.passengers.Length > freeSeat)
        {
            byte freeSeat2 = (byte)freeSeat;
            bool shouldAllow = false;

            EventDispatcher.InvokeVehicleManagerOnSwapSeatRequested(player, vehicle, ref shouldAllow, fromSeat, ref freeSeat2);
            if (!shouldAllow || freeSeat >= vehicle.passengers.Length)
                return false;

            Data.SendSwapVehicleSeats.InvokeAndLoopback(ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), vehicle.instanceID, fromSeat, freeSeat2);
            return player.channel.owner.Equals(vehicle.passengers[freeSeat].player);
        }

        return false;
    }
    public static bool TrySwapPlayerInVehicle(Player player, byte toSeat, bool updateCooldown = false)
    {
        ThreadUtil.assertIsGameThread();

        if (Data.SendSwapVehicleSeats == null)
            return false;

        InteractableVehicle? vehicle = player.movement.getVehicle();
        if (vehicle == null)
            return false;
        byte fromSeat = player.movement.getSeat();

        bool shouldAllow = false;
        EventDispatcher.InvokeVehicleManagerOnSwapSeatRequested(player, vehicle, ref shouldAllow, fromSeat, ref toSeat);
        if (!shouldAllow || toSeat >= vehicle.passengers.Length)
            return false;

        Passenger seat = vehicle.passengers[toSeat];
        SteamPlayer? existing = seat.player;
        int freeSeat = -1;
        PooledTransportConnectionList connections = Provider.GatherRemoteClientConnections();
        if (existing != null)
        {
            byte toSeat2 = fromSeat;
            EventDispatcher.InvokeVehicleManagerOnSwapSeatRequested(player, vehicle, ref shouldAllow, fromSeat, ref toSeat2);
            if (!shouldAllow || toSeat >= vehicle.passengers.Length || toSeat2 != fromSeat)
                return false;
            seat.player = Data.NilSteamPlayer;
            try
            {
                if (vehicle.tryAddPlayer(out byte freeSeat2, existing.player))
                    freeSeat = freeSeat2;
            }
            finally
            {
                seat.player = existing;
            }
            if (freeSeat is < 0 or > byte.MaxValue || vehicle.passengers.Length <= freeSeat)
            {
                freeSeat = -1;
                VehicleManager.sendExitVehicle(vehicle, toSeat, seat.seat.position, MeasurementTool.angleToByte(seat.seat.rotation.eulerAngles.y), true);
            }
            else
                Data.SendSwapVehicleSeats.InvokeAndLoopback(ENetReliability.Reliable, connections, vehicle.instanceID, toSeat, (byte)freeSeat);
        }
        
        Data.SendSwapVehicleSeats.InvokeAndLoopback(ENetReliability.Reliable, connections, vehicle.instanceID, fromSeat, toSeat);
        if (updateCooldown)
            vehicle.lastSeat = Time.realtimeSinceStartup;
        if (seat.player == null || seat.player.playerID.steamID.m_SteamID != player.channel.owner.playerID.steamID.m_SteamID)
        {
            if (existing != null)
            {
                // swap back
                if (freeSeat != -1 && existing.Equals(vehicle.passengers[freeSeat].player) && seat.player == null)
                    Data.SendSwapVehicleSeats.InvokeAndLoopback(ENetReliability.Reliable, connections, vehicle.instanceID, (byte)freeSeat, toSeat);
                else if (freeSeat == -1)
                {
                    // enter back
                    if (seat.player == null)
                        TryPutPlayerInVehicle(vehicle, player, toSeat, callEvent: false);
                    else if (vehicle.passengers[fromSeat].player == null)
                        TryPutPlayerInVehicle(vehicle, player, fromSeat, callEvent: false);
                    else
                        TryPutPlayerInVehicle(vehicle, player);
                }
            }
            return false;
        }
        if (existing != null)
            TryPutPlayerInVehicle(vehicle, player, fromSeat, callEvent: false);
        return true;
    }
    public static VehicleBarricadeRegion? FindRegionFromVehicleWithIndex(this InteractableVehicle vehicle, out ushort index, int subvehicleIndex = 0)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (vehicle == null)
        {
            index = ushort.MaxValue;
            return null;
        }
        for (ushort i = 0; i < BarricadeManager.vehicleRegions.Count; i++)
        {
            VehicleBarricadeRegion vehicleRegion = BarricadeManager.vehicleRegions[i];
            if (vehicleRegion.vehicle == vehicle && vehicleRegion.subvehicleIndex == subvehicleIndex)
            {
                index = i;
                return vehicleRegion;
            }
        }
        index = ushort.MaxValue;
        return null;
    }
    public static IEnumerable<InteractableVehicle> GetNearbyVehicles(Guid id, float radius, Vector3 origin)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        float sqrRadius = radius * radius;
        List<InteractableVehicle> vehicles = new List<InteractableVehicle>();
        List<InteractableVehicle> newvehicles = new List<InteractableVehicle>(vehicles.Count);
        VehicleManager.getVehiclesInRadius(origin, sqrRadius, vehicles);
        for (int v = 0; v < vehicles.Count; v++)
        {
            if (vehicles[v].asset.GUID == id)
                newvehicles.Add(vehicles[v]);
        }
        vehicles.Clear();
        return newvehicles;
    }
    public static int CountNearbyVehicles(Guid id, float radius, Vector3 origin, ulong team = 0)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        float sqrRadius = radius * radius;
        List<InteractableVehicle> vehicles = new List<InteractableVehicle>();
        int amt = 0;
        VehicleManager.getVehiclesInRadius(origin, sqrRadius, vehicles);
        for (int v = 0; v < vehicles.Count; v++)
        {
            if (vehicles[v].asset.GUID == id && team != 0 && vehicles[v].lockedGroup.m_SteamID.GetTeam() == team)
                ++amt;
        }
        vehicles.Clear();
        return amt;
    }
    public static bool IsVehicleNearby(Guid id, float radius, Vector3 origin, out InteractableVehicle vehicle)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        float sqrRadius = radius * radius;
        List<InteractableVehicle> vehicles = new List<InteractableVehicle>();
        VehicleManager.getVehiclesInRadius(origin, sqrRadius, vehicles);
        for (int v = 0; v < vehicles.Count; v++)
        {
            if (vehicles[v].asset.GUID == id)
            {
                vehicle = vehicles[v];
                return !vehicle.isDead;
            }
        }
        vehicles.Clear();
        vehicle = null!;
        return false;
    }
    public static IEnumerable<InteractableVehicle> GetNearbyVehicles(IEnumerable<Guid> ids, float radius, Vector3 origin)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        float sqrRadius = radius * radius;
        List<InteractableVehicle> vehicles = new List<InteractableVehicle>();
        List<InteractableVehicle> newvehicles = new List<InteractableVehicle>(vehicles.Count);
        VehicleManager.getVehiclesInRadius(origin, sqrRadius, vehicles);
        for (int v = 0; v < vehicles.Count; v++)
        {
            if (ids.Contains(vehicles[v].asset.GUID))
                newvehicles.Add(vehicles[v]);
        }
        vehicles.Clear();
        return newvehicles;
    }
    public static InteractableVehicle? GetNearestLogi(Vector3 point, float radius, ulong team = 0)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        List<InteractableVehicle> vehicles = new List<InteractableVehicle>();
        VehicleManager.getVehiclesInRadius(point, Mathf.Pow(radius, 2), vehicles);
        VehicleBay? bay = VehicleBay.GetSingletonQuick();
        if (bay == null)
            return null;
        team = TeamManager.GetGroupID(team);
        return vehicles.OrderBy(x => (point - x.transform.position).sqrMagnitude).FirstOrDefault(v =>
            v.lockedGroup.m_SteamID == team &&
            bay.GetDataSync(v.asset.GUID) is { } vehicleData &&
            VehicleData.IsLogistics(vehicleData.Type));
    }
}

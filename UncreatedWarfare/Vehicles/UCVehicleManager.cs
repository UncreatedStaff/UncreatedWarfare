using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Harmony;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Vehicles;

public static class UCVehicleManager
{
    internal static bool IgnoreSwapCooldown;
    internal static bool AllowEnterDriverSeat;
    public static bool TryPutPlayerInVehicle(InteractableVehicle vehicle, Player player, byte? seat = null, bool callEvent = true, bool force = false, bool autoSim = false)
    {
        ThreadUtil.assertIsGameThread();

        if (callEvent)
        {
            bool shouldAllow = true;
            IgnoreSwapCooldown = true;
            try
            {
                EventDispatcher.InvokeVehicleManagerOnEnterVehicleRequested(player, vehicle, ref shouldAllow);
                if (!shouldAllow)
                {
                    L.LogDebug("Not allowed to enter vehicle.");
                    return false;
                }
            }
            finally
            {
                IgnoreSwapCooldown = false;
            }
        }

        if (seat.HasValue && vehicle.passengers.Length <= seat.Value)
            seat = null;
        if (!seat.HasValue)
        {
            L.LogDebug("Generic entering vehicle.");
            if (Data.SendEnterVehicle != null)
            {
                if (vehicle.tryAddPlayer(out byte seat2, player))
                {
                    L.LogDebug($"Seat available: {seat2}.");
                    Data.SendEnterVehicle.InvokeAndLoopback(ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), vehicle.instanceID, seat2, player.channel.owner.playerID.steamID);
                    if (autoSim)
                        player.movement.simulate();
                    return true;
                }
                
                if (force)
                {
                    for (int i = vehicle.passengers.Length - 1; i >= 0; --i)
                    {
                        if (vehicle.passengers[i].player == null)
                        {
                            Data.SendEnterVehicle.InvokeAndLoopback(ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), vehicle.instanceID, (byte)i, player.channel.owner.playerID.steamID);
                            if (autoSim)
                                player.movement.simulate();
                            L.LogDebug($"Forced, seat available: {i}.");
                            return true;
                        }
                    }
                }
                L.LogDebug("No seat available.");
            }
            else return VehicleManager.ServerForcePassengerIntoVehicle(player, vehicle);

            return false;
        }

        Patches.VehiclePatches.DesiredSeat = seat.Value;
        try
        {
            L.LogDebug($"Entering at seat: {Patches.VehiclePatches.DesiredSeat}.");
            if (Data.SendEnterVehicle != null)
            {
                if (vehicle.tryAddPlayer(out byte seat2, player))
                {
                    L.LogDebug($"Seat available: {seat2}, wanted: {Patches.VehiclePatches.DesiredSeat}.");
                    Data.SendEnterVehicle.InvokeAndLoopback(ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), vehicle.instanceID, seat2, player.channel.owner.playerID.steamID);
                    if (autoSim)
                        player.movement.simulate();
                    return true;
                }
                else if (force && vehicle.passengers[seat.Value].player == null)
                {
                    L.LogDebug("Forced.");
                    Data.SendEnterVehicle.InvokeAndLoopback(ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), vehicle.instanceID, seat.Value, player.channel.owner.playerID.steamID);
                    if (autoSim)
                        player.movement.simulate();
                    return true;
                }
                else
                    L.LogDebug($"No seat available, wanted: {Patches.VehiclePatches.DesiredSeat}.");
            }
            else
            {
                if (VehicleManager.ServerForcePassengerIntoVehicle(player, vehicle))
                {
                    if (autoSim)
                        player.movement.simulate();
                    return true;
                }
            }

            return false;
        }
        finally
        {
            Patches.VehiclePatches.DesiredSeat = -1;
        }
    }
    public static InteractableVehicle? FindVehicleFromTrunkStorage(Items? trunk)
    {
        if (trunk is null)
            return null;
        for (int i = 0; i < VehicleManager.vehicles.Count; ++i)
        {
            InteractableVehicle vehicle = VehicleManager.vehicles[i];
            if (vehicle.trunkItems != trunk)
                continue;

            return vehicle;
        }

        return null;
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
            {
                L.LogDebug($"Found free seat: {freeSeat2}.");
                freeSeat = freeSeat2;
            }
            else
                L.LogDebug("Couldn't find free seat.");
        }
        finally
        {
            currentSeat.player = player.channel.owner;
        }

        if (freeSeat is >= 0 and <= byte.MaxValue && vehicle.passengers.Length > freeSeat)
        {
            byte freeSeat2 = (byte)freeSeat;
            bool shouldAllow = true;

            IgnoreSwapCooldown = true;
            try
            {
                EventDispatcher.InvokeVehicleManagerOnSwapSeatRequested(player, vehicle, ref shouldAllow, fromSeat, ref freeSeat2);
            }
            finally
            {
                IgnoreSwapCooldown = false;
            }
            if (!shouldAllow || freeSeat >= vehicle.passengers.Length)
            {
                L.LogDebug($"Not allowed to swap ({freeSeat}, {freeSeat2}).");
                return false;
            }
            L.LogDebug($"Adjusted free seat: {freeSeat} -> {freeSeat2}.");

            Data.SendSwapVehicleSeats.InvokeAndLoopback(ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), vehicle.instanceID, fromSeat, freeSeat2);
            L.LogDebug($"Swapped {fromSeat} -> {freeSeat2}.");
            return player.channel.owner.Equals(vehicle.passengers[freeSeat2].player);
        }

        L.LogDebug("Free seat out of range.");
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

        bool shouldAllow = true;
        IgnoreSwapCooldown = true;
        try
        {
            EventDispatcher.InvokeVehicleManagerOnSwapSeatRequested(player, vehicle, ref shouldAllow, fromSeat, ref toSeat);
        }
        finally
        {
            IgnoreSwapCooldown = false;
        }
        if (!shouldAllow || toSeat >= vehicle.passengers.Length)
        {
            L.LogDebug($"Not allowed to swap to {toSeat}.");
            return false;
        }

        Passenger seat = vehicle.passengers[toSeat];
        SteamPlayer? existing = seat.player;
        int freeSeat = -1;
        PooledTransportConnectionList connections = Provider.GatherRemoteClientConnections();
        bool existingNeedsToReenterVehicle = false;
        if (existing != null)
        {
            L.LogDebug("Player in seat.");
            byte toSeat2 = fromSeat;
            IgnoreSwapCooldown = true;
            if (fromSeat == 0)
                AllowEnterDriverSeat = true;
            try
            {
                EventDispatcher.InvokeVehicleManagerOnSwapSeatRequested(existing.player, vehicle, ref shouldAllow, toSeat, ref toSeat2);
            }
            finally
            {
                IgnoreSwapCooldown = false;
                if (fromSeat == 0)
                    AllowEnterDriverSeat = false;
            }
            seat.player = Data.NilSteamPlayer;
            try
            {
                if (vehicle.tryAddPlayer(out byte freeSeat2, existing.player))
                {
                    L.LogDebug($"Found seat for existing player: {freeSeat2}.");
                    freeSeat = freeSeat2;
                }
                else
                    L.LogDebug($"Could not find seat for existing player: {toSeat} -> {toSeat2}.");
            }
            finally
            {
                seat.player = existing;
            }
            if (!shouldAllow)
            {
                L.LogDebug("Other player not allowed to swap.");
                freeSeat = -1;
            }
            if (freeSeat is < 0 or > byte.MaxValue || vehicle.passengers.Length <= freeSeat)
            {
                freeSeat = -1;
                L.LogDebug("Kicked existing.");
                VehicleManager.sendExitVehicle(vehicle, toSeat, seat.seat.position, MeasurementTool.angleToByte(seat.seat.rotation.eulerAngles.y), true);
                existing.player.movement.simulate();
                existingNeedsToReenterVehicle = true;
            }
            else
            {
                L.LogDebug($"Swapped existing {toSeat} -> {freeSeat}.");
                Data.SendSwapVehicleSeats.InvokeAndLoopback(ENetReliability.Reliable, connections, vehicle.instanceID, toSeat, (byte)freeSeat);
            }
        }
        
        Data.SendSwapVehicleSeats.InvokeAndLoopback(ENetReliability.Reliable, connections, vehicle.instanceID, fromSeat, toSeat);
        if (updateCooldown)
            vehicle.lastSeat = Time.realtimeSinceStartup;
        bool success;
        if (seat.player == null || seat.player.playerID.steamID.m_SteamID != player.channel.owner.playerID.steamID.m_SteamID)
        {
            if (existing != null)
            {
                // swap back
                if (!existingNeedsToReenterVehicle && existing.Equals(vehicle.passengers[freeSeat].player) && seat.player == null)
                    Data.SendSwapVehicleSeats.InvokeAndLoopback(ENetReliability.Reliable, connections, vehicle.instanceID, (byte)freeSeat, toSeat);
                else if (existingNeedsToReenterVehicle)
                {
                    // enter back
                    if (seat.player == null)
                        success = TryPutPlayerInVehicle(vehicle, player, toSeat, callEvent: false, force: true);
                    else if (vehicle.passengers[fromSeat].player == null)
                        success = TryPutPlayerInVehicle(vehicle, player, fromSeat, callEvent: false, force: true);
                    else
                        success = TryPutPlayerInVehicle(vehicle, player, callEvent: false, force: true);
                    L.LogDebug("Putting back in vehicle.");
                    if (!success && UCPlayer.FromSteamPlayer(existing) is { IsOnline: true } pl)
                        TeamManager.TeleportToMain(pl);
                }
            }
            return false;
        }
        if (existingNeedsToReenterVehicle)
        {
            success = TryPutPlayerInVehicle(vehicle, player, fromSeat, callEvent: false, force: true);
            L.LogDebug($"Putting back in vehicle: {success}, {fromSeat}.");
            if (!success && UCPlayer.FromSteamPlayer(existing!) is { IsOnline: true } pl)
                TeamManager.TeleportToMain(pl);
        }
        else if (existing != null)
        {
            L.LogDebug($"Putting back in seat: {freeSeat} -> {fromSeat}.");
            Data.SendSwapVehicleSeats.InvokeAndLoopback(ENetReliability.Reliable, connections, vehicle.instanceID, (byte)freeSeat, fromSeat);
        }
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

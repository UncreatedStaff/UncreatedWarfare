using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Diagnostics.CodeAnalysis;
using Uncreated.Warfare.Events;

namespace Uncreated.Warfare.Util;

/// <summary>
/// Helper functions for vehicles.
/// </summary>
public static class VehicleUtility
{
    internal static bool IgnoreSwapCooldown;
    
    // may use later idk
    internal static bool AllowEnterDriverSeat;

    /// <summary>
    /// Find the vehicle who's trunk storage is backed by <paramref name="trunk"/>. Used to identify the vehicle from item events.
    /// </summary>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public static bool TryGetVehicleFromTrunkStorage([NotNullWhen(true)] Items? trunk, [MaybeNullWhen(false)] out InteractableVehicle vehicle)
    {
        ThreadUtil.assertIsGameThread();

        if (trunk is null)
        {
            vehicle = null;
            return false;
        }

        for (int i = 0; i < VehicleManager.vehicles.Count; ++i)
        {
            InteractableVehicle v = VehicleManager.vehicles[i];
            if (v.trunkItems != trunk)
                continue;

            vehicle = v;
            return true;
        }

        vehicle = null;
        return false;
    }

    /// <summary>
    /// Moves a player to an empty seat in their vehicle, returning <see langword="true"/> if successful.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="NotSupportedException">Not on main thread.</exception>
    public static bool TryMovePlayerToEmptySeat(Player player)
    {
        if (player is null)
            throw new ArgumentNullException(nameof(player));

        ThreadUtil.assertIsGameThread();

        if (Data.SendSwapVehicleSeats == null)
        {
            return false;
        }

        InteractableVehicle? vehicle = player.movement.getVehicle();
        if (vehicle == null || vehicle.isDead)
        {
            return false;
        }

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
}

using System;
using System.Globalization;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles.Spawners;
using Uncreated.Warfare.Zones;

namespace Uncreated.Warfare.Commands;

internal static class VehicleTargetCommandHelper
{
    /// <summary>
    /// Get a referenced seat.
    /// </summary>
    /// <returns>-1 or the seat index.</returns>
    public static async UniTask<int> GetSeat(CommandContext ctx, InteractableVehicle vehicleTarget, VehicleTranslations translations, int argOffset = 0, CancellationToken token = default)
    {
        int seatIndex = -1;
        if (ctx.MatchParameter(argOffset, "driver", "pilot", "drive"))
        {
            return vehicleTarget.passengers.Length > 0 ? 0 : -1;
        }

        // turret, gunner, gun
        if (ctx.HasArgsExact(argOffset + 1) && ctx.MatchParameter(argOffset, "turret", "gunner", "gun"))
        {
            for (int i = 0; i < vehicleTarget.turrets.Length; ++i)
            {
                Passenger turret = vehicleTarget.turrets[i];
                if (turret.player == null)
                    continue;
                
                if (seatIndex == -1)
                    seatIndex = turret.turret.seatIndex;
                else
                {
                    seatIndex = -1;
                    break;
                }
            }

            return seatIndex;
        }

        // quantifier[ ]<number>
        if (ctx.TryGetRange(argOffset, out string? query))
        {
            int start = query.Length;
            while (start > 0 && char.IsDigit(query, start - 1))
            {
                --start;
            }

            if (start < query.Length && int.TryParse(query.AsSpan(start), NumberStyles.Number, ctx.ParseCulture, out seatIndex))
            {
                if (seatIndex > 0)
                    --seatIndex;
            }

            // seat 1, position 1, etc.
            if (query.StartsWith("seat", StringComparison.InvariantCultureIgnoreCase) || query.StartsWith("position", StringComparison.InvariantCultureIgnoreCase))
            {
                if (seatIndex >= vehicleTarget.passengers.Length || seatIndex < 0)
                    return -1;

                return seatIndex;
            }

            // turret 1, gun 1, gunner 1, etc.
            if ((query.StartsWith("turret", StringComparison.InvariantCultureIgnoreCase) || query.StartsWith("gun", StringComparison.InvariantCultureIgnoreCase)))
            {
                if (seatIndex >= vehicleTarget.turrets.Length || seatIndex < 0)
                    return -1;
                
                seatIndex = vehicleTarget.turrets[seatIndex].turret.seatIndex;
                return seatIndex;
            }
        }

        (_, WarfarePlayer? player) = await ctx.TryGetPlayer(argOffset, remainder: true, searchType: PlayerNameType.NickName);
        await UniTask.SwitchToMainThread(token);

        if (player is { IsOnline: true })
        {
            if (player.UnturnedPlayer.movement.getVehicle() != vehicleTarget)
                throw ctx.Reply(translations.VehicleTargetNotInVehicle, player);

            return player.UnturnedPlayer.movement.getSeat();
        }

        if (!ctx.TryGet(argOffset, out seatIndex))
            throw ctx.SendHelp();

        if (seatIndex > 0)
            --seatIndex;

        if (seatIndex >= 0 && seatIndex < vehicleTarget.passengers.Length)
            return (byte)seatIndex;

        return -1;
    }
    public static InteractableVehicle? GetVehicleTarget(CommandContext ctx, VehicleSpawnerService spawnerService, ZoneStore zoneStore)
    {
        // get target vehicle or linked nearby vehicle
        if (ctx.TryGetVehicleTarget(out InteractableVehicle? vehicle))
        {
            return vehicle;
        }

        Vector3 playerPosition = ctx.Player.Position;
        bool playerInMain = zoneStore.IsInMainBase(playerPosition);

        foreach (VehicleSpawner spawn in spawnerService.Spawners)
        {
            InteractableVehicle? linked = spawn.LinkedVehicle;
            if (linked == null || linked.isDead || linked.lockedOwner.m_SteamID != ctx.CallerId.m_SteamID)
                continue;

            // another vehicle owned by this player

            Vector3 vehiclePosition = linked.transform.position;
            if (linked.asset.engine is EEngine.HELICOPTER or EEngine.PLANE || MathUtility.WithinRange(in playerPosition, in vehiclePosition, 150))
            {
                if (playerInMain && !zoneStore.IsInMainBase(vehiclePosition) && !zoneStore.IsInAntiMainCamp(vehiclePosition))
                    continue;
            }

            if (vehicle == null)
                vehicle = linked;
            else
            {
                vehicle = null;
                break;
            }
        }

        return vehicle;
    }
}
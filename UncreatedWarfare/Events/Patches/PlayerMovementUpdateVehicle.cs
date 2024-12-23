using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using System;
using System.Reflection;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events.Models.Vehicles;
using Uncreated.Warfare.Patches;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.Events.Patches;

[UsedImplicitly]
internal class PlayerMovementUpdateVehicle : IHarmonyPatch
{
    private static MethodInfo? _target;

    void IHarmonyPatch.Patch(ILogger logger, HarmonyLib.Harmony patcher)
    {
        _target = typeof(PlayerMovement).GetMethod("updateVehicle", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (_target != null)
        {
            patcher.Patch(_target, prefix: Accessor.GetMethod(Prefix));
            patcher.Patch(_target, postfix: Accessor.GetMethod(Postfix));
            logger.LogDebug("Patched {0} for handling delayed vehicle updating for the player.", _target);
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            new MethodDefinition("updateVehicle")
                .DeclaredIn<PlayerMovement>(isStatic: false)
                .WithNoParameters()
                .ReturningVoid()
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger, HarmonyLib.Harmony patcher)
    {
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(Prefix));
        patcher.Unpatch(_target, Accessor.GetMethod(Postfix));
        logger.LogDebug("Unpatched {0} for sign text updated event.", _target);
        _target = null;
    }

    private static void Prefix(PlayerMovement __instance, ref object? __state)
    {
        VehicleState state = default;
        state.Vehicle = __instance.getVehicle();
        state.Seat = __instance.getSeat();
        __state = state;
    }

    private static void Postfix(PlayerMovement __instance, object? __state)
    {
        VehicleState state = (VehicleState)__state!;

        WarfarePlayer player = WarfareModule.Singleton.ServiceProvider.Resolve<IPlayerService>().GetOnlinePlayer(__instance);

        InteractableVehicle? vehicle = player.UnturnedPlayer.movement.getVehicle();

        if (vehicle == null)
        {
            InteractableVehicle? oldVehicle = state.Vehicle;
            if (player == null || oldVehicle == null)
                return;

            WarfareVehicle warfareVehicle = oldVehicle.transform.GetComponent<WarfareVehicleComponent>().WarfareVehicle;

            ExitVehicle args = new ExitVehicle
            {
                Player = player,
                PassengerIndex = state.Seat,
                Vehicle = warfareVehicle
            };

            _ = WarfareModule.EventDispatcher.DispatchEventAsync(args, WarfareModule.Singleton.UnloadToken);
        }
        else
        {
            WarfareVehicle warfareVehicle = vehicle.transform.GetComponent<WarfareVehicleComponent>().WarfareVehicle;

            byte seat = player.UnturnedPlayer.movement.getSeat();

            warfareVehicle.TranportTracker.RecordPlayerEntry(player.Steam64.m_SteamID, warfareVehicle.Vehicle.transform.position, seat);

            if (vehicle == state.Vehicle)
            {
                VehicleSwappedSeat args = new VehicleSwappedSeat
                {
                    Player = player,
                    NewPassengerIndex = seat,
                    OldPassengerIndex = state.Seat,
                    Vehicle = warfareVehicle
                };

                _ = WarfareModule.EventDispatcher.DispatchEventAsync(args, WarfareModule.Singleton.UnloadToken);
            }
            else
            {
                EnterVehicle args = new EnterVehicle
                {
                    Player = player,
                    PassengerIndex = seat,
                    Vehicle = warfareVehicle
                };

                _ = WarfareModule.EventDispatcher.DispatchEventAsync(args, WarfareModule.Singleton.UnloadToken);
            }
        }
    }

    private struct VehicleState
    {
        public InteractableVehicle? Vehicle;
        public byte Seat;
    }
}
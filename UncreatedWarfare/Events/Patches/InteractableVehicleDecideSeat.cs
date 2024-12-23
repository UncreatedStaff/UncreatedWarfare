using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Emit;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using SDG.NetTransport;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events.Models.Vehicles;
using Uncreated.Warfare.Patches;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles.WarfareVehicles;

namespace Uncreated.Warfare.Events.Patches;

[UsedImplicitly]
internal sealed class InteractableVehicleDecideSeat : IHarmonyPatch
{
    private static readonly ClientStaticMethod<uint, byte, CSteamID>? SendEnterVehicle =
        ReflectionUtility.FindRpc<VehicleManager, ClientStaticMethod<uint, byte, CSteamID>>("SendEnterVehicle");

    private static MethodInfo? _target;

    private static MethodInfo? _tryAddPlayer;

    void IHarmonyPatch.Patch(ILogger logger, Harmony patcher)
    {
        _target = typeof(VehicleManager).GetMethod(nameof(VehicleManager.ReceiveEnterVehicleRequest), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        _tryAddPlayer = typeof(InteractableVehicle).GetMethod(nameof(InteractableVehicle.tryAddPlayer),
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (SendEnterVehicle == null)
        {
            logger.LogError("SendEnterVehicle RPC missing.");
            return;
        }

        if (_tryAddPlayer == null)
        {
            logger.LogError("Failed to find method: {0}.",
                new MethodDefinition(nameof(InteractableVehicle.tryAddPlayer))
                    .DeclaredIn<InteractableVehicle>(isStatic: false)
                    .WithParameter<byte>("instanceID", ByRefTypeMode.Out)
                    .WithParameter<Player>("player")
                    .Returning<bool>());
            return;
        }

        if (_target != null)
        {
            patcher.Patch(_target, transpiler: Accessor.GetMethod(Transpiler));
            logger.LogDebug("Patched {0} for deciding player seat.", _target);
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            new MethodDefinition(nameof(VehicleManager.ReceiveEnterVehicleRequest))
                .DeclaredIn<VehicleManager>(isStatic: true)
                .WithParameter<ServerInvocationContext>("context", ByRefTypeMode.In)
                .WithParameter<uint>("instanceID")
                .WithParameter<byte[]>("hash")
                .WithParameter<byte[]>("physicsProfileHash")
                .WithParameter<byte>("engine")
                .ReturningVoid()
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger, Harmony patcher)
    {
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(Transpiler));
        logger.LogDebug("Unpatched {0} for sign text updated event.", _target);
        _target = null;
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase method)
    {
        TranspileContext ctx = new TranspileContext(method, generator, instructions);
        
        while (ctx.MoveNext())
        {
            if (!ctx.Instruction.Calls(_tryAddPlayer))
            {
                continue;
            }

            ctx.Replace(emit => emit.Invoke(Accessor.GetMethod(TryGetSeat)!));
            break;
        }

        return ctx;
    }

    private static bool TryGetSeat(InteractableVehicle vehicle, out byte seat, Player unturnedPlayer)
    {
        if (!vehicle.tryAddPlayer(out seat, unturnedPlayer))
            return false;

        ILifetimeScope serviceProvider = WarfareModule.Singleton.IsLayoutActive()
            ? WarfareModule.Singleton.ScopedProvider
            : WarfareModule.Singleton.ServiceProvider;

        IPlayerService playerService = serviceProvider.Resolve<IPlayerService>();

        WarfarePlayer player = playerService.GetOnlinePlayer(unturnedPlayer);

        WarfareVehicle warfareVehicle = vehicle.transform.GetComponent<WarfareVehicleComponent>().WarfareVehicle;

        EnterVehicleRequested args = new EnterVehicleRequested
        {
            Player = player,
            Vehicle = warfareVehicle,
            Seat = seat
        };

        EventContinuations.Dispatch(args, WarfareModule.EventDispatcher, player.DisconnectToken, out bool shouldAllow, static args =>
        {
            if (!args.Player.IsOnline
                || args.Player.UnturnedPlayer.life.isDead
                || args.Player.UnturnedPlayer.equipment.isBusy
                || args.Player.UnturnedPlayer.equipment.HasValidUseable && !args.Player.UnturnedPlayer.equipment.IsEquipAnimationFinished
                || args.Vehicle.Vehicle.isDead
                || args.Vehicle.Vehicle.isExploded
                || args.Player.UnturnedPlayer.movement.getVehicle() != null
                || args.Vehicle.Vehicle.passengers[args.Seat].player != null
                || (args.Vehicle.Vehicle.transform.position - args.Player.Position).sqrMagnitude > 15 * 15
                || !args.Vehicle.Vehicle.checkEnter(args.Player.UnturnedPlayer))
            {
                return;
            }
            
            EnterVehicle(args.Vehicle.Vehicle, (byte)args.Seat, args.Player.UnturnedPlayer);
        });

        seat = (byte)args.Seat;
        return shouldAllow;
    }

    private static void EnterVehicle(InteractableVehicle vehicle, byte seat, Player player)
    {
        Transform seatTransform = vehicle.passengers[seat].seat;

        Vector3 seatPos = seatTransform.position;
        if ((Physics.Raycast(seatPos, Vector3.up, out RaycastHit hit, 2f, RayMasks.BLOCK_ENTRY, QueryTriggerInteraction.Ignore)
             || Physics.Linecast(seatPos, player.transform.position + Vector3.up, out hit, RayMasks.BLOCK_ENTRY, QueryTriggerInteraction.Ignore))
            && !hit.transform.IsChildOf(vehicle.transform))
        {
            return;
        }

        SendEnterVehicle!.InvokeAndLoopback(ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), vehicle.instanceID, seat, player.channel.owner.playerID.steamID);
    }
}
using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Uncreated.Warfare.Events.Components;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Patches;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Events.Patches;

[UsedImplicitly]
internal sealed class PlayerEquipmentReceiveEquip : IHarmonyPatch
{
    private static MethodInfo? _target;

    void IHarmonyPatch.Patch(ILogger logger, Harmony patcher)
    {
        _target = typeof(PlayerEquipment).GetMethod(nameof(PlayerEquipment.ReceiveEquip), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        if (_target != null)
        {
            patcher.Patch(_target, prefix: Accessor.GetMethod(Prefix));
            logger.LogDebug("Patched {0} for pre-equip item event.", _target);
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            new MethodDefinition(nameof(PlayerEquipment.ReceiveEquip))
                .DeclaredIn<PlayerEquipment>(isStatic: true)
                .WithParameter<byte>("page")
                .WithParameter<byte>("x")
                .WithParameter<byte>("y")
                .WithParameter<Guid>("newAssetGuid")
                .WithParameter<byte>("newQuality")
                .WithParameter<byte[]>("newState")
                .WithParameter<NetId>("useableNetId")
                .ReturningVoid()
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger, Harmony patcher)
    {
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(Prefix));
        logger.LogDebug("Unpatched {0} for pre-equip item event.", _target);
        _target = null;
    }

    private static void Prefix(PlayerEquipment __instance, byte page, byte x, byte y, Guid newAssetGuid, byte newQuality, byte[] newState, NetId useableNetId)
    {
        IPlayerService playerService = WarfareModule.Singleton.ServiceProvider.Resolve<IPlayerService>();
        WarfarePlayer player = playerService.GetOnlinePlayer(__instance);

        InteractableVehicle vehicle = player.UnturnedPlayer.movement.getVehicle();
        
        if (vehicle != null && player.UnturnedPlayer.equipment.asset is ItemGunAsset gun)
        {
            byte seat = player.UnturnedPlayer.movement.getSeat();
            if (vehicle.passengers[seat].turret.itemID == gun.id)
            {
                player.Data["LastEquippedItem"] = new LastEquipData { Vehicle = vehicle, Seat = seat };
                return;
            }
        }

        ItemJar? equippedItem = player.GetHeldItem(out _);
        if (equippedItem == null)
        {
            player.Data.TryRemove("LastEquippedItem", out _);
        }
        else
        {
            player.Data["LastEquippedItem"] = new LastEquipData { Item = equippedItem };
        }
    }
}

internal class LastEquipData
{
    public ItemJar? Item { get; init; }
    public InteractableVehicle? Vehicle { get; init; }
    public byte Seat { get; init; }
}
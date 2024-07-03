using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using SDG.Unturned;
using Steamworks;
using System;
using System.Reflection;
using Uncreated.Warfare.Events.Vehicles;
using Uncreated.Warfare.Harmony;
using Uncreated.Warfare.Patches;
using UnityEngine;
using static Uncreated.Warfare.Harmony.Patches;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Uncreated.Warfare.Events.Patches;

[UsedImplicitly]
internal class VehicleManagerAddVehicle : IHarmonyPatch
{
    private static MethodInfo? _target;

    void IHarmonyPatch.Patch(ILogger logger)
    {
        _target = typeof(VehicleManager).GetMethod("addVehicle", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance, null, CallingConventions.Any,
            [ 
                typeof(Guid), typeof(ushort), typeof(ushort), typeof(float), typeof(Vector3), typeof(Quaternion),
                typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(ushort), typeof(bool), typeof(ushort), typeof(ushort),
                typeof(CSteamID), typeof(CSteamID), typeof(bool), typeof(CSteamID[]), typeof(byte[][]), typeof(uint), typeof(byte),
                typeof(NetId), typeof(Color32) 
            ],
            null
        );

        if (_target != null)
        {
            Patcher.Patch(_target, postfix: PatchUtil.GetMethodInfo(Postfix));
            logger.LogDebug("Patched {0} for vehicle spawned event.", Accessor.Formatter.Format(_target));
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            Accessor.Formatter.Format(new MethodDefinition("addVehicle")
                .DeclaredIn<VehicleManager>(isStatic: false)
                .WithParameter<Guid>("assetGuid")
                .WithParameter<ushort>("skinID")
                .WithParameter<ushort>("mythicID")
                .WithParameter<float>("roadPosition")
                .WithParameter<Vector3>("point")
                .WithParameter<Quaternion>("angle")
                .WithParameter<bool>("sirens")
                .WithParameter<bool>("blimp")
                .WithParameter<bool>("headlights")
                .WithParameter<bool>("taillights")
                .WithParameter<ushort>("fuel")
                .WithParameter<bool>("isExploded")
                .WithParameter<ushort>("health")
                .WithParameter<ushort>("batteryCharge")
                .WithParameter<CSteamID>("owner")
                .WithParameter<CSteamID>("group")
                .WithParameter<bool>("locked")
                .WithParameter<CSteamID[]>("passengers")
                .WithParameter<byte[][]>("turrets")
                .WithParameter<uint>("instanceID")
                .WithParameter<byte>("tireAliveMask")
                .WithParameter<NetId>("netId")
                .WithParameter<Color32>("paintColor")
                .Returning<InteractableVehicle>()
            )
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger)
    {
        if (_target == null)
            return;

        Patcher.Unpatch(_target, PatchUtil.GetMethodInfo(Postfix));
        logger.LogDebug("Unpatched {0} for destroy structure event.", Accessor.Formatter.Format(_target));
        _target = null;
    }

    // SDG.Unturned.VehicleManager.addVehicle
    /// <summary>
    /// Postfix of <see cref="VehicleManager.addVehicle(Guid,ushort,ushort,float,Vector3,Quaternion,bool,bool,bool,bool,ushort,bool,ushort,ushort,CSteamID,CSteamID,bool,CSteamID[],byte[][],uint,byte,NetId,Color32)"/> to invoke <see cref="VehicleSpawned"/>.
    /// </summary>
    private static void Postfix(Guid assetGuid, ushort skinID, ushort mythicID, float roadPosition, Vector3 point, Quaternion angle,
        bool sirens, bool blimp, bool headlights, bool taillights, ushort fuel, bool isExploded, ushort health, ushort batteryCharge, CSteamID owner,
        CSteamID group, bool locked, CSteamID[] passengers, byte[][] turrets, uint instanceID, byte tireAliveMask, NetId netId, Color32 paintColor, InteractableVehicle __result)
    {
        if (__result == null)
            return;

        VehicleSpawned args = new VehicleSpawned
        {
            Vehicle = __result
        };

        _ = WarfareModule.EventDispatcher.DispatchEventAsync(args);
    }
}
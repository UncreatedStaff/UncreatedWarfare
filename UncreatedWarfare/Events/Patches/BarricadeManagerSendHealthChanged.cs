using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using System.Reflection;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Patches;

namespace Uncreated.Warfare.Events.Patches;

[UsedImplicitly]
internal sealed class BarricadeManagerSendHealthChanged : IHarmonyPatch
{
    private static MethodInfo? _target;

    internal static DamageBarricadeRequested? LastDamageRequestedEvent;

    void IHarmonyPatch.Patch(ILogger logger, Harmony patcher)
    {
        _target = typeof(BarricadeManager).GetMethod("sendHealthChanged", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

        if (_target != null)
        {
            patcher.Patch(_target, postfix: Accessor.GetMethod(BarricadePostfix));
            logger.LogDebug($"Patched {_target} for listening for post-barricade damage.");
        }
        else
        {
            logger.LogError("Failed to find method: {0}.",
                new MethodDefinition("sendHealthChanged")
                    .DeclaredIn<BarricadeManager>(isStatic: false)
                    .WithParameter<byte>("x")
                    .WithParameter<byte>("y")
                    .WithParameter<ushort>("plant")
                    .WithParameter<BarricadeDrop>("barricade")
                    .ReturningVoid()
            );
        }
    }

    void IHarmonyPatch.Unpatch(ILogger logger, Harmony patcher)
    {
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(BarricadePostfix));
        logger.LogDebug($"Unpatched {_target} for listening for post-barricade damage.");
        _target = null;
    }

    private static void BarricadePostfix(byte x, byte y, ushort plant, BarricadeDrop barricade)
    {
        DamageBarricadeRequested? reqArgs = LastDamageRequestedEvent;
        if (reqArgs == null || reqArgs.Barricade != barricade)
        {
            WarfareModule.Singleton.GlobalLogger.LogWarning($"[BarricadeManagerSendHealthChanged] Unable to identify damage requested event for barricade {barricade.asset}.");
            return;
        }

        BarricadeDamaged args = new BarricadeDamaged
        {
            ServersideData = reqArgs.ServersideData,
            Barricade = reqArgs.Barricade,
            DamageOrigin = reqArgs.DamageOrigin,
            PrimaryAsset = reqArgs.PrimaryAsset,
            SecondaryAsset = reqArgs.SecondaryAsset,
            Region = reqArgs.Region,
            VehicleRegionIndex = reqArgs.VehicleRegionIndex,
            Damage = reqArgs.PendingDamage,
            InstanceId = reqArgs.InstanceId,
            Instigator = reqArgs.Instigator,
            InstigatorId = reqArgs.InstigatorId,
            InstigatorTeam = reqArgs.InstigatorTeam,
            RegionIndex = reqArgs.RegionIndex,
            RegionPosition = reqArgs.RegionPosition
        };

        _ = WarfareModule.EventDispatcher.DispatchEventAsync(args);
    }
}
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
internal sealed class BarricadeManagerDestroyBarricade : IHarmonyPatch
{
    private static MethodInfo? _target;

    void IHarmonyPatch.Patch(ILogger logger, Harmony patcher)
    {
        _target = Accessor.GetMethod(new Action<BarricadeDrop, byte, byte, ushort>(BarricadeManager.destroyBarricade));

        if (_target != null)
        {
            patcher.Patch(_target, transpiler: Accessor.GetMethod(Transpiler));
            logger.LogDebug("Patched {0} for destroy barricade event.", _target);
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            new MethodDefinition(nameof(BarricadeManager.destroyBarricade))
                .DeclaredIn<BarricadeManager>(isStatic: true)
                .WithParameter<BarricadeDrop>("barricade")
                .WithParameter<byte>("x")
                .WithParameter<byte>("y")
                .WithParameter<ushort>("plant")
                .ReturningVoid()
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger, Harmony patcher)
    {
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(Transpiler));
        logger.LogDebug("Unpatched {0} for destroy barricade event.", _target);
        _target = null;
    }

    // SDG.Unturned.BarricadeManager
    /// <summary>
    /// Transpiler for <see cref="BarricadeManager.destroyBarricade(BarricadeDrop, byte, byte, ushort)"/> to invoke <see cref="BarricadeDestroyed"/>.
    /// </summary>
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        FieldInfo? rpc = typeof(BarricadeManager).GetField("SendDestroyBarricade", BindingFlags.NonPublic | BindingFlags.Static);
        if (rpc == null)
        {
            WarfareModule.Singleton.GlobalLogger.LogWarning("Unable to find field: BarricadeManager.SendDestroyBarricade");
        }

        bool one = false;
        foreach (CodeInstruction instruction in instructions)
        {
            if (!one && rpc != null && instruction.LoadsField(rpc))
            {
                CodeInstruction call = new CodeInstruction(OpCodes.Ldarg_0);
                call.labels.AddRange(instruction.labels);
                yield return call;
                yield return new CodeInstruction(OpCodes.Ldarg_1);
                yield return new CodeInstruction(OpCodes.Ldarg_2);
                yield return new CodeInstruction(OpCodes.Ldarg_3);
                yield return new CodeInstruction(OpCodes.Call, Accessor.GetMethod(DestroyBarricadeInvoker));
                WarfareModule.Singleton.GlobalLogger.LogDebug("Inserted DestroyBarricadeInvoker call to BarricadeManager.destroyBarricade.");
                CodeInstruction old = new CodeInstruction(instruction);
                old.labels.Clear();
                yield return old;
                one = true;
                continue;
            }

            yield return instruction;
        }
    }

    private static void DestroyBarricadeInvoker(BarricadeDrop barricade, byte x, byte y, ushort plant)
    {
        if (barricade == null)
            return;

        BarricadeRegion region;
        if (plant == ushort.MaxValue)
        {
            if (!Regions.checkSafe(x, y))
                return;

            region = BarricadeManager.regions[x, y];
        }
        else if (plant >= BarricadeManager.vehicleRegions.Count)
        {
            region = BarricadeManager.vehicleRegions[plant];
        }
        else
        {
            return;
        }

        ulong destroyer;
        EDamageOrigin origin = EDamageOrigin.Unknown;
        bool wasSalvaged = false;
        if (barricade.model.TryGetComponent(out DestroyerComponent comp))
        {
            destroyer = comp.Destroyer;
            float time = comp.RelevantTime;
            if (destroyer != 0 && Time.realtimeSinceStartup - time > 1f)
                destroyer = 0ul;
            else
            {
                origin = comp.DamageOrigin;
                wasSalvaged = comp.Salvaged;
            }
            Object.Destroy(comp);
        }
        else
        {
            destroyer = 0ul;
        }

        IPlayerService playerService = WarfareModule.Singleton.ServiceProvider.Resolve<IPlayerService>();

        WarfarePlayer? player = playerService.GetOnlinePlayerOrNull(destroyer);

        BarricadeDestroyed args = new BarricadeDestroyed
        {
            Instigator = player,
            Region = region,
            Barricade = barricade,
            ServersideData = barricade.GetServersideData(),
            InstanceId = barricade.instanceID,
            RegionPosition = new RegionCoord(x, y),
            VehicleRegionIndex = plant,
            DamageOrigin = origin,
            InstigatorId = new CSteamID(destroyer),
            WasSalvaged = wasSalvaged,
            // todo Primary and Secondary assets need filling
            InstigatorTeam = player?.Team ?? Team.NoTeam
        };

        BuildableExtensions.SetDestroyInfo(barricade.model, args, null);

        _ = WarfareModule.EventDispatcher.DispatchEventAsync(args);
    }
}
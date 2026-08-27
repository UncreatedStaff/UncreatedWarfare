using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Emit;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Uncreated.Warfare.Patches;

internal sealed class LastEffectSpawnedOnDedicatedServerPatch : IHarmonyPatch
{
    private static MethodInfo? _target;
    void IHarmonyPatch.Patch(ILogger logger, Harmony patcher)
    {
        _target = typeof(EffectManager).GetMethod(
            "internalSpawnEffect",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static,
            null,
            CallingConventions.Any,
            [ typeof(EffectAsset), typeof(Vector3), typeof(Quaternion), typeof(Vector3), typeof(bool), typeof(Transform) ],
            null
        );

        if (_target != null)
        {
            patcher.Patch(_target, transpiler: Accessor.GetMethod(Transpiler));
            logger.LogDebug("Patched {0} for getting the last spawned effect.", _target);
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            new MethodDefinition("SendRegion")
                .DeclaredIn<EffectManager>(isStatic: true)
                .WithParameter<EffectAsset>("asset")
                .WithParameter<Vector3>("point")
                .WithParameter<Quaternion>("rotation")
                .WithParameter<Vector3>("scaleMultiplier")
                .WithParameter<bool>("wasInstigatedByPlayer")
                .WithParameter<Transform>("parent")
                .ReturningVoid()
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger, Harmony patcher)
    {
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(Transpiler));
        logger.LogDebug("Unpatched {0} for overriding send region.", _target);
        _target = null;
    }

    internal static PoolReference? LastEffectSpawned;

    private static readonly FieldInfo LastEffectSpawnedField = typeof(LastEffectSpawnedOnDedicatedServerPatch)
        .GetField(nameof(LastEffectSpawned), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new MissingFieldException($"Field not found: {nameof(LastEffectSpawned)}.");

    // SDG.Unturned.EffectManager
    /// <summary>
    /// Prefix for saving the last effect spawned by <see cref="EffectManager.triggerEffect"/>.
    /// </summary>
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method, ILGenerator generator)
    {
        TranspileContext ctx = new TranspileContext(method, generator, instructions);

        MethodInfo? instantiateMethod = typeof(GameObjectPoolDictionary).GetMethod(
            nameof(GameObjectPoolDictionary.Instantiate),
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            CallingConventions.Any,
            [ typeof(GameObject), typeof(Vector3), typeof(Quaternion) ],
            null
        );

        if (instantiateMethod == null)
        {
            return ctx.Fail(new MethodDefinition(nameof(GameObjectPoolDictionary.Instantiate))
                .DeclaredIn<GameObjectPoolDictionary>(isStatic: false)
                .WithParameter<GameObject>("prefab")
                .WithParameter<Vector3>("position")
                .WithParameter<Quaternion>("rotation")
                .Returning<PoolReference>()
            );
        }

        bool patched = false;
        while (ctx.MoveNext())
        {
            if (!ctx.Instruction.Calls(instantiateMethod))
                continue;

            ctx.EmitBelow(emit =>
            {
                emit.Duplicate()
                    .SetStaticFieldValue(LastEffectSpawnedField);
            });
            patched = true;
            break;
        }

        if (!patched)
        {
            ctx.LogError($"Failed to find invocation of {Accessor.Formatter.Format(instantiateMethod)}.");
        }

        return ctx;
    }
}
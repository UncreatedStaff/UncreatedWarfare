using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Emit;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using System;
using System.Reflection;
using System.Reflection.Emit;
using TMPro;

namespace Uncreated.Warfare.Patches;

[UsedImplicitly]
internal sealed class ServerPrefabUtilRemoveClientComponentsFix : IHarmonyPatch
{
    private static MethodInfo? _target;
    void IHarmonyPatch.Patch(ILogger logger, Harmony patcher)
    {
        Type? type = typeof(Provider).Assembly.GetType("SDG.Unturned.ServerPrefabUtil", throwOnError: false);
        _target = type?.GetMethod("RemoveClientComponents", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

        if (_target != null)
        {
            patcher.Patch(_target, transpiler: Accessor.GetMethod(Transpiler));
            logger.LogDebug("Patched {0} for fixing bug with TMPro components on objects.", _target);
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            new MethodDefinition("explode")
                .DeclaredIn("ServerPrefabUtil", isStatic: true)
                .WithParameter<GameObject>("gameObject")
                .WithParameter<Asset>("context")
                .ReturningVoid()
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger, Harmony patcher)
    {
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(Transpiler));
        logger.LogDebug("Unpatched {0} for fixing bug with TMPro components on objects.", _target);
        _target = null;
    }

    // major tech debt: This prefix patch is doing too much
    
    // SDG.Unturned.InteractableVehicle
    /// <summary>
    /// Overriding prefix of <see cref="InteractableVehicle.explode"/> to set an instigator.
    /// </summary>
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method, ILGenerator generator)
    {
        TranspileContext ctx = new TranspileContext(method, generator, instructions);

        FieldInfo? workingComponents = method.DeclaringType!.GetField("workingComponents", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        if (workingComponents == null || workingComponents.FieldType != typeof(List<Component>))
        {
            return ctx.Fail(
                new FieldDefinition("workingComponents")
                    .DeclaredIn(method.DeclaringType, isStatic: true)
                    .WithFieldType(typeof(List<Component>))
            );
        }

        MethodInfo? sortMethod = typeof(List<Component>).GetMethod("Sort", [ typeof(Comparison<Component>) ]);
        if (sortMethod == null)
        {
            return ctx.Fail(
                new MethodDefinition("Sort")
                    .DeclaredIn<List<Component>>(isStatic: false)
                    .WithParameter<Comparison<Component>>("comparison")
                    .ReturningVoid()
            );
        }

        while (ctx.MoveNext())
        {
            if (!ctx.Instruction.Calls(sortMethod))
                continue;

            int endIndex = ctx.CaretIndex;
            int startIndex = -1;
            while (ctx.MoveBack())
            {
                if (!ctx.Instruction.LoadsField(workingComponents))
                    continue;

                startIndex = ctx.CaretIndex;
                break;
            }

            if (startIndex < 0)
            {
                ctx.LogError("Failed to find sort part of RemoveClientComponents.");
                break;
            }

            ctx.CaretIndex = startIndex + 1;
            ctx.Replace(endIndex - startIndex, emit =>
            {
                emit.Invoke(SortListProperlyMethod);
            });
        }

        return ctx;
    }

    private static readonly MethodInfo SortListProperlyMethod = typeof(ServerPrefabUtilRemoveClientComponentsFix)
        .GetMethod(nameof(SortListProperly), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
        ?? throw new MissingMethodException("SortListProperly not found.");

    private static void SortListProperly(List<Component> components)
    {
        int head = 0;
        for (int i = 0; i < components.Count; ++i)
        {
            Component comp = components[i];
            if (comp is not (TextMeshPro or TextMesh or LODGroupAdditionalData or EnableDopplerEffect or MusicAudioSource))
                continue;

            Component swap = components[head];
            components[head] = comp;
            components[i] = swap;
            ++head;
        }
    }
}
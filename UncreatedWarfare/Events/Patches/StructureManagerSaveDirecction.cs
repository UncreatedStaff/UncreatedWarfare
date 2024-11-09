using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using System.Reflection;
using Uncreated.Warfare.Patches;

namespace Uncreated.Warfare.Events.Patches;

[UsedImplicitly]
internal class StructureManagerSaveDirecction : IHarmonyPatch
{
    private static MethodInfo? _target;
    void IHarmonyPatch.Patch(ILogger logger, HarmonyLib.Harmony patcher)
    {
        _target = Accessor.GetMethod(StructureManager.damage);

        if (_target != null)
        {
            patcher.Patch(_target, prefix: Accessor.GetMethod(Prefix));
            logger.LogDebug("Patched {0} for structure damage (get direction) event.", _target);
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            new MethodDefinition(nameof(StructureManager.damage))
                .DeclaredIn<StructureManager>(isStatic: true)
                .WithParameter<Transform>("transform")
                .WithParameter<Vector3>("direction")
                .WithParameter<float>("damage")
                .WithParameter<float>("times")
                .WithParameter<bool>("armor")
                .WithParameter<CSteamID>("instigatorSteamID")
                .WithParameter<EDamageOrigin>("damageOrigin")
                .ReturningVoid()
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger, HarmonyLib.Harmony patcher)
    {
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(Prefix));
        logger.LogDebug("Unpatched {0} for structure damage (get direction) event.", _target);
        _target = null;
    }

    internal static Vector3 LastDirection;

    // SDG.Unturned.StructureManager
    /// <summary>
    /// Prefix for <see cref="StructureManager.damage"/> to save the direction for continuations.
    /// </summary>
    private static void Prefix(Transform transform, Vector3 direction, float damage, float times, bool armor, CSteamID instigatorSteamID, EDamageOrigin damageOrigin)
    {
        LastDirection = direction;
    }
}

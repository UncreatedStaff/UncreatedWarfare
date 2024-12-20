using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using System.Reflection;

namespace Uncreated.Warfare.Patches;

[UsedImplicitly]
internal sealed class CancelReputationPatch : IHarmonyPatch
{
    internal static bool IsSettingReputation;

    private static MethodInfo? _target;
    void IHarmonyPatch.Patch(ILogger logger, HarmonyLib.Harmony patcher)
    {
        _target = typeof(PlayerSkills).GetMethod(nameof(PlayerSkills.askRep), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (_target != null)
        {
            patcher.Patch(_target, prefix: Accessor.GetMethod(Prefix));
            logger.LogDebug("Patched {0} for cancelling vanilla reputation.", _target);
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            new MethodDefinition(nameof(PlayerSkills.askRep))
                .DeclaredIn<PlayerSkills>(isStatic: false)
                .WithParameter<int>("rep")
                .ReturningVoid()
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger, HarmonyLib.Harmony patcher)
    {
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(Prefix));
        logger.LogDebug("Unpatched {0} for cancelling vanilla reputation.", _target);
        _target = null;
    }

    // SDG.Unturned.PlayerLife
    /// <summary>
    /// Prefix for <see cref="PlayerSkills.askRep"/> to cancel vanilla reputation.
    /// </summary>
    private static bool Prefix(int rep)
    {
        return IsSettingReputation;
    }
}

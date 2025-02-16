using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using System.Reflection;

namespace Uncreated.Warfare.Patches;

[UsedImplicitly]
internal sealed class ProjectileSolvingDataCapturePatch : IHarmonyPatch
{
    private static MethodInfo? _target;
    void IHarmonyPatch.Patch(ILogger logger, Harmony patcher)
    {
        _target = typeof(UseableGun).GetMethod("project", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (_target != null)
        {
            patcher.Patch(_target, prefix: Accessor.GetMethod(Prefix));
            logger.LogDebug("Patched {0} for allowing projectile solving.", _target);
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            new MethodDefinition("project")
                .DeclaredIn<UseableGun>(isStatic: false)
                .WithNoParameters()
                .ReturningVoid()
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger, Harmony patcher)
    {
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(Prefix));
        logger.LogDebug("Unpatched {0} for allowing projectile solving.", _target);
        _target = null;
    }

    internal static UseableGun? LastGun;
    internal static ItemMagazineAsset? LastMagazineAsset;
    internal static ItemBarrelAsset? LastBarrelAsset;
    internal static Vector3 LastOrigin;
    internal static Vector3 LastDirection;

    // SDG.Unturned.UseableGun
    /// <summary>
    /// Postfix of <see cref="UseableGun.project(Vector3, Vector3, ItemBarrelAsset, ItemMagazineAsset)"/> to predict mortar hits.
    /// </summary>
    private static void Prefix(Vector3 origin, Vector3 direction, ItemBarrelAsset barrelAsset, ItemMagazineAsset magazineAsset, UseableGun __instance)
    {
        LastGun = __instance;
        LastOrigin = origin;
        LastDirection = direction;
        LastMagazineAsset = magazineAsset;
        LastBarrelAsset = barrelAsset;
    }
}
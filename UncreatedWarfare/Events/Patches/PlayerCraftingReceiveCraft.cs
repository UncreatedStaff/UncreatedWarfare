using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using System.Reflection;
using Uncreated.Warfare.Patches;

namespace Uncreated.Warfare.Events.Patches;

[UsedImplicitly]
internal sealed class PlayerCraftingReceiveCraft : IHarmonyPatch
{
    private static MethodInfo? _target;
    void IHarmonyPatch.Patch(ILogger logger, Harmony patcher)
    {
        _target = typeof(PlayerCrafting).GetMethod(nameof(PlayerCrafting.ReceiveCraft),
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (_target != null)
        {
            patcher.Patch(_target, prefix: Accessor.GetMethod(Prefix));
            logger.LogDebug("Patched {0} for receive craft (get force) event.", _target);
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            new MethodDefinition(nameof(PlayerCrafting.ReceiveCraft))
                .DeclaredIn<PlayerCrafting>(isStatic: false)
                .WithParameter<ServerInvocationContext>("context", ByRefTypeMode.In)
                .WithParameter<ushort>("id")
                .WithParameter<byte>("index")
                .WithParameter<bool>("force")
                .ReturningVoid()
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger, Harmony patcher)
    {
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(Prefix));
        logger.LogDebug("Unpatched {0} for receive craft (get force) event.", _target);
        _target = null;
    }

    internal static bool LastCraftAll;

    // SDG.Unturned.StructureManager
    /// <summary>
    /// Prefix for <see cref="PlayerCrafting.ReceiveCraft"/> to save the last value of <paramref name="force"/> (craft all).
    /// </summary>
    private static void Prefix(in ServerInvocationContext context, ushort id, byte index, bool force)
    {
        LastCraftAll = force;
    }
}

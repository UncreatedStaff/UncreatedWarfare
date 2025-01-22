using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using System.Reflection;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Patches;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Events.Patches;

[UsedImplicitly]
internal sealed class InteractableSignUpdateText : IHarmonyPatch
{
    private static MethodInfo? _target;

    void IHarmonyPatch.Patch(ILogger logger, Harmony patcher)
    {
        _target = typeof(InteractableSign).GetMethod(nameof(InteractableSign.updateText), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (_target != null)
        {
            patcher.Patch(_target, postfix: Accessor.GetMethod(Postfix));
            logger.LogDebug("Patched {0} for sign text updated event.", _target);
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            new MethodDefinition(nameof(InteractableSign.updateText))
                .DeclaredIn<InteractableSign>(isStatic: false)
                .WithParameter<string>("newText")
                .ReturningVoid()
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger, Harmony patcher)
    {
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(Postfix));
        logger.LogDebug("Unpatched {0} for sign text updated event.", _target);
        _target = null;
    }

    // SDG.Unturned.InteractableSign
    /// <summary>
    /// Postfix of <see cref="InteractableSign.updateText(string)"/> to invoke <see cref="SignTextChanged"/>.
    /// </summary>
    private static void Postfix(InteractableSign __instance, string newText)
    {
        if (!BarricadeManager.tryGetRegion(__instance.transform, out byte x, out byte y, out ushort plant, out BarricadeRegion region))
        {
            return;
        }

        BarricadeDrop? drop = region.FindBarricadeByRootTransform(__instance.transform);
        if (drop == null)
        {
            return;
        }

        BarricadeData data = drop.GetServersideData();

        IContainer serviceProvider = WarfareModule.Singleton.ServiceProvider;

        WarfarePlayer? instigator = null;
        BuildableContainer container = BuildableContainer.Get(drop);
        if (container.SignEditFrame.IsValid)
        {
            instigator = serviceProvider.Resolve<IPlayerService>().GetOnlinePlayerOrNull(container.SignEditor);
        }

        SignTextChanged args = new SignTextChanged
        {
            Barricade = drop,
            Instigator = instigator,
            RegionPosition = new RegionCoord(x, y),
            VehicleRegionIndex = plant,
            Region = region,
            Sign = __instance,
            ServersideData = data
        };

        _ = WarfareModule.EventDispatcher.DispatchEventAsync(args, CancellationToken.None);
    }
}
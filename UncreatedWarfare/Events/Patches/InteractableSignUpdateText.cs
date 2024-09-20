using Autofac;
using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using System.Reflection;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Patches;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using static Uncreated.Warfare.Harmony.Patches;

namespace Uncreated.Warfare.Events.Patches;

[UsedImplicitly]
internal class InteractableSignUpdateText : IHarmonyPatch
{
    private static MethodInfo? _target;

    void IHarmonyPatch.Patch(ILogger logger)
    {
        _target = typeof(InteractableSign).GetMethod(nameof(InteractableSign.updateText), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (_target != null)
        {
            Patcher.Patch(_target, postfix: Accessor.GetMethod(Postfix));
            logger.LogDebug("Patched {0} for sign text updated event.", Accessor.Formatter.Format(_target));
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            Accessor.Formatter.Format(new MethodDefinition(nameof(InteractableSign.updateText))
                .DeclaredIn<InteractableSign>(isStatic: false)
                .WithParameter<string>("newText")
                .ReturningVoid()
            )
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger)
    {
        if (_target == null)
            return;

        Patcher.Unpatch(_target, Accessor.GetMethod(Postfix));
        logger.LogDebug("Unpatched {0} for sign text updated event.", Accessor.Formatter.Format(_target));
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
        if (drop.model.TryGetComponent(out BarricadeComponent comp) && comp.EditTick >= serviceProvider.Resolve<WarfareTimeComponent>().Updates)
        {
            instigator = serviceProvider.Resolve<IPlayerService>().GetOnlinePlayerOrNull(comp.LastEditor);
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
using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using System.Reflection;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Patches;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Events.Patches;

[UsedImplicitly]
internal class InteractableBedReceiveClaimRequest : IHarmonyPatch
{
    private static MethodInfo? _target;

    void IHarmonyPatch.Patch(ILogger logger, HarmonyLib.Harmony patcher)
    {
        _target = typeof(InteractableBed).GetMethod(nameof(InteractableBed.ReceiveClaimRequest), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (_target != null)
        {
            patcher.Patch(_target, prefix: Accessor.GetMethod(Prefix));
            logger.LogDebug("Patched {0} for bedroll claim requested.", _target);
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            new MethodDefinition(nameof(InteractableBed.ReceiveClaimRequest))
                .DeclaredIn<InteractableBed>(isStatic: false)
                .WithParameter<string>("newText")
                .ReturningVoid()
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger, HarmonyLib.Harmony patcher)
    {
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(Prefix));
        logger.LogDebug("Unpatched {0} for sign text updated event.", _target);
        _target = null;
    }

    // SDG.Unturned.InteractableSign
    /// <summary>
    /// Postfix of <see cref="InteractableBed.ReceiveClaimRequest"/> to invoke <see cref="SignTextChanged"/>.
    /// </summary>
    private static bool Prefix(InteractableBed __instance, in ServerInvocationContext context)
    {
        if (!BarricadeManager.tryGetRegion(__instance.transform, out byte x, out byte y, out ushort plant, out BarricadeRegion region))
        {
            return true;
        }

        BarricadeDrop? drop = region.FindBarricadeByRootTransform(__instance.transform);
        if (drop == null)
        {
            return true;
        }

        BarricadeData data = drop.GetServersideData();

        IContainer serviceProvider = WarfareModule.Singleton.ServiceProvider;

        WarfarePlayer instigator = serviceProvider.Resolve<IPlayerService>().GetOnlinePlayer(context.GetPlayer());

        ClaimBedRequested args = new ClaimBedRequested
        {
            Barricade = drop,
            Player = instigator,
            RegionPosition = new RegionCoord(x, y),
            VehicleRegionIndex = plant,
            Region = region,
            ServersideData = data,
            Bed = __instance
        };

        EventContinuations.Dispatch(args, WarfareModule.EventDispatcher, default, out bool shouldAllow, args =>
        {
            if (args.ServersideData.barricade.isDead)
                return;

            if (args.Bed.isClaimed)
                BarricadeManager.ServerUnclaimBed(args.Bed);
            else
                BarricadeManager.ServerClaimBedForPlayer(args.Bed, args.PlayerObject);
        });

        return shouldAllow;
    }
}
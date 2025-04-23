using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using System.Reflection;
using Uncreated.Warfare.Events.Models.Barricades;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Interaction;
using Uncreated.Warfare.Patches;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Players.Permissions;

namespace Uncreated.Warfare.Events.Patches;

[UsedImplicitly]
internal sealed class PlayerDropMarkerRequest : IHarmonyPatch
{
    private static MethodInfo? _target;

    void IHarmonyPatch.Patch(ILogger logger, Harmony patcher)
    {
        _target = typeof(PlayerQuests).GetMethod(nameof(PlayerQuests.replicateSetMarker), BindingFlags.Instance | BindingFlags.Public);

        if (_target != null)
        {
            patcher.Patch(_target, prefix: Accessor.GetMethod(Prefix));
            logger.LogDebug("Patched {0} for request drop marker event.", _target);
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            new MethodDefinition(nameof(PlayerQuests.replicateSetMarker))
                .DeclaredIn<PlayerQuests>(isStatic: false)
                .WithParameter<bool>("newIsMarkerPlaced")
                .WithParameter<Vector3>("newMarkerPosition")
                .WithParameter<string>("newMarkerTextOverride")
                .ReturningVoid()
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger, Harmony patcher)
    {
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(Prefix));
        logger.LogDebug("Unpatched {0} for request drop marker event.", _target);
        _target = null;
    }
    
    // SDG.Unturned.PlayerQuests
    /// <summary>
    /// Prefix of <see cref="PlayerQuests.replicateSetMarker"/> to invoke <see cref="PlayerDropMarkerRequested"/>.
    /// </summary>
    private static bool Prefix(ref bool newIsMarkerPlaced, ref Vector3 newMarkerPosition, ref string newMarkerTextOverride, PlayerQuests __instance)
    {
        IContainer serviceProvider = WarfareModule.Singleton.ServiceProvider;
        
        WarfarePlayer warfarePlayer = serviceProvider.Resolve<IPlayerService>().GetOnlinePlayer(__instance.player);
        
        PlayerDropMarkerRequested args = new PlayerDropMarkerRequested
        {
            Player = warfarePlayer,
            MarkerWorldPosition = newMarkerPosition,
            IsNewMarkerBeingPlaced = newIsMarkerPlaced,
            MarkerDisplayText = newMarkerTextOverride
        };
        
        bool shouldAllow = WarfareModule.EventDispatcher.DispatchEventAsync(args, allowAsync: false).GetAwaiter().GetResult();
        
        newIsMarkerPlaced = args.IsNewMarkerBeingPlaced;
        newMarkerPosition = args.MarkerWorldPosition;
        newMarkerTextOverride = args.MarkerDisplayText;

        return shouldAllow;
    }
}
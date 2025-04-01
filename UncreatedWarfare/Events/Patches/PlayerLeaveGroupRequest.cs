using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using System.Reflection;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Patches;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Events.Patches;

[UsedImplicitly]
internal sealed class PlayerLeaveGroupRequest : IHarmonyPatch
{
    private static MethodInfo? _target;

    void IHarmonyPatch.Patch(ILogger logger, Harmony patcher)
    {
        _target = typeof(GroupManager).GetMethod(nameof(GroupManager.requestGroupExit), BindingFlags.Static | BindingFlags.Public);

        if (_target != null)
        {
            patcher.Patch(_target, prefix: Accessor.GetMethod(Prefix));
            logger.LogDebug("Patched {0} for request leave group event.", _target);
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            new MethodDefinition(nameof(GroupManager.requestGroupExit))
                .DeclaredIn<GroupManager>(isStatic: true)
                .WithParameter<Player>("player")
                .ReturningVoid()
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger, Harmony patcher)
    {
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(Prefix));
        logger.LogDebug("Unpatched {0} for request leave group event.", _target);
        _target = null;
    }
    
    // SDG.Unturned.InteractableSign
    /// <summary>
    /// Postfix of <see cref="GroupManager.requestGroupExit"/> to invoke <see cref="PlayerLeaveGroupRequested"/>.
    /// </summary>
    private static bool Prefix(Player player)
    {
        IContainer serviceProvider = WarfareModule.Singleton.ServiceProvider;
        
        WarfarePlayer warfarePlayer = serviceProvider.Resolve<IPlayerService>().GetOnlinePlayer(player);
        
        PlayerLeaveGroupRequested args = new()
        {
            Player = warfarePlayer,
        };

        return WarfareModule.EventDispatcher.DispatchEventAsync(args).GetAwaiter().GetResult();
    }
}
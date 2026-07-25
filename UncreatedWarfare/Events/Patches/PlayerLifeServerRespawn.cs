using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Emit;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using System;
using System.Reflection;
using System.Reflection.Emit;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Patches;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Events.Patches;

[UsedImplicitly]
internal sealed class PlayerLifeServerRespawn : IHarmonyPatch
{
    private static MethodInfo? _target;

    void IHarmonyPatch.Patch(ILogger logger, Harmony patcher)
    {
        _target = typeof(PlayerLife).GetMethod(nameof(PlayerLife.ServerRespawn), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, [ typeof(bool) ], null);

        if (_target != null)
        {
            patcher.Patch(_target, prefix: Accessor.GetMethod(Prefix), postfix: Accessor.GetMethod(Postfix));
            logger.LogDebug("Patched {0} for respawn event.", _target);
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            new MethodDefinition(nameof(PlayerLife.ServerRespawn))
                .DeclaredIn<PlayerLife>(isStatic: false)
                .WithParameter<bool>("atHome")
                .ReturningVoid()
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger, Harmony patcher)
    {
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(Prefix));
        patcher.Unpatch(_target, Accessor.GetMethod(Postfix));
        logger.LogDebug("Unpatched {0} for respawn event.", _target);
        _target = null;
    }

    private static bool _lastAlive;

    private static void Prefix(PlayerLife __instance, bool atHome)
    {
        _lastAlive = __instance.IsAlive;
    }

    private static void Postfix(PlayerLife __instance, bool atHome)
    {
        if (_lastAlive)
            return;

        InvokePlayerRespawned(__instance, atHome);
    }

    private static void InvokePlayerRespawned(PlayerLife playerCaller, bool atHome)
    {
        IPlayerService playerService = WarfareModule.Singleton.ServiceProvider.Resolve<IPlayerService>();

        playerCaller.transform.GetPositionAndRotation(out Vector3 pos, out Quaternion rot);

        PlayerRespawned args = new PlayerRespawned
        {
            Player = playerService.GetOnlinePlayer(playerCaller),
            RespawnPosition = pos,
            RespawnRotation = rot,
            RespawnAngle = rot.eulerAngles.y,
            AtHome = atHome
        };

        _ = WarfareModule.EventDispatcher.DispatchEventAsync(args, CancellationToken.None);
    }
}

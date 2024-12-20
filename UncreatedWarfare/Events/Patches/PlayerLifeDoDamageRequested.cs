using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using System.Reflection;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Patches;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Events.Patches;

[UsedImplicitly]
internal sealed class PlayerLifeDoDamageRequested : IHarmonyPatch
{
    internal static ulong Damaging;

    private static MethodInfo? _target;

    void IHarmonyPatch.Patch(ILogger logger, HarmonyLib.Harmony patcher)
    {
        _target = typeof(PlayerLife).GetMethod("doDamage", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (_target != null)
        {
            patcher.Patch(_target, prefix: Accessor.GetMethod(Prefix));
            logger.LogDebug("Patched {0} for trap trigger events.", _target);
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            new MethodDefinition("OnTriggerEnter")
                .DeclaredIn<PlayerLife>(isStatic: false)
                .WithParameter<byte>("amount")
                .WithParameter<Vector3>("newRagdoll")
                .WithParameter<EDeathCause>("newCause")
                .WithParameter<ELimb>("newLimb")
                .WithParameter<CSteamID>("newKiller")
                .WithParameter<EPlayerKill>("kill", ByRefTypeMode.Out)
                .WithParameter<bool>("trackKill")
                .WithParameter<ERagdollEffect>("newRagdollEffect")
                .WithParameter<bool>("canCauseBleeding")
                .ReturningVoid()
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger, HarmonyLib.Harmony patcher)
    {
        if (_target == null)
            return;
        
        patcher.Unpatch(_target, Accessor.GetMethod(Prefix));
        logger.LogDebug("Unpatched {0} for trap trigger events.", _target);
        _target = null;
    }

    // SDG.Unturned.PlayerLife.doDamage
    /// <summary>
    /// Actual onDamageRequested event.
    /// </summary>
    private static bool Prefix(PlayerLife __instance, byte amount, Vector3 newRagdoll, EDeathCause newCause, ELimb newLimb, CSteamID newKiller, ref EPlayerKill kill, bool trackKill, ERagdollEffect newRagdollEffect, bool canCauseBleeding)
    {
        if (__instance.channel.owner.playerID.steamID.m_SteamID == Damaging)
        {
            // already invoked via other event
            Damaging = 0;
            return true;
        }

        ILifetimeScope serviceProvider = WarfareModule.Singleton.ServiceProvider;
        IPlayerService playerService = serviceProvider.Resolve<IPlayerService>();

        WarfarePlayer player = playerService.GetOnlinePlayer(__instance);

        DamagePlayerParameters parameters = new DamagePlayerParameters(__instance.player)
        {
            applyGlobalArmorMultiplier = false,
            cause = newCause,
            damage = amount,
            direction = newRagdoll.normalized,
            killer = newKiller,
            limb = newLimb,
            player = __instance.player,
            trackKill = trackKill,
            ragdollEffect = newRagdollEffect,
            respectArmor = false
        };

        if (!canCauseBleeding)
            parameters.bleedingModifier = DamagePlayerParameters.Bleeding.Never;

        DamagePlayerRequested args = new DamagePlayerRequested(in parameters, playerService)
        {
            Player = player
        };

        // can't support async event handlers because any code calling damagePlayer
        // may expect the player to take damage or die instantly
        //  ex. hitmarkers are handled by checking which players were damaged immediately after shooting
        bool shouldallow = serviceProvider.Resolve<EventDispatcher>().DispatchEventAsync(args, CancellationToken.None, allowAsync: false).GetAwaiter().GetResult();
        return shouldallow;
    }
}
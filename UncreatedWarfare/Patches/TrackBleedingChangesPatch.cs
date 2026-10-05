using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Emit;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using System;
using System.Reflection;
using System.Reflection.Emit;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Deaths;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Patches;

[UsedImplicitly]
internal sealed class TrackBleedingChangesPatch : IHarmonyPatch
{
    private static MethodInfo? _targetPerformBleeding;
    private static MethodInfo? _targetDoDamage;
    void IHarmonyPatch.Patch(ILogger logger, Harmony patcher)
    {
        _targetPerformBleeding = typeof(UseableConsumeable).GetMethod(
            "performBleeding",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            CallingConventions.Any,
            [ typeof(Player), typeof(ItemConsumeableAsset.Bleeding) ],
            null
        );

        if (_targetPerformBleeding == null)
        {
            logger.LogError("Failed to find method: {0}.",
                new MethodDefinition("performBleeding")
                    .DeclaredIn<UseableConsumeable>(isStatic: false)
                    .WithParameter<Player>("target")
                    .WithParameter<ItemConsumeableAsset.Bleeding>("bleedingModifier")
                    .ReturningVoid()
            );
        }
        else
        {
            patcher.Patch(_targetPerformBleeding, prefix: Accessor.GetMethod(PrefixPerformBleeding));
            logger.LogDebug($"Patched {_targetPerformBleeding} for setting bleeding info for cutter consumables.");

            UseableConsumeable.onPerformingAid += OnPerformingAid;
            UseableConsumeable.onConsumeRequested += OnConsuming;
        }

        try
        {
            _targetDoDamage = typeof(PlayerLife).GetMethod(
                "doDamage",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
            );
        }
        catch (AmbiguousMatchException ex)
        {
            _targetDoDamage = null;
            logger.LogError(ex, "Ambiguous doDamage function.");
        }

        if (_targetDoDamage == null)
        {
            logger.LogError("Failed to find method: {0}.",
                new MethodDefinition("doDamage")
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
        else
        {
            patcher.Patch(_targetDoDamage, transpiler: Accessor.GetMethod(TranspilerDoDamage));
            logger.LogDebug($"Patched {_targetDoDamage} for setting bleeding info for doDamage calls.");
        }

        DamageTool.damagePlayerRequested += OnPlayerAboutToBeDamaged;
    }

    void IHarmonyPatch.Unpatch(ILogger logger, Harmony patcher)
    {
        if (_targetPerformBleeding != null)
        {
            patcher.Unpatch(_targetPerformBleeding, Accessor.GetMethod(PrefixPerformBleeding));
            logger.LogDebug($"Unpatched {_targetPerformBleeding} for setting bleeding info for cutter consumables.");
            _targetPerformBleeding = null;

            UseableConsumeable.onPerformingAid -= OnPerformingAid;
            UseableConsumeable.onConsumeRequested -= OnConsuming;
        }

        if (_targetDoDamage != null)
        {
            patcher.Unpatch(_targetDoDamage, Accessor.GetMethod(TranspilerDoDamage));
            logger.LogDebug($"Unpatched {_targetDoDamage} for setting bleeding info for doDamage calls.");
            _targetDoDamage = null;
        }

        DamageTool.damagePlayerRequested -= OnPlayerAboutToBeDamaged;
    }

    private static Player? _lastAidInstigator;

    private static void OnPerformingAid(Player instigator, Player target, ItemConsumeableAsset asset, ref bool shouldAllow)
    {
        if (!shouldAllow)
            return;

        _lastAidInstigator = instigator;
    }

    private static void OnConsuming(Player instigatingPlayer, ItemConsumeableAsset consumeableAsset, ref bool shouldAllow)
    {
        _lastAidInstigator = null;
    }

    private static void PrefixPerformBleeding(UseableConsumeable __instance, Player target, ItemConsumeableAsset.Bleeding bleedingModifier)
    {
        using IDisposable? profiler = ProfilerUtil.Profile();

        if (bleedingModifier != ItemConsumeableAsset.Bleeding.Cut)
            return;

        Player? instigator = _lastAidInstigator;
        _lastAidInstigator = null;

        IPlayerService playerService = WarfareModule.Singleton.ServiceProvider.Resolve<IPlayerService>();

        PlayerDeathTrackingComponent comp = PlayerDeathTrackingComponent.GetOrAdd(target);

        WarfarePlayer dead = playerService.GetOnlinePlayer(target);

        DamagePlayerParameters parameters = new DamagePlayerParameters(target)
        {
            bleedingModifier = DamagePlayerParameters.Bleeding.Always,
            cause = EDeathCause.FOOD,
            damage = 0f,
            killer = instigator == null ? CSteamID.Nil : instigator.channel.owner.playerID.steamID
        };

        WarfarePlayer? killer = playerService.GetOnlinePlayerOrNull(parameters.killer);
        CurrentKitState? killerKit = killer?.Component<KitPlayerComponent>().ActiveKit;

        CurrentKitState? deadKit = dead.Component<KitPlayerComponent>().ActiveKit;

        PlayerDied e = new PlayerDied(in parameters)
        {
            Player = dead,
            WasBleedout = true,
            Instigator = parameters.killer,
            Cause = EDeathCause.FOOD,
            MessageCause = EDeathCause.FOOD,
            MessageKey = "bleed-out-consumable",
            DeadTeam = dead.Team,
            PrimaryAsset = AssetLink.Create(__instance.GetEquippedAsset()),
            Point = dead.Position,
            WasSuicide = parameters.killer == CSteamID.Nil || parameters.killer == target.channel.owner.playerID.steamID,
            WasTeamkill = killer != null && killer.Team.IsFriendly(dead.Team),
            MessageFlags = DeathFlags.Bleeding | DeathFlags.Item,
            PlayerKitName = deadKit?.Id,
            Session = dead.CurrentSession,
            Limb = ELimb.SKULL
        };

        if (killer != null)
        {
            e.Killer = killer;
            e.KillerPoint = killer.Position;
            e.KillerTeam = killer.Team;
            e.KillerSession = killer.CurrentSession;
            e.KillerWasOnDuty = killer.IsOnDuty;
            e.KillDistance = (killer.Position - dead.Position).sqrMagnitude;
            if (killerKit != null)
            {
                e.KillerBranch = killerKit.Branch;
                e.KillerClass = killerKit.Class;
                e.KillerKitName = killerKit.Id;
            }

            e.MessageFlags |= DeathFlags.Killer;
        }

        comp.BleedOutInfo = e;
    }

    private static void OnPlayerAboutToBeDamaged(ref DamagePlayerParameters parameters, ref bool shouldAllow)
    {
        using IDisposable? profiler = ProfilerUtil.Profile();

        // only called from DamageTool.damagePlayer, not PlayerLife.doDamage
        if (parameters.player == null
            || parameters.player.life.isDead
            || !shouldAllow
            || parameters.bleedingModifier != DamagePlayerParameters.Bleeding.Always)
        {
            return;
        }

        DeathTracker deathTracker = WarfareModule.Singleton.ServiceProvider.Resolve<DeathTracker>();
        deathTracker.OnWillStartBleeding(ref parameters);
    }

    private static IEnumerable<CodeInstruction> TranspilerDoDamage(IEnumerable<CodeInstruction> instructions, MethodBase method, ILGenerator generator)
    {
        TranspileContext ctx = new TranspileContext(method, generator, instructions);

        MethodInfo? serverSetBleeding = typeof(PlayerLife).GetMethod(
            nameof(PlayerLife.serverSetBleeding),
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [typeof(bool)],
            null
        );

        if (serverSetBleeding == null)
        {
            return ctx.Fail(new MethodDefinition(nameof(PlayerLife.serverSetBleeding))
                .DeclaredIn<PlayerLife>(isStatic: false)
                .WithParameter<bool>("newBleeding")
                .ReturningVoid()
            );
        }

        ParameterInfo[] parameters = method.GetParameters();
        int amountIndex = Array.FindIndex(parameters, p => p.ParameterType == typeof(byte));
        int ragdollIndex = Array.FindIndex(parameters, p => p.ParameterType == typeof(Vector3));
        int causeIndex = Array.FindIndex(parameters, p => p.ParameterType == typeof(EDeathCause));
        int limbIndex = Array.FindIndex(parameters, p => p.ParameterType == typeof(ELimb));
        int killerIndex = Array.FindIndex(parameters, p => p.ParameterType == typeof(CSteamID));
        int ragdollEffectIndex = Array.FindIndex(parameters, p => p.ParameterType == typeof(ERagdollEffect));
        int trackKillIndex = Array.FindIndex(parameters, p => p.ParameterType == typeof(bool) && p.Name.Contains("track", StringComparison.OrdinalIgnoreCase));
        if (amountIndex < 0 || ragdollIndex < 0 || causeIndex < 0 || limbIndex < 0 || killerIndex < 0 || ragdollEffectIndex < 0 || trackKillIndex < 0)
        {
            return ctx.Fail($"Unable to find parameters for (amount: {amountIndex}, ragdoll: {ragdollIndex}, cause: {causeIndex}, limb: {limbIndex}, killer: {killerIndex}, ragdollEffect: {ragdollEffectIndex}, trackKill: {trackKillIndex}).");
        }

        bool any = false;
        while (ctx.MoveNext())
        {
            if (!ctx.Instruction.Calls(serverSetBleeding))
                continue;

            ctx.EmitAbove(emit =>
            {
                emit.Duplicate()                          // is bleeding?
                    .LoadArgument(0)                      // this
                    .LoadArgument(amountIndex + 1)        // amount of damage
                    .LoadArgument(ragdollIndex + 1)       // ragdoll
                    .LoadArgument(causeIndex + 1)         // death cause
                    .LoadArgument(limbIndex + 1)          // limb
                    .LoadArgument(killerIndex + 1)        // instigator
                    .LoadArgument(ragdollEffectIndex + 1) // ragdoll effect
                    .LoadArgument(trackKillIndex + 1)     // track kill?
                    .Invoke(Accessor.GetMethod(OnAboutToBleed)!);
            });
            any = true;
        }

        if (!any)
        {
            ctx.LogError($"Failed to find call to {Accessor.Formatter.Format(serverSetBleeding)}.");
        }

        return ctx;
    }

    private static void OnAboutToBleed(
        bool isBleeding,
        PlayerLife life,
        byte amount,
        Vector3 ragdoll,
        EDeathCause cause,
        ELimb limb,
        CSteamID killer,
        ERagdollEffect ragdollEffect,
        bool trackKill)
    {
        if (!isBleeding)
            return;

        using IDisposable? profiler = ProfilerUtil.Profile();

        DamagePlayerParameters parameters = new DamagePlayerParameters(life.player)
        {
            killer = killer,
            limb = limb,
            cause = cause,
            direction = ragdoll,
            damage = amount,
            times = 1f,
            ragdollEffect = ragdollEffect,
            trackKill = trackKill,
            respectArmor = false
        };

        DeathTracker deathTracker = WarfareModule.Singleton.ServiceProvider.Resolve<DeathTracker>();
        deathTracker.OnWillStartBleeding(ref parameters);
    }
}
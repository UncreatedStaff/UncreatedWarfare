#if DEBUG

//#define PROJECTILE_DEBUG

#endif


using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Emit;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using System;
using System.Reflection;
using System.Reflection.Emit;
using Uncreated.Warfare.Projectiles;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Patches;

[UsedImplicitly]
internal sealed class ProjectilePreExplodePatch : IHarmonyPatch
{
    private static MethodInfo? _target;
    void IHarmonyPatch.Patch(ILogger logger, Harmony patcher)
    {
        _target = typeof(Rocket).GetMethod("OnTriggerEnter", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (_target != null)
        {
            patcher.Patch(_target, transpiler: Accessor.GetMethod(Transpiler));
            logger.LogDebug($"Patched {_target} for receiving projectile explode event.");
            return;
        }

        logger.LogError("Failed to find method: {0}.",
            new MethodDefinition("OnTriggerEnter")
                .DeclaredIn<Rocket>(isStatic: false)
                .WithParameter<Collider>("other")
                .ReturningVoid()
        );
    }

    void IHarmonyPatch.Unpatch(ILogger logger, Harmony patcher)
    {
        if (_target == null)
            return;

        patcher.Unpatch(_target, Accessor.GetMethod(Transpiler));
        logger.LogDebug($"Unpatched {_target} for receiving projectile explode event.");
        _target = null;
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase method)
    {
        TranspileContext ctx = new TranspileContext(method, generator, instructions);

        MethodInfo? explode = typeof(DamageTool).GetMethod(nameof(DamageTool.explode),
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static, null,
            [ typeof(ExplosionParameters), typeof(List<EPlayerKill>).MakeByRefType() ], null);

        if (explode == null)
        {
            return ctx.Fail(new MethodDefinition(nameof(DamageTool.explode))
                .DeclaredIn(typeof(DamageTool), isStatic: true)
                .WithParameter<ExplosionParameters>("parameters")
                .WithParameter<List<EPlayerKill>>("kills", ByRefTypeMode.Ref)
                .ReturningVoid()
            );
        }

        bool patched = false;
        while (ctx.MoveNext())
        {
            // ldloc ExplosionParameters
            // ldloca List<EPlayerKill>&    <-- caret
            // call DamageTool.explode
            if (ctx.CaretIndex >= ctx.Count - 1 || !ctx[ctx.CaretIndex + 1].Calls(explode) || !ctx[ctx.CaretIndex - 1].opcode.IsLdLoc(either: true))
                continue;

            LocalReference lclRef = ctx[ctx.CaretIndex - 1].ToLocalReference();

            patched = true;
            Label lbl = default;

            ctx.MoveBack();

            ctx.EmitAbove(emit =>
            {
                emit.LoadLocalAddress(lclRef)
                    .LoadArgument(0)
                    .LoadArgument(1)
                    .Invoke(Accessor.GetMethod(InvokeExploding)!)
                    .AddLabel(out lbl)
                    .BranchIfTrue(lbl)
                    .Return();
            });

            ctx.MarkLabel(lbl);
            ctx.ApplyBlocksAndLabels();

            while (!ctx.Instruction.Calls(explode) && ctx.MoveNext()) ;

            ctx.EmitBelow(emit =>
            {
                emit.LoadArgument(0)
                    .LoadArgument(1)
                    .Invoke(new Action<Rocket, Collider>(OnExploded).Method);
            });
            break;
        }

        if (!patched)
        {
            return ctx.Fail("Failed to patch, didnt find explode method.");
        }

        return ctx;
    }

    private static void OnExploded(Rocket rocket, Collider other)
    {
        if (rocket.TryGetComponent(out WarfareProjectile projectile) && WarfareProjectile.ExplodingProjectile == projectile)
        {
            WarfareProjectile.ExplodingProjectile = null;
        }
    }

    private static readonly RaycastHit[] Hits = new RaycastHit[2];

    private const int BlockTrap = RayMasks.PLAYER | RayMasks.DEBRIS | RayMasks.ITEM | RayMasks.RESOURCE |
                                  RayMasks.LARGE | RayMasks.MEDIUM | RayMasks.ENVIRONMENT | RayMasks.GROUND |
                                  RayMasks.AGENT | RayMasks.VEHICLE | RayMasks.BARRICADE | RayMasks.STRUCTURE |
                                  RayMasks.TRAP;

    private static bool InvokeExploding(ref ExplosionParameters parameters, Rocket rocket, Collider other)
    {
        // handles rockets blowing up from their position during the previous frame
        // also handles correcting for the distance between the rocket's origin and it's collider bounds (where it'll actually hit)
        const float maxRocketSpeedMetersPerSec = 500;
        float len = Time.deltaTime * maxRocketSpeedMetersPerSec;

        Transform rocketTransform = rocket.transform;

        Ray ray = new Ray(rocket.secondLastPos, rocketTransform.up);

        int hits = Physics.RaycastNonAlloc(ray.origin, ray.direction, Hits, len, BlockTrap, QueryTriggerInteraction.Ignore);

#if PROJECTILE_DEBUG
        SteamPlayer owner = PlayerTool.getSteamPlayer(rocket.killer);
        bool clear = true;
#endif

        bool found = false;
        for (int i = 0; i < hits; ++i)
        {
            ref RaycastHit hit = ref Hits[i];
            if (hit.transform.IsChildOf(rocketTransform))
                continue;

            found = true;
            WarfareModule.Singleton.GlobalLogger.LogTrace($"Point updated | old point: {parameters.point:F1}");
            parameters.point = hit.point;
#if PROJECTILE_DEBUG
            if (owner != null)
            {
                EffectUtility.TriggerDebugEffect(owner.transportConnection, hit.point, ray.direction, rocketTransform.forward, clear);
                clear = false;
            }
#endif
            break;
        }

        if (!found)
        {
            WarfareModule.Singleton.GlobalLogger.LogWarning($"Failed to find hit position for rocket shot by {rocket.killer}.");
        }

#if PROJECTILE_DEBUG
        if (owner != null)
        {
            EffectUtility.TriggerDebugEffect(owner.transportConnection, parameters.point, ray.direction, rocketTransform.forward, clear);
        }
#endif

        Array.Clear(Hits, 0, hits);

        if (rocket.gameObject.TryGetComponent(out WarfareProjectile projectile) && !projectile.HasExploded)
        {
            return projectile.InvokeExploding(other, ref parameters);
        }

        return false;
    }
}
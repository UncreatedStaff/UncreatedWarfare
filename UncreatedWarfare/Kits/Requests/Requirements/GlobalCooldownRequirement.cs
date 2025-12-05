using Uncreated.Warfare.Players.Cooldowns;

namespace Uncreated.Warfare.Kits.Requests.Requirements;

/// <summary>
/// Checks if a player is on a global request cooldown.
/// </summary>
public sealed class GlobalCooldownRequirement(CooldownManager? cooldownManager = null) : IKitRequirement
{
    public KitRequirementResult AcceptCached<TState>(IKitRequirementVisitor<TState> visitor, in KitRequirementResolutionContext<TState> ctx)
    {
        if (ctx.Player.IsOnDuty || ctx.Kit.BypassGlobalCooldown || cooldownManager == null || !cooldownManager.HasCooldown(ctx.Player, KnownCooldowns.RequestKit, out Cooldown requestCooldown))
            return KitRequirementResult.Yes;

        visitor.AcceptGlobalCooldownNotMet(in ctx, in requestCooldown);
        return KitRequirementResult.No;
    }

    public ValueTask<KitRequirementResult> AcceptAsync<TState>(IKitRequirementVisitor<TState> visitor, in KitRequirementResolutionContext<TState> ctx, CancellationToken token = default)
    {
        return new ValueTask<KitRequirementResult>(AcceptCached(visitor, in ctx));
    }
}
using Uncreated.Warfare.Players.Cooldowns;

namespace Uncreated.Warfare.Kits.Requests.Requirements;

/// <summary>
/// Checks if a player is on a premium kit cooldown.
/// </summary>
public sealed class PremiumCooldownRequirement(CooldownManager? cooldownManager = null) : IKitRequirement
{
    public KitRequirementResult AcceptCached<TState>(IKitRequirementVisitor<TState> visitor, in KitRequirementResolutionContext<TState> ctx)
    {
        if (ctx.Player.IsOnDuty || !ctx.Kit.IsPaid || ctx.Kit.RequestCooldown.Ticks <= 0
            || cooldownManager == null
            || !cooldownManager.HasCooldown(ctx.Player, KnownCooldowns.RequestPremiumKit, out Cooldown requestCooldown, ctx.Kit.Id))
        {
            return KitRequirementResult.Yes;
        }

        visitor.AcceptPremiumCooldownNotMet(in ctx, in requestCooldown);
        return KitRequirementResult.No;
    }

    public ValueTask<KitRequirementResult> AcceptAsync<TState>(IKitRequirementVisitor<TState> visitor, in KitRequirementResolutionContext<TState> ctx, CancellationToken token = default)
    {
        return new ValueTask<KitRequirementResult>(AcceptCached(visitor, in ctx));
    }
}
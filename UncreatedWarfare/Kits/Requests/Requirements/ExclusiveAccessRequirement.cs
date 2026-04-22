namespace Uncreated.Warfare.Kits.Requests.Requirements;

/// <summary>
/// Checks that a player owns a special kit.
/// </summary>
public sealed class ExclusiveAccessRequirement(IKitAccessService kitAccessService) : IKitRequirement
{
    private static bool ShouldIgnoreKit(Kit kit)
    {
        return kit.Type == KitType.Public || kit.PremiumCost > 0 || kit.RequiresServerBoost;
    }

    public KitRequirementResult AcceptCached<TState>(IKitRequirementVisitor<TState> visitor, in KitRequirementResolutionContext<TState> ctx)
    {
        if (ShouldIgnoreKit(ctx.Kit) || ctx.Component.IsKitAccessible(ctx.Kit.Key))
            return KitRequirementResult.Yes;

        visitor.AcceptExclusiveKitNotMet(in ctx);
        return KitRequirementResult.No;
    }

    public ValueTask<KitRequirementResult> AcceptAsync<TState>(IKitRequirementVisitor<TState> visitor, in KitRequirementResolutionContext<TState> ctx, CancellationToken token = default)
    {
        return new ValueTask<KitRequirementResult>(Core(visitor, ctx, token));

        async Task<KitRequirementResult> Core(IKitRequirementVisitor<TState> visitor, KitRequirementResolutionContext<TState> ctx, CancellationToken token)
        {
            if (ShouldIgnoreKit(ctx.Kit) || await kitAccessService.HasAccessAsync(ctx.Player.Steam64, ctx.Kit.Key, token).ConfigureAwait(false))
                return KitRequirementResult.Yes;

            visitor.AcceptExclusiveKitNotMet(in ctx);
            return KitRequirementResult.No;
        }
    }
}
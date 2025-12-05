namespace Uncreated.Warfare.Kits.Requests.Requirements;

/// <summary>
/// Checks that a player owns a paid elite kit or loadout.
/// </summary>
public sealed class PremiumCostRequirement(IKitAccessService kitAccessService) : IKitRequirement
{
    public KitRequirementResult AcceptCached<TState>(IKitRequirementVisitor<TState> visitor, in KitRequirementResolutionContext<TState> ctx)
    {
        if (ctx.Kit.PremiumCost <= 0 || ctx.Component.IsKitAccessible(ctx.Kit.Key))
            return KitRequirementResult.Yes;

        visitor.AcceptPremiumCostNotMet(in ctx, ctx.Kit.PremiumCost);
        return KitRequirementResult.No;
    }

    public ValueTask<KitRequirementResult> AcceptAsync<TState>(IKitRequirementVisitor<TState> visitor, in KitRequirementResolutionContext<TState> ctx, CancellationToken token = default)
    {
        return new ValueTask<KitRequirementResult>(Core(visitor, ctx, token));

        async Task<KitRequirementResult> Core(IKitRequirementVisitor<TState> visitor, KitRequirementResolutionContext<TState> ctx, CancellationToken token)
        {
            if (ctx.Kit.PremiumCost <= 0 || await kitAccessService.HasAccessAsync(ctx.Player.Steam64, ctx.Kit.Key, token).ConfigureAwait(false))
                return KitRequirementResult.Yes;

            visitor.AcceptPremiumCostNotMet(in ctx, ctx.Kit.PremiumCost);
            return KitRequirementResult.No;
        }
    }
}
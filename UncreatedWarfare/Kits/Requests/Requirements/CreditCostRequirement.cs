namespace Uncreated.Warfare.Kits.Requests.Requirements;

/// <summary>
/// Checks that a player owns a public kit that costs credits.
/// </summary>
public sealed class CreditCostRequirement(IKitAccessService kitAccessService) : IKitRequirement
{
    public KitRequirementResult AcceptCached<TState>(IKitRequirementVisitor<TState> visitor, in KitRequirementResolutionContext<TState> ctx)
    {
        if (ctx.Kit.CreditCost <= 0 || ctx.Component.IsKitAccessible(ctx.Kit.Key))
            return KitRequirementResult.Yes;

        visitor.AcceptCreditCostNotMet(in ctx, ctx.Kit.CreditCost, ctx.Player.CachedPoints.Credits);
        return KitRequirementResult.No;
    }

    public ValueTask<KitRequirementResult> AcceptAsync<TState>(IKitRequirementVisitor<TState> visitor, in KitRequirementResolutionContext<TState> ctx, CancellationToken token = default)
    {
        return new ValueTask<KitRequirementResult>(Core(visitor, ctx, token));

        async Task<KitRequirementResult> Core(IKitRequirementVisitor<TState> visitor, KitRequirementResolutionContext<TState> ctx, CancellationToken token)
        {
            if (ctx.Kit.CreditCost <= 0 || await kitAccessService.HasAccessAsync(ctx.Player.Steam64, ctx.Kit.Key, token).ConfigureAwait(false))
                return KitRequirementResult.Yes;

            visitor.AcceptCreditCostNotMet(in ctx, ctx.Kit.CreditCost, ctx.Player.CachedPoints.Credits);
            return KitRequirementResult.No;
        }
    }
}
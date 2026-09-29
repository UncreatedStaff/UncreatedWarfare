using System;

namespace Uncreated.Warfare.Kits.Requests.Requirements;

/// <summary>
/// Checks that a player owns a public kit that costs credits.
/// </summary>
public sealed class CreditCostRequirement(IKitAccessService kitAccessService, PublicKitLevelConfiguration config) : IKitRequirement
{
    private bool NeedsAccess(Kit kit, out PublicKitLevel levelInfo, out double cost)
    {
        if (kit.TryGetLevel(out levelInfo) && config.CreditCostBylevel.TryGetValue(levelInfo, out double v) && v >= 0)
        {
            cost = v;
            return true;
        }

        cost = 0;
        return false;
    }

    public KitRequirementResult AcceptCached<TState>(IKitRequirementVisitor<TState> visitor, in KitRequirementResolutionContext<TState> ctx)
    {
        if (ctx.Kit.Type != KitType.Public
            || kitAccessService.ArePrimaryKitsGloballyAccessible
            || !NeedsAccess(ctx.Kit, out PublicKitLevel lvlInfo, out double cost)
            || ctx.Component.IsKitAccessible(lvlInfo))
        {
            return KitRequirementResult.Yes;
        }

        visitor.AcceptCreditCostNotMet(in ctx, (int)Math.Round(cost), ctx.Player.CachedPoints.Credits);
        return KitRequirementResult.No;
    }

    public ValueTask<KitRequirementResult> AcceptAsync<TState>(IKitRequirementVisitor<TState> visitor, in KitRequirementResolutionContext<TState> ctx, CancellationToken token = default)
    {
        return new ValueTask<KitRequirementResult>(Core(visitor, ctx, token));

        async Task<KitRequirementResult> Core(IKitRequirementVisitor<TState> visitor, KitRequirementResolutionContext<TState> ctx, CancellationToken token)
        {
            if (ctx.Kit.Type != KitType.Public
                || kitAccessService.ArePrimaryKitsGloballyAccessible
                || !NeedsAccess(ctx.Kit, out PublicKitLevel lvlInfo, out double cost)
                || await kitAccessService.HasAccessAsync(ctx.Player.Steam64, lvlInfo, token).ConfigureAwait(false))
            {
                return KitRequirementResult.Yes;
            }

            visitor.AcceptCreditCostNotMet(in ctx, (int)Math.Round(cost), ctx.Player.CachedPoints.Credits);
            return KitRequirementResult.No;
        }
    }
}
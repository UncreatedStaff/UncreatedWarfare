using Uncreated.Warfare.Players;

namespace Uncreated.Warfare.Kits.Requests.Requirements;

/// <summary>
/// Checks that a player is nitro boosting the discord server.
/// </summary>
public sealed class NitroBoostRequirement(IKitAccessService kitAccessService, PlayerNitroBoostService? nitroBoostService = null) : IKitRequirement
{
    public KitRequirementResult AcceptCached<TState>(IKitRequirementVisitor<TState> visitor, in KitRequirementResolutionContext<TState> ctx)
    {
        if (!ctx.Kit.RequiresServerBoost || ctx.Component.IsKitAccessible(ctx.Kit.Key))
            return KitRequirementResult.Yes;

        bool? isBoosting = nitroBoostService?.IsBoostingQuick(ctx.Player.Steam64);
        switch (isBoosting)
        {
            case true:
                return KitRequirementResult.Yes;

            case null:
                return KitRequirementResult.Inconclusive;

            default:
                visitor.AcceptNitroBoostRequirementNotMet(in ctx);
                return KitRequirementResult.No;
        }
    }

    public ValueTask<KitRequirementResult> AcceptAsync<TState>(IKitRequirementVisitor<TState> visitor, in KitRequirementResolutionContext<TState> ctx, CancellationToken token = default)
    {
        return new ValueTask<KitRequirementResult>(Core(visitor, ctx, token));

        async Task<KitRequirementResult> Core(IKitRequirementVisitor<TState> visitor, KitRequirementResolutionContext<TState> ctx, CancellationToken token)
        {
            if (!ctx.Kit.RequiresServerBoost || await kitAccessService.HasAccessAsync(ctx.Player.Steam64, ctx.Kit.Key, token).ConfigureAwait(false))
                return KitRequirementResult.Yes;

            if (nitroBoostService == null)
                return KitRequirementResult.No;

            bool isBoosting = await nitroBoostService.IsBoosting(ctx.Player.Steam64, forceRecheck: true, token).ConfigureAwait(false);
            if (isBoosting)
                return KitRequirementResult.Yes;

            visitor.AcceptNitroBoostRequirementNotMet(in ctx);
            return KitRequirementResult.No;
        }
    }
}
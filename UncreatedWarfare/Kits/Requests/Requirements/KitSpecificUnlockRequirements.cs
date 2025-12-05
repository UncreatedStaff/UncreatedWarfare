using Uncreated.Warfare.Players.Unlocks;

namespace Uncreated.Warfare.Kits.Requests.Requirements;

/// <summary>
/// Invokes kit-specific <see cref="UnlockRequirement"/>'s.
/// </summary>
public sealed class KitSpecificUnlockRequirements : IKitRequirement
{
    public KitRequirementResult AcceptCached<TState>(IKitRequirementVisitor<TState> visitor, in KitRequirementResolutionContext<TState> ctx)
    {
        if (ctx.Kit.UnlockRequirements == null)
            return KitRequirementResult.Yes;

        foreach (UnlockRequirement req in ctx.Kit.UnlockRequirements)
        {
            if (req == null || req.CanAccessFast(ctx.Player))
                continue;

            visitor.AcceptKitSpecificUnlockRequirementNotMet(in ctx, req);
            return KitRequirementResult.No;
        }

        return KitRequirementResult.Yes;
    }

    public ValueTask<KitRequirementResult> AcceptAsync<TState>(IKitRequirementVisitor<TState> visitor, in KitRequirementResolutionContext<TState> ctx, CancellationToken token = default)
    {
        return new ValueTask<KitRequirementResult>(Core(visitor, ctx, token));

        static async Task<KitRequirementResult> Core(IKitRequirementVisitor<TState> visitor, KitRequirementResolutionContext<TState> ctx, CancellationToken token)
        {
            if (ctx.Kit.UnlockRequirements == null)
                return KitRequirementResult.Yes;

            foreach (UnlockRequirement req in ctx.Kit.UnlockRequirements)
            {
                if (req == null)
                    continue;

                await UniTask.SwitchToMainThread(token);
                if (await req.CanAccessAsync(ctx.Player, token))
                    continue;

                visitor.AcceptKitSpecificUnlockRequirementNotMet(in ctx, req);
                return KitRequirementResult.No;
            }

            return KitRequirementResult.Yes;
        }
    }
}
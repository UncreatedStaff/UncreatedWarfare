namespace Uncreated.Warfare.Kits.Requests.Requirements;

/// <summary>
/// Checks that a loadout isn't locked.
/// </summary>
public sealed class LoadoutLockedRequirement : IKitRequirement
{
    public KitRequirementResult AcceptCached<TState>(IKitRequirementVisitor<TState> visitor, in KitRequirementResolutionContext<TState> ctx)
    {
        if (ctx.Kit.Type != KitType.Loadout || !ctx.Kit.IsLocked)
            return KitRequirementResult.Yes;

        visitor.AcceptLoadoutLockedNotMet(in ctx);
        return KitRequirementResult.No;
    }

    public ValueTask<KitRequirementResult> AcceptAsync<TState>(IKitRequirementVisitor<TState> visitor, in KitRequirementResolutionContext<TState> ctx, CancellationToken token = default)
    {
        return new ValueTask<KitRequirementResult>(AcceptCached(visitor, in ctx));
    }
}
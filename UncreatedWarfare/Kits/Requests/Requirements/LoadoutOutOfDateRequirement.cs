namespace Uncreated.Warfare.Kits.Requests.Requirements;

/// <summary>
/// Checks that a loadout isn't out of date.
/// </summary>
public sealed class LoadoutOutOfDateRequirement : IKitRequirement
{
    public KitRequirementResult AcceptCached<TState>(IKitRequirementVisitor<TState> visitor, in KitRequirementResolutionContext<TState> ctx)
    {
        if (ctx.Kit.Type != KitType.Loadout || ctx.Kit.Season >= WarfareModule.Season)
            return KitRequirementResult.Yes;

        visitor.AcceptLoadoutOutOfDateNotMet(in ctx, ctx.Kit.Season);
        return KitRequirementResult.No;
    }

    public ValueTask<KitRequirementResult> AcceptAsync<TState>(IKitRequirementVisitor<TState> visitor, in KitRequirementResolutionContext<TState> ctx, CancellationToken token = default)
    {
        return new ValueTask<KitRequirementResult>(AcceptCached(visitor, in ctx));
    }
}
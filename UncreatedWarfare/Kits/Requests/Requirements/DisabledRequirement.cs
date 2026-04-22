namespace Uncreated.Warfare.Kits.Requests.Requirements;

/// <summary>
/// Checks that a kit isn't disabled.
/// </summary>
public sealed class DisabledRequirement : IKitRequirement
{
    public KitRequirementResult AcceptCached<TState>(IKitRequirementVisitor<TState> visitor, in KitRequirementResolutionContext<TState> ctx)
    {
        if (ctx.Kit.Type == KitType.Loadout || !ctx.Kit.IsLocked)
            return KitRequirementResult.Yes;

        visitor.AcceptDisabledNotMet(in ctx);
        return KitRequirementResult.No;
    }

    public ValueTask<KitRequirementResult> AcceptAsync<TState>(IKitRequirementVisitor<TState> visitor, in KitRequirementResolutionContext<TState> ctx, CancellationToken token = default)
    {
        return new ValueTask<KitRequirementResult>(AcceptCached(visitor, in ctx));
    }
}
using Uncreated.Warfare.Maps;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Kits.Requests.Requirements;

/// <summary>
/// Checks that a kit's map filter is met.
/// </summary>
public sealed class MapFilterRequirement(MapScheduler mapScheduler) : IKitRequirement
{
    private bool IsCurrentMapAllowed(Kit kit)
    {
        if (kit.MapFilter.IsNullOrEmpty())
            return true;

        int map = mapScheduler.Current;
        if (map != -1)
        {
            for (int i = 0; i < kit.MapFilter.Length; ++i)
            {
                if (kit.MapFilter[i] == map)
                    return kit.MapFilterIsWhitelist;
            }
        }

        return !kit.MapFilterIsWhitelist;
    }

    public KitRequirementResult AcceptCached<TState>(IKitRequirementVisitor<TState> visitor, in KitRequirementResolutionContext<TState> ctx)
    {
        if (IsCurrentMapAllowed(ctx.Kit))
            return KitRequirementResult.Yes;

        visitor.AcceptMapFilterNotMet(in ctx);
        return KitRequirementResult.No;
    }

    public ValueTask<KitRequirementResult> AcceptAsync<TState>(IKitRequirementVisitor<TState> visitor, in KitRequirementResolutionContext<TState> ctx, CancellationToken token = default)
    {
        return new ValueTask<KitRequirementResult>(AcceptCached(visitor, in ctx));
    }
}
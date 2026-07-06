using Uncreated.Warfare.Maps;
using Uncreated.Warfare.Models.Seasons;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Kits.Requests.Requirements;

/// <summary>
/// Checks that a kit's map filter is met.
/// </summary>
public sealed class MapFilterRequirement(MapScheduler mapScheduler) : IKitRequirement
{
    private bool IsCurrentMapAllowed(Kit kit)
    {
        if (kit.MapFilter.IsNullOrEmpty() || !mapScheduler.HasSelectedMap)
            return true;

        MapData map = mapScheduler.Current;
        for (int i = 0; i < kit.MapFilter.Length; ++i)
        {
            if (kit.MapFilter[i] == map.Id)
                return kit.MapFilterIsWhitelist;
        }

        return !kit.MapFilterIsWhitelist;
    }

    public KitRequirementResult AcceptCached<TState>(IKitRequirementVisitor<TState> visitor, in KitRequirementResolutionContext<TState> ctx)
    {
        if (IsCurrentMapAllowed(ctx.Kit) || !mapScheduler.HasSelectedMap)
            return KitRequirementResult.Yes;

        visitor.AcceptMapFilterNotMet(in ctx, mapScheduler.Current.DisplayName);
        return KitRequirementResult.No;
    }

    public ValueTask<KitRequirementResult> AcceptAsync<TState>(IKitRequirementVisitor<TState> visitor, in KitRequirementResolutionContext<TState> ctx, CancellationToken token = default)
    {
        return new ValueTask<KitRequirementResult>(AcceptCached(visitor, in ctx));
    }
}
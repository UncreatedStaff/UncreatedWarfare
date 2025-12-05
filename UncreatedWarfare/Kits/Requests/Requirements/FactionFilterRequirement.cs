using System.Linq;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Kits.Requests.Requirements;

/// <summary>
/// Checks that a kit's faction filter is met.
/// </summary>
public sealed class FactionFilterRequirement : IKitRequirement
{
    private static bool IsCurrentFactionAllowed(Kit kit, Team team)
    {
        if (!kit.Faction.IsDefaultFaction)
        {
            if (team.Opponents.Any(x => x.Faction.Equals(kit.Faction)))
                return false;
        }

        if (kit.FactionFilter.IsNullOrEmpty())
            return true;

        for (int i = 0; i < kit.FactionFilter.Length; ++i)
        {
            if (kit.FactionFilter[i].Equals(team.Faction))
                return kit.FactionFilterIsWhitelist;
        }

        return !kit.FactionFilterIsWhitelist;
    }

    public KitRequirementResult AcceptCached<TState>(IKitRequirementVisitor<TState> visitor, in KitRequirementResolutionContext<TState> ctx)
    {
        if (IsCurrentFactionAllowed(ctx.Kit, ctx.Team))
            return KitRequirementResult.Yes;

        visitor.AcceptFactionFilterNotMet(in ctx, ctx.Team.Faction);
        return KitRequirementResult.No;
    }

    public ValueTask<KitRequirementResult> AcceptAsync<TState>(IKitRequirementVisitor<TState> visitor, in KitRequirementResolutionContext<TState> ctx, CancellationToken token = default)
    {
        return new ValueTask<KitRequirementResult>(AcceptCached(visitor, in ctx));
    }
}
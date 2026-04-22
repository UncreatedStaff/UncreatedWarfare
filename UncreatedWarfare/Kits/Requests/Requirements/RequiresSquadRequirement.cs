using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Squads;

namespace Uncreated.Warfare.Kits.Requests.Requirements;

/// <summary>
/// Checks if a kit requires a squad or squadleader.
/// </summary>
public sealed class RequiresSquadRequirement(SquadManager? squadManager = null) : IKitRequirement
{
    public KitRequirementResult AcceptCached<TState>(IKitRequirementVisitor<TState> visitor, in KitRequirementResolutionContext<TState> ctx)
    {
        if (!ctx.Kit.RequiresSquad || squadManager == null)
            return KitRequirementResult.Yes;

        bool needsSquadLead = ctx.Kit.Class == Class.Squadleader;
        
        Squad? squad = ctx.Player.GetSquad();
        if (squad == null || needsSquadLead && !squad.IsLeader(ctx.Player))
        {
            visitor.AcceptRequiresSquadNotMet(in ctx, needsSquadLead);
            return KitRequirementResult.No;
        }

        return KitRequirementResult.Yes;
    }

    public ValueTask<KitRequirementResult> AcceptAsync<TState>(IKitRequirementVisitor<TState> visitor, in KitRequirementResolutionContext<TState> ctx, CancellationToken token = default)
    {
        return new ValueTask<KitRequirementResult>(Core(visitor, ctx, token));

        async Task<KitRequirementResult> Core(IKitRequirementVisitor<TState> visitor, KitRequirementResolutionContext<TState> ctx, CancellationToken token)
        {
            await UniTask.SwitchToMainThread(token);
            return AcceptCached(visitor, in ctx);
        }
    }
}
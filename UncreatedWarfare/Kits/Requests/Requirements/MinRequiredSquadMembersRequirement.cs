using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Squads;

namespace Uncreated.Warfare.Kits.Requests.Requirements;

/// <summary>
/// Checks if there are too many squad members using a kit or not enough squad members to use a kit.
/// </summary>
public sealed class MinRequiredSquadMembersRequirement(SquadManager? squadManager = null) : IKitRequirement
{
    public KitRequirementResult AcceptCached<TState>(IKitRequirementVisitor<TState> visitor, in KitRequirementResolutionContext<TState> ctx)
    {
        if (!ctx.Kit.RequiresSquad || !ctx.Kit.MinRequiredSquadMembers.HasValue || squadManager == null)
            return KitRequirementResult.Yes;

        Squad? squad = ctx.Player.GetSquad();
        if (squad == null)
            return KitRequirementResult.Yes;

        int min = ctx.Kit.MinRequiredSquadMembers.Value;

        foreach (WarfarePlayer player in squad.Members)
        {
            if (player.Component<KitPlayerComponent>().ActiveKitKey is { } pk && pk == ctx.Kit.Key)
            {
                visitor.AcceptMinRequiredSquadMembersNotMet(in ctx, player, squad.Members.Count, min);
                return KitRequirementResult.No;
            }
        }

        if (squad.Members.Count >= ctx.Kit.MinRequiredSquadMembers.Value)
            return KitRequirementResult.Yes;

        visitor.AcceptMinRequiredSquadMembersNotMet(in ctx, null, squad.Members.Count, min);
        return KitRequirementResult.No;
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
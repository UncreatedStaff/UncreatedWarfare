using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Squads;

namespace Uncreated.Warfare.Kits.Requests.Requirements;

/// <summary>
/// Checks if there are enough players for another player of this kit's class.
/// </summary>
public sealed class ClassesAllowedPerXTeammatesRequirement(IPlayerService playerService, SquadConfiguration? squadConfiguration = null) : IKitRequirement
{
    public KitRequirementResult AcceptCached<TState>(IKitRequirementVisitor<TState> visitor, in KitRequirementResolutionContext<TState> ctx)
    {
        if (!ctx.Kit.RequiresSquad || squadConfiguration == null)
            return KitRequirementResult.Yes;

        int allowedPerXUsers = squadConfiguration.KitClassesAllowedPerXTeammates.GetValueOrDefault(ctx.Kit.Class);
        if (allowedPerXUsers <= 0)
            return KitRequirementResult.Yes;

        int currentUsers = 0;
        int teammates = 0;
        foreach (WarfarePlayer player in playerService.OnlinePlayersOnTeam(ctx.Team))
        {
            if (player.Equals(ctx.Player))
                continue;

            teammates++;
            if (player.Component<KitPlayerComponent>().ActiveClass == ctx.Kit.Class)
                currentUsers++;
        }

        int kitsAllowed = teammates / allowedPerXUsers + 1;
        if (currentUsers + 1 <= kitsAllowed)
            return KitRequirementResult.Yes;

        visitor.AcceptClassesAllowedPerXTeammatesRequirementNotMet(in ctx, allowedPerXUsers, currentUsers, teammates, kitsAllowed);
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
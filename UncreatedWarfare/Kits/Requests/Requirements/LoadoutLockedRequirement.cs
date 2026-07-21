using Microsoft.EntityFrameworkCore;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Kits.Requests.Requirements;

/// <summary>
/// Checks that a loadout isn't locked.
/// </summary>
public sealed class LoadoutLockedRequirement : IKitRequirement
{
    private readonly IKitsDbContext _dbContext;

    public LoadoutLockedRequirement(IKitsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public KitRequirementResult AcceptCached<TState>(IKitRequirementVisitor<TState> visitor, in KitRequirementResolutionContext<TState> ctx)
    {
        if (ctx.Kit.Type != KitType.Loadout || !ctx.Kit.IsLocked)
            return KitRequirementResult.Yes;

        visitor.AcceptLoadoutLockedNotMet(in ctx, false);
        return KitRequirementResult.No;
    }

    public ValueTask<KitRequirementResult> AcceptAsync<TState>(IKitRequirementVisitor<TState> visitor, in KitRequirementResolutionContext<TState> ctx, CancellationToken token = default)
    {
        if (ctx.Kit.Type != KitType.Loadout || !ctx.Kit.IsLocked)
            return new ValueTask<KitRequirementResult>(KitRequirementResult.Yes);

        return new ValueTask<KitRequirementResult>(Core(visitor, ctx, token));

        async Task<KitRequirementResult> Core(IKitRequirementVisitor<TState> visitor, KitRequirementResolutionContext<TState> ctx, CancellationToken token)
        {
            Kit kit = ctx.Kit;
            bool isUnpaid = await _dbContext.Loadouts.AnyAsync(x => x.KitId == kit.Key && !x.Paid, token);

            visitor.AcceptLoadoutLockedNotMet(in ctx, isUnpaid);
            return KitRequirementResult.No;
        }
    }
}
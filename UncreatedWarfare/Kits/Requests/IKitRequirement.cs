using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Immutable;
using Uncreated.Warfare.Kits.Requests.Requirements;
using Uncreated.Warfare.Layouts.Teams;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Cooldowns;
using Uncreated.Warfare.Players.Unlocks;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Kits.Requests;

/// <summary>
/// A requirement that needs to be met to buy or request a kit.
/// </summary>
public interface IKitRequirement
{
    /// <summary>
    /// A quick check to see if a requirement is met. Used for UI, doesn't have to be 100% accurate.
    /// </summary>
    KitRequirementResult AcceptCached<TState>(IKitRequirementVisitor<TState> visitor, in KitRequirementResolutionContext<TState> ctx);

    /// <summary>
    /// A thorough check to see if a requirement is met.
    /// </summary>
    ValueTask<KitRequirementResult> AcceptAsync<TState>(IKitRequirementVisitor<TState> visitor, in KitRequirementResolutionContext<TState> ctx, CancellationToken token = default);
}

public readonly struct KitRequirementResolutionContext<TState>
{
    public WarfarePlayer Player { get; }
    public Team Team { get; }
    public Kit Kit { get; }
    public Kit? CurrentKit { get; }
    public KitPlayerComponent Component { get; }

    public readonly TState State;

    /// <param name="player">The player requesting the kit.</param>
    /// <param name="team">The team the player's requesting the kit for.</param>
    /// <param name="kit">The kit being requested.</param>
    /// <param name="currentKit">The player's currently equipped kit, if any.</param>
    /// <param name="component">The player's <see cref="KitPlayerComponent"/>.</param>
    public KitRequirementResolutionContext(WarfarePlayer player, Team team, Kit kit, Kit? currentKit, KitPlayerComponent component, TState state)
    {
        Player = player;
        Team = team;
        Kit = kit;
        CurrentKit = currentKit;
        Component = component;
        State = state;
    }
}

/// <summary>
/// Manages the list of kit requirements for requesting kits.
/// </summary>
/// <remarks>Session-scoped service.</remarks>
public sealed class KitRequirementManager
{
    /// <summary>
    /// Requirements for a kit request.
    /// </summary>
    public ImmutableArray<IKitRequirement> Request { get; private set; }

    public KitRequirementManager(IServiceProvider serviceProvider)
    {
        Request =
        [
            ActivatorUtilities.CreateInstance<PremiumCostRequirement>(serviceProvider),
            ActivatorUtilities.CreateInstance<CreditCostRequirement>(serviceProvider),
            ActivatorUtilities.CreateInstance<RequiresSquadRequirement>(serviceProvider),
            ActivatorUtilities.CreateInstance<MinRequiredSquadMembersRequirement>(serviceProvider),
            ActivatorUtilities.CreateInstance<ClassesAllowedPerXTeammatesRequirement>(serviceProvider),
            ActivatorUtilities.CreateInstance<MapFilterRequirement>(serviceProvider),
            ActivatorUtilities.CreateInstance<FactionFilterRequirement>(serviceProvider),
            ActivatorUtilities.CreateInstance<LoadoutLockedRequirement>(serviceProvider),
            ActivatorUtilities.CreateInstance<LoadoutOutOfDateRequirement>(serviceProvider),
            ActivatorUtilities.CreateInstance<DisabledRequirement>(serviceProvider),
            ActivatorUtilities.CreateInstance<GlobalCooldownRequirement>(serviceProvider),
            ActivatorUtilities.CreateInstance<PremiumCooldownRequirement>(serviceProvider),
            ActivatorUtilities.CreateInstance<ExclusiveAccessRequirement>(serviceProvider),
            ActivatorUtilities.CreateInstance<KitSpecificUnlockRequirements>(serviceProvider),
            ActivatorUtilities.CreateInstance<NitroBoostRequirement>(serviceProvider)
        ];
    }

    /// <summary>
    /// Inserts a new kit requirement into the <see cref="Request"/> kit requirement list thread-safely.
    /// </summary>
    /// <param name="requirement">The requirement.</param>
    /// <param name="index">The index to insert it, or -1 to append it to the end of the list.</param>
    public void AddKitRequirement(IKitRequirement requirement, int index = -1)
    {
        if (requirement == null)
            throw new ArgumentNullException(nameof(requirement));

        lock (this)
        {
            ImmutableArray<IKitRequirement> request = Request;

            if (index < 0)
                index = request.Length;

            if (index > request.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            ImmutableArray<IKitRequirement>.Builder bldr = ImmutableArray.CreateBuilder<IKitRequirement>(request.Length + 1);

            if (index > 0)
            {
                bldr.AddRange(request, index);
            }

            bldr.Add(requirement);
            
            if (index < request.Length)
            {
                bldr.AddRange(request.AsSpan(index, request.Length - index));
            }

            Request = bldr.DrainToImmutable();
        }
    }
}

/// <summary>
/// New requirements should be added to this visitor.
/// </summary>
/// <remarks>May not be invoked on main thread.</remarks>
public interface IKitRequirementVisitor<TState>
{
    /// <summary>
    /// A generic requirement that doesn't have a special overload.
    /// </summary>
    void AcceptGenericRequirementNotMet(in KitRequirementResolutionContext<TState> ctx, string message);

    /// <summary>
    /// Invoked by <see cref="PremiumCostRequirement"/>.
    /// </summary>
    void AcceptPremiumCostNotMet(in KitRequirementResolutionContext<TState> ctx, decimal cost);

    /// <summary>
    /// Invoked by <see cref="CreditCostRequirement"/>.
    /// </summary>
    void AcceptCreditCostNotMet(in KitRequirementResolutionContext<TState> ctx, double cost, double current);

    /// <summary>
    /// Invoked by <see cref="ExclusiveAccessRequirement"/>.
    /// </summary>
    void AcceptExclusiveKitNotMet(in KitRequirementResolutionContext<TState> ctx);

    /// <summary>
    /// Invoked by <see cref="LoadoutLockedRequirement"/>.
    /// </summary>
    void AcceptLoadoutLockedNotMet(in KitRequirementResolutionContext<TState> ctx);

    /// <summary>
    /// Invoked by <see cref="LoadoutOutOfDateRequirement"/>.
    /// </summary>
    void AcceptLoadoutOutOfDateNotMet(in KitRequirementResolutionContext<TState> ctx, int season);

    /// <summary>
    /// Invoked by <see cref="DisabledRequirement"/>.
    /// </summary>
    void AcceptDisabledNotMet(in KitRequirementResolutionContext<TState> ctx);

    /// <summary>
    /// Invoked by <see cref="NitroBoostRequirement"/>.
    /// </summary>
    void AcceptNitroBoostRequirementNotMet(in KitRequirementResolutionContext<TState> ctx);

    /// <summary>
    /// Invoked by <see cref="MapFilterRequirement"/>.
    /// </summary>
    void AcceptMapFilterNotMet(in KitRequirementResolutionContext<TState> ctx);

    /// <summary>
    /// Invoked by <see cref="FactionFilterRequirement"/>.
    /// </summary>
    void AcceptFactionFilterNotMet(in KitRequirementResolutionContext<TState> ctx, FactionInfo faction);

    /// <summary>
    /// Invoked by <see cref="RequiresSquadRequirement"/>.
    /// </summary>
    void AcceptRequiresSquadNotMet(in KitRequirementResolutionContext<TState> ctx, bool needsSquadLead);

    /// <summary>
    /// Invoked by <see cref="MinRequiredSquadMembersRequirement"/>
    /// </summary>
    /// <param name="playerTakingKit">If any, the player that has already taken this kit in the squad.</param>
    /// <param name="squadMemberCount">Number of members in the squad.</param>
    /// <param name="minimumSquadMembers">Minimum required members.</param>
    void AcceptMinRequiredSquadMembersNotMet(in KitRequirementResolutionContext<TState> ctx, WarfarePlayer? playerTakingKit, int squadMemberCount, int minimumSquadMembers);

    /// <summary>
    /// Invoked by <see cref="ClassesAllowedPerXTeammatesRequirement"/>.
    /// </summary>
    /// <param name="allowedPerXUsers">Number of users that have to be available for each kit of this class.</param>
    /// <param name="currentUsers">Current number of teammates using this kit.</param>
    /// <param name="teammates">Total teammates.</param>
    /// <param name="kitsAllowed">Maximum number of kits allowed at this moment.</param>
    void AcceptClassesAllowedPerXTeammatesRequirementNotMet(in KitRequirementResolutionContext<TState> ctx, int allowedPerXUsers, int currentUsers, int teammates, int kitsAllowed);

    /// <summary>
    /// Invoked by <see cref="GlobalCooldownRequirement"/>.
    /// </summary>
    void AcceptGlobalCooldownNotMet(in KitRequirementResolutionContext<TState> ctx, in Cooldown requestCooldown);

    /// <summary>
    /// Invoked by <see cref="PremiumCooldownRequirement"/>.
    /// </summary>
    void AcceptPremiumCooldownNotMet(in KitRequirementResolutionContext<TState> ctx, in Cooldown requestCooldown);

    /// <summary>
    /// Invoked by <see cref="KitSpecificUnlockRequirements"/>.
    /// </summary>
    void AcceptKitSpecificUnlockRequirementNotMet(in KitRequirementResolutionContext<TState> ctx, UnlockRequirement requirement);
}

public enum KitRequirementResult
{
    /// <summary>
    /// The requirement is not met.
    /// </summary>
    No,

    /// <summary>
    /// Unable to tell, mainly for <see cref="IKitRequirement.IsMetCached"/>.
    /// </summary>
    Inconclusive,

    /// <summary>
    /// The requirement is met.
    /// </summary>
    Yes
}
using System;
using System.Collections.Generic;

namespace Uncreated.Warfare.Players;

/// <summary>
/// Handles player-wide votes for certain events. Currently used by seeding gamemode.
/// </summary>
public interface IPlayerVoteManager
{
    /// <summary>
    /// If there's a vote currently active.
    /// </summary>
    bool IsVoting { get; }

    /// <summary>
    /// The time at which the vote started (UTC).
    /// </summary>
    /// <exception cref="InvalidOperationException">Not voting.</exception>
    DateTime VoteStart { get; }

    /// <summary>
    /// The time at which the vote will end (UTC).
    /// </summary>
    /// <exception cref="InvalidOperationException">Not voting.</exception>
    DateTime VoteEnd { get; }

    /// <summary>
    /// Starts a new vote.
    /// </summary>
    /// <param name="settings">General settings for the vote.</param>
    /// <param name="callback">Invoked when the vote is completed or cancelled.</param>
    /// <param name="startCancellationToken">Cancellation token used to cancel starting the vote, not the same as the token used to cancel the vote.</param>
    /// <exception cref="InvalidOperationException">Already voting.</exception>
    UniTask StartVoteAsync(VoteSettings settings, Action<IVoteResult>? callback, CancellationToken startCancellationToken = default);

    /// <summary>
    /// Ends the current vote
    /// </summary>
    /// <param name="token">Cancellation token used to cancel ending the vote.</param>
    /// <param name="cancelled">If the vote should be cancelled instead of ended.</param>
    /// <exception cref="InvalidOperationException">Not voting.</exception>
    UniTask EndVoteAsync(CancellationToken token = default, bool cancelled = false);

    /// <summary>
    /// Gets a player's current vote state (unanswered/yes/no).
    /// </summary>
    /// <exception cref="InvalidOperationException">Not voting.</exception>
    PlayerVoteState GetVoteState(CSteamID player);

    /// <summary>
    /// Gets the number of votes in a certain state.
    /// </summary>
    /// <exception cref="InvalidOperationException">Not voting.</exception>
    int GetVoteCount(PlayerVoteState vote);

    /// <summary>
    /// Registers a player's vote with the system, overriding their current vote if they've already voted.
    /// </summary>
    /// <param name="player">The player that's voting.</param>
    /// <param name="vote">Their vote, or <see cref="PlayerVoteState.Unanswered"/> to remove thier vote.</param>
    /// <returns>Their previous vote, or <see cref="PlayerVoteState.Unanswered"/> if they hadn't voted.</returns>
    /// <exception cref="InvalidOperationException">Not voting.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Invalid vote.</exception>
    PlayerVoteState RegisterVote(CSteamID player, PlayerVoteState vote);
}

/// <summary>
/// The final results of a vote.
/// </summary>
public interface IVoteResult
{
    /// <summary>
    /// The overall result of the vote.
    /// </summary>
    VoteResult Result { get; }

    /// <summary>
    /// Dictionary of all participating player's final vote states (unanswered/yes/no).
    /// </summary>
    IReadOnlyDictionary<CSteamID, PlayerVoteState> Votes { get; }
    
    /// <summary>
    /// The original settings used for the vote.
    /// </summary>
    VoteSettings Settings { get; }
}

/// <summary>
/// Settings used to start votes for <see cref="IPlayerVoteManager"/>.
/// </summary>
public readonly struct VoteSettings
{
    /// <summary>
    /// Number of votes that must be reached for a Yes result.
    /// </summary>
    /// <remarks>If less than one this is the percentage of the current amount of players, otherwise it's a number of players.</remarks>
    public required double RequiredYes { get; init; }

    /// <summary>
    /// Number of votes that must be reached for a No result.
    /// </summary>
    /// <remarks>If less than one this is the percentage of the current amount of players, otherwise it's a number of players.</remarks>
    public required double RequiredNo { get; init; }

    /// <summary>
    /// Total duration of the vote.
    /// </summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// The vote result in the case of a tie (or no one votes).
    /// </summary>
    /// <remarks>Defaults to <see cref="VoteResult.No"/>.</remarks>
    public VoteResult DefaultResult { get; init; }

    /// <summary>
    /// Cancellation used to cancel the vote.
    /// </summary>
    public CancellationToken VoteCancellationToken { get; init; }

    /// <summary>
    /// Allows the vote to be displayed somehow.
    /// </summary>
    public IVoteDisplay? Display { get; init; }

    /// <summary>
    /// Whether or not <see cref="Display"/> should be disposed at the end of this vote, if it implements <see cref="IDisposable"/>.
    /// </summary>
    public bool OwnsDisplay { get; init; }
}

/// <summary>
/// The result of a vote from a <see cref="IPlayerVoteManager"/>.
/// </summary>
public enum VoteResult
{
    No,
    Yes,

    /// <summary>
    /// The vote was cancelled by <see cref="VoteSettings.VoteCancellationToken"/>.
    /// </summary>
    Cancelled
}

/// <summary>
/// The state of a single player's answer to a vote from a <see cref="IPlayerVoteManager"/>.
/// </summary>
public enum PlayerVoteState
{
    /// <summary>
    /// The player has not voted yet or did not vote.
    /// </summary>
    Unanswered,
    No,
    Yes
}

/// <summary>
/// Implements callbacks for vote updates, allowing them to be displayed on a HUD or similar.
/// </summary>
public interface IVoteDisplay
{
    /// <summary>
    /// Invoked on the game thread when the vote first starts.
    /// </summary>
    void VoteStarted(in VoteSettings settings, Func<WarfarePlayer, bool>? playerSelector);

    /// <summary>
    /// Invoked on the game thread when the vote finishes or gets cancelled.
    /// </summary>
    void VoteFinished(IVoteResult result);

    /// <summary>
    /// Invoked on the game thread when a player joins the vote.
    /// </summary>
    /// <param name="player"></param>
    void PlayerJoinedVote(WarfarePlayer player);

    /// <summary>
    /// Invoked on the game thread when a player leaves the vote.
    /// </summary>
    /// <param name="player"></param>
    void PlayerLeftVote(WarfarePlayer player);

    /// <summary>
    /// Invoked when a player enters or changes their vote.
    /// </summary>
    void PlayerVoteUpdated(CSteamID playerId, PlayerVoteState newVote, PlayerVoteState oldVote);
}
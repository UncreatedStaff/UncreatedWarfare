using Uncreated.Warfare.Events.Models.Players;

namespace Uncreated.Warfare.Players.PendingTasks;

/// <summary>
/// Used to perform tasks while the player is pending. Downloading kit data, stats, language data, etc.
/// </summary>
public interface IPlayerPendingTask
{
    /// <summary>
    /// If this <see cref="IPlayerPendingTask"/> will ever reject players (by returning <see langword="false"/> in <see cref="RunAsync"/>).
    /// </summary>
    bool CanReject { get; }

    /// <summary>
    /// Runs this task and returns whether or not the player is allowed to connect.
    /// </summary>
    Task<bool> RunAsync(PlayerPending e, CancellationToken token = default);

    /// <summary>
    /// Applies this task to the player once they join and all tasks have finished.
    /// </summary>
    void Apply(WarfarePlayer player);
}
using System;
using Uncreated.Warfare.Moderation.Appeals;
using Uncreated.Warfare.Moderation.Reports;

namespace Uncreated.Warfare.Moderation.Punishments;

public abstract class Punishment : ModerationEntry
{
    /// <summary>
    /// All related appeals.
    /// </summary>
    public Appeal[] Appeals { get; set; } = Array.Empty<Appeal>();

    /// <summary>
    /// All related reports.
    /// </summary>
    public Report[] Reports { get; set; } = Array.Empty<Report>();

    /// <summary>
    /// Try to find a resolved appeal with a state matching the value for <paramref name="state"/> in <see cref="Appeals"/>.
    /// </summary>
    /// <param name="appeal">The first matching appeal found.</param>
    /// <param name="state">Which state to look for, defaults to accepted.</param>
    /// <returns><see langword="true"/> if an appeal is found.</returns>
    public bool TryFindAppeal(out Appeal appeal, bool state = true)
    {
        for (int i = 0; i < Appeals.Length; ++i)
        {
            Appeal appeal2 = Appeals[i];
            if (appeal2.AppealState.HasValue && appeal2.AppealState.Value == state)
            {
                appeal = appeal2;
                return true;
            }
        }

        appeal = null!;
        return false;
    }
}

public abstract class DurationPunishment : Punishment
{
    /// <summary>
    /// Length of the punishment, negative implies permanent.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Returns <see langword="true"/> if the punishment will never expire.
    /// </summary>
    /// <remarks>This is indicated by a negative <see cref="Duration"/>.</remarks>
    public bool IsPermanent => Duration.Ticks < 0L;

    /// <summary>
    /// Gets the time at which the punishment expires, assuming it isn't appealed.
    /// </summary>
    /// <exception cref="InvalidOperationException">This punishment hasn't been resolved (<see cref="ModerationEntry.ResolvedTimestamp"/> is <see langword="null"/>).</exception>
    public DateTimeOffset GetExpiryTimestamp()
    {
        if (!ResolvedTimestamp.HasValue)
            throw new InvalidOperationException(GetType().Name + " has not been resolved.");

        return IsPermanent ? DateTimeOffset.MaxValue : ResolvedTimestamp.Value.Add(Duration);
    }
}
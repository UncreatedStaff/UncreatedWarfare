using System;
using System.Threading.Tasks;
using Uncreated.SQL;
using Uncreated.Warfare.Moderation.Appeals;
using Uncreated.Warfare.Moderation.Reports;

namespace Uncreated.Warfare.Moderation.Punishments;

public abstract class Punishment : ModerationEntry
{
    /// <summary>
    /// Keys for all related appeals.
    /// </summary>
    public PrimaryKey[] AppealKeys { get; set; } = Array.Empty<PrimaryKey>();

    /// <summary>
    /// Keys for all related reports.
    /// </summary>
    public PrimaryKey[] ReportKeys { get; set; } = Array.Empty<PrimaryKey>();

    /// <summary>
    /// All related appeals.
    /// </summary>
    public Appeal?[] Appeals { get; set; } = Array.Empty<Appeal>();

    /// <summary>
    /// All related reports.
    /// </summary>
    public Report?[] Reports { get; set; } = Array.Empty<Report>();

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
            Appeal? appeal2 = Appeals[i];
            if (appeal2 is { AppealState: not null } && appeal2.AppealState.Value == state)
            {
                appeal = appeal2;
                return true;
            }
        }

        appeal = null!;
        return false;
    }
    internal override async Task FillDetail(DatabaseInterface db)
    {
        if (Appeals.Length != AppealKeys.Length)
            Appeals = new Appeal?[AppealKeys.Length];
        if (Reports.Length != ReportKeys.Length)
            Reports = new Report?[ReportKeys.Length];
        for (int i = 0; i < AppealKeys.Length; ++i)
        {
            PrimaryKey key = AppealKeys[i];
            if (db.Cache.TryGet<Appeal>(key.Key, out Appeal? appeal, DatabaseInterface.DefaultInvalidateDuration))
                Appeals[i] = appeal;
            else
            {
                appeal = await db.ReadOne<Appeal>(key).ConfigureAwait(false);
                Appeals[i] = appeal;
            }
        }
        for (int i = 0; i < ReportKeys.Length; ++i)
        {
            PrimaryKey key = ReportKeys[i];
            if (db.Cache.TryGet<Report>(key.Key, out Report? report, DatabaseInterface.DefaultInvalidateDuration))
                Reports[i] = report;
            else
            {
                report = await db.ReadOne<Report>(key).ConfigureAwait(false);
                Reports[i] = report;
            }
        }
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

    /// <summary>
    /// Gets the time at which the punishment expires, assuming it isn't appealed.
    /// </summary>
    /// <exception cref="InvalidOperationException">This punishment hasn't been resolved (<see cref="ModerationEntry.ResolvedTimestamp"/> is <see langword="null"/>).</exception>
    public bool IsApplied()
    {
        return ResolvedTimestamp.HasValue && IsPermanent || DateTime.UtcNow > GetExpiryTimestamp().UtcDateTime;
    }
}
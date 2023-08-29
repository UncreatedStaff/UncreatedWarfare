using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.SQL;
using Uncreated.Warfare.Moderation.Appeals;
using Uncreated.Warfare.Moderation.Punishments;
using Uncreated.Warfare.Moderation.Reports;

namespace Uncreated.Warfare.Moderation;
internal static class Migration
{
    private static readonly DateTime UTCCutoff = new DateTime(2022, 6, 12, 3, 14, 0, DateTimeKind.Utc);
    public static readonly TimeZoneInfo RollbackWarfareTimezone;

    static Migration()
    {
        try
        {
            RollbackWarfareTimezone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                RollbackWarfareTimezone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
            }
            catch (TimeZoneNotFoundException)
            {
                RollbackWarfareTimezone = TimeZoneInfo.CreateCustomTimeZone("EST", TimeSpan.FromHours(-5d), "Eastern Standard Time", "Eastern Standard Time");
            }
        }
    }
    private static DateTimeOffset ConvertTime(DateTime dt)
    {
        if (dt <= UTCCutoff)
        {
            DateTime dt2 = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(dt, DateTimeKind.Unspecified), RollbackWarfareTimezone);
            return new DateTimeOffset(dt2, RollbackWarfareTimezone.GetUtcOffset(dt2));
        }

        return new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc));
    }
    public static async Task MigrateBans(DatabaseInterface db, CancellationToken token = default)
    {
        List<Ban> bans = new List<Ban>();
        await db.Sql.QueryAsync("SELECT `BanID`, `Banned`, `Banner`, `Duration`, `Reason`, `Timestamp` FROM `bans` ORDER BY `Banned`, `Timestamp`;", null,
            reader =>
            {
                Ban ban = new Ban
                {
                    LegacyId = reader.GetUInt32(0),
                    IsLegacy = true,
                    Player = reader.GetUInt64(1),
                    Actors = new RelatedActor[]
                    {
                        new RelatedActor(RelatedActor.RolePrimaryAdmin, true, Actors.GetActor(reader.GetUInt64(2)))
                    },
                    Removed = false,
                    Duration = TimeSpan.FromSeconds(reader.GetInt32(3)),
                    Message = reader.GetString(4),
                    StartedTimestamp = ConvertTime(reader.GetDateTime(5)),
                    Reputation = 0d,
                    ReputationApplied = false,
                    RemovedBy = null,
                    RemovedMessage = null,
                    RemovedTimestamp = null,
                    Evidence = Array.Empty<Evidence>(),
                    AppealKeys = Array.Empty<PrimaryKey>(),
                    Appeals = Array.Empty<Appeal>(),
                    ReportKeys = Array.Empty<PrimaryKey>(),
                    Reports = Array.Empty<Report>(),
                    RelevantLogsBegin = null,
                    RelevantLogsEnd = null,
                    ResolvedTimestamp = null,
                    Id = PrimaryKey.NotAssigned
                };
                ban.ResolvedTimestamp = ban.StartedTimestamp;
                ban.ResolvedTimestamp = ban.StartedTimestamp;
                bans.Add(ban);
            }, token).ConfigureAwait(false);

        await db.Sql.QueryAsync("SELECT `UnbanID`, `Pardoned`, `Pardoner`, `Timestamp` FROM `unbans` ORDER BY `Pardoned`, `Timestamp`;", null,
            reader =>
            {
                ulong steam64 = reader.GetUInt64(1);
                ulong admin = reader.GetUInt64(2);
                DateTimeOffset unbanTime = ConvertTime(reader.GetDateTime(3));
                for (int i = 0; i < bans.Count; ++i)
                {
                    Ban ban = bans[i];
                    if (ban.Player != steam64 || ban.Removed)
                        continue;

                    if (ban.WasAppliedAt(unbanTime))
                    {
                        ban.Removed = true;
                        ban.RemovedBy = Actors.GetActor(admin);
                        ban.RemovedTimestamp = unbanTime;
                    }
                }
            }, token).ConfigureAwait(false);


        for (int i = 0; i < bans.Count; i++)
        {
            Ban ban = bans[i];
            await db.AddOrUpdate(ban, token).ConfigureAwait(false);

            if (i % 10 == 0 || i == bans.Count - 1)
                L.LogDebug($"Bans: {i + 1}/{bans.Count}.");
        }
    }
}

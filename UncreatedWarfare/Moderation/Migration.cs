using System;
using System.Collections.Generic;
using System.Globalization;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Moderation.Appeals;
using Uncreated.Warfare.Moderation.Punishments;
using Uncreated.Warfare.Moderation.Records;
using Report = Uncreated.Warfare.Moderation.Reports.Report;

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
                L.LogWarning("Couldn't find rollback timezone.");
                return;
            }
        }

        L.Log($"Found rollback timezone: {RollbackWarfareTimezone.DisplayName} (Offset: {RollbackWarfareTimezone.BaseUtcOffset.TotalHours} HRS), {RollbackWarfareTimezone.Id}.");
    }
    private static DateTimeOffset ConvertTime(DateTime dt)
    {
        if (dt <= UTCCutoff)
        {
            DateTime dt2 = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(dt, DateTimeKind.Unspecified), RollbackWarfareTimezone);
            return new DateTimeOffset(dt2);
        }

        return new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc));
    }
    public static async Task MigrateKicks(DatabaseInterface db, CancellationToken token = default)
    {
        List<Kick> kicks = new List<Kick>();
        await db.Sql.QueryAsync("SELECT `KickID`, `Kicked`, `Kicker`, `Reason`, `Timestamp` FROM `kicks` ORDER BY `KickID`;", null, token,
            reader =>
            {
                Kick kick = new Kick
                {
                    LegacyId = reader.GetUInt32(0),
                    IsLegacy = true,
                    Player = reader.GetUInt64(1),
                    Actors = new RelatedActor[]
                    {
                        new RelatedActor(RelatedActor.RolePrimaryAdmin, true, Actors.GetActor(reader.GetUInt64(2)))
                    },
                    Removed = false,
                    Message = reader.GetString(3),
                    StartedTimestamp = ConvertTime(reader.GetDateTime(4)),
                    Reputation = 0d,
                    PendingReputation = 0d,
                    RemovedBy = null,
                    RemovedMessage = null,
                    RemovedTimestamp = null,
                    Evidence = Array.Empty<Evidence>(),
                    AppealKeys = Array.Empty<uint>(),
                    Appeals = Array.Empty<Appeal>(),
                    ReportKeys = Array.Empty<uint>(),
                    Reports = Array.Empty<Report>(),
                    RelevantLogsBegin = null,
                    RelevantLogsEnd = null,
                    ResolvedTimestamp = null,
                    Id = 0u
                };
                kick.ResolvedTimestamp = kick.StartedTimestamp;
                kicks.Add(kick);
            }).ConfigureAwait(false);

        for (int i = 0; i < kicks.Count; i++)
        {
            Kick kick = kicks[i];
            await db.AddOrUpdate(kick, token).ConfigureAwait(false);

            if (i % 10 == 0 || i == kicks.Count - 1)
                L.LogDebug($"Kicks: {i + 1}/{kicks.Count}.");
        }
    }
    public static async Task MigrateWarnings(DatabaseInterface db, CancellationToken token = default)
    {
        List<Warning> warnings = new List<Warning>();
        await db.Sql.QueryAsync("SELECT `WarnID`, `Warned`, `Warner`, `Reason`, `Timestamp` FROM `warnings` ORDER BY `WarnID`;", null, token,
            reader =>
            {
                Warning warning = new Warning
                {
                    LegacyId = reader.GetUInt32(0),
                    IsLegacy = true,
                    Player = reader.GetUInt64(1),
                    Actors = new RelatedActor[]
                    {
                        new RelatedActor(RelatedActor.RolePrimaryAdmin, true, Actors.GetActor(reader.GetUInt64(2)))
                    },
                    Removed = false,
                    Message = reader.GetString(3),
                    StartedTimestamp = ConvertTime(reader.GetDateTime(4)),
                    Reputation = 0d,
                    PendingReputation = 0d,
                    RemovedBy = null,
                    RemovedMessage = null,
                    RemovedTimestamp = null,
                    Evidence = Array.Empty<Evidence>(),
                    AppealKeys = Array.Empty<uint>(),
                    Appeals = Array.Empty<Appeal>(),
                    ReportKeys = Array.Empty<uint>(),
                    Reports = Array.Empty<Report>(),
                    RelevantLogsBegin = null,
                    RelevantLogsEnd = null,
                    ResolvedTimestamp = null,
                    Id = 0u,
                    DisplayedTimestamp = null
                };
                warning.ResolvedTimestamp = warning.StartedTimestamp;
                warnings.Add(warning);
            }).ConfigureAwait(false);

        for (int i = 0; i < warnings.Count; i++)
        {
            Warning warning = warnings[i];
            await db.AddOrUpdate(warning, token).ConfigureAwait(false);

            if (i % 10 == 0 || i == warnings.Count - 1)
                L.LogDebug($"Warnings: {i + 1}/{warnings.Count}.");
        }
    }
    public static async Task MigrateBattlEyeKicks(DatabaseInterface db, CancellationToken token = default)
    {
        List<BattlEyeKick> kicks = new List<BattlEyeKick>();
        await db.Sql.QueryAsync("SELECT `BattleyeID`, `Kicked`, `Reason`, `Timestamp` FROM `battleye_kicks` ORDER BY `BattleyeID`;", null, token,
            reader =>
            {
                BattlEyeKick kick = new BattlEyeKick
                {
                    LegacyId = reader.GetUInt32(0),
                    IsLegacy = true,
                    Player = reader.GetUInt64(1),
                    Actors = new RelatedActor[]
                    {
                        new RelatedActor(RelatedActor.RolePrimaryAdmin, true, Actors.BattlEye)
                    },
                    Removed = false,
                    Message = reader.GetString(2),
                    StartedTimestamp = ConvertTime(reader.GetDateTime(3)),
                    Reputation = 0d,
                    PendingReputation = 0d,
                    RemovedBy = null,
                    RemovedMessage = null,
                    RemovedTimestamp = null,
                    Evidence = Array.Empty<Evidence>(),
                    RelevantLogsBegin = null,
                    RelevantLogsEnd = null,
                    ResolvedTimestamp = null,
                    Id = 0u
                };
                kick.ResolvedTimestamp = kick.StartedTimestamp;
                kicks.Add(kick);
            }).ConfigureAwait(false);

        for (int i = 0; i < kicks.Count; i++)
        {
            BattlEyeKick kick = kicks[i];
            await db.AddOrUpdate(kick, token).ConfigureAwait(false);

            if (i % 10 == 0 || i == kicks.Count - 1)
                L.LogDebug($"BattlEye Kicks: {i + 1}/{kicks.Count}.");
        }
    }
    public static async Task MigrateMutes(DatabaseInterface db, CancellationToken token = default)
    {
        List<Mute> mutes = new List<Mute>();
        await db.Sql.QueryAsync("SELECT `ID`, `Steam64`, `Admin`, `Reason`, `Duration`, `Timestamp`, `Type`, `Deactivated`, `DeactivateTimestamp` FROM `muted` ORDER BY `ID`;", null, token,
            reader =>
            {
                int time = reader.GetInt32(4);
                Mute mute = new Mute
                {
                    LegacyId = reader.GetUInt32(0),
                    IsLegacy = true,
                    Player = reader.GetUInt64(1),
                    Actors =
                    [
                        new RelatedActor(RelatedActor.RolePrimaryAdmin, true, Actors.GetActor(reader.GetUInt64(2)))
                    ],
                    Removed = false,
                    Message = reader.GetString(3),
                    Duration = time < 0 ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(time),
                    StartedTimestamp = ConvertTime(reader.GetDateTime(5)),
                    Reputation = 0d,
                    PendingReputation = 0d,
                    RemovedBy = null,
                    RemovedMessage = null,
                    RemovedTimestamp = null,
                    Evidence = Array.Empty<Evidence>(),
                    AppealKeys = Array.Empty<uint>(),
                    Appeals = Array.Empty<Appeal>(),
                    ReportKeys = Array.Empty<uint>(),
                    Reports = Array.Empty<Report>(),
                    RelevantLogsBegin = null,
                    RelevantLogsEnd = null,
                    ResolvedTimestamp = null,
                    Id = 0u,
                    Type = reader.GetByte(6) switch
                    {
                        1 => MuteType.Voice,
                        2 => MuteType.Text,
                        3 => MuteType.Both,
                        _ => MuteType.None
                    }
                };
                mute.ResolvedTimestamp = mute.StartedTimestamp;
                if (reader.GetBoolean(7))
                {
                    DateTimeOffset? dt = reader.IsDBNull(8) ? null : new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(8), DateTimeKind.Utc));
                    mute.Forgiven = true;
                    mute.ForgiveTimestamp = dt;
                }
                mutes.Add(mute);
            }).ConfigureAwait(false);

        for (int i = 0; i < mutes.Count; i++)
        {
            Mute mute = mutes[i];
            await db.AddOrUpdate(mute, token).ConfigureAwait(false);

            if (i % 10 == 0 || i == mutes.Count - 1)
                L.LogDebug($"Mutes: {i + 1}/{mutes.Count}.");
        }
    }
    public static async Task MigrateBans(DatabaseInterface db, CancellationToken token = default)
    {
        List<Ban> bans = new List<Ban>();
        await db.Sql.QueryAsync("SELECT `BanID`, `Banned`, `Banner`, `Duration`, `Reason`, `Timestamp` FROM `bans` ORDER BY `Banned`, `Timestamp`;", null, token,
            reader =>
            {
                int time = reader.GetInt32(3);
                Ban ban = new Ban
                {
                    LegacyId = reader.GetUInt32(0),
                    IsLegacy = true,
                    Player = reader.GetUInt64(1),
                    Actors =
                    [
                        new RelatedActor(RelatedActor.RolePrimaryAdmin, true, Actors.GetActor(reader.GetUInt64(2)))
                    ],
                    Removed = false,
                    Duration = time < 0 ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(time),
                    Message = reader.GetString(4),
                    StartedTimestamp = ConvertTime(reader.GetDateTime(5)),
                    Reputation = 0d,
                    PendingReputation = 0d,
                    RemovedBy = null,
                    RemovedMessage = null,
                    RemovedTimestamp = null,
                    Evidence = Array.Empty<Evidence>(),
                    AppealKeys = Array.Empty<uint>(),
                    Appeals = Array.Empty<Appeal>(),
                    ReportKeys = Array.Empty<uint>(),
                    Reports = Array.Empty<Report>(),
                    RelevantLogsBegin = null,
                    RelevantLogsEnd = null,
                    ResolvedTimestamp = null,
                    Id = 0u
                };
                ban.ResolvedTimestamp = ban.StartedTimestamp;
                bans.Add(ban);
            }).ConfigureAwait(false);

        await db.Sql.QueryAsync("SELECT `UnbanID`, `Pardoned`, `Pardoner`, `Timestamp` FROM `unbans` ORDER BY `Pardoned`, `Timestamp`;", null, token,
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

                    if (ban.WasAppliedAt(unbanTime, false))
                    {
                        ban.Forgiven = true;
                        ban.ForgivenBy = Actors.GetActor(admin);
                        ban.ForgiveTimestamp = unbanTime;
                        RelatedActor[] actors = ban.Actors;
                        Array.Resize(ref actors, (actors?.Length ?? 0) + 1);
                        actors[^1] = new RelatedActor(RelatedActor.RoleRemovingAdmin, true, ban.ForgivenBy);
                        ban.Actors = actors;
                    }
                }
            }).ConfigureAwait(false);

        bans.Sort((a, b) => a.LegacyId!.Value.CompareTo(b.LegacyId!.Value));

        for (int i = 0; i < bans.Count; i++)
        {
            Ban ban = bans[i];
            await db.AddOrUpdate(ban, token).ConfigureAwait(false);

            if (i % 10 == 0 || i == bans.Count - 1)
                L.LogDebug($"Bans: {i + 1}/{bans.Count}.");
        }
    }
    public static async Task MigrateTeamkills(DatabaseInterface db, CancellationToken token = default)
    {
        List<Teamkill> teamkills = new List<Teamkill>();
        await db.Sql.QueryAsync("SELECT `TeamkillID`, `Teamkiller`, `Teamkilled`, `Cause`, `Item`, `ItemID`, `Distance`, `Timestamp` FROM `teamkills` ORDER BY `TeamkillID`;", null, token,
            reader =>
            {
                Teamkill teamkill = new Teamkill
                {
                    LegacyId = reader.GetUInt32(0),
                    IsLegacy = true,
                    Player = reader.GetUInt64(1),
                    Actors = new RelatedActor[]
                    {
                        new RelatedActor(Teamkill.RoleTeamkilled, false, Actors.GetActor(reader.GetUInt64(2)))
                    },
                    Removed = false,
                    Message = null,
                    StartedTimestamp = ConvertTime(reader.GetDateTime(7)),
                    Reputation = 0d,
                    PendingReputation = 0d,
                    RemovedBy = null,
                    RemovedMessage = null,
                    RemovedTimestamp = null,
                    Evidence = Array.Empty<Evidence>(),
                    RelevantLogsBegin = null,
                    RelevantLogsEnd = null,
                    ResolvedTimestamp = null,
                    Id = 0u,
                    Limb = null,
                    Cause = reader.IsDBNull(3) ? null : (!int.TryParse(reader.GetString(3), NumberStyles.Number, CultureInfo.InvariantCulture, out _) && Enum.TryParse(reader.GetString(3), true, out EDeathCause cause)) ? cause : null,
                    Distance = reader.IsDBNull(6) ? null : reader.GetFloat(6)
                };
                ushort itemId = reader.IsDBNull(5) ? (ushort)0 : reader.GetUInt16(5);
                if (itemId != 0 && Provider.isInitialized && Assets.hasLoadedUgc && (Assets.find(EAssetType.ITEM, itemId) ?? Assets.find(EAssetType.VEHICLE, itemId)) is { } asset)
                {
                    teamkill.ItemName = asset.FriendlyName ?? asset.name ?? asset.id.ToString();
                    teamkill.Item = asset.GUID;
                }
                else
                {
                    teamkill.ItemName = reader.IsDBNull(4) ? null : reader.GetString(4);
                }

                teamkill.ResolvedTimestamp = teamkill.StartedTimestamp;
                teamkills.Add(teamkill);
            }).ConfigureAwait(false);

        for (int i = 0; i < teamkills.Count; i++)
        {
            Teamkill teamkill = teamkills[i];
            await db.AddOrUpdate(teamkill, token).ConfigureAwait(false);

            if (i % 10 == 0 || i == teamkills.Count - 1)
                L.LogDebug($"Teamkills: {i + 1}/{teamkills.Count}.");
        }
    }
}

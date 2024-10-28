#if DEBUG
using System;
using System.Collections.Generic;
using System.Globalization;
using Uncreated.Warfare.Database.Manual;
using Uncreated.Warfare.Interaction.Commands;
using Uncreated.Warfare.Moderation;
using Uncreated.Warfare.Moderation.Appeals;
using Uncreated.Warfare.Moderation.Punishments;
using Uncreated.Warfare.Moderation.Records;
using Uncreated.Warfare.Moderation.Reports;

namespace Uncreated.Warfare.Commands;

[Command("offenses"), HideFromHelp, SubCommandOf(typeof(MigrateCommand))]
public class MigrateLegacyOffensesCommand : IExecutableCommand
{
    // the UTC time at which we switched from storing data in EST to UTC
    private readonly DateTime _universalTimeCutoff = new DateTime(2022, 6, 12, 3, 14, 0, DateTimeKind.Utc);
    private readonly TimeZoneInfo _rollbackWarfareTimezone;

    private readonly IManualMySqlProvider _mySqlProvider;
    private readonly DatabaseInterface _moderationSql;
    public CommandContext Context { get; set; }

    public MigrateLegacyOffensesCommand(IManualMySqlProvider mySqlProvider, DatabaseInterface moderationSql, ILogger<MigrateLegacyOffensesCommand> logger)
    {
        _mySqlProvider = mySqlProvider;
        _moderationSql = moderationSql;

        const string timeZoneWindows = "Eastern Standard Time";
        const string timeZoneUnix = "America/New_York";

        try
        {
            _rollbackWarfareTimezone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneWindows);
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                _rollbackWarfareTimezone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneUnix);
            }
            catch (TimeZoneNotFoundException)
            {
                _rollbackWarfareTimezone = TimeZoneInfo.CreateCustomTimeZone("EST", TimeSpan.FromHours(-5d), timeZoneWindows, timeZoneWindows);
                logger.LogWarning("Couldn't find rollback timezone ({0} or {1}).", timeZoneWindows, timeZoneUnix);
            }
        }

        logger.LogInformation("Found rollback timezone: {0} (Offset: {1} HRS), {2}.", _rollbackWarfareTimezone.DisplayName, _rollbackWarfareTimezone.BaseUtcOffset.TotalHours, _rollbackWarfareTimezone.Id);
    }

    public UniTask ExecuteAsync(CancellationToken token)
    {
        Context.AssertRanByTerminal();

        return MigrateOffenses(token);
    }

    private DateTimeOffset ConvertTime(DateTime dt)
    {
        if (dt <= _universalTimeCutoff)
        {
            DateTime dt2 = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(dt, DateTimeKind.Unspecified), _rollbackWarfareTimezone);
            return new DateTimeOffset(dt2);
        }

        return new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc));
    }

    private async UniTask MigrateOffenses(CancellationToken token)
    {
        Context.AssertRanByTerminal();

        Context.ReplyString("Kicks...");
        await MigrateKicks(token).ConfigureAwait(false);

        Context.ReplyString("Warnings...");
        await MigrateWarnings(token).ConfigureAwait(false);

        Context.ReplyString("BattlEye Kicks...");
        await MigrateBattlEyeKicks(token).ConfigureAwait(false);

        Context.ReplyString("Mutes...");
        await MigrateMutes(token).ConfigureAwait(false);

        Context.ReplyString("Bans...");
        await MigrateBans(token).ConfigureAwait(false);

        Context.ReplyString("Teamkills...");
        await MigrateTeamkills(token).ConfigureAwait(false);

        Context.ReplyString("Done.");
    }

    public async Task MigrateKicks(CancellationToken token = default)
    {
        List<Kick> kicks = new List<Kick>();
        await _mySqlProvider.QueryAsync("SELECT `KickID`, `Kicked`, `Kicker`, `Reason`, `Timestamp` FROM `kicks` ORDER BY `KickID`;", null, token,
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
            await _moderationSql.AddOrUpdate(kick, token).ConfigureAwait(false);

            if (i % 10 == 0 || i == kicks.Count - 1)
                Context.ReplyString($"Kicks: {i + 1}/{kicks.Count} ({(float)(i + 1) / kicks.Count:P2}).");
        }
    }
    public async Task MigrateWarnings(CancellationToken token = default)
    {
        List<Warning> warnings = new List<Warning>();
        await _mySqlProvider.QueryAsync("SELECT `WarnID`, `Warned`, `Warner`, `Reason`, `Timestamp` FROM `warnings` ORDER BY `WarnID`;", null, token,
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
            await _moderationSql.AddOrUpdate(warning, token).ConfigureAwait(false);

            if (i % 10 == 0 || i == warnings.Count - 1)
                Context.ReplyString($"Warnings: {i + 1}/{warnings.Count} ({(float)(i + 1) / warnings.Count:P2}).");
        }
    }
    public async Task MigrateBattlEyeKicks(CancellationToken token = default)
    {
        List<BattlEyeKick> kicks = new List<BattlEyeKick>();
        await _mySqlProvider.QueryAsync("SELECT `BattleyeID`, `Kicked`, `Reason`, `Timestamp` FROM `battleye_kicks` ORDER BY `BattleyeID`;", null, token,
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
            await _moderationSql.AddOrUpdate(kick, token).ConfigureAwait(false);

            if (i % 10 == 0 || i == kicks.Count - 1)
                Context.ReplyString($"BattlEye Kicks: {i + 1}/{kicks.Count} ({(float)(i + 1) / kicks.Count:P2}).");
        }
    }
    public async Task MigrateMutes(CancellationToken token = default)
    {
        List<Mute> mutes = new List<Mute>();
        await _mySqlProvider.QueryAsync("SELECT `ID`, `Steam64`, `Admin`, `Reason`, `Duration`, `Timestamp`, `Type`, `Deactivated`, `DeactivateTimestamp` FROM `muted` ORDER BY `ID`;", null, token,
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
            await _moderationSql.AddOrUpdate(mute, token).ConfigureAwait(false);

            if (i % 10 == 0 || i == mutes.Count - 1)
                Context.ReplyString($"Mutes: {i + 1}/{mutes.Count} ({(float)(i + 1) / mutes.Count:P2}).");
        }
    }
    public async Task MigrateBans(CancellationToken token = default)
    {
        List<Ban> bans = new List<Ban>();
        await _mySqlProvider.QueryAsync("SELECT `BanID`, `Banned`, `Banner`, `Duration`, `Reason`, `Timestamp` FROM `bans` ORDER BY `Banned`, `Timestamp`;", null, token,
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

        await _mySqlProvider.QueryAsync("SELECT `UnbanID`, `Pardoned`, `Pardoner`, `Timestamp` FROM `unbans` ORDER BY `Pardoned`, `Timestamp`;", null, token,
            reader =>
            {
                ulong steam64 = reader.GetUInt64(1);
                ulong admin = reader.GetUInt64(2);
                DateTimeOffset unbanTime = ConvertTime(reader.GetDateTime(3));
                for (int i = 0; i < bans.Count; ++i)
                {
                    Ban ban = bans[i];
                    if (ban.Player != steam64 || ban.Forgiven)
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
            await _moderationSql.AddOrUpdate(ban, token).ConfigureAwait(false);

            if (i % 10 == 0 || i == bans.Count - 1)
                Context.ReplyString($"Bans: {i + 1}/{bans.Count} ({(float)(i + 1) / bans.Count:P2}).");
        }
    }
    public async Task MigrateTeamkills(CancellationToken token = default)
    {
        List<Teamkill> teamkills = new List<Teamkill>();
        await _mySqlProvider.QueryAsync("SELECT `TeamkillID`, `Teamkiller`, `Teamkilled`, `Cause`, `Item`, `ItemID`, `Distance`, `Timestamp` FROM `teamkills` ORDER BY `TeamkillID`;", null, token,
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
            await _moderationSql.AddOrUpdate(teamkill, token).ConfigureAwait(false);

            if (i % 10 == 0 || i == teamkills.Count - 1)
                Context.ReplyString($"Teamkills: {i + 1}/{teamkills.Count} ({(float)(i + 1) / teamkills.Count:P2}).");
        }
    }
}
#endif
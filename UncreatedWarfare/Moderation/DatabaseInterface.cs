using MySqlConnector;
using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Framework;
using Uncreated.Networking;
using Uncreated.Players;
using Uncreated.SQL;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Moderation.Appeals;
using Uncreated.Warfare.Moderation.Commendation;
using Uncreated.Warfare.Moderation.Punishments;
using Uncreated.Warfare.Moderation.Records;
using Uncreated.Warfare.Moderation.Reports;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Vehicles;
using Report = Uncreated.Warfare.Moderation.Reports.Report;

namespace Uncreated.Warfare.Moderation;
public abstract class DatabaseInterface
{
    public static readonly TimeSpan DefaultInvalidateDuration = TimeSpan.FromSeconds(3);
    private readonly Dictionary<ulong, string> _iconUrlCacheSmall = new Dictionary<ulong, string>(128);
    private readonly Dictionary<ulong, string> _iconUrlCacheMedium = new Dictionary<ulong, string>(128);
    private readonly Dictionary<ulong, string> _iconUrlCacheFull = new Dictionary<ulong, string>(128);
    public ModerationCache Cache { get; } = new ModerationCache(64);
    public bool TryGetAvatar(ulong steam64, AvatarSize size, out string avatar)
    {
        Dictionary<ulong, string> dict = size switch
        {
            AvatarSize.Full => _iconUrlCacheFull,
            AvatarSize.Medium => _iconUrlCacheMedium,
            _ => _iconUrlCacheSmall
        };
        return dict.TryGetValue(steam64, out avatar);
    }
    public void UpdateAvatar(ulong steam64, AvatarSize size, string value)
    {
        Dictionary<ulong, string> dict = size switch
        {
            AvatarSize.Full => _iconUrlCacheFull,
            AvatarSize.Medium => _iconUrlCacheMedium,
            _ => _iconUrlCacheSmall
        };
        dict[steam64] = value;
    }
    public abstract IWarfareSql Sql { get; }
    public Task VerifyTables(CancellationToken token = default) => Sql.VerifyTables(Schema, token);
    public async Task<PlayerNames> GetUsernames(ulong id, CancellationToken token = default)
    {
        if (UCWarfare.IsLoaded)
            return await F.GetPlayerOriginalNamesAsync(id, token).ConfigureAwait(false);

        return await Sql.GetUsernamesAsync(id, token).ConfigureAwait(false);
    }
    public async Task<T?> ReadOne<T>(PrimaryKey id, bool tryGetFromCache, CancellationToken token = default) where T : ModerationEntry
    {
        if (tryGetFromCache && Cache.TryGet(id, out T val, DefaultInvalidateDuration))
            return val;

        StringBuilder sb = new StringBuilder("SELECT ", 128);
        int flag = AppendReadColumns(sb, typeof(T));
        AppendTables(sb, flag);
        sb.Append($" WHERE `main`.`{ColumnEntriesPrimaryKey}` = @0;");

        object[] pkArgs = { id.Key };
        ModerationEntry? entry = null;
        await Sql.QueryAsync(sb.ToString(), pkArgs, reader =>
        {
            entry = ReadEntry(flag, reader);
            return true;
        }, token).ConfigureAwait(false);

        if (entry == null)
        {
            Cache.Remove(id.Key);
            return null;
        }

        await Fill(new ModerationEntry[] { entry }, null, token).ConfigureAwait(false);
        
        return entry as T;
    }
    public async Task<T?[]> ReadAll<T>(PrimaryKey[] ids, bool tryGetFromCache, CancellationToken token = default) where T : ModerationEntry
    {
        T?[] result = new T?[ids.Length];
        await ReadAll(result, ids, tryGetFromCache, token).ConfigureAwait(false);
        return result;
    }
    public async Task ReadAll<T>(T?[] result, PrimaryKey[] ids, bool tryGetFromCache, CancellationToken token = default) where T : ModerationEntry
    {
        if (result.Length != ids.Length)
            throw new ArgumentException("Result must be the same length as ids.", nameof(result));

        StringBuilder sb = new StringBuilder("SELECT ", 164);
        int flag = AppendReadColumns(sb, typeof(T));
        AppendTables(sb, flag);
        sb.Append($" WHERE `main`.`{ColumnEntriesPrimaryKey}` IN (");
        SqlTypes.AppendParameterList(sb, 0, ids.Length);
        sb.Append(");");

        object[] parameters = new object[ids.Length];
        for (int i = 0; i < ids.Length; ++i)
            parameters[i] = ids[i].Key;
        
        BitArray? mask = null;
        if (tryGetFromCache)
        {
            bool miss = false;
            for (int i = 0; i < ids.Length; ++i)
            {
                if (Cache.TryGet(ids[i].Key, out T val, DefaultInvalidateDuration))
                    result[i] = val;
                else
                    miss = true;
            }

            if (!miss)
                return;

            mask = new BitArray(result.Length);
            for (int i = 0; i < mask.Length; ++i)
                mask[i] = result[i] is null;
        }
        await Sql.QueryAsync(sb.ToString(), parameters, reader =>
        {
            ModerationEntry? entry = ReadEntry(flag, reader);
            if (entry == null)
                return;
            int pk = entry.Id;
            int index = -1;
            for (int j = 0; j < ids.Length; ++j)
            {
                if (ids[j].Key == pk)
                {
                    index = j;
                    break;
                }
            }
            if (index == -1 || entry is not T val)
                return;
            result[index] = val;
        }, token).ConfigureAwait(false);
        
        // ReSharper disable once CoVariantArrayConversion
        await Fill(result, mask, token).ConfigureAwait(false);
    }
    public async Task<T[]> ReadAll<T>(ulong actor, ActorRelationType relation, DateTimeOffset? start = null, DateTimeOffset? end = null, string? orderBy = null, string? condition = null, object[]? conditionArgs = null, CancellationToken token = default) where T : ModerationEntry
        => (T[])await ReadAll(typeof(T), actor, relation, start, end, orderBy, condition, conditionArgs, token).ConfigureAwait(false);
    public async Task<Array> ReadAll(Type type, ulong actor, ActorRelationType relation, DateTimeOffset? start = null, DateTimeOffset? end = null, string? orderBy = null, string? condition = null, object[]? conditionArgs = null, CancellationToken token = default)
    {
        StringBuilder sb = new StringBuilder("SELECT ", 164);
        int flag = AppendReadColumns(sb, type);
        AppendTables(sb, flag);
        sb.Append(" WHERE ");

        switch (relation)
        {
            default:
            case ActorRelationType.IsTarget:
                sb.Append($" `{ColumnEntriesSteam64}`=@0;");
                break;
            case ActorRelationType.IsActor:
                sb.Append($" (SELECT COUNT(*) FROM `{TableActors}` AS `a` WHERE `a`.`{ColumnExternalPrimaryKey}` = `main`.`{ColumnEntriesPrimaryKey}` AND `a`.`{ColumnActorsId}`=@0) > 0");
                break;
            case ActorRelationType.IsAdminActor:
                sb.Append($" (SELECT COUNT(*) FROM `{TableActors}` AS `a` WHERE `a`.`{ColumnExternalPrimaryKey}` = `main`.`{ColumnEntriesPrimaryKey}` AND `a`.`{ColumnActorsId}`=@0 AND `a`.`{ColumnActorsAsAdmin}` != 0) > 0");
                break;
            case ActorRelationType.IsNonAdminActor:
                sb.Append($" (SELECT COUNT(*) FROM `{TableActors}` AS `a` WHERE `a`.`{ColumnExternalPrimaryKey}` = `main`.`{ColumnEntriesPrimaryKey}` AND `a`.`{ColumnActorsId}`=@0 AND `a`.`{ColumnActorsAsAdmin}` == 0) > 0");
                break;
        }
        
        ModerationEntryType[]? types = null;
        if (typeof(T) != typeof(ModerationEntry))
            ModerationReflection.TypeInheritance.TryGetValue(typeof(T), out types);
        object[] actorArgs;
        int offset;
        int xtraLength = (types == null ? 0 : types.Length) + (conditionArgs == null || condition == null ? 0 : conditionArgs.Length);
        if (start.HasValue && end.HasValue)
        {
            offset = 3;
            actorArgs = new object[3 + xtraLength];
            actorArgs[1] = start.Value.UtcDateTime;
            actorArgs[2] = end.Value.UtcDateTime;
            sb.Append($" AND `main`.`{ColumnEntriesStartTimestamp}` >= @1 AND `main`.`{ColumnEntriesStartTimestamp}` <= @2");
        }
        else if (start.HasValue)
        {
            offset = 2;
            actorArgs = new object[2 + xtraLength];
            actorArgs[1] = start.Value.UtcDateTime;
            sb.Append($" AND `main`.`{ColumnEntriesStartTimestamp}` >= @1");
        }
        else if (end.HasValue)
        {
            offset = 2;
            actorArgs = new object[2 + xtraLength];
            actorArgs[1] = end.Value.UtcDateTime;
            sb.Append($" AND `main`.`{ColumnEntriesStartTimestamp}` <= @1");
        }
        else
        {
            offset = 1;
            actorArgs = new object[1 + xtraLength];
        }

        actorArgs[0] = actor;
        if (types is { Length: > 0 })
        {
            for (int i = 0; i < types.Length; ++i)
                actorArgs[offset + i] = types[i].ToString();
            sb.Append($" AND `main`.`{ColumnEntriesType}` IN (");
            for (int i = 0; i < types.Length; ++i)
            {
                if (i != 0)
                    sb.Append(',');
                sb.Append('@').Append((i + offset).ToString(CultureInfo.InvariantCulture));
            }

            sb.Append(")");
        }

        if (types != null)
            offset += types.Length;
        if (conditionArgs != null && condition != null)
        {
            for (int i = 0; i < conditionArgs.Length; ++i)
            {
                actorArgs[offset + i] = conditionArgs[i];
                condition = Util.QuickFormat(condition, "@" + (offset + i).ToString(CultureInfo.InvariantCulture), i);
            }
        }

        if (condition != null)
            sb.Append(" AND " + condition);

        if (orderBy != null)
            sb.Append(" ORDER BY " + orderBy);

        sb.Append(';');


        ArrayList entries = new ArrayList(4);
        await Sql.QueryAsync(sb.ToString(), actorArgs, reader =>
        {
            ModerationEntry? entry = ReadEntry(flag, reader);
            if (type.IsInstanceOfType(entry))
                entries.Add(entry);
        }, token).ConfigureAwait(false);

        Array rtn = entries.ToArray(type);

        // ReSharper disable once CoVariantArrayConversion
        await Fill((ModerationEntry[])rtn, null, token).ConfigureAwait(false);
        
        return rtn;
    }
    public async Task<T[]> ReadAll<T>(DateTimeOffset? start = null, DateTimeOffset? end = null, string? condition = null, string? orderBy = null, object[]? conditionArgs = null, CancellationToken token = default) where T : ModerationEntry
        => (T[])await ReadAll(typeof(T), start, end, condition, orderBy, conditionArgs, token).ConfigureAwait(false);
    public async Task<Array> ReadAll(Type type, DateTimeOffset? start = null, DateTimeOffset? end = null, string? condition = null, string? orderBy = null, object[]? conditionArgs = null, CancellationToken token = default)
    {
        StringBuilder sb = new StringBuilder("SELECT ", 164);
        int flag = AppendReadColumns(sb, type);
        AppendTables(sb, flag);
        sb.Append(" WHERE ");

        ModerationEntryType[]? types = null;
        if (type != typeof(ModerationEntry))
            ModerationReflection.TypeInheritance.TryGetValue(type, out types);
        bool and = false;
        object[] args;
        int offset;
        int xtraLength = (types == null ? 0 : types.Length) + (conditionArgs == null || condition == null ? 0 : conditionArgs.Length);
        if (start.HasValue && end.HasValue)
        {
            offset = 2;
            args = new object[2 + xtraLength];
            args[0] = start.Value.UtcDateTime;
            args[1] = end.Value.UtcDateTime;
            sb.Append($" `main`.`{ColumnEntriesStartTimestamp}` >= @1 AND `main`.`{ColumnEntriesStartTimestamp}` <= @2");
            and = true;
        }
        else if (start.HasValue)
        {
            offset = 1;
            args = new object[1 + xtraLength];
            args[0] = start.Value.UtcDateTime;
            sb.Append($" `main`.`{ColumnEntriesStartTimestamp}` >= @1");
            and = true;
        }
        else if (end.HasValue)
        {
            offset = 1;
            args = new object[1 + xtraLength];
            args[0] = end.Value.UtcDateTime;
            sb.Append($" `main`.`{ColumnEntriesStartTimestamp}` <= @1");
            and = true;
        }
        else
        {
            offset = 0;
            args = new object[xtraLength];
        }
        
        if (types is { Length: > 0 })
        {
            for (int i = 0; i < types.Length; ++i)
                args[offset + i] = types[i].ToString();
            if (and)
                sb.Append(" AND");
            sb.Append($" `main`.`{ColumnEntriesType}` IN (");
            and = true;
            for (int i = 0; i < types.Length; ++i)
            {
                if (i != 0)
                    sb.Append(',');
                sb.Append('@').Append((i + offset).ToString(CultureInfo.InvariantCulture));
            }

            sb.Append(')');
        }

        if (types != null)
            offset += types.Length;
        if (conditionArgs != null && condition != null)
        {
            for (int i = 0; i < conditionArgs.Length; ++i)
            {
                args[offset + i] = conditionArgs[i];
                condition = Util.QuickFormat(condition, "@" + (offset + i).ToString(CultureInfo.InvariantCulture), i);
            }
        }

        if (condition != null)
            sb.Append((and ? " AND " : " ") + condition);

        if (orderBy != null)
            sb.Append(" ORDER BY " + orderBy);

        sb.Append(';');


        ArrayList entries = new ArrayList(4);
        await Sql.QueryAsync(sb.ToString(), args, reader =>
        {
            ModerationEntry? entry = ReadEntry(flag, reader);
            if (type.IsInstanceOfType(entry))
                entries.Add(entry);
        }, token).ConfigureAwait(false);

        Array rtn = entries.ToArray(type);

        // ReSharper disable once CoVariantArrayConversion
        await Fill((ModerationEntry[])rtn, null, token).ConfigureAwait(false);
        
        return rtn;
    }
    private async Task Fill(ModerationEntry?[] entries, BitArray? mask = null, CancellationToken token = default)
    {
        if (entries.Length == 0 || entries.Length == 1 && (entries[0] is null || mask is not null && !mask[0])) return;
        string inArg;
        if (entries.Length == 1)
            inArg = " = " + entries[0]!.Id.Key.ToString(CultureInfo.InvariantCulture);
        else
        {
            StringBuilder sb = new StringBuilder("IN (", entries.Length * 4 + 6);
            int ct = 0;
            for (int i = 0; i < entries.Length; ++i)
            {
                if (entries[i] is not { } e || mask is not null && !mask[i])
                    continue;
                ++ct;
                if (i != 0)
                    sb.Append(',');
                sb.Append(e.Id.Key.ToString(CultureInfo.InvariantCulture));
            }

            if (ct == 0)
                return;

            inArg = sb.Append(')').ToString();
        }

        bool anyPunishments = false;
        bool anyDurationPunishments = false;
        for (int i = 0; i < entries.Length; ++i)
        {
            if (entries[i] is Punishment p && (mask is null || mask[i]))
            {
                anyPunishments = true;
                if (p is DurationPunishment)
                {
                    anyDurationPunishments = true;
                    break;
                }
            }
        }
        string query = $"SELECT {SqlTypes.ColumnList(ColumnExternalPrimaryKey, ColumnActorsId, ColumnActorsRole, ColumnActorsAsAdmin, ColumnActorsIndex)} " +
                $"FROM `{TableActors}` WHERE `{ColumnExternalPrimaryKey}` {inArg};";

        List<(int, List<RelatedActor>)> actors = new List<(int, List<RelatedActor>)>();
        await Sql.QueryAsync(query, null, reader =>
        {
            int pk = reader.GetInt32(0);
            int index = reader.GetInt32(4);
            int arrIndex = actors.FindIndex(x => x.Item1 == pk);
            RelatedActor actor = ReadActor(reader, 1);
            List<RelatedActor> list;
            if (arrIndex == -1)
                actors.Add((pk, list = new List<RelatedActor>()));
            else list = actors[arrIndex].Item2;

            if (actors.Count <= index)
                list.Add(actor);
            else
                list.Insert(index, actor);
        }, token).ConfigureAwait(false);

        for (int i = 0; i < actors.Count; ++i)
        {
            int pk = actors[i].Item1;
            for (int j = 0; j < entries.Length; ++j)
            {
                if (entries[j] is { } e && (mask is null || mask[i]) && e.Id.Key == pk)
                {
                    e.Actors = actors[i].Item2.AsArrayFast();
                    break;
                }
            }
        }

        query = $"SELECT {SqlTypes.ColumnList(ColumnExternalPrimaryKey, ColumnEvidenceId, ColumnEvidenceLink,
            ColumnEvidenceLocalSource, ColumnEvidenceMessage, ColumnEvidenceIsImage,
            ColumnEvidenceTimestamp, ColumnEvidenceActorId)} " +
                $"FROM `{TableEvidence}` WHERE `{ColumnExternalPrimaryKey}` {inArg};";

        List<(int, List<Evidence>)> evidence = new List<(int, List<Evidence>)>();
        await Sql.QueryAsync(query, null, reader =>
        {
            int pk = reader.GetInt32(0);
            int arrIndex = evidence.FindIndex(x => x.Item1 == pk);
            Evidence piece = ReadEvidence(reader, 1);
            List<Evidence> list;
            if (arrIndex == -1)
                evidence.Add((pk, list = new List<Evidence>()));
            else list = evidence[arrIndex].Item2;
            list.Add(piece);
        }, token).ConfigureAwait(false);

        for (int i = 0; i < evidence.Count; ++i)
        {
            int pk = evidence[i].Item1;
            for (int j = 0; j < entries.Length; ++j)
            {
                if (entries[j] is { } e && (mask is null || mask[i]) && e.Id.Key == pk)
                {
                    e.Evidence = evidence[i].Item2.AsArrayFast();
                    break;
                }
            }
        }

        if (anyDurationPunishments)
        {
            query = $"SELECT {SqlTypes.ColumnList(ColumnExternalPrimaryKey, ColumnDuationsDurationSeconds)} FROM `{TableDurationPunishments}` WHERE `{ColumnExternalPrimaryKey}` {inArg};";
            await Sql.QueryAsync(query, null, reader =>
            {
                int pk = reader.GetInt32(0);
                long sec = reader.GetInt64(1);
                if (sec < 0)
                    sec = -1;
                for (int i = 0; i < entries.Length; ++i)
                {
                    if (entries[i] is DurationPunishment e && (mask is null || mask[i]) && e.Id.Key == pk)
                    {
                        e.Duration = TimeSpan.FromSeconds(sec);
                        break;
                    }
                }
            }, token).ConfigureAwait(false);
        }

        if (anyPunishments)
        {
            List<(int, List<PrimaryKey>)> links = new List<(int, List<PrimaryKey>)>();

            query = $"SELECT {SqlTypes.ColumnList(ColumnExternalPrimaryKey, ColumnLinkedAppealsAppeal)} FROM `{TableLinkedAppeals}` WHERE `{ColumnExternalPrimaryKey}` {inArg};";
            await Sql.QueryAsync(query, null, reader =>
            {
                int pk = reader.GetInt32(0);
                int arrIndex = links.FindIndex(x => x.Item1 == pk);
                PrimaryKey val = reader.GetInt32(1);
                List<PrimaryKey> list;
                if (arrIndex == -1)
                    links.Add((pk, list = new List<PrimaryKey>()));
                else list = links[arrIndex].Item2;
                list.Add(val);
            }, token).ConfigureAwait(false);
            for (int i = 0; i < links.Count; ++i)
            {
                int pk = links[i].Item1;
                List<PrimaryKey> vals = links[i].Item2;
                for (int j = 0; j < entries.Length; ++j)
                {
                    if (entries[j] is Punishment p && (mask is null || mask[i]) && p.Id.Key == pk)
                    {
                        p.AppealKeys = vals.AsArrayFast();
                        break;
                    }
                }
                vals.Clear();
            }

            query = $"SELECT {SqlTypes.ColumnList(ColumnExternalPrimaryKey, ColumnLinkedReportsReport)} FROM `{TableLinkedReports}` WHERE `{ColumnExternalPrimaryKey}` {inArg};";
            await Sql.QueryAsync(query, null, reader =>
            {
                int pk = reader.GetInt32(0);
                int arrIndex = links.FindIndex(x => x.Item1 == pk);
                PrimaryKey val = reader.GetInt32(1);
                List<PrimaryKey> list;
                if (arrIndex == -1)
                    links.Add((pk, list = new List<PrimaryKey>()));
                else list = links[arrIndex].Item2;
                list.Add(val);
            }, token).ConfigureAwait(false);
            for (int i = 0; i < links.Count; ++i)
            {
                int pk = links[i].Item1;
                List<PrimaryKey> vals = links[i].Item2;
                for (int j = 0; j < entries.Length; ++j)
                {
                    if (entries[j] is Punishment p && (mask is null || mask[i]) && p.Id.Key == pk)
                    {
                        p.ReportKeys = vals.AsArrayFast();
                        break;
                    }
                }
            }
        }

        for (int i = 0; i < entries.Length; ++i)
        {
            if (entries[i] is not { } e || mask is not null && !mask[i]) continue;
            Cache.AddOrUpdate(e);
            await e.FillDetail(this, token).ConfigureAwait(false);
        }
    }
    private static int AppendReadColumns(StringBuilder sb, Type type)
    {
        sb.Append(SqlTypes.ColumnListAliased("main", ColumnEntriesPrimaryKey,
            ColumnEntriesType, ColumnEntriesSteam64,
            ColumnEntriesMessage,
            ColumnEntriesIsLegacy, ColumnEntriesStartTimestamp,
            ColumnEntriesResolvedTimestamp, ColumnEntriesReputation,
            ColumnEntriesReputationApplied, ColumnEntriesLegacyId,
            ColumnEntriesRelavantLogsStartTimestamp,
            ColumnEntriesRelavantLogsEndTimestamp,
            ColumnEntriesRemoved, ColumnEntriesRemovedBy,
            ColumnEntriesRemovedTimestamp, ColumnEntriesRemovedReason));
        int flag = 0;
        if (type.IsAssignableFrom(typeof(DurationPunishment)) || typeof(DurationPunishment).IsAssignableFrom(type))
        {
            flag |= 1;
            sb.Append(",`dur`.`" + ColumnDuationsDurationSeconds + "`");
        }
        if (type.IsAssignableFrom(typeof(Mute)) || typeof(Mute).IsAssignableFrom(type))
        {
            flag |= 1 << 1;
            sb.Append(",`mutes`.`" + ColumnMutesType + "`");
        }
        if (type.IsAssignableFrom(typeof(Warning)) || typeof(Warning).IsAssignableFrom(type))
        {
            flag |= 1 << 2;
            sb.Append(",`warns`.`" + ColumnWarningsHasBeenDisplayed + "`");
        }
        if (type.IsAssignableFrom(typeof(PlayerReportAccepted)) || typeof(PlayerReportAccepted).IsAssignableFrom(type))
        {
            flag |= 1 << 3;
            sb.Append(",`praccept`.`" + ColumnPlayerReportAcceptedsReport + "`");
        }
        if (type.IsAssignableFrom(typeof(BugReportAccepted)) || typeof(BugReportAccepted).IsAssignableFrom(type))
        {
            flag |= 1 << 4;
            sb.Append("," + SqlTypes.ColumnListAliased("braccept", ColumnTableBugReportAcceptedsIssue, ColumnTableBugReportAcceptedsCommit));
        }
        if (type.IsAssignableFrom(typeof(Teamkill)) || typeof(Teamkill).IsAssignableFrom(type))
        {
            flag |= 1 << 5;
            sb.Append("," + SqlTypes.ColumnListAliased("tks", ColumnTeamkillsAsset, ColumnTeamkillsAssetName, ColumnTeamkillsDeathCause, ColumnTeamkillsDeathMessage, ColumnTeamkillsDistance, ColumnTeamkillsLimb));
        }
        if (type.IsAssignableFrom(typeof(VehicleTeamkill)) || typeof(VehicleTeamkill).IsAssignableFrom(type))
        {
            flag |= 1 << 6;
            sb.Append("," + SqlTypes.ColumnListAliased("vtks", ColumnVehicleTeamkillsAsset, ColumnVehicleTeamkillsAssetName,
                ColumnVehicleTeamkillsDamageOrigin, ColumnVehicleTeamkillsDeathMessage, ColumnVehicleTeamkillsVehicleAsset, ColumnVehicleTeamkillsVehicleAssetName));
        }
        if (type.IsAssignableFrom(typeof(Appeal)) || typeof(Appeal).IsAssignableFrom(type))
        {
            flag |= 1 << 7;
            sb.Append("," + SqlTypes.ColumnListAliased("app", ColumnAppealsState, ColumnAppealsDiscordId, ColumnAppealsTicketId));
        }
        if (type.IsAssignableFrom(typeof(Report)) || typeof(Report).IsAssignableFrom(type))
        {
            flag |= 1 << 8;
            sb.Append(",`rep`.`" + ColumnReportsType + "`");
        }

        return flag;
    }
    private static void AppendTables(StringBuilder sb, int flag)
    {
        sb.Append($" FROM `{TableEntries}` AS `main`");

        if ((flag & 1) != 0)
        {
            sb.Append($" INNER JOIN `{TableDurationPunishments}` AS `dur` ON `main`.`{ColumnEntriesPrimaryKey}` = `dur`.`{ColumnExternalPrimaryKey}`");
        }
        if ((flag & (1 << 1)) != 0)
        {
            sb.Append($" INNER JOIN `{TableMutes}` AS `mutes` ON `main`.`{ColumnEntriesPrimaryKey}` = `mutes`.`{ColumnExternalPrimaryKey}`");
        }
        if ((flag & (1 << 2)) != 0)
        {
            sb.Append($" INNER JOIN `{TableWarnings}` AS `warns` ON `main`.`{ColumnEntriesPrimaryKey}` = `warns`.`{ColumnExternalPrimaryKey}`");
        }
        if ((flag & (1 << 3)) != 0)
        {
            sb.Append($" INNER JOIN `{TablePlayerReportAccepteds}` AS `praccept` ON `main`.`{ColumnEntriesPrimaryKey}` = `praccept`.`{ColumnExternalPrimaryKey}`");
        }
        if ((flag & (1 << 4)) != 0)
        {
            sb.Append($" INNER JOIN `{TableBugReportAccepteds}` AS `braccept` ON `main`.`{ColumnEntriesPrimaryKey}` = `braccept`.`{ColumnExternalPrimaryKey}`");
        }
        if ((flag & (1 << 5)) != 0)
        {
            sb.Append($" INNER JOIN `{TableTeamkills}` AS `tks` ON `main`.`{ColumnEntriesPrimaryKey}` = `tks`.`{ColumnExternalPrimaryKey}`");
        }
        if ((flag & (1 << 6)) != 0)
        {
            sb.Append($" INNER JOIN `{TableVehicleTeamkills}` AS `vtks` ON `main`.`{ColumnEntriesPrimaryKey}` = `vtks`.`{ColumnExternalPrimaryKey}`");
        }
        if ((flag & (1 << 7)) != 0)
        {
            sb.Append($" INNER JOIN `{TableAppeals}` AS `app` ON `main`.`{ColumnEntriesPrimaryKey}` = `app`.`{ColumnExternalPrimaryKey}`");
        }
        if ((flag & (1 << 8)) != 0)
        {
            sb.Append($" INNER JOIN `{TableReports}` AS `rep` ON `main`.`{ColumnEntriesPrimaryKey}` = `rep`.`{ColumnExternalPrimaryKey}`");
        }
    }
    private static ModerationEntry? ReadEntry(int flag, MySqlDataReader reader)
    {
        ModerationEntryType? type = reader.ReadStringEnum<ModerationEntryType>(1);
        Type? csType = type.HasValue ? ModerationReflection.GetType(type.Value) : null;
        if (csType == null)
        {
            Logging.LogWarning($"Invalid type while reading moderation entry: {reader.GetString(1)}.");
            return null;
        }

        ModerationEntry entry = (ModerationEntry)Activator.CreateInstance(csType);
        entry.Id = reader.GetInt32(0);
        entry.Player = reader.GetUInt64(2);
        entry.Message = reader.IsDBNull(3) ? null : reader.GetString(3);
        entry.IsLegacy = reader.GetBoolean(4);
        entry.StartedTimestamp = DateTime.SpecifyKind(reader.GetDateTime(5), DateTimeKind.Utc);
        entry.ResolvedTimestamp = reader.IsDBNull(6) ? null : DateTime.SpecifyKind(reader.GetDateTime(6), DateTimeKind.Utc);
        entry.Reputation = reader.GetDouble(7);
        entry.ReputationApplied = reader.GetBoolean(8);
        entry.LegacyId = reader.IsDBNull(9) ? null : reader.GetUInt32(9);
        entry.RelevantLogsBegin = reader.IsDBNull(10) ? null : DateTime.SpecifyKind(reader.GetDateTime(10), DateTimeKind.Utc);
        entry.RelevantLogsEnd = reader.IsDBNull(11) ? null : DateTime.SpecifyKind(reader.GetDateTime(11), DateTimeKind.Utc);
        entry.Removed = !reader.IsDBNull(12) && reader.GetBoolean(12);
        entry.RemovedBy = reader.IsDBNull(13) ? null : Actors.GetActor(reader.GetUInt64(13));
        entry.RemovedTimestamp = reader.IsDBNull(14) ? null : DateTime.SpecifyKind(reader.GetDateTime(14), DateTimeKind.Utc);
        entry.Message = reader.IsDBNull(15) ? null : reader.GetString(15);
        
        int offset = 15;
        if ((flag & 1) != 0)
        {
            offset++;
            if (entry is DurationPunishment dur)
            {
                long sec = reader.IsDBNull(offset) ? -1L : reader.GetInt64(offset);
                if (sec < -1)
                    sec = -1;
                dur.Duration = TimeSpan.FromSeconds(sec);
            }
        }
        if ((flag & (1 << 1)) != 0)
        {
            offset++;
            if (entry is Mute mute)
            {
                MuteType? sec = reader.IsDBNull(offset) ? null : reader.ReadStringEnum<MuteType>(offset);
                mute.Type = sec ?? MuteType.None;
            }
        }
        if ((flag & (1 << 2)) != 0)
        {
            offset++;
            if (entry is Warning warning)
            {
                warning.HasBeenDisplayed = !reader.IsDBNull(offset) && reader.GetBoolean(offset);
            }
        }
        if ((flag & (1 << 3)) != 0)
        {
            offset++;
            if (entry is PlayerReportAccepted praccept)
            {
                praccept.ReportKey = reader.IsDBNull(offset) ? PrimaryKey.NotAssigned : reader.GetInt32(offset);
            }
        }
        if ((flag & (1 << 4)) != 0)
        {
            offset += 2;
            if (entry is BugReportAccepted braccept)
            {
                braccept.Issue = reader.IsDBNull(offset - 1) ? null : reader.GetInt32(offset - 1);
                braccept.Commit = reader.IsDBNull(offset) ? null : reader.GetString(offset);
            }
        }
        if ((flag & (1 << 5)) != 0)
        {
            offset += 6;
            if (entry is Teamkill tk)
            {
                tk.Item = reader.IsDBNull(offset - 5) ? null : reader.ReadGuidString(offset - 5);
                tk.ItemName = reader.IsDBNull(offset - 4) ? null : reader.GetString(offset - 4);
                tk.Cause = reader.IsDBNull(offset - 3) ? null : reader.ReadStringEnum<EDeathCause>(offset - 3);
                tk.DeathMessage = reader.IsDBNull(offset - 2) ? null : reader.GetString(offset - 2);
                tk.Distance = reader.IsDBNull(offset - 1) ? null : reader.GetDouble(offset - 1);
                tk.Limb = reader.IsDBNull(offset) ? null : reader.ReadStringEnum<ELimb>(offset);
            }
        }
        if ((flag & (1 << 6)) != 0)
        {
            offset += 6;
            if (entry is VehicleTeamkill tk)
            {
                tk.Item = reader.IsDBNull(offset - 5) ? null : reader.ReadGuidString(offset - 5);
                tk.ItemName = reader.IsDBNull(offset - 4) ? null : reader.GetString(offset - 4);
                tk.Origin = reader.IsDBNull(offset - 3) ? null : reader.ReadStringEnum<EDamageOrigin>(offset - 3);
                tk.DeathMessage = reader.IsDBNull(offset - 2) ? null : reader.GetString(offset - 2);
                tk.Vehicle = reader.IsDBNull(offset - 1) ? null : reader.ReadGuidString(offset - 1);
                tk.VehicleName = reader.IsDBNull(offset) ? null : reader.GetString(offset);
            }
        }
        if ((flag & (1 << 7)) != 0)
        {
            offset += 3;
            if (entry is Appeal app)
            {
                app.AppealState = !reader.IsDBNull(offset - 2) && reader.GetBoolean(offset - 2);
                app.DiscordUserId = reader.IsDBNull(offset - 1) ? null : reader.GetUInt64(offset - 1);
                app.TicketId = reader.IsDBNull(offset) ? Guid.Empty : (reader.ReadGuidString(offset) ?? Guid.Empty);
            }
        }
        if ((flag & (1 << 8)) != 0)
        {
            offset++;
            if (entry is Report rep)
            {
                ReportType? sec = reader.IsDBNull(offset) ? null : reader.ReadStringEnum<ReportType>(offset);
                rep.Type = sec ?? ReportType.Custom;
            }
        }

        return entry;
    }
    private static Evidence ReadEvidence(MySqlDataReader reader, int offset)
    {
        return new Evidence(
            reader.IsDBNull(offset) ? PrimaryKey.NotAssigned : reader.GetInt32(offset),
            reader.GetString(1 + offset),
            reader.IsDBNull(3 + offset) ? null : reader.GetString(3 + offset),
            reader.IsDBNull(2 + offset) ? null : reader.GetString(2 + offset),
            reader.GetBoolean(4 + offset),
            Actors.GetActor(reader.GetUInt64(6 + offset)),
            DateTime.SpecifyKind(reader.GetDateTime(5 + offset), DateTimeKind.Utc));
    }
    private static RelatedActor ReadActor(MySqlDataReader reader, int offset)
    {
        return new RelatedActor(
            reader.GetString(1 + offset),
            reader.GetBoolean(2 + offset),
            Actors.GetActor(reader.GetUInt64(offset)));
    }
    public async Task AddOrUpdate(ModerationEntry entry, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();

        PrimaryKey pk = entry.Id;
        object[] objs = new object[pk.IsValid ? 16 : 15];
        objs[0] = (ModerationReflection.GetType(entry.GetType()) ?? ModerationEntryType.None).ToString();
        objs[1] = entry.Player;
        objs[2] = (object?)entry.Message.MaxLength(1024) ?? DBNull.Value;
        objs[3] = entry.IsLegacy;
        objs[4] = entry.StartedTimestamp.UtcDateTime;
        objs[5] = entry.ResolvedTimestamp.HasValue ? entry.ResolvedTimestamp.Value.UtcDateTime : DBNull.Value;
        objs[6] = entry.Reputation;
        objs[7] = entry.ReputationApplied;
        objs[8] = entry.LegacyId.HasValue ? entry.LegacyId.Value : DBNull.Value;
        objs[9] = entry.RelevantLogsBegin.HasValue ? entry.RelevantLogsBegin.Value.UtcDateTime : DBNull.Value;
        objs[10] = entry.RelevantLogsEnd.HasValue ? entry.RelevantLogsEnd.Value.UtcDateTime : DBNull.Value;
        objs[11] = entry.Removed;
        objs[12] = entry.RemovedBy == null ? DBNull.Value : entry.RemovedBy.Id;
        objs[13] = entry.RemovedTimestamp.HasValue ? entry.RemovedTimestamp.Value.UtcDateTime : DBNull.Value;
        objs[14] = (object?)entry.RemovedMessage ?? DBNull.Value;

        if (pk.IsValid)
            objs[15] = pk.Key;

        string query = F.BuildInitialInsertQuery(TableEntries, ColumnEntriesPrimaryKey, pk.IsValid, null, null,

            ColumnEntriesType, ColumnEntriesSteam64, ColumnEntriesMessage,
            ColumnEntriesIsLegacy, ColumnEntriesStartTimestamp, ColumnEntriesResolvedTimestamp, ColumnEntriesReputation,
            ColumnEntriesReputationApplied, ColumnEntriesLegacyId,
            ColumnEntriesRelavantLogsStartTimestamp, ColumnEntriesRelavantLogsEndTimestamp,
            ColumnEntriesRemoved, ColumnEntriesRemovedBy, ColumnEntriesRemovedTimestamp, ColumnEntriesRemovedReason);

        await Sql.QueryAsync(query, objs, reader =>
        {
            pk = reader.GetInt32(0);
        }, token).ConfigureAwait(false);

        if (pk.IsValid)
            entry.Id = pk;

        List<object> args = new List<object>(entry.EstimateColumnCount()) { pk.Key };

        StringBuilder builder = new StringBuilder(82);

        bool hasNewEvidence = entry.AppendWriteCall(builder, args);

        if (!hasNewEvidence)
        {
            await Sql.NonQueryAsync(builder.ToString(), args.ToArray(), token).ConfigureAwait(false);
        }
        else
        {
            await Sql.QueryAsync(builder.ToString(), args.ToArray(), reader =>
            {
                Evidence read = ReadEvidence(reader, 0);
                for (int i = 0; i < entry.Evidence.Length; ++i)
                {
                    ref Evidence existing = ref entry.Evidence[i];
                    if (existing.Id.IsValid)
                    {
                        if (read.Id.Key == existing.Id.Key)
                        {
                            existing = read;
                            return;
                        }
                    }
                    else if (string.Equals(existing.URL, read.URL, StringComparison.OrdinalIgnoreCase) && existing.Timestamp == read.Timestamp)
                    {
                        existing = read;
                    }
                }
            }, token).ConfigureAwait(false);
        }

        Cache.AddOrUpdate(entry);
    }

    public const string TableEntries = "moderation_entries";
    public const string TableActors = "moderation_actors";
    public const string TableEvidence = "moderation_evidence";
    public const string TableAssetBanFilters = "moderation_asset_ban_filters";
    public const string TableDurationPunishments = "moderation_durations";
    public const string TableLinkedAppeals = "moderation_linked_appeals";
    public const string TableLinkedReports = "moderation_linked_reports";
    public const string TableMutes = "moderation_mutes";
    public const string TableWarnings = "moderation_warnings";
    public const string TablePlayerReportAccepteds = "moderation_accepted_player_reports";
    public const string TableBugReportAccepteds = "moderation_accepted_bug_reports";
    public const string TableTeamkills = "moderation_teamkills";
    public const string TableVehicleTeamkills = "moderation_vehicle_teamkills";
    public const string TableAppeals = "moderation_appeals";
    public const string TableAppealPunishments = "moderation_appeal_punishments";
    public const string TableAppealResponses = "moderation_appeal_responses";
    public const string TableReports = "moderation_reports";
    public const string TableReportChatRecords = "moderation_report_chat_records";
    public const string TableReportStructureDamageRecords = "moderation_report_struct_dmg_records";
    public const string TableReportVehicleRequestRecords = "moderation_report_veh_req_records";
    public const string TableReportTeamkillRecords = "moderation_report_tk_records";
    public const string TableReportVehicleTeamkillRecords = "moderation_report_veh_tk_records";

    public const string ColumnExternalPrimaryKey = "Entry";
    
    public const string ColumnEntriesPrimaryKey = "Id";
    public const string ColumnEntriesType = "Type";
    public const string ColumnEntriesSteam64 = "Steam64";
    public const string ColumnEntriesMessage = "Message";
    public const string ColumnEntriesIsLegacy = "IsLegacy";
    public const string ColumnEntriesStartTimestamp = "StartTimeUTC";
    public const string ColumnEntriesResolvedTimestamp = "ResolvedTimeUTC";
    public const string ColumnEntriesReputation = "Reputation";
    public const string ColumnEntriesReputationApplied = "ReputationApplied";
    public const string ColumnEntriesLegacyId = "LegacyId";
    public const string ColumnEntriesMessageId = "OffenseMessageId";
    public const string ColumnEntriesInvalidated = "Invalidated";
    public const string ColumnEntriesInvalidatedActor = "InvalidatedActor";
    public const string ColumnEntriesInvalidatedTimestamp = "InvalidatedTimestamp";
    public const string ColumnEntriesRelavantLogsStartTimestamp = "RelavantLogsStartTimeUTC";
    public const string ColumnEntriesRelavantLogsEndTimestamp = "RelavantLogsEndTimeUTC";
    public const string ColumnEntriesRemoved = "Removed";
    public const string ColumnEntriesRemovedBy = "RemovedBy";
    public const string ColumnEntriesRemovedTimestamp = "RemovedTimeUTC";
    public const string ColumnEntriesRemovedReason = "RemovedReason";

    public const string ColumnActorsId = "ActorId";
    public const string ColumnActorsRole = "ActorRole";
    public const string ColumnActorsAsAdmin = "ActorAsAdmin";
    public const string ColumnActorsIndex = "ActorIndex";

    public const string ColumnEvidenceId = "Id";
    public const string ColumnEvidenceLink = "EvidenceURL";
    public const string ColumnEvidenceLocalSource = "EvidenceLocalSource";
    public const string ColumnEvidenceMessage = "EvidenceMessage";
    public const string ColumnEvidenceIsImage = "EvidenceIsImage";
    public const string ColumnEvidenceActorId = "EvidenceActor";
    public const string ColumnEvidenceTimestamp = "EvidenceTimestampUTC";

    public const string ColumnAssetBanFiltersAsset = "AssetBanAsset";

    public const string ColumnDuationsDurationSeconds = "Duration";

    public const string ColumnLinkedAppealsAppeal = "LinkedAppeal";
    public const string ColumnLinkedReportsReport = "LinkedReport";

    public const string ColumnMutesType = "MuteType";

    public const string ColumnWarningsHasBeenDisplayed = "WarningHasBeenDisplayed";

    public const string ColumnPlayerReportAcceptedsReport = "AcceptedReport";

    public const string ColumnTableBugReportAcceptedsCommit = "AcceptedCommit";
    public const string ColumnTableBugReportAcceptedsIssue = "AcceptedIssue";

    public const string ColumnTeamkillsDeathCause = "DeathCause";
    public const string ColumnTeamkillsAsset = "Asset";
    public const string ColumnTeamkillsAssetName = "AssetName";
    public const string ColumnTeamkillsLimb = "Limb";
    public const string ColumnTeamkillsDistance = "Distance";
    public const string ColumnTeamkillsDeathMessage = "DeathMessage";

    public const string ColumnVehicleTeamkillsDamageOrigin = "DamageOrigin";
    public const string ColumnVehicleTeamkillsVehicleAsset = "VehicleAsset";
    public const string ColumnVehicleTeamkillsVehicleAssetName = "VehicleAssetName";
    public const string ColumnVehicleTeamkillsAsset = "Asset";
    public const string ColumnVehicleTeamkillsAssetName = "AssetName";
    public const string ColumnVehicleTeamkillsDeathMessage = "DeathMessage";

    public const string ColumnAppealsTicketId = "TicketId";
    public const string ColumnAppealsState = "State";
    public const string ColumnAppealsDiscordId = "DiscordId";

    public const string ColumnAppealPunishmentsPunishment = "Punishment";

    public const string ColumnAppealResponsesQuestion = "Question";
    public const string ColumnAppealResponsesResponse = "Response";

    public const string ColumnReportsType = "Type";

    public const string ColumnReportsChatRecordsTimestamp = "TimeUTC";
    public const string ColumnReportsChatRecordsCount = "Count";
    public const string ColumnReportsChatRecordsMessage = "Message";

    public const string ColumnReportsStructureDamageStructure = "Structure";
    public const string ColumnReportsStructureDamageStructureName = "StructureName";
    public const string ColumnReportsStructureDamageStructureOwner = "StructureOwner";
    public const string ColumnReportsStructureDamageStructureType = "StructureType";
    public const string ColumnReportsStructureDamageDamageOrigin = "DamageOrigin";
    public const string ColumnReportsStructureDamageInstanceId = "InstanceId";
    public const string ColumnReportsStructureDamageDamage = "Damage";
    public const string ColumnReportsStructureDamageWasDestroyed = "WasDestroyed";

    public const string ColumnReportsTeamkillRecordTeamkill = "Teamkill";
    public const string ColumnReportsTeamkillRecordVictim = "Victim";
    public const string ColumnReportsTeamkillRecordDeathCause = "DeathCause";
    public const string ColumnReportsTeamkillRecordMessage = "Message";
    public const string ColumnReportsTeamkillRecordWasIntentional = "WasIntentional";

    public const string ColumnReportsVehicleTeamkillRecordTeamkill = "Teamkill";
    public const string ColumnReportsVehicleTeamkillRecordVictim = "Victim";
    public const string ColumnReportsVehicleTeamkillRecordDamageOrigin = "DamageOrigin";
    public const string ColumnReportsVehicleTeamkillRecordMessage = "Message";

    public const string ColumnReportsVehicleRequestRecordAsset = "Asset";
    public const string ColumnReportsVehicleRequestRecordVehicle = "Vehicle";
    public const string ColumnReportsVehicleRequestRecordVehicleName = "VehicleName";
    public const string ColumnReportsVehicleRequestRecordRequestTimestamp = "RequestTimeUTC";
    public const string ColumnReportsVehicleRequestRecordDestroyTimestamp = "DestroyTimeUTC";
    public const string ColumnReportsVehicleRequestRecordDamageOrigin = "DamageOrigin";
    public const string ColumnReportsVehicleRequestRecordInstigator = "DamageInstigator";

    public static Schema[] Schema =
    {
        new Schema(TableEntries, new Schema.Column[]
        {
            new Schema.Column(ColumnEntriesPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true
            },
            new Schema.Column(ColumnEntriesType, SqlTypes.Enum(exclude: ModerationEntryType.None)),
            new Schema.Column(ColumnEntriesSteam64, SqlTypes.STEAM_64)
            {
                Indexed = true
            },
            new Schema.Column(ColumnEntriesMessage, SqlTypes.String(1024))
            {
                Nullable = true
            },
            new Schema.Column(ColumnEntriesIsLegacy, SqlTypes.BOOLEAN)
            {
                Default = "b'0'"
            },
            new Schema.Column(ColumnEntriesLegacyId, SqlTypes.UINT)
            {
                Nullable = true
            },
            new Schema.Column(ColumnEntriesStartTimestamp, SqlTypes.DATETIME),
            new Schema.Column(ColumnEntriesResolvedTimestamp, SqlTypes.DATETIME)
            {
                Nullable = true
            },
            new Schema.Column(ColumnEntriesReputationApplied, SqlTypes.BOOLEAN)
            {
                Nullable = true,
                Default = "b'0'"
            },
            new Schema.Column(ColumnEntriesReputation, SqlTypes.DOUBLE)
            {
                Default = "'0'"
            },
            new Schema.Column(ColumnEntriesRelavantLogsStartTimestamp, SqlTypes.DATETIME)
            {
                Nullable = true
            },
            new Schema.Column(ColumnEntriesRelavantLogsEndTimestamp, SqlTypes.DATETIME)
            {
                Nullable = true
            },
            new Schema.Column(ColumnEntriesInvalidated, SqlTypes.BOOLEAN)
            {
                Default = "b'0'"
            },
            new Schema.Column(ColumnEntriesInvalidatedActor, SqlTypes.STEAM_64)
            {
                Nullable = true
            },
            new Schema.Column(ColumnEntriesInvalidatedTimestamp, SqlTypes.DATETIME)
            {
                Nullable = true
            },
            new Schema.Column(ColumnEntriesRemoved, SqlTypes.BOOLEAN)
            {
                Default = "b'0'"
            },
            new Schema.Column(ColumnEntriesRemovedBy, SqlTypes.STEAM_64)
            {
                Nullable = true
            },
            new Schema.Column(ColumnEntriesRemovedTimestamp, SqlTypes.DATETIME)
            {
                Nullable = true
            },
            new Schema.Column(ColumnEntriesRemovedReason, SqlTypes.String(1024))
            {
                Nullable = true
            }
        }, true, typeof(ModerationEntry)),
        new Schema(TableActors, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnActorsRole, SqlTypes.STRING_255),
            new Schema.Column(ColumnActorsId, SqlTypes.STEAM_64),
            new Schema.Column(ColumnActorsAsAdmin, SqlTypes.BOOLEAN),
            new Schema.Column(ColumnActorsIndex, SqlTypes.INT)
        }, true, typeof(RelatedActor)),
        new Schema(TableEvidence, new Schema.Column[]
        {
            new Schema.Column(ColumnEvidenceId, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true
            },
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries,
                ForeignKeyDeleteBehavior = ConstraintBehavior.SetNull,
                Nullable = true
            },
            new Schema.Column(ColumnEvidenceLink, SqlTypes.String(512)),
            new Schema.Column(ColumnEvidenceLocalSource, SqlTypes.String(512))
            {
                Nullable = true
            },
            new Schema.Column(ColumnEvidenceIsImage, SqlTypes.BOOLEAN),
            new Schema.Column(ColumnEvidenceTimestamp, SqlTypes.DATETIME),
            new Schema.Column(ColumnEvidenceActorId, SqlTypes.STEAM_64)
            {
                Nullable = true
            },
            new Schema.Column(ColumnEvidenceMessage, SqlTypes.String(1024))
            {
                Nullable = true
            }

        }, false, typeof(Evidence)),
        new Schema(TableAssetBanFilters, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnAssetBanFiltersAsset, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyTable = VehicleBay.TABLE_MAIN,
                ForeignKeyColumn = VehicleBay.COLUMN_PK
            }
        }, false, typeof(VehicleData)),
        new Schema(TableDurationPunishments, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true,
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnDuationsDurationSeconds, SqlTypes.LONG)
        }, false, typeof(DurationPunishment)),
        new Schema(TableLinkedAppeals, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnLinkedAppealsAppeal, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            }
        }, false, typeof(Appeal)),
        new Schema(TableLinkedReports, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnLinkedReportsReport, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            }
        }, false, typeof(Report)),
        new Schema(TableMutes, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true,
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnMutesType, SqlTypes.Enum(exclude: MuteType.None))
        }, false, typeof(Mute)),
        new Schema(TableWarnings, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true,
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnWarningsHasBeenDisplayed, SqlTypes.BOOLEAN)
            {
                Default = "b'0'"
            }
        }, false, typeof(Warning)),
        new Schema(TablePlayerReportAccepteds, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true,
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnPlayerReportAcceptedsReport, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries,
                Nullable = true
            }
        }, false, typeof(PlayerReportAccepted)),
        new Schema(TableBugReportAccepteds, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true,
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnTableBugReportAcceptedsCommit, "char(7)")
            {
                Nullable = true
            },
            new Schema.Column(ColumnTableBugReportAcceptedsIssue, SqlTypes.INT)
            {
                Nullable = true
            }
        }, false, typeof(PlayerReportAccepted)),
        new Schema(TableTeamkills, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true,
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnTeamkillsAsset, SqlTypes.GUID_STRING)
            {
                Nullable = true
            },
            new Schema.Column(ColumnTeamkillsAssetName, SqlTypes.String(48))
            {
                Nullable = true
            },
            new Schema.Column(ColumnTeamkillsDeathCause, SqlTypes.Enum<EDeathCause>())
            {
                Nullable = true
            },
            new Schema.Column(ColumnTeamkillsDistance, SqlTypes.FLOAT)
            {
                Nullable = true
            },
            new Schema.Column(ColumnTeamkillsLimb, SqlTypes.Enum<ELimb>())
            {
                Nullable = true
            },
            new Schema.Column(ColumnTeamkillsDeathMessage, SqlTypes.STRING_255)
            {
                Nullable = true
            }
        }, false, typeof(Teamkill)),
        new Schema(TableVehicleTeamkills, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true,
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnVehicleTeamkillsVehicleAsset, SqlTypes.GUID_STRING)
            {
                Nullable = true
            },
            new Schema.Column(ColumnVehicleTeamkillsVehicleAssetName, SqlTypes.String(48))
            {
                Nullable = true
            },
            new Schema.Column(ColumnVehicleTeamkillsAsset, SqlTypes.GUID_STRING)
            {
                Nullable = true
            },
            new Schema.Column(ColumnVehicleTeamkillsAssetName, SqlTypes.String(48))
            {
                Nullable = true
            },
            new Schema.Column(ColumnVehicleTeamkillsDamageOrigin, SqlTypes.Enum<EDamageOrigin>())
            {
                Nullable = true
            },
            new Schema.Column(ColumnVehicleTeamkillsDeathMessage, SqlTypes.STRING_255)
            {
                Nullable = true
            }
        }, false, typeof(VehicleTeamkill)),
        new Schema(TableAppeals, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true,
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnAppealsTicketId, SqlTypes.GUID_STRING),
            new Schema.Column(ColumnAppealsState, SqlTypes.BOOLEAN)
            {
                Nullable = true
            },
            new Schema.Column(ColumnAppealsDiscordId, SqlTypes.ULONG)
            {
                Nullable = true
            }
        }, false, typeof(Appeal)),
        new Schema(TableAppealPunishments, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnAppealPunishmentsPunishment, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            }
        }, false, typeof(Punishment)),
        new Schema(TableAppealResponses, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnAppealResponsesQuestion, SqlTypes.STRING_255),
            new Schema.Column(ColumnAppealResponsesResponse, SqlTypes.String(1024))
        }, false, typeof(AppealResponse)),
        new Schema(TableReports, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true,
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnReportsType, SqlTypes.Enum<ReportType>())
        }, false, typeof(Report)),
        new Schema(TableReportChatRecords, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnReportsChatRecordsMessage, SqlTypes.String(512)),
            new Schema.Column(ColumnReportsChatRecordsCount, SqlTypes.INT)
            {
                Default = "1"
            },
            new Schema.Column(ColumnReportsChatRecordsTimestamp, SqlTypes.DATETIME)
        }, false, typeof(ReportChatRecord)),
        new Schema(TableReportStructureDamageRecords, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnReportsStructureDamageStructure, SqlTypes.GUID_STRING),
            new Schema.Column(ColumnReportsStructureDamageStructureName, SqlTypes.String(48)),
            new Schema.Column(ColumnReportsStructureDamageStructureOwner, SqlTypes.STEAM_64),
            new Schema.Column(ColumnReportsStructureDamageStructureType, SqlTypes.Enum(StructType.Unknown)),
            new Schema.Column(ColumnReportsStructureDamageDamage, SqlTypes.INT),
            new Schema.Column(ColumnReportsStructureDamageDamageOrigin, SqlTypes.Enum<EDamageOrigin>()),
            new Schema.Column(ColumnReportsStructureDamageInstanceId, SqlTypes.INSTANCE_ID),
            new Schema.Column(ColumnReportsStructureDamageWasDestroyed, SqlTypes.BOOLEAN)
            {
                Default = "b'0'"
            }
        }, false, typeof(StructureDamageRecord)),
        new Schema(TableReportTeamkillRecords, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnReportsTeamkillRecordTeamkill, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries,
                ForeignKeyDeleteBehavior = ConstraintBehavior.SetNull,
                Nullable = true
            },
            new Schema.Column(ColumnReportsTeamkillRecordVictim, SqlTypes.STEAM_64),
            new Schema.Column(ColumnReportsTeamkillRecordDeathCause, SqlTypes.Enum<EDeathCause>()),
            new Schema.Column(ColumnReportsTeamkillRecordWasIntentional, SqlTypes.BOOLEAN)
            {
                Nullable = true
            },
            new Schema.Column(ColumnReportsTeamkillRecordMessage, SqlTypes.STRING_255)
            {
                Nullable = true
            }
        }, false, typeof(TeamkillRecord)),
        new Schema(TableReportVehicleTeamkillRecords, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnReportsVehicleTeamkillRecordTeamkill, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries,
                ForeignKeyDeleteBehavior = ConstraintBehavior.SetNull,
                Nullable = true
            },
            new Schema.Column(ColumnReportsVehicleTeamkillRecordVictim, SqlTypes.STEAM_64),
            new Schema.Column(ColumnReportsVehicleTeamkillRecordDamageOrigin, SqlTypes.Enum<EDamageOrigin>()),
            new Schema.Column(ColumnReportsVehicleTeamkillRecordMessage, SqlTypes.STRING_255)
            {
                Nullable = true
            }
        }, false, typeof(VehicleTeamkillRecord)),
        new Schema(TableReportVehicleRequestRecords, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnReportsVehicleRequestRecordAsset, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyTable = VehicleBay.TABLE_MAIN,
                ForeignKeyColumn = VehicleBay.COLUMN_PK,
                ForeignKeyDeleteBehavior = ConstraintBehavior.SetNull,
                Nullable = true
            },
            new Schema.Column(ColumnReportsVehicleRequestRecordVehicle, SqlTypes.GUID_STRING),
            new Schema.Column(ColumnReportsVehicleRequestRecordVehicleName, SqlTypes.String(48)),
            new Schema.Column(ColumnReportsVehicleRequestRecordDamageOrigin, SqlTypes.Enum<EDamageOrigin>()),
            new Schema.Column(ColumnReportsVehicleRequestRecordInstigator, SqlTypes.STEAM_64)
            {
                Nullable = true
            },
            new Schema.Column(ColumnReportsVehicleRequestRecordRequestTimestamp, SqlTypes.DATETIME),
            new Schema.Column(ColumnReportsVehicleRequestRecordDestroyTimestamp, SqlTypes.DATETIME)
            {
                Nullable = true
            }
        }, false, typeof(VehicleRequestRecord))
    };
}

public enum ActorRelationType
{
    IsTarget,
    IsAdminActor,
    IsNonAdminActor,
    IsActor
}

internal class WarfareDatabaseInterface : DatabaseInterface
{
    public override IWarfareSql Sql => Data.AdminSql;
}
using MySql.Data.MySqlClient;
using SDG.Unturned;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
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
using Uncreated.Warfare.Moderation.Punishments.Presets;
using Uncreated.Warfare.Moderation.Records;
using Uncreated.Warfare.Moderation.Reports;
using Uncreated.Warfare.Networking;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using ChatAbuseReport = Uncreated.Warfare.Moderation.Reports.ChatAbuseReport;
using CheatingReport = Uncreated.Warfare.Moderation.Reports.CheatingReport;
using Report = Uncreated.Warfare.Moderation.Reports.Report;

namespace Uncreated.Warfare.Moderation;
public abstract class DatabaseInterface
{
    public readonly TimeSpan DefaultInvalidateDuration = TimeSpan.FromSeconds(3);
    private readonly Dictionary<ulong, string> _iconUrlCacheSmall = new Dictionary<ulong, string>(128);
    private readonly Dictionary<ulong, string> _iconUrlCacheMedium = new Dictionary<ulong, string>(128);
    private readonly Dictionary<ulong, string> _iconUrlCacheFull = new Dictionary<ulong, string>(128);
    private readonly Dictionary<ulong, PlayerNames> _usernameCache = new Dictionary<ulong, PlayerNames>(128);

    private IIPAddressFilter[]? _nonRemotePlayAddressFilters;
    private IIPAddressFilter[]? _ipAddressFilters;
    private IPv4AddressRangeFilter[]? _remotePlayAddressFilters;

    private readonly string[] _columns =
    {
        ColumnEntriesPrimaryKey,
        ColumnEntriesType, ColumnEntriesSteam64,
        ColumnEntriesMessage,
        ColumnEntriesIsLegacy, ColumnEntriesStartTimestamp,
        ColumnEntriesResolvedTimestamp, ColumnEntriesReputation,
        ColumnEntriesPendingReputation, ColumnEntriesLegacyId,
        ColumnEntriesRelavantLogsStartTimestamp,
        ColumnEntriesRelavantLogsEndTimestamp,
        ColumnEntriesRemoved, ColumnEntriesRemovedBy,
        ColumnEntriesRemovedTimestamp, ColumnEntriesRemovedReason
    };

    public IIPAddressFilter[] NonRemotePlayFilters => _nonRemotePlayAddressFilters ??= new IIPAddressFilter[]
    {
        new MySqlAddressFilter(() => Sql)
    };
    public IIPAddressFilter[] IPAddressFilters => _ipAddressFilters ??= new IIPAddressFilter[]
    {
        IPv4AddressRangeFilter.GeforceNow,
        IPv4AddressRangeFilter.VKPlay,
        NonRemotePlayFilters[0]
    };
    public IPv4AddressRangeFilter[] RemotePlayAddressFilters => _remotePlayAddressFilters ??= new IPv4AddressRangeFilter[]
    {
        IPv4AddressRangeFilter.GeforceNow,
        IPv4AddressRangeFilter.VKPlay
    };

    public event Action<ModerationEntry>? OnNewModerationEntryAdded;
    public event Action<ModerationEntry>? OnModerationEntryUpdated;
    public ModerationCache Cache { get; } = new ModerationCache(64);
    public bool TryGetAvatar(IModerationActor actor, AvatarSize size, out string avatar)
    {
        if (!Util.IsValidSteam64Id(actor.Id))
        {
            avatar = null!;
            return false;
        }
        return TryGetAvatar(actor.Id, size, out avatar);
    }
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
    public bool TryGetUsernames(IModerationActor actor, out PlayerNames names)
    {
        if (!Util.IsValidSteam64Id(actor.Id))
        {
            names = default;
            return false;
        }
        return TryGetUsernames(actor.Id, out names);
    }
    public bool TryGetUsernames(ulong steam64, out PlayerNames names)
    {
        return _usernameCache.TryGetValue(steam64, out names);
    }
    public void UpdateUsernames(ulong steam64, PlayerNames names)
    {
        _usernameCache[steam64] = names;
    }
    public abstract IWarfareSql Sql { get; }
    public Task VerifyTables(CancellationToken token = default) => Sql.VerifyTables(Schema, token);
    public async Task<PlayerNames> GetUsernames(ulong id, bool useCache, CancellationToken token = default)
    {
        if (useCache && TryGetUsernames(id, out PlayerNames names))
            return names;

        if (UCWarfare.IsLoaded)
            return await F.GetPlayerOriginalNamesAsync(id, token).ConfigureAwait(false);

        names = await Sql.GetUsernamesAsync(id, token).ConfigureAwait(false);
        UpdateUsernames(id, names);
        return names;
    }
    public async Task<T?> ReadOne<T>(PrimaryKey id, bool tryGetFromCache, bool detail = true, bool baseOnly = false, CancellationToken token = default) where T : class, IModerationEntry
    {
        if (tryGetFromCache && Cache.TryGet(id, out T val, DefaultInvalidateDuration))
            return val;

        StringBuilder sb = new StringBuilder("SELECT ", 128);
        int flag = AppendReadColumns(sb, typeof(T), baseOnly);
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

        await Fill(new IModerationEntry[] { entry }, detail, baseOnly, null, token).ConfigureAwait(false);
        
        return entry as T;
    }
    public async Task<T?[]> ReadAll<T>(PrimaryKey[] ids, bool tryGetFromCache, bool detail = true, bool baseOnly = false, CancellationToken token = default) where T : class, IModerationEntry
    {
        T?[] result = new T?[ids.Length];
        await ReadAll(result, ids, tryGetFromCache, detail, baseOnly, token).ConfigureAwait(false);
        return result;
    }
    public async Task ReadAll<T>(T?[] result, PrimaryKey[] ids, bool tryGetFromCache, bool detail = true, bool baseOnly = false, CancellationToken token = default) where T : class, IModerationEntry
    {
        if (result.Length != ids.Length)
            throw new ArgumentException("Result must be the same length as ids.", nameof(result));

        StringBuilder sb = new StringBuilder("SELECT ", 164);
        int flag = AppendReadColumns(sb, typeof(T), baseOnly);
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
        await Fill(result, detail, baseOnly, mask, token).ConfigureAwait(false);
    }
    public async Task<T[]> ReadAll<T>(ulong actor, ActorRelationType relation, bool detail = true, bool baseOnly = false, DateTimeOffset? start = null, DateTimeOffset? end = null, string? condition = null, string? orderBy = null, object[]? conditionArgs = null, CancellationToken token = default) where T : IModerationEntry
        => (T[])await ReadAll(typeof(T), actor, relation, detail, baseOnly, start, end, condition, orderBy, conditionArgs, token).ConfigureAwait(false);
    public async Task<Array> ReadAll(Type type, ulong actor, ActorRelationType relation, bool detail = true, bool baseOnly = false, DateTimeOffset? start = null, DateTimeOffset? end = null, string? condition = null, string? orderBy = null, object[]? conditionArgs = null, CancellationToken token = default)
    {
        ModerationEntryType[]? types = null;
        if (type != typeof(ModerationEntry) && type != typeof(IModerationEntry))
            ModerationReflection.TryGetInheritance(type, out types);

        StringBuilder sb = new StringBuilder("SELECT ", 164);
        int flag = AppendReadColumns(sb, type, baseOnly);
        AppendTables(sb, flag);
        sb.Append(" WHERE");
        List<object?> args = new List<object?>((types == null ? 0 : types.Length) + (conditionArgs == null ? 0 : conditionArgs.Length) + 3) { actor };
        if (conditionArgs != null && !string.IsNullOrEmpty(condition))
        {
            args.AddRange(conditionArgs);
            for (int i = 0; i < conditionArgs.Length; ++i)
                condition = Util.QuickFormat(condition, "@" + (i + 1).ToString(CultureInfo.InvariantCulture), i, repeat: true);
        }

        if (!string.IsNullOrEmpty(condition))
            sb.Append(" (").Append(condition).Append(')').Append(" AND");

        switch (relation)
        {
            default:
            case ActorRelationType.IsTarget:
                sb.Append($" `{ColumnEntriesSteam64}`=@0");
                break;
            case ActorRelationType.IsActor:
                sb.Append($" EXISTS (SELECT `a`.`{ColumnActorsId}` FROM `{TableActors}` AS `a` WHERE `a`.`{ColumnExternalPrimaryKey}` = `main`.`{ColumnEntriesPrimaryKey}` AND `a`.`{ColumnActorsId}`=@0)");
                break;
            case ActorRelationType.IsAdminActor:
                sb.Append($" EXISTS (SELECT * FROM `{TableActors}` AS `a` WHERE `a`.`{ColumnExternalPrimaryKey}` = `main`.`{ColumnEntriesPrimaryKey}` AND `a`.`{ColumnActorsId}`=@0 AND `a`.`{ColumnActorsAsAdmin}` != 0)");
                break;
            case ActorRelationType.IsNonAdminActor:
                sb.Append($" EXISTS (SELECT * FROM `{TableActors}` AS `a` WHERE `a`.`{ColumnExternalPrimaryKey}` = `main`.`{ColumnEntriesPrimaryKey}` AND `a`.`{ColumnActorsId}`=@0 AND `a`.`{ColumnActorsAsAdmin}` == 0)");
                break;
        }
        
        if (start.HasValue && end.HasValue)
        {
            sb.Append($" AND `main`.`{ColumnEntriesStartTimestamp}` >= @{args.Count.ToString(CultureInfo.InvariantCulture)} AND `main`.`{ColumnEntriesStartTimestamp}` <= @{(args.Count + 1).ToString(CultureInfo.InvariantCulture)}");

            args.Add(start.Value.UtcDateTime);
            args.Add(end.Value.UtcDateTime);
        }
        else if (start.HasValue)
        {
            sb.Append($" AND `main`.`{ColumnEntriesStartTimestamp}` >= @{args.Count.ToString(CultureInfo.InvariantCulture)}");
            args.Add(start.Value.UtcDateTime);
        }
        else if (end.HasValue)
        {
            sb.Append($" AND `main`.`{ColumnEntriesStartTimestamp}` <= @1");
            args.Add(end.Value.UtcDateTime);
        }
        
        if (types is { Length: > 0 })
        {
            sb.Append($" AND `main`.`{ColumnEntriesType}` IN (");

            for (int i = 0; i < types.Length; ++i)
            {
                if (i != 0)
                    sb.Append(',');
                sb.Append('@').Append(args.Count.ToString(CultureInfo.InvariantCulture));
                args.Add(types[i].ToString());
            }

            sb.Append(")");
        }

        if (orderBy != null)
            sb.Append(" ORDER BY " + orderBy);

        sb.Append(';');


        ArrayList entries = new ArrayList(16);
        await Sql.QueryAsync(sb.ToString(), args, reader =>
        {
            ModerationEntry? entry = ReadEntry(flag, reader);
            if (type.IsInstanceOfType(entry))
                entries.Add(entry);
        }, token).ConfigureAwait(false);

        Array rtn = entries.ToArray(type);

        // ReSharper disable once CoVariantArrayConversion
        await Fill((ModerationEntry[])rtn, detail, baseOnly, null, token).ConfigureAwait(false);
        
        return rtn;
    }
    public async Task<T[]> ReadAll<T>(bool detail = true, bool baseOnly = false, DateTimeOffset ? start = null, DateTimeOffset? end = null, string? condition = null, string? orderBy = null, object[]? conditionArgs = null, CancellationToken token = default) where T : class, IModerationEntry
        => (T[])await ReadAll(typeof(T), detail, baseOnly, start, end, condition, orderBy, conditionArgs, token).ConfigureAwait(false);
    public async Task<Array> ReadAll(Type type, bool detail = true, bool baseOnly = false, DateTimeOffset? start = null, DateTimeOffset? end = null, string? condition = null, string? orderBy = null, object[]? conditionArgs = null, CancellationToken token = default)
    {
        ModerationEntryType[]? types = null;
        if (type != typeof(ModerationEntry) && type != typeof(IModerationEntry))
            ModerationReflection.TryGetInheritance(type, out types);

        StringBuilder sb = new StringBuilder("SELECT ", 164);
        int flag = AppendReadColumns(sb, type, baseOnly);
        AppendTables(sb, flag);
        bool where = false, and = true;
        List<object?> args = new List<object?>((types == null ? 0 : types.Length) + (conditionArgs == null ? 0 : conditionArgs.Length) + 2);
        if (conditionArgs != null && !string.IsNullOrEmpty(condition))
        {
            sb.Append(" WHERE ");
            where = true;
            args.AddRange(conditionArgs);
            for (int i = 0; i < conditionArgs.Length; ++i)
                condition = Util.QuickFormat(condition, "@" + i.ToString(CultureInfo.InvariantCulture), i, repeat: true);
        }
        if (!string.IsNullOrEmpty(condition))
        {
            sb.Append('(').Append(condition).Append(')');
            and = false;
        }
        
        if (start.HasValue && end.HasValue)
        {
            if (!where)
            {
                sb.Append(" WHERE ");
                where = true;
            }
            if (!and)
                sb.Append(" AND ");
            sb.Append($"`main`.`{ColumnEntriesStartTimestamp}` >= @{args.Count.ToString(CultureInfo.InvariantCulture)} AND `main`.`{ColumnEntriesStartTimestamp}` <= @{(args.Count + 1).ToString(CultureInfo.InvariantCulture)}");

            args.Add(start.Value.UtcDateTime);
            args.Add(end.Value.UtcDateTime);
        }
        else if (start.HasValue)
        {
            if (!where)
            {
                sb.Append(" WHERE ");
                where = true;
            }
            if (!and)
                sb.Append(" AND ");
            sb.Append($"`main`.`{ColumnEntriesStartTimestamp}` >= @{args.Count.ToString(CultureInfo.InvariantCulture)}");
            args.Add(start.Value.UtcDateTime);
        }
        else if (end.HasValue)
        {
            if (!where)
            {
                sb.Append(" WHERE ");
                where = true;
            }
            if (!and)
                sb.Append(" AND ");
            sb.Append($"`main`.`{ColumnEntriesStartTimestamp}` <= @1");
            args.Add(end.Value.UtcDateTime);
        }

        if (types is { Length: > 0 })
        {
            if (!where)
                sb.Append(" WHERE ");
            if (!and)
                sb.Append(" AND ");
            sb.Append($"`main`.`{ColumnEntriesType}` IN (");

            for (int i = 0; i < types.Length; ++i)
            {
                if (i != 0)
                    sb.Append(',');
                sb.Append('@').Append(args.Count.ToString(CultureInfo.InvariantCulture));
                args.Add(types[i].ToString());
            }

            sb.Append(")");
        }

        if (orderBy != null)
            sb.Append(" ORDER BY " + orderBy);

        sb.Append(';');

        ArrayList entries = new ArrayList(16);
        await Sql.QueryAsync(sb.ToString(), args, reader =>
        {
            ModerationEntry? entry = ReadEntry(flag, reader);
            if (type.IsInstanceOfType(entry))
                entries.Add(entry);
        }, token).ConfigureAwait(false);

        Array rtn = entries.ToArray(type);

        // ReSharper disable once CoVariantArrayConversion
        await Fill((ModerationEntry[])rtn, detail, baseOnly, null, token).ConfigureAwait(false);
        
        return rtn;
    }
    public async Task<T[]> GetEntriesOfLevel<T>(ulong player, int level, PresetType type, bool detail = true, bool baseOnly = false, CancellationToken token = default) where T : Punishment
        => (T[])await GetEntriesOfLevel(typeof(T), player, level, type, detail, baseOnly, token).ConfigureAwait(false);
    public async Task<Array> GetEntriesOfLevel(Type type, ulong player, int level, PresetType presetType, bool detail = true, bool baseOnly = false, CancellationToken token = default)
    {
        if (!typeof(Punishment).IsAssignableFrom(type))
            return Array.Empty<Punishment>();
        ModerationReflection.TryGetInheritance(type, out ModerationEntryType[] types);

        StringBuilder sb = new StringBuilder("SELECT ", 164);
        int flag = AppendReadColumns(sb, type, baseOnly);
        AppendTables(sb, flag);
        object?[] args = new object?[(types == null ? 0 : types.Length) + 3];

        args[0] = presetType.ToString();
        args[1] = level;
        args[2] = player;

        sb.Append($" WHERE `main`.`{ColumnEntriesRemoved}`=0 AND `pnsh`.`{ColumnPunishmentsPresetType}`=@0 AND `pnsh`.`{ColumnPunishmentsPresetLevel}`=@1 AND `main`.`{ColumnEntriesSteam64}`=@2");

        if (types is { Length: > 0 })
        {
            sb.Append($" AND `main`.`{ColumnEntriesType}` IN (");

            for (int i = 0; i < types.Length; ++i)
            {
                if (i != 0)
                    sb.Append(',');
                sb.Append('@').Append((i + 3).ToString(CultureInfo.InvariantCulture));
                args[i + 3] = types[i].ToString();
            }

            sb.Append(")");
        }

        sb.Append(';');

        ArrayList entries = new ArrayList(2);
        await Sql.QueryAsync(sb.ToString(), args, reader =>
        {
            ModerationEntry? entry = ReadEntry(flag, reader);
            if (type.IsInstanceOfType(entry))
                entries.Add(entry);
        }, token).ConfigureAwait(false);

        Array rtn = entries.ToArray(type);

        // ReSharper disable once CoVariantArrayConversion
        await Fill((ModerationEntry[])rtn, detail, baseOnly, null, token).ConfigureAwait(false);

        return rtn;
    }
    private async Task Fill(IModerationEntry?[] entries, bool detail, bool baseOnly, BitArray? mask = null, CancellationToken token = default)
    {
        detail &= baseOnly;
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
        bool anyAppeals = false;
        bool anyAssetBans = false;
        bool anyGreifingReports = false;
        bool anyChatAbuseReports = false;
        bool anyCheatingReports = false;
        for (int i = 0; i < entries.Length; ++i)
        {
            IModerationEntry? entry = entries[i];
            if (entry == null || mask is not null && !mask[i])
                continue;
            if (entry is Punishment p)
            {
                anyPunishments = true;
                if (p is AssetBan)
                    anyAssetBans = true;
            }
            else if (entry is Appeal)
                anyAppeals = true;
            else if (entry is GriefingReport)
                anyGreifingReports = true;
            else if (entry is ChatAbuseReport)
                anyChatAbuseReports = true;
            else if (entry is CheatingReport)
                anyCheatingReports = true;
        }

        // Actors
        string query = $"SELECT {SqlTypes.ColumnList(ColumnExternalPrimaryKey, ColumnActorsId, ColumnActorsRole, ColumnActorsAsAdmin)} " +
                $"FROM `{TableActors}` WHERE `{ColumnExternalPrimaryKey}` {inArg} ORDER BY `{ColumnExternalPrimaryKey}`, `{ColumnActorsIndex}`;";

        List<PrimaryKeyPair<RelatedActor>> actors = new List<PrimaryKeyPair<RelatedActor>>();
        await Sql.QueryAsync(query, null, reader =>
        {
            actors.Add(new PrimaryKeyPair<RelatedActor>(reader.GetInt32(0), ReadActor(reader, 1)));
        }, token).ConfigureAwait(false);

        F.ApplyQueriedList(actors, (key, arr) =>
        {
            IModerationEntry? info = entries.FindIndexed((x, i) => x != null && (mask is null || mask[i]) && x.Id.Key == key);
            if (info != null)
                info.Actors = arr;
        }, false);

        // Evidence
        query = $"SELECT {SqlTypes.ColumnList(ColumnExternalPrimaryKey, ColumnEvidenceId, ColumnEvidenceLink,
            ColumnEvidenceLocalSource, ColumnEvidenceMessage, ColumnEvidenceIsImage,
            ColumnEvidenceTimestamp, ColumnEvidenceActorId)} " +
                $"FROM `{TableEvidence}` WHERE `{ColumnExternalPrimaryKey}` {inArg} ORDER BY `{ColumnExternalPrimaryKey}`, `{ColumnEvidenceId}`;";

        List<PrimaryKeyPair<Evidence>> evidence = new List<PrimaryKeyPair<Evidence>>();
        await Sql.QueryAsync(query, null, reader =>
        {
            evidence.Add(new PrimaryKeyPair<Evidence>(reader.GetInt32(0), ReadEvidence(reader, 1)));
        }, token).ConfigureAwait(false);

        F.ApplyQueriedList(evidence, (key, arr) =>
        {
            IModerationEntry? info = entries.FindIndexed((x, i) => x != null && (mask is null || mask[i]) && x.Id.Key == key);
            if (info != null)
                info.Evidence = arr;
        }, false);

        List<PrimaryKeyPair<PrimaryKey>> links = new List<PrimaryKeyPair<PrimaryKey>>();

        if (!baseOnly)
        {
            // RelatedEntries
            query = $"SELECT {SqlTypes.ColumnList(ColumnExternalPrimaryKey, ColumnRelatedEntry)} FROM `{TableRelatedEntries}` WHERE `{ColumnExternalPrimaryKey}` {inArg} ORDER BY `{ColumnExternalPrimaryKey}`;";
            await Sql.QueryAsync(query, null, reader =>
            {
                links.Add(new PrimaryKeyPair<PrimaryKey>(reader.GetInt32(0), reader.GetUInt32(1)));
            }, token).ConfigureAwait(false);

            F.ApplyQueriedList(links, (key, arr) =>
            {
                IModerationEntry? info = entries.FindIndexed((x, i) => x != null && (mask is null || mask[i]) && x.Id.Key == key);
                if (info != null)
                    info.RelatedEntryKeys = arr;
            }, false);
        }
        
        if (anyPunishments)
        {
            if (!baseOnly)
            {
                links.Clear();

                // Punishment.Appeals
                query = $"SELECT {SqlTypes.ColumnList(ColumnExternalPrimaryKey, ColumnLinkedAppealsAppeal)} FROM `{TableLinkedAppeals}` WHERE `{ColumnExternalPrimaryKey}` {inArg} ORDER BY `{ColumnExternalPrimaryKey}`;";
                await Sql.QueryAsync(query, null, reader =>
                {
                    links.Add(new PrimaryKeyPair<PrimaryKey>(reader.GetInt32(0), reader.GetUInt32(1)));
                }, token).ConfigureAwait(false);

                F.ApplyQueriedList(links, (key, arr) =>
                {
                    Punishment? info = (Punishment?)entries.FindIndexed((x, i) => x is Punishment && (mask is null || mask[i]) && x.Id.Key == key);
                    if (info != null)
                        info.AppealKeys = arr;
                }, false);

                links.Clear();

                // Punishment.Reports
                query = $"SELECT {SqlTypes.ColumnList(ColumnExternalPrimaryKey, ColumnLinkedReportsReport)} FROM `{TableLinkedReports}` WHERE `{ColumnExternalPrimaryKey}` {inArg} ORDER BY `{ColumnExternalPrimaryKey}`;";
                await Sql.QueryAsync(query, null, reader =>
                {
                    links.Add(new PrimaryKeyPair<PrimaryKey>(reader.GetInt32(0), reader.GetUInt32(1)));
                }, token).ConfigureAwait(false);

                F.ApplyQueriedList(links, (key, arr) =>
                {
                    Punishment? info = (Punishment?)entries.FindIndexed((x, i) => x is Punishment && (mask is null || mask[i]) && x.Id.Key == key);
                    if (info != null)
                        info.ReportKeys = arr;
                }, false);
            }

            if (anyAssetBans)
            {
                List<PrimaryKeyPair<VehicleType>> types = new List<PrimaryKeyPair<VehicleType>>();

                // AssetBan.AssetFilter
                query = $"SELECT {SqlTypes.ColumnList(ColumnExternalPrimaryKey, ColumnAssetBanFiltersType)} FROM `{TableAssetBanTypeFilters}` WHERE `{ColumnExternalPrimaryKey}` {inArg} ORDER BY `{ColumnExternalPrimaryKey}`;";
                await Sql.QueryAsync(query, null, reader =>
                {
                    types.Add(new PrimaryKeyPair<VehicleType>(reader.GetInt32(0), reader.ReadStringEnum(1, VehicleType.None)));
                }, token).ConfigureAwait(false);

                F.ApplyQueriedList(types, (key, arr) =>
                {
                    AssetBan? info = (AssetBan?)entries.FindIndexed((x, i) => x is AssetBan && (mask is null || mask[i]) && x.Id.Key == key);
                    if (info != null)
                        info.VehicleTypeFilter = arr;
                }, false);
            }
        }

        if (!baseOnly && anyAppeals)
        {
            links.Clear();

            // Appeal.Punishments
            query = $"SELECT {SqlTypes.ColumnList(ColumnExternalPrimaryKey, ColumnAppealPunishmentsPunishment)} FROM `{TableAppealPunishments}` WHERE `{ColumnExternalPrimaryKey}` {inArg} ORDER BY `{ColumnExternalPrimaryKey}`;";
            await Sql.QueryAsync(query, null, reader =>
            {
                links.Add(new PrimaryKeyPair<PrimaryKey>(reader.GetInt32(0), reader.GetUInt32(1)));
            }, token).ConfigureAwait(false);

            F.ApplyQueriedList(links, (key, arr) =>
            {
                Appeal? info = (Appeal?)entries.FindIndexed((x, i) => x is Appeal && (mask is null || mask[i]) && x.Id.Key == key);
                if (info != null)
                    info.PunishmentKeys = arr;
            }, false);

            List<PrimaryKeyPair<AppealResponse>> responses = new List<PrimaryKeyPair<AppealResponse>>();

            // Appeal.Responses
            query = $"SELECT {SqlTypes.ColumnList(ColumnExternalPrimaryKey, ColumnAppealResponsesQuestion, ColumnAppealResponsesResponse)} " +
                    $"FROM `{TableAppealResponses}` WHERE `{ColumnExternalPrimaryKey}` {inArg} ORDER BY `{ColumnExternalPrimaryKey}`;";
            await Sql.QueryAsync(query, null, reader =>
            {
                responses.Add(new PrimaryKeyPair<AppealResponse>(reader.GetInt32(0), new AppealResponse(reader.GetString(1), reader.GetString(2))));
            }, token).ConfigureAwait(false);

            F.ApplyQueriedList(responses, (key, arr) =>
            {
                Appeal? info = (Appeal?)entries.FindIndexed((x, i) => x is Appeal && (mask is null || mask[i]) && x.Id.Key == key);
                if (info != null)
                    info.Responses = arr;
            }, false);
        }

        if (!baseOnly && anyChatAbuseReports)
        {
            List<PrimaryKeyPair<AbusiveChatRecord>> chats = new List<PrimaryKeyPair<AbusiveChatRecord>>();

            // ChatAbuseReport.Messages
            query = $"SELECT {SqlTypes.ColumnList(ColumnExternalPrimaryKey, ColumnReportsChatRecordsMessage, ColumnReportsChatRecordsTimestamp)} " +
                    $"FROM `{TableReportChatRecords}` WHERE `{ColumnExternalPrimaryKey}` {inArg} ORDER BY `{ColumnExternalPrimaryKey}`, `{ColumnReportsChatRecordsIndex}`;";
            await Sql.QueryAsync(query, null, reader =>
            {
                chats.Add(new PrimaryKeyPair<AbusiveChatRecord>(reader.GetInt32(0), new AbusiveChatRecord(reader.GetString(1), new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(2), DateTimeKind.Utc)))));
            }, token).ConfigureAwait(false);

            F.ApplyQueriedList(chats, (key, arr) =>
            {
                ChatAbuseReport? info = (ChatAbuseReport?)entries.FindIndexed((x, i) => x is ChatAbuseReport && (mask is null || mask[i]) && x.Id.Key == key);
                if (info != null)
                    info.Messages = arr;
            }, false);
        }

        if (!baseOnly && anyCheatingReports)
        {
            List<PrimaryKeyPair<ShotRecord>> shots = new List<PrimaryKeyPair<ShotRecord>>();

            // CheatingReport.Shots
            query = $"SELECT {SqlTypes.ColumnList(ColumnExternalPrimaryKey, ColumnReportsShotRecordAmmo, ColumnReportsShotRecordAmmoName,
                ColumnReportsShotRecordItem, ColumnReportsShotRecordItemName,
                ColumnReportsShotRecordDamageDone, ColumnReportsShotRecordLimb,
                ColumnReportsShotRecordIsProjectile, ColumnReportsShotRecordDistance,
                ColumnReportsShotRecordHitPointX, ColumnReportsShotRecordHitPointY, ColumnReportsShotRecordHitPointZ,
                ColumnReportsShotRecordShootFromPointX, ColumnReportsShotRecordShootFromPointY, ColumnReportsShotRecordShootFromPointZ,
                ColumnReportsShotRecordShootFromRotationX, ColumnReportsShotRecordShootFromRotationY, ColumnReportsShotRecordShootFromRotationZ,
                ColumnReportsShotRecordHitType, ColumnReportsShotRecordHitActor,
                ColumnReportsShotRecordHitAsset, ColumnReportsShotRecordHitAssetName,
                ColumnReportsShotRecordTimestamp)} " +
                    $"FROM `{TableReportShotRecords}` WHERE `{ColumnExternalPrimaryKey}` {inArg} ORDER BY `{ColumnExternalPrimaryKey}`;";
            await Sql.QueryAsync(query, null, reader =>
            {
                shots.Add(new PrimaryKeyPair<ShotRecord>(reader.GetInt32(0), new ShotRecord(
                    reader.GetGuid(3),
                    reader.GetGuid(1),
                    reader.IsDBNull(4) ? null : reader.GetString(4),
                    reader.IsDBNull(2) ? null : reader.GetString(2),
                    reader.ReadStringEnum(18, EPlayerKill.NONE),
                    reader.IsDBNull(19) ? null : Actors.GetActor(reader.GetUInt64(19)),
                    reader.IsDBNull(20) ? null : reader.GetGuid(20),
                    reader.IsDBNull(21) ? null : reader.GetString(21),
                    reader.IsDBNull(6) ? null : (reader.ReadStringEnum<ELimb>(6) ?? null),
                    new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(22), DateTimeKind.Utc)),
                    new Vector3(reader.GetFloat(12), reader.GetFloat(13), reader.GetFloat(14)),
                    new Vector3(reader.GetFloat(15), reader.GetFloat(16), reader.GetFloat(17)),
                    reader.IsDBNull(9) || reader.IsDBNull(10) || reader.IsDBNull(11) ? null : new Vector3(reader.GetFloat(9), reader.GetFloat(10), reader.GetFloat(11)),
                    !reader.IsDBNull(7) && reader.GetBoolean(7),
                    reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                    reader.IsDBNull(8) ? 0d : reader.GetDouble(8)
                )));
            }, token).ConfigureAwait(false);

            F.ApplyQueriedList(shots, (key, arr) =>
            {
                CheatingReport? info = (CheatingReport?)entries.FindIndexed((x, i) => x is CheatingReport && (mask is null || mask[i]) && x.Id.Key == key);
                if (info != null)
                    info.Shots = arr;
            }, false);
        }

        if (!baseOnly && anyGreifingReports)
        {
            List<PrimaryKeyPair<StructureDamageRecord>> damages = new List<PrimaryKeyPair<StructureDamageRecord>>();

            // GriefingReport.DamageRecord
            query = $"SELECT {SqlTypes.ColumnList(ColumnExternalPrimaryKey, ColumnReportsStructureDamageDamage, ColumnReportsStructureDamageDamageOrigin,
                ColumnReportsStructureDamageInstanceId, ColumnReportsStructureDamageStructure, ColumnReportsStructureDamageStructureName,
                ColumnReportsStructureDamageStructureOwner, ColumnReportsStructureDamageStructureType, ColumnReportsStructureDamageWasDestroyed,
                ColumnReportsStructureDamageTimestamp)} " +
                    $"FROM `{TableReportStructureDamageRecords}` WHERE `{ColumnExternalPrimaryKey}` {inArg} ORDER BY `{ColumnExternalPrimaryKey}`;";
            await Sql.QueryAsync(query, null, reader =>
            {
                damages.Add(new PrimaryKeyPair<StructureDamageRecord>(reader.GetInt32(0),
                    new StructureDamageRecord(reader.ReadGuidString(4) ?? Guid.Empty, reader.GetString(5), reader.GetUInt64(6),
                        reader.ReadStringEnum(2, EDamageOrigin.Unknown), reader.ReadStringEnum(7, StructType.Unknown), reader.GetUInt32(3),
                        reader.GetInt32(1), reader.GetBoolean(8), new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(9), DateTimeKind.Utc)))));
            }, token).ConfigureAwait(false);

            F.ApplyQueriedList(damages, (key, arr) =>
            {
                GriefingReport? info = (GriefingReport?)entries.FindIndexed((x, i) => x is GriefingReport && (mask is null || mask[i]) && x.Id.Key == key);
                if (info != null)
                    info.DamageRecord = arr;
            }, false);

            List<PrimaryKeyPair<TeamkillRecord>> tks = new List<PrimaryKeyPair<TeamkillRecord>>();

            // GriefingReport.TeamkillRecord
            query = $"SELECT {SqlTypes.ColumnList(ColumnExternalPrimaryKey, ColumnReportsTeamkillRecordVictim, ColumnReportsTeamkillRecordDeathCause,
                ColumnReportsTeamkillRecordWasIntentional, ColumnReportsTeamkillRecordTeamkill, ColumnReportsTeamkillRecordMessage, ColumnReportsTeamkillRecordTimestamp)} " +
                    $"FROM `{TableReportTeamkillRecords}` WHERE `{ColumnExternalPrimaryKey}` {inArg} ORDER BY `{ColumnExternalPrimaryKey}`;";
            await Sql.QueryAsync(query, null, reader =>
            {
                tks.Add(new PrimaryKeyPair<TeamkillRecord>(reader.GetInt32(0),
                    new TeamkillRecord(reader.GetUInt32(4), reader.GetUInt64(1), reader.ReadStringEnum(2, EDeathCause.KILL),
                        reader.GetString(5), reader.IsDBNull(3) ? null : reader.GetBoolean(3), new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(6), DateTimeKind.Utc)))));
            }, token).ConfigureAwait(false);

            F.ApplyQueriedList(tks, (key, arr) =>
            {
                GriefingReport? info = (GriefingReport?)entries.FindIndexed((x, i) => x is GriefingReport && (mask is null || mask[i]) && x.Id.Key == key);
                if (info != null)
                    info.TeamkillRecord = arr;
            }, false);

            List<PrimaryKeyPair<VehicleTeamkillRecord>> vtks = new List<PrimaryKeyPair<VehicleTeamkillRecord>>();

            // GriefingReport.VehicleTeamkillRecord
            query = $"SELECT {SqlTypes.ColumnList(ColumnExternalPrimaryKey, ColumnReportsVehicleTeamkillRecordVictim, ColumnReportsVehicleTeamkillRecordDamageOrigin,
                ColumnReportsVehicleTeamkillRecordTeamkill, ColumnReportsVehicleTeamkillRecordMessage, ColumnReportsVehicleTeamkillRecordTimestamp)} " +
                    $"FROM `{TableReportVehicleTeamkillRecords}` WHERE `{ColumnExternalPrimaryKey}` {inArg} ORDER BY `{ColumnExternalPrimaryKey}`;";
            await Sql.QueryAsync(query, null, reader =>
            {
                vtks.Add(new PrimaryKeyPair<VehicleTeamkillRecord>(reader.GetInt32(0),
                    new VehicleTeamkillRecord(reader.GetUInt32(3), reader.GetUInt64(1), reader.ReadStringEnum(2, EDamageOrigin.Unknown),
                        reader.GetString(4), new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(5), DateTimeKind.Utc)))));
            }, token).ConfigureAwait(false);

            F.ApplyQueriedList(vtks, (key, arr) =>
            {
                GriefingReport? info = (GriefingReport?)entries.FindIndexed((x, i) => x is GriefingReport && (mask is null || mask[i]) && x.Id.Key == key);
                if (info != null)
                    info.VehicleTeamkillRecord = arr;
            }, false);

            List<PrimaryKeyPair<VehicleRequestRecord>> reqs = new List<PrimaryKeyPair<VehicleRequestRecord>>();

            // GriefingReport.VehicleRequestRecord
            query = $"SELECT {SqlTypes.ColumnList(ColumnExternalPrimaryKey, ColumnReportsVehicleRequestRecordAsset, ColumnReportsVehicleRequestRecordVehicle,
                ColumnReportsVehicleRequestRecordVehicleName, ColumnReportsVehicleRequestRecordInstigator, ColumnReportsVehicleRequestRecordDamageOrigin,
                ColumnReportsVehicleRequestRecordRequestTimestamp, ColumnReportsVehicleRequestRecordDestroyTimestamp)} " +
                    $"FROM `{TableReportVehicleTeamkillRecords}` WHERE `{ColumnExternalPrimaryKey}` {inArg} ORDER BY `{ColumnExternalPrimaryKey}`;";
            await Sql.QueryAsync(query, null, reader =>
            {
                reqs.Add(new PrimaryKeyPair<VehicleRequestRecord>(reader.GetInt32(0),
                    new VehicleRequestRecord(reader.ReadGuidString(2) ?? Guid.Empty, reader.IsDBNull(1) ? PrimaryKey.NotAssigned : reader.GetUInt32(1), reader.GetString(3),
                    new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(6), DateTimeKind.Utc)),
                    reader.IsDBNull(7) ? null : new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(7), DateTimeKind.Utc)), reader.ReadStringEnum(5, EDamageOrigin.Unknown), reader.GetUInt64(4))));
            }, token).ConfigureAwait(false);

            F.ApplyQueriedList(reqs, (key, arr) =>
            {
                GriefingReport? info = (GriefingReport?)entries.FindIndexed((x, i) => x is GriefingReport && (mask is null || mask[i]) && x.Id.Key == key);
                if (info != null)
                    info.VehicleRequestRecord = arr;
            }, false);
        }

        if (detail)
        {
            for (int i = 0; i < entries.Length; ++i)
            {
                if (entries[i] is not { } e || mask is not null && !mask[i]) continue;
                Cache.AddOrUpdate(e);
            }

            List<Task> tasks = new List<Task>();
            for (int i = 0; i < entries.Length; ++i)
            {
                if (entries[i] is not ModerationEntry e || mask is not null && !mask[i]) continue;
                tasks.Add(e.FillDetail(this, token));
            }

            await Task.WhenAll(tasks.AsArrayFast()).ConfigureAwait(false);
        }
    }
    public async Task<T[]> GetActiveEntries<T>(ulong steam64, bool detail = true, bool baseOnly = false, string? condition = null, string? orderBy = null, object[]? conditionArgs = null, DateTimeOffset? start = null, DateTimeOffset? end = null, CancellationToken token = default) where T : IDurationModerationEntry
        => (T[])await GetActiveEntries(typeof(T), steam64, detail, baseOnly, condition, orderBy, conditionArgs, start, end, token);
    public Task<Array> GetActiveEntries(Type type, ulong steam64, bool detail = true, bool baseOnly = false, string? condition = null, string? orderBy = null, object[]? conditionArgs = null, DateTimeOffset? start = null, DateTimeOffset? end = null, CancellationToken token = default)
    {
        bool dur = typeof(IDurationModerationEntry).IsAssignableFrom(type);
        string cond = (dur
            ? $"(`dur`.`{ColumnDurationsForgiven}` = 0 OR `dur`.`{ColumnDurationsForgiven}` IS NULL) AND "
            : string.Empty) + $"`main`.`{ColumnEntriesRemoved}` = 0";
        if (dur)
        {
            cond += " AND " + Util.BuildCheckDurationClause("dur", "main", ColumnDurationsDurationSeconds, ColumnEntriesResolvedTimestamp, ColumnEntriesStartTimestamp);
        }

        if (condition != null)
            cond += " AND (" + condition + ")";
        
        if (dur)
            orderBy ??= $"(IF(`dur`.`{ColumnDurationsDurationSeconds}` < 0, 2147483647, `dur`.`{ColumnDurationsDurationSeconds}`)) DESC";

        return ReadAll(type, steam64, ActorRelationType.IsTarget, detail, baseOnly, start, end, cond, orderBy, conditionArgs, token: token);
    }
    public async Task<T[]> GetActiveEntries<T>(ulong baseSteam64, IReadOnlyList<PlayerIPAddress> addresses, IReadOnlyList<PlayerHWID> hwids, bool detail = true, bool baseOnly = false, string? condition = null, string? orderBy = null, object[]? conditionArgs = null, DateTimeOffset? start = null, DateTimeOffset? end = null, CancellationToken token = default) where T : IDurationModerationEntry
        => (T[])await GetActiveEntries(typeof(T), baseSteam64, addresses, hwids, detail, baseOnly, condition, orderBy, conditionArgs, start, end, token);
    public async Task<Array> GetActiveEntries(Type type, ulong baseSteam64, IReadOnlyList<PlayerIPAddress> addresses, IReadOnlyList<PlayerHWID> hwids, bool detail = true, bool baseOnly = false, string? condition = null, string? orderBy = null, object[]? conditionArgs = null, DateTimeOffset? start = null, DateTimeOffset? end = null, CancellationToken token = default)
    {
        bool dur = typeof(IDurationModerationEntry).IsAssignableFrom(type);
        string cond = (dur
            ? $"(`dur`.`{ColumnDurationsForgiven}` = 0 OR `dur`.`{ColumnDurationsForgiven}` IS NULL) AND "
            : string.Empty) + $"`main`.`{ColumnEntriesRemoved}` = 0";
        if (dur)
            cond += " AND " + Util.BuildCheckDurationClause("dur", "main", ColumnDurationsDurationSeconds, ColumnEntriesResolvedTimestamp, ColumnEntriesStartTimestamp);

        if (condition != null)
            cond += " AND (" + condition + ")";

        ModerationEntryType[]? types = null;
        if (type != typeof(ModerationEntry) && type != typeof(IModerationEntry))
            ModerationReflection.TryGetInheritance(type, out types);

        StringBuilder sb = new StringBuilder("SELECT ", 164);
        int flag = AppendReadColumns(sb, type, baseOnly);
        AppendTables(sb, flag);
        sb.Append(" WHERE");
        List<object?> args = new List<object?>((types == null ? 0 : types.Length) + (conditionArgs == null ? 0 : conditionArgs.Length) + 2 + hwids.Count * 2) { baseSteam64 };
        if (conditionArgs != null && !string.IsNullOrEmpty(condition))
        {
            args.AddRange(conditionArgs);
            for (int i = 0; i < conditionArgs.Length; ++i)
                cond = Util.QuickFormat(cond, "@" + (i + 1).ToString(CultureInfo.InvariantCulture), i, repeat: true);
        }

        if (!string.IsNullOrEmpty(cond))
            sb.Append(" (").Append(cond).Append(')');

        if (start.HasValue && end.HasValue)
        {
            sb.Append($" AND `main`.`{ColumnEntriesStartTimestamp}` >= @{args.Count.ToString(CultureInfo.InvariantCulture)} AND `main`.`{ColumnEntriesStartTimestamp}` <= @{(args.Count + 1).ToString(CultureInfo.InvariantCulture)}");

            args.Add(start.Value.UtcDateTime);
            args.Add(end.Value.UtcDateTime);
        }
        else if (start.HasValue)
        {
            sb.Append($" AND `main`.`{ColumnEntriesStartTimestamp}` >= @{args.Count.ToString(CultureInfo.InvariantCulture)}");
            args.Add(start.Value.UtcDateTime);
        }
        else if (end.HasValue)
        {
            sb.Append($" AND `main`.`{ColumnEntriesStartTimestamp}` <= @1");
            args.Add(end.Value.UtcDateTime);
        }

        if (types is { Length: > 0 })
        {
            if (types.Length == 1)
            {
                sb.Append($" AND `main`.`{ColumnEntriesType}`=@{args.Count.ToString(CultureInfo.InvariantCulture)}");
                args.Add(types[0].ToString());
            }
            else
            {
                sb.Append($" AND `main`.`{ColumnEntriesType}` IN (");

                for (int i = 0; i < types.Length; ++i)
                {
                    if (i != 0)
                        sb.Append(',');
                    sb.Append('@').Append(args.Count.ToString(CultureInfo.InvariantCulture));
                    args.Add(types[i].ToString());
                }

                sb.Append(")");
            }
        }

        if (addresses.Count > 0 || hwids.Count > 0)
            sb.Append($" AND (`main`.`{ColumnEntriesSteam64}`=@0 OR ");
        else
            sb.Append($" AND `main`.`{ColumnEntriesSteam64}`=@0 ");

        if (addresses.Count > 0)
        {
            sb.Append($"(EXISTS (SELECT * FROM `{WarfareSQL.TableIPAddresses}` AS `ip` WHERE `ip`.`{WarfareSQL.ColumnIPAddressesSteam64}`=`main`.`{ColumnEntriesSteam64}`");

            sb.Append($"AND `ip`.`{WarfareSQL.ColumnIPAddressesPackedIP}` IN (");

            for (int i = 0; i < addresses.Count; ++i)
            {
                if (i != 0)
                    sb.Append(',');
                sb.Append(addresses[i].PackedIP.ToString(CultureInfo.InvariantCulture));
            }

            sb.Append(")))");
        }

        if (hwids.Count > 0)
        {
            if (addresses.Count > 0)
                sb.Append(" OR ");
            sb.Append($"(EXISTS (SELECT * FROM `{WarfareSQL.TableHWIDs}` AS `hwid` WHERE `hwid`.`{WarfareSQL.ColumnHWIDsSteam64}`=`main`.`{ColumnEntriesSteam64}`");

            sb.Append($"AND `hwid`.`{WarfareSQL.ColumnHWIDsHWID}` IN (");

            for (int i = 0; i < hwids.Count; ++i)
            {
                if (i != 0)
                    sb.Append(',');
                sb.Append('@').Append(args.Count.ToString(CultureInfo.InvariantCulture));
                args.Add(hwids[i].HWID.ToByteArray());
            }

            sb.Append(")))");
        }

        if (addresses.Count > 0 || hwids.Count > 0)
            sb.Append(')');

        if (orderBy != null)
            sb.Append(" ORDER BY ").Append(orderBy);
        else if (dur)
            sb.Append($" ORDER BY (IF(`dur`.`{ColumnDurationsDurationSeconds}` < 0, 2147483647, `dur`.`{ColumnDurationsDurationSeconds}`)) DESC");

        sb.Append(';');

        ArrayList entries = new ArrayList(16);
        await Sql.QueryAsync(sb.ToString(), args, reader =>
        {
            ModerationEntry? entry = ReadEntry(flag, reader);
            if (type.IsInstanceOfType(entry))
                entries.Add(entry);
        }, token).ConfigureAwait(false);

        Array rtn = entries.ToArray(type);

        // ReSharper disable once CoVariantArrayConversion
        await Fill((ModerationEntry[])rtn, detail, baseOnly, null, token).ConfigureAwait(false);

        return rtn;
    }
    public async Task<AssetBan?> GetActiveAssetBan(ulong steam64, VehicleType type, bool detail = true, CancellationToken token = default)
    {
        AssetBan? result = null;
        
        await Sql.QueryAsync(
            $"SELECT {SqlTypes.ColumnListAliased("e", _columns)}, " +
            $"{SqlTypes.ColumnListAliased("d", ColumnDurationsDurationSeconds, ColumnDurationsForgiven, ColumnDurationsForgivenBy, ColumnDurationsForgivenTimestamp, ColumnDurationsForgivenReason)}, " +
            $"{SqlTypes.ColumnListAliased("pnsh", ColumnPunishmentsPresetType, ColumnPunishmentsPresetLevel)}" +
            $"FROM `{TableEntries}` AS `e` LEFT JOIN `{TableDurationPunishments}` AS `d` ON `e`.`{ColumnEntriesPrimaryKey}`=`d`.`{ColumnExternalPrimaryKey}` LEFT JOIN `{TablePunishments}` AS `pnsh` ON `e`.`{ColumnEntriesPrimaryKey}`=`pnsh`.`{ColumnExternalPrimaryKey}` " +
            $"WHERE `e`.`{ColumnEntriesSteam64}` = @0 " +
            $"AND `e`.`{ColumnEntriesType}` = '" + nameof(ModerationEntryType.AssetBan) + "' " +
            $"AND `d`.`{ColumnDurationsForgiven}` = 0 " +
            $"AND `e`.`{ColumnEntriesRemoved}` = 0 " +
            $"AND {Util.BuildCheckDurationClause("d", "e", ColumnDurationsDurationSeconds, ColumnEntriesResolvedTimestamp, ColumnEntriesStartTimestamp)} " +
            $"AND (NOT EXISTS (SELECT NULL FROM `{TableAssetBanTypeFilters}` AS `a` WHERE `a`.`{ColumnExternalPrimaryKey}`=`e`.`{ColumnEntriesPrimaryKey}`) " +
            $"OR (@1 IN (SELECT `a`.`{ColumnAssetBanFiltersType}` FROM `{TableAssetBanTypeFilters}` AS `a` WHERE `a`.`{ColumnExternalPrimaryKey}`=`e`.`{ColumnEntriesPrimaryKey}`))) " +
            $"ORDER BY (IF(`d`.`{ColumnDurationsDurationSeconds}` < 0, 2147483647, `d`.`{ColumnDurationsDurationSeconds}`)) DESC;",
            new object[] { steam64, type.ToString() },
            reader =>
            {
                result = ReadEntry(1 | (1 << 9), reader) as AssetBan;
                return true;
            }, token).ConfigureAwait(false);

        if (detail && result != null)
            await Fill(new IModerationEntry[] { result }, true, false, token: token).ConfigureAwait(false);

        return result;
    }
    private int AppendReadColumns(StringBuilder sb, Type type, bool baseOnly)
    {
        sb.Append(SqlTypes.ColumnListAliased("main", _columns));
        int flag = 0;
        if (type.IsAssignableFrom(typeof(IDurationModerationEntry)) || typeof(IDurationModerationEntry).IsAssignableFrom(type) || type.IsAssignableFrom(typeof(DurationPunishment)) || typeof(DurationPunishment).IsAssignableFrom(type))
        {
            flag |= 1;
            sb.Append("," + SqlTypes.ColumnListAliased("dur", ColumnDurationsDurationSeconds, ColumnDurationsForgiven, ColumnDurationsForgivenBy, ColumnDurationsForgivenTimestamp, ColumnDurationsForgivenReason));
        }
        if (type.IsAssignableFrom(typeof(Mute)) || typeof(Mute).IsAssignableFrom(type))
        {
            flag |= 1 << 1;
            sb.Append(",`mutes`.`" + ColumnMutesType + "`");
        }
        if (!baseOnly && (type.IsAssignableFrom(typeof(Warning)) || typeof(Warning).IsAssignableFrom(type)))
        {
            flag |= 1 << 2;
            sb.Append(",`warns`.`" + ColumnWarningsDisplayedTimestamp + "`");
        }
        if (!baseOnly && (type.IsAssignableFrom(typeof(PlayerReportAccepted)) || typeof(PlayerReportAccepted).IsAssignableFrom(type)))
        {
            flag |= 1 << 3;
            sb.Append(",`praccept`.`" + ColumnPlayerReportAcceptedsReport + "`");
        }
        if (!baseOnly && (type.IsAssignableFrom(typeof(BugReportAccepted)) || typeof(BugReportAccepted).IsAssignableFrom(type)))
        {
            flag |= 1 << 4;
            sb.Append("," + SqlTypes.ColumnListAliased("braccept", ColumnTableBugReportAcceptedsIssue, ColumnTableBugReportAcceptedsCommit));
        }
        if (!baseOnly && (type.IsAssignableFrom(typeof(Teamkill)) || typeof(Teamkill).IsAssignableFrom(type)))
        {
            flag |= 1 << 5;
            sb.Append("," + SqlTypes.ColumnListAliased("tks", ColumnTeamkillsAsset, ColumnTeamkillsAssetName, ColumnTeamkillsDeathCause, ColumnTeamkillsDistance, ColumnTeamkillsLimb));
        }
        if (!baseOnly && (type.IsAssignableFrom(typeof(VehicleTeamkill)) || typeof(VehicleTeamkill).IsAssignableFrom(type)))
        {
            flag |= 1 << 6;
            sb.Append("," + SqlTypes.ColumnListAliased("vtks", ColumnVehicleTeamkillsDamageOrigin, ColumnVehicleTeamkillsVehicleAsset, ColumnVehicleTeamkillsVehicleAssetName));
        }
        if (!baseOnly && (type.IsAssignableFrom(typeof(Appeal)) || typeof(Appeal).IsAssignableFrom(type)))
        {
            flag |= 1 << 7;
            sb.Append("," + SqlTypes.ColumnListAliased("app", ColumnAppealsState, ColumnAppealsDiscordId, ColumnAppealsTicketId));
        }
        if (type.IsAssignableFrom(typeof(Report)) || typeof(Report).IsAssignableFrom(type))
        {
            flag |= 1 << 8;
            sb.Append(",`rep`.`" + ColumnReportsType + "`");
        }
        if (type.IsAssignableFrom(typeof(Punishment)) || typeof(Punishment).IsAssignableFrom(type))
        {
            flag |= 1 << 9;
            sb.Append("," + SqlTypes.ColumnListAliased("pnsh", ColumnPunishmentsPresetType, ColumnPunishmentsPresetLevel));
        }

        return flag;
    }
    private static void AppendTables(StringBuilder sb, int flag)
    {
        sb.Append($" FROM `{TableEntries}` AS `main`");
        if (flag == 0) return;
        if ((flag & 1) != 0)
        {
            sb.Append($" LEFT JOIN `{TableDurationPunishments}` AS `dur` ON `main`.`{ColumnEntriesPrimaryKey}` = `dur`.`{ColumnExternalPrimaryKey}`");
        }
        if ((flag & (1 << 1)) != 0)
        {
            sb.Append($" LEFT JOIN `{TableMutes}` AS `mutes` ON `main`.`{ColumnEntriesPrimaryKey}` = `mutes`.`{ColumnExternalPrimaryKey}`");
        }
        if ((flag & (1 << 2)) != 0)
        {
            sb.Append($" LEFT JOIN `{TableWarnings}` AS `warns` ON `main`.`{ColumnEntriesPrimaryKey}` = `warns`.`{ColumnExternalPrimaryKey}`");
        }
        if ((flag & (1 << 3)) != 0)
        {
            sb.Append($" LEFT JOIN `{TablePlayerReportAccepteds}` AS `praccept` ON `main`.`{ColumnEntriesPrimaryKey}` = `praccept`.`{ColumnExternalPrimaryKey}`");
        }
        if ((flag & (1 << 4)) != 0)
        {
            sb.Append($" LEFT JOIN `{TableBugReportAccepteds}` AS `braccept` ON `main`.`{ColumnEntriesPrimaryKey}` = `braccept`.`{ColumnExternalPrimaryKey}`");
        }
        if ((flag & (1 << 5)) != 0)
        {
            sb.Append($" LEFT JOIN `{TableTeamkills}` AS `tks` ON `main`.`{ColumnEntriesPrimaryKey}` = `tks`.`{ColumnExternalPrimaryKey}`");
        }
        if ((flag & (1 << 6)) != 0)
        {
            sb.Append($" LEFT JOIN `{TableVehicleTeamkills}` AS `vtks` ON `main`.`{ColumnEntriesPrimaryKey}` = `vtks`.`{ColumnExternalPrimaryKey}`");
        }
        if ((flag & (1 << 7)) != 0)
        {
            sb.Append($" LEFT JOIN `{TableAppeals}` AS `app` ON `main`.`{ColumnEntriesPrimaryKey}` = `app`.`{ColumnExternalPrimaryKey}`");
        }
        if ((flag & (1 << 8)) != 0)
        {
            sb.Append($" LEFT JOIN `{TableReports}` AS `rep` ON `main`.`{ColumnEntriesPrimaryKey}` = `rep`.`{ColumnExternalPrimaryKey}`");
        }
        if ((flag & (1 << 9)) != 0)
        {
            sb.Append($" LEFT JOIN `{TablePunishments}` AS `pnsh` ON `main`.`{ColumnEntriesPrimaryKey}` = `pnsh`.`{ColumnExternalPrimaryKey}`");
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
        entry.Id = reader.GetUInt32(0);
        entry.Player = reader.GetUInt64(2);
        entry.Message = reader.IsDBNull(3) ? null : reader.GetString(3);
        entry.IsLegacy = reader.GetBoolean(4);
        entry.StartedTimestamp = DateTime.SpecifyKind(reader.GetDateTime(5), DateTimeKind.Utc);
        entry.ResolvedTimestamp = reader.IsDBNull(6) ? null : DateTime.SpecifyKind(reader.GetDateTime(6), DateTimeKind.Utc);
        entry.Reputation = reader.IsDBNull(7) ? 0d : reader.GetDouble(7);
        entry.PendingReputation = reader.IsDBNull(8) ? 0d : reader.GetDouble(8);
        entry.LegacyId = reader.IsDBNull(9) ? null : reader.GetUInt32(9);
        entry.RelevantLogsBegin = reader.IsDBNull(10) ? null : DateTime.SpecifyKind(reader.GetDateTime(10), DateTimeKind.Utc);
        entry.RelevantLogsEnd = reader.IsDBNull(11) ? null : DateTime.SpecifyKind(reader.GetDateTime(11), DateTimeKind.Utc);
        entry.Removed = !reader.IsDBNull(12) && reader.GetBoolean(12);
        entry.RemovedBy = reader.IsDBNull(13) ? null : Actors.GetActor(reader.GetUInt64(13));
        entry.RemovedTimestamp = reader.IsDBNull(14) ? null : DateTime.SpecifyKind(reader.GetDateTime(14), DateTimeKind.Utc);
        entry.RemovedMessage = reader.IsDBNull(15) ? null : reader.GetString(15);

        int offset = 15;
        if ((flag & 1) != 0)
        {
            offset += 5;
            if (entry is DurationPunishment dur)
            {
                long sec = reader.IsDBNull(offset - 4) ? -1L : reader.GetInt64(offset - 4);

                dur.Duration = sec < 0 ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(sec);
                dur.Forgiven = !reader.IsDBNull(offset - 3) && reader.GetBoolean(offset - 3);
                dur.ForgivenBy = reader.IsDBNull(offset - 2) ? null : Actors.GetActor(reader.GetUInt64(offset - 2));
                dur.ForgiveTimestamp = reader.IsDBNull(offset - 1) ? null : new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(offset - 1), DateTimeKind.Utc));
                dur.ForgiveMessage = reader.IsDBNull(offset) ? null : reader.GetString(offset);
            }
        }

        if ((flag & (1 << 1)) != 0)
        {
            ++offset;
            if (entry is Mute mute)
            {
                MuteType? sec = reader.IsDBNull(offset) ? null : reader.ReadStringEnum<MuteType>(offset);
                mute.Type = sec ?? MuteType.None;
            }
        }
        if ((flag & (1 << 2)) != 0)
        {
            ++offset;
            if (entry is Warning warning)
            {
                warning.DisplayedTimestamp = reader.IsDBNull(offset) ? null : new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(offset), DateTimeKind.Utc));
            }
        }
        if ((flag & (1 << 3)) != 0)
        {
            ++offset;
            if (entry is PlayerReportAccepted praccept)
            {
                praccept.ReportKey = reader.IsDBNull(offset) ? PrimaryKey.NotAssigned : reader.GetUInt32(offset);
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
            offset += 5;
            if (entry is Teamkill tk)
            {
                tk.Item = reader.IsDBNull(offset - 4) ? null : reader.ReadGuidString(offset - 4);
                tk.ItemName = reader.IsDBNull(offset - 3) ? null : reader.GetString(offset - 3);
                tk.Cause = reader.IsDBNull(offset - 2) ? null : reader.ReadStringEnum<EDeathCause>(offset - 2);
                tk.Distance = reader.IsDBNull(offset - 1) ? null : reader.GetDouble(offset - 1);
                tk.Limb = reader.IsDBNull(offset) ? null : reader.ReadStringEnum<ELimb>(offset);
            }
        }
        if ((flag & (1 << 6)) != 0)
        {
            offset += 3;
            if (entry is VehicleTeamkill tk)
            {
                tk.Origin = reader.IsDBNull(offset - 2) ? null : reader.ReadStringEnum<EDamageOrigin>(offset - 2);
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
            ++offset;
            if (entry is Report rep)
            {
                ReportType? sec = reader.IsDBNull(offset) ? null : reader.ReadStringEnum<ReportType>(offset);
                rep.Type = sec ?? ReportType.Custom;
            }
        }
        if ((flag & (1 << 9)) != 0)
        {
            offset += 2;
            if (entry is Punishment punishment)
            {
                punishment.PresetType = reader.IsDBNull(offset - 1) ? PresetType.None : reader.ReadStringEnum(offset - 1, PresetType.None);
                punishment.PresetLevel = reader.IsDBNull(offset) ? 0 : reader.GetInt32(offset);
            }
        }

        return entry;
    }
    private static Evidence ReadEvidence(MySqlDataReader reader, int offset)
    {
        return new Evidence(
            reader.IsDBNull(offset) ? PrimaryKey.NotAssigned : reader.GetUInt32(offset),
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
    public async Task AddOrUpdate(IModerationEntry entry, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        PrimaryKey pk = entry.Id;
        bool isNew = !pk.IsValid;
        object[] objs = new object[!isNew ? 16 : 15];
        objs[0] = (ModerationReflection.GetType(entry.GetType()) ?? ModerationEntryType.None).ToString();
        objs[1] = entry.Player;
        objs[2] = (object?)entry.Message.MaxLength(1024) ?? DBNull.Value;
        objs[3] = entry.IsLegacy;
        objs[4] = entry.StartedTimestamp.UtcDateTime;
        objs[5] = entry.ResolvedTimestamp.HasValue ? entry.ResolvedTimestamp.Value.UtcDateTime : DBNull.Value;
        objs[6] = entry.Reputation;
        objs[7] = entry.PendingReputation;
        objs[8] = entry.LegacyId.HasValue ? entry.LegacyId.Value : DBNull.Value;
        objs[9] = entry.RelevantLogsBegin.HasValue ? entry.RelevantLogsBegin.Value.UtcDateTime : DBNull.Value;
        objs[10] = entry.RelevantLogsEnd.HasValue ? entry.RelevantLogsEnd.Value.UtcDateTime : DBNull.Value;
        objs[11] = entry.Removed;
        objs[12] = entry.RemovedBy == null ? DBNull.Value : entry.RemovedBy.Id;
        objs[13] = entry.RemovedTimestamp.HasValue ? entry.RemovedTimestamp.Value.UtcDateTime : DBNull.Value;
        objs[14] = (object?)entry.RemovedMessage ?? DBNull.Value;

        if (!isNew)
            objs[15] = pk.Key;

        string query = F.BuildInitialInsertQuery(TableEntries, ColumnEntriesPrimaryKey, !isNew, null, null,
            ColumnEntriesType, ColumnEntriesSteam64, ColumnEntriesMessage,
            ColumnEntriesIsLegacy, ColumnEntriesStartTimestamp, ColumnEntriesResolvedTimestamp, ColumnEntriesReputation,
            ColumnEntriesPendingReputation, ColumnEntriesLegacyId,
            ColumnEntriesRelavantLogsStartTimestamp, ColumnEntriesRelavantLogsEndTimestamp,
            ColumnEntriesRemoved, ColumnEntriesRemovedBy, ColumnEntriesRemovedTimestamp, ColumnEntriesRemovedReason);

        await Sql.QueryAsync(query, objs, reader =>
        {
            pk = reader.GetUInt32(0);
        }, token).ConfigureAwait(false);

        if (pk.IsValid)
            entry.Id = pk;

        if (entry is not ModerationEntry mod)
            return;

        List<object> args = new List<object>(mod.EstimateParameterCount()) { pk.Key };

        StringBuilder builder = new StringBuilder(82);

        bool hasNewEvidence = mod.AppendWriteCall(builder, args);

        if (!hasNewEvidence)
        {
            await Sql.NonQueryAsync(builder.ToString(), args.ToArray(), token).ConfigureAwait(false);
        }
        else
        {
            await Sql.QueryAsync(builder.ToString(), args.ToArray(), reader =>
            {
                Evidence read = ReadEvidence(reader, 0);
                for (int i = 0; i < mod.Evidence.Length; ++i)
                {
                    ref Evidence existing = ref mod.Evidence[i];
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

        Cache.AddOrUpdate(mod);

        if (isNew)
            UCWarfare.RunOnMainThread(() => OnNewModerationEntryAdded?.Invoke(mod));
        else
            UCWarfare.RunOnMainThread(() => OnModerationEntryUpdated?.Invoke(mod));
    }
    public async Task<int> GetNextPresetLevel(ulong player, PresetType type, CancellationToken token = default)
    {
        if (type == PresetType.None)
            throw new ArgumentException("Preset type can not be None.", nameof(type));

        int max = -1;
        await Sql.QueryAsync($"SELECT MAX(`pnsh`.`{ColumnPunishmentsPresetLevel}`) " +
                             $"FROM `{TableEntries}` as `main` " +
                             $"LEFT JOIN `{TablePunishments}` AS `pnsh` ON `main`.`{ColumnEntriesPrimaryKey}` = `pnsh`.`{ColumnExternalPrimaryKey}` " +
                             $"WHERE `main`.`{ColumnEntriesSteam64}` = @1 AND `pnsh`.`{ColumnPunishmentsPresetType}` = @0 AND `main`.`{ColumnEntriesRemoved}` = 0;", new object[] { type.ToString(), player },
            reader =>
            {
                if (!reader.IsDBNull(0))
                    max = reader.GetInt32(0);

                return true;

            }, token).ConfigureAwait(false);

        if (max == -1)
            return 1;
        
        return max + 1;
    }
    public async Task<ulong[]> GetActorSteam64IDs(IList<IModerationActor> actors, CancellationToken token = default)
    {
        ulong[] steamIds = new ulong[actors.Count];
        bool anyDiscord = false;
        StringBuilder? sb = null;
        for (int i = 0; i < steamIds.Length; ++i)
        {
            IModerationActor actor = actors[i];
            if (actor is DiscordActor)
            {
                sb ??= new StringBuilder("IN (");
                if (anyDiscord)
                    sb.Append(',');
                else anyDiscord = true;
                sb.Append(actor.Id.ToString(CultureInfo.InvariantCulture));
            }

            steamIds[i] = actor.Id;
        }

        if (!anyDiscord)
        {
            for (int k = 0; k < steamIds.Length; ++k)
            {
                if (!Util.IsValidSteam64Id(steamIds[k]))
                    steamIds[k] = 0ul;
            }

            return steamIds;
        }

        await Sql.QueryAsync(
            $"SELECT `{WarfareSQL.ColumnDiscordIdsSteam64}`,`{WarfareSQL.ColumnDiscordIdsDiscordId}` FROM `{WarfareSQL.TableDiscordIds}` WHERE `{WarfareSQL.ColumnDiscordIdsDiscordId}` {sb});",
            null, reader =>
            {
                ulong d = reader.GetUInt64(1);
                for (int j = 0; j < steamIds.Length; ++j)
                {
                    if (d == steamIds[j])
                    {
                        steamIds[j] = reader.GetUInt64(0);
                        break;
                    }
                }
            }, token);

        for (int k = 0; k < steamIds.Length; ++k)
        {
            if (!Util.IsValidSteam64Id(steamIds[k]))
                steamIds[k] = 0ul;
        }

        return steamIds;
    }
    public async Task CacheUsernames(ulong[] players, CancellationToken token = default)
    {
        if (UCWarfare.IsLoaded)
        {
            _ = await Sql.GetUsernamesAsync(players, token);
        }
        else
        {
            PlayerNames[] names = await Sql.GetUsernamesAsync(players, token);
            for (int i = 0; i < names.Length; ++i)
            {
                PlayerNames name = names[i];
                UpdateUsernames(name.Steam64, name);
            }
        }
    }
    public async Task<PlayerIPAddress[]> GetIPAddresses(ulong player, bool removeFiltered, CancellationToken token = default)
    {
        List<PlayerIPAddress> addresses = new List<PlayerIPAddress>(4);

        await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(WarfareSQL.ColumnIPAddressesPrimaryKey,
            WarfareSQL.ColumnIPAddressesSteam64, WarfareSQL.ColumnIPAddressesPackedIP, WarfareSQL.ColumnIPAddressesLoginCount,
            WarfareSQL.ColumnIPAddressesLastLogin, WarfareSQL.ColumnIPAddressesFirstLogin)} FROM `{WarfareSQL.TableIPAddresses}` WHERE `{WarfareSQL.ColumnIPAddressesSteam64}`=@0;",
            new object[] { player }, reader =>
            {
                addresses.Add(new PlayerIPAddress(reader.GetUInt32(0), reader.GetUInt64(1), reader.GetUInt32(2), reader.GetInt32(3),
                    reader.IsDBNull(5) ? null : new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(5), DateTimeKind.Utc)),
                    new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(4), DateTimeKind.Utc))));
            }, token).ConfigureAwait(false);

        IPv4AddressRangeFilter[] remotePlay = RemotePlayAddressFilters;

        for (int i = 0; i < addresses.Count; ++i)
        {
            PlayerIPAddress ipAddr = addresses[i];
            for (int j = 0; j < remotePlay.Length; ++j)
            {
                if (remotePlay[j].IsFiltered(ipAddr.PackedIP))
                {
                    ipAddr.RemotePlay = true;
                    if (removeFiltered)
                    {
                        addresses.RemoveAt(i);
                        --i;
                    }
                    break;
                }
            }
        }

        if (removeFiltered)
        {
            IIPAddressFilter[] filters = NonRemotePlayFilters;

            for (int i = 0; i < filters.Length; ++i)
                await filters[i].RemoveFilteredIPs(addresses, x => x.PackedIP, player, token).ConfigureAwait(false);
        }

        PlayerIPAddress[] addressArray = addresses.ToArray();

        return addressArray;
    }
    public async Task<PlayerHWID[]> GetHWIDs(ulong player, CancellationToken token = default)
    {
        List<PlayerHWID> hwids = new List<PlayerHWID>(9);

        await Sql.QueryAsync($"SELECT {SqlTypes.ColumnList(WarfareSQL.ColumnHWIDsPrimaryKey, WarfareSQL.ColumnHWIDsIndex,
            WarfareSQL.ColumnHWIDsSteam64, WarfareSQL.ColumnHWIDsHWID, WarfareSQL.ColumnHWIDsLoginCount,
            WarfareSQL.ColumnHWIDsLastLogin, WarfareSQL.ColumnHWIDsFirstLogin)} FROM `{WarfareSQL.TableHWIDs}` WHERE `{WarfareSQL.ColumnHWIDsSteam64}`=@0",
            new object[] { player }, reader =>
            {
                hwids.Add(new PlayerHWID(reader.GetUInt32(0), reader.GetInt32(1),
                    reader.GetUInt64(2), HWID.ReadFromDataReader(3, reader), reader.GetInt32(4),
                    reader.IsDBNull(6) ? null : new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(6), DateTimeKind.Utc)),
                    new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(5), DateTimeKind.Utc))));
            }, token).ConfigureAwait(false);

        return hwids.ToArray();
    }
    public bool IsRemotePlay(IPAddress address) => IsRemotePlay(OffenseManager.Pack(address));
    public bool IsRemotePlay(uint address)
    {
        IPv4AddressRangeFilter[] filters = RemotePlayAddressFilters;

        for (int i = 0; i < filters.Length; ++i)
        {
            if (filters[i].IsFiltered(address))
                return true;
        }

        return false;
    }
    public bool IsAnyRemotePlay(IEnumerable<IPAddress> addresses) => IsAnyRemotePlay(addresses.Select(OffenseManager.Pack));
    public bool IsAnyRemotePlay(IEnumerable<uint> addresses)
    {
        IPv4AddressRangeFilter[] filters = RemotePlayAddressFilters;

        foreach (uint addr in addresses)
        {
            for (int i = 0; i < filters.Length; ++i)
            {
                if (filters[i].IsFiltered(addr))
                    return true;
            }
        }

        return false;
    }
    public async Task<bool> IsIPFiltered(IPAddress address, ulong steam64, CancellationToken token = default)
    {
        IIPAddressFilter[] filters = IPAddressFilters;

        for (int i = 0; i < filters.Length; ++i)
        {
            if (await filters[i].IsFiltered(address, steam64, token).ConfigureAwait(false))
                return true;
        }

        return false;
    }
    public Task<StandardErrorCode> WhitelistIP(ulong targetId, ulong callerId, IPv4Range range, DateTimeOffset timestamp, CancellationToken token = default)
        => WhitelistIP(targetId, callerId, range, true, timestamp, token);
    public Task<StandardErrorCode> UnwhitelistIP(ulong targetId, ulong callerId, IPv4Range range, DateTimeOffset timestamp, CancellationToken token = default)
        => WhitelistIP(targetId, callerId, range, false, timestamp, token);
    public async Task<StandardErrorCode> WhitelistIP(ulong targetId, ulong callerId, IPv4Range range, bool add, DateTimeOffset timestamp, CancellationToken token)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (add)
        {
            await Sql.NonQueryAsync(
                $"DELETE FROM `{WarfareSQL.TableIPWhitelists}` WHERE `{WarfareSQL.ColumnIPWhitelistsSteam64}` = @0 AND " +
                $"`{WarfareSQL.ColumnIPWhitelistsIPRange}` = @2; INSERT INTO `{WarfareSQL.TableIPWhitelists}` " +
                $"(`{WarfareSQL.ColumnIPWhitelistsSteam64}`, `{WarfareSQL.ColumnIPWhitelistsAdmin}`, `{WarfareSQL.ColumnIPWhitelistsIPRange}`) VALUES (@0, @1, @2);",
                new object[] { targetId, callerId, range.ToString() }, token).ConfigureAwait(false);
        }
        else
        {

            StandardErrorCode success = (await Sql.NonQueryAsync(
                $"DELETE FROM `{WarfareSQL.TableIPWhitelists}` WHERE `{WarfareSQL.ColumnIPWhitelistsSteam64}` = @0 AND `{WarfareSQL.ColumnIPWhitelistsIPRange}` = @1;",
                new object[] { targetId, range.ToString() }, token).ConfigureAwait(false)) > 0 ? StandardErrorCode.Success : StandardErrorCode.NotFound;

            if (success == StandardErrorCode.NotFound)
                return StandardErrorCode.NotFound;
        }

        if (UCWarfare.IsLoaded)
        {
            ActionLog.Add(ActionLogType.IPWhitelist, $"IP {(add ? "WHITELIST" : "BLACKLIST")} {targetId.ToString(CultureInfo.InvariantCulture)} FOR {range}.", callerId);

            L.Log($"{targetId} was ip {(add ? "whitelisted" : "blacklisted")} by {callerId} on {range}.", ConsoleColor.Cyan);
        }

        return StandardErrorCode.Success;
    }

    public const string TableEntries = "moderation_entries";
    public const string TableActors = "moderation_actors";
    public const string TableEvidence = "moderation_evidence";
    public const string TableRelatedEntries = "moderation_related_entries";
    public const string TableAssetBanTypeFilters = "moderation_asset_ban_filters";
    public const string TablePunishments = "moderation_punishments";
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
    public const string TableReportShotRecords = "moderation_report_shot_record";

    public const string ColumnExternalPrimaryKey = "Entry";
    
    public const string ColumnEntriesPrimaryKey = "Id";
    public const string ColumnEntriesType = "Type";
    public const string ColumnEntriesSteam64 = "Steam64";
    public const string ColumnEntriesMessage = "Message";
    public const string ColumnEntriesIsLegacy = "IsLegacy";
    public const string ColumnEntriesStartTimestamp = "StartTimeUTC";
    public const string ColumnEntriesResolvedTimestamp = "ResolvedTimeUTC";
    public const string ColumnEntriesReputation = "Reputation";
    public const string ColumnEntriesPendingReputation = "PendingReputation";
    public const string ColumnEntriesLegacyId = "LegacyId";
    public const string ColumnEntriesMessageId = "OffenseMessageId";
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

    public const string ColumnRelatedEntry = "RelatedEntry";
    
    public const string ColumnAssetBanFiltersType = "VehicleType";

    public const string ColumnPunishmentsPresetType = "PresetType";
    public const string ColumnPunishmentsPresetLevel = "PresetLevel";

    public const string ColumnDurationsDurationSeconds = "Duration";
    public const string ColumnDurationsForgiven = "Forgiven";
    public const string ColumnDurationsForgivenBy = "ForgivenBy";
    public const string ColumnDurationsForgivenTimestamp = "ForgivenTimeUTC";
    public const string ColumnDurationsForgivenReason = "ForgivenReason";

    public const string ColumnLinkedAppealsAppeal = "LinkedAppeal";
    public const string ColumnLinkedReportsReport = "LinkedReport";

    public const string ColumnMutesType = "MuteType";

    public const string ColumnWarningsDisplayedTimestamp = "Displayed";

    public const string ColumnPlayerReportAcceptedsReport = "AcceptedReport";

    public const string ColumnTableBugReportAcceptedsCommit = "AcceptedCommit";
    public const string ColumnTableBugReportAcceptedsIssue = "AcceptedIssue";

    public const string ColumnTeamkillsDeathCause = "DeathCause";
    public const string ColumnTeamkillsAsset = "Asset";
    public const string ColumnTeamkillsAssetName = "AssetName";
    public const string ColumnTeamkillsLimb = "Limb";
    public const string ColumnTeamkillsDistance = "Distance";

    public const string ColumnVehicleTeamkillsDamageOrigin = "DamageOrigin";
    public const string ColumnVehicleTeamkillsVehicleAsset = "VehicleAsset";
    public const string ColumnVehicleTeamkillsVehicleAssetName = "VehicleAssetName";

    public const string ColumnAppealsTicketId = "TicketId";
    public const string ColumnAppealsState = "State";
    public const string ColumnAppealsDiscordId = "DiscordId";

    public const string ColumnAppealPunishmentsPunishment = "Punishment";

    public const string ColumnAppealResponsesQuestion = "Question";
    public const string ColumnAppealResponsesResponse = "Response";

    public const string ColumnReportsType = "Type";

    public const string ColumnReportsChatRecordsTimestamp = "TimeUTC";
    public const string ColumnReportsChatRecordsMessage = "Message";
    public const string ColumnReportsChatRecordsIndex = "Index";

    public const string ColumnReportsStructureDamageStructure = "Structure";
    public const string ColumnReportsStructureDamageStructureName = "StructureName";
    public const string ColumnReportsStructureDamageStructureOwner = "StructureOwner";
    public const string ColumnReportsStructureDamageStructureType = "StructureType";
    public const string ColumnReportsStructureDamageDamageOrigin = "DamageOrigin";
    public const string ColumnReportsStructureDamageInstanceId = "InstanceId";
    public const string ColumnReportsStructureDamageDamage = "Damage";
    public const string ColumnReportsStructureDamageWasDestroyed = "WasDestroyed";
    public const string ColumnReportsStructureDamageTimestamp = "Timestamp";

    public const string ColumnReportsTeamkillRecordTeamkill = "Teamkill";
    public const string ColumnReportsTeamkillRecordVictim = "Victim";
    public const string ColumnReportsTeamkillRecordDeathCause = "DeathCause";
    public const string ColumnReportsTeamkillRecordMessage = "Message";
    public const string ColumnReportsTeamkillRecordWasIntentional = "WasIntentional";
    public const string ColumnReportsTeamkillRecordTimestamp = "Timestamp";

    public const string ColumnReportsVehicleTeamkillRecordTeamkill = "Teamkill";
    public const string ColumnReportsVehicleTeamkillRecordVictim = "Victim";
    public const string ColumnReportsVehicleTeamkillRecordDamageOrigin = "DamageOrigin";
    public const string ColumnReportsVehicleTeamkillRecordMessage = "Message";
    public const string ColumnReportsVehicleTeamkillRecordTimestamp = "Timestamp";

    public const string ColumnReportsVehicleRequestRecordAsset = "Asset";
    public const string ColumnReportsVehicleRequestRecordVehicle = "Vehicle";
    public const string ColumnReportsVehicleRequestRecordVehicleName = "VehicleName";
    public const string ColumnReportsVehicleRequestRecordRequestTimestamp = "RequestTimeUTC";
    public const string ColumnReportsVehicleRequestRecordDestroyTimestamp = "DestroyTimeUTC";
    public const string ColumnReportsVehicleRequestRecordDamageOrigin = "DamageOrigin";
    public const string ColumnReportsVehicleRequestRecordInstigator = "DamageInstigator";

    public const string ColumnReportsShotRecordAmmo = "Ammo";
    public const string ColumnReportsShotRecordAmmoName = "AmmoName";
    public const string ColumnReportsShotRecordItem = "Item";
    public const string ColumnReportsShotRecordItemName = "ItemName";
    public const string ColumnReportsShotRecordDamageDone = "DamageDone";
    public const string ColumnReportsShotRecordLimb = "Limb";
    public const string ColumnReportsShotRecordIsProjectile = "IsProjectile";
    public const string ColumnReportsShotRecordDistance = "Distance";
    public const string ColumnReportsShotRecordHitPointX = "HitPointX";
    public const string ColumnReportsShotRecordHitPointY = "HitPointY";
    public const string ColumnReportsShotRecordHitPointZ = "HitPointZ";
    public const string ColumnReportsShotRecordShootFromPointX = "ShootFromPointX";
    public const string ColumnReportsShotRecordShootFromPointY = "ShootFromPointY";
    public const string ColumnReportsShotRecordShootFromPointZ = "ShootFromPointZ";
    public const string ColumnReportsShotRecordShootFromRotationX = "ShootFromRotationX";
    public const string ColumnReportsShotRecordShootFromRotationY = "ShootFromRotationY";
    public const string ColumnReportsShotRecordShootFromRotationZ = "ShootFromRotationZ";
    public const string ColumnReportsShotRecordHitType = "HitType";
    public const string ColumnReportsShotRecordHitActor = "HitActor";
    public const string ColumnReportsShotRecordHitAsset = "HitAsset";
    public const string ColumnReportsShotRecordHitAssetName = "HitAssetName";
    public const string ColumnReportsShotRecordTimestamp = "Timestamp";

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
            new Schema.Column(ColumnEntriesPendingReputation, SqlTypes.DOUBLE)
            {
                Nullable = true,
                Default = "'0'"
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
            new Schema.Column(ColumnEntriesRemoved, SqlTypes.BOOLEAN)
            {
                Default = "b'0'"
            },
            new Schema.Column(ColumnEntriesRemovedBy, SqlTypes.STEAM_64)
            {
                Indexed = true,
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
            new Schema.Column(ColumnActorsId, SqlTypes.STEAM_64)
            {
                Indexed = true
            },
            new Schema.Column(ColumnActorsAsAdmin, SqlTypes.BOOLEAN),
            new Schema.Column(ColumnActorsIndex, SqlTypes.INT)
        }, false, typeof(RelatedActor)),
        new Schema(TableRelatedEntries, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnRelatedEntry, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            }
        }, false, typeof(ModerationEntry)),
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
        new Schema(TableAssetBanTypeFilters, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnAssetBanFiltersType, SqlTypes.Enum<VehicleType>())
        }, false, typeof(VehicleType)),
        new Schema(TablePunishments, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true,
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnPunishmentsPresetType, SqlTypes.Enum<PresetType>())
            {
                Indexed = true,
                Nullable = true
            },
            new Schema.Column(ColumnPunishmentsPresetLevel, SqlTypes.INT)
            {
                Indexed = true,
                Nullable = true
            }
        }, false, typeof(Punishment)),
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
            new Schema.Column(ColumnDurationsDurationSeconds, SqlTypes.LONG),
            new Schema.Column(ColumnDurationsForgiven, SqlTypes.BOOLEAN)
            {
                Default = "b'0'"
            },
            new Schema.Column(ColumnDurationsForgivenBy, SqlTypes.STEAM_64)
            {
                Indexed = true,
                Nullable = true
            },
            new Schema.Column(ColumnDurationsForgivenTimestamp, SqlTypes.DATETIME)
            {
                Nullable = true
            },
            new Schema.Column(ColumnDurationsForgivenReason, SqlTypes.String(1024))
            {
                Nullable = true
            }
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
            new Schema.Column(ColumnWarningsDisplayedTimestamp, SqlTypes.DATETIME)
            {
                Indexed = true,
                Nullable = true
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
            new Schema.Column(ColumnVehicleTeamkillsDamageOrigin, SqlTypes.Enum<EDamageOrigin>())
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
                Indexed = true,
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
            new Schema.Column(ColumnReportsChatRecordsTimestamp, SqlTypes.DATETIME),
            new Schema.Column(ColumnReportsChatRecordsIndex, SqlTypes.INT)
        }, false, typeof(AbusiveChatRecord)),
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
            },
            new Schema.Column(ColumnReportsStructureDamageTimestamp, SqlTypes.DATETIME)
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
        }, false, typeof(VehicleRequestRecord)),
        new Schema(TableReportShotRecords, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnReportsShotRecordAmmo, SqlTypes.GUID_STRING),
            new Schema.Column(ColumnReportsShotRecordAmmoName, SqlTypes.String(48)),
            new Schema.Column(ColumnReportsShotRecordItem, SqlTypes.GUID_STRING),
            new Schema.Column(ColumnReportsShotRecordItemName, SqlTypes.String(48)),
            new Schema.Column(ColumnReportsShotRecordDamageDone, SqlTypes.INT),
            new Schema.Column(ColumnReportsShotRecordLimb, SqlTypes.Enum<ELimb>())
            {
                Nullable = true
            },
            new Schema.Column(ColumnReportsShotRecordIsProjectile, SqlTypes.BOOLEAN),
            new Schema.Column(ColumnReportsShotRecordDistance, SqlTypes.DOUBLE)
            {
                Nullable = true
            },
            new Schema.Column(ColumnReportsShotRecordHitPointX, SqlTypes.FLOAT)
            {
                Nullable = true
            },
            new Schema.Column(ColumnReportsShotRecordHitPointY, SqlTypes.FLOAT)
            {
                Nullable = true
            },
            new Schema.Column(ColumnReportsShotRecordHitPointZ, SqlTypes.FLOAT)
            {
                Nullable = true
            },
            new Schema.Column(ColumnReportsShotRecordShootFromPointX, SqlTypes.FLOAT),
            new Schema.Column(ColumnReportsShotRecordShootFromPointY, SqlTypes.FLOAT),
            new Schema.Column(ColumnReportsShotRecordShootFromPointZ, SqlTypes.FLOAT),
            new Schema.Column(ColumnReportsShotRecordShootFromRotationX, SqlTypes.FLOAT),
            new Schema.Column(ColumnReportsShotRecordShootFromRotationY, SqlTypes.FLOAT),
            new Schema.Column(ColumnReportsShotRecordShootFromRotationZ, SqlTypes.FLOAT),
            new Schema.Column(ColumnReportsShotRecordHitType, SqlTypes.Enum<EPlayerHit>()),
            new Schema.Column(ColumnReportsShotRecordHitActor, SqlTypes.STEAM_64)
            {
                Nullable = true
            },
            new Schema.Column(ColumnReportsShotRecordHitAsset, SqlTypes.GUID_STRING)
            {
                Nullable = true
            },
            new Schema.Column(ColumnReportsShotRecordHitAssetName, SqlTypes.String(48))
            {
                Nullable = true
            },
            new Schema.Column(ColumnReportsShotRecordTimestamp, SqlTypes.DATETIME)
        }, false, typeof(ShotRecord))
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
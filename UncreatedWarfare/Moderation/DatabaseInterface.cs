using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Exceptions;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Framework.UI;
using Uncreated.Warfare.Database.Manual;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Moderation.Appeals;
using Uncreated.Warfare.Moderation.Commendation;
using Uncreated.Warfare.Moderation.Punishments;
using Uncreated.Warfare.Moderation.Punishments.Presets;
using Uncreated.Warfare.Moderation.Records;
using Uncreated.Warfare.Moderation.Reports;
using Uncreated.Warfare.Networking;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Steam;
using Uncreated.Warfare.Steam.Models;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles.WarfareVehicles;
using UnityEngine.Networking;

namespace Uncreated.Warfare.Moderation;
public class DatabaseInterface : IHostedService
{
    private readonly object _cacheSync = new object();
    public readonly TimeSpan DefaultInvalidateDuration = TimeSpan.FromSeconds(3);
    private readonly ILogger<DatabaseInterface> _logger;
    private readonly IUserDataService _userDataService;
    private readonly IPlayerService _playerService;
    private readonly Dictionary<ulong, string> _iconUrlCacheSmall = new Dictionary<ulong, string>(128);
    private readonly Dictionary<ulong, string> _iconUrlCacheMedium = new Dictionary<ulong, string>(128);
    private readonly Dictionary<ulong, string> _iconUrlCacheFull = new Dictionary<ulong, string>(128);
    private readonly Dictionary<ulong, PlayerNames> _usernameCache = new Dictionary<ulong, PlayerNames>(128);
    internal IManualMySqlProvider Sql { get; }

    private IIPAddressFilter[]? _nonRemotePlayAddressFilters;
    private IIPAddressFilter[]? _ipAddressFilters;
    private IPv4AddressRangeFilter[]? _remotePlayAddressFilters;

    private static readonly string[] Columns =
    [
        ColumnEntriesPrimaryKey,
        ColumnEntriesType, ColumnEntriesSteam64,
        ColumnEntriesMessage,
        ColumnEntriesIsLegacy, ColumnEntriesStartTimestamp,
        ColumnEntriesResolvedTimestamp, ColumnEntriesReputation,
        ColumnEntriesPendingReputation, ColumnEntriesLegacyId,
        ColumnEntriesRelavantLogsStartTimestamp,
        ColumnEntriesRelavantLogsEndTimestamp,
        ColumnEntriesRemoved, ColumnEntriesRemovedBy,
        ColumnEntriesRemovedTimestamp, ColumnEntriesRemovedReason, ColumnEntriesDiscordMessageId
    ];

    private static readonly string[] ColumnsNoPk =
    [
        ColumnEntriesType, ColumnEntriesSteam64, ColumnEntriesMessage,
        ColumnEntriesIsLegacy, ColumnEntriesStartTimestamp, ColumnEntriesResolvedTimestamp,
        ColumnEntriesReputation, ColumnEntriesPendingReputation, ColumnEntriesLegacyId,
        ColumnEntriesRelavantLogsStartTimestamp, ColumnEntriesRelavantLogsEndTimestamp,
        ColumnEntriesRemoved, ColumnEntriesRemovedBy, ColumnEntriesRemovedTimestamp,
        ColumnEntriesRemovedReason, ColumnEntriesDiscordMessageId
    ];

    private static readonly string[] ShotRecordColumns =
    [
        ColumnExternalPrimaryKey, ColumnReportsShotRecordAmmo, ColumnReportsShotRecordAmmoName,
        ColumnReportsShotRecordItem, ColumnReportsShotRecordItemName,
        ColumnReportsShotRecordDamageDone, ColumnReportsShotRecordLimb,
        ColumnReportsShotRecordIsProjectile, ColumnReportsShotRecordDistance,
        ColumnReportsShotRecordHitPointX, ColumnReportsShotRecordHitPointY, ColumnReportsShotRecordHitPointZ,
        ColumnReportsShotRecordShootFromPointX, ColumnReportsShotRecordShootFromPointY, ColumnReportsShotRecordShootFromPointZ,
        ColumnReportsShotRecordShootFromRotationX, ColumnReportsShotRecordShootFromRotationY, ColumnReportsShotRecordShootFromRotationZ,
        ColumnReportsShotRecordHitType, ColumnReportsShotRecordHitActor,
        ColumnReportsShotRecordHitAsset, ColumnReportsShotRecordHitAssetName,
        ColumnReportsShotRecordTimestamp
    ];

    public IIPAddressFilter[] IPAddressFilters => _ipAddressFilters ??=
    [
        IPv4AddressRangeFilter.GeforceNow,
        IPv4AddressRangeFilter.VKPlay,
        NonRemotePlayFilters[0]
    ];

    public IIPAddressFilter[] NonRemotePlayFilters => _nonRemotePlayAddressFilters ??=
    [
        new MySqlAddressFilter(() => Sql)
    ];

    public IPv4AddressRangeFilter[] RemotePlayAddressFilters => _remotePlayAddressFilters ??=
    [
        IPv4AddressRangeFilter.GeforceNow,
        IPv4AddressRangeFilter.VKPlay
    ];

    public event Action<ModerationEntry>? OnNewModerationEntryAdded;
    public event Action<ModerationEntry>? OnModerationEntryUpdated;
    public ModerationCache Cache { get; } = new ModerationCache();
    internal ISteamApiService SteamAPI { get; }
    public DatabaseInterface(IManualMySqlProvider mySqlProvider, ILogger<DatabaseInterface> logger, ISteamApiService steamApi, IUserDataService userDataService, IPlayerService playerService)
    {
        SteamAPI = steamApi;
        Sql = mySqlProvider;
        _logger = logger;
        _userDataService = userDataService;
        _playerService = playerService;
    }
    /// <inheritdoc />
    async UniTask IHostedService.StartAsync(CancellationToken token)
    {
        // downloads a live list of IPv4 ranges from Geforce Now remote play service.
        using UnityWebRequest downloadGfnIpsRequest = UnityWebRequest.Get("https://ipranges.nvidiangn.net/v1/ips");
        downloadGfnIpsRequest.timeout = 2;

        try
        {
            await downloadGfnIpsRequest.SendWebRequest();
            List<string>? ips = JsonSerializer.Deserialize<GeforceIpList>(downloadGfnIpsRequest.downloadHandler.text)?.IPs;
            if (ips != null)
            {
                List<IPv4Range> ranges = new List<IPv4Range>(ips.Count);
                ranges.AddRange(IPv4AddressRangeFilter.GeforceNow.Ranges);
                int oldCount = ranges.Count;

                foreach (string ip in ips)
                {
                    if (!IPv4Range.TryParse(ip, out IPv4Range range))
                        continue;

                    if (!ranges.Contains(range))
                        ranges.Add(range);
                }

                IPv4AddressRangeFilter.GeforceNow.Ranges = ranges.ToArrayFast();
                _logger.LogDebug("Downloaded {0} GeforceNow IPs (from {1} pre-defined).", ranges.Count, oldCount);
            }
        }
        catch (UnityWebRequestException ex)
        {
            _logger.LogError(ex, "Error querying GeForceNow IPs.");
        }
    }
    private class GeforceIpList
    {
        [JsonPropertyName("ipList")]
        public List<string>? IPs { get; set; }
    }

    /// <inheritdoc />
    UniTask IHostedService.StopAsync(CancellationToken token)
    {
        return UniTask.CompletedTask;
    }

    public bool TryGetAvatar(IModerationActor actor, AvatarSize size, out string avatar)
    {
        if (new CSteamID(actor.Id).GetEAccountType() != EAccountType.k_EAccountTypeIndividual)
        {
            avatar = null!;
            return false;
        }
        return TryGetAvatar(actor.Id, size, out avatar);
    }

    public bool TryGetAvatar(ulong steam64, AvatarSize size, out string avatar)
    {
        WarfarePlayer? online = _playerService.GetOnlinePlayerOrNullThreadSafe(steam64);
        if (online != null)
        {
            avatar = size switch
            {
                AvatarSize.Full => online.SteamSummary.AvatarUrlFull,
                AvatarSize.Medium => online.SteamSummary.AvatarUrlMedium,
                _ => online.SteamSummary.AvatarUrlSmall
            };
        }
        else avatar = null!;

        Dictionary<ulong, string> dict = size switch
        {
            AvatarSize.Full => _iconUrlCacheFull,
            AvatarSize.Medium => _iconUrlCacheMedium,
            _ => _iconUrlCacheSmall
        };

        if (!string.IsNullOrEmpty(avatar))
        {
            dict[steam64] = avatar;
            return true;
        }

        lock (_cacheSync)
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

        lock (_cacheSync)
            dict[steam64] = value;
    }

    public bool TryGetUsernames(IModerationActor actor, out PlayerNames names)
    {
        CSteamID steam64 = new CSteamID(actor.Id);
        if (steam64.GetEAccountType() != EAccountType.k_EAccountTypeIndividual)
        {
            names = default;
            return false;
        }

        return TryGetUsernames(steam64, out names);
    }

    public bool TryGetUsernames(CSteamID steam64, out PlayerNames names)
    {
        lock (_cacheSync)
            return _usernameCache.TryGetValue(steam64.m_SteamID, out names);
    }

    public void UpdateUsernames(CSteamID steam64, PlayerNames names)
    {
        lock (_cacheSync)
            _usernameCache[steam64.m_SteamID] = names;
    }

    public ValueTask<string> GetAvatarAsync(CSteamID steam64, AvatarSize size, bool allowCache = true, CancellationToken token = default)
    {
        if (allowCache && TryGetAvatar(steam64.m_SteamID, size, out string avatar))
        {
            return new ValueTask<string>(avatar);
        }

        return Core(this, steam64.m_SteamID, size, token);

        static async ValueTask<string> Core(DatabaseInterface t, ulong steam64, AvatarSize size, CancellationToken token)
        {
            PlayerSummary summary = await t.SteamAPI.GetPlayerSummaryAsync(steam64, token).ConfigureAwait(false);
            lock (t._cacheSync)
            {
                t._iconUrlCacheSmall[summary.Steam64] = summary.AvatarUrlSmall;
                t._iconUrlCacheMedium[summary.Steam64] = summary.AvatarUrlMedium;
                t._iconUrlCacheFull[summary.Steam64] = summary.AvatarUrlFull;
            }

            return size switch
            {
                AvatarSize.Full => summary.AvatarUrlFull,
                AvatarSize.Medium => summary.AvatarUrlMedium,
                _ => summary.AvatarUrlSmall
            };
        }
    }

    public async Task CacheAvatarsAsync(IEnumerable<ulong> steamIds, AvatarSize size, CancellationToken token = default)
    {
        List<ulong> s64 = steamIds.ToList();

        Dictionary<ulong, string> dict = size switch
        {
            AvatarSize.Full => _iconUrlCacheFull,
            AvatarSize.Medium => _iconUrlCacheMedium,
            _ => _iconUrlCacheSmall
        };

        lock (_cacheSync)
        {
            for (int i = s64.Count - 1; i >= 0; --i)
            {
                if (dict.TryGetValue(s64[i], out _))
                    s64.RemoveAtFast(i);
            }
        }

        if (s64.Count == 0)
            return;

        PlayerSummary[] summaries = await SteamAPI.GetPlayerSummariesAsync(s64, token).ConfigureAwait(false);
        lock (_cacheSync)
        {
            for (int i = 0; i < summaries.Length; ++i)
            {
                PlayerSummary summary = summaries[i];
                _iconUrlCacheSmall[summary.Steam64]  = summary.AvatarUrlSmall;
                _iconUrlCacheMedium[summary.Steam64] = summary.AvatarUrlMedium;
                _iconUrlCacheFull[summary.Steam64]   = summary.AvatarUrlFull;
            }
        }
    }

    public async Task<PlayerNames> GetUsernames(CSteamID id, bool useCache, CancellationToken token = default)
    {
        if (useCache && TryGetUsernames(id, out PlayerNames names))
            return names;

        names = await _userDataService.GetUsernamesAsync(id.m_SteamID, token).ConfigureAwait(false);
        if (names.WasFound)
            UpdateUsernames(id, names);
        return names;
    }

    public async Task<T?> ReadOne<T>(uint id, bool tryGetFromCache, bool detail = true, bool baseOnly = false, CancellationToken token = default) where T : class, IModerationEntry
    {
        if (tryGetFromCache && Cache.TryGet(id, out T val, DefaultInvalidateDuration))
            return val;

        StringBuilder sb = new StringBuilder("SELECT ", 128);
        int flag = AppendReadColumns(sb, typeof(T), baseOnly);
        AppendTables(sb, flag);
        sb.Append($" WHERE `main`.`{ColumnEntriesPrimaryKey}` = @0;");

        object[] pkArgs = { id };
        ModerationEntry? entry = null;
        await Sql.QueryAsync(sb.ToString(), pkArgs, token, reader =>
        {
            entry = ReadEntry(flag, reader);
            return false;
        }).ConfigureAwait(false);

        if (entry == null)
        {
            Cache.TryRemove(id, out _);
            return null;
        }

        await Fill([ entry ], detail, baseOnly, null, token).ConfigureAwait(false);
        
        return entry as T;
    }
    public async Task<T?[]> ReadAll<T>(uint[] ids, bool tryGetFromCache, bool detail = true, bool baseOnly = false, CancellationToken token = default) where T : class, IModerationEntry
    {
        T?[] result = new T?[ids.Length];
        await ReadAll(result, ids, tryGetFromCache, detail, baseOnly, token).ConfigureAwait(false);
        return result;
    }
    public async Task ReadAll<T>(T?[] result, uint[] ids, bool tryGetFromCache, bool detail = true, bool baseOnly = false, CancellationToken token = default) where T : class, IModerationEntry
    {
        if (result.Length != ids.Length)
            throw new ArgumentException("Result must be the same length as ids.", nameof(result));

        StringBuilder sb = new StringBuilder("SELECT ", 164);
        int flag = AppendReadColumns(sb, typeof(T), baseOnly);
        AppendTables(sb, flag);
        sb.Append($" WHERE `main`.`{ColumnEntriesPrimaryKey}` IN (");
        MySqlSnippets.AppendParameterList(sb, 0, ids.Length);
        sb.Append(");");
        
        BitArray? mask = null;
        if (tryGetFromCache)
        {
            bool miss = false;
            for (int i = 0; i < ids.Length; ++i)
            {
                if (Cache.TryGet(ids[i], out T val, DefaultInvalidateDuration))
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

        object[] parameters = new object[ids.Length];
        for (int i = 0; i < parameters.Length; ++i)
            parameters[i] = ids[i];

        await Sql.QueryAsync(sb.ToString(), parameters, token, reader =>
        {
            ModerationEntry? entry = ReadEntry(flag, reader);
            if (entry == null)
                return;
            uint pk = entry.Id;
            int index = -1;
            for (int j = 0; j < ids.Length; ++j)
            {
                if (ids[j] == pk)
                {
                    index = j;
                    break;
                }
            }
            if (index == -1 || entry is not T val)
                return;
            result[index] = val;
        }).ConfigureAwait(false);
        
        // ReSharper disable once CoVariantArrayConversion
        await Fill(result, detail, baseOnly, mask, token).ConfigureAwait(false);
    }
    public async Task<T[]> ReadAll<T>(CSteamID actor, ActorRelationType relation, bool detail = true, bool baseOnly = false, DateTimeOffset? start = null, DateTimeOffset? end = null, string? condition = null, string? orderBy = null, object[]? conditionArgs = null, CancellationToken token = default) where T : IModerationEntry
        => (T[])await ReadAll(typeof(T), actor, relation, detail, baseOnly, start, end, condition, orderBy, conditionArgs, token).ConfigureAwait(false);
    public async Task<Array> ReadAll(Type type, CSteamID actor, ActorRelationType relation, bool detail = true, bool baseOnly = false, DateTimeOffset? start = null, DateTimeOffset? end = null, string? condition = null, string? orderBy = null, object[]? conditionArgs = null, CancellationToken token = default)
    {
        ModerationEntryType[]? types = null;
        if (type != typeof(ModerationEntry) && type != typeof(IModerationEntry))
            ModerationReflection.TryGetInheritance(type, out types);

        StringBuilder sb = new StringBuilder("SELECT ", 164);
        int flag = AppendReadColumns(sb, type, baseOnly);
        AppendTables(sb, flag);
        sb.Append(" WHERE");
        List<object?> args = new List<object?>((types == null ? 0 : types.Length) + (conditionArgs == null ? 0 : conditionArgs.Length) + 3) { actor.m_SteamID };
        if (conditionArgs != null && !string.IsNullOrEmpty(condition))
        {
            args.AddRange(conditionArgs);
            for (int i = 0; i < conditionArgs.Length; ++i)
                condition = UnturnedUIUtility.QuickFormat(condition, "@" + (i + 1).ToString(CultureInfo.InvariantCulture), i);
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
            sb.Append($" AND `main`.`{ColumnEntriesStartTimestamp}` >= @").Append(args.Count.ToString(CultureInfo.InvariantCulture))
                .Append($" AND `main`.`{ColumnEntriesStartTimestamp}` <= @").Append((args.Count + 1).ToString(CultureInfo.InvariantCulture));

            args.Add(start.Value.UtcDateTime);
            args.Add(end.Value.UtcDateTime);
        }
        else if (start.HasValue)
        {
            sb.Append($" AND `main`.`{ColumnEntriesStartTimestamp}` >= @").Append(args.Count.ToString(CultureInfo.InvariantCulture));
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

        if (!string.IsNullOrWhiteSpace(orderBy))
            sb.Append(" ORDER BY ").Append(orderBy);

        sb.Append(';');


        ArrayList entries = new ArrayList(16);
        await Sql.QueryAsync(sb.ToString(), args, token, reader =>
        {
            ModerationEntry? entry = ReadEntry(flag, reader);
            if (type.IsInstanceOfType(entry))
                entries.Add(entry);
        }).ConfigureAwait(false);

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
                condition = UnturnedUIUtility.QuickFormat(condition, "@" + i.ToString(CultureInfo.InvariantCulture), i);
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
            sb.Append($"`main`.`{ColumnEntriesStartTimestamp}` >= @").Append(args.Count.ToString(CultureInfo.InvariantCulture))
                .Append($" AND `main`.`{ColumnEntriesStartTimestamp}` <= @").Append((args.Count + 1).ToString(CultureInfo.InvariantCulture));

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
            sb.Append($"`main`.`{ColumnEntriesStartTimestamp}` >= @").Append(args.Count.ToString(CultureInfo.InvariantCulture));
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
        await Sql.QueryAsync(sb.ToString(), args, token, reader =>
        {
            ModerationEntry? entry = ReadEntry(flag, reader);
            if (type.IsInstanceOfType(entry))
                entries.Add(entry);
        }).ConfigureAwait(false);

        Array rtn = entries.ToArray(type);

        // ReSharper disable once CoVariantArrayConversion
        await Fill((ModerationEntry[])rtn, detail, baseOnly, null, token).ConfigureAwait(false);
        
        return rtn;
    }
    public async Task<T[]> GetEntriesOfLevel<T>(CSteamID player, int level, PresetType type, bool detail = true, bool baseOnly = false, CancellationToken token = default) where T : Punishment
        => (T[])await GetEntriesOfLevel(typeof(T), player, level, type, detail, baseOnly, token).ConfigureAwait(false);
    public async Task<Array> GetEntriesOfLevel(Type type, CSteamID player, int level, PresetType presetType, bool detail = true, bool baseOnly = false, CancellationToken token = default)
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
        args[2] = player.m_SteamID;

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
        await Sql.QueryAsync(sb.ToString(), args, token, reader =>
        {
            ModerationEntry? entry = ReadEntry(flag, reader);
            if (type.IsInstanceOfType(entry))
                entries.Add(entry);
        }).ConfigureAwait(false);

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
            inArg = " = " + entries[0]!.Id.ToString(CultureInfo.InvariantCulture);
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
                sb.Append(e.Id.ToString(CultureInfo.InvariantCulture));
            }

            if (ct == 0)
                return;

            inArg = sb.Append(')').ToString();
        }

        bool anyPunishments = false;
        bool anyAppeals = false;
        bool anyAssetBans = false;
        bool anyGriefingReports = false;
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
                anyGriefingReports = true;
            else if (entry is ChatAbuseReport)
                anyChatAbuseReports = true;
            else if (entry is CheatingReport)
                anyCheatingReports = true;
        }

        // Actors
        string query = $"SELECT {MySqlSnippets.ColumnList(ColumnExternalPrimaryKey, ColumnActorsId, ColumnActorsRole, ColumnActorsAsAdmin)} " +
                $"FROM `{TableActors}` WHERE `{ColumnExternalPrimaryKey}` {inArg} ORDER BY `{ColumnExternalPrimaryKey}`, `{ColumnActorsIndex}`;";

        List<PrimaryKeyPair<RelatedActor>> actors = new List<PrimaryKeyPair<RelatedActor>>();
        await Sql.QueryAsync(query, null, token, reader =>
        {
            actors.Add(new PrimaryKeyPair<RelatedActor>(reader.GetUInt32(0), ReadActor(reader, 1)));
        }).ConfigureAwait(false);

        MySqlSnippets.ApplyQueriedList(actors, (key, arr) =>
        {
            IModerationEntry? info = entries.FindIndexed((x, i) => x != null && (mask is null || mask[i]) && x.Id == key);
            if (info != null)
                info.Actors = arr;
        }, false);

        // Evidence
        query = $"SELECT {MySqlSnippets.ColumnList(ColumnExternalPrimaryKey, ColumnEvidenceId, ColumnEvidenceLink,
            ColumnEvidenceLocalSource, ColumnEvidenceMessage, ColumnEvidenceIsImage,
            ColumnEvidenceTimestamp, ColumnEvidenceActorId)} " +
                $"FROM `{TableEvidence}` WHERE `{ColumnExternalPrimaryKey}` {inArg} ORDER BY `{ColumnExternalPrimaryKey}`, `{ColumnEvidenceId}`;";

        List<PrimaryKeyPair<Evidence>> evidence = new List<PrimaryKeyPair<Evidence>>();
        await Sql.QueryAsync(query, null, token, reader =>
        {
            evidence.Add(new PrimaryKeyPair<Evidence>(reader.GetUInt32(0), ReadEvidence(reader, 1)));
        }).ConfigureAwait(false);

        MySqlSnippets.ApplyQueriedList(evidence, (key, arr) =>
        {
            IModerationEntry? info = entries.FindIndexed((x, i) => x != null && (mask is null || mask[i]) && x.Id == key);
            if (info != null)
                info.Evidence = arr;
        }, false);

        List<PrimaryKeyPair<uint>> links = new List<PrimaryKeyPair<uint>>();

        if (!baseOnly)
        {
            // RelatedEntries
            query = $"SELECT {MySqlSnippets.ColumnList(ColumnExternalPrimaryKey, ColumnRelatedEntry)} FROM `{TableRelatedEntries}` WHERE `{ColumnExternalPrimaryKey}` {inArg} ORDER BY `{ColumnExternalPrimaryKey}`;";
            await Sql.QueryAsync(query, null, token, reader =>
            {
                links.Add(new PrimaryKeyPair<uint>(reader.GetUInt32(0), reader.GetUInt32(1)));
            }).ConfigureAwait(false);

            MySqlSnippets.ApplyQueriedList(links, (key, arr) =>
            {
                IModerationEntry? info = entries.FindIndexed((x, i) => x != null && (mask is null || mask[i]) && x.Id == key);
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
                query = $"SELECT {MySqlSnippets.ColumnList(ColumnExternalPrimaryKey, ColumnLinkedAppealsAppeal)} FROM `{TableLinkedAppeals}` WHERE `{ColumnExternalPrimaryKey}` {inArg} ORDER BY `{ColumnExternalPrimaryKey}`;";
                await Sql.QueryAsync(query, null, token, reader =>
                {
                    links.Add(new PrimaryKeyPair<uint>(reader.GetUInt32(0), reader.GetUInt32(1)));
                }).ConfigureAwait(false);

                MySqlSnippets.ApplyQueriedList(links, (key, arr) =>
                {
                    Punishment? info = (Punishment?)entries.FindIndexed((x, i) => x is Punishment && (mask is null || mask[i]) && x.Id == key);
                    if (info != null)
                        info.AppealKeys = arr;
                }, false);

                links.Clear();

                // Punishment.Reports
                query = $"SELECT {MySqlSnippets.ColumnList(ColumnExternalPrimaryKey, ColumnLinkedReportsReport)} FROM `{TableLinkedReports}` WHERE `{ColumnExternalPrimaryKey}` {inArg} ORDER BY `{ColumnExternalPrimaryKey}`;";
                await Sql.QueryAsync(query, null, token, reader =>
                {
                    links.Add(new PrimaryKeyPair<uint>(reader.GetUInt32(0), reader.GetUInt32(1)));
                }).ConfigureAwait(false);

                MySqlSnippets.ApplyQueriedList(links, (key, arr) =>
                {
                    Punishment? info = (Punishment?)entries.FindIndexed((x, i) => x is Punishment && (mask is null || mask[i]) && x.Id == key);
                    if (info != null)
                        info.ReportKeys = arr;
                }, false);
            }

            if (anyAssetBans)
            {
                List<PrimaryKeyPair<VehicleType>> types = new List<PrimaryKeyPair<VehicleType>>();

                // AssetBan.AssetFilter
                query = $"SELECT {MySqlSnippets.ColumnList(ColumnExternalPrimaryKey, ColumnAssetBanFiltersType)} FROM `{TableAssetBanTypeFilters}` WHERE `{ColumnExternalPrimaryKey}` {inArg} ORDER BY `{ColumnExternalPrimaryKey}`;";
                await Sql.QueryAsync(query, null, token, reader =>
                {
                    types.Add(new PrimaryKeyPair<VehicleType>(reader.GetUInt32(0), reader.ReadStringEnum(1, VehicleType.None)));
                }).ConfigureAwait(false);

                MySqlSnippets.ApplyQueriedList(types, (key, arr) =>
                {
                    AssetBan? info = (AssetBan?)entries.FindIndexed((x, i) => x is AssetBan && (mask is null || mask[i]) && x.Id == key);
                    if (info != null)
                        info.VehicleTypeFilter = arr;
                }, false);
            }
        }

        if (!baseOnly && anyAppeals)
        {
            links.Clear();

            // Appeal.Punishments
            query = $"SELECT {MySqlSnippets.ColumnList(ColumnExternalPrimaryKey, ColumnAppealPunishmentsPunishment)} FROM `{TableAppealPunishments}` WHERE `{ColumnExternalPrimaryKey}` {inArg} ORDER BY `{ColumnExternalPrimaryKey}`;";
            await Sql.QueryAsync(query, null, token, reader =>
            {
                links.Add(new PrimaryKeyPair<uint>(reader.GetUInt32(0), reader.GetUInt32(1)));
            }).ConfigureAwait(false);

            MySqlSnippets.ApplyQueriedList(links, (key, arr) =>
            {
                Appeal? info = (Appeal?)entries.FindIndexed((x, i) => x is Appeal && (mask is null || mask[i]) && x.Id == key);
                if (info != null)
                    info.PunishmentKeys = arr;
            }, false);

            List<PrimaryKeyPair<AppealResponse>> responses = new List<PrimaryKeyPair<AppealResponse>>();

            // Appeal.Responses
            query = $"SELECT {MySqlSnippets.ColumnList(ColumnExternalPrimaryKey, ColumnAppealResponsesQuestion, ColumnAppealResponsesResponse)} " +
                    $"FROM `{TableAppealResponses}` WHERE `{ColumnExternalPrimaryKey}` {inArg} ORDER BY `{ColumnExternalPrimaryKey}`;";
            await Sql.QueryAsync(query, null, token, reader =>
            {
                responses.Add(new PrimaryKeyPair<AppealResponse>(reader.GetUInt32(0), new AppealResponse(reader.GetString(1), reader.GetString(2))));
            }).ConfigureAwait(false);

            MySqlSnippets.ApplyQueriedList(responses, (key, arr) =>
            {
                Appeal? info = (Appeal?)entries.FindIndexed((x, i) => x is Appeal && (mask is null || mask[i]) && x.Id == key);
                if (info != null)
                    info.Responses = arr;
            }, false);
        }

        if (!baseOnly && anyChatAbuseReports)
        {
            List<PrimaryKeyPair<AbusiveChatRecord>> chats = new List<PrimaryKeyPair<AbusiveChatRecord>>();

            // ChatAbuseReport.Messages
            query = $"SELECT {MySqlSnippets.ColumnList(ColumnExternalPrimaryKey, ColumnReportsChatRecordsMessage, ColumnReportsChatRecordsTimestamp)} " +
                    $"FROM `{TableReportChatRecords}` WHERE `{ColumnExternalPrimaryKey}` {inArg} ORDER BY `{ColumnExternalPrimaryKey}`, `{ColumnReportsChatRecordsIndex}`;";
            await Sql.QueryAsync(query, null, token, reader =>
            {
                chats.Add(new PrimaryKeyPair<AbusiveChatRecord>(reader.GetUInt32(0), new AbusiveChatRecord(reader.GetString(1), new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(2), DateTimeKind.Utc)))));
            }).ConfigureAwait(false);

            MySqlSnippets.ApplyQueriedList(chats, (key, arr) =>
            {
                ChatAbuseReport? info = (ChatAbuseReport?)entries.FindIndexed((x, i) => x is ChatAbuseReport && (mask is null || mask[i]) && x.Id == key);
                if (info != null)
                    info.Messages = arr;
            }, false);
        }

        if (!baseOnly && anyCheatingReports)
        {
            List<PrimaryKeyPair<ShotRecord>> shots = new List<PrimaryKeyPair<ShotRecord>>();

            // CheatingReport.Shots
            query = $"SELECT {MySqlSnippets.ColumnList(ShotRecordColumns)} " +
                    $"FROM `{TableReportShotRecords}` WHERE `{ColumnExternalPrimaryKey}` {inArg} ORDER BY `{ColumnExternalPrimaryKey}`;";
            await Sql.QueryAsync(query, null, token, reader =>
            {
                shots.Add(new PrimaryKeyPair<ShotRecord>(reader.GetUInt32(0), new ShotRecord(
                    reader.GetGuid(3),
                    reader.GetGuid(1),
                    reader.IsDBNull(4) ? null : reader.GetString(4),
                    reader.IsDBNull(2) ? null : reader.GetString(2),
                    reader.ReadStringEnum(18, ERaycastInfoType.NONE),
                    reader.IsDBNull(19) ? null : Actors.GetActor(reader.GetUInt64(19)),
                    reader.IsDBNull(20) ? null : reader.GetGuid(20),
                    reader.IsDBNull(21) ? null : reader.GetString(21),
                    reader.ReadStringEnum<ELimb>(6),
                    new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(22), DateTimeKind.Utc)),
                    new Vector3(reader.GetFloat(12), reader.GetFloat(13), reader.GetFloat(14)),
                    new Vector3(reader.GetFloat(15), reader.GetFloat(16), reader.GetFloat(17)),
                    reader.IsDBNull(9) || reader.IsDBNull(10) || reader.IsDBNull(11) ? null : new Vector3(reader.GetFloat(9), reader.GetFloat(10), reader.GetFloat(11)),
                    !reader.IsDBNull(7) && reader.GetBoolean(7),
                    reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                    reader.IsDBNull(8) ? 0d : reader.GetDouble(8)
                )));
            }).ConfigureAwait(false);

            MySqlSnippets.ApplyQueriedList(shots, (key, arr) =>
            {
                CheatingReport? info = (CheatingReport?)entries.FindIndexed((x, i) => x is CheatingReport && (mask is null || mask[i]) && x.Id == key);
                if (info != null)
                    info.Shots = arr;
            }, false);
        }

        if (!baseOnly && anyGriefingReports)
        {
            List<PrimaryKeyPair<StructureDamageRecord>> damages = new List<PrimaryKeyPair<StructureDamageRecord>>();

            // GriefingReport.DamageRecord
            query = $"SELECT {MySqlSnippets.ColumnList(ColumnExternalPrimaryKey, ColumnReportsStructureDamageDamage, ColumnReportsStructureDamageDamageOrigin,
                ColumnReportsStructureDamageInstanceId, ColumnReportsStructureDamageStructure, ColumnReportsStructureDamageStructureName,
                ColumnReportsStructureDamageStructureOwner, ColumnReportsStructureDamageStructureType, ColumnReportsStructureDamageWasDestroyed,
                ColumnReportsStructureDamageTimestamp)} " +
                    $"FROM `{TableReportStructureDamageRecords}` WHERE `{ColumnExternalPrimaryKey}` {inArg} ORDER BY `{ColumnExternalPrimaryKey}`;";
            await Sql.QueryAsync(query, null, token, reader =>
            {
                damages.Add(new PrimaryKeyPair<StructureDamageRecord>(reader.GetUInt32(0),
                    new StructureDamageRecord(reader.ReadGuidStringOrDefault(4), reader.GetString(5), reader.GetUInt64(6),
                        reader.ReadStringEnum(2, EDamageOrigin.Unknown), reader.GetBoolean(7), reader.GetUInt32(3),
                        reader.GetInt32(1), reader.GetBoolean(8), new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(9), DateTimeKind.Utc)))));
            }).ConfigureAwait(false);

            MySqlSnippets.ApplyQueriedList(damages, (key, arr) =>
            {
                GriefingReport? info = (GriefingReport?)entries.FindIndexed((x, i) => x is GriefingReport && (mask is null || mask[i]) && x.Id == key);
                if (info != null)
                    info.DamageRecord = arr;
            }, false);

            List<PrimaryKeyPair<TeamkillRecord>> tks = new List<PrimaryKeyPair<TeamkillRecord>>();

            // GriefingReport.TeamkillRecord
            query = $"SELECT {MySqlSnippets.ColumnList(ColumnExternalPrimaryKey, ColumnReportsTeamkillRecordVictim, ColumnReportsTeamkillRecordDeathCause,
                ColumnReportsTeamkillRecordWasIntentional, ColumnReportsTeamkillRecordTeamkill, ColumnReportsTeamkillRecordMessage, ColumnReportsTeamkillRecordTimestamp)} " +
                    $"FROM `{TableReportTeamkillRecords}` WHERE `{ColumnExternalPrimaryKey}` {inArg} ORDER BY `{ColumnExternalPrimaryKey}`;";
            await Sql.QueryAsync(query, null, token, reader =>
            {
                tks.Add(new PrimaryKeyPair<TeamkillRecord>(reader.GetUInt32(0),
                    new TeamkillRecord(reader.GetUInt32(4), reader.GetUInt64(1), reader.ReadStringEnum(2, EDeathCause.KILL),
                        reader.GetString(5), reader.IsDBNull(3) ? null : reader.GetBoolean(3), new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(6), DateTimeKind.Utc)))));
            }).ConfigureAwait(false);

            MySqlSnippets.ApplyQueriedList(tks, (key, arr) =>
            {
                GriefingReport? info = (GriefingReport?)entries.FindIndexed((x, i) => x is GriefingReport && (mask is null || mask[i]) && x.Id == key);
                if (info != null)
                    info.TeamkillRecord = arr;
            }, false);

            List<PrimaryKeyPair<VehicleTeamkillRecord>> vtks = new List<PrimaryKeyPair<VehicleTeamkillRecord>>();

            // GriefingReport.VehicleTeamkillRecord
            query = $"SELECT {MySqlSnippets.ColumnList(ColumnExternalPrimaryKey, ColumnReportsVehicleTeamkillRecordVictim, ColumnReportsVehicleTeamkillRecordDamageOrigin,
                ColumnReportsVehicleTeamkillRecordTeamkill, ColumnReportsVehicleTeamkillRecordMessage, ColumnReportsVehicleTeamkillRecordTimestamp)} " +
                    $"FROM `{TableReportVehicleTeamkillRecords}` WHERE `{ColumnExternalPrimaryKey}` {inArg} ORDER BY `{ColumnExternalPrimaryKey}`;";
            await Sql.QueryAsync(query, null, token, reader =>
            {
                vtks.Add(new PrimaryKeyPair<VehicleTeamkillRecord>(reader.GetUInt32(0),
                    new VehicleTeamkillRecord(reader.GetUInt32(3), reader.GetUInt64(1), reader.ReadStringEnum(2, EDamageOrigin.Unknown),
                        reader.GetString(4), new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(5), DateTimeKind.Utc)))));
            }).ConfigureAwait(false);

            MySqlSnippets.ApplyQueriedList(vtks, (key, arr) =>
            {
                GriefingReport? info = (GriefingReport?)entries.FindIndexed((x, i) => x is GriefingReport && (mask is null || mask[i]) && x.Id == key);
                if (info != null)
                    info.VehicleTeamkillRecord = arr;
            }, false);

            List<PrimaryKeyPair<VehicleRequestRecord>> reqs = new List<PrimaryKeyPair<VehicleRequestRecord>>();

            // GriefingReport.VehicleRequestRecord
            query = $"SELECT {MySqlSnippets.ColumnList(ColumnExternalPrimaryKey, ColumnReportsVehicleRequestRecordAsset, ColumnReportsVehicleRequestRecordVehicle,
                ColumnReportsVehicleRequestRecordVehicleName, ColumnReportsVehicleRequestRecordInstigator, ColumnReportsVehicleRequestRecordDamageOrigin,
                ColumnReportsVehicleRequestRecordRequestTimestamp, ColumnReportsVehicleRequestRecordDestroyTimestamp)} " +
                    $"FROM `{TableReportVehicleTeamkillRecords}` WHERE `{ColumnExternalPrimaryKey}` {inArg} ORDER BY `{ColumnExternalPrimaryKey}`;";
            await Sql.QueryAsync(query, null, token, reader =>
            {
                reqs.Add(new PrimaryKeyPair<VehicleRequestRecord>(reader.GetUInt32(0),
                    new VehicleRequestRecord(reader.ReadGuidStringOrDefault(2), reader.IsDBNull(1) ? 0u : reader.GetUInt32(1), reader.GetString(3),
                    new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(6), DateTimeKind.Utc)),
                    reader.IsDBNull(7) ? null : new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(7), DateTimeKind.Utc)), reader.ReadStringEnum(5, EDamageOrigin.Unknown), reader.GetUInt64(4))));
            }).ConfigureAwait(false);

            MySqlSnippets.ApplyQueriedList(reqs, (key, arr) =>
            {
                GriefingReport? info = (GriefingReport?)entries.FindIndexed((x, i) => x is GriefingReport && (mask is null || mask[i]) && x.Id == key);
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

            await Task.WhenAll(tasks.ToArrayFast()).ConfigureAwait(false);
        }
    }
    public async Task<T[]> GetActiveEntries<T>(CSteamID steam64, bool detail = true, bool baseOnly = false, string? condition = null, string? orderBy = null, object[]? conditionArgs = null, DateTimeOffset? start = null, DateTimeOffset? end = null, CancellationToken token = default) where T : IDurationModerationEntry
        => (T[])await GetActiveEntries(typeof(T), steam64, detail, baseOnly, condition, orderBy, conditionArgs, start, end, token);
    public Task<Array> GetActiveEntries(Type type, CSteamID steam64, bool detail = true, bool baseOnly = false, string? condition = null, string? orderBy = null, object[]? conditionArgs = null, DateTimeOffset? start = null, DateTimeOffset? end = null, CancellationToken token = default)
    {
        bool dur = typeof(IDurationModerationEntry).IsAssignableFrom(type);
        string cond = (dur
            ? $"(`dur`.`{ColumnDurationsForgiven}` = 0 OR `dur`.`{ColumnDurationsForgiven}` IS NULL) AND "
            : string.Empty) + $"`main`.`{ColumnEntriesRemoved}` = 0";
        if (dur)
        {
            cond += " AND " + MySqlSnippets.BuildCheckDurationClause("dur", "main", ColumnDurationsDurationSeconds, ColumnEntriesResolvedTimestamp, ColumnEntriesStartTimestamp);
        }

        if (condition != null)
            cond += " AND (" + condition + ")";
        
        if (dur)
            orderBy ??= $"(IF(`dur`.`{ColumnDurationsDurationSeconds}` < 0, 2147483647, `dur`.`{ColumnDurationsDurationSeconds}`)) DESC";

        return ReadAll(type, steam64, ActorRelationType.IsTarget, detail, baseOnly, start, end, cond, orderBy, conditionArgs, token: token);
    }
    public async Task<T[]> GetActiveEntries<T>(CSteamID baseSteam64, IReadOnlyList<PlayerIPAddress> addresses, IReadOnlyList<PlayerHWID> hwids, bool detail = true, bool baseOnly = false, string? condition = null, string? orderBy = null, object[]? conditionArgs = null, DateTimeOffset? start = null, DateTimeOffset? end = null, CancellationToken token = default) where T : IDurationModerationEntry
        => (T[])await GetActiveEntries(typeof(T), baseSteam64, addresses, hwids, detail, baseOnly, condition, orderBy, conditionArgs, start, end, token);
    public async Task<Array> GetActiveEntries(Type type, CSteamID baseSteam64, IReadOnlyList<PlayerIPAddress> addresses, IReadOnlyList<PlayerHWID> hwids, bool detail = true, bool baseOnly = false, string? condition = null, string? orderBy = null, object[]? conditionArgs = null, DateTimeOffset? start = null, DateTimeOffset? end = null, CancellationToken token = default)
    {
        bool dur = typeof(IDurationModerationEntry).IsAssignableFrom(type);
        string cond = (dur
            ? $"(`dur`.`{ColumnDurationsForgiven}` = 0 OR `dur`.`{ColumnDurationsForgiven}` IS NULL) AND "
            : string.Empty) + $"`main`.`{ColumnEntriesRemoved}` = 0";
        if (dur)
            cond += " AND " + MySqlSnippets.BuildCheckDurationClause("dur", "main", ColumnDurationsDurationSeconds, ColumnEntriesResolvedTimestamp, ColumnEntriesStartTimestamp);

        if (condition != null)
            cond += " AND (" + condition + ")";

        ModerationEntryType[]? types = null;
        if (type != typeof(ModerationEntry) && type != typeof(IModerationEntry))
            ModerationReflection.TryGetInheritance(type, out types);

        StringBuilder sb = new StringBuilder("SELECT ", 164);
        int flag = AppendReadColumns(sb, type, baseOnly);
        AppendTables(sb, flag);
        sb.Append(" WHERE");
        List<object?> args = new List<object?>((types == null ? 0 : types.Length) + (conditionArgs == null ? 0 : conditionArgs.Length) + 2 + hwids.Count * 2) { baseSteam64.m_SteamID };
        if (conditionArgs != null && !string.IsNullOrEmpty(condition))
        {
            args.AddRange(conditionArgs);
            for (int i = 0; i < conditionArgs.Length; ++i)
                cond = UnturnedUIUtility.QuickFormat(cond, "@" + (i + 1).ToString(CultureInfo.InvariantCulture), i);
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
            sb.Append($"(EXISTS (SELECT * FROM `{TableIPAddresses}` AS `ip` WHERE `ip`.`{ColumnIPAddressesSteam64}`=`main`.`{ColumnEntriesSteam64}`");

            sb.Append($"AND `ip`.`{ColumnIPAddressesPackedIP}` IN (");

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
            sb.Append($"(EXISTS (SELECT * FROM `{TableHWIDs}` AS `hwid` WHERE `hwid`.`{ColumnHWIDsSteam64}`=`main`.`{ColumnEntriesSteam64}`");

            sb.Append($"AND `hwid`.`{ColumnHWIDsHWID}` IN (");

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
        await Sql.QueryAsync(sb.ToString(), args, token, reader =>
        {
            ModerationEntry? entry = ReadEntry(flag, reader);
            if (type.IsInstanceOfType(entry))
                entries.Add(entry);
        }).ConfigureAwait(false);

        Array rtn = entries.ToArray(type);

        // ReSharper disable once CoVariantArrayConversion
        await Fill((ModerationEntry[])rtn, detail, baseOnly, null, token).ConfigureAwait(false);

        return rtn;
    }

    private static readonly string GetActiveAssetBanQuery = $"SELECT {MySqlSnippets.AliasedColumnList("e", Columns)}, " +
                                                            $"{MySqlSnippets.AliasedColumnList("d", ColumnDurationsDurationSeconds, ColumnDurationsForgiven, ColumnDurationsForgivenBy, ColumnDurationsForgivenTimestamp, ColumnDurationsForgivenReason)}, " +
                                                            $"{MySqlSnippets.AliasedColumnList("pnsh", ColumnPunishmentsPresetType, ColumnPunishmentsPresetLevel)}" +
                                                            $"FROM `{TableEntries}` AS `e` " +
                                                            $"LEFT JOIN `{TableDurationPunishments}` AS `d` ON `e`.`{ColumnEntriesPrimaryKey}`=`d`.`{ColumnExternalPrimaryKey}` " +
                                                            $"LEFT JOIN `{TablePunishments}` AS `pnsh` ON `e`.`{ColumnEntriesPrimaryKey}`=`pnsh`.`{ColumnExternalPrimaryKey}` " +
                                                            $"WHERE `e`.`{ColumnEntriesSteam64}` = @0 " +
                                                            $"AND `e`.`{ColumnEntriesType}` = '{nameof(ModerationEntryType.AssetBan)}' " +
                                                            $"AND `d`.`{ColumnDurationsForgiven}` = 0 " +
                                                            $"AND `e`.`{ColumnEntriesRemoved}` = 0 " +
                                                            $"AND {MySqlSnippets.BuildCheckDurationClause("d", "e", ColumnDurationsDurationSeconds, ColumnEntriesResolvedTimestamp, ColumnEntriesStartTimestamp)} " +
                                                            $"AND (NOT EXISTS (SELECT NULL FROM `{TableAssetBanTypeFilters}` AS `a` WHERE `a`.`{ColumnExternalPrimaryKey}`=`e`.`{ColumnEntriesPrimaryKey}`) " +
                                                            $"OR (@1 IN (SELECT `a`.`{ColumnAssetBanFiltersType}` FROM `{TableAssetBanTypeFilters}` AS `a` WHERE `a`.`{ColumnExternalPrimaryKey}`=`e`.`{ColumnEntriesPrimaryKey}`))) " +
                                                            $"ORDER BY (IF(`d`.`{ColumnDurationsDurationSeconds}` < 0, 2147483647, `d`.`{ColumnDurationsDurationSeconds}`)) DESC;";


    public async Task<AssetBan?> GetActiveAssetBan(CSteamID steam64, VehicleType type, bool detail = true, CancellationToken token = default)
    {
        AssetBan? result = null;
        
        await Sql.QueryAsync(GetActiveAssetBanQuery,
            [ steam64.m_SteamID, type.ToString() ],
            token,
            reader =>
            {
                result = ReadEntry(1 | (1 << 10), reader) as AssetBan;
                return false;
            }).ConfigureAwait(false);

        if (detail && result != null)
            await Fill([ result ], true, false, token: token).ConfigureAwait(false);

        return result;
    }
    private int AppendReadColumns(StringBuilder sb, Type type, bool baseOnly)
    {
        sb.Append(MySqlSnippets.AliasedColumnList("main", Columns));
        int flag = 0;
        if (type.IsAssignableFrom(typeof(IDurationModerationEntry)) || typeof(IDurationModerationEntry).IsAssignableFrom(type) || type.IsAssignableFrom(typeof(DurationPunishment)) || typeof(DurationPunishment).IsAssignableFrom(type))
        {
            flag |= 1;
            sb.Append("," + MySqlSnippets.AliasedColumnList("dur", ColumnDurationsDurationSeconds, ColumnDurationsForgiven, ColumnDurationsForgivenBy, ColumnDurationsForgivenTimestamp, ColumnDurationsForgivenReason));
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
            sb.Append("," + MySqlSnippets.AliasedColumnList("braccept", ColumnTableBugReportAcceptedsIssue, ColumnTableBugReportAcceptedsCommit));
        }
        if (!baseOnly && (type.IsAssignableFrom(typeof(Teamkill)) || typeof(Teamkill).IsAssignableFrom(type)))
        {
            flag |= 1 << 5;
            sb.Append("," + MySqlSnippets.AliasedColumnList("tks", ColumnTeamkillsAsset, ColumnTeamkillsAssetName, ColumnTeamkillsDeathCause, ColumnTeamkillsDistance, ColumnTeamkillsLimb));
        }
        if (!baseOnly && (type.IsAssignableFrom(typeof(VehicleTeamkill)) || typeof(VehicleTeamkill).IsAssignableFrom(type)))
        {
            flag |= 1 << 6;
            sb.Append("," + MySqlSnippets.AliasedColumnList("vtks", ColumnVehicleTeamkillsDamageOrigin, ColumnVehicleTeamkillsVehicleAsset, ColumnVehicleTeamkillsVehicleAssetName));
        }
        if (!baseOnly && (type.IsAssignableFrom(typeof(Appeal)) || typeof(Appeal).IsAssignableFrom(type)))
        {
            flag |= 1 << 7;
            sb.Append("," + MySqlSnippets.AliasedColumnList("app", ColumnAppealsState, ColumnAppealsDiscordId, ColumnAppealsTicketId));
        }
        if (type.IsAssignableFrom(typeof(Report)) || typeof(Report).IsAssignableFrom(type))
        {
            flag |= 1 << 8;
            sb.Append("," + MySqlSnippets.AliasedColumnList("rep", ColumnReportsType, ColumnReportsScreenshotData));
            if (type.IsAssignableFrom(typeof(VoiceChatAbuseReport)) || typeof(VoiceChatAbuseReport).IsAssignableFrom(type))
            {
                flag |= 1 << 9;
                sb.Append(",`vrep`.`" + ColumnVoiceChatReportsData + "`");
            }
        }
        if (type.IsAssignableFrom(typeof(Punishment)) || typeof(Punishment).IsAssignableFrom(type))
        {
            flag |= 1 << 10;
            sb.Append("," + MySqlSnippets.AliasedColumnList("pnsh", ColumnPunishmentsPresetType, ColumnPunishmentsPresetLevel));
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
            sb.Append($" LEFT JOIN `{TableVoiceChatReports}` AS `vrep` ON `main`.`{ColumnEntriesPrimaryKey}` = `vrep`.`{ColumnExternalPrimaryKey}`");
        }
        if ((flag & (1 << 10)) != 0)
        {
            sb.Append($" LEFT JOIN `{TablePunishments}` AS `pnsh` ON `main`.`{ColumnEntriesPrimaryKey}` = `pnsh`.`{ColumnExternalPrimaryKey}`");
        }
    }
    private ModerationEntry? ReadEntry(int flag, MySqlDataReader reader)
    {
        ModerationEntryType? type = reader.ReadStringEnum<ModerationEntryType>(1);
        Type? csType = type.HasValue ? ModerationReflection.GetType(type.Value) : null;
        if (csType == null)
        {
            _logger.LogWarning("Invalid type while reading moderation entry: {0}.", reader.GetString(1));
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
        entry.DiscordMessageId = reader.IsDBNull(16) ? 0 : reader.GetUInt64(16);

        int offset = 16;
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
                MuteType? sec = reader.ReadStringEnum<MuteType>(offset);
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
                praccept.ReportKey = reader.IsDBNull(offset) ? 0u : reader.GetUInt32(offset);
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
                tk.Item = reader.ReadGuidString(offset - 4);
                tk.ItemName = reader.IsDBNull(offset - 3) ? null : reader.GetString(offset - 3);
                tk.Cause = reader.ReadStringEnum<EDeathCause>(offset - 2);
                tk.Distance = reader.IsDBNull(offset - 1) ? null : reader.GetDouble(offset - 1);
                tk.Limb = reader.ReadStringEnum<ELimb>(offset);
            }
        }
        if ((flag & (1 << 6)) != 0)
        {
            offset += 3;
            if (entry is VehicleTeamkill tk)
            {
                tk.Origin = reader.ReadStringEnum<EDamageOrigin>(offset - 2);
                tk.Vehicle = reader.ReadGuidString(offset - 1);
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
                app.TicketId = reader.ReadGuidStringOrDefault(offset);
            }
        }
        if ((flag & (1 << 8)) != 0)
        {
            offset += 2;
            if (entry is Report rep)
            {
                ReportType? sec = reader.ReadStringEnum<ReportType>(offset - 1);
                byte[]? imgData = reader.IsDBNull(offset) ? null : reader.ReadByteArray(offset);
                rep.Type = sec ?? ReportType.Custom;
                rep.ScreenshotJpgData = imgData;
            }
        }
        if ((flag & (1 << 9)) != 0)
        {
            ++offset;
            if (entry is VoiceChatAbuseReport vrep)
            {
                byte[] voiceData = reader.ReadByteArray(offset);
                vrep.PreviousVoiceData = voiceData;
            }
        }
        if ((flag & (1 << 10)) != 0)
        {
            offset += 2;
            if (entry is Punishment punishment)
            {
                punishment.PresetType = reader.ReadStringEnum(offset - 1, PresetType.None);
                punishment.PresetLevel = reader.IsDBNull(offset) ? 0 : reader.GetInt32(offset);
            }
        }

        return entry;
    }
    private static Evidence ReadEvidence(MySqlDataReader reader, int offset)
    {
        return new Evidence(
            reader.IsDBNull(offset) ? 0u : reader.GetUInt32(offset),
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
        uint pk = entry.Id;
        bool isNew = pk == 0u;
        object[] objs = new object[!isNew ? 17 : 16];
        objs[0] = (ModerationReflection.GetType(entry.GetType()) ?? ModerationEntryType.None).ToString();
        objs[1] = entry.Player;
        objs[2] = (object?)entry.Message.Truncate(1024) ?? DBNull.Value;
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
        objs[15] = entry.DiscordMessageId;

        if (!isNew)
            objs[16] = pk;

        string query = MySqlSnippets.BuildInitialInsertQuery(TableEntries, ColumnEntriesPrimaryKey, !isNew, null, null, ColumnsNoPk);

        await Sql.QueryAsync(query, objs, token, reader =>
        {
            pk = reader.GetUInt32(0);
        }).ConfigureAwait(false);

        if (pk != 0u)
            entry.Id = pk;

        if (entry is not ModerationEntry mod)
        {
            InvokeSendModerationEntryUpdated(entry.Id, isNew);
            return;
        }

        List<object> args = new List<object>(mod.EstimateParameterCount()) { pk };

        StringBuilder builder = new StringBuilder(82);

        bool hasNewEvidence = mod.AppendWriteCall(builder, args);

        if (!hasNewEvidence)
        {
            await Sql.NonQueryAsync(builder.ToString(), args, CancellationToken.None).ConfigureAwait(false);
        }
        else
        {
            await Sql.QueryAsync(builder.ToString(), args, CancellationToken.None, reader =>
            {
                Evidence read = ReadEvidence(reader, 0);
                for (int i = 0; i < mod.Evidence.Length; ++i)
                {
                    ref Evidence existing = ref mod.Evidence[i];
                    if (existing.Id != 0u)
                    {
                        if (read.Id == existing.Id)
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
            }).ConfigureAwait(false);
        }

        Cache.AddOrUpdate(mod);

        if (WarfareModule.IsActive)
        {
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread(CancellationToken.None);

                if (isNew)
                    OnNewModerationEntryAdded?.Invoke(mod);
                else
                    OnModerationEntryUpdated?.Invoke(mod);
            });
        }
        else
        {
            if (isNew)
                OnNewModerationEntryAdded?.Invoke(mod);
            else
                OnModerationEntryUpdated?.Invoke(mod);
        }

        InvokeSendModerationEntryUpdated(mod.Id, isNew);
    }

    public async Task<int> GetNextPresetLevel(ulong player, PresetType type, CancellationToken token = default)
    {
        if (type == PresetType.None)
            throw new ArgumentException("Preset type can not be None.", nameof(type));

        int max = -1;
        await Sql.QueryAsync($"SELECT MAX(`pnsh`.`{ColumnPunishmentsPresetLevel}`) " +
                             $"FROM `{TableEntries}` as `main` " +
                             $"LEFT JOIN `{TablePunishments}` AS `pnsh` ON `main`.`{ColumnEntriesPrimaryKey}` = `pnsh`.`{ColumnExternalPrimaryKey}` " +
                             $"WHERE `main`.`{ColumnEntriesSteam64}` = @1 AND `pnsh`.`{ColumnPunishmentsPresetType}` = @0 AND `main`.`{ColumnEntriesRemoved}` = 0;", [ type.ToString(), player ],
            token,
            reader =>
            {
                if (!reader.IsDBNull(0))
                    max = reader.GetInt32(0);

                return false;

            }).ConfigureAwait(false);

        if (max == -1)
            return 1;
        
        return max + 1;
    }

    public async Task<ulong[]> GetActorSteam64IDs(IList<IModerationActor> actors, CancellationToken token = default)
    {
        ulong[] steamIds = new ulong[actors.Count];
        int discordCt = 0;
        int index = -1;
        foreach (IModerationActor actor in actors)
        {
            if (actor is DiscordActor)
                ++discordCt;

            steamIds[++index] = actor.Id;
        }

        if (discordCt > 0)
        {
            ulong[] discordIds = new ulong[discordCt];
            discordCt = 0;
            foreach (IModerationActor actor in actors)
            {
                if (actor is not DiscordActor)
                    continue;
                discordIds[discordCt] = actor.Id;
                ++discordCt;
            }

            ulong[] steam64Ids = await _userDataService.GetSteam64sAsync(discordIds, token).ConfigureAwait(false);
            index = -1;
            discordCt = 0;
            foreach (IModerationActor actor in actors)
            {
                ++index;
                if (actor is not DiscordActor)
                    continue;

                steamIds[index] = steam64Ids[discordCt];
                ++discordCt;
            }
        }

        for (int k = 0; k < steamIds.Length; ++k)
        {
            if (new CSteamID(steamIds[k]).GetEAccountType() != EAccountType.k_EAccountTypeIndividual)
                steamIds[k] = 0ul;
        }

        return steamIds;
    }

    public Task<ulong> GetActorSteam64ID(IModerationActor actor, CancellationToken token = default)
    {
        ulong id = actor.Id;
        return actor is DiscordActor
            ? _userDataService.GetSteam64Async(id, token)
            : Task.FromResult(Unsafe.As<ulong, CSteamID>(ref id).GetEAccountType() == EAccountType.k_EAccountTypeIndividual ? id : 0ul);
    }

    public async Task CacheUsernames(ulong[] players, CancellationToken token = default)
    {
        PlayerNames[] names = await _userDataService.GetUsernamesAsync(players, token);
        for (int i = 0; i < names.Length; ++i)
        {
            PlayerNames name = names[i];
            UpdateUsernames(name.Steam64, name);
        }
    }

    private static readonly string GetIPAddressesQuery = $"SELECT {MySqlSnippets.ColumnList(ColumnIPAddressesPrimaryKey,
        ColumnIPAddressesSteam64, ColumnIPAddressesPackedIP, ColumnIPAddressesLoginCount,
        ColumnIPAddressesLastLogin, ColumnIPAddressesFirstLogin)} FROM `{TableIPAddresses}` WHERE `{ColumnIPAddressesSteam64}`=@0;";

    public async Task<PlayerIPAddress[]> GetIPAddresses(CSteamID player, bool removeFiltered, CancellationToken token = default)
    {
        List<PlayerIPAddress> addresses = new List<PlayerIPAddress>(4);

        await Sql.QueryAsync(GetIPAddressesQuery,
            [ player.m_SteamID ], token, reader =>
            {
                addresses.Add(new PlayerIPAddress(reader.GetUInt32(0), reader.GetUInt64(1), reader.GetUInt32(2), reader.GetInt32(3),
                    reader.IsDBNull(5) ? null : new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(5), DateTimeKind.Utc)),
                    new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(4), DateTimeKind.Utc))));
            }).ConfigureAwait(false);

        IPv4AddressRangeFilter[] remotePlay = RemotePlayAddressFilters;

        for (int i = 0; i < addresses.Count; ++i)
        {
            PlayerIPAddress ipAddr = addresses[i];
            for (int j = 0; j < remotePlay.Length; ++j)
            {
                if (!remotePlay[j].IsFiltered(ipAddr.PackedIP))
                    continue;

                ipAddr.RemotePlay = true;
                if (removeFiltered)
                {
                    addresses.RemoveAt(i);
                    --i;
                }
                break;
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

    private static readonly string GetHWIDsQuery = $"SELECT {MySqlSnippets.ColumnList(ColumnHWIDsPrimaryKey, ColumnHWIDsIndex,
        ColumnHWIDsSteam64, ColumnHWIDsHWID, ColumnHWIDsLoginCount,
        ColumnHWIDsLastLogin, ColumnHWIDsFirstLogin)} FROM `{TableHWIDs}` WHERE `{ColumnHWIDsSteam64}`=@0";

    public async Task<PlayerHWID[]> GetHWIDs(CSteamID player, CancellationToken token = default)
    {
        List<PlayerHWID> hwids = new List<PlayerHWID>(9);

        await Sql.QueryAsync(GetHWIDsQuery,
            [ player.m_SteamID ], token, reader =>
            {
                hwids.Add(new PlayerHWID(reader.GetUInt32(0), reader.GetInt32(1),
                    reader.GetUInt64(2), HWID.ReadFromDataReader(3, reader), reader.GetInt32(4),
                    reader.IsDBNull(6) ? null : new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(6), DateTimeKind.Utc)),
                    new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(5), DateTimeKind.Utc))));
            }).ConfigureAwait(false);

        return hwids.ToArray();
    }
    public bool IsRemotePlay(IPAddress address) => IsRemotePlay(IPv4Range.Pack(address));
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
    public bool IsAnyRemotePlay(IEnumerable<IPAddress> addresses) => IsAnyRemotePlay(addresses.Select(IPv4Range.Pack));
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
    public async Task<bool> IsIPFiltered(uint packedIp, CSteamID steam64, CancellationToken token = default)
    {
        IIPAddressFilter[] filters = IPAddressFilters;

        for (int i = 0; i < filters.Length; ++i)
        {
            if (await filters[i].IsFiltered(packedIp, steam64, token).ConfigureAwait(false))
                return true;
        }

        return false;
    }
    public Task<bool> WhitelistIP(CSteamID targetId, CSteamID callerId, IPv4Range range, DateTimeOffset timestamp, CancellationToken token = default)
        => WhitelistIP(targetId, callerId, range, true, timestamp, token);
    public Task<bool> UnwhitelistIP(CSteamID targetId, CSteamID callerId, IPv4Range range, DateTimeOffset timestamp, CancellationToken token = default)
        => WhitelistIP(targetId, callerId, range, false, timestamp, token);
    public async Task<bool> WhitelistIP(CSteamID targetId, CSteamID callerId, IPv4Range range, bool add, DateTimeOffset timestamp, CancellationToken token)
    {
        if (add)
        {
            await Sql.NonQueryAsync(
                $"DELETE FROM `{TableIPWhitelists}` WHERE `{ColumnIPWhitelistsSteam64}` = @0 AND " +
                $"`{ColumnIPWhitelistsIPRange}` = @2; INSERT INTO `{TableIPWhitelists}` " +
                $"(`{ColumnIPWhitelistsSteam64}`, `{ColumnIPWhitelistsAdmin}`, `{ColumnIPWhitelistsIPRange}`) VALUES (@0, @1, @2);",
                [ targetId.m_SteamID, callerId.m_SteamID, range.ToString() ], token).ConfigureAwait(false);
        }
        else
        {

            bool success = await Sql.NonQueryAsync(
                $"DELETE FROM `{TableIPWhitelists}` WHERE `{ColumnIPWhitelistsSteam64}` = @0 AND `{ColumnIPWhitelistsIPRange}` = @1;",
                [ targetId.m_SteamID, range.ToString() ], token).ConfigureAwait(false) > 0;

            if (!success)
                return false;
        }

        if (Provider.isInitialized)
        {
            ActionLog.Add(ActionLogType.IPWhitelist, $"IP {(add ? "WHITELIST" : "BLACKLIST")} {targetId.m_SteamID.ToString(CultureInfo.InvariantCulture)} FOR {range}.", callerId);
        }

        _logger.LogInformation("{0} was ip {1} by {2} on {3}.", targetId, add ? "whitelisted" : "blacklisted", callerId, range);
        return true;
    }

    private void InvokeSendModerationEntryUpdated(uint entryId, bool isNew)
    {
        try
        {
            SendModerationEntryUpdated(entryId, isNew);
        }
        catch (NotImplementedException)
        {
            _logger.LogWarning("Moderation.DatabaseInterface was not created as an RPC service.");
        }
        catch (RpcNoConnectionsException) { }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error sending moderation entry update via RPC.");
        }
    }

    [RpcSend("ReceiveModerationEntryUpdated")]
    protected virtual void SendModerationEntryUpdated(uint entryId, bool isNew) { _ = RpcTask.NotImplemented; }

    [RpcReceive("SendModerationEntryUpdated")]
    private void ReceiveModerationEntryUpdated(uint entryId, bool isNew)
    {
        Task.Run(async () =>
        {
            try
            {
                ModerationEntry? entry = await ReadOne<ModerationEntry>(entryId, tryGetFromCache: false, detail: false, baseOnly: true, CancellationToken.None);

                if (entry == null)
                    return;

                await UniTask.SwitchToMainThread();

                if (isNew)
                    OnNewModerationEntryAdded?.Invoke(entry);
                else
                    OnModerationEntryUpdated?.Invoke(entry);
                // ReadOne auto-caches
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating moderation entry from cache after requested by homebase.");
            }
        });
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
    public const string TableVoiceChatReports = "moderation_voice_chat_reports";
    public const string TableReportStructureDamageRecords = "moderation_report_struct_dmg_records";
    public const string TableReportVehicleRequestRecords = "moderation_report_veh_req_records";
    public const string TableReportTeamkillRecords = "moderation_report_tk_records";
    public const string TableReportVehicleTeamkillRecords = "moderation_report_veh_tk_records";
    public const string TableReportShotRecords = "moderation_report_shot_record";
    public const string TableIPAddresses = "ip_addresses";
    public const string TableHWIDs = "hwids";
    public const string TableIPWhitelists = "ip_whitelists";
    public const string TableBanListWhitelists = "ban_list_whitelists";

    public const string TableUserData = "users";
    public const string ColumnUserDataSteam64 = "Steam64";
    public const string ColumnUserDataPlayerName = "PlayerName";
    public const string ColumnUserDataCharacterName = "CharacterName";
    public const string ColumnUserDataNickName = "NickName";
    public const string ColumnUserDataDisplayName = "DisplayName";
    public const string ColumnUserDataFirstJoined = "FirstJoined";
    public const string ColumnUserDataLastJoined = "LastJoined";
    public const string ColumnUserDataDiscordId = "DiscordId";
    
    [Obsolete] public const string TableUsernames = "usernames";
    [Obsolete] public const string TableDiscordIds = "discordnames";

    [Obsolete] public const string ColumnUsernamesSteam64 = "Steam64";
    [Obsolete] public const string ColumnUsernamesPlayerName = "PlayerName";
    [Obsolete] public const string ColumnUsernamesCharacterName = "CharacterName";
    [Obsolete] public const string ColumnUsernamesNickName = "NickName";

    [Obsolete] public const string ColumnDiscordIdsSteam64 = "Steam64";
    [Obsolete] public const string ColumnDiscordIdsDiscordId = "DiscordID";

    public const string ColumnIPWhitelistsSteam64 = "Steam64";
    public const string ColumnIPWhitelistsIPRange = "IPRange";
    public const string ColumnIPWhitelistsAdmin = "Admin";

    public const string ColumnHWIDsPrimaryKey = "Id";
    public const string ColumnHWIDsIndex = "Index";
    public const string ColumnHWIDsSteam64 = "Steam64";
    public const string ColumnHWIDsHWID = "HWID";
    public const string ColumnHWIDsLoginCount = "LoginCount";
    public const string ColumnHWIDsLastLogin = "LastLogin";
    public const string ColumnHWIDsFirstLogin = "FirstLogin";

    public const string ColumnIPAddressesPrimaryKey = "Id";
    public const string ColumnIPAddressesSteam64 = "Steam64";
    public const string ColumnIPAddressesPackedIP = "Packed";
    public const string ColumnIPAddressesUnpackedIP = "Unpacked";
    public const string ColumnIPAddressesLoginCount = "LoginCount";
    public const string ColumnIPAddressesLastLogin = "LastLogin";
    public const string ColumnIPAddressesFirstLogin = "FirstLogin";


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
    public const string ColumnEntriesDiscordMessageId = "DiscordMessageId";

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
    public const string ColumnReportsScreenshotData = "ScreenshotData";

    public const string ColumnVoiceChatReportsData = "VoiceData";

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

    public const string ColumnBanListWhitelistSteam64 = "Steam64";
    public const string ColumnBanListWhitelistAdmin = "Admin";
    public const string ColumnBanListWhitelistTimestamp = "TimeAddedUTC";
    public const string ColumnBanListWhitelistReason = "Reason";
}

public enum ActorRelationType
{
    IsTarget,
    IsAdminActor,
    IsNonAdminActor,
    IsActor
}
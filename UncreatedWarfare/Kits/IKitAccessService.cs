using DanielWillett.ModularRpcs.Abstractions;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Database.Manual;
using Uncreated.Warfare.Kits.Loadouts;
using Uncreated.Warfare.Logging;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Networking;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Kits;

public interface IKitAccessService
{
    /// <summary>
    /// Check if a player has access to a kit.
    /// </summary>
    /// <param name="steam64">The player's Steam64 ID.</param>
    /// <param name="primaryKey">The primary key of the kit.</param>
    /// <returns><see langword="true"/> if they have access to the kit, otherwise <see langword="false"/>.</returns>
    Task<bool> HasAccessAsync(CSteamID steam64, uint primaryKey, CancellationToken token = default);

    /// <summary>
    /// Get a list of the primary keys of all kits a player owns.
    /// </summary>
    /// <param name="steam64">The player's Steam64 ID.</param>
    Task<IList<uint>> GetOwnedKitKeysAsync(CSteamID steam64, CancellationToken token = default);

    /// <summary>
    /// Get the access type a player has for a kit.
    /// </summary>
    /// <param name="steam64">The player's Steam64 ID.</param>
    /// <param name="primaryKey">The primary key of the kit.</param>
    /// <returns>The type of access if they have access, otherwise <see langword="null"/>.</returns>
    Task<KitAccessType?> GetAccessAsync(CSteamID steam64, uint primaryKey, CancellationToken token = default);

    /// <summary>
    /// Set the access type a player has for a kit.
    /// </summary>
    /// <param name="steam64">The player's Steam64 ID.</param>
    /// <param name="primaryKey">The primary key of the kit.</param>
    /// <param name="access">The access to give the player. If this is <see langword="null"/>, the player's access will be removed</param>
    /// <returns><see langword="true"/> if the player's access was updated, otherwise <see langword="false"/> if no changes were made.</returns>
    Task<bool> UpdateAccessAsync(CSteamID steam64, uint primaryKey, KitAccessType? access, CSteamID instigator, CancellationToken token = default);

    /// <summary>
    /// Set the access type a player has for multiple kits at once.
    /// </summary>
    /// <param name="steam64">The player's Steam64 ID.</param>
    /// <param name="primaryKeys">List of primary keys to apply changes to.</param>
    /// <param name="access">The access to give the player. If this is <see langword="null"/>, the player's access will be removed</param>
    /// <returns><see langword="true"/> if the player's access was updated for at least one kit, otherwise <see langword="false"/> if no changes were made.</returns>
    Task<bool[]> UpdateAccessBulkAsync(CSteamID steam64, uint[] primaryKeys, KitAccessType? access, CSteamID instigator, CancellationToken token = default);
}

public delegate void KitAccessUpdatedHandler(CSteamID steam64, uint kitPrimaryKey, KitAccessType? access);

public class MySqlKitAccessService : IKitAccessService, IDisposable
{
    private string? _updateQuery;
    private string? _bulkUpdateQueryStart;
    private string? _bulkUpdateQueryEnd;

    private readonly ILogger<MySqlKitAccessService> _logger;
    private readonly IKitsDbContext _dbContext;
    private readonly IKitDataStore? _kitDataStore;
    private readonly IPlayerService? _playerService;
    private readonly IRpcConnectionService? _rpcConnectionService;
    private readonly KitSignService? _kitSignService;
    private readonly LoadoutService? _loadoutService;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    /// <summary>
    /// Invoked when a player's kit access is updated remotely or locally.
    /// </summary>
    public event KitAccessUpdatedHandler? PlayerAccessUpdated;

    public MySqlKitAccessService(IServiceProvider serviceProvider, ILogger<MySqlKitAccessService> logger)
    {
        _logger = logger;
        _dbContext = serviceProvider.GetRequiredService<IKitsDbContext>();

        if (WarfareModule.IsActive)
        {
            _playerService = serviceProvider.GetRequiredService<IPlayerService>();
            _kitSignService = serviceProvider.GetRequiredService<KitSignService>();
            _kitDataStore = serviceProvider.GetRequiredService<IKitDataStore>();
            _loadoutService = serviceProvider.GetRequiredService<LoadoutService>();
        }
        else
        {
            _rpcConnectionService = serviceProvider.GetRequiredService<IRpcConnectionService>();
        }

        _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
    }

    /// <inheritdoc />
    public async Task<IList<uint>> GetOwnedKitKeysAsync(CSteamID steam64, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            ulong s64 = steam64.m_SteamID;

            List<uint> kits = new List<uint>(16);
            await foreach (uint kitId in _dbContext.KitAccess
                               .AsNoTracking()
                               .Where(x => x.Steam64 == s64)
                               .Select(x => x.KitId)
                               .AsAsyncEnumerable()
                               .WithCancellation(token)
                               .ConfigureAwait(false))
            {
                kits.Add(kitId);
            }

            return kits;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<KitAccessType?> GetAccessAsync(CSteamID steam64, uint primaryKey, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            ulong s64 = steam64.m_SteamID;

            KitAccessType? type = null;
            await foreach (KitAccessType row in _dbContext.KitAccess
                               .AsNoTracking()
                               .Where(x => x.Steam64 == s64 && x.KitId == primaryKey)
                               .Select(x => x.AccessType)
                               .Take(1)
                               .AsAsyncEnumerable()
                               .WithCancellation(token)
                               .ConfigureAwait(false))
            {
                type = row;
                break;
            }

            if (WarfareModule.IsActive && _playerService?.GetOnlinePlayerThreadSafe(steam64) is { } player)
            {
                KitPlayerComponent? comp = player.ComponentOrNull<KitPlayerComponent>();
                if (type.HasValue)
                {
                    comp?.AddAccessibleKit(primaryKey);
                }
                else
                {
                    comp?.RemoveAccessibleKit(primaryKey);
                }
            }

            return type;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAccessAsync(CSteamID steam64, uint primaryKey, KitAccessType? access, CSteamID instigator, CancellationToken token = default)
    {
        if (!WarfareModule.IsActive && _rpcConnectionService?.TryGetWarfareConnection(out IModularRpcRemoteConnection? connection) is true)
        {
            try
            {
                return await SendUpdateAccess(connection, steam64.m_SteamID, primaryKey, access, token);
            }
            catch (RpcNoConnectionsException) { }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to set access remotely (higher chance of concurrency issues).");
            }
        }

        DateTime now = DateTime.UtcNow;

        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            ulong s64 = steam64.m_SteamID;

            if (access.HasValue)
            {
                if (_updateQuery == null)
                    GenerateUpdateQuery();

                int updated;
                try
                {
                    updated = await _dbContext.Database
                        .ExecuteSqlRawAsync(_updateQuery!, [ primaryKey, steam64.m_SteamID, EnumUtility.GetName(access.Value) ?? access.Value.ToString(), now ], token)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // probably kit not found (foreign key constraint violation)
                    _logger.LogWarning(ex, "Error updating kit access.");
                    updated = 0;
                }

                // update UI and cache
                if (WarfareModule.IsActive && _playerService?.GetOnlinePlayerThreadSafe(steam64) is { } player)
                {
                    KitPlayerComponent kitPlayerComponent = player.Component<KitPlayerComponent>();
                    kitPlayerComponent.AddAccessibleKit(primaryKey);

                    if (_kitSignService != null && _kitDataStore != null && _kitDataStore.CachedKitsByKey.TryGetValue(primaryKey, out Kit? kit))
                    {
                        if (_loadoutService != null && kit.Type == KitType.Loadout)
                            _ = await _loadoutService.GetLoadouts(steam64, KitInclude.Cached, CancellationToken.None).ConfigureAwait(false);
                        _kitSignService.UpdateSigns(kit, player);
                    }
                }

                // invoke local/remote events
                ReceiveAccessUpdated(steam64.m_SteamID, primaryKey, instigator.m_SteamID, access);
                try
                {
                    await SendAccessUpdated(steam64.m_SteamID, primaryKey, instigator.m_SteamID, access).IgnoreNoConnections();
                }
                catch (RpcException ex)
                {
                    _logger.LogError(ex, "Error sending access updated.");
                }

                return updated != 0;
            }
            else
            {
                int updated = await _dbContext.KitAccess
                    .DeleteRangeAsync((DbContext)_dbContext, x => x.Steam64 == s64 && x.KitId == primaryKey, cancellationToken: token)
                    .ConfigureAwait(false);

                // update UI and cache
                if (WarfareModule.IsActive && _playerService?.GetOnlinePlayerThreadSafe(steam64) is { } player)
                {
                    KitPlayerComponent component = player.Component<KitPlayerComponent>();
                    component.RemoveAccessibleKit(primaryKey);
                
                    if (_kitSignService != null && _kitDataStore != null && _kitDataStore.CachedKitsByKey.TryGetValue(primaryKey, out Kit? kit))
                    {
                        if (_loadoutService != null && kit.Type == KitType.Loadout)
                            component.RemoveLoadout(kit.Key);

                        _kitSignService.UpdateSigns(kit, player);
                    }
                }

                // invoke local/remote events
                ReceiveAccessUpdated(steam64.m_SteamID, primaryKey, instigator.m_SteamID, null);
                try
                {
                    await SendAccessUpdated(steam64.m_SteamID, primaryKey, instigator.m_SteamID, null).IgnoreNoConnections();
                }
                catch (RpcException ex)
                {
                    _logger.LogError(ex, "Error sending access removed.");
                }

                return updated != 0;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool[]> UpdateAccessBulkAsync(CSteamID steam64, uint[] primaryKeys, KitAccessType? access, CSteamID instigator, CancellationToken token = default)
    {
        if (primaryKeys.Length == 0)
            return Array.Empty<bool>();
        if (primaryKeys.Length == 1)
            return [ await UpdateAccessAsync(steam64, primaryKeys[0], access, instigator, token).ConfigureAwait(false) ];

        if (!WarfareModule.IsActive && _rpcConnectionService?.TryGetWarfareConnection(out IModularRpcRemoteConnection? connection) is true)
        {
            try
            {
                return await SendUpdateAccessBulk(connection, steam64.m_SteamID, primaryKeys, access, token);
            }
            catch (RpcNoConnectionsException) { }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to set bulk access remotely (higher chance of concurrency issues).");
            }
        }

        DateTime now = DateTime.UtcNow;
        
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            ulong s64 = steam64.m_SteamID;

            if (access.HasValue)
            {
                if (_bulkUpdateQueryStart == null || _bulkUpdateQueryEnd == null)
                    GenerateUpdateQuery();

                // build bulk update query (INSERT ON DUPLICATE KEY UPDATE)
                StringBuilder query = new StringBuilder(_bulkUpdateQueryStart!, _bulkUpdateQueryStart!.Length + _bulkUpdateQueryEnd!.Length + 19 * primaryKeys.Length);
                object[] parameters = new object[primaryKeys.Length + 3];
                parameters[0] = steam64.m_SteamID;
                parameters[1] = EnumUtility.GetName(access.Value) ?? access.Value.ToString();
                parameters[2] = now;
                for (int i = 0; i < primaryKeys.Length; ++i)
                {
                    if (i != 0)
                        query.Append(',');

                    query.Append("({").Append(i + 3).Append("}, {0}, {1}, {2})");
                    parameters[i + 3] = primaryKeys[i];
                }

                query.Append(_bulkUpdateQueryEnd!);

                bool[] mask = new bool[primaryKeys.Length];
                try
                {
                    // attempt bulk update, if it fails do it indivdually
                    await _dbContext.Database
                        .ExecuteSqlRawAsync(query.ToString(), parameters, token)
                        .ConfigureAwait(false);

                    Array.Fill(mask, true);
                }
                catch (Exception ex)
                {
                    // probably kit not found (foreign key constraint violation)
                    _logger.LogWarning(ex, "Error bulk updating kits, falling back to indivdual updates.");

                    // figure out which ones worked
                    await foreach (var row in _dbContext.KitAccess
                                       .AsNoTracking()
                                       .Where(x => x.Steam64 == s64 && primaryKeys.Contains(x.KitId))
                                       .Select(x => new { x.AccessType, x.KitId })
                                       .AsAsyncEnumerable()
                                       .WithCancellation(token)
                                       .ConfigureAwait(false))
                    {
                        int index = Array.IndexOf(primaryKeys, row.KitId);
                        if (index != -1 && row.AccessType == access.Value)
                            mask[index] = true;
                    }

                    for (int i = 0; i < primaryKeys.Length; ++i)
                    {
                        if (mask[i])
                            continue;

                        // this one didn't work, retry
                        try
                        {
                            int updated2 = await _dbContext.Database
                                .ExecuteSqlRawAsync(_updateQuery!, [ primaryKeys[i], steam64.m_SteamID, EnumUtility.GetName(access.Value) ?? access.Value.ToString(), now ], token)
                                .ConfigureAwait(false);

                            mask[i] = updated2 > 0;
                        }
                        catch
                        {
                            // probably kit not found (foreign key constraint violation)
                            mask[i] = false;
                        }
                    }
                }

                // update UI and cache
                if (WarfareModule.IsActive && _playerService?.GetOnlinePlayerThreadSafe(steam64) is { } player)
                {
                    KitPlayerComponent kitPlayerComponent = player.Component<KitPlayerComponent>();

                    bool needsLoadoutUpdate = false;
                    for (int i = 0; i < primaryKeys.Length; i++)
                    {
                        uint primaryKey = primaryKeys[i];
                        if (!mask[i])
                            continue;

                        kitPlayerComponent.AddAccessibleKit(primaryKey);

                        if (_kitSignService != null && _kitDataStore != null &&
                            _kitDataStore.CachedKitsByKey.TryGetValue(primaryKey, out Kit? kit))
                        {
                            if (_loadoutService != null && kit.Type == KitType.Loadout)
                                _ = await _loadoutService
                                    .GetLoadouts(steam64, KitInclude.Cached, CancellationToken.None)
                                    .ConfigureAwait(false);

                            if (kit.Type == KitType.Loadout)
                                needsLoadoutUpdate = true;
                            else
                                _kitSignService.UpdateSigns(kit, player);
                        }
                    }

                    if (needsLoadoutUpdate)
                        _kitSignService!.UpdateLoadoutSigns(player);
                }

                // invoke remote/local events
                ReceiveAccessUpdatedBulk(steam64.m_SteamID, primaryKeys, instigator.m_SteamID, access);
                try
                {
                    await SendAccessUpdatedBulk(steam64.m_SteamID, primaryKeys, instigator.m_SteamID, access).IgnoreNoConnections();
                }
                catch (RpcException ex)
                {
                    _logger.LogError(ex, "Error sending access updated.");
                }

                return mask;
            }
            else
            {
                bool[] mask = new bool[primaryKeys.Length];
                Array.Fill(mask, true);
                await foreach (var row in _dbContext.KitAccess
                                   .AsNoTracking()
                                   .Where(x => x.Steam64 == s64 && primaryKeys.Contains(x.KitId))
                                   .Select(x => new { x.AccessType, x.KitId })
                                   .AsAsyncEnumerable()
                                   .WithCancellation(token)
                                   .ConfigureAwait(false))
                {
                    int index = Array.IndexOf(primaryKeys, row.KitId);
                    if (index != -1)
                        mask[index] = false;
                }

                if (Array.IndexOf(mask, false) == -1)
                    return mask;

                // mass delete
                await _dbContext.KitAccess
                    .DeleteRangeAsync((DbContext)_dbContext, x => x.Steam64 == s64 && primaryKeys.Contains(x.KitId), cancellationToken: token)
                    .ConfigureAwait(false);

                // update UI and cache
                if (WarfareModule.IsActive && _playerService?.GetOnlinePlayerThreadSafe(steam64) is { } player)
                {
                    KitPlayerComponent component = player.Component<KitPlayerComponent>();
                    bool needsLoadoutUpdate = false;
                    foreach (uint primaryKey in primaryKeys)
                    {
                        component.RemoveAccessibleKit(primaryKey);

                        if (_kitSignService != null && _kitDataStore != null && _kitDataStore.CachedKitsByKey.TryGetValue(primaryKey, out Kit? kit))
                        {
                            if (_loadoutService != null && kit.Type == KitType.Loadout)
                                component.RemoveLoadout(kit.Key);

                            if (kit.Type == KitType.Loadout)
                                needsLoadoutUpdate = true;
                            else
                                _kitSignService.UpdateSigns(kit, player);
                        }
                    }

                    if (needsLoadoutUpdate)
                        _kitSignService!.UpdateLoadoutSigns(player);
                }

                // invoke remote/local events
                ReceiveAccessUpdatedBulk(steam64.m_SteamID, primaryKeys, instigator.m_SteamID, null);
                try
                {
                    await SendAccessUpdatedBulk(steam64.m_SteamID, primaryKeys, instigator.m_SteamID, null).IgnoreNoConnections();
                }
                catch (RpcException ex)
                {
                    _logger.LogError(ex, "Error sending access removed.");
                }

                return mask;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> HasAccessAsync(CSteamID steam64, uint primaryKey, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            ulong s64 = steam64.m_SteamID;
            bool access = await _dbContext.KitAccess
                .AsNoTracking()
                .Where(x => x.Steam64 == s64 && x.KitId == primaryKey)
                .AnyAsync(token)
                .ConfigureAwait(false);

            if (WarfareModule.IsActive && _playerService?.GetOnlinePlayerThreadSafe(steam64) is { } player)
            {
                KitPlayerComponent? comp = player.ComponentOrNull<KitPlayerComponent>();
                if (access)
                {
                    comp?.AddAccessibleKit(primaryKey);
                }
                else
                {
                    comp?.RemoveAccessibleKit(primaryKey);
                }
            }

            return access;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void GenerateUpdateQuery()
    {
        IEntityType type = _dbContext.Model.FindEntityType(typeof(KitAccess));

        IProperty kitIdProp = type.FindProperty(typeof(KitAccess).GetProperty(nameof(KitAccess.KitId), BindingFlags.Public | BindingFlags.Instance)!);
        IProperty steam64Prop = type.FindProperty(typeof(KitAccess).GetProperty(nameof(KitAccess.Steam64), BindingFlags.Public | BindingFlags.Instance)!);
        IProperty accessTypeProp = type.FindProperty(typeof(KitAccess).GetProperty(nameof(KitAccess.AccessType), BindingFlags.Public | BindingFlags.Instance)!);
        IProperty timestampProp = type.FindProperty(typeof(KitAccess).GetProperty(nameof(KitAccess.Timestamp), BindingFlags.Public | BindingFlags.Instance)!);

        string kitIdColumn = kitIdProp.GetColumnName(StoreObjectIdentifier.SqlQuery(type));
        string steam64Column = steam64Prop.GetColumnName(StoreObjectIdentifier.SqlQuery(type));
        string accessTypeColumn = accessTypeProp.GetColumnName(StoreObjectIdentifier.SqlQuery(type));
        string timestampColumn = timestampProp.GetColumnName(StoreObjectIdentifier.SqlQuery(type));

        string tableName = type.GetTableName();

        // upsert access and update type if necessary.
        _bulkUpdateQueryStart = $"INSERT INTO `{tableName}` ({MySqlSnippets.ColumnList(kitIdColumn, steam64Column, accessTypeColumn, timestampColumn)}) VALUES ";
        _bulkUpdateQueryEnd = " AS `new` ON DUPLICATE KEY UPDATE " +
                       $"`{tableName}`.`{accessTypeColumn}` = `new`.`{accessTypeColumn}`," +
                       $"`{tableName}`.`{timestampColumn}` = `new`.`{timestampColumn}`;";

        _updateQuery = _bulkUpdateQueryStart + "({0}, {1}, {2}, {3})" + _bulkUpdateQueryEnd;
    }

    void IDisposable.Dispose()
    {
        _semaphore.Dispose();
    }

    [RpcReceive]
    public void ReceiveAccessUpdated(ulong player, uint primaryKey, ulong instigator, KitAccessType? newAccess)
    {
        _logger.LogDebug($"Kit access updated for {player} on kit {primaryKey}: \"{newAccess?.ToString() ?? "no access"}\".");

        CSteamID steamId = new CSteamID(player);
        try
        {
            PlayerAccessUpdated?.Invoke(steamId, primaryKey, newAccess);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error thrown while invoking PlayerAccessUpdated.");
        }

        if (!WarfareModule.IsActive)
            return;

        string kitId;

        if (_kitDataStore != null && _kitDataStore.CachedKitsByKey.TryGetValue(primaryKey, out Kit kit))
            kitId = kit.Id;
        else
            kitId = primaryKey.ToString(CultureInfo.InvariantCulture);

        //if (newAccess.HasValue)
        //    // todo: ActionLog.Add(ActionLogType.ChangeKitAccess, player.ToString(CultureInfo.InvariantCulture) + " GIVEN ACCESS TO " + kitId + ", REASON: " + newAccess.Value, instigator);
        //else
        //    // todo: ActionLog.Add(ActionLogType.ChangeKitAccess, player.ToString(CultureInfo.InvariantCulture) + " DENIED ACCESS TO " + kitId, instigator);
    }

    [RpcReceive]
    public void ReceiveAccessUpdatedBulk(ulong player, uint[] primaryKeys, ulong instigator, KitAccessType? newAccess)
    {
        if (primaryKeys.Length == 0)
            return;

        if (primaryKeys.Length == 1)
        {
            ReceiveAccessUpdated(player, primaryKeys[0], instigator, newAccess);
            return;
        }

        _logger.LogDebug($"Kit access updated for {player} on kits {string.Join(", ", primaryKeys)}: \"{newAccess?.ToString() ?? "no access"}\".");

        CSteamID steamId = new CSteamID(player);
        foreach (uint pk in primaryKeys)
        {
            try
            {
                PlayerAccessUpdated?.Invoke(steamId, pk, newAccess);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error thrown while invoking PlayerAccessUpdated.");
            }

            if (!WarfareModule.IsActive)
                continue;

            string kitId;

            if (_kitDataStore != null && _kitDataStore.CachedKitsByKey.TryGetValue(pk, out Kit kit))
                kitId = kit.Id;
            else
                kitId = pk.ToString(CultureInfo.InvariantCulture);

            //if (newAccess.HasValue)
            //    // todo: ActionLog.Add(ActionLogType.ChangeKitAccess, player.ToString(CultureInfo.InvariantCulture) + " GIVEN ACCESS TO " + kitId + ", REASON: " + newAccess.Value, instigator);
            //else
            //    // todo: ActionLog.Add(ActionLogType.ChangeKitAccess, player.ToString(CultureInfo.InvariantCulture) + " DENIED ACCESS TO " + kitId, instigator);
        }
    }

    [RpcReceive]
    private Task<bool> ReceiveUpdateAccess(ulong player, uint primaryKey, KitAccessType? newAccess, ulong instigator, CancellationToken token = default)
    {
        return UpdateAccessAsync(new CSteamID(player), primaryKey, newAccess, new CSteamID(instigator), token);
    }

    [RpcReceive]
    private Task<bool[]> ReceiveUpdateAccessBulk(ulong player, uint[] primaryKeys, KitAccessType? newAccess, ulong instigator, CancellationToken token = default)
    {
        return UpdateAccessBulkAsync(new CSteamID(player), primaryKeys, newAccess, new CSteamID(instigator), token);
    }

    [RpcSend(nameof(ReceiveUpdateAccess))]
    protected virtual RpcTask<bool> SendUpdateAccess(IModularRpcRemoteConnection connection, ulong player, uint primaryKey, KitAccessType? newAccess, CancellationToken token = default)
    {
        return RpcTask<bool>.NotImplemented;
    }

    [RpcSend(nameof(ReceiveUpdateAccessBulk)), RpcTimeout(1 * Timeouts.Minutes)]
    protected virtual RpcTask<bool[]> SendUpdateAccessBulk(IModularRpcRemoteConnection connection, ulong player, uint[] primaryKeys, KitAccessType? newAccess, CancellationToken token = default)
    {
        return RpcTask<bool[]>.NotImplemented;
    }

    [RpcSend(nameof(ReceiveAccessUpdated)), RpcTimeout(3 * Timeouts.Seconds), RpcFireAndForget]
    protected virtual RpcTask SendAccessUpdated(ulong player, uint primaryKey, ulong instigator, KitAccessType? newAccess)
    {
        return RpcTask.NotImplemented;
    }

    [RpcSend(nameof(ReceiveAccessUpdatedBulk)), RpcTimeout(5 * Timeouts.Seconds), RpcFireAndForget]
    protected virtual RpcTask SendAccessUpdatedBulk(ulong player, uint[] primaryKeys, ulong instigator, KitAccessType? newAccess)
    {
        return RpcTask.NotImplemented;
    }
}
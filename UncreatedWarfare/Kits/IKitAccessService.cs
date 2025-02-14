using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using DanielWillett.ModularRpcs.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Database.Manual;
using Uncreated.Warfare.Kits.Loadouts;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players.Management;

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
    Task<bool> UpdateAccessAsync(CSteamID steam64, uint primaryKey, KitAccessType? access, CancellationToken token = default);
}

public delegate void KitAccessUpdatedHandler(CSteamID steam64, uint kitPrimaryKey, KitAccessType? access);

public class MySqlKitAccessService : IKitAccessService, IDisposable
{
    private string? _updateQuery;

    private readonly ILogger<MySqlKitAccessService> _logger;
    private readonly IKitsDbContext _dbContext;
    private readonly IKitDataStore? _kitDataStore;
    private readonly IPlayerService? _playerService;
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

        _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
    }

    [RpcReceive]
    public void ReceiveAccessUpdated(ulong player, uint primaryKey, KitAccessType? newAccess)
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
    }

    [RpcSend(nameof(ReceiveAccessUpdated)), RpcTimeout(3 * Timeouts.Seconds)]
    protected virtual RpcTask SendAccessUpdated(ulong player, uint primaryKey, KitAccessType? newAccess)
    {
        return RpcTask.CompletedTask;
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
    public async Task<bool> UpdateAccessAsync(CSteamID steam64, uint primaryKey, KitAccessType? access, CancellationToken token = default)
    {
        DateTime now = DateTime.UtcNow;

        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            ulong s64 = steam64.m_SteamID;

            if (access.HasValue)
            {
                if (_updateQuery == null)
                    GenerateUpdateQuery();

                int updated = await _dbContext.Database
                    .ExecuteSqlRawAsync(_updateQuery!, [ primaryKey, steam64.m_SteamID, access.Value, now ], token)
                    .ConfigureAwait(false);

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

                ReceiveAccessUpdated(steam64.m_SteamID, primaryKey, access);
                try
                {
                    await SendAccessUpdated(steam64.m_SteamID, primaryKey, access);
                }
                catch (RpcNoConnectionsException) { }
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

                ReceiveAccessUpdated(steam64.m_SteamID, primaryKey, null);
                try
                {
                    await SendAccessUpdated(steam64.m_SteamID, primaryKey, null);
                }
                catch (RpcNoConnectionsException) { }
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
        _updateQuery = $"INSERT INTO `{tableName}` ({MySqlSnippets.ColumnList(kitIdColumn, steam64Column, accessTypeColumn, timestampColumn)}) " +
                       "VALUES ({0}, {1}, {2}, {3}) AS `new` " +
                       "ON DUPLICATE KEY UPDATE " +
                       $"`{tableName}`.`{accessTypeColumn}` = `new`.`{accessTypeColumn}`," +
                       $"`{tableName}`.`{timestampColumn}` = `new`.`{timestampColumn}`;";
    }

    void IDisposable.Dispose()
    {
        _semaphore.Dispose();
    }
}
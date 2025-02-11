using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Linq;
using System.Reflection;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Database.Manual;
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

public class MySqlKitAccessService : IKitAccessService, IDisposable
{
    private string? _updateQuery;

    private readonly IKitsDbContext _dbContext;
    private readonly IPlayerService? _playerService;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    public MySqlKitAccessService(IKitsDbContext dbContext, IPlayerService? playerService = null)
    {
        _dbContext = dbContext;
        _playerService = playerService;
        _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
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

            if (_playerService?.GetOnlinePlayerThreadSafe(steam64) is { } player)
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
                    .ExecuteSqlRawAsync(_updateQuery!, primaryKey, steam64, access.Value, now, token)
                    .ConfigureAwait(false);

                if (_playerService?.GetOnlinePlayerThreadSafe(steam64) is { } player)
                {
                    player.ComponentOrNull<KitPlayerComponent>()?.AddAccessibleKit(primaryKey);
                }

                return updated != 0;
            }
            else
            {
                int updated = await _dbContext.KitAccess
                    .DeleteRangeAsync((DbContext)_dbContext, x => x.Steam64 == s64 && x.KitId == primaryKey, cancellationToken: token)
                    .ConfigureAwait(false);

                if (_playerService?.GetOnlinePlayerThreadSafe(steam64) is { } player)
                {
                    player.ComponentOrNull<KitPlayerComponent>()?.RemoveAccessibleKit(primaryKey);
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

            if (_playerService?.GetOnlinePlayerThreadSafe(steam64) is { } player)
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
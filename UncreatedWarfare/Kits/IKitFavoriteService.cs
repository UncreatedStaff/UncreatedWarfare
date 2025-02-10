using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using System;
using System.Linq;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players.Management;
using Z.EntityFramework.Plus;

namespace Uncreated.Warfare.Kits;

public interface IKitFavoriteService
{
    /// <summary>
    /// Check if a kit is favorited.
    /// </summary>
    Task<bool> IsFavorited(CSteamID player, uint kitPrimaryKey, CancellationToken token = default);

    /// <summary>
    /// Favorite a kit if it isn't already.
    /// </summary>
    Task<bool> AddFavorite(CSteamID player, uint kitPrimaryKey, CancellationToken token = default);

    /// <summary>
    /// Remove a favorite on a kit if it's favorited.
    /// </summary>
    Task<bool> RemoveFavorite(CSteamID player, uint kitPrimaryKey, CancellationToken token = default);
}

public class MySqlKitFavoriteService : IKitFavoriteService, IDisposable
{
    private readonly IKitsDbContext _dbContext;
    private readonly IPlayerService? _playerService;
    private readonly SemaphoreSlim _semaphore;

    public MySqlKitFavoriteService(IKitsDbContext dbContext, IPlayerService? playerService = null)
    {
        _dbContext = dbContext;
        _playerService = playerService;
        _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;

        _semaphore = new SemaphoreSlim(1, 1);
    }

    /// <inheritdoc />
    public async Task<bool> IsFavorited(CSteamID player, uint kitPrimaryKey, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            ulong s64 = player.m_SteamID;

            if (await _dbContext.KitFavorites
                .AsNoTracking()
                .Where(x => x.Steam64 == s64 && x.KitId == kitPrimaryKey)
                .AnyAsync(token)
                .ConfigureAwait(false))
            {
                if (_playerService?.GetOnlinePlayerThreadSafe(player) is { } onlinePlayer)
                {
                    onlinePlayer.ComponentOrNull<KitPlayerComponent>()?.AddFavoriteKit(kitPrimaryKey);
                }

                return true;
            }
            else if (_playerService?.GetOnlinePlayerThreadSafe(player) is { } onlinePlayer)
            {
                onlinePlayer.ComponentOrNull<KitPlayerComponent>()?.RemoveFavoriteKit(kitPrimaryKey);
            }

            return false;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> AddFavorite(CSteamID player, uint kitPrimaryKey, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            bool success;
            try
            {
                KitFavorite favorite = new KitFavorite
                {
                    KitId = kitPrimaryKey,
                    Steam64 = player.m_SteamID
                };
                
                _dbContext.KitFavorites.Add(favorite);

                await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);

                success = true;
            }
            catch (DbUpdateException ex) when (ex.GetBaseException() is MySqlException { ErrorCode: MySqlErrorCode.DuplicateKeyEntry })
            {
                // handle non-unique primary key
                success = false;
            }
            finally
            {
                _dbContext.ChangeTracker.Clear();
            }

            if (_playerService?.GetOnlinePlayerThreadSafe(player) is { } onlinePlayer)
            {
                onlinePlayer.ComponentOrNull<KitPlayerComponent>()?.AddFavoriteKit(kitPrimaryKey);
            }

            return success;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> RemoveFavorite(CSteamID player, uint kitPrimaryKey, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            ulong s64 = player.m_SteamID;
            int change = await _dbContext.KitFavorites
                .AsNoTracking()
                .Where(x => x.Steam64 == s64 && x.KitId == kitPrimaryKey)
                .DeleteAsync(token)
                .ConfigureAwait(false);

            await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);

            if (_playerService?.GetOnlinePlayerThreadSafe(player) is { } onlinePlayer)
            {
                onlinePlayer.ComponentOrNull<KitPlayerComponent>()?.RemoveFavoriteKit(kitPrimaryKey);
            }

            return change > 0;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
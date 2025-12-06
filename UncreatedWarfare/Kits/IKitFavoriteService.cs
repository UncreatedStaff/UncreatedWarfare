using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using System;
using System.Linq;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Kits;

public interface IKitFavoriteService
{
    /// <summary>
    /// Invoked when a kit's favorite status is updated for a player.
    /// </summary>
    event Action<CSteamID, uint, bool>? OnFavoriteStatusUpdated;

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

[GenerateRpcSource]
public partial class MySqlKitFavoriteService : IKitFavoriteService, IDisposable
{
    private readonly IKitsDbContext _dbContext;
    private readonly IKitDataStore? _kitDataStore;
    private readonly IPlayerService? _playerService;
    private readonly SemaphoreSlim _semaphore;
    private readonly KitSignService? _kitSignService;
    private readonly ILogger<MySqlKitFavoriteService> _logger;

    public event Action<CSteamID, uint, bool>? OnFavoriteStatusUpdated;

    public MySqlKitFavoriteService(IServiceProvider serviceProvider, ILogger<MySqlKitFavoriteService> logger)
    {
        _logger = logger;
        _dbContext = serviceProvider.GetRequiredService<IKitsDbContext>();

        if (WarfareModule.IsActive)
        {
            _playerService = serviceProvider.GetService<IPlayerService>();
            _kitSignService = serviceProvider.GetService<KitSignService>();
            _kitDataStore = serviceProvider.GetService<IKitDataStore>();
        }

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
                    KitPlayerComponent? comp = onlinePlayer.ComponentOrNull<KitPlayerComponent>();
                    if (comp != null && comp.AddFavoriteKit(kitPrimaryKey))
                    {
                        InvokeFavoriteUpdatedAndFireRemote(player.m_SteamID, kitPrimaryKey, isFavorited: true);
                    }
                }

                return true;
            }
            else if (_playerService?.GetOnlinePlayerThreadSafe(player) is { } onlinePlayer)
            {
                KitPlayerComponent? comp = onlinePlayer.ComponentOrNull<KitPlayerComponent>();
                if (comp != null && comp.RemoveFavoriteKit(kitPrimaryKey))
                {
                    InvokeFavoriteUpdatedAndFireRemote(player.m_SteamID, kitPrimaryKey, isFavorited: false);
                }
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

            InvokeFavoriteUpdatedAndFireRemote(player.m_SteamID, kitPrimaryKey, isFavorited: true);

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
                .DeleteRangeAsync((DbContext)_dbContext, x => x.Steam64 == s64 && x.KitId == kitPrimaryKey, cancellationToken: token)
                .ConfigureAwait(false);

            await _dbContext.SaveChangesAsync(token).ConfigureAwait(false);

            InvokeFavoriteUpdatedAndFireRemote(player.m_SteamID, kitPrimaryKey, isFavorited: false);

            return change > 0;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    [RpcSend(nameof(InvokeFavoriteUpdated)), RpcFireAndForget]
    private partial void RemoteInvokeFavoriteUpdated(ulong steam64, uint kit, bool isFavorited);

    [RpcReceive]
    private void InvokeFavoriteUpdated(ulong player, uint kitPrimaryKey, bool isFavorited)
    {
        try
        {
            OnFavoriteStatusUpdated?.Invoke(new CSteamID(player), kitPrimaryKey, isFavorited);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking OnFavoriteStatusUpdated.");
        }

        if (!WarfareModule.IsActive || _playerService?.GetOnlinePlayerThreadSafe(player) is not { } onlinePlayer)
            return;
        
        KitPlayerComponent component = onlinePlayer.Component<KitPlayerComponent>();
        component.AddFavoriteKit(kitPrimaryKey);
        if (_kitSignService == null || _kitDataStore == null || !_kitDataStore.CachedKitsByKey.TryGetValue(kitPrimaryKey, out Kit? value))
            return;

        if (value.Type == KitType.Loadout)
        {
            component.UpdateLoadout(value);
        }

        _kitSignService.UpdateSigns(value, onlinePlayer);
    }

    private void InvokeFavoriteUpdatedAndFireRemote(ulong steam64, uint kit, bool isFavorited)
    {
        InvokeFavoriteUpdated(steam64, kit, isFavorited);
        try
        {
            RemoteInvokeFavoriteUpdated(steam64, kit, isFavorited);
        }
        catch (RpcNoConnectionsException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replicating favorite update to remote.");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
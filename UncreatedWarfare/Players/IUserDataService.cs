using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Uncreated.Warfare.Database.Abstractions;
using Uncreated.Warfare.Models.Users;
using Uncreated.Warfare.Moderation;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Players;

public interface IUserDataService
{
    /// <summary>
    /// Get a player's Discord ID, or 0 if their Discord is not linked.
    /// </summary>
    Task<ulong> GetDiscordIdAsync(ulong steam64, CancellationToken token = default);

    /// <summary>
    /// Get a player's Steam64 ID from their Discord ID, or 0 if their Discord is not linked.
    /// </summary>
    Task<ulong> GetSteam64Async(ulong discordId, CancellationToken token = default);

    /// <summary>
    /// Get a set of players' Discord IDs, or 0 if their Discord is not linked.
    /// </summary>
    /// <returns>An array in the same order as the input, with 0s in the place of unlinked IDs.</returns>
    Task<ulong[]> GetDiscordIdsAsync(IReadOnlyList<ulong> steam64s, CancellationToken token = default);

    /// <summary>
    /// Get a player's Steam64 ID from their Discord ID, or 0 if their Discord is not linked.
    /// </summary>
    /// <returns>An array in the same order as the input, with 0s in the place of unlinked IDs.</returns>
    Task<ulong[]> GetSteam64sAsync(IReadOnlyList<ulong> discordIds, CancellationToken token = default);

    /// <summary>
    /// Get a player's usernames from their Steam64 ID.
    /// </summary>
    Task<PlayerNames> GetUsernamesAsync(ulong steam64, CancellationToken token = default);

    /// <summary>
    /// Get a single player's user data if they've joined.
    /// </summary>
    Task<WarfareUserData?> ReadAsync(ulong steam64, CancellationToken token = default);
    
    /// <summary>
    /// Get multiple players' user data in bulk if they've joined. Not necessarily returned in the same order as it was inputted.
    /// </summary>
    Task<IReadOnlyList<WarfareUserData>> ReadAsync([InstantHandle] IEnumerable<ulong> steam64, CancellationToken token = default);

    /// <summary>
    /// Get a single player's user data if they've joined from their Discord ID if it's linked.
    /// </summary>
    Task<WarfareUserData?> ReadFromDiscordIdAsync(ulong discordId, CancellationToken token = default);

    /// <summary>
    /// Update a <see cref="WarfareUserData"/> object. Any added HWIDs and IP addresses need to be added using the <see cref="IDbContext"/> argument and any updated need to be updated using it.
    /// </summary>
    Task<WarfareUserData> AddOrUpdateAsync(ulong steam64, [InstantHandle] Action<WarfareUserData, IDbContext> update, CancellationToken token = default);

    /// <summary>
    /// Search all players that have ever joined by their usernames, and add them to a collection as they're found.
    /// </summary>
    /// <param name="prioritizedName">The type of name to prioritize.</param>
    /// <param name="byLastJoined">If they should be ordered by the last time they joined. Exact matches will still be prioritized.</param>
    /// <param name="output">Collection to add matches to.</param>
    /// <param name="limit">Maximum number of names to find.</param>
    /// <returns>Number of results.</returns>
    Task<int> SearchPlayersAsync(string input, PlayerNameType prioritizedName, bool byLastJoined, ICollection<PlayerNames> output, int limit = -1, CancellationToken token = default);

    /// <summary>
    /// Search all players that have ever joined by their usernames, and return the best match.
    /// </summary>
    /// <param name="prioritizedName">The type of name to prioritize.</param>
    /// <param name="byLastJoined">If they should be ordered by the last time they joined. Exact matches will still be prioritized.</param>
    Task<PlayerNames> SearchFirstPlayerAsync(string input, PlayerNameType prioritizedName, bool byLastJoined, CancellationToken token = default);
}

public class UserDataService : IUserDataService, IDisposable
{
    private readonly IUserDataDbContext _dbContext;
    private readonly SemaphoreSlim _semaphore;
 
    public UserDataService(IUserDataDbContext dbContext)
    {
        _dbContext = dbContext;
        _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
        _semaphore = new SemaphoreSlim(1, 1);
    }

    private IQueryable<WarfareUserData> Set()
    {
        return _dbContext.UserData.Include(x => x.HWIDs).Include(x => x.IPAddresses);
    }

    /// <inheritdoc />
    public async Task<ulong> GetDiscordIdAsync(ulong steam64, CancellationToken token = default)
    {
        if (steam64 == 0)
            return 0;

        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            return await _dbContext.UserData.Where(x => x.Steam64 == steam64).Select(x => x.DiscordId).FirstOrDefaultAsync(token).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<ulong> GetSteam64Async(ulong discordId, CancellationToken token = default)
    {
        if (discordId == 0)
            return 0;

        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            return await _dbContext.UserData.Where(x => x.DiscordId == discordId).Select(x => x.Steam64).FirstOrDefaultAsync(token).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<ulong[]> GetDiscordIdsAsync(IReadOnlyList<ulong> steam64s, CancellationToken token = default)
    {
        ulong[]? steamIdArray = steam64s as ulong[];
        if (steamIdArray == null)
        {
            steamIdArray = new ulong[steam64s.Count];
            int index = -1;
            foreach (ulong id in steam64s)
            {
                steamIdArray[++index] = id;
            }
        }

        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            ulong[] output = new ulong[steamIdArray.Length];
            await foreach (var idPair in _dbContext.UserData
                               .Where(x => steamIdArray.Contains(x.DiscordId))
                               .Select(x => new { x.Steam64, x.DiscordId })
                               .AsAsyncEnumerable()
                               .WithCancellation(token))
            {
                int index = Array.IndexOf(steamIdArray, idPair.DiscordId);
                if (index >= 0)
                    output[index] = idPair.Steam64;
            }

            return output;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<ulong[]> GetSteam64sAsync(IReadOnlyList<ulong> discordIds, CancellationToken token = default)
    {
        ulong[]? discordIdArray = discordIds as ulong[];
        if (discordIdArray == null)
        {
            discordIdArray = new ulong[discordIds.Count];
            int index = -1;
            foreach (ulong id in discordIds)
            {
                discordIdArray[++index] = id;
            }
        }
        
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            ulong[] output = new ulong[discordIdArray.Length];
            await foreach (var idPair in _dbContext.UserData
                               .Where(x => discordIdArray.Contains(x.DiscordId))
                               .Select(x => new { x.Steam64, x.DiscordId })
                               .AsAsyncEnumerable()
                               .WithCancellation(token))
            {
                int index = Array.IndexOf(discordIdArray, idPair.DiscordId);
                if (index >= 0)
                    output[index] = idPair.Steam64;
            }

            return output;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<PlayerNames> GetUsernamesAsync(ulong steam64, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            var result = await _dbContext.UserData
                .Where(x => x.Steam64 == steam64)
                .Select(x => new { x.CharacterName, x.NickName, x.PlayerName, x.DisplayName })
                .FirstOrDefaultAsync(token)
                .ConfigureAwait(false);

            PlayerNames names = default;
            names.Steam64 = new CSteamID(steam64);
            if (result == null)
            {
                string s64String = steam64.ToString("D17", CultureInfo.InvariantCulture);
                names.CharacterName = s64String;
                names.NickName = s64String;
                names.PlayerName = s64String;
                names.WasFound = false;
            }
            else
            {
                names.CharacterName = result.CharacterName;
                names.NickName = result.NickName;
                names.PlayerName = result.PlayerName;
                names.DisplayName = result.DisplayName;
                names.WasFound = true;
            }

            return names;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<WarfareUserData?> ReadAsync(ulong steam64, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            WarfareUserData? data = await Set().FirstOrDefaultAsync(x => x.Steam64 == steam64, token).ConfigureAwait(false);
            _dbContext.ChangeTracker.Clear();
            return data;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IReadOnlyList<WarfareUserData>> ReadAsync([InstantHandle] IEnumerable<ulong> steam64, CancellationToken token = default)
    {
        if (steam64 is ICollection { Count: 0 })
            return Array.Empty<WarfareUserData>();

        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            List<WarfareUserData> result = await Set().Where(x => steam64.Contains(x.Steam64)).ToListAsync(token).ConfigureAwait(false);
            _dbContext.ChangeTracker.Clear();
            return result;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<WarfareUserData?> ReadFromDiscordIdAsync(ulong discordId, CancellationToken token = default)
    {
        if (discordId == 0)
            return null;

        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            WarfareUserData? data = await Set().FirstOrDefaultAsync(x => x.DiscordId == discordId, token).ConfigureAwait(false);
            _dbContext.ChangeTracker.Clear();
            return data;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<WarfareUserData> AddOrUpdateAsync(ulong steam64, [InstantHandle] Action<WarfareUserData, IDbContext> update, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        WarfareUserData? existing;
        try
        {
            existing = await Set().FirstOrDefaultAsync(x => x.Steam64 == steam64, token).ConfigureAwait(false);
            if (existing != null)
            {
                update(existing, _dbContext);
                existing.Steam64 = steam64;
                _dbContext.Update(existing);
            }
            else
            {
                string toStr = steam64.ToString("D17", CultureInfo.InvariantCulture);
                WarfareUserData data = new WarfareUserData
                {
                    Steam64 = steam64,
                    CharacterName = toStr,
                    PlayerName = toStr,
                    NickName = toStr,
                    DiscordId = 0,
                    FirstJoined = DateTimeOffset.UtcNow,
                    LastJoined = DateTimeOffset.UtcNow,
                    HWIDs = new List<PlayerHWID>(0),
                    IPAddresses = new List<PlayerIPAddress>(0)
                };

                update(data, _dbContext);

                _dbContext.Add(data);
                existing = data;
            }

            await _dbContext.SaveChangesAsync(token);
        }
        finally
        {
            _semaphore.Release();
        }

        return existing;
    }

    /// <inheritdoc />
    public async Task<PlayerNames> SearchFirstPlayerAsync(string input, PlayerNameType prioritizedName, bool byLastJoined, CancellationToken token = default)
    {
        CSteamID? steamId = await SteamIdHelper.TryParseSteamIdOrUrl(input, token).ConfigureAwait(false);
        if (steamId.HasValue && steamId.Value.GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
        {
            return await GetUsernamesAsync(steamId.Value.m_SteamID, token).ConfigureAwait(false);
        }

        IQueryable<WarfareUserData> data = GetSearchQuery(input, prioritizedName, byLastJoined, -1);

        await _semaphore.WaitAsync(token);
        try
        {
            var nameData = await data.Select(x => new { x.Steam64, x.CharacterName, x.PlayerName, x.NickName, x.DisplayName }).FirstOrDefaultAsync(token).ConfigureAwait(false);
            PlayerNames names = default;
            if (nameData == null)
            {
                return names;
            }

            names.WasFound = true;
            names.Steam64 = new CSteamID(nameData.Steam64);
            names.CharacterName = nameData.CharacterName;
            names.PlayerName = nameData.PlayerName;
            names.NickName = nameData.NickName;
            names.DisplayName = nameData.DisplayName;
            return names;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<int> SearchPlayersAsync(string input, PlayerNameType prioritizedName, bool byLastJoined, ICollection<PlayerNames> output, int limit = -1, CancellationToken token = default)
    {
        if (limit == 0)
        {
            return 0;
        }

        CSteamID? steamId = await SteamIdHelper.TryParseSteamIdOrUrl(input, token).ConfigureAwait(false);
        if (steamId.HasValue && steamId.Value.GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
        {
            PlayerNames names = await GetUsernamesAsync(steamId.Value.m_SteamID, token).ConfigureAwait(false);
            
            if (names.WasFound)
                output.Add(names);

            return names.WasFound ? 1 : 0;
        }

        IQueryable<WarfareUserData> data = GetSearchQuery(input, prioritizedName, byLastJoined, limit);

        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            int ct = 0;
            await foreach (var nameData in data.Select(x => new { x.Steam64, x.CharacterName, x.PlayerName, x.NickName, x.DisplayName }).AsAsyncEnumerable().WithCancellation(token).ConfigureAwait(false))
            {
                PlayerNames names = default;
                names.WasFound = true;
                names.Steam64 = new CSteamID(nameData.Steam64);
                names.CharacterName = nameData.CharacterName;
                names.PlayerName = nameData.PlayerName;
                names.NickName = nameData.NickName;
                names.DisplayName = nameData.DisplayName;
                output.Add(names);
                ++ct;
                if (limit > 0 && ct >= limit)
                    break;
            }

            return ct;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private IQueryable<WarfareUserData> GetSearchQuery(string input, PlayerNameType prioritizedName, bool byLastJoined, int limit)
    {
        string match = "%" + input + "%";
        IQueryable<WarfareUserData> data = prioritizedName switch
        {
            PlayerNameType.CharacterName => GetNameTypeQuery(PlayerNameType.CharacterName, match, input, byLastJoined)
                .Union(GetNameTypeQuery(PlayerNameType.NickName, match, input, byLastJoined))
                .Union(GetNameTypeQuery(PlayerNameType.PlayerName, match, input, byLastJoined)),

            PlayerNameType.NickName => GetNameTypeQuery(PlayerNameType.NickName, match, input, byLastJoined)
                .Union(GetNameTypeQuery(PlayerNameType.CharacterName, match, input, byLastJoined))
                .Union(GetNameTypeQuery(PlayerNameType.PlayerName, match, input, byLastJoined)),

            _ => GetNameTypeQuery(PlayerNameType.PlayerName, match, input, byLastJoined)
                .Union(GetNameTypeQuery(PlayerNameType.CharacterName, match, input, byLastJoined))
                .Union(GetNameTypeQuery(PlayerNameType.NickName, match, input, byLastJoined))
        };

        if (limit >= 0)
            data = data.Take(limit);

        return data;
    }

    private IQueryable<WarfareUserData> GetNameTypeQuery(PlayerNameType prioritizedName, string match, string input, bool byLastJoined)
    {
        IOrderedQueryable<WarfareUserData> data = prioritizedName switch
        {
            PlayerNameType.CharacterName => _dbContext.UserData
                .Where(x => EF.Functions.Like(x.CharacterName, match))
                .OrderByDescending(x => x.CharacterName == input)
                .ThenByDescending(x => EF.Functions.Like(x.CharacterName, input)),
            PlayerNameType.NickName => _dbContext.UserData
                .Where(x => EF.Functions.Like(x.NickName, match))
                .OrderByDescending(x => x.NickName == input)
                .ThenByDescending(x => EF.Functions.Like(x.NickName, input)),
            _ => _dbContext.UserData
                .Where(x => EF.Functions.Like(x.PlayerName, match))
                .OrderByDescending(x => x.PlayerName == input)
                .ThenByDescending(x => EF.Functions.Like(x.PlayerName, input))
        };

        if (byLastJoined)
        {
            data = data.ThenByDescending(x => x.LastJoined);
        }
        else
        {
            data = data.ThenBy<WarfareUserData, int>(
                prioritizedName switch
                {
                    PlayerNameType.CharacterName => x => x.CharacterName.Length,
                    PlayerNameType.NickName => x => x.NickName.Length,
                    _ => x => x.PlayerName.Length
                }
            );
        }

        return data;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _semaphore.Dispose();
    }
}

public static class UserDataServiceExtensions
{
    /// <summary>
    /// Search all players that have ever joined by their usernames, and add them to a collection as they're found.
    /// </summary>
    /// <param name="prioritizedName">The type of name to prioritize.</param>
    /// <param name="byLastJoined">If they should be ordered by the last time they joined. Exact matches will still be prioritized.</param>
    /// <returns>All matches in order of relevance.</returns>
    public static async Task<List<PlayerNames>> SearchPlayersAsync(this IUserDataService dataService, string input, PlayerNameType prioritizedName, bool byLastJoined, int limit = -1, CancellationToken token = default)
    {
        List<PlayerNames> list = new List<PlayerNames>();
        await dataService.SearchPlayersAsync(input, prioritizedName, byLastJoined, list, limit, token);
        return list;
    }

    /// <summary>
    /// Get multiple players' stored username data in bulk. Not necessarily returned in the same order as it was inputted.
    /// </summary>
    public static async Task<PlayerNames[]> GetUsernamesAsync(this IUserDataService dataService, [InstantHandle] IEnumerable<ulong> steamIds, CancellationToken token = default)
    {
        List<ulong> ids = [ ..steamIds ];
        if (ids.Count == 0)
        {
            return Array.Empty<PlayerNames>();
        }

        if (ids.Count == 1)
        {
            PlayerNames singleNames = await dataService.GetUsernamesAsync(ids[0], token).ConfigureAwait(false);
            return singleNames.WasFound ? [ singleNames ] : Array.Empty<PlayerNames>();
        }

        IReadOnlyList<WarfareUserData> userData = await dataService.ReadAsync(ids, token).ConfigureAwait(false);
        if (userData.Count == 0)
        {
            return Array.Empty<PlayerNames>();
        }

        PlayerNames[] newNames = new PlayerNames[userData.Count];
        PlayerNames names = default;
        for (int i = 0; i < newNames.Length; ++i)
        {
            WarfareUserData user = userData[i];
            names.Steam64 = new CSteamID(user.Steam64);
            names.CharacterName = user.CharacterName;
            names.NickName = user.NickName;
            names.PlayerName = user.PlayerName;
            names.DisplayName = user.DisplayName;
            names.WasFound = true;
            newNames[i] = names;
        }

        return newNames;
    }
}
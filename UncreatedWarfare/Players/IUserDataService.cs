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
    /// Get a single player's user data if they've joined.
    /// </summary>
    Task<WarfareUserData?> ReadAsync(ulong steam64, CancellationToken token = default);
    
    /// <summary>
    /// Get multiple players' user data in bulk if they've joined. Not necessarily returned in the same order as it was inputted.
    /// </summary>
    Task<IReadOnlyList<WarfareUserData>> ReadAsync([InstantHandle] IEnumerable<ulong> steam64, CancellationToken token = default);

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
    public async Task<WarfareUserData?> ReadAsync(ulong steam64, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            return await Set().FirstOrDefaultAsync(x => x.Steam64 == steam64, token).ConfigureAwait(false);
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
            return await Set().Where(x => steam64.Contains(x.Steam64)).ToListAsync(token).ConfigureAwait(false);
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
        if (FormattingUtility.TryParseSteamId(input, out CSteamID steamId) && steamId.GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
        {
            return await this.GetUsernamesAsync(steamId.m_SteamID, token).ConfigureAwait(false);
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

        if (FormattingUtility.TryParseSteamId(input, out CSteamID steamId) && steamId.GetEAccountType() == EAccountType.k_EAccountTypeIndividual)
        {
            PlayerNames names = await this.GetUsernamesAsync(steamId.m_SteamID, token).ConfigureAwait(false);
            
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
    /// Get a given player's stored username data.
    /// </summary>
    public static async Task<PlayerNames> GetUsernamesAsync(this IUserDataService dataService, ulong steam64, CancellationToken token = default)
    {
        WarfareUserData? userData = await dataService.ReadAsync(steam64, token).ConfigureAwait(false);

        PlayerNames names = default;
        if (userData != null)
        {
            names.Steam64 = new CSteamID(userData.Steam64);
            names.CharacterName = userData.CharacterName;
            names.NickName = userData.NickName;
            names.PlayerName = userData.PlayerName;
            names.DisplayName = userData.DisplayName;
            names.WasFound = true;
        }
        else
        {
            string s64String = steam64.ToString("D17", CultureInfo.InvariantCulture);
            names.Steam64 = new CSteamID(steam64);
            names.CharacterName = s64String;
            names.NickName = s64String;
            names.PlayerName = s64String;
            names.WasFound = false;
        }

        return names;
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
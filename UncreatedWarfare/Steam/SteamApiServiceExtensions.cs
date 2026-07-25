using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Uncreated.Warfare.Steam.Models;

namespace Uncreated.Warfare.Steam;

/// <summary>
/// Extensions implementing common functions for the Steam Web API.
/// </summary>
public static class SteamApiServiceExtensions
{
    /// <summary>
    /// Base URL for all API requests.
    /// </summary>
    public static readonly string BaseSteamApiUrl = "https://api.steampowered.com/";

    /// <summary>
    /// Get the <see cref="PlayerFriendsList"/> steam API object for a single player, which contains a list of their friends if their profile is public.
    /// </summary>
    /// <exception cref="SteamApiRequestException">Failed to fetch the data for some reason.</exception>
    /// <exception cref="InvalidOperationException">Service not enabled.</exception>
    public static async Task<PlayerFriendsList> GetPlayerFriendsAsync(this ISteamApiService service, ulong player, CancellationToken token = default)
    {
        if (!service.IsEnabled)
        {
            ThrowServiceNotEnabled();
        }

        // create string "&steamid=76500"
        string queryString = string.Create(26, player, static (span, state) =>
        {
            "&steamid=".AsSpan().CopyTo(span);
            state.TryFormat(span[9..], out _, "D17", CultureInfo.InvariantCulture);
        });

        SteamApiQuery query = new SteamApiQuery("ISteamUser", "GetFriendList", 1, queryString);

        PlayerFriendsListResponse response;
        try
        {
            response = await service.ExecuteQueryAsync<PlayerFriendsListResponse>(query, token).ConfigureAwait(false);
        }
        catch (SteamApiRequestException ex) when (ex.IsApiResponseError)
        {
            return new PlayerFriendsList { Friends = new List<PlayerFriend>(0) };
        }

        return response?.FriendsList?.Friends == null ? new PlayerFriendsList { Friends = new List<PlayerFriend>(0) } : response.FriendsList;
    }

    /// <summary>
    /// Get the <see cref="PlayerSummary"/> steam API object for a single player.
    /// </summary>
    /// <exception cref="SteamApiRequestException">Failed to fetch the data for some reason.</exception>
    /// <exception cref="InvalidOperationException">Service not enabled.</exception>
    public static async Task<PlayerSummary> GetPlayerSummaryAsync(this ISteamApiService service, ulong player, CancellationToken token = default)
    {
        if (!service.IsEnabled)
        {
            ThrowServiceNotEnabled();
        }

        // create string "&steamids=76500"
        string queryString = string.Create(27, player, static (span, state) =>
        {
            "&steamids=".AsSpan().CopyTo(span);
            state.TryFormat(span[10..], out _, "D17", CultureInfo.InvariantCulture);
        });

        SteamApiQuery query = new SteamApiQuery("ISteamUser", "GetPlayerSummaries", 2, queryString);

        PlayerSummariesResponse response = await service.ExecuteQueryAsync<PlayerSummariesResponse>(query, token).ConfigureAwait(false);

        return response?.Data?.Results?.FirstOrDefault() ?? throw new SteamApiRequestException($"Failed to get player summary for {player}.");
    }

    /// <summary>
    /// Get the <see cref="PlayerSummary"/> steam API object for multiple players.
    /// </summary>
    /// <exception cref="SteamApiRequestException">Failed to fetch the data for some reason.</exception>
    /// <exception cref="InvalidOperationException">Service not enabled.</exception>
    public static async Task<PlayerSummary[]> GetPlayerSummariesAsync(this ISteamApiService service, IReadOnlyList<ulong> players, int index, int length, CancellationToken token = default)
    {
        if (length == 0)
            return Array.Empty<PlayerSummary>();
        if (index > players.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        if (index < 0)
            index = 0;
        if (length < 0)
            length = players.Count - index;
        if (index + length > players.Count)
            throw new ArgumentOutOfRangeException(nameof(length));

        if (!service.IsEnabled)
        {
            ThrowServiceNotEnabled();
        }

        CreatePlayerSummariesStringState state = default;
        state.Players = players;
        state.Index = index;
        state.Length = length;

        // create string "&steamids=76500,76500,etc"
        string queryString = string.Create(9 + length * 18, state, static (span, state) =>
        {
            "&steamids=".AsSpan().CopyTo(span);
            int index = 10;

            for (int i = 0; i < state.Length; ++i)
            {
                ulong s64 = state.Players[i + state.Index];
                if (i != 0)
                {
                    span[index] = ',';
                    ++index;
                }

                s64.TryFormat(span[index..], out _, "D17", CultureInfo.InvariantCulture);
                index += 17;
            }
        });

        SteamApiQuery query = new SteamApiQuery("ISteamUser", "GetPlayerSummaries", 2, queryString);

        PlayerSummariesResponse response = await service.ExecuteQueryAsync<PlayerSummariesResponse>(query, token).ConfigureAwait(false);
        return response?.Data?.Results ?? Array.Empty<PlayerSummary>();
    }

    private struct CreatePlayerSummariesStringState
    {
        public IReadOnlyList<ulong> Players;
        public int Index;
        public int Length;
    }

    /// <summary>
    /// Get the <see cref="PlayerSummary"/> steam API object for multiple players.
    /// </summary>
    /// <exception cref="SteamApiRequestException">Failed to fetch the data for some reason.</exception>
    public static Task<PlayerSummary[]> GetPlayerSummariesAsync(this ISteamApiService service, IReadOnlyList<ulong> players, CancellationToken token = default)
    {
        return service.GetPlayerSummariesAsync(players, 0, players.Count, token);
    }

    /// <inheritdoc cref="ResolveVanityUrlAsync(ISteamApiService,ReadOnlySpan{char},VanityUrlType,CancellationToken)"/>
    public static Task<CSteamID?> ResolveVanityUrlAsync(this ISteamApiService service, ReadOnlySpan<char> customUrlId, CancellationToken token = default)
    {
        return ResolveVanityUrlAsync(service, customUrlId, VanityUrlType.Individual, token);
    }

    /// <summary>
    /// Get the Steam ID associated with the given vanity URL.
    /// </summary>
    /// <exception cref="OperationCanceledException"/>
    /// <exception cref="SteamApiRequestException">Failed to fetch the data for some reason.</exception>
    /// <exception cref="InvalidOperationException">Service not enabled.</exception>
    public static Task<CSteamID?> ResolveVanityUrlAsync(this ISteamApiService service, ReadOnlySpan<char> customUrlId, VanityUrlType type, CancellationToken token = default)
    {
        if (!service.IsEnabled)
        {
            ThrowServiceNotEnabled();
        }

        token.ThrowIfCancellationRequested();

        customUrlId = customUrlId.Trim();
        if (customUrlId.IsEmpty)
            return Task.FromResult<CSteamID?>(null);

        // trim beginning of URL from the vanity name
        // ex: "https://steamcommunity.com/id/my-vanity-url/" -> "my-vanity-url"
        if (customUrlId.IndexOf('/') >= 0)
        {
            // trim trailing slash
            if (customUrlId[^1] == '/')
                customUrlId = customUrlId[..^1];

            int lastSlash = customUrlId.LastIndexOf('/');
            if (lastSlash == customUrlId.Length - 1)
                return Task.FromResult<CSteamID?>(null);

            if (lastSlash >= 0)
            {
                customUrlId = customUrlId.Slice(lastSlash + 1);

                if (customUrlId.IsEmpty)
                    return Task.FromResult<CSteamID?>(null);
            }
        }

        return Core(service, new string(customUrlId), type, token);

        static async Task<CSteamID?> Core(ISteamApiService service, string vanityUrl, VanityUrlType type, CancellationToken token)
        {
            SteamApiQuery query = new SteamApiQuery(
                "ISteamUser",
                "ResolveVanityURL",
                version: 1,
                type == VanityUrlType.Individual
                    ? $"vanityurl={Uri.EscapeDataString(vanityUrl)}"
                    : $"vanityurl={Uri.EscapeDataString(vanityUrl)}&type={type:D}"
            );

            ResolveVanityURLResponse response = await service.ExecuteQueryAsync<ResolveVanityURLResponse>(query, token);

            if (response.Response is not { StatusCode: 1 }
                || !ulong.TryParse(response.Response.SteamId, NumberStyles.Number, CultureInfo.InvariantCulture, out ulong id)
                || id == 0)
            {
                return null;
            }

            return new CSteamID(id);
        }
    }

    /// <exception cref="InvalidOperationException"/>
    [DoesNotReturn]
    internal static void ThrowServiceNotEnabled()
    {
        throw new InvalidOperationException("Steam API service is not enabled.");
    }
}

/// <summary>
/// Category of Steam vanity URLs.
/// </summary>
public enum VanityUrlType
{
    /// <summary>
    /// A Steam user.<br/><c>https://steamcommunity.com/id/vanity-url</c>
    /// </summary>
    Individual = 1,

    /// <summary>
    /// A Steam group.<br/><c>https://steamcommunity.com/groups/vanity-url</c>
    /// </summary>
    Group = 2,

    /// <summary>
    /// A Steam Game Hub.<br/><c>https://steamcommunity.com/app/vanity-url</c>
    /// </summary>
    OfficialGameGroup = 3
}
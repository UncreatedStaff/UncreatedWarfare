using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Steam.Models;
using Uncreated.Warfare.Util;
using UnityEngine.Networking;

namespace Uncreated.Warfare.Steam;
public class SteamApiService
{
    private const string BaseUrl = "https://api.steampowered.com/";
    private readonly ILogger<SteamApiService> _logger;
    private readonly IPlayerService _playerService;

    private readonly string? _steamApiKey;

    public SteamApiService(IConfiguration systemConfig, ILogger<SteamApiService> logger, IPlayerService playerService)
    {
        _logger = logger;
        _playerService = playerService;

        _steamApiKey = systemConfig["steam_api_key"];
    }

    public async UniTask TryDownloadAllPlayerSummaries(bool allowCache = true, CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        List<ulong>? players = null;
        foreach (WarfarePlayer player in _playerService.OnlinePlayers)
        {
            if (!allowCache || player.SteamSummary == null)
                (players ??= new List<ulong>(_playerService.OnlinePlayers.Count)).Add(player.Steam64.m_SteamID);
        }

        if (players == null)
            return;

        PlayerSummary[] summaries = await GetPlayerSummaries(players, token);
        for (int j = 0; j < summaries.Length; ++j)
        {
            PlayerSummary summary = summaries[j];
            WarfarePlayer? player = _playerService.GetOnlinePlayerOrNull(summary.Steam64);
            if (player != null)
                player.SteamSummary = summary;
        }
    }

    public async UniTask<PlayerFriendsList> GetPlayerFriends(ulong player, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(_steamApiKey))
            throw new InvalidOperationException("Steam API key not present.");

        const int tryCt = 5;
        const float delay = 1.0f;

        for (int tries = 0; tries < tryCt; ++tries)
        {
            await UniTask.SwitchToMainThread(token);

            string responseText;

            try
            {
                using UnityWebRequest webRequest = UnityWebRequest.Get(CreateUrl("ISteamUser", 1, "GetFriendList", "&steamid=" + player.ToString(CultureInfo.InvariantCulture)));
                webRequest.timeout = tries + 1;
                _logger.LogConditional("[GetPlayerFriends] Sending GetFriendList request: {0} with timeout {1}... ({2}/{3})", webRequest.url, webRequest.timeout, tries + 1, tryCt);
                await webRequest.SendWebRequest().WithCancellation(token);
                _logger.LogConditional("[GetPlayerFriends]   Done with {0}.", webRequest.url);

                if (webRequest.result != UnityWebRequest.Result.Success)
                    throw new Exception($"Error getting player friend list for {player} from {webRequest.url.Replace(_steamApiKey!, "API_KEY")}: {webRequest.responseCode} ({webRequest.result}).");

                responseText = webRequest.downloadHandler.text;
                if (string.IsNullOrEmpty(responseText))
                    return new PlayerFriendsList { Friends = new List<PlayerFriend>(0) };
            }
            catch (Exception ex)
            {
                if (tries == tryCt - 1)
                    throw new Exception("Error downloading friends list for " + player + ".", ex);

                _logger.LogError("[GetPlayerFriends] Error getting steam player summaries. Retrying {0} / {1}.", (tries + 1).ToString(CultureInfo.InvariantCulture), tryCt.ToString(CultureInfo.InvariantCulture));

                await UniTask.WaitForSeconds(delay, cancellationToken: token);
                continue;
            }

            if (tries > 0)
                _logger.LogInformation("[GetPlayerFriends] Try {0} / {1} succeeded.", (tries + 1).ToString(CultureInfo.InvariantCulture), tryCt.ToString(CultureInfo.InvariantCulture));

            try
            {
                PlayerFriendsListResponse? responseData = JsonSerializer.Deserialize<PlayerFriendsListResponse>(responseText, ConfigurationSettings.JsonSerializerSettings);

                return responseData?.FriendsList?.Friends == null ? new PlayerFriendsList { Friends = new List<PlayerFriend>(0) } : responseData.FriendsList;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deserializing response from Steam API: {responseText}.", ex);
            }
        }

        return new PlayerFriendsList { Friends = new List<PlayerFriend>(0) }; // this will never be reached
    }

    public async UniTask<PlayerSummary?> GetPlayerSummary(ulong player, CancellationToken token = default)
    {
        try
        {
            PlayerSummary[] summary = await GetPlayerSummaries([ player ], token);
            for (int i = 0; i < summary.Length; ++i)
            {
                if (summary[i].Steam64 == player)
                    return summary[i];
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting player summary: {0}.", new CSteamID(player));
        }
        return null;
    }

    public UniTask<PlayerSummary[]> GetPlayerSummaries(IList<ulong> players, CancellationToken token = default)
    {
        return GetPlayerSummaries(players, 0, players.Count, token);
    }

    public async UniTask<PlayerSummary[]> GetPlayerSummaries(IList<ulong> players, int index, int length, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(_steamApiKey))
            throw new InvalidOperationException("Steam API key not present.");
        if (length == 0)
            return Array.Empty<PlayerSummary>();
        if (index > players.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        if (index + length > players.Count)
            throw new ArgumentOutOfRangeException(nameof(length));
        if (length < 0)
            length = players.Count;
        if (index < 0)
            index = 0;

        StringBuilder sb = new StringBuilder(length * 18);
        bool any = false;
        for (int i = 0; i < length; ++i)
        {
            bool alreadyAdded = false;
            ulong current = players[i + index];
            for (int j = i - 1; j >= 0; --j)
            {
                if (players[j + index] == current)
                {
                    alreadyAdded = true;
                    break;
                }
            }

            if (alreadyAdded)
                continue;
            if (!any)
                any = true;
            else
                sb.Append(',');
            sb.Append(current.ToString("D17", CultureInfo.InvariantCulture));
        }

        string str = sb.ToString();
        sb.Clear();
        sb.Capacity = 0;

        const int tryCt = 5;
        const float delay = 1.0f;

        for (int tries = 0; tries < tryCt; ++tries)
        {
            await UniTask.SwitchToMainThread(token);

            string responseText;

            try
            {
                using UnityWebRequest webRequest = UnityWebRequest.Get(CreateUrl("ISteamUser", 2, "GetPlayerSummaries", "&steamids=" + str));
                webRequest.timeout = tries + 1;
                _logger.LogConditional("Sending PlayerSummary request: {0} with timeout {1}... ({2}/{3})", webRequest.url, webRequest.timeout, tries + 1, tryCt);
                await webRequest.SendWebRequest().WithCancellation(token);
                _logger.LogConditional("  Done with {0}.", webRequest.url);

                if (webRequest.result != UnityWebRequest.Result.Success)
                    throw new Exception($"Error getting player summaries from {webRequest.url.Replace(_steamApiKey!, "API_KEY")}: {webRequest.responseCode} ({webRequest.result}).");

                responseText = webRequest.downloadHandler.text;
                if (string.IsNullOrEmpty(responseText))
                    return Array.Empty<PlayerSummary>();

            }
            catch (Exception ex)
            {
                if (tries == tryCt - 1)
                    throw new Exception("Error downloading " + players.Count + " player summary(ies).", ex);

                _logger.LogError("Error getting steam player summaries. Retrying {0} / {1}.", (tries + 1).ToString(CultureInfo.InvariantCulture), tryCt.ToString(CultureInfo.InvariantCulture));

                await UniTask.WaitForSeconds(delay, cancellationToken: token);
                continue;
            }

            if (tries > 0)
                _logger.LogInformation("[GETPLAYERSUMMARIES] Try {0} / {1} succeeded.", (tries + 1).ToString(CultureInfo.InvariantCulture), tryCt.ToString(CultureInfo.InvariantCulture));

            try
            {
                PlayerSummariesResponse? responseData = JsonSerializer.Deserialize<PlayerSummariesResponse>(responseText, ConfigurationSettings.JsonSerializerSettings);

                return responseData?.Data.Results ?? Array.Empty<PlayerSummary>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deserializing response from Steam API: {responseText}.", ex);
            }
        }

        return Array.Empty<PlayerSummary>(); // this will never be reached
    }

    public string? CreateUrl(string @interface, int version, string method, string? query)
    {
        if (string.IsNullOrWhiteSpace(_steamApiKey))
        {
            _logger.LogError("Steam API key is not configured in configuration at \"{0}\".", "steam_api_key");
            throw new InvalidOperationException("Missing Steam API key.");
        }

        int vLen = MathUtility.CountDigits(version);
        int stringLen = BaseUrl.Length + @interface.Length + 1 + method.Length + 2 + vLen + 5 + _steamApiKey.Length;
        if (query != null)
            stringLen += 1 + query.Length;

        CreateUrlState state = default;
        state.APIKey = _steamApiKey;
        state.Version = version;
        state.Method = method;
        state.Interface = @interface;
        state.VersionLength = vLen;
        state.Query = query;

        return string.Create(stringLen, state, (span, state) =>
        {
            int index = 0;
            BaseUrl.AsSpan().CopyTo(span);
            index += BaseUrl.Length;

            state.Interface.AsSpan().CopyTo(span[index..]);
            index += state.Interface.Length;

            span[index++] = '/';

            state.Method.AsSpan().CopyTo(span[index..]);
            index += state.Method.Length;

            span[index++] = '/';
            span[index++] = 'v';

            state.Version.TryFormat(span[index..], out _, "D", CultureInfo.InvariantCulture);
            index += state.VersionLength;

            span[index++] = '?';
            span[index++] = 'k';
            span[index++] = 'e';
            span[index++] = 'y';
            span[index++] = '=';

            state.APIKey.AsSpan().CopyTo(span[index..]);
            index += state.APIKey.Length;

            if (state.Query == null)
                return;

            span[index++] = '&';
            state.Query.AsSpan().CopyTo(span[index..]);
        });
    }
    private struct CreateUrlState
    {
        public string Interface;
        public string Method;
        public string? Query;
        public string APIKey;
        public int Version;
        public int VersionLength;
    }
}
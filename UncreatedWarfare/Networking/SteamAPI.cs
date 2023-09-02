﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using Cysharp.Threading.Tasks;
using SDG.Unturned;
using Uncreated.Framework;
using Uncreated.Json;
using UnityEngine.Networking;

namespace Uncreated.Warfare.Networking;
public sealed class SteamAPI
{
    private const string BaseUrl = "https://api.steampowered.com/";

    public static async UniTask<PlayerSummary?> GetPlayerSummary(ulong player, CancellationToken token = default)
    {
        try
        {
            PlayerSummary[] summary = await GetPlayerSummaries(new ulong[] { player }, token);
            for (int i = 0; i < summary.Length; ++i)
            {
                if (summary[i].Steam64 == player)
                    return summary[i];
            }
        }
        catch (Exception ex)
        {
            L.LogError(ex);
        }
        return null;
    }
    public static string MakeUrl(string @interface, int version, string method, string? query)
    {
        string url = BaseUrl + @interface + "/" + method + "/v" + version.ToString(CultureInfo.InvariantCulture) + "?key=" + UCWarfare.Config.SteamAPIKey;
        if (query != null)
            url += "&" + query;
        return url;
    }

    public static UniTask<PlayerSummary[]> GetPlayerSummaries(IList<ulong> players, CancellationToken token = default) => GetPlayerSummaries(players, 0, players.Count, token);
    public static async UniTask<PlayerSummary[]> GetPlayerSummaries(IList<ulong> players, int index, int length, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(UCWarfare.Config.SteamAPIKey))
            throw new InvalidOperationException("Steam API key not present.");
        if (index >= players.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        if (index + length > players.Count)
            throw new ArgumentOutOfRangeException(nameof(length));
        if (length < 0)
            length = players.Count;
        if (index < 0)
            index = 0;
        if (players.Count == 0)
            return Array.Empty<PlayerSummary>();

        string[] strs = new string[length];
        for (int i = 0; i < length; ++i)
            strs[i] = players[i + index].ToString(CultureInfo.InvariantCulture);

        using UnityWebRequest webRequest = UnityWebRequest.Get(MakeUrl("ISteamUser", 2, "GetPlayerSummaries", "&steamids=" + string.Join(",", strs)));
        await webRequest.SendWebRequest().WithCancellation(token);
        
        if (webRequest.result != UnityWebRequest.Result.Success)
            throw new Exception($"Error getting player summaries from {webRequest.url.Replace(UCWarfare.Config.SteamAPIKey!, "API_KEY")}: {webRequest.responseCode} ({webRequest.result}).");
        
        string responseText = webRequest.downloadHandler.text;
        if (string.IsNullOrEmpty(responseText))
            return Array.Empty<PlayerSummary>();

        try
        {
            PlayerSummariesResponse? responseData = JsonSerializer.Deserialize<PlayerSummariesResponse>(responseText, JsonEx.serializerSettings);

            return responseData?.Data.Results ?? Array.Empty<PlayerSummary>();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error deserializing response from Steam API: {responseText}.", ex);
        }
    }
}

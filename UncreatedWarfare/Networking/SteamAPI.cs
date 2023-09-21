using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
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
            string responseText;

            try
            {
                using UnityWebRequest webRequest = UnityWebRequest.Get(MakeUrl("ISteamUser", 2, "GetPlayerSummaries", "&steamids=" + str));
                webRequest.timeout = tries + 1;
                L.LogDebug($"Sending PlayerSummary request: {webRequest.url} with timeout {webRequest.timeout}... ({tries + 1}/{tryCt})");
                await webRequest.SendWebRequest().WithCancellation(token);
                L.LogDebug($"  Done with {webRequest.url}.");

                if (webRequest.result != UnityWebRequest.Result.Success)
                    throw new Exception($"Error getting player summaries from {webRequest.url.Replace(UCWarfare.Config.SteamAPIKey!, "API_KEY")}: {webRequest.responseCode} ({webRequest.result}).");

                responseText = webRequest.downloadHandler.text;
                if (string.IsNullOrEmpty(responseText))
                    return Array.Empty<PlayerSummary>();

            }
            catch (Exception ex)
            {
                if (tries == tryCt - 1)
                    throw new Exception("Error downloading " + players.Count + " player summary(ies).", ex);

                L.LogError($"Error getting steam player summaries. Retrying {(tries + 1).ToString(CultureInfo.InvariantCulture)} / {tryCt.ToString(CultureInfo.InvariantCulture)}.");

                await UniTask.WaitForSeconds(delay, cancellationToken: token);
                continue;
            }

            if (tries > 0)
                L.Log($"[GETPLAYERSUMMARIES] Try {(tries + 1).ToString(CultureInfo.InvariantCulture)} / {tryCt.ToString(CultureInfo.InvariantCulture)} succeeded.", ConsoleColor.Green);

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

        return Array.Empty<PlayerSummary>(); // this will never be reached
    }
}

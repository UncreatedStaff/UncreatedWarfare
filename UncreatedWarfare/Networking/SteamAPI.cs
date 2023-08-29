using System;
using System.Collections;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using Cysharp.Threading.Tasks;
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

    public static async UniTask<PlayerSummary[]> GetPlayerSummaries(ulong[] players, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(UCWarfare.Config.SteamAPIKey))
            throw new InvalidOperationException("Steam API key not present.");
        if (players.Length == 0)
            return Array.Empty<PlayerSummary>();

        using UnityWebRequest webRequest = UnityWebRequest.Get(MakeUrl("ISteamUser", 2, "GetPlayerSummaries", "&steamids=" + string.Join(",", players)));
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

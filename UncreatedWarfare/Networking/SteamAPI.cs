using System;
using System.Collections;
using System.Globalization;
using System.Text.Json;
using Uncreated.Json;
using UnityEngine.Networking;
using UnturnedWorkshopAnalyst.Models;

namespace Uncreated.Warfare.Networking;
public sealed class SteamAPI
{
    private const string BaseUrl = "https://api.steampowered.com/";
    public static string MakeUrl(string @interface, int version, string method, string? query)
    {
        string url = BaseUrl + @interface + "/" + method + "/v" + version.ToString(CultureInfo.InvariantCulture) + "?key=" + UCWarfare.Config.SteamAPIKey;
        if (query != null)
            url += "&" + query;
        return url;
    }

    public static IEnumerator GetPlayerSummaries(ulong[] players, Wrapper<PlayerSummary[]> response)
    {
        if (string.IsNullOrEmpty(UCWarfare.Config.SteamAPIKey))
        {
            response.Value = Array.Empty<PlayerSummary>();
            throw new InvalidOperationException("Steam API key not present.");
        }
        UnityWebRequest webRequest = UnityWebRequest.Get(MakeUrl("ISteamUser", 2, "GetPlayerSummaries", "&steamids=" + string.Join(",", players)));
        yield return webRequest.SendWebRequest();
        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            L.LogError($"Error getting player summaries from {webRequest.url.Replace(UCWarfare.Config.SteamAPIKey!, "API_KEY")}: {webRequest.responseCode} ({webRequest.result}).");
            response.Value = Array.Empty<PlayerSummary>();
            yield break;
        }
        string responseText = webRequest.downloadHandler.text;
        if (string.IsNullOrEmpty(responseText))
        {
            response.Value = Array.Empty<PlayerSummary>();
            yield break;
        }

        try
        {
            PlayerSummariesResponse? responseData = JsonSerializer.Deserialize<PlayerSummariesResponse>(responseText, JsonEx.serializerSettings);

            response.Value = responseData?.Data.Results ?? Array.Empty<PlayerSummary>();
        }
        catch (Exception ex)
        {
            L.LogError($"Error deserializing response from Steam API: {responseText}.");
            L.LogError(ex);
        }
    }
}

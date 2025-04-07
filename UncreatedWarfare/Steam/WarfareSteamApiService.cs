using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Text.Json;
using UnityEngine.Networking;

namespace Uncreated.Warfare.Steam;

internal class WarfareSteamApiService : ISteamApiService
{
    private readonly ILogger<WarfareSteamApiService> _logger;

    private readonly string? _steamApiKey;

    // retry constants
    private const int TryCount = 5;
    private const float RetryDelay = 1.0f;

    public WarfareSteamApiService(IConfiguration systemConfig, ILogger<WarfareSteamApiService> logger)
    {
        _logger = logger;
        _steamApiKey = systemConfig["steam_api_key"];
    }

    public async Task<TResponse> ExecuteQueryAsync<TResponse>(SteamApiQuery query, CancellationToken token) where TResponse : notnull
    {
        if (string.IsNullOrEmpty(_steamApiKey))
            throw new InvalidOperationException("Steam API key not present.");

        string url = query.CreateUrl(_steamApiKey);
        string toShowUrl = url;
#if RELEASE
        toShowUrl = toShowUrl.Replace(_steamApiKey, "API_KEY_REDACTED");
#endif

        for (int tryNum = 0; tryNum < TryCount; ++tryNum)
        {
            await UniTask.SwitchToMainThread(token);

            using UnityWebRequest request = new UnityWebRequest(url, "GET", downloadHandler: new DownloadHandlerBuffer(), uploadHandler: null);
            string data;
            try
            {
                request.timeout = (int)Math.Ceiling(query.StartTimeout + tryNum);

                await request.SendWebRequest().WithCancellation(token);

                data = request.downloadHandler.text;
            }
            catch (Exception ex)
            {
                if (ex is UnityWebRequestException { ResponseCode: (long)HttpStatusCode.Unauthorized })
                {
                    _logger.LogDebug("Unauthorized to access certain information from the Steam API when executing query: {0}.", query);
                    throw new SteamApiRequestException($"Unauthorized to access information from Steam API query: {query}, url: \"{toShowUrl}\".", ex)
                    {
                        IsApiResponseError = true
                    };
                }

                if (tryNum == TryCount - 1)
                {
                    throw new SteamApiRequestException($"Error executing Steam API query: {query}, url: \"{toShowUrl}\".", ex);
                }

                _logger.LogError("Error executing Steam API query: {0}. Retrying {1} / {2}.", query, tryNum + 1, TryCount);
                await Task.Delay(TimeSpan.FromSeconds(RetryDelay), token);
                continue;
            }

            if (tryNum > 0)
            {
                _logger.LogInformation("Executing Steam API query: {0} succeeded after {1} tries.", query, tryNum + 1);
            }

            try
            {
                return JsonSerializer.Deserialize<TResponse>(data) ?? throw new SteamApiRequestException($"Error parsing result from Steam API query: {query}.");
            }
            catch (Exception ex)
            {
                throw new SteamApiRequestException($"Error parsing result from Steam API query: {query}, url: \"{toShowUrl}\".", ex);
            }
        }

        throw new SteamApiRequestException($"Error executing Steam API query: {query}, url: \"{toShowUrl}\".");
    }
}
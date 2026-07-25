using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Warfare.Steam;

namespace Uncreated.Warfare.Tests.Utility;

public class TestSteamApiService : ISteamApiService, IDisposable
{
    private readonly HttpClient _client;
    private readonly string _steamApiKey;

    private const int TryCount = 5;
    private const int RetryDelay = 500;

    public bool IsEnabled => true;

    public TestSteamApiService(string steamApiKey)
    {
        _steamApiKey = steamApiKey ?? throw new ArgumentNullException(nameof(steamApiKey));

        _client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(2d)
        };
    }

    public async Task<TResponse> ExecuteQueryAsync<TResponse>(SteamApiQuery query, CancellationToken token) where TResponse : notnull
    {
        if (string.IsNullOrEmpty(_steamApiKey))
            throw new InvalidOperationException("Steam API key not present.");

        string url = query.CreateUrl(_steamApiKey);

        for (int tryNum = 0; tryNum < TryCount; ++tryNum)
        {
            string data;
            try
            {
                HttpResponseMessage response = await _client.GetAsync(url, token).ConfigureAwait(false);

                response.EnsureSuccessStatusCode();
                data = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (tryNum == TryCount - 1)
                    throw new SteamApiRequestException($"Error executing Steam API query: {query}.", ex);

                Console.WriteLine("Error executing Steam API query: {0}. Retrying {1} / {2}.", query, tryNum + 1, TryCount);
                await Task.Delay(RetryDelay, token);
                continue;
            }

            if (tryNum > 0)
            {
                Console.WriteLine("Executing Steam API query: {0} succeeded after {1} tries.", query, tryNum + 1);
            }

            try
            {
                return JsonSerializer.Deserialize<TResponse>(data) ?? throw new SteamApiRequestException($"Error parsing result from Steam API query: {query}.");
            }
            catch (Exception ex)
            {
                throw new SteamApiRequestException($"Error parsing result from Steam API query: {query}.", ex);
            }
        }

        throw new SteamApiRequestException($"Error executing Steam API query: {query}.");
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
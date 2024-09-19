using DanielWillett.ModularRpcs;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.WebSockets;
using Microsoft.Extensions.Configuration;
using System;
using UnityEngine.Networking;

namespace Uncreated.Warfare.Networking;
public class HomebaseConnector
{
    private readonly ILogger<HomebaseConnector> _logger;
    private readonly string? _authKey;
    private readonly Uri? _authEndpoint;
    private readonly Uri? _connectEndpoint;

    public bool Enabled { get; }
    
    public HomebaseConnector(IConfiguration systemConfig, ILogger<HomebaseConnector> logger)
    {
        _logger = logger;

        IConfigurationSection homebaseSection = systemConfig.GetSection("homebase");

        Enabled = string.Equals(homebaseSection["enabled"], "true", StringComparison.InvariantCultureIgnoreCase);

        _authKey = homebaseSection["auth_key"];

        string authEndpoint = homebaseSection["auth_endpoint"];
        string connectEndpoint = homebaseSection["connect_endpoint"];

        _authEndpoint = string.IsNullOrWhiteSpace(authEndpoint) ? null : new Uri(authEndpoint);
        _connectEndpoint = string.IsNullOrWhiteSpace(connectEndpoint) ? null : new Uri(connectEndpoint);
    }

    public async Task<bool> ConnectAsync(CancellationToken token = default)
    {
        Uri? connectUri = await GetConnectUri(token);
        if (connectUri == null)
            return false;

        _logger.LogDebug("Connecting to homebase at: {0}.", connectUri);

        WebSocketEndpoint endpoint = WebSocketEndpoint.AsClient(connectUri);
        endpoint.ShouldAutoReconnect = true;
#if DEBUG
        // lower the reconnect delay
        endpoint.DelaySettings = new PlateauingDelay(amplifier: 3.6d, climb: 1.8d, maximum: 60d, start: 10d);
#endif
        WebSocketClientsideRemoteRpcConnection connection;
        try
        {
            connection = await endpoint.RequestConnectionAsync(Data.RpcRouter, Data.HomebaseLifetime, Data.RpcSerializer, token).ConfigureAwait(false);
            connection.Local.SetLogger(_logger);
            connection.OnReconnect += GetConnectUri;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open WebSocket client.");
            return false;
        }

        await UniTask.SwitchToMainThread(token);
        Data.RpcConnection = connection;
        return true;
    }

    private Task<Uri?> GetConnectUri(WebSocketClientsideRemoteRpcConnection connection)
    {
        return GetConnectUri(CancellationToken.None);
    }

    private async Task<Uri?> GetConnectUri(CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);
        
        if (!Enabled || _connectEndpoint == null)
        {
            _logger.LogWarning("Homebase disabled or not configured.");
            return null;
        }

        string? authJwt = null;

        if (_authEndpoint != null)
        {
            if (string.IsNullOrWhiteSpace(_authKey))
            {
                _logger.LogWarning("Authentication key not configured.");
                return null;
            }

            using UnityWebRequest authRequest = UnityWebRequest.Get(_authEndpoint);
            authRequest.SetRequestHeader("Authorization", "Bearer " + _authKey);

#if DEBUG
            _logger.LogDebug("Authenticating for homebase at: {0}.", _authEndpoint);
#endif

            try
            {
                await authRequest.SendWebRequest();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to authenticate WebSocket client.");
                return null;
            }

            await UniTask.SwitchToMainThread(token);

            authJwt = authRequest.downloadHandler.text;
        }

        Uri connectUri = _connectEndpoint;

        if (authJwt != null)
        {
            connectUri = new Uri(connectUri, "?token=" + Uri.EscapeDataString(authJwt));
        }

        return connectUri;
    }
}
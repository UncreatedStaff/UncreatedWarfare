using DanielWillett.ModularRpcs;
using DanielWillett.ModularRpcs.Protocol;
using DanielWillett.ModularRpcs.WebSockets;
using System;
using UnityEngine.Networking;

namespace Uncreated.Warfare.Networking;
internal static class HomebaseConnector
{
    public static async Task<bool> ConnectAsync(CancellationToken token = default)
    {
        Uri? connectUri = await GetConnectUri(token);
        if (connectUri == null)
            return false;

        L.LogDebug($"Connecting to homebase at: {connectUri}.");

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
            connection.Local.SetLogger(L.Logger);
            connection.OnReconnect += GetConnectUri;
        }
        catch (Exception ex)
        {
            L.LogError("Failed to open WebSocket client.");
            L.LogError(ex);
            return false;
        }

        await UniTask.SwitchToMainThread(token);
        Data.RpcConnection = connection;
        return true;
    }

    private static Task<Uri?> GetConnectUri(WebSocketClientsideRemoteRpcConnection connection)
    {
        return GetConnectUri(CancellationToken.None);
    }

    private static async Task<Uri?> GetConnectUri(CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);
        
        if (UCWarfare.Config.HomebaseConfig is not { Enabled: true, ConnectEndpoint.Length: > 0 })
        {
            L.LogWarning("Homebase disabled or not configured.");
            return null;
        }

        string? authJwt = null;

        if (!string.IsNullOrEmpty(UCWarfare.Config.HomebaseConfig.AuthEndpoint))
        {
            if (string.IsNullOrEmpty(UCWarfare.Config.HomebaseConfig.AuthKey))
            {
                L.LogWarning("Authentication key not configured.");
                return null;
            }

            Uri authUri = new Uri(UCWarfare.Config.HomebaseConfig.AuthEndpoint);
            
            using UnityWebRequest authRequest = UnityWebRequest.Get(authUri);
            authRequest.SetRequestHeader("Authorization", "Bearer " + UCWarfare.Config.HomebaseConfig.AuthKey);

            L.LogDebug($"Authenticating for homebase at: {authUri} with key {UCWarfare.Config.HomebaseConfig.AuthKey}.");

            try
            {
                await authRequest.SendWebRequest();
            }
            catch (Exception ex)
            {
                L.LogError("Failed to authenticate WebSocket client.");
                L.LogError(ex);
                return null;
            }

            await UniTask.SwitchToMainThread(token);

            authJwt = authRequest.downloadHandler.text;
        }

        Uri connectUri = new Uri(UCWarfare.Config.HomebaseConfig.ConnectEndpoint);

        if (authJwt != null)
        {
            connectUri = new Uri(connectUri, "?token=" + Uri.EscapeDataString(authJwt));
        }

        return connectUri;
    }
}
using Cysharp.Threading.Tasks;
using DanielWillett.ModularRpcs;
using DanielWillett.ModularRpcs.WebSockets;
using DanielWillett.ReflectionTools;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Uncreated.Warfare.Networking;
public static class HomebaseConnector
{
    public static async Task<bool> ConnectAsync(CancellationToken token = default)
    {
        await UniTask.SwitchToMainThread(token);

        if (UCWarfare.Config.HomebaseConfig is not { Enabled: true, ConnectEndpoint.Length: > 0 })
        {
            L.LogWarning("Homebase disabled or not configured.");
            return false;
        }

        string? authJwt = null;

        if (!string.IsNullOrEmpty(UCWarfare.Config.HomebaseConfig.AuthEndpoint))
        {
            if (string.IsNullOrEmpty(UCWarfare.Config.HomebaseConfig.AuthKey))
            {
                L.LogWarning("Authentication key not configured.");
                return false;
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
                return false;
            }

            await UniTask.SwitchToMainThread(token);

            authJwt = authRequest.downloadHandler.text;
        }

        Uri connectUri = new Uri(UCWarfare.Config.HomebaseConfig.ConnectEndpoint);

        if (authJwt != null)
        {
            connectUri = new Uri(connectUri, "?token=" + Uri.EscapeDataString(authJwt));
        }

        L.LogDebug($"Connecting to homebase at: {connectUri}.");

        WebSocketEndpoint endpoint = WebSocketEndpoint.AsClient(connectUri);
        WebSocketClientsideRemoteRpcConnection connection;
        try
        {
            connection = await endpoint.RequestConnectionAsync(Data.RpcRouter, Data.HomebaseLifetime, Data.RpcSerializer, token).ConfigureAwait(false);
            connection.Local.SetLogger(Accessor.Active);
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
}
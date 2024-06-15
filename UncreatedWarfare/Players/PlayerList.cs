using Cysharp.Threading.Tasks;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using JetBrains.Annotations;
using System;
using System.Threading;
using Uncreated.Networking;
using Uncreated.Warfare.Singletons;
using UnityEngine;

namespace Uncreated.Warfare.Players;

[RpcClass]
public class PlayerList : BaseSingletonComponent
{
    private const float UpdateTime = 5f;
    private float _lastTick;
    public static PlayerList? Instance { get; private set; }
    public override void Load()
    {
        Instance = this;
    }

    public override void Unload()
    {
        Instance = null;
    }

    [RpcSend, RpcTimeout(5 * Timeouts.Seconds)]
    protected internal virtual RpcTask TickPlayerList(CancellationToken token = default) => RpcTask.NotImplemented;

    [UsedImplicitly]
    private void Update()
    {
        if (Time.realtimeSinceStartup - _lastTick <= UpdateTime)
        {
            return;
        }

        _lastTick = Time.realtimeSinceStartup;

        UCWarfare.RunTask(async token =>
        {
            try
            {
                await TickPlayerList(token).IgnoreNoConnections();
            }
            catch (Exception ex)
            {
                await UniTask.SwitchToMainThread();
                L.LogError("Error ticking player list.");
                L.LogError(ex);
            }
        }, UCWarfare.UnloadCancel);
    }

    [RpcReceive]
    private void ReceiveUpdateDelay()
    {
        _lastTick = Time.realtimeSinceStartup;
        L.Log("received update delay.");
    }

    public static class NetCalls
    {
        public static NetCall SendTickPlayerList = new NetCall(ReceivePlayerListTick);

        [NetCall(NetCallOrigin.ServerOnly, KnownNetMessage.SendTickPlayerList)]
        private static void ReceivePlayerListTick(MessageContext ctx)
        {
            PlayerList? list = Instance;
            if (list != null)
                list._lastTick = Time.realtimeSinceStartup;
        }
    }
}

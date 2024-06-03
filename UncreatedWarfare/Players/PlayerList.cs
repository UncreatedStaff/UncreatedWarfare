using Cysharp.Threading.Tasks;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using JetBrains.Annotations;
using System;
using System.Threading.Tasks;
using Uncreated.Networking;
using Uncreated.Warfare.Singletons;
using UnityEngine;

namespace Uncreated.Warfare.Players;

[RpcClass]
public class PlayerList : BaseSingletonComponent
{
    private const float UpdateTime = 5f;
    public static PlayerList? Instance { get; private set; }
    private float _lastTick;
    public override void Load()
    {
        Instance = this;
    }
    public override void Unload()
    {
        Instance = null;
    }
    [UsedImplicitly]
    private void Update()
    {
        if (Time.realtimeSinceStartup - _lastTick <= UpdateTime)
        {
            return;
        }

        _lastTick = Time.realtimeSinceStartup;

        Task.Run(async () =>
        {
            try
            {
                await TickPlayerList();
            }
            catch (Exception ex)
            {
                await UniTask.SwitchToMainThread();
                L.LogError("Error ticking player list.");
                L.LogError(ex);
            }
        });
    }

    [RpcSend, RpcTimeout(5 * Timeouts.Seconds)]
    protected virtual RpcTask TickPlayerList() => _ = RpcTask.NotImplemented;

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

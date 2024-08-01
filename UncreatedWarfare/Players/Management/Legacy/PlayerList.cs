using Cysharp.Threading.Tasks;
using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Async;
using System;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Warfare.Singletons;

namespace Uncreated.Warfare.Players.Management.Legacy;

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
    protected internal virtual RpcTask<string> TickPlayerList(int num, CancellationToken token = default) => RpcTask<string>.NotImplemented;

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
                L.Log("received: " + await TickPlayerList(1).IgnoreNoConnections());
            }
            catch (Exception ex)
            {
                await UniTask.SwitchToMainThread();
                L.LogError("Error ticking player list.");
                L.LogError(ex);
            }
        });
    }

    [RpcReceive]
    private void ReceiveUpdateDelay()
    {
        _lastTick = Time.realtimeSinceStartup;
        L.Log("received update delay.");
    }
}

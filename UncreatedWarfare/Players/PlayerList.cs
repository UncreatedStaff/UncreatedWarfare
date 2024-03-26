using JetBrains.Annotations;
using Uncreated.Networking;
using Uncreated.Warfare.Singletons;
using UnityEngine;

namespace Uncreated.Warfare.Players;
public sealed class PlayerList : BaseSingletonComponent
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
        if (Time.realtimeSinceStartup - _lastTick > UpdateTime)
        {
            _lastTick = Time.realtimeSinceStartup;

            if (UCWarfare.CanUseNetCall)
            {
                NetCalls.SendTickPlayerList.NetInvoke();
            }
        }
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

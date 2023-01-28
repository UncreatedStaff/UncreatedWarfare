using JetBrains.Annotations;
using System.Threading.Tasks;
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

        [NetCall(ENetCall.FROM_SERVER, 1102)]
        private static async Task ReceivePlayerListTick(MessageContext ctx)
        {
            await UCWarfare.ToUpdate();
            if (Instance != null)
            {
                Instance._lastTick = Time.realtimeSinceStartup;
            }
        }
    }
}

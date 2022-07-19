using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Networking;

public sealed class SpyTask
{
    private readonly SpyTaskAwaiter _awaiter;
    public SpyTask(SteamPlayer target)
    {
        _awaiter = new SpyTaskAwaiter(this);
        target.player.sendScreenshot(CSteamID.Nil, OnReceive);
        L.Log("sent");
    }
    private void OnReceive(CSteamID player, byte[] jpg)
    {
        _awaiter.rtn = jpg;
        _awaiter.TellReceived();
    }
    public static SpyTask RequestScreenshot(SteamPlayer target)
    {
        return new SpyTask(target);
    }
    public SpyTaskAwaiter GetAwaiter() => _awaiter;
    public sealed class SpyTaskAwaiter : INotifyCompletion
    {
        public System.Action continuation;
        private readonly SpyTask _task;
        public byte[] rtn;
        public bool IsCompleted => _isCompleted;
        private bool _isCompleted;
        public SpyTaskAwaiter(SpyTask task)
        {
            _task = task;
        }
        public void TellReceived()
        {
            _isCompleted = true;
            continuation.Invoke();
        }
        public void OnCompleted(System.Action continuation)
        {
            this.continuation = continuation;
        }
        public byte[] GetResult()
        {
            return rtn ?? new byte[0];
        }
    }
}

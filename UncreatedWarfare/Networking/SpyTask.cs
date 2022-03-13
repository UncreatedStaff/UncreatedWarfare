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
            L.Log("received");
        }
        public void OnCompleted(System.Action continuation)
        {
            this.continuation = continuation;
        }
        public byte[] GetResult()
        {
            /*
            L.Log("getting result");
            int counter = 0;
            int maxloop = DEFAULT_TIMEOUT_MS / POLL_SPEED_MS;
            while (!_isCompleted && counter < maxloop)
            {
                L.Log("awaiting " + UCWarfare.IsMainThread.ToString());
                Thread.Sleep(POLL_SPEED_MS);
                counter++;
            }
            L.Log("got result " + counter);
            return rtn ?? new byte[0];*/
            return rtn ?? new byte[0];
        }
    }
}

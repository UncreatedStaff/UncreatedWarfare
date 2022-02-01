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

public sealed class SpyTask : IDisposable
{
    public static Dictionary<ulong, SpyTask> awaiters = new Dictionary<ulong, SpyTask>(1);
    private readonly SpyTaskAwaiter _awaiter;
    private readonly SemaphoreSlim semaphore;
    public SpyTask(SteamPlayer target)
    {
        _awaiter = new SpyTaskAwaiter(this);
        semaphore = new SemaphoreSlim(1, 1);
        target.player.sendScreenshot(CSteamID.Nil, OnReceive);
        semaphore.Wait();
    }
    ~SpyTask()
    {
        semaphore.Dispose();
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
    public void Dispose()
    {
        semaphore.Dispose();
        GC.SuppressFinalize(this);
    }
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
            _task.semaphore.Release();
            _task.Dispose();
        }
        public void OnCompleted(System.Action continuation)
        {
            this.continuation = continuation;
        }
        public byte[] GetResult()
        {
            CancellationTokenSource token = new CancellationTokenSource(10000); // prevent blocking thread
            _task.semaphore.Wait(token.Token);
            _task.Dispose();
            return rtn ?? new byte[0];
        }
    }
}

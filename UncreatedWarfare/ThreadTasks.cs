using SDG.Unturned;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Action = System.Action;

namespace Uncreated.Warfare;

public sealed class MainThreadTask
{
    internal const int DEFAULT_TIMEOUT_MS = 5000;
    internal const int POLL_SPEED_MS = 25;
    private readonly bool skipFrame;
    private volatile bool isCompleted = false;
    private readonly MainThreadResult awaiter;
    public readonly CancellationToken Token;
    public MainThreadTask(bool skipFrame, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        this.skipFrame = skipFrame;
        this.Token = token;
        awaiter = new MainThreadResult(this);
    }
    public MainThreadResult GetAwaiter()
    {
        return awaiter;
    }
    public sealed class MainThreadResult : INotifyCompletion
    {
        public readonly MainThreadTask Task;
        public MainThreadResult(MainThreadTask task)
        {
            this.Task = task ?? throw new ArgumentNullException(nameof(task), "Task was null in MainThreadResult constructor.");
        }
        internal Action continuation;
        public bool IsCompleted { get => Task.isCompleted; }
        public void OnCompleted(Action continuation)
        {
            Task.Token.ThrowIfCancellationRequested();
            if (UCWarfare.IsMainThread && !Task.skipFrame)
            {
                continuation();
                Task.isCompleted = true;
            }
            else
            {
                this.continuation = continuation;
                lock (UCWarfare.ThreadActionRequests)
                    UCWarfare.ThreadActionRequests.Enqueue(this);
            }
        }
        internal void Complete()
        {
            Task.isCompleted = true;
        }

        private bool WaitCheck() => Task.isCompleted;
        public void GetResult()
        {
            if (UCWarfare.IsMainThread)
                return;
            UCWarfare.SpinWaitUntil(WaitCheck, DEFAULT_TIMEOUT_MS);
            /*
            int counter = 0;
            int maxloop = DEFAULT_TIMEOUT_MS / POLL_SPEED_MS;
            while (!Task.isCompleted && counter < maxloop)
            {
                Task.Token.ThrowIfCancellationRequested();
                Thread.Sleep(POLL_SPEED_MS);
                counter++;
            }*/
        }
    }
}
public sealed class LevelLoadTask
{
    internal const int DEFAULT_TIMEOUT_MS = 120000;
    internal const int POLL_SPEED_MS = 25;
    private bool isCompleted = false;
    private readonly LevelLoadResult awaiter;
    public readonly CancellationToken Token;
    public LevelLoadTask(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        awaiter = new LevelLoadResult(this);
        Token = token;
    }
    public LevelLoadResult GetAwaiter()
    {
        return awaiter;
    }
    public sealed class LevelLoadResult : INotifyCompletion
    {
        public readonly LevelLoadTask Task;
        public LevelLoadResult(LevelLoadTask task)
        {
            this.Task = task ?? throw new ArgumentNullException(nameof(task), "Task was null in MainThreadResult constructor.");
        }
        internal Action continuation;
        public bool IsCompleted { get => Task.isCompleted; }
        public void OnCompleted(Action continuation)
        {
            Task.Token.ThrowIfCancellationRequested();
            if (UCWarfare.IsMainThread && Level.isLoaded)
            {
                continuation();
                Task.isCompleted = true;
            }
            else
            {
                this.continuation = continuation;
                lock (UCWarfare.LevelLoadRequests)
                    UCWarfare.LevelLoadRequests.Enqueue(this);
            }
        }
        internal void Complete()
        {
            Task.isCompleted = true;
        }
        private bool WaitCheck() => Task.isCompleted;
        public void GetResult()
        {
            if (UCWarfare.IsMainThread && Level.isLoaded)
                return;
            UCWarfare.SpinWaitUntil(WaitCheck, DEFAULT_TIMEOUT_MS);
            /*
            int counter = 0;
            int maxloop = DEFAULT_TIMEOUT_MS / POLL_SPEED_MS;
            while (!Task.isCompleted && counter < maxloop)
            {
                Task.Token.ThrowIfCancellationRequested();
                Thread.Sleep(POLL_SPEED_MS);
                counter++;
            }*/
        }
    }
}

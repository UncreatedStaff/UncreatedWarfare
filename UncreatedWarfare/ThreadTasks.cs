using SDG.Unturned;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Action = System.Action;

namespace Uncreated.Warfare;

public class MainThreadTask
{
    internal static MainThreadTask CompletedNoSkip => new MainThreadTask(false);
    internal static MainThreadTask CompletedSkip => new MainThreadTask(true);
    internal const int DefaultTimeout = 5000;
    protected readonly bool SkipFrame;
    protected volatile bool IsCompleted = false;
    protected readonly MainThreadResult Awaiter;
    public readonly CancellationToken Token;

    private MainThreadTask(bool skipFrame)
    {
        this.SkipFrame = skipFrame;
        this.Token = CancellationToken.None;
        IsCompleted = true;
        Awaiter = new MainThreadResult(this);
    }
    public MainThreadTask(bool skipFrame, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        this.SkipFrame = skipFrame;
        this.Token = token;
        Awaiter = new MainThreadResult(this);
    }
    public MainThreadResult GetAwaiter()
    {
        return Awaiter;
    }
    public sealed class MainThreadResult : INotifyCompletion
    {
        internal Action Continuation;
        public readonly MainThreadTask Task;
        public MainThreadResult(MainThreadTask task)
        {
            this.Task = task ?? throw new ArgumentNullException(nameof(task), "Task was null in MainThreadResult constructor.");
        }
        public bool IsCompleted { get => UCWarfare.IsMainThread || Task.IsCompleted; }
        public void OnCompleted(Action continuation)
        {
            Task.Token.ThrowIfCancellationRequested();
            if (UCWarfare.IsMainThread && !Task.SkipFrame)
            {
                continuation();
                Task.IsCompleted = true;
            }
            else
            {
                this.Continuation = continuation;
                lock (UCWarfare.ThreadActionRequests)
                    UCWarfare.ThreadActionRequests.Enqueue(this);
            }
        }
        internal void Complete()
        {
            Task.IsCompleted = true;
        }

        private bool WaitCheck() => Task.IsCompleted;
        public void GetResult()
        {
            if (UCWarfare.IsMainThread)
                return;
            UCWarfare.SpinWaitUntil(WaitCheck, DefaultTimeout);
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

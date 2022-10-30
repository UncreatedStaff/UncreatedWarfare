using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Uncreated.Warfare
{
    public sealed class MainThreadTask
    {
        internal const int DEFAULT_TIMEOUT_MS = 5000;
        internal const int POLL_SPEED_MS = 25;
        private readonly bool skipFrame;
        private bool isCompleted = false;
        private readonly MainThreadResult awaiter;
        public MainThreadTask(bool skipFrame)
        {
            this.skipFrame = skipFrame;
            awaiter = new MainThreadResult(this);
        }
        public MainThreadResult GetAwaiter()
        {
            return awaiter;
        }
        public sealed class MainThreadResult : INotifyCompletion
        {
            private readonly MainThreadTask task;
            public MainThreadResult(MainThreadTask task)
            {
                this.task = task ?? throw new ArgumentNullException(nameof(task), "Task was null in MainThreadResult constructor.");
            }
            internal Action continuation;
            public bool IsCompleted { get => task.isCompleted; }
            public void OnCompleted(Action continuation)
            {
                if (UCWarfare.IsMainThread && !task.skipFrame)
                {
                    continuation();
                    task.isCompleted = true;
                }
                else
                {
                    this.continuation = continuation;
                    UCWarfare.ThreadActionRequests.Enqueue(this);
                }
            }
            internal void Complete()
            {
                task.isCompleted = true;
            }
            public void GetResult()
            {
                if (UCWarfare.IsMainThread)
                {
                    return;
                }
                int counter = 0;
                int maxloop = DEFAULT_TIMEOUT_MS / POLL_SPEED_MS;
                while (!task.isCompleted && counter < maxloop)
                {
                    Thread.Sleep(POLL_SPEED_MS);
                    counter++;
                }
            }
        }
    }
}

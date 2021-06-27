using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Uncreated
{
    public static class ThreadTool
    {
        private static SynchronizationContext GameThreadContext;
        public static void SetGameThread() => GameThreadContext = SynchronizationContext.Current;
        ///<summary>Source: "https://thomaslevesque.com/2015/11/11/explicitly-switch-to-the-ui-thread-in-an-async-method/"</summary>
        public class ResumeOnMainAwaiter : INotifyCompletion
        {
            private readonly SendOrPostCallback _postCallback = (state) => ((Action)state).Invoke();
            private readonly SynchronizationContext _context;
            public ResumeOnMainAwaiter(SynchronizationContext context)
            {
                this._context = context;
            }
            public bool IsCompleted => _context == SynchronizationContext.Current;
            public void OnCompleted(Action continuation) => _context.Post(_postCallback, continuation);
            public void GetResult() { }
        }
        public static ResumeOnMainAwaiter GetAwaiter(this SynchronizationContext context) => new ResumeOnMainAwaiter(context);
        /// <summary>
        /// Call await SwitchToGameThread() to switch to the main unity thread which should be set using <see cref="SetGameThread"/> from the game thread.
        /// </summary>
        public static async Task<SynchronizationContext> SwitchToGameThread()
        {
            if (GameThreadContext != default) await GameThreadContext;
            return SynchronizationContext.Current;
        }
    }
}

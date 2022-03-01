using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Uncreated.Networking
{
    public sealed class NetTask : IDisposable
    {
        internal const int DEFAULT_TIMEOUT_MS = 5000;
        internal const int POLL_SPEED_MS = 25;
        private readonly CancellationTokenSource _src = new CancellationTokenSource();
        private readonly NetCallRequestResult result;
        private readonly BaseNetCall caller;
        private readonly int Timeout;
        public NetTask(BaseNetCall caller, int TimeoutMS = DEFAULT_TIMEOUT_MS)
        {
            this.caller = caller;
            this.Timeout = TimeoutMS;
            result = new NetCallRequestResult(this);
        }
        internal void RegisterListener(BaseNetCall caller)
        {
            _src.CancelAfter(Timeout);
            NetFactory.Instance?.RegisterListener(this, caller);
        }
        private Response _parameters = Response.FAIL;
        internal void TellCompleted(object[] parameters)
        {
            isCompleted = true;
            if (parameters == null || parameters.Length == 0)
            {
                _parameters = Response.FAIL;
                _src.Cancel();
                return;
            }
            object[] p = parameters.Length == 1 ? new object[0] : new object[parameters.Length - 1];
            if (p.Length > 0)
                Array.Copy(parameters, 1, p, 0, p.Length);
            _parameters = new Response(true, parameters[0] as IConnection, p);
            _src.Cancel();
            result.TellComplete();
        }
        public NetCallRequestResult GetAwaiter() => result;

        public bool isCompleted = false;
        private bool disposedValue;

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _src.Dispose();
                }
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        public sealed class NetTaskResult : INotifyCompletion
        {
            private readonly NetTask task;
            public NetTaskResult(NetTask task)
            {
                this.task = task ?? throw new ArgumentNullException(nameof(task), "Task was null in NetTaskResult constructor.");
            }
            public bool IsCompleted { get => task.isCompleted; }
            public void OnCompleted(Action continuation)
            {
                continuation.Invoke();
                task.Dispose();
            }
            public Response GetResult()
            {
                if (task._src.IsCancellationRequested) return task._parameters;
                int counter = 0;
                int maxloop = task.Timeout / POLL_SPEED_MS;
                while(!task._src.IsCancellationRequested && counter < maxloop)
                {
                    Thread.Sleep(POLL_SPEED_MS);
                    counter++;
                }
                NetFactory.Instance?.TimeoutListener(task, task.caller);
                return task._parameters;
            }
        }
        public sealed class NetCallRequestResult : INotifyCompletion
        {
            private readonly NetTask task;
            public NetCallRequestResult(NetTask task)
            {
                this.task = task ?? throw new ArgumentNullException(nameof(task), "Task was null in NetTaskResult constructor.");
            }
            public bool IsCompleted { get => task.isCompleted; }
            private Action continuation;
            public void OnCompleted(Action continuation)
            {
                this.continuation = continuation;
            }
            internal void TellComplete()
            {
                task.isCompleted = true;
                continuation?.Invoke();
                task.Dispose();
            }
            public Response GetResult()
            {
                if (task._src.IsCancellationRequested) return task._parameters;
                int counter = 0;
                int maxloop = task.Timeout / POLL_SPEED_MS;
                while(!task._src.IsCancellationRequested && counter < maxloop)
                {
                    Thread.Sleep(POLL_SPEED_MS);
                    counter++;
                }
                NetFactory.Instance?.TimeoutListener(task, task.caller);
                return task._parameters;
            }
        }
        public struct Response
        {
            public static readonly Response FAIL = new Response(false, null, new object[0]);
            public bool Responded;
            public IConnection Connection;
            public object[] Parameters;
            public Response(bool responded, IConnection connection, object[] parameters)
            {
                this.Responded = responded;
                this.Connection = connection;
                this.Parameters = parameters;
            }
        }
    }
}

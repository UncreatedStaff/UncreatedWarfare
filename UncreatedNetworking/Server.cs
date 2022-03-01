using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Globalization;

namespace Uncreated.Networking
{
    public delegate void ConnectionDelegate(IConnection connection);
    public delegate void IdentityDelegate(string identity);
    public class Server : IDisposable
    {
        public readonly List<IConnection> ConnectedClients;
        private readonly TcpListener _listener;
        public static Server Instance;
        public TcpListener Listener => _listener;
        private bool disposedValue;
        public ushort Port { get; private set; }
        public event MessageReceivedDelegate OnReceived;
        public event MessageParsedDelegate OnParsed;
        public event ConnectionDelegate OnConnectionVerified;
        public event IdentityDelegate OnClientDisconnected;
        public event MessageSentDelegate OnAutoSent;
        internal void InvokeClientVerified(IConnection connection) => OnConnectionVerified?.Invoke(connection);
        public Server(bool localOnly, ushort port)
        {
            if (Instance != null) Instance.Dispose();
            Instance = this;
            this.Port = port;
            ConnectedClients = new List<IConnection>();
            _listener = new TcpListener(localOnly ? IPAddress.Loopback : IPAddress.Any, port);
        }
        public void Initialize()
        {
            _listener.Start();
            _listener.BeginAcceptTcpClient(OnClientAccepted, _listener);
        }
        public IConnection GetClient(string identity)
        {
            for (int i = 0; i < ConnectedClients.Count; i++)
            {
                if (ConnectedClients[i].Identity == identity)
                    return ConnectedClients[i];
            }
            return null;
        }
        /// <summary>
        /// Gets active client by that name
        /// </summary>
        public bool TryGetClient(string identity, out IConnection connection)
        {
            for (int i = 0; i < ConnectedClients.Count; i++)
            {
                if (ConnectedClients[i].Identity == identity && ConnectedClients[i].IsActive)
                {
                    connection = ConnectedClients[i];
                    return true;
                }
            }
            connection = null;
            return false;
        }
        protected uint GetLowestAvailableNetID()
        {
            uint rtn = 0;
            bool found;
            while (true)
            {
                found = false;
                for (int i = 0; i < ConnectedClients.Count; i++)
                {
                    if (ConnectedClients[i].NetworkID == rtn)
                    {
                        found = true;
                        break;
                    }
                }
                if (found) rtn++;
                else return rtn;
            }
        }
        private void OnClientAccepted(IAsyncResult ar)
        {
            try
            {
                TcpClient pending = _listener.EndAcceptTcpClient(ar);
                if (pending != null)
                {
                    pending.ReceiveBufferSize = TcpClientConnection.BUFF_SIZE;
                    pending.SendBufferSize = TcpClientConnection.BUFF_SIZE;
                    IConnection connection = new TcpClientConnection(GetLowestAvailableNetID(), pending);
                    connection.OnReceived += OnReceived;
                    connection.OnAutoSent += OnAutoSent;
                    connection.OnDisconnect += OnClientDisconnected;
                    connection.OnParsed += OnParsed;
                    ConnectedClients.Add(connection);
                    connection.Listen();
                }
            }
            catch (ObjectDisposedException)
            {
                return; // disposed Server object
            }
            catch (Exception ex)
            {
                Logging.LogError("Error accepting client: " + ex.GetType().Name);
                Logging.LogError(ex);
            }
            try
            {
                _listener.BeginAcceptTcpClient(OnClientAccepted, _listener);
            }
            catch (InvalidOperationException)
            {}
        }
        public void Shutdown()
        {
            foreach (IConnection connection in ConnectedClients)
            {
                connection.Close();
                connection.OnReceived -= OnReceived;
                connection.OnAutoSent -= OnAutoSent;
                connection.OnDisconnect -= OnClientDisconnected;
                connection.OnParsed -= OnParsed;
                connection.Dispose();
            }
            ConnectedClients.Clear();
            _listener.Stop();
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Shutdown();
                    _listener.Server.Disconnect(false);
                    _listener.Server.Dispose();
                }

                // TODO: set large fields to null
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

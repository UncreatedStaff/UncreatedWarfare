using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Networking
{
    public class TcpServerConnection : IConnection
    {
        private bool _isActive;
        protected byte[] _buffer;
        public string Identity { get; set; }
        public bool IsActive => _isActive;
        uint _netId;
        public uint NetworkID => _netId;
        private TcpClient _socket;
        public TcpClient Socket => _socket;
        private NetworkStream _stream;
        public event MessageReceivedDelegate OnReceived;
        public event MessageParsedDelegate OnParsed;
        public event MessageSentDelegate OnAutoSent;
        public event IdentityDelegate OnDisconnect;
        public event IdentityDelegate OnServerConnectionEstablished;
        public readonly int Port;
        public readonly string Host;
        private IAsyncResult connectingAr;
        public TcpServerConnection(TcpClient client, string host, int port, string identity)
        {
            Port = port;
            Host = host;
            _netId = uint.MaxValue;
            _socket = client;
            _buffer = new byte[TcpClientConnection.BUFF_SIZE];
            Identity = identity;
        }
        public void Close()
        {
            try
            {
                _stream?.Close();
                _stream?.Dispose();
                _socket.Close();
                _socket.Dispose();
                _isActive = false;
            }
            catch (Exception ex)
            {
                Logging.LogError("Error closing connection: ");
                Logging.LogError(ex);
            }
        }
        internal void AssertConnected()
        {
            if (connectingAr != null && !connectingAr.IsCompleted)
            {
                try
                {
                    connectingAr.AsyncWaitHandle.WaitOne();
                }
                catch { }
            }
        }
        public void Listen()
        {
            try
            {
                if (_stream == null)
                    _stream = _socket.GetStream();
                _stream.BeginRead(_buffer, 0, TcpClientConnection.BUFF_SIZE, ReceivedCallback, null);
            }
            catch (Exception ex)
            {
                Logging.LogError("Error listening for server messages: ");
                Logging.LogError(ex);
            }
        }
        private void ReceivedCallback(IAsyncResult ar)
        {
            try
            {
                int receivedBytes = _stream.EndRead(ar);
                if (receivedBytes > 0)
                {
                    byte[] received = new byte[receivedBytes];
                    Buffer.BlockCopy(_buffer, 0, received, 0, receivedBytes);
                    OnReceived?.Invoke(received, this);
                    Receive(received);
                    Listen();
                }
                else
                {
                    Close();
                }
            }
            catch (IOException)
            {
                LogAndReconnect();
            }
            catch (ArgumentException ex)
            {
                Logging.LogError("Error receiving server messages: ");
                Logging.LogError(ex);
                Listen();
            }
            catch (Exception ex)
            {
                Logging.LogError("Error receiving server messages: ");
                Logging.LogError(ex);
                Close();
            }
        }
        private void LogAndReconnect()
        {
            Logging.Log("TCP Server was shutdown, looping reconnect request.", ConsoleColor.DarkYellow);
            try
            {
                ResetConnectionAndStartReconnecting();
            }
            catch (Exception ex)
            {
                Logging.LogError("Error reconnecting: ");
                Logging.LogError(ex);
            }
        }
        #region buffer extension
        public byte[] CurrentMessage;
        public int CurrentSize;
        public void Receive(byte[] message)
        {
            int size = CurrentMessage == null ? BitConverter.ToInt32(message, sizeof(ushort)) : CurrentSize;
            int expSize = size + sizeof(ushort) + sizeof(int); // overhead data and message
            if (message.Length < expSize || CurrentMessage != null)
            {
                if (CurrentMessage == null) // starting a long message
                {
                    CurrentMessage = message;
                    CurrentSize = size;
                }
                else // continuing a long message
                {
                    byte[] old = CurrentMessage;
                    CurrentMessage = new byte[old.Length + message.Length];
                    Buffer.BlockCopy(old, 0, CurrentMessage, 0, old.Length);
                    Buffer.BlockCopy(message, 0, CurrentMessage, old.Length, message.Length);
                    if (CurrentMessage.Length >= expSize)
                    {
                        Parse(CurrentMessage);
                        CurrentMessage = null;
                        CurrentSize = 0;
                        return;
                    }
                }
            }
            else
            {
                Parse(message);
                CurrentMessage = null;
                CurrentSize = 0;
            }
        }
        private void Parse(byte[] message)
        {
            if (NetFactory.Parse(message, this))
                OnParsed?.Invoke(message, this);
        }
        #endregion
        public void ResetConnectionAndStartReconnecting()
        {
            OnDisconnect?.Invoke(Identity);
            _isActive = false;
            _socket.Dispose();
            _socket = new TcpClient()
            {
                SendBufferSize = TcpClientConnection.BUFF_SIZE,
                ReceiveBufferSize = TcpClientConnection.BUFF_SIZE
            };
            _stream = null;
            StartReconnect();
        }
        public void Send(byte[] data)
        {
            bool allow = true;
            OnAutoSent?.Invoke(data, this, ref allow);
            if (!allow) return;
            SendManual(data);
        }
        public void SendManual(byte[] data)
        {
            if (!IsActive || _socket == null || _socket.Connected == false)
            {
                Logging.LogError("Message not sent while server is disconnected: " + string.Join(",", data));
                return;
            }
            if (_stream == null)
                _stream = _socket.GetStream();
            _stream.BeginWrite(data, 0, data.Length, WriteComplete, data);
        }
        public unsafe void WriteComplete(IAsyncResult ar)
        {
            if (_stream == null)
            {
                LogAndReconnect();
                ar.AsyncWaitHandle.Dispose();
                return;
            }
            try
            {
                _stream.EndWrite(ar);
            }
            catch (IOException ex)
            {
                ushort? id = null;
                if (ar.AsyncState is byte[] bytes && bytes.Length >= sizeof(ushort))
                {
                    fixed (byte* b = bytes)
                        id = *(ushort*)b;
                }
                Logging.LogWarning("Unexpected error writing message: " + (id?.ToString() ?? "* unknown message * ") + "!");
                Logging.LogError(ex);
                LogAndReconnect();
            }
            catch (Exception ex)
            {
                Logging.LogError($"Error writing to {NetworkID}: ");
                Logging.LogError(ex);
            }
            ar.AsyncWaitHandle.Dispose();
        }
        public void SetNetID(uint id)
        {
            _netId = id;
        }

        private int _connectTries = 0;
        const int MAX_CONNECT_TRIES = -1;
        public void StartConnect(bool first)
        {
            if (first) _connectTries = 0;
            try
            {
                connectingAr = _socket.BeginConnect(Host, Port, ConnectResolved, null);
            }
            catch (Exception ex)
            {
                Logging.LogError("Error connecting to the server: ");
                Logging.LogError(ex);
            }
        }
        private void ConnectResolved(IAsyncResult ar)
        {
            try
            {
                _socket.EndConnect(ar);
                _isActive = true;
                OnServerConnectionEstablished?.Invoke(this.Identity);
                Listen();
                SharedInvocations.RequestVerify.Invoke(this, Identity);
            }
            catch (Exception ex)
            {
                
                Logging.LogError($"Error connecting to the server ({_connectTries} / {MAX_CONNECT_TRIES}) - ({ex.GetType().Name}).");
                if (_isActive || _connectTries >= MAX_CONNECT_TRIES)
                {
                    StartReconnect();
                    Logging.Log("TCP Server not live, looping reconnect request.", ConsoleColor.DarkYellow);
                }
                else
                {
                    _connectTries++;
                    StartConnect(false);
                }
            }
            ar.AsyncWaitHandle.Dispose();
        }
        public void StartReconnect()
        {
            try
            {
                connectingAr = _socket.BeginConnect(Host, Port, ReconnectResolved, null);
            }
            catch (Exception ex)
            {
                Logging.LogError("Error connecting to the server: ");
                Logging.LogError(ex);
            }
        }
        private void ReconnectResolved(IAsyncResult ar)
        {
            try
            {
                _socket.EndConnect(ar);
                _isActive = true;
                OnServerConnectionEstablished?.Invoke(this.Identity);
                Listen();
                SharedInvocations.RequestVerify.Invoke(this, Identity);
            }
            catch (SocketException)
            {
                StartReconnect();
            }
            catch (Exception ex)
            {
                Logging.LogError("Error trying to reconnect after the connection to the server was lost.");
                Logging.LogError(ex);
                StartReconnect();
            }
            ar.AsyncWaitHandle.Dispose();
        }
        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _stream?.Close();
                    _stream?.Dispose();
                    _socket?.Close();
                    _socket?.Dispose();
                }
                _stream = null;
                _socket = null;
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        public int CompareTo(IConnection other) => _netId.CompareTo(other.NetworkID);
    }
}

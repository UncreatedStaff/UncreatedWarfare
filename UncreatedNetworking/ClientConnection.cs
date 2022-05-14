using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Networking
{
    public delegate void MessageReceivedDelegate(byte[] bytes, IConnection connection);
    public delegate void MessageParsedDelegate(byte[] bytes, IConnection connection);
    public delegate void MessageSentDelegate(byte[] bytes, IConnection connection, ref bool Allow);

    /// <summary>Exists only on the server-side.</summary>
    public class TcpClientConnection : IConnection
    {
        internal const int BUFF_SIZE = 4096;
        protected byte[] _buffer;
        protected uint _netId;
        public uint NetworkID => _netId;
        private TcpClient _socket;
        public TcpClient Socket { get => _socket; }
        public string Identity { get; set;}
        protected bool _isActive = false;
        public bool IsActive => _isActive;
        private NetworkStream _stream;
        public event MessageReceivedDelegate OnReceived;
        public event MessageParsedDelegate OnParsed;
        public event MessageSentDelegate OnAutoSent;
        public event IdentityDelegate OnDisconnect;
        public TcpClientConnection(uint ID, TcpClient client)
        {
            _netId = ID;
            _socket = client;
            _buffer = new byte[BUFF_SIZE];
            _stream = client.GetStream();
            _isActive = true;
            Identity = ID.ToString();
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
            //Logging.Log("Sending " + data.Length.ToString() + " bytes of data to " + Identity, ConsoleColor.DarkGray);
            if (!IsActive || _socket == null || _socket.Connected == false)
                return;
            try
            {
                if (_stream == null)
                    _stream = _socket.GetStream();
                _stream.BeginWrite(data, 0, data.Length, WriteComplete, data);
            }
            catch (IOException ex)
            {
                Logging.LogError($"Error writing to {NetworkID}, disconnecting.");
                Logging.LogError(ex);
                Disconnect();
            }
            catch (Exception ex)
            {
                Logging.LogError($"Error writing to {NetworkID}: ");
                Logging.LogError(ex);
            }
        }
        public void WriteComplete(IAsyncResult ar)
        {
            try
            {
                _stream.EndWrite(ar);
            }
            catch (Exception ex)
            {
                Logging.LogError($"Error writing to {NetworkID}: ");
                Logging.LogError(ex);
            }
            ar.AsyncWaitHandle.Dispose();
        }
        public void Listen()
        {
            try
            {
                if (_stream == null)
                    _stream = _socket.GetStream();
                _stream.BeginRead(_buffer, 0, BUFF_SIZE, ReceivedCallback, null);
            }
            catch (Exception ex)
            {
                Logging.LogError("Error listening for client messages: ");
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
                    Disconnect();
                }
            }
            catch (IOException)
            {
                Disconnect();
            }
            catch (Exception ex)
            {
                Logging.LogError("Error receiving client messages: ");
                Logging.LogError(ex);
                Close();
            }
            ar.AsyncWaitHandle.Dispose();
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
                    CurrentMessage = new byte[message.Length];
                    CurrentSize = size;
                    Buffer.BlockCopy(message, 0, CurrentMessage, 0, message.Length);
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
        public void Disconnect()
        {
            Logging.Log($"Disconnecting {Identity} ({NetworkID}) because the connection was terminated.", ConsoleColor.DarkYellow);
            OnDisconnect?.Invoke(Identity);
            Server.Instance?.ConnectedClients.RemoveAll(x => x.NetworkID == NetworkID);
            Close();
            Dispose();
        }
        public void SetNetID(uint id)
        {
            _netId = id;
        }
        public int CompareTo(IConnection other) => _netId.CompareTo(other.NetworkID);


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
                _isActive = false;
                
                _socket = null;
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void Close()
        {
            try
            {
                if (_socket != null)
                {
                    _stream?.Close();
                    _stream?.Dispose();
                    _socket?.Close();
                    _socket?.Dispose();
                }
                _isActive = false;
            }
            catch (Exception ex)
            {
                Logging.LogError("Error closing connection: ");
                Logging.LogError(ex);
            }
        }
    }
    public interface IConnection : IDisposable, IComparable<IConnection>
    {
        bool IsActive { get; }
        uint NetworkID { get; }
        string Identity { get; set; }
        void Send(byte[] data);
        void SendManual(byte[] data);
        void Close();
        void Listen();
        void SetNetID(uint id);

        event MessageReceivedDelegate OnReceived;
        event MessageParsedDelegate OnParsed;
        event MessageSentDelegate OnAutoSent;
        event IdentityDelegate OnDisconnect;
    }
}

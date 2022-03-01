using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;
using Uncreated.Networking.Encoding;

namespace Uncreated.Networking
{
    public class Client : IDisposable
    {
        public TcpServerConnection connection;
        public Client(ConnectionInfo info) : this(info.ip, info.port, info.identity) { }
        public Client(string ip, ushort port, string identity)
        {
            TcpClient client = new TcpClient()
            {
                SendBufferSize = TcpClientConnection.BUFF_SIZE,
                ReceiveBufferSize = TcpClientConnection.BUFF_SIZE
            };
            this.connection = new TcpServerConnection(client, ip, port, identity);
            this.connection.StartConnect(true);
        }
        public void AssertConnected() => connection.AssertConnected();
        public void Send(byte[] message) => connection.Send(message);
        public void SendManual(byte[] message) => connection.SendManual(message);
        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    connection.Close();
                    connection.Dispose();
                }
                connection = null;
                disposedValue = true;
            }
        }
        public virtual void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        public struct ConnectionInfo
        {
            public string ip;
            public ushort port;
            public string identity;
            [JsonConstructor]
            public ConnectionInfo(string ip, ushort port, string identity)
            {
                this.port = port;
                this.ip = ip;
                this.identity = identity;
            }
            public static ConnectionInfo Read(ByteReader R)
                => new ConnectionInfo(R.ReadString(), R.ReadUInt16(), R.ReadString());
            public static void Write(ByteWriter W, ConnectionInfo C)
            {
                W.Write(C.ip);
                W.Write(C.port);
                W.Write(C.identity);
            }
        }
    }
}

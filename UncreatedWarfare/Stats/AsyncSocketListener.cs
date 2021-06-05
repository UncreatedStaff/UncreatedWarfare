using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace Uncreated.Warfare.Stats
{
    internal class StateObject
    {
        public const int BufferSize = 2048; // 0x0000-0x007F use 1 byte, 0x0080-0x07FF use 2 bytes, 0x0800-0xFFFF use 3 bytes, 0x10000-0x10FFFF use 4 bytes.
        public byte[] buffer = new byte[BufferSize];
        public List<byte> allData = new List<byte>();
        public Socket workSocket = null;
        public void AppendBuffer(int length)
        {
            byte[] temp = new byte[length];
            Array.Copy(buffer, 0, temp, 0, length);
            allData.AddRange(temp);
        }
    }
    public class AsyncListenServer : IDisposable
    {
        public class MessageReceivedEventArgs : EventArgs { public byte[] message; public string utf8; }
        public event EventHandler<MessageReceivedEventArgs> OnMessageReceived;
        public readonly byte[] EndOfFileUTF8 = new byte[] { 0x3c, 0x45, 0x4f, 0x46, 0x3e };
        IPHostEntry host;
        IPAddress address;
        EndPoint endpoint;
        Socket client;
        public void Start()
        {
            try
            {
                host = Dns.GetHostEntry("localhost");
                address = host.AddressList[0];
                endpoint = new IPEndPoint(address, 8081);
                client = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
                {
                    SendTimeout = 5000
                };
                client.Bind(endpoint);
                ReconnectSync(true);
                MainThread(); // event loop
            }
            catch (Exception ex)
            {
                F.LogError(ex);
            }
        }
        public bool ReconnectSync(bool firstTime = false)
        {
            F.Log((firstTime ? "H" : "Reh") + "osting socket.", ConsoleColor.Cyan);
            try
            {
                client.Listen(10);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public void MainThread()
        {
            while (true)
            {
                //F.Log("about to accept");
                IAsyncResult ar = client.BeginAccept(AcceptedCallback, client);
                ar.AsyncWaitHandle.WaitOne();
                //F.Log("just accepted");
            }
        }
        private void AcceptedCallback(IAsyncResult ar)
        {
            if (ar.AsyncState is Socket listener)
            {
                Socket handler = listener.EndAccept(ar);
                StateObject state = new StateObject() { workSocket = handler };
                StateDelegate st = ReceiveLoop;
                st.BeginInvoke(state, DisposeReceiveLoopAsync, state);
            }
            else
            {
                F.LogError("Invalid type in AcceptedCallback(IAsyncResult ar).");
            }
            ar.AsyncWaitHandle.Close();
            ar.AsyncWaitHandle.Dispose();
        }
        private void ReceiveLoop(StateObject state)
        {
            while (true)
            {
                if (!Receive(state)) break;
            }
            try
            {
                ShutdownHandler(state.workSocket);
            }
            catch { }
        }
        private void DisposeReceiveLoopAsync(IAsyncResult ar)
        {
            ar.AsyncWaitHandle.Close();
            ar.AsyncWaitHandle.Dispose();
        }
        private delegate void StateDelegate(StateObject state);
        private bool Receive(StateObject state)
        {
            if (!state.workSocket.Connected)
            {
                ShutdownHandler(state.workSocket);
                return false;
            }
            int receivedBytes;
            SocketError errorCode;
            try
            {
                receivedBytes = state.workSocket.Receive(state.buffer, 0, StateObject.BufferSize, SocketFlags.None, out errorCode);
            }
            catch (Exception ex)
            {
                F.LogError("Error: ");
                F.LogError(ex);
                ShutdownHandler(state.workSocket);
                return false;
            }
            if (errorCode != SocketError.Success)
            {
                F.LogError("Error: " + errorCode.ToString());
                receivedBytes = 0;
            }
            //F.Log($"Received {receivedBytes} bytes.", ConsoleColor.Yellow);
            if (receivedBytes > 0)
            {
                state.AppendBuffer(receivedBytes);
                byte[] olddata = state.allData.ToArray();
                byte[] data;
                int matchcounter = 0;
                int actualdatalength = olddata.Length;
                for (int i = 0; i < olddata.Length; i++)
                {
                    if (olddata[i] == EndOfFileUTF8[matchcounter])
                    {
                        matchcounter++;
                        if (matchcounter > EndOfFileUTF8.Length)
                        {
                            actualdatalength = i - matchcounter;
                            break;
                        }
                    }
                }
                if (matchcounter >= EndOfFileUTF8.Length) // if found <EOF>
                {
                    data = new byte[actualdatalength + 1];
                    Array.Copy(olddata, 0, data, 0, actualdatalength);
                    string sentmessage;
                    try
                    {
                        sentmessage = Encoding.UTF8.GetString(olddata);
                    }
                    catch (ArgumentException)
                    {
                        sentmessage = string.Join(", ", olddata);
                    }
                    //F.Log($"Received {olddata.Length} ({data.Length}) bytes total: \"{sentmessage}\"", ConsoleColor.Green);
                    OnMessageReceived?.Invoke(this, new MessageReceivedEventArgs { message = data, utf8 = sentmessage });
                    state.allData.Clear();
                }
                else // didnt find <EOF>, continue looking.
                {
                    F.Log($"Appending {receivedBytes} bytes to final then looping back.", ConsoleColor.White);
                }
            }
            if (state.workSocket.Connected)
            {
                return true;
            }
            else
            {
                ShutdownHandler(state.workSocket);
                return false;
            }
        }
        private void ShutdownHandler(Socket handler)
        {
            try
            {
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
            finally
            {
                handler.Dispose();
            }
        }

        public void Dispose()
        {
            try
            {
                this.client.Shutdown(SocketShutdown.Both);
            }
            catch { }
            try
            {
                this.client.Close();
            } catch { }
            try
            {
                this.client.Dispose();
            } catch { }
            GC.SuppressFinalize(this);
        }
    }
}

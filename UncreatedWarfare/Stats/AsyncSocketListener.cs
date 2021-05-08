using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace UncreatedWarfare.Stats
{
    public class HeardResponseEventArgs : EventArgs
    {
        public string data;
        public Socket handler;
        public string response;

        public HeardResponseEventArgs(string data, Socket handler)
        {
            this.data = data;
            this.handler = handler;
            this.response = data;
        }
    }
    internal class StateObject
    {
        public const int BufferSize = 2048; // 0x0000-0x007F use 1 byte, 0x0080-0x07FF use 2 bytes, 0x0800-0xFFFF use 3 bytes, 0x10000-0x10FFFF use 4 bytes.
        public byte[] buffer = new byte[BufferSize];
        public StringBuilder builder = new StringBuilder();
        public Socket workSocket = null;
    }
    // https://docs.microsoft.com/en-us/dotnet/framework/network-programming/asynchronous-server-socket-example
    internal class AsyncListenServer
    {
        public event EventHandler<HeardResponseEventArgs> ListenerResultHeard;
        public ManualResetEvent Done = new ManualResetEvent(false);
        public AsyncListenServer()
        {
            ConsoleColor oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.WriteLine("Created web listen server");
            Console.ForegroundColor = oldColor;
        }
        public void StartListening()
        {
            Socket Listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                Listener.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8081));
                Listener.Listen(100);

                while (true)
                {
                    Done.Reset();
                    //Console.WriteLine("Waiting for a connection...");
                    Listener.BeginAccept(new AsyncCallback(TriggerEvent), Listener);
                    Done.WaitOne();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        private void TriggerEvent(IAsyncResult ar)
        {
            Done.Set();
            Socket listener = ar.AsyncState as Socket;
            Socket handler = listener.EndAccept(ar);

            StateObject state = new StateObject();
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, SocketFlags.None, new AsyncCallback(Read), state);
        }
        private void Read(IAsyncResult ar)
        {
            string content = string.Empty;
            StateObject state = ar.AsyncState as StateObject;
            Socket handler = state.workSocket;

            int bytesRead = handler.EndReceive(ar);
            if (bytesRead > 0)
            {
                state.builder.Append(Encoding.UTF8.GetString(state.buffer, 0, bytesRead));
                content = state.builder.ToString();
                if (content.IndexOf("<EOF>") != -1)
                {
                    //Console.WriteLine($"Read {bytesRead} bytes from socket: \n Data : {content}");
                    Send(handler, content);
                }
                else
                {
                    //Console.WriteLine("String too big, sending what i have.");
                    Send(handler, content);
                }
            }
            else
            {
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, SocketFlags.None, new AsyncCallback(Read), state);
                //ListenerResultHeard.BeginInvoke(state, new HeardResponseEventArgs(), new AsyncCallback(), state);
            }
        }
        private void Send(Socket handler, string content)
        {
            HeardResponseEventArgs args = new HeardResponseEventArgs(content, handler);
            ListenerResultHeard?.BeginInvoke(this, args, new AsyncCallback(SendAfterEvent), args);
        }

        private void SendAfterEvent(IAsyncResult ar)
        {
            HeardResponseEventArgs args = ar.AsyncState as HeardResponseEventArgs;
            byte[] byteData = Encoding.UTF8.GetBytes(args.response);
            args.handler.BeginSend(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(SendBack), args.handler);
        }
        private void SendBack(IAsyncResult ar)
        {
            try
            {
                Socket handler = ar.AsyncState as Socket;
                int bytesSent = handler.EndSend(ar);
                //Console.WriteLine($"Sentback {bytesSent} tp client.");
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}

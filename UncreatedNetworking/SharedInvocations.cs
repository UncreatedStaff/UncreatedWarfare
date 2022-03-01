using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Networking
{
    public static class SharedInvocations
    {
        public static NetCall<string> RequestVerify = new NetCall<string>(1);

        [NetCall(ENetCall.FROM_CLIENT, 1)]
        public static void ReceiveVerifyRequest(in IConnection connection, string identity)
        {
            connection.Identity = identity;
            VerifyCall.Invoke(connection, connection.NetworkID);
            Server.Instance?.InvokeClientVerified(connection);
        }



        public static NetCall<uint> VerifyCall = new NetCall<uint>(2);

        [NetCall(ENetCall.FROM_SERVER, 2)]
        public static void ReceiveVerify(in IConnection connection, uint id)
        {
            connection.SetNetID(id);
        }
        


        public static NetCall<DateTime, string, ConsoleColor> PrintText = new NetCall<DateTime, string, ConsoleColor>(3);

        [NetCall(ENetCall.FROM_EITHER, 3)]
        public static void ReceivePrint(in IConnection connection, DateTime timestamp, string text, ConsoleColor color)
        {
            Logging.Log(connection.NetworkID.ToString() + " - " + timestamp.ToString("g") + " - " + text, color);
        }

        public static NetCall<string> RequestPingConnection = new NetCall<string>(4);
        [NetCall(ENetCall.FROM_EITHER, 4)]
        public static async void ReceivePingRequest(IConnection connection, string identity)
        {
            if (Server.Instance != null)
            {
                for (int i = 0; i < Server.Instance.ConnectedClients.Count; i++)
                {
                    if (Server.Instance.ConnectedClients[i].Identity == identity)
                    {
                        NetTask.Response response = await RequestPingConnection.Request(RespondPingConnection, 
                            Server.Instance.ConnectedClients[i], identity, 1000);

                        RespondPingConnection.Invoke(connection, response.Responded);
                        return;
                    }
                }
            } 
            else
            {
                RespondPingConnection.Invoke(connection, true);
            }
        }
        public static NetCall<bool> RespondPingConnection = new NetCall<bool>(5);
        [NetCall(ENetCall.FROM_EITHER, 5)]
        public static void ReceivePingResponse(IConnection connection, bool success) { }
    }
}

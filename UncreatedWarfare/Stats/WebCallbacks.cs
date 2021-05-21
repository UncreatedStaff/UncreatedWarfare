using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace UncreatedWarfare.Stats
{
    public static class WebCallbacks
    {
        internal static void Ping(IAsyncResult ar)
        {
            (ar.AsyncState as WebInterface.Query.AsyncQueryDelegate).EndInvoke(out Response r, ar);
            SendPingData(r);
            ar.Dispose();
        }
        private static bool SendPingData(Response r)
        {
            if (r.Success)
            {
                F.Log("Connected to NodeJS server successfully. Ping: " + r.Reply + "ms.", ConsoleColor.Magenta);
                if (int.TryParse(r.Reply, out int ping) && ping > 300)
                    F.LogWarning(r.Reply + "ms seems a bit high, is the connection to the Node server stable?");
            }
            else
                F.LogError("Failed to ping NodeJS Server!");
            return r.Success;
        }
        internal static void Dispose(this IAsyncResult ar)
        {
            try
            {
                ar.AsyncWaitHandle.Close();
                ar.AsyncWaitHandle.Dispose();
            }
            catch (ObjectDisposedException) { }
        }
        private static void GetResponse(this IAsyncResult ar, out Response r)
        {
            (ar.AsyncState as WebInterface.Query.AsyncQueryDelegate).EndInvoke(out r, ar);
        }
        internal static void PingAndSend(IAsyncResult ar)
        {
            ar.GetResponse(out Response r);
            if(SendPingData(r))
            {
                IAsyncResult res = Data.WebInterface.SendPlayerListAsync();
                res.AsyncWaitHandle.WaitOne();
            }
            ar.Dispose();
        }
        internal static void SendPlayerList(IAsyncResult ar)
        {
            ar.GetResponse(out Response r);
            if (r.Success && UCWarfare.Config.Debug)
                F.Log("Sent Player List to web server.", ConsoleColor.DarkYellow);
            ar.Dispose();
        }
        internal static void SendPlayerJoin(IAsyncResult ar)
        {
            ar.GetResponse(out Response r);
            if (r.Success && UCWarfare.Config.Debug)
                F.Log("Added player to player list on web server.", ConsoleColor.DarkYellow);
            ar.Dispose();
        }
        internal static void SendPlayerLeft(IAsyncResult ar)
        {
            ar.GetResponse(out Response r);
            if (r.Success && UCWarfare.Config.Debug)
                F.Log("Removed player from player list on web server.", ConsoleColor.DarkYellow);
            ar.Dispose();
        }
        internal static void SendPlayerLocationData(IAsyncResult ar)
        {
            ar.GetResponse(out Response r);
            if (r.Success && UCWarfare.Config.Debug)
                F.Log("Removed player from player list on web server.", ConsoleColor.DarkYellow);
            ar.Dispose();
        }
        internal static void SendAssetUpdate(IAsyncResult ar)
        {
            ar.GetResponse(out Response r);
            if (r.Success)
                F.Log("Sent assets to server.", ConsoleColor.DarkYellow);
            else
                F.Log("Failed to send assets to server, could be out of date.", ConsoleColor.DarkYellow);
            ar.Dispose();
        }
        internal static void Default(IAsyncResult ar)
        {
            ar.GetResponse(out Response r);
            if (UCWarfare.Config.Debug)
                F.Log(r.Reply, ConsoleColor.DarkYellow);
            ar.Dispose();
        }
    }
}

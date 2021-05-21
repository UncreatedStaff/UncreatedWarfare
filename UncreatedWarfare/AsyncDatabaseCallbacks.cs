using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UncreatedWarfare
{
    public static class AsyncDatabaseCallbacks
    {
        private static void Dispose(this IAsyncResult ar) => Stats.WebCallbacks.Dispose(ar);
        internal static void DisposeAsyncResult(IAsyncResult ar)
        {
            if (UCWarfare.Config.Debug)
                F.Log("Disposed of an SQL request");
            ar.Dispose();
        }
        internal static void OpenedOnLoad(IAsyncResult ar)
        {
            DisposeAsyncResult(ar);
            F.Log("MySql database connection has been opened.", ConsoleColor.Magenta);
        }

        internal static void ClosedOnUnload(IAsyncResult ar)
        {
            DisposeAsyncResult(ar);
            F.LogWarning("MySql database connection has been closed.", ConsoleColor.Magenta);
        }
        internal static void PlayerReceivedZonesCallback(Player player)
        {
            player.SendChat("Picture finished generating, check your spy menu.", UCWarfare.GetColor("default"));
        }
    }
}

using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare;

namespace Uncreated.SQL
{
    public static class AsyncDatabaseCallbacks
    {
        public static void Dispose(this IAsyncResult ar)
        {
            try
            {
                ar.AsyncWaitHandle.Close();
                ar.AsyncWaitHandle.Dispose();
            }
            catch (ObjectDisposedException) { }
        }
        public static void DisposeAsyncResult(IAsyncResult ar)
        {
            if (UCWarfare.Config.Debug)
                F.Log("Disposed of an SQL request");
            ar.Dispose();
        }
        public static void OpenedOnLoad(IAsyncResult ar)
        {
            DisposeAsyncResult(ar);
            F.Log("MySql database connection has been opened.", ConsoleColor.Magenta);
        }

        public static void ClosedOnUnload(IAsyncResult ar)
        {
            DisposeAsyncResult(ar);
            F.LogWarning("MySql database connection has been closed.", ConsoleColor.Magenta);
        }
    }
}

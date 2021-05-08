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
            ar.Dispose();
        }
    }
}

using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Networking;
using Uncreated.Warfare.Kits;

namespace Uncreated.Warfare.Sync;
public static class KitSync
{
    public static void OnKitUpdated(Kit kit)
    {

    }

    public static class NetCalls
    {
        public static readonly NetCallRaw<Kit> MulticastKitUpdated = new NetCallRaw<Kit>(OnForeignKitUpdated, Kit.Read!, Kit.Write!, Kit.CAPACITY);

        [NetCall(ENetCall.FROM_SERVER, 3001)]
        public static void OnForeignKitUpdated(MessageContext ctx, Kit kit)
        {

        }
    }
}

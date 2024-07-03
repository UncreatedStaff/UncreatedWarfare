using Steamworks;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Networking;
public interface IIPAddressFilter
{
    ValueTask<bool> IsFiltered(IPAddress ip, CSteamID player, CancellationToken token = default);
    ValueTask RemoveFilteredIPs<T>(IList<T> ips, Func<T, uint> selector, CSteamID player, CancellationToken token = default);
}
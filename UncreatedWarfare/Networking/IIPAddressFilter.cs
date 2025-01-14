using System;
using System.Collections.Generic;

namespace Uncreated.Warfare.Networking;
public interface IIPAddressFilter
{
    ValueTask<bool> IsFiltered(uint packedIp, CSteamID player, CancellationToken token = default);
    ValueTask RemoveFilteredIPs<T>(IList<T> ips, Func<T, uint> selector, CSteamID player, CancellationToken token = default);
}
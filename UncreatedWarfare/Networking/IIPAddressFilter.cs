using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Networking;
public interface IIPAddressFilter
{
    ValueTask<bool> IsFiltered(IPAddress ip, ulong player, CancellationToken token = default);
    ValueTask RemoveFilteredIPs(IList<uint> ips, ulong player, CancellationToken token = default);
}
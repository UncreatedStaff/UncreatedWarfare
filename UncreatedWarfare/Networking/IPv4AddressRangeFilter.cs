using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Networking;

namespace Uncreated.Warfare.Networking;
// ReSharper disable once InconsistentNaming
public sealed class IPv4AddressRangeFilter : IIPAddressFilter
{
    public IPv4Range[] Ranges { get; }
    public IPv4AddressRangeFilter(IPv4Range[] ranges)
    {
        Ranges = ranges;
    }
    public bool IsFiltered(IPAddress ip) => IsFiltered(OffenseManager.Pack(ip));
    public bool IsFiltered(uint ip)
    {
        IPv4Range[] ranges = Ranges;
        for (int i = 0; i < ranges.Length; ++i)
        {
            if (ranges[i].InRange(ip))
                return true;
        }

        return false;
    }
    public void RemoveFilteredIPs(IList<uint> ips)
    {
        IPv4Range[] ranges = Ranges;
        for (int j = ips.Count - 1; j >= 0; --j)
        {
            for (int i = 0; i < ranges.Length; ++i)
            {
                if (ranges[i].InRange(ips[j]))
                {
                    ips.RemoveAt(j);
                    break;
                }
            }
        }
    }
    public ValueTask<bool> IsFiltered(IPAddress ip, ulong player, CancellationToken token) => new ValueTask<bool>(IsFiltered(ip));
    public ValueTask RemoveFilteredIPs(IList<uint> ips, ulong player, CancellationToken token)
    {
        RemoveFilteredIPs(ips);
        return new ValueTask();
    }

    public static IPv4AddressRangeFilter GeforceNow = new IPv4AddressRangeFilter(new IPv4Range[]
    {
        // AS11414 (USA)
        new IPv4Range(216, 228, 112, 0, 20),
        new IPv4Range(216, 228, 116, 0, 23),
        new IPv4Range(216, 228, 118, 0, 23),
        new IPv4Range(216, 228, 121, 0, 24),
        new IPv4Range(216, 228, 122, 0, 24),
        new IPv4Range(216, 228, 125, 0, 24),
        new IPv4Range(216, 228, 127, 0, 24),
        new IPv4Range(24, 51, 0, 0, 24),
        new IPv4Range(24, 51, 10, 0, 24),
        new IPv4Range(24, 51, 11, 0, 24),
        new IPv4Range(24, 51, 12, 0, 24),
        new IPv4Range(24, 51, 13, 0, 24),
        new IPv4Range(24, 51, 14, 0, 24),
        new IPv4Range(24, 51, 15, 0, 24),
        new IPv4Range(24, 51, 16, 0, 24),
        new IPv4Range(24, 51, 17, 0, 24),
        new IPv4Range(24, 51, 2, 0, 24),
        new IPv4Range(24, 51, 21, 0, 24),
        new IPv4Range(24, 51, 26, 0, 24),
        new IPv4Range(24, 51, 27, 0, 24),
        new IPv4Range(24, 51, 28, 0, 24),
        new IPv4Range(24, 51, 3, 0, 24),
        new IPv4Range(24, 51, 4, 0, 24),
        new IPv4Range(24, 51, 5, 0, 24),
        new IPv4Range(24, 51, 6, 0, 23),
        new IPv4Range(24, 51, 6, 0, 24),
        new IPv4Range(24, 51, 7, 0, 24),
        new IPv4Range(24, 51, 9, 0, 24),
        new IPv4Range(66, 22, 129, 0, 24),
        new IPv4Range(66, 22, 130, 0, 24),
        new IPv4Range(66, 22, 131, 0, 24),
        new IPv4Range(66, 22, 139, 0, 24),
        new IPv4Range(72, 25, 64, 0, 23),
        new IPv4Range(72, 25, 66, 0, 24),
        new IPv4Range(72, 25, 68, 0, 24),
        new IPv4Range(8, 26, 146, 0, 24),
        new IPv4Range(8, 28, 229, 0, 24),
        new IPv4Range(8, 36, 112, 0, 24),
        new IPv4Range(8, 36, 120, 0, 24),
        new IPv4Range(8, 44, 51, 0, 24),
        new IPv4Range(8, 44, 52, 0, 24),
        new IPv4Range(8, 44, 53, 0, 24),
        new IPv4Range(8, 44, 54, 0, 24),
        new IPv4Range(8, 44, 55, 0, 24),
        new IPv4Range(8, 47, 67, 0, 24),

        // AS20347 (USA/SK)
        new IPv4Range(112, 217, 128, 0, 24),
        new IPv4Range(112, 128, 102, 0, 24),
        new IPv4Range(24, 51, 18, 0, 24),
        new IPv4Range(24, 51, 19, 0, 24),
        new IPv4Range(24, 51, 20, 0, 24),
        new IPv4Range(24, 51, 22, 0, 24),
        new IPv4Range(24, 51, 23, 0, 24),
        new IPv4Range(24, 51, 24, 0, 24),
        new IPv4Range(24, 51, 25, 0, 24),
        new IPv4Range(24, 51, 29, 0, 24),
        new IPv4Range(24, 51, 30, 0, 24),
        new IPv4Range(24, 51, 31, 0, 24),
        new IPv4Range(24, 51, 8, 0, 24),
        new IPv4Range(66, 22, 128, 0, 24),
        new IPv4Range(66, 22, 132, 0, 24),
        new IPv4Range(66, 22, 133, 0, 24),
        new IPv4Range(66, 22, 134, 0, 24),
        new IPv4Range(66, 22, 135, 0, 24),
        new IPv4Range(66, 22, 136, 0, 24),
        new IPv4Range(66, 22, 137, 0, 24),
        new IPv4Range(66, 22, 138, 0, 24),
        new IPv4Range(66, 22, 140, 0, 24),
        new IPv4Range(66, 22, 141, 0, 24),

        // AS50889 (UK/Asia)
        new IPv4Range(103, 6, 211, 0, 24),
        new IPv4Range(121, 200, 45, 0, 24),
        new IPv4Range(185, 136, 69, 0, 24),
        new IPv4Range(185, 136, 70, 0, 24),
        new IPv4Range(185, 136, 71, 0, 24),
        new IPv4Range(193, 246, 51, 0, 24),
        new IPv4Range(217, 199, 209, 0, 24),
        new IPv4Range(217, 199, 222, 0, 24),
        new IPv4Range(37, 186, 111, 0, 24),
        new IPv4Range(77, 111, 249, 0, 24),
        new IPv4Range(77, 111, 251, 0, 24),
        new IPv4Range(80, 84, 160, 0, 24),
        new IPv4Range(80, 84, 161, 0, 24),
        new IPv4Range(80, 84, 162, 0, 24),
        new IPv4Range(80, 84, 163, 0, 24),
        new IPv4Range(80, 84, 164, 0, 24),
        new IPv4Range(80, 84, 165, 0, 24),
        new IPv4Range(80, 84, 166, 0, 24),
        new IPv4Range(80, 84, 167, 0, 24),
        new IPv4Range(80, 84, 168, 0, 24),
        new IPv4Range(80, 84, 169, 0, 24),
        new IPv4Range(80, 84, 170, 0, 24),
        new IPv4Range(80, 84, 171, 0, 24),
        new IPv4Range(80, 84, 172, 0, 24),
        new IPv4Range(80, 84, 173, 0, 24),
        new IPv4Range(80, 84, 174, 0, 24),
        new IPv4Range(80, 84, 175, 0, 24),
        new IPv4Range(85, 29, 14, 0, 24),
        new IPv4Range(85, 29, 18, 0, 24),
        new IPv4Range(89, 248, 237, 0, 24)
    });

    public static IPv4AddressRangeFilter VKPlay = new IPv4AddressRangeFilter(new IPv4Range[]
    {
        // AS21051 (RU)
        new IPv4Range(178, 22, 90, 0, 23),
        new IPv4Range(178, 22, 90, 0, 24),
        new IPv4Range(178, 22, 91, 0, 24),
        new IPv4Range(178, 22, 92, 0, 23),
        new IPv4Range(195, 211, 128, 0, 23),
        new IPv4Range(195, 211, 128, 0, 24),
        new IPv4Range(195, 211, 130, 0, 23),
        new IPv4Range(195, 211, 20, 0, 24),
        new IPv4Range(195, 211, 21, 0, 24),
        new IPv4Range(208, 87, 93, 0, 24),
        new IPv4Range(208, 87, 94, 0, 24),
        new IPv4Range(208, 87, 95, 0, 24),
        new IPv4Range(79, 137, 165, 0, 24),
        new IPv4Range(95, 163, 32, 0, 23),
        new IPv4Range(95, 163, 32, 0, 24),
        new IPv4Range(95, 163, 33, 0, 24),

        // AS57973 (RU)
        new IPv4Range(185, 16, 8, 0, 24),
        new IPv4Range(185, 16, 9, 0, 24),
        new IPv4Range(91, 237, 76, 0, 24),
    });
}

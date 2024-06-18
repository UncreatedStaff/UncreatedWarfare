using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Runtime.InteropServices;

namespace Uncreated.Warfare.Networking;

// ReSharper disable once InconsistentNaming

/// <summary>
/// IPv4 with a mask used for filtering IP owners.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public readonly struct IPv4Range
{
    [FieldOffset(0)]
    public readonly byte IP1;
    [FieldOffset(1)]
    public readonly byte IP2;
    [FieldOffset(2)]
    public readonly byte IP3;
    [FieldOffset(3)]
    public readonly byte IP4;
    [FieldOffset(4)]
    public readonly byte Mask;

    public uint IncludedIPs => (uint)Math.Pow(2, 32 - Mask);
    public uint PackedIP => ((uint)IP1 << 24) | ((uint)IP2 << 16) | ((uint)IP3 << 8) | IP4;
    public IPv4Range(IPAddress ipAddress, byte mask = 32)
    {
        if (mask > 32)
            throw new ArgumentOutOfRangeException(nameof(mask));
        byte[] bytes = ipAddress.MapToIPv4().GetAddressBytes();
        uint packedIp = ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];
        IP1 = (byte)((packedIp << 24) & byte.MaxValue);
        IP2 = (byte)((packedIp << 16) & byte.MaxValue);
        IP3 = (byte)((packedIp << 8) & byte.MaxValue);
        IP4 = (byte)(packedIp & byte.MaxValue);
        Mask = mask;
    }
    public IPv4Range(uint packedIp, byte mask)
    {
        if (mask > 32)
            throw new ArgumentOutOfRangeException(nameof(mask));
        IP1 = (byte)((packedIp << 24) & byte.MaxValue);
        IP2 = (byte)((packedIp << 16) & byte.MaxValue);
        IP3 = (byte)((packedIp << 8) & byte.MaxValue);
        IP4 = (byte)(packedIp & byte.MaxValue);
        Mask = mask;
    }
    public IPv4Range(byte ip1, byte ip2, byte ip3, byte ip4, byte mask = 32)
    {
        if (mask > 32)
            throw new ArgumentOutOfRangeException(nameof(mask));
        IP1 = ip1;
        IP2 = ip2;
        IP3 = ip3;
        IP4 = ip4;
        Mask = mask;
    }

    public override bool Equals(object obj) => obj is IPv4Range r && Equals(r);
    public bool Equals(IPv4Range other) => IP1 == other.IP1 && IP2 == other.IP2 && IP3 == other.IP3 && IP4 == other.IP4 && Mask == other.Mask;
    public bool IPEquals(IPv4Range other) => IP1 == other.IP1 && IP2 == other.IP2 && IP3 == other.IP3 && IP4 == other.IP4;
    public override int GetHashCode() => unchecked(((int)PackedIP * 397) ^ Mask.GetHashCode());
    public static bool operator ==(IPv4Range left, IPv4Range right) => left.Equals(right);
    public static bool operator !=(IPv4Range left, IPv4Range right) => !left.Equals(right);
    public static bool operator >(IPv4Range left, IPv4Range right) => left.IP1 > right.IP1 || left.IP2 > right.IP2 || left.IP3 > right.IP3 || left.IP4 > right.IP4 || left.Mask < right.Mask;
    public static bool operator <(IPv4Range left, IPv4Range right) => !(left > right);
    public static bool operator >=(IPv4Range left, IPv4Range right) => left.IP1 >= right.IP1 || left.IP2 >= right.IP2 || left.IP3 >= right.IP3 || left.IP4 >= right.IP4 || left.Mask <= right.Mask;
    public static bool operator <=(IPv4Range left, IPv4Range right) => !(left >= right);
    public bool InRange(uint packedIp)
    {
        int b1 = (int)((packedIp >> 24) & byte.MaxValue),
            b2 = (int)((packedIp >> 16) & byte.MaxValue),
            b3 = (int)((packedIp >> 8) & byte.MaxValue),
            b4 = (int)(packedIp & byte.MaxValue);
        int num;
        switch (Mask)
        {
            case 32:
                return b1 == IP1 && b2 == IP2 && b3 == IP3 && b4 == IP4;
            case >= 24 and < 32:
                if (b1 == IP1 && b2 == IP2 && b3 == IP3)
                {
                    num = ~(int)(Math.Pow(2, 32 - Mask) - 1) & byte.MaxValue;
                    return (b4 & num) == IP4;
                }

                break;
            case >= 16 and < 24:
                if (b1 == IP1 && b2 == IP2)
                {
                    num = ~(int)(Math.Pow(2, 24 - Mask) - 1) & byte.MaxValue;
                    return (b3 & num) == IP3;
                }

                break;
            case >= 8 and < 16:
                if (b1 == IP1)
                {
                    num = ~(int)(Math.Pow(2, 16 - Mask) - 1) & byte.MaxValue;
                    return (b2 & num) == IP2;
                }

                break;
            case < 8:
                num = ~(int)(Math.Pow(2, 8 - Mask) - 1) & byte.MaxValue;
                return (b1 & num) == IP1;
        }

        return false;
    }
    public bool InRange(IPAddress address)
    {
        byte[] bytes = address.MapToIPv4().GetAddressBytes();
        return InRange(((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3]);
    }
    public bool InRange(byte[] ipv4) => InRange(((uint)ipv4[0] << 24) | ((uint)ipv4[1] << 16) | ((uint)ipv4[2] << 8) | ipv4[3]);
    public bool InRange(string ipv4) => TryParseIPv4(ipv4, out IPv4Range range) && InRange(range.PackedIP);

    public string IPToString() => IP1.ToString(CultureInfo.InvariantCulture) + "." +
                                  IP2.ToString(CultureInfo.InvariantCulture) + "." +
                                  IP3.ToString(CultureInfo.InvariantCulture) + "." +
                                  IP4.ToString(CultureInfo.InvariantCulture);
    public override string ToString() => IPToString() + "/" + Mask.ToString(CultureInfo.InvariantCulture);
    public static IPv4Range Parse(string str)
    {
        if (!TryParse(str, out IPv4Range range))
            throw new FormatException("Unable to parse \"" + str + "\" as a IPv4 range. (255.255.255.255/32).");
        return range;
    }
    public static IPv4Range ParseIPv4(string str)
    {
        if (!TryParseIPv4(str, out IPv4Range range))
            throw new FormatException("Unable to parse \"" + str + "\" as a IPv4. (255.255.255.255).");
        return range;
    }
    public static unsafe bool TryParse(string str, out IPv4Range range)
    {
        range = default;
        int last = -1;
        byte* vals = stackalloc byte[5];
        string part;
        for (int i = 0; i < 4; ++i)
        {
            int index = str.IndexOf(i == 3 ? '/' : '.', last + 1);
            if (index == -1)
                return false;
            part = str.Substring(last + 1, index - last - 1);
            last = index;
            if (!byte.TryParse(part, NumberStyles.Number, CultureInfo.InvariantCulture, out vals[i]))
                return false;
        }

        part = str.Substring(last + 1);
        if (!byte.TryParse(part, NumberStyles.Number, CultureInfo.InvariantCulture, out vals[4]))
            return false;
        if (vals[4] > 32)
            return false;
        range = new IPv4Range(*vals, vals[1], vals[2], vals[3], vals[4]);
        return true;
    }
    public static unsafe bool TryParseIPv4(string str, out IPv4Range range)
    {
        range = default;
        int last = -1;
        byte* vals = stackalloc byte[4];
        for (int i = 0; i < 4; ++i)
        {
            int index = i == 3 ? str.Length : str.IndexOf('.', last + 1);
            if (index == -1)
                return false;
            string part = str.Substring(last + 1, index - last - 1);
            last = index;
            if (!byte.TryParse(part, NumberStyles.Number, CultureInfo.InvariantCulture, out vals[i]))
                return false;
        }

        range = new IPv4Range(*vals, vals[1], vals[2], vals[3], 32);
        return true;
    }
    public static long CountIncludedIPs(IEnumerable<IPv4Range> ranges)
    {
        long c = 0;

        foreach (IPv4Range range in ranges)
            c += range.IncludedIPs;

        return c;
    }
    public static List<IPv4Range> GetNonOverlappingIPs(IEnumerable<IPv4Range> ranges)
    {
        List<IPv4Range> uniqueRanges = new List<IPv4Range>(32);
        foreach (IPv4Range range in ranges)
        {
            bool overlaps = false;
            for (int j = uniqueRanges.Count - 1; j >= 0; --j)
            {
                IPv4Range uniqueRange = uniqueRanges[j];
                if (!uniqueRange.IPEquals(range))
                    continue;

                if (range.Mask >= uniqueRange.Mask)
                {
                    overlaps = true;
                    break;
                }

                uniqueRanges.RemoveAt(j);
            }
            if (!overlaps)
                uniqueRanges.Add(range);
        }

        return uniqueRanges;
    }
}
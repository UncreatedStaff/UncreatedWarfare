using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Runtime.InteropServices;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Networking;

// ReSharper disable once InconsistentNaming

/// <summary>
/// IPv4 with a mask used for filtering IP owners.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public readonly struct IPv4Range : IEquatable<IPv4Range>, IComparable<IPv4Range>, IFormattable
{
    public const uint PackedLoopback = 2130706433;

    public static readonly IPv4Range Loopback = new IPv4Range(PackedLoopback);

    // http://www.rfcreader.com/#rfc1918_line141
    public static readonly IPv4Range[] LocalIpRanges =
    [
        new IPv4Range(10, 0, 0, 0, 8),
        new IPv4Range(172, 16, 0, 0, 12),
        new IPv4Range(192, 168, 0, 0, 16)
    ];

    public static bool IsLocalIP(IPv4Range ip)
    {
        return IsLocalIP(ip.PackedIP);
    }
    public static bool IsLocalIP(uint packed)
    {
        if (packed == PackedLoopback)
            return true;

        for (int i = 0; i < LocalIpRanges.Length; ++i)
        {
            if (LocalIpRanges[i].InRange(packed))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Little endian composite of an IP address.
    /// </summary>
    [FieldOffset(0)]
    public readonly uint PackedIP;

    /// <summary>
    /// 0-32 bit shift for filtering a range of IP addresses.
    /// </summary>
    [FieldOffset(4)]
    public readonly byte Mask;

    /// <summary>
    /// Number of IP addresses in this range.
    /// </summary>
    public uint IncludedIPs => (uint)Math.Pow(2, 32 - Mask);

    public IPv4Range(IPAddress ipAddress, byte mask = 32)
    {
        if (mask > 32)
            throw new ArgumentOutOfRangeException(nameof(mask));

        PackedIP = Pack(ipAddress);
        Mask = mask;
    }

    public IPv4Range(uint packedIp, byte mask = 32)
    {
        if (mask > 32)
            throw new ArgumentOutOfRangeException(nameof(mask));

        PackedIP = packedIp;
        Mask = mask;
    }

    public IPv4Range(byte ip1, byte ip2, byte ip3, byte ip4, byte mask = 32)
    {
        if (mask > 32)
            throw new ArgumentOutOfRangeException(nameof(mask));

        PackedIP = unchecked((uint)(ip1 << 24 | ip2 << 16 | ip3 << 8 | ip4));
        Mask = mask;
    }

    public static IPAddress Unpack(uint address)
    {
        uint newAddr = BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(address) : address;
        return new IPAddress(newAddr);
    }

    public static uint Pack(IPAddress address)
    {
        Span<byte> ipv4 = stackalloc byte[4];
        address.MapToIPv4().TryWriteBytes(ipv4, out _);

        uint packed = MemoryMarshal.Read<uint>(ipv4);

        return BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(packed) : packed;
    }

    /// <inheritdoc />
    public int CompareTo(IPv4Range other)
    {
        if (PackedIP == other.PackedIP && Mask == other.Mask)
            return 0;

        return other < this ? 1 : -1;
    }

    public override bool Equals(object? obj) => obj is IPv4Range r && Equals(r);
    public bool Equals(IPv4Range other) => PackedIP == other.PackedIP && Mask == other.Mask;
    public bool IPEquals(IPv4Range other) => PackedIP == other.PackedIP;
    public override int GetHashCode() => unchecked((int)(PackedIP * Mask));
    public static bool operator ==(IPv4Range left, IPv4Range right) => left.Equals(right);
    public static bool operator !=(IPv4Range left, IPv4Range right) => !left.Equals(right);
    public static bool operator >(IPv4Range left, IPv4Range right) => left.PackedIP > right.PackedIP || left.Mask < right.Mask;
    public static bool operator <(IPv4Range left, IPv4Range right) => !(left >= right);
    public static bool operator >=(IPv4Range left, IPv4Range right) => left.PackedIP >= right.PackedIP || left.Mask <= right.Mask;
    public static bool operator <=(IPv4Range left, IPv4Range right) => !(left > right);

    public bool InRange(uint packedIp)
    {
        if (packedIp == PackedIP)
            return true;

        int b1 = (int)((packedIp >> 24) & byte.MaxValue),
            b2 = (int)((packedIp >> 16) & byte.MaxValue),
            b3 = (int)((packedIp >> 8) & byte.MaxValue),
            b4 = (int)(packedIp & byte.MaxValue);

        int num;

        switch (Mask)
        {
            case 32:
                return false;

            case >= 24 and < 32:
                if (b1 == (int)((PackedIP >> 24) & byte.MaxValue) && b2 == (int)((PackedIP >> 16) & byte.MaxValue) && b3 == (int)((PackedIP >> 8) & byte.MaxValue))
                {
                    num = ~(int)(Math.Pow(2, 32 - Mask) - 1) & byte.MaxValue;
                    return (b4 & num) == (int)(PackedIP & byte.MaxValue);
                }

                break;

            case >= 16 and < 24:
                if (b1 == (int)((PackedIP >> 24) & byte.MaxValue) && b2 == (int)((PackedIP >> 16) & byte.MaxValue))
                {
                    num = ~(int)(Math.Pow(2, 24 - Mask) - 1) & byte.MaxValue;
                    return (b3 & num) == (int)((PackedIP >> 8) & byte.MaxValue);
                }

                break;

            case >= 8 and < 16:
                if (b1 == (int)((PackedIP >> 24) & byte.MaxValue))
                {
                    num = ~(int)(Math.Pow(2, 16 - Mask) - 1) & byte.MaxValue;
                    return (b2 & num) == (int)((PackedIP >> 16) & byte.MaxValue);
                }

                break;

            case < 8:
                num = ~(int)(Math.Pow(2, 8 - Mask) - 1) & byte.MaxValue;
                return (b1 & num) == (int)((PackedIP >> 24) & byte.MaxValue);
        }

        return false;
    }
    public bool InRange(IPAddress address)
    {
        return InRange(Pack(address));
    }

    public bool InRange(byte[] ipv4) => InRange(MemoryMarshal.Read<uint>(ipv4));
    public bool InRange(ReadOnlySpan<char> ipv4) => TryParseIPv4(ipv4, out IPv4Range range) && InRange(range.PackedIP);


    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        return TryFormat(destination, out charsWritten, provider);
    }

    public bool TryFormat(Span<char> destination, out int charsWritten, IFormatProvider? provider)
    {
        if (destination.Length < 9)
        {
            charsWritten = 0;
            return false;
        }

        if (!TryFormatIPv4(destination, out int index, provider) || destination.Length <= index)
        {
            charsWritten = index;
            return false;
        }

        destination[index] = '/';
        ++index;

        if (!Mask.TryFormat(destination[index..], out int cw, default, provider))
        {
            charsWritten = index + cw;
            return false;
        }

        charsWritten = index + cw;
        return true;
    }

    public bool TryFormatIPv4(Span<char> destination, out int charsWritten, IFormatProvider? provider)
    {
        unchecked
        {
            if (destination.Length < 7)
            {
                charsWritten = 0;
                return false;
            }

            if (!((byte)(PackedIP >> 24)).TryFormat(destination, out int index, default, provider) || destination.Length <= index)
            {
                charsWritten = index;
                return false;
            }

            destination[index] = '.';
            ++index;
            if (!((byte)(PackedIP >> 16)).TryFormat(destination[index..], out int cw, default, provider) || destination.Length <= index)
            {
                charsWritten = index + cw;
                return false;
            }

            index += cw;
            destination[index] = '.';
            ++index;
            if (!((byte)(PackedIP >> 8)).TryFormat(destination[index..], out cw, default, provider) || destination.Length <= index)
            {
                charsWritten = index + cw;
                return false;
            }

            index += cw;
            destination[index] = '.';
            ++index;
            if (!((byte)PackedIP).TryFormat(destination[index..], out cw, default, provider))
            {
                charsWritten = index + cw;
                return false;
            }

            charsWritten = index + cw;
            return true;
        }
    }

    public override string ToString()
    {
        return ToString(CultureInfo.InvariantCulture);
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return ToString(formatProvider);
    }

    public string ToString(IFormatProvider? formatProvider)
    {
        int length = GetLength(false);

        ToStringFmtProviderState state = default;
        state.FormatProvider = formatProvider;
        state.IP = this;

        return string.Create(length, state, Format);
    }

    public string ToIPv4String()
    {
        return ToIPv4String(CultureInfo.InvariantCulture);
    }

    public string ToIPv4String(IFormatProvider? formatProvider)
    {
        int length = GetLength(true);

        ToStringFmtProviderState state = default;
        state.FormatProvider = formatProvider;
        state.IP = this;
        state.IPOnly = true;

        return string.Create(length, state, Format);
    }

    private static void Format(Span<char> span, ToStringFmtProviderState state)
    {
        if (state.IPOnly)
            state.IP.TryFormatIPv4(span, out _, state.FormatProvider);
        else
            state.IP.TryFormat(span, out _, state.FormatProvider);
    }

    private struct ToStringFmtProviderState
    {
        public IFormatProvider? FormatProvider;
        public IPv4Range IP;
        public bool IPOnly;
    }

    private int GetLength(bool ipOnly)
    {
        unchecked
        {
            uint packedIP = PackedIP;

            int len = GetDigitCount((byte)(packedIP >> 24))
                      + GetDigitCount((byte)(packedIP >> 16))
                      + GetDigitCount((byte)(packedIP >> 8))
                      + GetDigitCount((byte)packedIP)
                      + 3;

            return !ipOnly ? len + GetDigitCount(Mask) + 1 : len;
        }
    }

    private static int GetDigitCount(byte num)
    {
        return num < 10 ? 1 : num < 100 ? 2 : 3;
    }

    public static IPv4Range Parse(ReadOnlySpan<char> str, IFormatProvider? formatProvider)
    {
        if (!TryParse(str, formatProvider, out IPv4Range range))
            throw new FormatException("Unable to parse an IPv4 range. (255.255.255.255/32).");
        return range;
    }

    public static IPv4Range ParseIPv4(ReadOnlySpan<char> str, IFormatProvider? formatProvider)
    {
        if (!TryParseIPv4(str, formatProvider, out IPv4Range range))
            throw new FormatException("Unable to parse an IPv4. (255.255.255.255).");
        return range;
    }

    public static IPv4Range Parse(ReadOnlySpan<char> str)
    {
        return Parse(str, CultureInfo.InvariantCulture);
    }

    public static IPv4Range ParseIPv4(ReadOnlySpan<char> str)
    {
        return ParseIPv4(str, CultureInfo.InvariantCulture);
    }

    public static bool TryParse(ReadOnlySpan<char> str, out IPv4Range range)
    {
        return TryParse(str, CultureInfo.InvariantCulture, out range);
    }

    public static bool TryParseIPv4(ReadOnlySpan<char> str, out IPv4Range range)
    {
        return TryParseIPv4(str, CultureInfo.InvariantCulture, out range);
    }

    public static bool TryParse(ReadOnlySpan<char> str, IFormatProvider? formatProvider, out IPv4Range range)
    {
        range = default;
        int last = -1;
        Span<byte> vals = stackalloc byte[5];
        scoped ReadOnlySpan<char> part;
        for (int i = 0; i < 4; ++i)
        {
            int index = str.IndexOf(i == 3 ? '/' : '.', last + 1);
            if (index == -1)
                return false;
            part = str.Slice(last + 1, index - last - 1);
            last = index;
            if (!byte.TryParse(part, NumberStyles.Number, formatProvider, out vals[i]))
                return false;
        }

        part = str.Slice(last + 1);
        if (!byte.TryParse(part, NumberStyles.Number, formatProvider, out vals[4]))
            return false;
        if (vals[4] > 32)
            return false;
        range = new IPv4Range(BinaryPrimitives.ReverseEndianness(MemoryMarshal.Read<uint>(vals)), vals[4]);
        return true;
    }

    public static bool TryParseIPv4(ReadOnlySpan<char> str, IFormatProvider? formatProvider, out IPv4Range range)
    {
        range = default;
        int last = -1;
        Span<byte> vals = stackalloc byte[4];
        for (int i = 0; i < 4; ++i)
        {
            int index = i == 3 ? str.Length : str.IndexOf('.', last + 1);
            if (index == -1)
                return false;
            ReadOnlySpan<char> part = str.Slice(last + 1, index - last - 1);
            last = index;
            if (!byte.TryParse(part, NumberStyles.Number, formatProvider, out vals[i]))
                return false;
        }

        range = new IPv4Range(BinaryPrimitives.ReverseEndianness(MemoryMarshal.Read<uint>(vals)), 32);
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

    public static IPv4Range Parse(string s, IFormatProvider? provider)
    {
        return Parse(s.AsSpan(), provider);
    }

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out IPv4Range result)
    {
        return TryParse(s.AsSpan(), provider, out result);
    }
}
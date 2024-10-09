using DanielWillett.SpeedBytes;
using System;
using System.Buffers.Binary;
using System.Data;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Database.Automation;
using Uncreated.Warfare.Database.ValueConverters;

namespace Uncreated.Warfare.Moderation;

// tested

/// <summary>
/// A 20-length hardware ID of an Unturned player.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 20)]
[JsonConverter(typeof(HWIDJsonConverter))]
[ValueConverter(typeof(HWIDValueConverter))]
public readonly struct HWID : IEquatable<HWID>
{
    public const int Size = 20;

    public static readonly HWID Zero = default;

    [FieldOffset(0)]
    private readonly long _b07;

    [FieldOffset(8)]
    private readonly long _b815;

    [FieldOffset(16)]
    private readonly int _b1619;

    public HWID(long i1, long i2, int i3)
    {
        _b07 = i1;
        _b815 = i2;
        _b1619 = i3;
    }
    public unsafe HWID(byte* ptr)
    {
        this = Unsafe.ReadUnaligned<HWID>(ptr);
    }
    public HWID(Span<byte> span)
    {
        if (span.Length < Size)
            throw new ArgumentException("Span must have at least " + Size + " bytes.", nameof(span));

        this = MemoryMarshal.Read<HWID>(span);
    }
    public HWID(byte[] bytes, int index = 0)
    {
        if (index < 0)
            index = 0;

        if (bytes.Length - index < Size)
            throw new ArgumentException("Array must have at least " + Size + " bytes.", nameof(bytes));

        if (BitConverter.IsLittleEndian)
        {
            this = MemoryMarshal.Read<HWID>(bytes.AsSpan(index));
        }
        else
        {
            _b07 = BinaryPrimitives.ReverseEndianness(MemoryMarshal.Read<long>(bytes.AsSpan(index)));
            _b815 = BinaryPrimitives.ReverseEndianness(MemoryMarshal.Read<long>(bytes.AsSpan(index + 8)));
            _b1619 = BinaryPrimitives.ReverseEndianness(MemoryMarshal.Read<int>(bytes.AsSpan(index + 16)));
        }
    }
    public static unsafe HWID GenerateRandomHWID()
    {
        Guid guid = Guid.NewGuid();
        Guid guid2 = Guid.NewGuid();
        Guid* addr = &guid;
        Guid* addr2 = &guid2;
        return new HWID(*(long*)addr, *(long*)addr2, *((int*)addr + 2));
    }
    public byte[] ToByteArray()
    {
        byte[] bytes = new byte[Size];

        BitConverter.TryWriteBytes(bytes, _b07);
        BitConverter.TryWriteBytes(bytes.AsSpan(8), _b815);
        BitConverter.TryWriteBytes(bytes.AsSpan(16), _b1619);

        return bytes;
    }
    public int CopyTo(Span<byte> span)
    {
        if (span.Length < Size)
            throw new ArgumentException("Span must be at least 20 bytes long.");

        if (BitConverter.IsLittleEndian)
        {
            MemoryMarshal.Write(span, ref Unsafe.AsRef(in this));
            return Size;
        }

        BitConverter.TryWriteBytes(span, _b07);
        BitConverter.TryWriteBytes(span[8..], _b815);
        BitConverter.TryWriteBytes(span[16..], _b1619);

        return Size;
    }
    public unsafe int CopyTo(byte* ptr)
    {
        Span<byte> span = new Span<byte>(ptr, 20);

        BitConverter.TryWriteBytes(span, _b07);
        BitConverter.TryWriteBytes(span[8..], _b815);
        BitConverter.TryWriteBytes(span[16..], _b1619);

        return Size;
    }
    public int CopyTo(byte[] array, int offset)
    {
        if (offset < 0 || offset > array.Length)
            throw new ArgumentOutOfRangeException(nameof(offset));
        if (array.Length - offset < Size)
            throw new ArgumentException("Array must be at least " + Size + " elements.", nameof(array));

        BitConverter.TryWriteBytes(array.AsSpan(offset), _b07);
        BitConverter.TryWriteBytes(array.AsSpan(offset + 8), _b815);
        BitConverter.TryWriteBytes(array.AsSpan(offset + 16), _b1619);

        return Size;
    }
    public string ToBase16String(bool lowerCase = false, bool includePrefix = false)
    {
        ToStringState state = default;
        state.HWID = this;
        state.IncludePrefix = includePrefix;
        state.LowerCase = lowerCase;

        return string.Create(Size * 2 + (includePrefix ? 1 : 0) * 2, state, static (span, state) =>
        {
            int offset = state.IncludePrefix ? 2 : 0;

            long l1 = state.HWID._b07;
            for (int i = 0; i < 16; i += 2)
            {
                span[i + offset] = ToBase16Nibble(l1, i + 1, state.LowerCase);
                span[i + offset + 1] = ToBase16Nibble(l1, i, state.LowerCase);
            }

            l1 = state.HWID._b815;
            for (int i = 16; i < 32; i += 2)
            {
                span[i + offset] = ToBase16Nibble(l1, i - 15, state.LowerCase);
                span[i + offset + 1] = ToBase16Nibble(l1, i - 16, state.LowerCase);
            }

            int i1 = state.HWID._b1619;
            for (int i = 32; i < 40; i += 2)
            {
                span[i + offset] = ToBase16Nibble(i1, i - 31, state.LowerCase);
                span[i + offset + 1] = ToBase16Nibble(i1, i - 32, state.LowerCase);
            }

            if (state.IncludePrefix)
            {
                span[0] = '0';
                span[1] = 'x';
            }
        });
    }

    public string ToBase10String()
    {
        Span<byte> bytes = stackalloc byte[Size + 1];

        BitConverter.TryWriteBytes(bytes, _b07);
        BitConverter.TryWriteBytes(bytes[8..], _b815);
        BitConverter.TryWriteBytes(bytes[16..], _b1619);

        return new BigInteger(bytes).ToString("D");
    }

    public string ToBase64String()
    {
        if (BitConverter.IsLittleEndian)
        {
            return Convert.ToBase64String(MemoryMarshal.Cast<HWID, byte>(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in this), 1)));
        }
        else
        {

        }

        return Convert.ToBase64String(ToByteArray());
    }

    private struct ToStringState
    {
        public HWID HWID;
        public bool IncludePrefix;
        public bool LowerCase;
    }

    private static char ToBase16Nibble(long val, int index, bool lowerCase)
    {
        index *= 4;
        int v = (int)((val & (0x0FL << index)) >> index);
        if (index == 60 && v < 0)
            v += 16;
        return v < 10 ? (char)(v + 48) : (lowerCase ? (char)(v + 87) : (char)(v + 55));
    }
    private static char ToBase16Nibble(int val, int index, bool lowerCase)
    {
        index *= 4;
        int v = (val & (0x0F << index)) >> index;
        if (index == 28 && v < 0)
            v += 16;
        return v < 10 ? (char)(v + 48) : (lowerCase ? (char)(v + 87) : (char)(v + 55));
    }
    private static bool TryParseBase16Nibble(char c, out byte b)
    {
        b = 0;
        if (c is < '0' or > 'f' or > 'F' and < 'a' or > '9' and < 'A')
            return false;
        
        if (c >= 'A')
            b = (byte)(c - 55);
        else if (c >= 'a')
            b = (byte)(c - 87);
        else
            b = (byte)(c - 48);

        return true;
    }
    public static unsafe bool TryParseBase16(string str, out HWID hwid)
    {
        hwid = default;
        if (str.Length is not 40 and not 42)
            return false;

        byte* data = stackalloc byte[Size];
        int offset = str[0] == '0' && str[1] == 'x' ? 2 : 0;
        if (offset == 2 && str.Length != 42)
            return false;
        
        for (int i = 0; i < Size; ++i)
        {
            if (!TryParseBase16Nibble(str[i * 2 + offset], out byte high) || !TryParseBase16Nibble(str[i * 2 + offset + 1], out byte low))
                return false;

            data[i] = (byte)((high << 4) | low);
        }

        hwid = new HWID(data);
        return true;
    }
    public static bool TryParseBase10(string str, out HWID hwid)
    {
        hwid = default;
        if (!BigInteger.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out BigInteger num))
            return false;

        byte[] bytes = num.ToByteArray();
        if (bytes.Length < Size)
            return false;

        hwid = new HWID(bytes);
        return true;
    }
    public static bool TryParseBase64(string str, out HWID hwid)
    {
        hwid = default;
        try
        {
            byte[] bytes = Convert.FromBase64String(str);
            if (bytes.Length != Size)
                return false;
            hwid = new HWID(bytes);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
    public override string ToString() => ToBase16String(false, true);

    public bool Equals(HWID other)
    {
        return _b07 == other._b07 && _b815 == other._b815 && _b1619 == other._b1619;
    }
    public bool Equals(byte[] hwid) => hwid.Length == Size && Equals(new HWID(hwid));

    public override bool Equals(object? obj)
    {
        return obj is HWID other && Equals(other);
    }
    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = _b07.GetHashCode();
            hashCode = (hashCode * 397) ^ _b815.GetHashCode();
            hashCode = (hashCode * 397) ^ _b1619;
            return hashCode;
        }
    }
    public static bool operator ==(HWID left, HWID right)
    {
        return left.Equals(right);
    }
    public static bool operator !=(HWID left, HWID right)
    {
        return !left.Equals(right);
    }
    public static HWID ReadFromDataReader(int ordinal, IDataReader reader)
    {
        byte[] buffer = new byte[Size];
        reader.GetBytes(ordinal, 0, buffer, 0, Size);
        return new HWID(buffer);
    }
    public static HWID ReadFromByteReader(ByteReader reader)
    {
        Span<byte> span = stackalloc byte[Size];
        reader.ReadBlockTo(span);
        return new HWID(span);
    }
    public static unsafe HWID ReadFromJsonReader(ref Utf8JsonReader reader)
    {
        HWID hwid;
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return Zero;
            case JsonTokenType.Number:
                return new HWID(reader.GetInt64(), 0L, 0);
            case JsonTokenType.String:
                string str = reader.GetString()!;
                if (str.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!TryParseBase16(str, out hwid))
                        throw new JsonException("Invalid HWID base 16 format.");

                    return hwid;
                }
                if (TryParseBase64(str, out hwid))
                    return hwid;
                if (TryParseBase16(str, out hwid))
                    return hwid;
                if (TryParseBase10(str, out hwid))
                    return hwid;
                throw new JsonException("Invalid HWID format.");
            case JsonTokenType.StartArray:
                byte* bytes = stackalloc byte[Size];
                for (int i = 0; i < Size; ++i)
                {
                    if (!reader.Read() || reader.TokenType == JsonTokenType.EndArray)
                        break;
                    if (!reader.TryGetByte(out byte b))
                        throw new JsonException("Failed to get byte in HWID array format.");
                    bytes[i] = b;
                }

                return new HWID(bytes);
            case JsonTokenType.StartObject:
                hwid = default;
                bool found = false;
                while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
                {
                    string property = reader.GetString()!;
                    if (!reader.Read())
                        break;

                    if (found)
                        throw new JsonException("Multiple properties in HWID object notation.");
                    if (property.IndexOf("64", StringComparison.Ordinal) != -1)
                    {
                        if (reader.TokenType != JsonTokenType.String)
                            throw new JsonException("Invalid HWID base 64 in object notation.");
                        str = reader.GetString()!;
                        if (!TryParseBase64(str, out hwid))
                            throw new JsonException("Invalid HWID base 64 format.");
                        found = true;
                    }
                    else if (property.IndexOf("16", StringComparison.Ordinal) != -1)
                    {
                        if (reader.TokenType != JsonTokenType.String)
                            throw new JsonException("Invalid HWID base 16 in object notation.");
                        str = reader.GetString()!;
                        if (!TryParseBase16(str, out hwid))
                            throw new JsonException("Invalid HWID base 16 format.");
                        found = true;
                    }
                    else if (property.IndexOf("10", StringComparison.Ordinal) != -1)
                    {
                        if (reader.TokenType != JsonTokenType.String)
                            throw new JsonException("Invalid HWID base 10 in object notation.");
                        str = reader.GetString()!;
                        if (!TryParseBase10(str, out hwid))
                            throw new JsonException("Invalid HWID base 10 format.");
                        found = true;
                    }
                    else throw new JsonException($"Invalid HWID object notation property: \"{property}\".");
                }

                if (found)
                    return hwid;
                throw new JsonException("Unable to parse HWID from object notation. Must contain a property containing '10', '16', or '64', defining the base.");
            default:
                throw new JsonException($"Unexpected token parsing HWID: {reader.TokenType}.");
        }
    }
    public object AsMySQLParameter() => ToByteArray();
    public void WriteToByteWriter(ByteWriter writer)
    {
        if (writer.Stream != null)
        {
            writer.WriteBlock(ToByteArray());
        }
        else
        {
            writer.ExtendBuffer(writer.Count + Size);
            CopyTo(writer.Buffer, writer.Count);
            writer.Count += Size;
        }
    }
    public void WriteToJsonWriter(Utf8JsonWriter writer) => writer.WriteStringValue(ToBase16String(true, true));
}

public class HWIDJsonConverter : JsonConverter<HWID>
{
    public override HWID Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => HWID.ReadFromJsonReader(ref reader);
    public override void Write(Utf8JsonWriter writer, HWID value, JsonSerializerOptions options)
    {
        value.WriteToJsonWriter(writer);
    }
}
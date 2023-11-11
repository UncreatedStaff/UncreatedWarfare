using System;
using System.Data;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Encoding;
using Uncreated.Warfare.Database.Automation;
using Uncreated.Warfare.Database.ValueConverters;

namespace Uncreated.Warfare.Moderation;

// tested

[StructLayout(LayoutKind.Explicit, Size = 20)]
[JsonConverter(typeof(HWIDJsonConverter))]
[ValueConverter(typeof(HWIDValueConverter))]
public readonly struct HWID : IEquatable<HWID>
{
    public const int Size = 20;

    public static readonly HWID Zero;

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
        this = *(HWID*)ptr;
    }
    public unsafe HWID(Span<byte> span, int index = 0)
    {
        if (index < 0)
            index = 0;

        if (span.Length - index < Size)
            throw new ArgumentException("Span must have at least " + Size + " bytes.", nameof(span));

        fixed (byte* ptr = &span[index])
        {
            this = *(HWID*)ptr;
        }
    }
    public unsafe HWID(byte[] bytes, int index = 0)
    {
        if (index < 0)
            index = 0;

        if (bytes.Length - index < Size)
            throw new ArgumentException("Array must have at least " + Size + " bytes.", nameof(bytes));

        if (BitConverter.IsLittleEndian)
        {
            fixed (byte* ptr = &bytes[index])
            {
                this = *(HWID*)ptr;
            }
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
    public unsafe byte[] ToByteArray()
    {
        byte[] bytes = new byte[Size];
        fixed (byte* ptr = bytes)
        {
            UnsafeBitConverter.GetBytes(ptr, _b07);
            UnsafeBitConverter.GetBytes(ptr + 8, _b815);
            UnsafeBitConverter.GetBytes(ptr + 16, _b1619);
        }

        return bytes;
    }
    public unsafe int CopyTo(byte* ptr)
    {
        UnsafeBitConverter.GetBytes(ptr, _b07);
        UnsafeBitConverter.GetBytes(ptr + 8, _b815);
        UnsafeBitConverter.GetBytes(ptr + 16, _b1619);

        return Size;
    }
    public unsafe int CopyTo(byte[] array, int offset)
    {
        if (offset < 0 || offset > array.Length)
            throw new ArgumentOutOfRangeException(nameof(offset));
        if (array.Length - offset < Size)
            throw new ArgumentException("Array must be at least " + Size + " elements.", nameof(array));

        fixed (byte* ptr = &array[offset])
        {
            UnsafeBitConverter.GetBytes(ptr, _b07);
            UnsafeBitConverter.GetBytes(ptr + 8, _b815);
            UnsafeBitConverter.GetBytes(ptr + 16, _b1619);
        }

        return Size;
    }
    public string ToBase16String(bool lowerCase = false, bool includePrefix = false)
    {
        int offset = includePrefix ? 2 : 0;
        char[] chars = new char[Size * 2 + offset];
        long l1 = _b07;
        for (int i = 0; i < 16; i += 2)
        {
            chars[i + offset] = ToBase16Nibble(l1, i + 1, lowerCase);
            chars[i + offset + 1] = ToBase16Nibble(l1, i, lowerCase);
        }

        l1 = _b815;
        for (int i = 16; i < 32; i += 2)
        {
            chars[i + offset] = ToBase16Nibble(l1, i - 15, lowerCase);
            chars[i + offset + 1] = ToBase16Nibble(l1, i - 16, lowerCase);
        }

        int i1 = _b1619;
        for (int i = 32; i < 40; i += 2)
        {
            chars[i + offset] = ToBase16Nibble(i1, i - 31, lowerCase);
            chars[i + offset + 1] = ToBase16Nibble(i1, i - 32, lowerCase);
        }

        if (includePrefix)
        {
            chars[0] = '0';
            chars[1] = 'x';
        }

        return new string(chars);
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

    public unsafe string ToBase10String()
    {
        byte[] bytes = new byte[Size + 1];
        fixed (byte* ptr = bytes)
        {
            UnsafeBitConverter.GetBytes(ptr, _b07);
            UnsafeBitConverter.GetBytes(ptr + 8, _b815);
            UnsafeBitConverter.GetBytes(ptr + 16, _b1619);
        }

        return new BigInteger(bytes).ToString("D");
    }
    public string ToBase64String() => Convert.ToBase64String(ToByteArray());
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
        reader.Skip(Size);
        return new HWID(reader.InternalBuffer!, reader.BufferIndex - Size);
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
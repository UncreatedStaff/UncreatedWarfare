using DanielWillett.ModularRpcs.Annotations;
using DanielWillett.ModularRpcs.Configuration;
using DanielWillett.ModularRpcs.Exceptions;
using DanielWillett.ModularRpcs.Serialization;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Uncreated.Warfare.Networking.Parsers;

/// <summary>
/// Binary serializer for <see cref="CSteamID"/> to use with Modular RPCs.
/// </summary>
[RpcParser(typeof(CSteamID))]
public class CSteamIDSerializer : BinaryTypeParser<CSteamID>
{
    private static readonly MethodInfo SetErrorCodeOvf = typeof(RpcOverflowException)
        .GetProperty(nameof(RpcOverflowException.ErrorCode), BindingFlags.Public | BindingFlags.Instance)!
        .GetSetMethod(true);
    private static readonly MethodInfo SetErrorCodeParse = typeof(RpcParseException)
        .GetProperty(nameof(RpcParseException.ErrorCode), BindingFlags.Public | BindingFlags.Instance)!
        .GetSetMethod(true);

    public override bool IsVariableSize => false;
    public override int MinimumSize => 8;
    public override unsafe int WriteObject(CSteamID value, byte* bytes, uint maxSize)
    {
        if (maxSize < 8)
        {
            RpcOverflowException ex = new RpcOverflowException("The buffer overflowed while writing from the binary type parser: 'CSteamIDSerializer'.");
            SetErrorCodeOvf.Invoke(ex, [ 1 ]);
            throw ex;
        }

        if (BitConverter.IsLittleEndian)
        {
            Unsafe.WriteUnaligned(bytes, value);
        }
        else
        {
            Unsafe.WriteUnaligned(bytes, BinaryPrimitives.ReverseEndianness(value.m_SteamID));
        }

        return 8;
    }

    public override int WriteObject(CSteamID value, Stream stream)
    {
        if (BitConverter.IsLittleEndian)
        {
            stream.Write(MemoryMarshal.Cast<CSteamID, byte>(MemoryMarshal.CreateReadOnlySpan(ref value, 1)));
        }
        else
        {
            Span<byte> span = stackalloc byte[8];
            ulong rev = BinaryPrimitives.ReverseEndianness(value.m_SteamID);
            MemoryMarshal.Write(span, ref rev);
        }

        return 8;
    }

    public override unsafe CSteamID ReadObject(byte* bytes, uint maxSize, out int bytesRead)
    {
        if (maxSize < 8)
        {
            RpcParseException ex = new RpcParseException("The RpcOverhead failed to parse a message because the buffer was too short while reading from the binary type parser: 'CSteamIDSerializer'.");
            SetErrorCodeParse.Invoke(ex, [ 1 ]);
            throw ex;
        }

        ulong value = Unsafe.ReadUnaligned<ulong>(bytes);

        if (!BitConverter.IsLittleEndian)
            value = BinaryPrimitives.ReverseEndianness(value);

        bytesRead = 8;
        return Unsafe.As<ulong, CSteamID>(ref value);
    }

    public override CSteamID ReadObject(Stream stream, out int bytesRead)
    {
        Span<byte> span = stackalloc byte[8];
        int ct = stream.Read(span);
        bytesRead = ct;
        if (ct != 8)
        {
            RpcParseException ex = new RpcParseException("The RpcOverhead failed to parse a message because the stream ended too early while reading from the binary type parser: 'CSteamIDSerializer'.");
            SetErrorCodeParse.Invoke(ex, [ 2 ]);
            throw ex;
        }

        ulong value = MemoryMarshal.Read<ulong>(span);

        if (!BitConverter.IsLittleEndian)
            value = BinaryPrimitives.ReverseEndianness(value);

        return Unsafe.As<ulong, CSteamID>(ref value);
    }

    public class Many(SerializationConfiguration config) : UnmanagedValueTypeBinaryArrayTypeParser<CSteamID>(config);
}
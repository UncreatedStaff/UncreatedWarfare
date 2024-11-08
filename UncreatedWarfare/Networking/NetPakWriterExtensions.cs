using SDG.NetPak;
using System;

namespace Uncreated.Warfare.Networking;
public static class NetPakWriterExtensions
{
    /// <summary>
    /// Write a span of bytes to a network buffer as a byte array.
    /// </summary
    public static bool WriteSpan(this NetPakWriter writer, ReadOnlySpan<byte> span)
    {
        if (span.Length < 1)
            return true;
        
        if (!writer.AlignToByte() || !writer.Flush())
            return false;

        if (writer.writeByteIndex + span.Length > writer.buffer.Length)
        {
            writer.errors |= NetPakWriter.EErrorFlags.BufferOverflow;
            return false;
        }

        span.CopyTo(writer.buffer.AsSpan(writer.writeByteIndex));
        writer.writeByteIndex += span.Length;
        return true;
    }
}

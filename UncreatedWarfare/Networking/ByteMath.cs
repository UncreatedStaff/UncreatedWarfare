using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Uncreated.Networking
{
    public static class ByteMath
    {
        public static byte ToByte(this bool source) => (byte)(source ? 1 : 0);
        public static byte[] Callify(this byte[] source, ECall call)
        {
            byte[] n = new byte[source.Length + sizeof(ushort)];
            Array.Copy(source, 0, n, sizeof(ushort), source.Length);
            CopyUInt16((ushort)call, 0, ref n);
            return n;
        }
        public static void CopyUInt16(ushort input, int index, ref byte[] source)
        {
            byte[] b = BitConverter.GetBytes(input);
            Array.Copy(b, 0, source, index, b.Length);
        }
        public static void CopyUInt32(uint input, int index, ref byte[] source)
        {
            byte[] b = BitConverter.GetBytes(input);
            Array.Copy(b, 0, source, index, b.Length);
        }
        public static void CopyUInt64(ulong input, int index, ref byte[] source)
        {
            byte[] b = BitConverter.GetBytes(input);
            Array.Copy(b, 0, source, index, b.Length);
        }
        public static void CopyInt16(short input, int index, ref byte[] source)
        {
            byte[] b = BitConverter.GetBytes(input);
            Array.Copy(b, 0, source, index, b.Length);
        }
        public static void CopyInt32(int input, int index, ref byte[] source)
        {
            byte[] b = BitConverter.GetBytes(input);
            Array.Copy(b, 0, source, index, b.Length);
        }
        public static void CopyInt64(long input, int index, ref byte[] source)
        {
            byte[] b = BitConverter.GetBytes(input);
            Array.Copy(b, 0, source, index, b.Length);
        }
        public static void CopyUInt8(byte input, int index, ref byte[] source)
        {
            if (source.Length > index) source[index] = input;
        }
        public static void CopyInt8(sbyte input, int index, ref byte[] source)
        {
            byte[] b = BitConverter.GetBytes(input);
            Array.Copy(b, 0, source, index, b.Length);
        }
        public static void CopyString(string input, int index, ref byte[] source)
        {
            byte[] b = Encoding.UTF8.GetBytes(input);
            Array.Copy(b, 0, source, index, b.Length);
        }
        public static bool ReadUInt16(out ushort output, int index, byte[] source)
        {
            if (source.Length > index + sizeof(ushort))
            {
                try
                {
                    output = BitConverter.ToUInt16(source, index);
                    return true;
                }
                catch
                {
                    output = 0;
                    return false;
                }
            }
            output = 0;
            return false;
        }
        public static bool ReadUInt32(out uint output, int index, byte[] source)
        {
            if (source.Length > index + sizeof(uint))
            {
                try
                {
                    output = BitConverter.ToUInt32(source, index);
                    return true;
                }
                catch
                {
                    output = 0;
                    return false;
                }
            }
            output = 0;
            return false;
        }
        public static bool ReadUInt64(out ulong output, int index, byte[] source)
        {
            if (source.Length > index + sizeof(ulong))
            {
                try
                {
                    output = BitConverter.ToUInt64(source, index);
                    return true;
                }
                catch
                {
                    output = 0;
                    return false;
                }
            }
            output = 0;
            return false;
        }
        public static bool ReadInt16(out short output, int index, byte[] source)
        {
            if (source.Length > index + sizeof(short))
            {
                try
                {
                    output = BitConverter.ToInt16(source, index);
                    return true;
                }
                catch
                {
                    output = -1;
                    return false;
                }
            }
            output = -1;
            return false;
        }
        public static bool ReadInt32(out int output, int index, byte[] source)
        {
            if (source.Length > index + sizeof(int))
            {
                try
                {
                    output = BitConverter.ToInt32(source, index);
                    return true;
                }
                catch
                {
                    output = -1;
                    return false;
                }
            }
            output = -1;
            return false;
        }
        public static bool ReadInt64(out long output, int index, byte[] source)
        {
            if (source.Length > index + sizeof(long))
            {
                try
                {
                    output = BitConverter.ToInt64(source, index);
                    return true;
                }
                catch
                {
                    output = -1;
                    return false;
                }
            }
            output = -1;
            return false;
        }
        public static bool ReadUInt8(out byte output, int index, byte[] source)
        {
            if (source.Length > index)
            {
                output = source[index];
                return true;
            }
            else
            {
                output = 0;
                return false;
            }
        }
        public static bool ReadInt8(out sbyte output, int index, byte[] source)
        {
            if (ReadUInt8(out byte b, index, source))
            {
                output = unchecked((sbyte)b);
                return true;
            }
            output = -1;
            return false;
        }
        /// <summary>Reads a ushort representing length first.</summary>
        public static bool ReadString(out string output, int index, byte[] source)
        {
            if (ReadUInt16(out ushort length, index, source))
                if (ReadString(out output, index + sizeof(ushort), source, length))
                    return true;
            output = string.Empty;
            return false;
        }
        public static bool ReadString(out string output, int index, byte[] source, ushort length)
        {
            if (source.Length < index + length)
            {
                output = string.Empty;
                return false;
            }
            byte[] text = new byte[length];
            Array.Copy(source, index, text, 0, length);
            try
            {
                output = Encoding.UTF8.GetString(text);
                return true;
            }
            catch
            {
                output = string.Empty;
                return false;
            }
        }
    }
}

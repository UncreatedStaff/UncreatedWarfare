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
    }
}

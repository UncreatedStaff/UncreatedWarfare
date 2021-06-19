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
            byte[] b = BitConverter.GetBytes((ushort)call);
            Array.Copy(b, 0, n, 0, b.Length);
            return n;
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
        public static bool ReadFloat(out float output, int index, byte[] source)
        {
            if (source.Length > index + sizeof(float))
            {
                try
                {
                    output = BitConverter.ToSingle(source, index);
                    return true;
                }
                catch
                {
                    output = -1f;
                    return false;
                }
            }
            output = -1f;
            return false;
        }
        public static bool ReadDouble(out double output, int index, byte[] source)
        {
            if (source.Length > index + sizeof(double))
            {
                try
                {
                    output = BitConverter.ToDouble(source, index);
                    return true;
                }
                catch
                {
                    output = -1d;
                    return false;
                }
            }
            output = -1d;
            return false;
        }
        public static bool ReadBoolean(out bool output, int index, byte[] source)
        {
            if (source.Length > index)
            {
                try
                {
                    output = source[index] == 0 ? false : true;
                    return true;
                }
                catch
                {
                    output = false;
                    return false;
                }
            }
            output = false;
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
        /// <summary>Reads a ushort representing length first.</summary>
        public static bool ReadString(out string output, int index, byte[] source, out int size)
        {
            if (ReadUInt16(out ushort length, index, source))
                if (ReadString(out output, index + sizeof(ushort), source, length))
                {
                    size = sizeof(ushort) + length; 
                    return true;
                }
            output = string.Empty;
            size = 0;
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
        public static byte[] GetBytes(object t)
        {
            Type type = t.GetType();
            if (type.IsPrimitive)
            {
                if (t is ulong ul)
                    return BitConverter.GetBytes(ul);
                else if (t is float fl)
                    return BitConverter.GetBytes(fl);
                else if (t is long l)
                    return BitConverter.GetBytes(l);
                else if (t is ushort ush)
                    return BitConverter.GetBytes(ush);
                else if (t is short sh)
                    return BitConverter.GetBytes(sh);
                else if (t is byte by)
                    return new byte[1] { by };
                else if (t is int i32)
                    return BitConverter.GetBytes(i32);
                else if (t is uint ui32)
                    return BitConverter.GetBytes(ui32);
                else if (t is bool bo)
                    return new byte[1] { bo ? (byte)1 : (byte)0 };
                else if (t is sbyte sb)
                    return new byte[1] { unchecked((byte)sb) };
                else if (t is decimal de)
                    return BitConverter.GetBytes(Convert.ToDouble(de));
                else if (t is double du)
                    return BitConverter.GetBytes(du);
                else throw new ArgumentException("Can not convert that type!", "t");
            }
            else if (t is string str)
            {
                byte[] strbytes = Encoding.UTF8.GetBytes(str);
                byte[] length = BitConverter.GetBytes(unchecked((ushort)strbytes.Length));
                byte[] rtn = new byte[length.Length + strbytes.Length];
                Array.Copy(length, 0, rtn, 0, length.Length);
                Array.Copy(strbytes, 0, rtn, length.Length, strbytes.Length);
                return rtn;
            }
            else if (type.IsEnum)
            {
                Type underlying = Enum.GetUnderlyingType(type);
                try
                {
                    if (!underlying.IsEnum) return GetBytes(Convert.ChangeType(t, underlying));
                    else throw new ArgumentException("Can not convert that enum type!", "t");
                }
                catch (Exception)
                {
                    throw;
                }
            }
            else throw new ArgumentException("Can not convert that type!", "t");
        }
        public static object ReadBytes(byte[] data, int index, Type type, out int size)
        {
            if (type.IsPrimitive)
            {
                if (type == typeof(ulong) && ReadUInt64(out ulong ul, index, data))
                {
                    size = sizeof(ulong);
                    return Convert.ChangeType(ul, type);
                }
                else if (type == typeof(float) && ReadFloat(out float fl, index, data))
                {
                    size = sizeof(float);
                    return Convert.ChangeType(fl, type);
                }
                else if (type == typeof(long) && ReadInt64(out long l, index, data))
                {
                    size = sizeof(long);
                    return Convert.ChangeType(l, type);
                }
                else if (type == typeof(ushort) && ReadUInt16(out ushort ush, index, data))
                {
                    size = sizeof(ushort);
                    return Convert.ChangeType(ush, type);
                }
                else if (type == typeof(short) && ReadInt16(out short sh, index, data))
                {
                    size = sizeof(short);
                    return Convert.ChangeType(sh, type);
                }
                else if (type == typeof(byte) && ReadUInt8(out byte b, index, data))
                {
                    size = 1;
                    return Convert.ChangeType(b, type);
                }
                else if (type == typeof(int) && ReadInt32(out int i32, index, data))
                {
                    size = sizeof(int);
                    return Convert.ChangeType(i32, type);
                }
                else if (type == typeof(uint) && ReadUInt32(out uint ui32, index, data))
                {
                    size = sizeof(uint);
                    return Convert.ChangeType(ui32, type);
                }
                else if (type == typeof(bool) && ReadBoolean(out bool bo, index, data))
                {
                    size = 1;
                    return Convert.ChangeType(bo, type);
                }
                else if (type == typeof(sbyte) && ReadInt8(out sbyte sb, index, data))
                {
                    size = 1;
                    return Convert.ChangeType(sb, type);
                }
                else if (type == typeof(decimal) && ReadDouble(out double decdb, index, data))
                {
                    size = sizeof(double);
                    return Convert.ChangeType((decimal)decdb, type);
                }
                else if (type == typeof(double) && ReadDouble(out double db, index, data))
                {
                    size = sizeof(double);
                    return Convert.ChangeType(db, type);
                }
                else throw new ArgumentException("Can not convert that type!", "t");
            }
            else if (type == typeof(string))
            {
                if (ReadString(out string output, index, data, out size))
                {
                    return Convert.ChangeType(output, type);
                }
                else throw new Exception("Failed to convert string");
            }
            else if (type.IsEnum)
            {
                Type underlying = Enum.GetUnderlyingType(type);
                try
                {
                    if (!underlying.IsEnum)
                    {
                        return ReadBytes(data, index, underlying, out size);
                    }
                    else throw new ArgumentException("Can not convert that enum type!", "t");
                }
                catch (Exception)
                {
                    throw;
                }
            }
            else throw new ArgumentException("Can not convert that type!", "t");
        }
        public static T ReadBytes<T>(byte[] data, int index, out int size) => (T)ReadBytes(data, index, typeof(T), out size);
    }
}

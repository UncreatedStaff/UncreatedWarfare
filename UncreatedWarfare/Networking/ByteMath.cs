using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Networking.Invocations;
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
        public static byte[] Callify(ECall call) => BitConverter.GetBytes((ushort)call);
        public static bool ReadUInt16(out ushort output, int index, byte[] source)
        {
            if (source.Length > index + sizeof(ushort) - 1)
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
            if (source.Length > index + sizeof(uint) - 1)
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
            if (source.Length > index + sizeof(ulong) - 1)
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
            if (source.Length > index + sizeof(short) - 1)
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
            if (source.Length > index + sizeof(int) - 1)
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
            if (source.Length > index + sizeof(long) - 1)
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
            if (source.Length > index)
            {
                output = unchecked((sbyte)source[index]);
                return true;
            }
            output = -1;
            return false;
        }
        public static bool ReadFloat(out float output, int index, byte[] source)
        {
            if (source.Length > index + sizeof(float) - 1)
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
            if (source.Length > index + sizeof(double) - 1)
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
        /// <summary>Does not read the length, must be supplied.</summary>
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
        /// <summary><para>
        /// Works with all primitives except for <see cref="char"/>. Also works for <see cref="Enum"/>, <see cref="string"/>, and <see cref="DateTime"/></para>
        /// <para>Note: <see cref="decimal"/> objects are written and read as a <see cref="double"/>.</para>
        /// </summary>
        public static Reader<T> GetReadFunction<T>()
        {
            Type type = typeof(T);
            if (type.IsPrimitive)
            {
                if (type == typeof(ulong)) 
                {
                    return new Reader<T>((byte[] data, int index, out int size) =>
                    {
                        size = sizeof(ulong);
                        if (ReadUInt64(out ulong ul, index, data))
                        {
                            return (T)Convert.ChangeType(ul, type);
                        }
                        else
                        throw new ArgumentException("Failed to convert unsigned long.", nameof(data));
                    });
                }
                else if (type == typeof(float))
                {
                    return new Reader<T>((byte[] data, int index, out int size) =>
                    {
                        size = sizeof(float);
                        if (ReadFloat(out float fl, index, data))
                        {
                            return (T)Convert.ChangeType(fl, type);
                        }
                        else
                        throw new ArgumentException("Failed to convert float.", nameof(data));
                    });
                }
                else if (type == typeof(long))
                {
                    return new Reader<T>((byte[] data, int index, out int size) =>
                    {
                        size = sizeof(long);
                        if (ReadInt64(out long l, index, data))
                        {
                            return (T)Convert.ChangeType(l, type);
                        }
                        else
                        throw new ArgumentException("Failed to convert signed long.", nameof(data));
                    });
                }
                else if (type == typeof(ushort))
                {
                    return new Reader<T>((byte[] data, int index, out int size) =>
                    {
                        size = sizeof(ushort);
                        if (ReadUInt16(out ushort ush, index, data))
                        {
                            return (T)Convert.ChangeType(ush, type);
                        }
                        else
                        throw new ArgumentException("Failed to convert unsigned short.", nameof(data));
                    });
                }
                else if (type == typeof(short))
                {
                    return new Reader<T>((byte[] data, int index, out int size) =>
                    {
                        size = sizeof(short);
                        if (ReadInt16(out short sh, index, data))
                        {
                            return (T)Convert.ChangeType(sh, type);
                        }
                        else
                        throw new ArgumentException("Failed to convert signed short.", nameof(data));
                    });
                }
                else if (type == typeof(byte))
                {
                    return new Reader<T>((byte[] data, int index, out int size) =>
                    {
                        size = 1;
                        if (ReadUInt8(out byte b, index, data))
                        {
                            return (T)Convert.ChangeType(b, type);
                        }
                        else
                        throw new ArgumentException("Failed to convert unsigned byte.", nameof(data));
                    });
                }
                else if (type == typeof(int))
                {
                    return new Reader<T>((byte[] data, int index, out int size) =>
                    {
                        size = sizeof(int);
                        if (ReadInt32(out int i32, index, data))
                        {
                            return (T)Convert.ChangeType(i32, type);
                        }
                        else
                        throw new ArgumentException("Failed to convert signed int.", nameof(data));
                    });
                }
                else if (type == typeof(uint))
                {
                    return new Reader<T>((byte[] data, int index, out int size) =>
                    {
                        size = sizeof(uint);
                        if (ReadUInt32(out uint ui32, index, data))
                        {
                            return (T)Convert.ChangeType(ui32, type);
                        }
                        else
                        throw new ArgumentException("Failed to convert unsigned int.", nameof(data));
                    });
                }
                else if (type == typeof(bool))
                {
                    return new Reader<T>((byte[] data, int index, out int size) =>
                    {
                        size = 1;
                        if (ReadBoolean(out bool bo, index, data))
                        {
                            return (T)Convert.ChangeType(bo, type);
                        }
                        else throw new ArgumentException("Failed to convert boolean.", nameof(data));
                    });
                }
                else if (type == typeof(sbyte))
                {
                    return new Reader<T>((byte[] data, int index, out int size) =>
                    {
                        size = 1;
                        if (ReadInt8(out sbyte sb, index, data))
                        {
                            return (T)Convert.ChangeType(sb, type);
                        }
                        else
                        throw new ArgumentException("Failed to convert signed byte.", nameof(data));
                    });
                }
                else if (type == typeof(decimal))
                {
                    return new Reader<T>((byte[] data, int index, out int size) =>
                    {
                        size = 1;
                        if (ReadDouble(out double du, index, data))
                        {
                            return (T)Convert.ChangeType(Convert.ToDecimal(du), type);
                        }
                        else throw new ArgumentException("Failed to convert decimal.", nameof(data));
                    });
                }
                else if (type == typeof(double))
                {
                    return new Reader<T>((byte[] data, int index, out int size) =>
                    {
                        size = 1;
                        if (ReadDouble(out double du, index, data))
                        {
                            return (T)Convert.ChangeType(du, type);
                        }
                        else throw new ArgumentException("Failed to convert double.", nameof(data));
                    });
                }
                else throw new ArgumentException("Can not convert " + type.Name + "!", nameof(T));
            }
            else if (type == typeof(string))
            {
                return new Reader<T>((byte[] data, int index, out int size) =>
                {
                    if (ReadString(out string output, index, data, out size))
                    {
                        return (T)Convert.ChangeType(output, type);
                    }
                    else throw new ArgumentException("Failed to convert string.", nameof(data));
                });
            }
            else if (type.IsEnum)
            {
                Type underlying = Enum.GetUnderlyingType(type);
                if (underlying == typeof(ulong))
                {
                    return new Reader<T>((byte[] data, int index, out int size) =>
                    {
                        size = sizeof(ulong);
                        if (ReadUInt64(out ulong ul, index, data))
                        {
                            return (T)Convert.ChangeType(ul, type);
                        }
                        else
                            throw new ArgumentException("Failed to convert unsigned long.", nameof(data));
                    });
                }
                else if (underlying == typeof(long))
                {
                    return new Reader<T>((byte[] data, int index, out int size) =>
                    {
                        size = sizeof(long);
                        if (ReadInt64(out long l, index, data))
                        {
                            return (T)Convert.ChangeType(l, type);
                        }
                        else
                            throw new ArgumentException("Failed to convert signed long.", nameof(data));
                    });
                }
                else if (underlying == typeof(ushort))
                {
                    return new Reader<T>((byte[] data, int index, out int size) =>
                    {
                        size = sizeof(ushort);
                        if (ReadUInt16(out ushort ush, index, data))
                        {
                            return (T)Convert.ChangeType(ush, type);
                        }
                        else
                            throw new ArgumentException("Failed to convert unsigned short.", nameof(data));
                    });
                }
                else if (underlying == typeof(short))
                {
                    return new Reader<T>((byte[] data, int index, out int size) =>
                    {
                        size = sizeof(short);
                        if (ReadInt16(out short sh, index, data))
                        {
                            return (T)Convert.ChangeType(sh, type);
                        }
                        else
                            throw new ArgumentException("Failed to convert signed short.", nameof(data));
                    });
                }
                else if (underlying == typeof(byte))
                {
                    return new Reader<T>((byte[] data, int index, out int size) =>
                    {
                        size = 1;
                        if (ReadUInt8(out byte b, index, data))
                        {
                            return (T)Convert.ChangeType(b, type);
                        }
                        else
                            throw new ArgumentException("Failed to convert unsigned byte.", nameof(data));
                    });
                }
                else if (underlying == typeof(int))
                {
                    return new Reader<T>((byte[] data, int index, out int size) =>
                    {
                        size = sizeof(int);
                        if (ReadInt32(out int i32, index, data))
                        {
                            return (T)Convert.ChangeType(i32, type);
                        }
                        else
                            throw new ArgumentException("Failed to convert signed int.", nameof(data));
                    });
                }
                else if (underlying == typeof(uint))
                {
                    return new Reader<T>((byte[] data, int index, out int size) =>
                    {
                        size = sizeof(uint);
                        if (ReadUInt32(out uint ui32, index, data))
                        {
                            return (T)Convert.ChangeType(ui32, type);
                        }
                        else
                            throw new ArgumentException("Failed to convert unsigned int.", nameof(data));
                    });
                }
                else if (type == typeof(sbyte))
                {
                    return new Reader<T>((byte[] data, int index, out int size) =>
                    {
                        size = 1;
                        if (ReadInt8(out sbyte sb, index, data))
                        {
                            return (T)Convert.ChangeType(sb, type);
                        }
                        else
                            throw new ArgumentException("Failed to convert signed byte.", nameof(data));
                    });
                }
                else throw new ArgumentException("Can not convert that enum type because its underlying type is not ulong, long, ushort, short, byte, int, uint, or sbyte.", nameof(T));
            }
            else if (type == typeof(DateTime))
            {
                return new Reader<T>((byte[] data, int index, out int size) =>
                {
                    if (ReadInt64(out long l, index, data))
                    {
                        size = sizeof(long);
                        return (T)(new DateTime(l) as object);
                    }
                    else
                    {
                        Console.WriteLine($"Couldn't read datetime from {string.Join(", ", data.Skip(index))}.");
                        throw new ArgumentException("Failed to convert DateTime!", nameof(T));
                    }
                });
            }
            else throw new ArgumentException("Can not convert " + type.Name + "!", nameof(T));
        }
        /// <summary><para>
        /// Works with all primitives except for <see cref="char"/>. Also works for <see cref="Enum"/>, <see cref="string"/>, and <see cref="DateTime"/></para>
        /// <para>Note: <see cref="decimal"/> objects are written and read as a <see cref="double"/>.</para>
        /// </summary>
        public static Func<object, byte[]> GetWriteFunction<T>()
        {
            Type type = typeof(T);
            if (type.IsPrimitive)
            {
                if (type == typeof(ulong))
                    return (o) => BitConverter.GetBytes((ulong)o);
                else if (type == typeof(float))
                    return (o) => BitConverter.GetBytes((float)o);
                else if (type == typeof(long))
                    return (o) => BitConverter.GetBytes((long)o);
                else if (type == typeof(ushort))
                    return (o) => BitConverter.GetBytes((ushort)o);
                else if (type == typeof(short))
                    return (o) => BitConverter.GetBytes((short)o);
                else if (type == typeof(byte))
                    return (o) => new byte[1] { (byte)o };
                else if (type == typeof(int))
                    return (o) => BitConverter.GetBytes((int)o);
                else if (type == typeof(uint))
                    return (o) => BitConverter.GetBytes((uint)o);
                else if (type == typeof(bool))
                    return (o) => new byte[1] { (bool)o ? (byte)1 : (byte)0 };
                else if (type == typeof(sbyte))
                    return (o) => new byte[1] { unchecked((byte)(sbyte)o) };
                else if (type == typeof(decimal))
                    return (o) => BitConverter.GetBytes(Convert.ToDouble((decimal)o));
                else if (type == typeof(double))
                    return (o) => BitConverter.GetBytes((double)o);
                else throw new ArgumentException("Can not convert that type!", nameof(type));
            }
            else if (type == typeof(string))
            {
                return (o) =>
                {
                    byte[] strbytes = Encoding.UTF8.GetBytes((string)o);
                    byte[] length = BitConverter.GetBytes(unchecked((ushort)strbytes.Length));
                    byte[] rtn = new byte[length.Length + strbytes.Length];
                    Array.Copy(length, 0, rtn, 0, length.Length);
                    Array.Copy(strbytes, 0, rtn, length.Length, strbytes.Length);
                    return rtn;
                };
            }
            else if (type.IsEnum)
            {
                Type underlying = Enum.GetUnderlyingType(type);
                if (!underlying.IsEnum)
                {
                    if (underlying.IsPrimitive)
                    {
                        if (underlying == typeof(ulong))
                            return (o) => BitConverter.GetBytes((ulong)Convert.ChangeType(o, underlying));
                        else if (underlying == typeof(float))
                            return (o) => BitConverter.GetBytes((float)Convert.ChangeType(o, underlying));
                        else if (underlying == typeof(long))
                            return (o) => BitConverter.GetBytes((long)Convert.ChangeType(o, underlying));
                        else if (underlying == typeof(ushort))
                            return (o) => BitConverter.GetBytes((ushort)Convert.ChangeType(o, underlying));
                        else if (underlying == typeof(short))
                            return (o) => BitConverter.GetBytes((short)Convert.ChangeType(o, underlying));
                        else if (underlying == typeof(byte))
                            return (o) => new byte[1] { (byte)Convert.ChangeType(o, underlying) };
                        else if (underlying == typeof(int))
                            return (o) => BitConverter.GetBytes((int)Convert.ChangeType(o, underlying));
                        else if (underlying == typeof(uint))
                            return (o) => BitConverter.GetBytes((uint)Convert.ChangeType(o, underlying));
                        else if (underlying == typeof(sbyte))
                            return (o) => new byte[1] { unchecked((byte)(sbyte)Convert.ChangeType(o, underlying)) };
                        else if (underlying == typeof(decimal))
                            return (o) => BitConverter.GetBytes(Convert.ToDouble((decimal)Convert.ChangeType(o, underlying)));
                        else if (underlying == typeof(double))
                            return (o) => BitConverter.GetBytes((double)Convert.ChangeType(o, underlying));
                        else throw new ArgumentException("Can not convert that type!", nameof(type));
                    }
                    else throw new ArgumentException("Can not convert that enum type!", nameof(type));
                }
                else throw new ArgumentException("Can not convert that enum type!", nameof(type));
            }
            else if (type == typeof(DateTime))
            {
                return (o) => BitConverter.GetBytes(((DateTime)Convert.ChangeType(o, type)).Ticks);
            }
            else throw new ArgumentException("Can not convert that type!", nameof(type));
        }
        /// <summary>Works with all primitives except for <see cref="char"/>. Also works for <see cref="Enum"/>, <see cref="string"/>, and <see cref="DateTime"/></summary>
        [Obsolete("Use GetReadFunction<T>() in constructor and run that. It's much more effecient.")]
        public static T ReadBytes<T>(byte[] bytes, int index, out int size) => GetReadFunction<T>().Invoke(bytes, index, out size);
        /// <summary>Works with all primitives except for <see cref="char"/>. Also works for <see cref="Enum"/>, <see cref="string"/>, and <see cref="DateTime"/></summary>
        [Obsolete("Use GetWriteFunction<T>() in constructor and run that. It's much more effecient.")]
        public static byte[] WriteBytes<T>(object o) => GetWriteFunction<T>().Invoke(o);

    }
}

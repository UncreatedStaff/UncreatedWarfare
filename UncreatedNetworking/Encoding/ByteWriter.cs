using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Networking.Encoding
{
    public class ByteWriter
    {
        public delegate void Writer<T>(ByteWriter writer, T arg1);
        public int size = 0;
        protected byte[] buffer;
        public byte[] ByteBuffer => buffer;
        protected bool shouldPrepend;
        public int BaseCapacity;
        public ushort message;
        public ByteWriter(ushort message, bool shouldPrepend = true, int capacity = 0)
        {
            this.message = message;
            this.BaseCapacity = capacity;
            buffer = new byte[BaseCapacity];
            this.shouldPrepend = shouldPrepend;
        }
        /// <summary>
        /// Returns the buffer and resets the internal buffer.
        /// </summary>
        public byte[] FinishWrite()
        {
            byte[] rtn = buffer;
            Flush();
            return rtn;
        }
        private void ExtendBuffer(int newsize)
        {
            if (buffer.Length < newsize)
            {
                byte[] old = buffer;
                buffer = new byte[newsize];
                Buffer.BlockCopy(old, 0, buffer, 0, old.Length);
            }
        }
        public unsafe void PrependData(ushort MessageID)
        {
            if (!shouldPrepend) return;
            byte[] old = buffer;
            buffer = new byte[size + sizeof(ushort) + sizeof(int)];
            Buffer.BlockCopy(old, 0, buffer, sizeof(ushort) + sizeof(int), size);

            fixed (byte* ptr = buffer)
            {
                *(ushort*)ptr = MessageID;
                *(int*)(ptr + sizeof(ushort)) = size;
            }
        }
        public static unsafe void CopyPrependData(byte[] bytes, int index, ushort MessageID, int length)
        {
            fixed (byte* ptr = bytes)
            {
                *(ushort*)(ptr + index) = MessageID;
                *(int*)(ptr + index + sizeof(ushort)) = length;
            }
        }
        public unsafe void Write(int n)
        {
            int newsize = size + sizeof(int);
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);
            fixed (byte* ptr = buffer)
            {
                *(int*)(ptr + size) = n;
            }
            size = newsize;
        }
        public unsafe void Write(uint n)
        {
            int newsize = size + sizeof(uint);
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);
            fixed (byte* ptr = buffer)
            {
                *(uint*)(ptr + size) = n;
            }
            size = newsize;
        }
        public void Write(byte n)
        {
            int newsize = size + 1;
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);
            buffer[size] = n;
            size = newsize;
        }
        public void Write(sbyte n)
        {
            int newsize = size + 1;
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);
            buffer[size] = unchecked((byte)n);
            size = newsize;
        }
        public void Write(bool n)
        {
            int newsize = size + 1;
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);
            buffer[size] = (byte)(n ? 1 : 0);
            size = newsize;
        }
        public unsafe void Write(long n)
        {
            int newsize = size + sizeof(long);
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);
            fixed (byte* ptr = buffer)
            {
                *(long*)(ptr + size) = n;
            }
            size = newsize;
        }
        public unsafe void Write(ulong n)
        {
            int newsize = size + sizeof(ulong);
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);
            fixed (byte* ptr = buffer)
            {
                *(ulong*)(ptr + size) = n;
            }
            size = newsize;
        }
        public unsafe void Write(short n)
        {
            int newsize = size + sizeof(short);
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);
            fixed (byte* ptr = buffer)
            {
                *(short*)(ptr + size) = n;
            }
            size = newsize;
        }
        public unsafe void Write(ushort n)
        {
            int newsize = size + sizeof(ushort);
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);
            fixed (byte* ptr = buffer)
            {
                *(ushort*)(ptr + size) = n;
            }
            size = newsize;
        }
        public unsafe void Write(float n)
        {
            int newsize = size + sizeof(float);
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);
            fixed (byte* ptr = buffer)
            {
                *(float*)(ptr + size) = n;
            }
            size = newsize;
        }
        public unsafe void Write(decimal n)
        {
            int newsize = size + sizeof(decimal);
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);
            fixed (byte* ptr = buffer)
            {
                *(decimal*)(ptr + size) = n;
            }
            size = newsize;
        }
        public unsafe void Write(double n)
        {
            int newsize = size + sizeof(double);
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);
            fixed (byte* ptr = buffer)
            {
                *(double*)(ptr + size) = n;
            }
            size = newsize;
        }
        public unsafe void Write(char n)
        {
            int newsize = size + sizeof(char);
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);
            fixed (byte* ptr = buffer)
            {
                *(char*)(ptr + size) = n;
            }
            size = newsize;
        }
        public void Write(string n)
        {
            byte[] str = System.Text.Encoding.UTF8.GetBytes(n);
            if (str.Length > ushort.MaxValue)
            {
                Logging.LogWarning($"String too long for writing, must be below {ushort.MaxValue} bytes of UTF8, it was {str.Length} bytes long.");
                Logging.LogWarning(Environment.StackTrace);
                return;
            }
            int newsize = size + sizeof(ushort) + str.Length;
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);
            Buffer.BlockCopy(BitConverter.GetBytes((ushort)str.Length), 0, buffer, size, sizeof(ushort));
            Buffer.BlockCopy(str, 0, buffer, size + sizeof(ushort), str.Length);
            size = newsize;
        }
        public void WriteShort(string n)
        {
            byte[] str = System.Text.Encoding.UTF8.GetBytes(n);
            if (str.Length > byte.MaxValue)
            {
                Logging.LogWarning($"String too long for writing, must be below {byte.MaxValue} bytes of UTF8, it was {str.Length} bytes long.");
                Logging.LogWarning(Environment.StackTrace);
                return;
            }
            int newsize = size + 1 + str.Length;
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);
            buffer[size] = (byte)str.Length;
            Buffer.BlockCopy(str, 0, buffer, size + 1, str.Length);
            size = newsize;
        }
        public void WriteAsciiSmall(string n)
        {
            byte[] str = System.Text.Encoding.ASCII.GetBytes(n);
            if (str.Length > byte.MaxValue)
            {
                Logging.LogWarning($"String too long for writing, must be below {ushort.MaxValue} bytes of UTF8, it was {str.Length} bytes long.");
                Logging.LogWarning(Environment.StackTrace);
                return;
            }
            int newsize = size + 1 + str.Length;
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);
            buffer[size] = (byte)str.Length;
            Buffer.BlockCopy(str, 0, buffer, size + 1, str.Length);
            size = newsize;
        }
        public void Write(DateTime n) => Write(n.Ticks);
        public void Write(TimeSpan n) => Write(n.Ticks);
        public void Write(Guid n)
        {
            byte[] guid = n.ToByteArray();
            int newsize = size + guid.Length;
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);
            Buffer.BlockCopy(guid, 0, buffer, size, guid.Length);
            size = newsize;
        }
        public unsafe void Write<TEnum>(TEnum o) where TEnum : struct, Enum
        {
            Type e = typeof(TEnum);
            if (!e.IsEnum)
            {
                Logging.LogWarning($"Tried to write {e.Name} as an enum.");
                return;
            }

            Type underlying = Enum.GetUnderlyingType(e);
            if (underlying.IsPrimitive)
            {
                if (underlying == typeof(byte))
                    Write((byte)Convert.ChangeType(o, underlying));
                else if (underlying == typeof(int))
                    Write((int)Convert.ChangeType(o, underlying));
                else if (underlying == typeof(ulong))
                    Write((ulong)Convert.ChangeType(o, underlying));
                else if (underlying == typeof(long))
                    Write((long)Convert.ChangeType(o, underlying));
                else if (underlying == typeof(ushort))
                    Write((ushort)Convert.ChangeType(o, underlying));
                else if (underlying == typeof(short))
                    Write((short)Convert.ChangeType(o, underlying));
                else if (underlying == typeof(uint))
                    Write((uint)Convert.ChangeType(o, underlying));
                else if (underlying == typeof(sbyte))
                    Write((sbyte)Convert.ChangeType(o, underlying));
                else Logging.LogWarning($"Tried to write {e.Name} as an enum, but didn't have a proper underlying type ({underlying.Name}).");
            }
            else Logging.LogWarning($"Tried to write {e.Name} as an enum, but didn't have a proper underlying type ({underlying.Name}).");
        }
        public unsafe void Write(byte[] n)
        {
            if (n.Length > ushort.MaxValue)
            {
                Logging.LogWarning($"UInt8 array too long for writing, must be below {ushort.MaxValue} elements, it was {n.Length} elements long.");
                Logging.LogWarning(Environment.StackTrace);
                return;
            }
            int newsize = size + sizeof(ushort) + n.Length;
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);
            fixed (byte* ptr = buffer)
            {
                *(ushort*)(ptr + size) = (ushort)n.Length;
            }
            Buffer.BlockCopy(n, 0, buffer, size + sizeof(ushort), n.Length);
            size = newsize;
        }
        public unsafe void WriteLong(byte[] n)
        {
            if (n.Length > int.MaxValue)
            {
                Logging.LogWarning($"UInt8 array too long for writing, must be below {int.MaxValue} elements, it was {n.Length} elements long.");
                Logging.LogWarning(Environment.StackTrace);
                return;
            }
            int newsize = size + sizeof(int) + n.Length;
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);
            fixed (byte* ptr = buffer)
            {
                *(int*)(ptr + size) = n.Length;
            }
            Buffer.BlockCopy(n, 0, buffer, size + sizeof(int), n.Length);
            size = newsize;
        }
        public void Flush()
        {
            buffer = new byte[BaseCapacity];
            size = 0;
        }
        public unsafe void Write(int[] n)
        {
            if (n.Length > ushort.MaxValue)
            {
                Logging.LogWarning($"Int32 array too long for writing, must be below {ushort.MaxValue} elements, it was {n.Length} elements long.");
                Logging.LogWarning(Environment.StackTrace);
                return;
            }
            int len = sizeof(int) * n.Length;
            int newsize = size + len + sizeof(ushort);
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);
            fixed (byte* ptr = buffer)
            {
                *(ushort*)(ptr + size) = (ushort)n.Length;
                for (int i = 0; i < n.Length; i++)
                    *((int*)(ptr + size + sizeof(ushort)) + i) = n[i];
            }
            size = newsize;
        }
        public unsafe void Write(uint[] n)
        {
            if (n.Length > ushort.MaxValue)
            {
                Logging.LogWarning($"UInt32 array too long for writing, must be below {ushort.MaxValue} elements, it was {n.Length} elements long.");
                Logging.LogWarning(Environment.StackTrace);
                return;
            }
            int len = sizeof(uint) * n.Length;
            int newsize = size + len + sizeof(ushort);
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);
            fixed (byte* ptr = buffer)
            {
                *(ushort*)(ptr + size) = (ushort)n.Length;
                for (int i = 0; i < n.Length; i++)
                    *((uint*)(ptr + size + sizeof(ushort)) + i) = n[i];
            }
            size = newsize;
        }
        public unsafe void Write(sbyte[] n)
        {
            if (n.Length > ushort.MaxValue)
            {
                Logging.LogWarning($"Int8 array too long for writing, must be below {ushort.MaxValue} elements, it was {n.Length} elements long.");
                Logging.LogWarning(Environment.StackTrace);
                return;
            }
            int newsize = size + n.Length + sizeof(ushort);
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);
            fixed (byte* ptr = buffer)
            {
                *(ushort*)(ptr + size) = (ushort)n.Length;
                for (int i = 0; i < n.Length; i++)
                    *(sbyte*)(ptr + size + sizeof(ushort) + i) = n[i];
            }
            size = newsize;
        }
        public unsafe void Write(bool[] n)
        {
            if (n.Length > ushort.MaxValue)
            {
                Logging.LogWarning($"Boolean array too long for writing, must be below {ushort.MaxValue} elements, it was {n.Length} elements long.");
                Logging.LogWarning(Environment.StackTrace);
                return;
            }
            int newsize = size + (int)Math.Ceiling(n.Length / 8f) + sizeof(ushort);
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);

            fixed (byte* ptr = buffer)
            {
                *(ushort*)(ptr + size) = (ushort)n.Length;
                int offset = size - 1 + sizeof(ushort);
                for (int i = 0; i < n.Length; i++)
                {
                    int pos = i % 8;
                    if (pos == 0) offset++;
                    *(ptr + offset) <<= 8 - pos;
                    *(ptr + offset) |= (byte)(n[i] ? 1 : 0);
                }
            }
            size = newsize;
        }
        public unsafe void Write(long[] n)
        {
            if (n.Length > ushort.MaxValue)
            {
                Logging.LogWarning($"Int64 array too long for writing, must be below {ushort.MaxValue} elements, it was {n.Length} elements long.");
                Logging.LogWarning(Environment.StackTrace);
                return;
            }
            int len = sizeof(long) * n.Length;
            int newsize = size + len + sizeof(ushort);
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);
            fixed (byte* ptr = buffer)
            {
                *(ushort*)(ptr + size) = (ushort)n.Length;
                for (int i = 0; i < n.Length; i++)
                    *(long*)(ptr + size + sizeof(ushort) + i) = n[i];
            }
            size = newsize;
        }
        public unsafe void Write(ulong[] n)
        {
            if (n.Length > ushort.MaxValue)
            {
                Logging.LogWarning($"UInt64 array too long for writing, must be below {ushort.MaxValue} elements, it was {n.Length} elements long.");
                Logging.LogWarning(Environment.StackTrace);
                return;
            }
            int len = sizeof(ulong) * n.Length;
            int newsize = size + len + sizeof(ushort);
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);
            fixed (byte* ptr = buffer)
            {
                *(ushort*)(ptr + size) = (ushort)n.Length;
                for (int i = 0; i < n.Length; i++)
                    *(ulong*)(ptr + size + sizeof(ushort) + i) = n[i];
            }
            size = newsize;
        }
        public unsafe void Write(short[] n)
        {
            if (n.Length > ushort.MaxValue)
            {
                Logging.LogWarning($"Int16 array too long for writing, must be below {ushort.MaxValue} elements, it was {n.Length} elements long.");
                Logging.LogWarning(Environment.StackTrace);
                return;
            }
            int len = sizeof(short) * n.Length;
            int newsize = size + len + sizeof(ushort);
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);
            fixed (byte* ptr = buffer)
            {
                *(ushort*)(ptr + size) = (ushort)n.Length;
                for (int i = 0; i < n.Length; i++)
                    *(short*)(ptr + size + sizeof(ushort) + i) = n[i];
            }
            size = newsize;
        }
        public unsafe void Write(ushort[] n)
        {
            if (n.Length > ushort.MaxValue)
            {
                Logging.LogWarning($"UInt16 array too long for writing, must be below {ushort.MaxValue} elements, it was {n.Length} elements long.");
                Logging.LogWarning(Environment.StackTrace);
                return;
            }
            int len = sizeof(ushort) * n.Length;
            int newsize = size + len + sizeof(ushort);
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);
            fixed (byte* ptr = buffer)
            {
                *(ushort*)(ptr + size) = (ushort)n.Length;
                for (int i = 0; i < n.Length; i++)
                    *(ushort*)(ptr + size + sizeof(ushort) + i) = n[i];
            }
            size = newsize;
        }
        public unsafe void Write(float[] n)
        {
            if (n.Length > ushort.MaxValue)
            {
                Logging.LogWarning($"Float array too long for writing, must be below {ushort.MaxValue} elements, it was {n.Length} elements long.");
                Logging.LogWarning(Environment.StackTrace);
                return;
            }
            int len = sizeof(float) * n.Length;
            int newsize = size + len + sizeof(ushort);
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);
            fixed (byte* ptr = buffer)
            {
                *(ushort*)(ptr + size) = (ushort)n.Length;
                for (int i = 0; i < n.Length; i++)
                    *(float*)(ptr + size + sizeof(ushort) + i) = n[i];
            }
            size = newsize;
        }
        public unsafe void Write(decimal[] n)
        {
            if (n.Length > ushort.MaxValue)
            {
                Logging.LogWarning($"Decimal array too long for writing, must be below {ushort.MaxValue} elements, it was {n.Length} elements long.");
                Logging.LogWarning(Environment.StackTrace);
                return;
            }
            int len = sizeof(decimal) * n.Length;
            int newsize = size + len + sizeof(ushort);
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);
            fixed (byte* ptr = buffer)
            {
                *(ushort*)(ptr + size) = (ushort)n.Length;
                for (int i = 0; i < n.Length; i++)
                    *(decimal*)(ptr + size + sizeof(ushort) + i) = n[i];
            }
            size = newsize;
        }
        public unsafe void Write(double[] n)
        {
            if (n.Length > ushort.MaxValue)
            {
                Logging.LogWarning($"Double array too long for writing, must be below {ushort.MaxValue} elements, it was {n.Length} elements long.");
                Logging.LogWarning(Environment.StackTrace);
                return;
            }
            int len = sizeof(double) * n.Length;
            int newsize = size + len + sizeof(ushort);
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);
            fixed (byte* ptr = buffer)
            {
                *(ushort*)(ptr + size) = (ushort)n.Length;
                for (int i = 0; i < n.Length; i++)
                    *(double*)(ptr + size + sizeof(ushort) + i) = n[i];
            }
            size = newsize;
        }
        public unsafe void Write(char[] n)
        {
            byte[] str = System.Text.Encoding.UTF8.GetBytes(n);
            if (str.Length > ushort.MaxValue)
            {
                Logging.LogWarning($"Character Array too long for writing, must be below {ushort.MaxValue} bytes of UTF8, it was {str.Length} bytes long.");
                Logging.LogWarning(Environment.StackTrace);
                return;
            }
            int newsize = size + sizeof(ushort) + str.Length;
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);
            fixed (byte* ptr = buffer)
            {
                *(ushort*)(ptr + size) = (ushort)str.Length;
            }
            Buffer.BlockCopy(str, 0, buffer, size + sizeof(ushort), str.Length);
            size = newsize;
        }
        public unsafe void Write(string[] n)
        {
            if (n.Length > ushort.MaxValue)
            {
                Logging.LogWarning($"String array too long for writing, must be below {ushort.MaxValue} elements, it was {n.Length} elements long.");
                Logging.LogWarning(Environment.StackTrace);
                return;
            }
            fixed (byte* ptr = buffer)
            {
                *(ushort*)(ptr + size) = (ushort)n.Length;
            }
            for (int i = 0; i < n.Length; i++)
            {
                byte[] str = System.Text.Encoding.UTF8.GetBytes(n[i]);
                if (str.Length > ushort.MaxValue)
                {
                    Logging.LogWarning($"String {i} too long for writing, must be below {ushort.MaxValue} bytes of UTF8, it was {str.Length} bytes long.");
                    Logging.LogWarning(Environment.StackTrace);
                    return;
                }
                int newsize = size + sizeof(ushort) + str.Length;
                if (newsize > buffer.Length)
                    ExtendBuffer(newsize);
                fixed (byte* ptr = buffer)
                {
                    *(ushort*)(ptr + size) = (ushort)str.Length;
                }
                Buffer.BlockCopy(str, 0, buffer, size + sizeof(ushort), str.Length);
                size = newsize;
            }
        }
        public void Write<T1>(Writer<T1> writer, T1 arg)
        {
            writer.Invoke(this, arg);
        }
        public static Writer<T1> GetWriter<T1>()
        {
            Type type = typeof(T1);
            if (type.IsPrimitive)
            {
                if (type == typeof(ulong))
                    return (a, o) => a.Write((ulong)(object)o);
                else if (type == typeof(float))
                    return (a, o) => a.Write((float)(object)o);
                else if (type == typeof(long))
                    return (a, o) => a.Write((long)(object)o);
                else if (type == typeof(ushort))
                    return (a, o) => a.Write((ushort)(object)o);
                else if (type == typeof(short))
                    return (a, o) => a.Write((short)(object)o);
                else if (type == typeof(byte))
                    return (a, o) => a.Write((byte)(object)o);
                else if (type == typeof(int))
                    return (a, o) => a.Write((int)(object)o);
                else if (type == typeof(uint))
                    return (a, o) => a.Write((uint)(object)o);
                else if (type == typeof(bool))
                    return (a, o) => a.Write((bool)(object)o);
                else if (type == typeof(sbyte))
                    return (a, o) => a.Write((sbyte)(object)o);
                else if (type == typeof(decimal))
                    return (a, o) => a.Write((decimal)(object)o);
                else if (type == typeof(double))
                    return (a, o) => a.Write((double)(object)o);
                else throw new ArgumentException("Can not convert that type!", nameof(type));
            }
            else if (type == typeof(string))
            {
                return (a, o) => a.Write((string)(object)o);
            }
            else if (type.IsEnum)
            {
                Type underlying = Enum.GetUnderlyingType(type);
                if (underlying.IsPrimitive)
                {
                    if (underlying == typeof(int))
                        return (a, o) => a.Write((int)Convert.ChangeType(o, underlying));
                    else if (underlying == typeof(byte))
                        return (a, o) => a.Write((byte)Convert.ChangeType(o, underlying));
                    else if (underlying == typeof(ulong))
                        return (a, o) => a.Write((ulong)Convert.ChangeType(o, underlying));
                    else if (underlying == typeof(long))
                        return (a, o) => a.Write((long)Convert.ChangeType(o, underlying));
                    else if (underlying == typeof(ushort))
                        return (a, o) => a.Write((ushort)Convert.ChangeType(o, underlying));
                    else if (underlying == typeof(short))
                        return (a, o) => a.Write((short)Convert.ChangeType(o, underlying));
                    else if (underlying == typeof(uint))
                        return (a, o) => a.Write((uint)Convert.ChangeType(o, underlying));
                    else if (underlying == typeof(sbyte))
                        return (a, o) => a.Write((sbyte)Convert.ChangeType(o, underlying));
                    else throw new ArgumentException($"Can not convert that enum type ({type.Name}.{underlying.Name}), underlying is not a primitive integral type!", nameof(type));
                }
                else throw new ArgumentException($"Can not convert that enum type ({type.Name}.{underlying.Name}), underlying is not primitive!", nameof(type));
            }
            else if (type == typeof(DateTime))
            {
                return (a, o) => a.Write((DateTime)(object)o);
            }
            else if (type == typeof(TimeSpan))
            {
                return (a, o) => a.Write((TimeSpan)(object)o);
            }
            else if (type == typeof(Guid))
            {
                return (a, o) => a.Write((Guid)(object)o);
            }
            else if (type.IsArray)
            {
                Type arrayType = type.GetElementType();
                if (arrayType == typeof(ulong))
                    return (a, o) => a.Write((ulong[])(object)o);
                else if (arrayType == typeof(float))
                    return (a, o) => a.Write((float[])(object)o);
                else if (arrayType == typeof(long))
                    return (a, o) => a.Write((long[])(object)o);
                else if (arrayType == typeof(ushort))
                    return (a, o) => a.Write((ushort[])(object)o);
                else if (arrayType == typeof(short))
                    return (a, o) => a.Write((short[])(object)o);
                else if (arrayType == typeof(byte))
                    return (a, o) => a.Write((byte[])(object)o);
                else if (arrayType == typeof(int))
                    return (a, o) => a.Write((int[])(object)o);
                else if (arrayType == typeof(uint))
                    return (a, o) => a.Write((uint[])(object)o);
                else if (arrayType == typeof(bool))
                    return (a, o) => a.Write((bool[])(object)o);
                else if (arrayType == typeof(sbyte))
                    return (a, o) => a.Write((sbyte[])(object)o);
                else if (arrayType == typeof(decimal))
                    return (a, o) => a.Write((decimal[])(object)o);
                else if (arrayType == typeof(double))
                    return (a, o) => a.Write((double[])(object)o);
                else if (arrayType == typeof(string))
                    return (a, o) => a.Write((string[])(object)o);
                else throw new ArgumentException("Can not convert that array type!", nameof(type));
            }
            else throw new ArgumentException("Can not convert that type!", nameof(type));
        }
        public static bool TryGetWriter(Type type, out Writer<object> writer)
        {
            if (type.IsPrimitive)
            {
                if (type == typeof(ulong))
                {
                    writer = (a, o) => a.Write((ulong)o);
                    return true;
                }
                else if (type == typeof(float))
                {
                    writer = (a, o) => a.Write((float)o);
                    return true;
                }
                else if (type == typeof(long))
                {
                    writer = (a, o) => a.Write((long)o);
                    return true;
                }
                else if (type == typeof(ushort))
                {
                    writer = (a, o) => a.Write((ushort)o);
                    return true;
                }
                else if (type == typeof(short))
                {
                    writer = (a, o) => a.Write((short)o);
                    return true;
                }
                else if (type == typeof(byte))
                {
                    writer = (a, o) => a.Write((byte)o);
                    return true;
                }
                else if (type == typeof(int))
                {
                    writer = (a, o) => a.Write((int)o);
                    return true;
                }
                else if (type == typeof(uint))
                {
                    writer = (a, o) => a.Write((uint)o);
                    return true;
                }
                else if (type == typeof(bool))
                {
                    writer = (a, o) => a.Write((bool)o);
                    return true;
                }
                else if (type == typeof(sbyte))
                {
                    writer = (a, o) => a.Write((sbyte)o);
                    return true;
                }
                else if (type == typeof(decimal))
                {
                    writer = (a, o) => a.Write((decimal)o);
                    return true;
                }
                else if (type == typeof(double))
                {
                    writer = (a, o) => a.Write((double)o);
                    return true;
                }
                else
                {
                    writer = null;
                    return false;
                }
            }
            else if (type == typeof(string))
            {
                writer = (a, o) => a.Write((string)o);
                return true;
            }
            else if (type.IsEnum)
            {
                Type underlying = Enum.GetUnderlyingType(type);
                if (underlying.IsPrimitive)
                {
                    if (underlying == typeof(int))
                    {
                        writer = (a, o) => a.Write((int)Convert.ChangeType(o, underlying));
                        return true;
                    }
                    else if (underlying == typeof(byte))
                    {
                        writer = (a, o) => a.Write((byte)Convert.ChangeType(o, underlying));
                        return true;
                    }
                    else if (underlying == typeof(ulong))
                    {
                        writer = (a, o) => a.Write((ulong)Convert.ChangeType(o, underlying));
                        return true;
                    }
                    else if (underlying == typeof(long))
                    {
                        writer = (a, o) => a.Write((long)Convert.ChangeType(o, underlying));
                        return true;
                    }
                    else if (underlying == typeof(ushort))
                    {
                        writer = (a, o) => a.Write((ushort)Convert.ChangeType(o, underlying));
                        return true;
                    }
                    else if (underlying == typeof(short))
                    {
                        writer = (a, o) => a.Write((short)Convert.ChangeType(o, underlying));
                        return true;
                    }
                    else if (underlying == typeof(uint))
                    {
                        writer = (a, o) => a.Write((uint)Convert.ChangeType(o, underlying));
                        return true;
                    }
                    else if (underlying == typeof(sbyte))
                    {
                        writer = (a, o) => a.Write((sbyte)Convert.ChangeType(o, underlying));
                        return true;
                    }
                    else
                    {
                        writer = null;
                        return false;
                    }
                }
                else
                {
                    writer = null;
                    return false;
                }
            }
            else if (type == typeof(DateTime))
            {
                writer = (a, o) => a.Write((DateTime)o);
                return true;
            }
            else if (type == typeof(TimeSpan))
            {
                writer = (a, o) => a.Write((TimeSpan)o);
                return true;
            }
            else if (type == typeof(Guid))
            {
                writer = (a, o) => a.Write((Guid)o);
                return true;
            }
            else if (type.IsArray)
            {
                Type arrayType = type.GetElementType();
                if (arrayType == typeof(ulong))
                {
                    writer = (a, o) => a.Write((ulong[])o);
                    return true;
                }
                else if (arrayType == typeof(float))
                {
                    writer = (a, o) => a.Write((float[])o);
                    return true;
                }
                else if (arrayType == typeof(long))
                {
                    writer = (a, o) => a.Write((long[])o);
                    return true;
                }
                else if (arrayType == typeof(ushort))
                {
                    writer = (a, o) => a.Write((ushort[])o);
                    return true;
                }
                else if (arrayType == typeof(short))
                {
                    writer = (a, o) => a.Write((short[])o);
                    return true;
                }
                else if (arrayType == typeof(byte))
                {
                    writer = (a, o) => a.Write((byte[])o);
                    return true;
                }
                else if (arrayType == typeof(int))
                {
                    writer = (a, o) => a.Write((int[])o);
                    return true;
                }
                else if (arrayType == typeof(uint))
                {
                    writer = (a, o) => a.Write((uint[])o);
                    return true;
                }
                else if (arrayType == typeof(bool))
                {
                    writer = (a, o) => a.Write((bool[])o);
                    return true;
                }
                else if (arrayType == typeof(sbyte))
                {
                    writer = (a, o) => a.Write((sbyte[])o);
                    return true;
                }
                else if (arrayType == typeof(decimal))
                {
                    writer = (a, o) => a.Write((decimal[])o);
                    return true;
                }
                else if (arrayType == typeof(double))
                {
                    writer = (a, o) => a.Write((double[])o);
                    return true;
                }
                else if (arrayType == typeof(string))
                {
                    writer = (a, o) => a.Write((string[])o);
                    return true;
                }
                else
                {
                    writer = null;
                    return false;
                }
            }
            else
            {
                writer = null;
                return false;
            }
        }
        public static bool TryGetWriter<T1>(out Writer<T1> writer)
        {
            Type type = typeof(T1);
            if (type.IsPrimitive)
            {
                if (type == typeof(ulong))
                {
                    writer = (a, o) => a.Write((ulong)(object)o);
                    return true;
                }
                else if (type == typeof(float))
                {
                    writer = (a, o) => a.Write((float)(object)o);
                    return true;
                }
                else if (type == typeof(long))
                {
                    writer = (a, o) => a.Write((long)(object)o);
                    return true;
                }
                else if (type == typeof(ushort))
                {
                    writer = (a, o) => a.Write((ushort)(object)o);
                    return true;
                }
                else if (type == typeof(short))
                {
                    writer = (a, o) => a.Write((short)(object)o);
                    return true;
                }
                else if (type == typeof(byte))
                {
                    writer = (a, o) => a.Write((byte)(object)o);
                    return true;
                }
                else if (type == typeof(int))
                {
                    writer = (a, o) => a.Write((int)(object)o);
                    return true;
                }
                else if (type == typeof(uint))
                {
                    writer = (a, o) => a.Write((uint)(object)o);
                    return true;
                }
                else if (type == typeof(bool))
                {
                    writer = (a, o) => a.Write((bool)(object)o);
                    return true;
                }
                else if (type == typeof(sbyte))
                {
                    writer = (a, o) => a.Write((sbyte)(object)o);
                    return true;
                }
                else if (type == typeof(decimal))
                {
                    writer = (a, o) => a.Write((decimal)(object)o);
                    return true;
                }
                else if (type == typeof(double))
                {
                    writer = (a, o) => a.Write((double)(object)o);
                    return true;
                }
                else
                {
                    writer = null;
                    return false;
                }
            }
            else if (type == typeof(string))
            {
                writer = (a, o) => a.Write((string)(object)o);
                return true;
            }
            else if (type.IsEnum)
            {
                Type underlying = Enum.GetUnderlyingType(type);
                if (underlying.IsPrimitive)
                {
                    if (underlying == typeof(int))
                    {
                        writer = (a, o) => a.Write((int)Convert.ChangeType(o, underlying));
                        return true;
                    }
                    else if (underlying == typeof(byte))
                    {
                        writer = (a, o) => a.Write((byte)Convert.ChangeType(o, underlying));
                        return true;
                    }
                    else if (underlying == typeof(ulong))
                    {
                        writer = (a, o) => a.Write((ulong)Convert.ChangeType(o, underlying));
                        return true;
                    }
                    else if (underlying == typeof(long))
                    {
                        writer = (a, o) => a.Write((long)Convert.ChangeType(o, underlying));
                        return true;
                    }
                    else if (underlying == typeof(ushort))
                    {
                        writer = (a, o) => a.Write((ushort)Convert.ChangeType(o, underlying));
                        return true;
                    }
                    else if (underlying == typeof(short))
                    {
                        writer = (a, o) => a.Write((short)Convert.ChangeType(o, underlying));
                        return true;
                    }
                    else if (underlying == typeof(uint))
                    {
                        writer = (a, o) => a.Write((uint)Convert.ChangeType(o, underlying));
                        return true;
                    }
                    else if (underlying == typeof(sbyte))
                    {
                        writer = (a, o) => a.Write((sbyte)Convert.ChangeType(o, underlying));
                        return true;
                    }
                    else
                    {
                        writer = null;
                        return false;
                    }
                }
                else
                {
                    writer = null;
                    return false;
                }
            }
            else if (type == typeof(DateTime))
            {
                writer = (a, o) => a.Write((DateTime)(object)o);
                return true;
            }
            else if (type == typeof(TimeSpan))
            {
                writer = (a, o) => a.Write((TimeSpan)(object)o);
                return true;
            }
            else if (type == typeof(Guid))
            {
                writer = (a, o) => a.Write((Guid)(object)o);
                return true;
            }
            else if (type.IsArray)
            {
                Type arrayType = type.GetElementType();
                if (arrayType == typeof(ulong))
                {
                    writer = (a, o) => a.Write((ulong[])(object)o);
                    return true;
                }
                else if (arrayType == typeof(float))
                {
                    writer = (a, o) => a.Write((float[])(object)o);
                    return true;
                }
                else if (arrayType == typeof(long))
                {
                    writer = (a, o) => a.Write((long[])(object)o);
                    return true;
                }
                else if (arrayType == typeof(ushort))
                {
                    writer = (a, o) => a.Write((ushort[])(object)o);
                    return true;
                }
                else if (arrayType == typeof(short))
                {
                    writer = (a, o) => a.Write((short[])(object)o);
                    return true;
                }
                else if (arrayType == typeof(byte))
                {
                    writer = (a, o) => a.Write((byte[])(object)o);
                    return true;
                }
                else if (arrayType == typeof(int))
                {
                    writer = (a, o) => a.Write((int[])(object)o);
                    return true;
                }
                else if (arrayType == typeof(uint))
                {
                    writer = (a, o) => a.Write((uint[])(object)o);
                    return true;
                }
                else if (arrayType == typeof(bool))
                {
                    writer = (a, o) => a.Write((bool[])(object)o);
                    return true;
                }
                else if (arrayType == typeof(sbyte))
                {
                    writer = (a, o) => a.Write((sbyte[])(object)o);
                    return true;
                }
                else if (arrayType == typeof(decimal))
                {
                    writer = (a, o) => a.Write((decimal[])(object)o);
                    return true;
                }
                else if (arrayType == typeof(double))
                {
                    writer = (a, o) => a.Write((double[])(object)o);
                    return true;
                }
                else if (arrayType == typeof(string))
                {
                    writer = (a, o) => a.Write((string[])(object)o);
                    return true;
                }
                else
                {
                    writer = null;
                    return false;
                }
            }
            else
            {
                writer = null;
                return false;
            }
        }
        public static int GetMinimumSize<T1>()
        {
            Type type = typeof(T1);
            if (type.IsPrimitive)
            {
                if (type == typeof(ulong))
                    return sizeof(ulong);
                else if (type == typeof(float))
                    return sizeof(float);
                else if (type == typeof(long))
                    return sizeof(long);
                else if (type == typeof(ushort))
                    return sizeof(ushort);
                else if (type == typeof(short))
                    return sizeof(short);
                else if (type == typeof(byte))
                    return 1;
                else if (type == typeof(int))
                    return sizeof(int);
                else if (type == typeof(uint))
                    return sizeof(uint);
                else if (type == typeof(bool))
                    return 1;
                else if (type == typeof(sbyte))
                    return 1;
                else if (type == typeof(decimal))
                    return sizeof(double);
                else if (type == typeof(double))
                    return sizeof(double);
                else throw new ArgumentException("Can not convert that type!", nameof(type));
            }
            else if (type == typeof(string))
            {
                return sizeof(ushort);
            }
            else if (type.IsEnum)
            {
                Type underlying = Enum.GetUnderlyingType(type);
                if (underlying.IsPrimitive)
                {
                    if (underlying == typeof(int))
                        return sizeof(int);
                    else if (underlying == typeof(byte))
                        return 1;
                    else if (underlying == typeof(ulong))
                        return sizeof(ulong);
                    else if (underlying == typeof(long))
                        return sizeof(long);
                    else if (underlying == typeof(ushort))
                        return sizeof(ushort);
                    else if (underlying == typeof(short))
                        return sizeof(short);
                    else if (underlying == typeof(uint))
                        return sizeof(uint);
                    else if (underlying == typeof(sbyte))
                        return 1;
                    else throw new ArgumentException($"Can not convert that enum type ({type.Name}.{underlying.Name}), underlying is not a primitive integral type!", nameof(type));
                }
                else throw new ArgumentException($"Can not convert that enum type ({type.Name}.{underlying.Name}), underlying is not primitive!", nameof(type));
            }
            else if (type == typeof(DateTime))
            {
                return sizeof(long);
            }
            else if (type == typeof(TimeSpan))
            {
                return sizeof(long);
            }
            else if (type == typeof(Guid))
            {
                return 16;
            }
            else if (type.IsArray)
            {
                return sizeof(ushort);
            }
            else throw new ArgumentException("Can not convert that type!", nameof(type));
        }
    }
    public sealed class ByteWriterRaw<T> : ByteWriter
    {
        private readonly Writer<T> writer;
        /// <summary>Leave <paramref name="writer"/> null to auto-fill.</summary>
        public ByteWriterRaw(ushort message, Writer<T> writer, bool shouldPrepend = true, int capacity = 0) : base(message, shouldPrepend, capacity)
        {
            this.writer = writer ?? (TryGetWriter(out Writer<T> w) ? w : null);
            this.message = message;
        }
        public byte[] Get(T obj)
        {
            Flush();
            writer.Invoke(this, obj);
            PrependData(message);
            return buffer;
        }
    }
    public sealed class ByteWriterRaw<T1, T2> : ByteWriter
    {
        private readonly Writer<T1> writer1;
        private readonly Writer<T2> writer2;
        /// <summary>Leave any writer null to auto-fill.</summary>
        public ByteWriterRaw(ushort message, Writer<T1> writer1, Writer<T2> writer2, bool shouldPrepend = true, int capacity = 0) : base(message, shouldPrepend, capacity)
        {
            this.message = message;
            this.writer1 = writer1 ?? (TryGetWriter(out Writer<T1> w1) ? w1 : null);
            this.writer2 = writer2 ?? (TryGetWriter(out Writer<T2> w2) ? w2 : null);
        }
        public byte[] Get(T1 arg1, T2 arg2)
        {
            Flush();
            writer1.Invoke(this, arg1);
            writer2.Invoke(this, arg2);
            PrependData(message);
            return buffer;
        }
    }
    public sealed class ByteWriterRaw<T1, T2, T3> : ByteWriter
    {
        private readonly Writer<T1> writer1;
        private readonly Writer<T2> writer2;
        private readonly Writer<T3> writer3;
        /// <summary>Leave any writer null to auto-fill.</summary>
        public ByteWriterRaw(ushort message, Writer<T1> writer1, Writer<T2> writer2, Writer<T3> writer3, bool shouldPrepend = true, int capacity = 0) : base(message, shouldPrepend, capacity)
        {
            this.message = message;
            this.writer1 = writer1 ?? (TryGetWriter(out Writer<T1> w1) ? w1 : null);
            this.writer2 = writer2 ?? (TryGetWriter(out Writer<T2> w2) ? w2 : null);
            this.writer3 = writer3 ?? (TryGetWriter(out Writer<T3> w3) ? w3 : null);
        }
        public byte[] Get(T1 arg1, T2 arg2, T3 arg3)
        {
            Flush();
            writer1.Invoke(this, arg1);
            writer2.Invoke(this, arg2);
            writer3.Invoke(this, arg3);
            PrependData(message);
            return buffer;
        }
    }
    public sealed class ByteWriterRaw<T1, T2, T3, T4> : ByteWriter
    {
        private readonly Writer<T1> writer1;
        private readonly Writer<T2> writer2;
        private readonly Writer<T3> writer3;
        private readonly Writer<T4> writer4;
        /// <summary>Leave any writer null to auto-fill.</summary>
        public ByteWriterRaw(ushort message, Writer<T1> writer1, Writer<T2> writer2, Writer<T3> writer3, Writer<T4> writer4, bool shouldPrepend = true, int capacity = 0) : base(message, shouldPrepend, capacity)
        {
            this.message = message;
            this.writer1 = writer1 ?? (TryGetWriter(out Writer<T1> w1) ? w1 : null);
            this.writer2 = writer2 ?? (TryGetWriter(out Writer<T2> w2) ? w2 : null);
            this.writer3 = writer3 ?? (TryGetWriter(out Writer<T3> w3) ? w3 : null);
            this.writer4 = writer4 ?? (TryGetWriter(out Writer<T4> w4) ? w4 : null);
        }
        public byte[] Get(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            Flush();
            writer1.Invoke(this, arg1);
            writer2.Invoke(this, arg2);
            writer3.Invoke(this, arg3);
            writer4.Invoke(this, arg4);
            PrependData(message);
            return buffer;
        }
    }
    public sealed class DynamicByteWriter<T1> : ByteWriter
    {
        public Writer<T1> writer;
        public DynamicByteWriter(ushort message, bool shouldPrepend = true, int capacity = 0) : base(message, shouldPrepend, capacity < 1 ? GetMinimumSize<T1>() : capacity)
        {
            this.message = message;
            writer = GetWriter<T1>();
        }
        public byte[] Get(T1 obj)
        {
            Flush();
            writer.Invoke(this, obj);
            PrependData(message);
            return buffer;
        }
    }
    public sealed class DynamicByteWriter<T1, T2> : ByteWriter
    {
        private readonly Writer<T1> writer1;
        private readonly Writer<T2> writer2;
        public DynamicByteWriter(ushort message, bool shouldPrepend = true, int capacity = 0) : base(message, shouldPrepend, capacity < 1 ? GetMinimumSize<T1>() + GetMinimumSize<T2>() : capacity)
        {
            this.message = message;
            writer1 = GetWriter<T1>();
            writer2 = GetWriter<T2>();
        }
        public byte[] Get(T1 arg1, T2 arg2)
        {
            Flush();
            Write(writer1, arg1);
            Write(writer2, arg2);
            PrependData(message);
            return buffer;
        }
    }
    public sealed class DynamicByteWriter<T1, T2, T3> : ByteWriter
    {
        private readonly Writer<T1> writer1;
        private readonly Writer<T2> writer2;
        private readonly Writer<T3> writer3;
        public DynamicByteWriter(ushort message, bool shouldPrepend = true, int capacity = 0) : base(message, shouldPrepend, capacity < 1 ? GetMinimumSize<T1>() + GetMinimumSize<T2>() + GetMinimumSize<T3>() : capacity)
        {
            this.message = message;
            writer1 = GetWriter<T1>();
            writer2 = GetWriter<T2>();
            writer3 = GetWriter<T3>();
        }
        public byte[] Get(T1 arg1, T2 arg2, T3 arg3)
        {
            Flush();
            Write(writer1, arg1);
            Write(writer2, arg2);
            Write(writer3, arg3);
            PrependData(message);
            return buffer;
        }
    }
    public sealed class DynamicByteWriter<T1, T2, T3, T4> : ByteWriter
    {
        private readonly Writer<T1> writer1;
        private readonly Writer<T2> writer2;
        private readonly Writer<T3> writer3;
        private readonly Writer<T4> writer4;
        public DynamicByteWriter(ushort message, bool shouldPrepend = true, int capacity = 0) : base(message, shouldPrepend, capacity < 1 ? 
                                                                                                                                   GetMinimumSize<T1>() + GetMinimumSize<T2>() + GetMinimumSize<T3>() + 
                                                                                                                                   GetMinimumSize<T4>() : capacity)
        {
            this.message = message;
            writer1 = GetWriter<T1>();
            writer2 = GetWriter<T2>();
            writer3 = GetWriter<T3>();
            writer4 = GetWriter<T4>();
        }
        public byte[] Get(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            Flush();
            Write(writer1, arg1);
            Write(writer2, arg2);
            Write(writer3, arg3);
            Write(writer4, arg4);
            PrependData(message);
            return buffer;
        }
    }
    public sealed class DynamicByteWriter<T1, T2, T3, T4, T5> : ByteWriter
    {
        private readonly Writer<T1> writer1;
        private readonly Writer<T2> writer2;
        private readonly Writer<T3> writer3;
        private readonly Writer<T4> writer4;
        private readonly Writer<T5> writer5;
        public DynamicByteWriter(ushort message, bool shouldPrepend = true, int capacity = 0) : base(message, shouldPrepend, capacity < 1 ? 
                                                                                                                                   GetMinimumSize<T1>() + GetMinimumSize<T2>() + GetMinimumSize<T3>() + 
                                                                                                                                   GetMinimumSize<T4>() + GetMinimumSize<T5>() : capacity)
        {
            this.message = message;
            writer1 = GetWriter<T1>();
            writer2 = GetWriter<T2>();
            writer3 = GetWriter<T3>();
            writer4 = GetWriter<T4>();
            writer5 = GetWriter<T5>();
        }
        public byte[] Get(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            Flush();
            Write(writer1, arg1);
            Write(writer2, arg2);
            Write(writer3, arg3);
            Write(writer4, arg4);
            Write(writer5, arg5);
            PrependData(message);
            return buffer;
        }
    }
    public sealed class DynamicByteWriter<T1, T2, T3, T4, T5, T6> : ByteWriter
    {
        private readonly Writer<T1> writer1;
        private readonly Writer<T2> writer2;
        private readonly Writer<T3> writer3;
        private readonly Writer<T4> writer4;
        private readonly Writer<T5> writer5;
        private readonly Writer<T6> writer6;
        public DynamicByteWriter(ushort message, bool shouldPrepend = true, int capacity = 0) : base(message, shouldPrepend, capacity < 1 ? 
                                                                                                                                   GetMinimumSize<T1>() + GetMinimumSize<T2>() + GetMinimumSize<T3>() +
                                                                                                                                   GetMinimumSize<T4>() + GetMinimumSize<T5>() + GetMinimumSize<T6>() : capacity)
        {
            this.message = message;
            writer1 = GetWriter<T1>();
            writer2 = GetWriter<T2>();
            writer3 = GetWriter<T3>();
            writer4 = GetWriter<T4>();
            writer5 = GetWriter<T5>();
            writer6 = GetWriter<T6>();
        }
        public byte[] Get(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            Flush();
            Write(writer1, arg1);
            Write(writer2, arg2);
            Write(writer3, arg3);
            Write(writer4, arg4);
            Write(writer5, arg5);
            Write(writer6, arg6);
            PrependData(message);
            return buffer;
        }
    }
    public sealed class DynamicByteWriter<T1, T2, T3, T4, T5, T6, T7> : ByteWriter
    {
        private readonly Writer<T1> writer1;
        private readonly Writer<T2> writer2;
        private readonly Writer<T3> writer3;
        private readonly Writer<T4> writer4;
        private readonly Writer<T5> writer5;
        private readonly Writer<T6> writer6;
        private readonly Writer<T7> writer7;
        public DynamicByteWriter(ushort message, bool shouldPrepend = true, int capacity = 0) : base(message, shouldPrepend, capacity < 1 ? 
                                                                                                                                   GetMinimumSize<T1>() + GetMinimumSize<T2>() + GetMinimumSize<T3>() +
                                                                                                                                   GetMinimumSize<T4>() + GetMinimumSize<T5>() + GetMinimumSize<T6>() + 
                                                                                                                                   GetMinimumSize<T7>() : capacity)
        {
            this.message = message;
            writer1 = GetWriter<T1>();
            writer2 = GetWriter<T2>();
            writer3 = GetWriter<T3>();
            writer4 = GetWriter<T4>();
            writer5 = GetWriter<T5>();
            writer6 = GetWriter<T6>();
            writer7 = GetWriter<T7>();
        }
        public byte[] Get(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            Flush();
            Write(writer1, arg1);
            Write(writer2, arg2);
            Write(writer3, arg3);
            Write(writer4, arg4);
            Write(writer5, arg5);
            Write(writer6, arg6);
            Write(writer7, arg7);
            PrependData(message);
            return buffer;
        }
    }
    public sealed class DynamicByteWriter<T1, T2, T3, T4, T5, T6, T7, T8> : ByteWriter
    {
        private readonly Writer<T1> writer1;
        private readonly Writer<T2> writer2;
        private readonly Writer<T3> writer3;
        private readonly Writer<T4> writer4;
        private readonly Writer<T5> writer5;
        private readonly Writer<T6> writer6;
        private readonly Writer<T7> writer7;
        private readonly Writer<T8> writer8;
        public DynamicByteWriter(ushort message, bool shouldPrepend = true, int capacity = 0) : base(message, shouldPrepend, capacity < 1 ? 
                                                                                                                                   GetMinimumSize<T1>() + GetMinimumSize<T2>() + GetMinimumSize<T3>() +
                                                                                                                                   GetMinimumSize<T4>() + GetMinimumSize<T5>() + GetMinimumSize<T6>() +
                                                                                                                                   GetMinimumSize<T7>() + GetMinimumSize<T8>() : capacity)
        {
            this.message = message;
            writer1 = GetWriter<T1>();
            writer2 = GetWriter<T2>();
            writer3 = GetWriter<T3>();
            writer4 = GetWriter<T4>();
            writer5 = GetWriter<T5>();
            writer6 = GetWriter<T6>();
            writer7 = GetWriter<T7>();
            writer8 = GetWriter<T8>();
        }
        public byte[] Get(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7 ,T8 arg8)
        {
            Flush();
            Write(writer1, arg1);
            Write(writer2, arg2);
            Write(writer3, arg3);
            Write(writer4, arg4);
            Write(writer5, arg5);
            Write(writer6, arg6);
            Write(writer7, arg7);
            Write(writer8, arg8);
            PrependData(message);
            return buffer;
        }
    }
    public sealed class DynamicByteWriter<T1, T2, T3, T4, T5, T6, T7, T8, T9> : ByteWriter
    {
        private readonly Writer<T1> writer1;
        private readonly Writer<T2> writer2;
        private readonly Writer<T3> writer3;
        private readonly Writer<T4> writer4;
        private readonly Writer<T5> writer5;
        private readonly Writer<T6> writer6;
        private readonly Writer<T7> writer7;
        private readonly Writer<T8> writer8;
        private readonly Writer<T9> writer9;
        public DynamicByteWriter(ushort message, bool shouldPrepend = true, int capacity = 0) : base(message, shouldPrepend, capacity < 1 ? 
                                                                                                                                   GetMinimumSize<T1>() + GetMinimumSize<T2>() + GetMinimumSize<T3>() +
                                                                                                                                   GetMinimumSize<T4>() + GetMinimumSize<T5>() + GetMinimumSize<T6>() +
                                                                                                                                   GetMinimumSize<T7>() + GetMinimumSize<T8>() + GetMinimumSize<T9>() : capacity)
        {
            this.message = message;
            writer1 = GetWriter<T1>();
            writer2 = GetWriter<T2>();
            writer3 = GetWriter<T3>();
            writer4 = GetWriter<T4>();
            writer5 = GetWriter<T5>();
            writer6 = GetWriter<T6>();
            writer7 = GetWriter<T7>();
            writer8 = GetWriter<T8>();
            writer9 = GetWriter<T9>();
        }
        public byte[] Get(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
        {
            Flush();
            Write(writer1, arg1);
            Write(writer2, arg2);
            Write(writer3, arg3);
            Write(writer4, arg4);
            Write(writer5, arg5);
            Write(writer6, arg6);
            Write(writer7, arg7);
            Write(writer8, arg8);
            Write(writer9, arg9);
            PrependData(message);
            return buffer;
        }
    }
    public sealed class DynamicByteWriter<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : ByteWriter
    {
        private readonly Writer<T1> writer1;
        private readonly Writer<T2> writer2;
        private readonly Writer<T3> writer3;
        private readonly Writer<T4> writer4;
        private readonly Writer<T5> writer5;
        private readonly Writer<T6> writer6;
        private readonly Writer<T7> writer7;
        private readonly Writer<T8> writer8;
        private readonly Writer<T9> writer9;
        private readonly Writer<T10> writer10;
        public DynamicByteWriter(ushort message, bool shouldPrepend = true, int capacity = 0) : base(message, shouldPrepend, capacity < 1 ? 
                                                                                                                                   GetMinimumSize<T1>() + GetMinimumSize<T2>() + GetMinimumSize<T3>() +
                                                                                                                                   GetMinimumSize<T4>() + GetMinimumSize<T5>() + GetMinimumSize<T6>() +
                                                                                                                                   GetMinimumSize<T7>() + GetMinimumSize<T8>() + GetMinimumSize<T9>() + 
                                                                                                                                   GetMinimumSize<T10>() : capacity)
        {
            this.message = message;
            writer1 = GetWriter<T1>();
            writer2 = GetWriter<T2>();
            writer3 = GetWriter<T3>();
            writer4 = GetWriter<T4>();
            writer5 = GetWriter<T5>();
            writer6 = GetWriter<T6>();
            writer7 = GetWriter<T7>();
            writer8 = GetWriter<T8>();
            writer9 = GetWriter<T9>();
            writer10 = GetWriter<T10>();
        }
        public byte[] Get(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
        {
            Flush();
            Write(writer1, arg1);
            Write(writer2, arg2);
            Write(writer3, arg3);
            Write(writer4, arg4);
            Write(writer5, arg5);
            Write(writer6, arg6);
            Write(writer7, arg7);
            Write(writer8, arg8);
            Write(writer9, arg9);
            Write(writer10, arg10);
            PrependData(message);
            return buffer;
        }
    }
    public interface IReadWrite<T> where T : new()
    {
        public void Read(ByteReader R);
        public void Write(ByteWriter W);
    }
    public static class IReadWriteEx
    {
        public static void WriteArrayShort<T>(this T[] objs, ByteWriter W) where T : IReadWrite<T>, new()
        {
            if (objs.Length > byte.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(objs), "Array too long, must be within byte range.");
            W.Write((byte)objs.Length);
            for (int i = 0; i < objs.Length; i++)
            {
                objs[i].Write(W);
            }
        }
        public static T[] ReadArrayShort<T>(this ByteReader R) where T : IReadWrite<T>, new()
        {
            int len = R.ReadUInt8();
            T[] rtn = new T[len];
            for (int i = 0; i < len; i++)
            {
                T obj = new T();
                obj.Read(R);
                rtn[i] = obj;
            }
            return rtn;
        }
        public static void WriteArray<T>(this T[] objs, ByteWriter W) where T : IReadWrite<T>, new()
        {
            if (objs.Length > ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(objs), "Array too long, must be within ushort range.");
            W.Write((ushort)objs.Length);
            for (int i = 0; i < objs.Length; i++)
            {
                objs[i].Write(W);
            }
        }
        public static T[] ReadArray<T>(this ByteReader R) where T : IReadWrite<T>, new()
        {
            int len = R.ReadUInt16();
            T[] rtn = new T[len];
            for (int i = 0; i < len; i++)
            {
                T obj = new T();
                obj.Read(R);
                rtn[i] = obj;
            }
            return rtn;
        }
        public static void WriteArrayLong<T>(this T[] objs, ByteWriter W) where T : IReadWrite<T>, new()
        {
            if (objs.LongLength > uint.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(objs), "Array too long, must be within uint range.");
            W.Write((uint)objs.LongLength);
            for (long i = 0; i < objs.LongLength; i++)
            {
                objs[i].Write(W);
            }
        }
        public static T[] ReadArrayLong<T>(this ByteReader R) where T : IReadWrite<T>, new()
        {
            long len = R.ReadUInt32();
            T[] rtn = new T[len];
            for (long i = 0; i < len; i++)
            {
                T obj = new T();
                obj.Read(R);
                rtn[i] = obj;
            }
            return rtn;
        }
    }
    public readonly struct NullableType<T> where T : IReadWrite<T>, new()
    {
        public T Value => _value;
        private readonly T _value;
        private readonly bool _hasValue;
        public NullableType(T obj)
        {
            _value = obj;
            _hasValue = obj != null;
        }
        public NullableType()
        {
            _value = default;
            _hasValue = false;
        }
        public static void Write(ref NullableType<T> obj, ByteWriter W)
        {
            W.Write(obj._hasValue);
            if (obj._hasValue)
                obj._value.Write(W);
        }
        public static NullableType<T> Read(ByteReader R)
        {
            bool hasValue = R.ReadBool();
            if (hasValue)
            {
                T value = new T();
                value.Read(R);
                return new NullableType<T>(value);
            }
            return new NullableType<T>();
        }
    }
}

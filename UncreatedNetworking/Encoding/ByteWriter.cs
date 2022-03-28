using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Uncreated.Networking.Encoding
{
    public class ByteWriter
    {
        public delegate void Writer<T>(ByteWriter writer, T arg1);
        public int size = 0;
        protected byte[] buffer;
        public byte[] ByteBuffer => buffer;
        protected static byte[] emptyArray = new byte[0];
        protected bool shouldPrepend;
        public int BaseCapacity;
        public ushort message;
        private readonly bool _isBigEndian;
        public ByteWriter(ushort message, bool shouldPrepend = true, int capacity = 0)
        {
            this.message = message;
            this._isBigEndian = !BitConverter.IsLittleEndian;
            this.BaseCapacity = capacity;
            if (BaseCapacity < 1)
            {
                buffer = emptyArray;
            }
            else
            {
                buffer = new byte[BaseCapacity];
            }
            this.shouldPrepend = shouldPrepend;
        }
        private void ExtendBuffer(int newsize)
        {
            byte[] old = buffer;
            int sz2 = old.Length;
            int sz = sz2 + sz2 / 2;
            if (sz < newsize) sz = newsize;
            buffer = new byte[sz];
            Buffer.BlockCopy(old, 0, buffer, 0, sz2);
        }
        public unsafe void PrependData(ushort MessageID)
        {
            if (!shouldPrepend) return;
            byte[] old = buffer;
            buffer = new byte[size + sizeof(ushort) + sizeof(int)];
            Buffer.BlockCopy(old, 0, buffer, sizeof(ushort) + sizeof(int), old.Length);

            fixed (byte* ptr = buffer)
            {
                byte* ptr2 = ptr + size;
                *(ushort*)ptr2 = MessageID;
                EndianCheck(ptr2, sizeof(ushort));
                ptr2 += sizeof(ushort);
                *(int*)ptr2 = size;
                EndianCheck(ptr2, sizeof(int));
            }
        }
        public static unsafe void CopyPrependData(byte[] bytes, int index, ushort MessageID, int length)
        {
            fixed (byte* ptr = bytes)
            {
                byte* ptr2 = ptr + index;
                *(ushort*)ptr2 = MessageID;
                if (!BitConverter.IsLittleEndian)
                {
                    byte* stack = stackalloc byte[sizeof(ushort)];
                    Buffer.MemoryCopy(ptr, stack, sizeof(ushort), sizeof(ushort));
                    for (int i = 0; i < sizeof(ushort); i++)
                        ptr[i] = stack[sizeof(ushort) - i - 1];
                }
                ptr2 += sizeof(ushort);
                *(int*)ptr2 = length;
                if (!BitConverter.IsLittleEndian)
                {
                    byte* stack = stackalloc byte[sizeof(int)];
                    Buffer.MemoryCopy(ptr, stack, sizeof(int), sizeof(int));
                    for (int i = 0; i < sizeof(int); i++)
                        ptr[i] = stack[sizeof(int) - i - 1];
                }
            }
        }
        private unsafe void EndianCheck(byte* litEndStrt, int size)
        {
            if (_isBigEndian && size > 1)
            {
                byte* stack = stackalloc byte[size];
                Buffer.MemoryCopy(litEndStrt, stack, size, size);
                for (int i = 0; i < size; i++)
                    litEndStrt[i] = stack[size - i - 1];
            }
        }
        private unsafe void WriteInternal<T>(T value) where T : unmanaged
        {
            int newsize = size + sizeof(T);
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);
            fixed (byte* ptr = buffer)
            {
                byte* ptr2 = ptr + size;
                *(T*)ptr2 = value;
                EndianCheck(ptr2, sizeof(T));
            }
            size = newsize;
        }

        private static readonly MethodInfo WriteInt32Method = typeof(ByteWriter).GetMethod(nameof(Write), BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(int) }, null);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Write(int n) => WriteInternal(n);

        private static readonly MethodInfo WriteUInt32Method = typeof(ByteWriter).GetMethod(nameof(Write), BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(uint) }, null);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Write(uint n) => WriteInternal(n);

        private static readonly MethodInfo WriteUInt8Method = typeof(ByteWriter).GetMethod(nameof(Write), BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(byte) }, null);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Write(byte n)
        {
            int newsize = size + 1;
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);
            buffer[size] = n;
            size = newsize;
        }

        private static readonly MethodInfo WriteInt8Method = typeof(ByteWriter).GetMethod(nameof(Write), BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(sbyte) }, null);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Write(sbyte n)
        {
            int newsize = size + 1;
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);
            buffer[size] = unchecked((byte)n);
            size = newsize;
        }

        private static readonly MethodInfo WriteBooleanMethod = typeof(ByteWriter).GetMethod(nameof(Write), BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(bool) }, null);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Write(bool n)
        {
            int newsize = size + 1;
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);
            buffer[size] = (byte)(n ? 1 : 0);
            size = newsize;
        }

        private static readonly MethodInfo WriteInt64Method = typeof(ByteWriter).GetMethod(nameof(Write), BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(long) }, null);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Write(long n) => WriteInternal(n);

        private static readonly MethodInfo WriteUInt64Method = typeof(ByteWriter).GetMethod(nameof(Write), BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(ulong) }, null);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Write(ulong n) => WriteInternal(n);

        private static readonly MethodInfo WriteInt16Method = typeof(ByteWriter).GetMethod(nameof(Write), BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(short) }, null);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Write(short n) => WriteInternal(n);

        private static readonly MethodInfo WriteUInt16Method = typeof(ByteWriter).GetMethod(nameof(Write), BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(ushort) }, null);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Write(ushort n) => WriteInternal(n);

        private static readonly MethodInfo WriteFloatMethod = typeof(ByteWriter).GetMethod(nameof(Write), BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(float) }, null);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Write(float n) => WriteInternal(n);

        private static readonly MethodInfo WriteDecimalMethod = typeof(ByteWriter).GetMethod(nameof(Write), BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(decimal) }, null);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Write(decimal n) => WriteInternal(n);

        private static readonly MethodInfo WriteDoubleMethod = typeof(ByteWriter).GetMethod(nameof(Write), BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(double) }, null);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Write(double n) => WriteInternal(n);

        private static readonly MethodInfo WriteCharMethod = typeof(ByteWriter).GetMethod(nameof(Write), BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(char) }, null);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Write(char n) => WriteInternal(n);

        private static readonly MethodInfo WriteStringMethod = typeof(ByteWriter).GetMethod(nameof(Write), BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(string) }, null);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Write(string n)
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

            fixed (byte* ptr = buffer)
            {
                byte* ptr2 = ptr + size;
                *(ushort*)ptr2 = (ushort)str.Length;
                EndianCheck(ptr2, sizeof(ushort));
            }
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

        private static readonly MethodInfo WriteDateTimeMethod = typeof(ByteWriter).GetMethod(nameof(Write), BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(DateTime) }, null);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Write(DateTime n) => WriteInternal(n.Ticks);

        private static readonly MethodInfo WriteTimeSpanMethod = typeof(ByteWriter).GetMethod(nameof(Write), BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(TimeSpan) }, null);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Write(TimeSpan n) => WriteInternal(n.Ticks);

        private static readonly MethodInfo WriteGUIDMethod = typeof(ByteWriter).GetMethod(nameof(Write), BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(Guid) }, null);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Write(Guid n)
        {
            byte[] guid = n.ToByteArray();
            int newsize = size + guid.Length;
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);
            Buffer.BlockCopy(guid, 0, buffer, size, guid.Length);
            size = newsize;
        }

        private static readonly MethodInfo WriteVector2Method = typeof(ByteWriter).GetMethod(nameof(Write), BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(Vector2) }, null);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Write(Vector2 n)
        {
            int newsize = size + sizeof(float) * 2;
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);
            fixed (byte* ptr = buffer)
            {
                byte* ptr2 = ptr + size;
                *(float*)ptr2 = n.x;
                EndianCheck(ptr2, sizeof(float));
                ptr2 += sizeof(float);
                *(float*)ptr2 = n.y;
                EndianCheck(ptr2, sizeof(float));
            }
            size = newsize;
        }

        private static readonly MethodInfo WriteVector3Method = typeof(ByteWriter).GetMethod(nameof(Write), BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(Vector3) }, null);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Write(Vector3 n)
        {
            int newsize = size + sizeof(float) * 3;
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);
            fixed (byte* ptr = buffer)
            {
                byte* ptr2 = ptr + size;
                *(float*)ptr2 = n.x;
                EndianCheck(ptr2, sizeof(float));
                ptr2 += sizeof(float);
                *(float*)ptr2 = n.y;
                EndianCheck(ptr2, sizeof(float));
                ptr2 += sizeof(float);
                *(float*)ptr2 = n.z;
                EndianCheck(ptr2, sizeof(float));
            }
            size = newsize;
        }

        private static readonly MethodInfo WriteVector4Method = typeof(ByteWriter).GetMethod(nameof(Write), BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(Vector4) }, null);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Write(Vector4 n)
        {
            int newsize = size + sizeof(float) * 4;
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);
            fixed (byte* ptr = buffer)
            {
                byte* ptr2 = ptr + size;
                *(float*)ptr2 = n.x;
                EndianCheck(ptr2, sizeof(float));
                ptr2 += sizeof(float);
                *(float*)ptr2 = n.y;
                EndianCheck(ptr2, sizeof(float));
                ptr2 += sizeof(float);
                *(float*)ptr2 = n.z;
                EndianCheck(ptr2, sizeof(float));
                ptr2 += sizeof(float);
                *(float*)ptr2 = n.w;
                EndianCheck(ptr2, sizeof(float));
            }
            size = newsize;
        }

        private static readonly MethodInfo WriteQuaternionMethod = typeof(ByteWriter).GetMethod(nameof(Write), BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(Quaternion) }, null);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Write(Quaternion n)
        {
            int newsize = size + sizeof(float) * 4;
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);
            fixed (byte* ptr = buffer)
            {
                byte* ptr2 = ptr + size;
                *(float*)ptr2 = n.x;
                EndianCheck(ptr2, sizeof(float));
                ptr2 += sizeof(float);
                *(float*)ptr2 = n.y;
                EndianCheck(ptr2, sizeof(float));
                ptr2 += sizeof(float);
                *(float*)ptr2 = n.z;
                EndianCheck(ptr2, sizeof(float));
                ptr2 += sizeof(float);
                *(float*)ptr2 = n.w;
                EndianCheck(ptr2, sizeof(float));
            }
            size = newsize;
        }

        private static readonly MethodInfo WriteColorMethod = typeof(ByteWriter).GetMethod(nameof(Write), BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(Color) }, null);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Write(Color n)
        {
            int newsize = size + sizeof(float) * 4;
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);
            fixed (byte* ptr = buffer)
            {
                byte* ptr2 = ptr + size;
                *(float*)ptr2 = n.r;
                EndianCheck(ptr2, sizeof(float));
                ptr2 += sizeof(float);
                *(float*)ptr2 = n.g;
                EndianCheck(ptr2, sizeof(float));
                ptr2 += sizeof(float);
                *(float*)ptr2 = n.b;
                EndianCheck(ptr2, sizeof(float));
                ptr2 += sizeof(float);
                *(float*)ptr2 = n.a;
                EndianCheck(ptr2, sizeof(float));
            }
            size = newsize;
        }

        private static readonly MethodInfo WriteColor32Method = typeof(ByteWriter).GetMethod(nameof(Write), BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(Color32) }, null);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Write(Color32 n)
        {
            int newsize = size + 4;
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);
            fixed (byte* ptr = buffer)
            {
                *(ptr + size) = n.r;
                *(ptr + size + 1) = n.g;
                *(ptr + size + 2) = n.b;
                *(ptr + size + 3) = n.a;
            }
            size = newsize;
        }

        private static readonly MethodInfo WriteEnumMethod = typeof(ByteWriter).GetMethod(nameof(WriteEnum), BindingFlags.Instance | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe void WriteEnum<TEnum>(TEnum o) where TEnum : unmanaged, Enum => WriteInternal(o);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write<TEnum>(TEnum o) where TEnum : unmanaged, Enum => WriteEnum(o);

        private static readonly MethodInfo WriteByteArrayMethod = typeof(ByteWriter).GetMethod(nameof(Write), BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(byte[]) }, null);
        [MethodImpl(MethodImplOptions.NoInlining)]
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
                byte* ptr2 = ptr + size;
                *(ushort*)ptr2 = (ushort)n.Length;
                EndianCheck(ptr2, sizeof(ushort));
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
                byte* ptr2 = ptr + size;
                *(int*)ptr2 = n.Length;
                EndianCheck(ptr2, sizeof(int));
            }
            Buffer.BlockCopy(n, 0, buffer, size + sizeof(int), n.Length);
            size = newsize;
        }
        public void Flush()
        {
            if (BaseCapacity == 0)
                buffer = emptyArray;
            else buffer = new byte[BaseCapacity];
            size = 0;
        }

        private static readonly MethodInfo WriteInt32ArrayMethod = typeof(ByteWriter).GetMethod(nameof(Write), BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(int[]) }, null);
        [MethodImpl(MethodImplOptions.NoInlining)]
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
                byte* ptr2 = ptr + size;
                *(ushort*)ptr2 = (ushort)n.Length;
                EndianCheck(ptr2, sizeof(ushort));
                ptr2 += sizeof(ushort);
                for (int i = 0; i < n.Length; i++)
                {
                    *(int*)ptr2 = n[i];
                    EndianCheck(ptr2, sizeof(int));
                    ptr2 += sizeof(int);
                }
            }
            size = newsize;
        }

        private static readonly MethodInfo WriteUInt32ArrayMethod = typeof(ByteWriter).GetMethod(nameof(Write), BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(uint[]) }, null);
        [MethodImpl(MethodImplOptions.NoInlining)]
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
                byte* ptr2 = ptr + size;
                *(ushort*)ptr2 = (ushort)n.Length;
                EndianCheck(ptr2, sizeof(ushort));
                ptr2 += sizeof(ushort);
                for (int i = 0; i < n.Length; i++)
                {
                    *(uint*)ptr2 = n[i];
                    EndianCheck(ptr2, sizeof(uint));
                    ptr2 += sizeof(uint);
                }
            }
            size = newsize;
        }

        private static readonly MethodInfo WriteInt8ArrayMethod = typeof(ByteWriter).GetMethod(nameof(Write), BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(sbyte[]) }, null);
        [MethodImpl(MethodImplOptions.NoInlining)]
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
                byte* ptr2 = ptr + size;
                *(ushort*)ptr2 = (ushort)n.Length;
                EndianCheck(ptr2, sizeof(ushort));
                ptr2 += sizeof(ushort);
                for (int i = 0; i < n.Length; i++)
                {
                    *(sbyte*)ptr2 = n[i];
                    EndianCheck(ptr2, sizeof(sbyte));
                    ptr2 += sizeof(sbyte);
                }
            }
            size = newsize;
        }

        private static readonly MethodInfo WriteBooleanArrayMethod = typeof(ByteWriter).GetMethod(nameof(Write), BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(bool[]) }, null);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Write(bool[] n)
        {
            if (n.Length > ushort.MaxValue)
            {
                Console.WriteLine($"Boolean array too long for writing, must be below {ushort.MaxValue} elements, it was {n.Length} elements long.");
                Console.WriteLine(Environment.StackTrace);
                return;
            }
            int newsize = size + (int)Math.Ceiling(n.Length / 8f) + sizeof(ushort);
            if (newsize > buffer.Length)
                ExtendBuffer(newsize);

            fixed (byte* ptr = buffer)
            {
                byte* ptr2 = ptr + size;
                *(ushort*)ptr2 = (ushort)n.Length;
                EndianCheck(ptr, sizeof(ushort));
                ptr2 += sizeof(ushort);
                byte current = 0;
                int cutoff = n.Length - 1;
                for (int i = 0; i < n.Length; i++)
                {
                    bool c = n[i];
                    int mod = i % 8;
                    if (mod == 0 && i != 0)
                    {
                        *ptr2 = current;
                        ptr2++;
                        current = (byte)(c ? 1 : 0);
                    }
                    else if (c) current |= (byte)(1 << mod);
                    if (i == cutoff)
                        *ptr2 = current;
                }
            }
            size = newsize;
        }

        private static readonly MethodInfo WriteInt64ArrayMethod = typeof(ByteWriter).GetMethod(nameof(Write), BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(long[]) }, null);
        [MethodImpl(MethodImplOptions.NoInlining)]
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
                byte* ptr2 = ptr + size;
                *(ushort*)ptr2 = (ushort)n.Length;
                EndianCheck(ptr2, sizeof(ushort));
                ptr2 += sizeof(ushort);
                for (int i = 0; i < n.Length; i++)
                {
                    *(long*)ptr2 = n[i];
                    EndianCheck(ptr2, sizeof(long));
                    ptr2 += sizeof(long);
                }
            }
            size = newsize;
        }

        private static readonly MethodInfo WriteUInt64ArrayMethod = typeof(ByteWriter).GetMethod(nameof(Write), BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(ulong[]) }, null);
        [MethodImpl(MethodImplOptions.NoInlining)]
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
                byte* ptr2 = ptr + size;
                *(ushort*)ptr2 = (ushort)n.Length;
                EndianCheck(ptr2, sizeof(ushort));
                ptr2 += sizeof(ushort);
                for (int i = 0; i < n.Length; i++)
                {
                    *(ulong*)ptr2 = n[i];
                    EndianCheck(ptr2, sizeof(ulong));
                    ptr2 += sizeof(ulong);
                }
            }
            size = newsize;
        }

        private static readonly MethodInfo WriteInt16ArrayMethod = typeof(ByteWriter).GetMethod(nameof(Write), BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(short[]) }, null);
        [MethodImpl(MethodImplOptions.NoInlining)]
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
                byte* ptr2 = ptr + size;
                *(ushort*)ptr2 = (ushort)n.Length;
                EndianCheck(ptr2, sizeof(ushort));
                ptr2 += sizeof(ushort);
                for (int i = 0; i < n.Length; i++)
                {
                    *(short*)ptr2 = n[i];
                    EndianCheck(ptr2, sizeof(short));
                    ptr2 += sizeof(short);
                }
            }
            size = newsize;
        }

        private static readonly MethodInfo WriteUInt16ArrayMethod = typeof(ByteWriter).GetMethod(nameof(Write), BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(ushort[]) }, null);
        [MethodImpl(MethodImplOptions.NoInlining)]
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
                byte* ptr2 = ptr + size;
                *(ushort*)ptr2 = (ushort)n.Length;
                EndianCheck(ptr2, sizeof(ushort));
                ptr2 += sizeof(ushort);
                for (int i = 0; i < n.Length; i++)
                {
                    *(ushort*)ptr2 = n[i];
                    EndianCheck(ptr2, sizeof(ushort));
                    ptr2 += sizeof(ushort);
                }
            }
            size = newsize;
        }

        private static readonly MethodInfo WriteFloatArrayMethod = typeof(ByteWriter).GetMethod(nameof(Write), BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(float[]) }, null);
        [MethodImpl(MethodImplOptions.NoInlining)]
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
                byte* ptr2 = ptr + size;
                *(ushort*)ptr2 = (ushort)n.Length;
                EndianCheck(ptr2, sizeof(ushort));
                ptr2 += sizeof(ushort);
                for (int i = 0; i < n.Length; i++)
                {
                    *(float*)ptr2 = n[i];
                    EndianCheck(ptr2, sizeof(float));
                    ptr2 += sizeof(float);
                }
            }
            size = newsize;
        }

        private static readonly MethodInfo WriteDecimalArrayMethod = typeof(ByteWriter).GetMethod(nameof(Write), BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(decimal[]) }, null);
        [MethodImpl(MethodImplOptions.NoInlining)]
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
                byte* ptr2 = ptr + size;
                *(ushort*)ptr2 = (ushort)n.Length;
                EndianCheck(ptr2, sizeof(ushort));
                ptr2 += sizeof(ushort);
                for (int i = 0; i < n.Length; i++)
                {
                    *(decimal*)ptr2 = n[i];
                    EndianCheck(ptr2, sizeof(decimal));
                    ptr2 += sizeof(decimal);
                }
            }
            size = newsize;
        }

        private static readonly MethodInfo WriteDoubleArrayMethod = typeof(ByteWriter).GetMethod(nameof(Write), BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(double[]) }, null);
        [MethodImpl(MethodImplOptions.NoInlining)]
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
                byte* ptr2 = ptr + size;
                *(ushort*)ptr2 = (ushort)n.Length;
                EndianCheck(ptr2, sizeof(ushort));
                ptr2 += sizeof(ushort);
                for (int i = 0; i < n.Length; i++)
                {
                    *(double*)ptr2 = n[i];
                    EndianCheck(ptr2, sizeof(double));
                    ptr2 += sizeof(double);
                }
            }
            size = newsize;
        }

        private static readonly MethodInfo WriteCharArrayMethod = typeof(ByteWriter).GetMethod(nameof(Write), BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(char[]) }, null);
        [MethodImpl(MethodImplOptions.NoInlining)]
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
                byte* ptr2 = ptr + size;
                *(ushort*)ptr2 = (ushort)n.Length;
                EndianCheck(ptr2, sizeof(ushort));
            }
            Buffer.BlockCopy(str, 0, buffer, size + sizeof(ushort), str.Length);
            size = newsize;
        }

        private static readonly MethodInfo WriteStringArrayMethod = typeof(ByteWriter).GetMethod(nameof(Write), BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(string[]) }, null);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void Write(string[] n)
        {
            if (n.Length > ushort.MaxValue)
            {
                Logging.LogWarning($"String array too long for writing, must be below {ushort.MaxValue} elements, it was {n.Length} elements long.");
                Logging.LogWarning(Environment.StackTrace);
                return;
            }

            WriteInternal((ushort)n.Length);
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
                    byte* ptr2 = ptr + size;
                    *(ushort*)ptr2 = (ushort)str.Length;
                    EndianCheck(ptr2, sizeof(ushort));
                }
                Buffer.BlockCopy(str, 0, buffer, size + sizeof(ushort), str.Length);
                size = newsize;
            }
        }
        public byte[] FinishWrite()
        {
            byte[] rtn = buffer;
            Flush();
            return rtn;
        }
        public void Write<T1>(Writer<T1> writer, T1 arg)
        {
            writer.Invoke(this, arg);
        }

        private static readonly Type[] parameters = new Type[2] { typeof(ByteWriter), null };
        public static Delegate GetWriter(Type type)
        {
            DynamicMethod method;
            lock (parameters)
            {
                parameters[1] = type ?? throw new ArgumentNullException(nameof(type));
                method = new DynamicMethod("Write" + type.Name, typeof(void), parameters, typeof(ByteWriter), false);
            }
            ILGenerator il = method.GetILGenerator();
            method.DefineParameter(1, ParameterAttributes.None, "writer");
            method.DefineParameter(2, ParameterAttributes.In, "value");

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);

            if (type.IsPrimitive)
            {
                if (type == typeof(ulong))
                    il.EmitCall(OpCodes.Call, WriteUInt64Method, null);
                else if (type == typeof(float))
                    il.EmitCall(OpCodes.Call, WriteFloatMethod, null);
                else if (type == typeof(long))
                    il.EmitCall(OpCodes.Call, WriteInt64Method, null);
                else if (type == typeof(ushort))
                    il.EmitCall(OpCodes.Call, WriteUInt16Method, null);
                else if (type == typeof(short))
                    il.EmitCall(OpCodes.Call, WriteInt16Method, null);
                else if (type == typeof(byte))
                    il.EmitCall(OpCodes.Call, WriteUInt8Method, null);
                else if (type == typeof(int))
                    il.EmitCall(OpCodes.Call, WriteInt32Method, null);
                else if (type == typeof(uint))
                    il.EmitCall(OpCodes.Call, WriteUInt32Method, null);
                else if (type == typeof(bool))
                    il.EmitCall(OpCodes.Call, WriteBooleanMethod, null);
                else if (type == typeof(sbyte))
                    il.EmitCall(OpCodes.Call, WriteInt8Method, null);
                else if (type == typeof(double))
                    il.EmitCall(OpCodes.Call, WriteDoubleMethod, null);
                else throw new ArgumentException($"Can not convert that type ({type.Name})!", nameof(type));
            }
            else if (type == typeof(string))
            {
                il.EmitCall(OpCodes.Call, WriteStringMethod, null);
            }
            else if (type.IsEnum)
            {
                il.EmitCall(OpCodes.Call, WriteEnumMethod.MakeGenericMethod(type), null);
            }
            else if (type == typeof(decimal))
            {
                il.EmitCall(OpCodes.Call, WriteDecimalMethod, null);
            }
            else if (type == typeof(DateTime))
            {
                il.EmitCall(OpCodes.Call, WriteDateTimeMethod, null);
            }
            else if (type == typeof(TimeSpan))
            {
                il.EmitCall(OpCodes.Call, WriteTimeSpanMethod, null);
            }
            else if (type == typeof(Guid))
            {
                il.EmitCall(OpCodes.Call, WriteGUIDMethod, null);
            }
            else if (type == typeof(Vector2))
            {
                il.EmitCall(OpCodes.Call, WriteVector2Method, null);
            }
            else if (type == typeof(Vector3))
            {
                il.EmitCall(OpCodes.Call, WriteVector3Method, null);
            }
            else if (type == typeof(Vector4))
            {
                il.EmitCall(OpCodes.Call, WriteVector4Method, null);
            }
            else if (type == typeof(Quaternion))
            {
                il.EmitCall(OpCodes.Call, WriteQuaternionMethod, null);
            }
            else if (type == typeof(Color))
            {
                il.EmitCall(OpCodes.Call, WriteColorMethod, null);
            }
            else if (type == typeof(Color32))
            {
                il.EmitCall(OpCodes.Call, WriteColor32Method, null);
            }
            else if (type.IsArray)
            {
                Type elemType = type.GetElementType();
                if (elemType == typeof(ulong))
                    il.EmitCall(OpCodes.Call, WriteUInt64ArrayMethod, null);
                else if (elemType == typeof(float))
                    il.EmitCall(OpCodes.Call, WriteFloatArrayMethod, null);
                else if (elemType == typeof(long))
                    il.EmitCall(OpCodes.Call, WriteInt64ArrayMethod, null);
                else if (elemType == typeof(ushort))
                    il.EmitCall(OpCodes.Call, WriteUInt16ArrayMethod, null);
                else if (elemType == typeof(short))
                    il.EmitCall(OpCodes.Call, WriteInt16ArrayMethod, null);
                else if (elemType == typeof(byte))
                    il.EmitCall(OpCodes.Call, WriteByteArrayMethod, null);
                else if (elemType == typeof(int))
                    il.EmitCall(OpCodes.Call, WriteInt32ArrayMethod, null);
                else if (elemType == typeof(uint))
                    il.EmitCall(OpCodes.Call, WriteUInt32ArrayMethod, null);
                else if (elemType == typeof(bool))
                    il.EmitCall(OpCodes.Call, WriteBooleanArrayMethod, null);
                else if (elemType == typeof(sbyte))
                    il.EmitCall(OpCodes.Call, WriteInt8ArrayMethod, null);
                else if (elemType == typeof(decimal))
                    il.EmitCall(OpCodes.Call, WriteDecimalArrayMethod, null);
                else if (elemType == typeof(double))
                    il.EmitCall(OpCodes.Call, WriteDoubleArrayMethod, null);
                else if (elemType == typeof(string))
                    il.EmitCall(OpCodes.Call, WriteStringArrayMethod, null);
                else throw new ArgumentException($"Can not convert that array type ({type.Name})!", nameof(type));
            }
            else throw new ArgumentException($"Can not convert that type ({type.Name})!", nameof(type));

            il.Emit(OpCodes.Ret);
            try
            {
                return method.CreateDelegate(typeof(Writer<>).MakeGenericType(type));
            }
            catch (InvalidProgramException ex)
            {
                Logging.LogError(ex);
                return null;
            }
        }
        public static bool TryGetWriter(Type type, out Delegate writer)
        {
            DynamicMethod method;
            lock (parameters)
            {
                parameters[1] = type ?? throw new ArgumentNullException(nameof(type));
                method = new DynamicMethod("Write" + type.Name, typeof(void), parameters, typeof(ByteWriter), false);
            }
            ILGenerator il = method.GetILGenerator();
            method.DefineParameter(1, ParameterAttributes.None, "writer");
            method.DefineParameter(2, ParameterAttributes.In, "value");

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            bool success = false;
            if (type.IsPrimitive)
            {
                if (type == typeof(ulong))
                {
                    il.EmitCall(OpCodes.Call, WriteUInt64Method, null);
                    success = true;
                }
                else if (type == typeof(float))
                {
                    il.EmitCall(OpCodes.Call, WriteFloatMethod, null);
                    success = true;
                }
                else if (type == typeof(long))
                {
                    il.EmitCall(OpCodes.Call, WriteInt64Method, null);
                    success = true;
                }
                else if (type == typeof(ushort))
                {
                    il.EmitCall(OpCodes.Call, WriteUInt16Method, null);
                    success = true;
                }
                else if (type == typeof(short))
                {
                    il.EmitCall(OpCodes.Call, WriteInt16Method, null);
                    success = true;
                }
                else if (type == typeof(byte))
                {
                    il.EmitCall(OpCodes.Call, WriteUInt8Method, null);
                    success = true;
                }
                else if (type == typeof(int))
                {
                    il.EmitCall(OpCodes.Call, WriteInt32Method, null);
                    success = true;
                }
                else if (type == typeof(uint))
                {
                    il.EmitCall(OpCodes.Call, WriteUInt32Method, null);
                    success = true;
                }
                else if (type == typeof(bool))
                {
                    il.EmitCall(OpCodes.Call, WriteBooleanMethod, null);
                    success = true;
                }
                else if (type == typeof(sbyte))
                {
                    il.EmitCall(OpCodes.Call, WriteInt8Method, null);
                    success = true;
                }
                else if (type == typeof(decimal))
                {
                    il.EmitCall(OpCodes.Call, WriteDecimalMethod, null);
                    success = true;
                }
                else if (type == typeof(double))
                {
                    il.EmitCall(OpCodes.Call, WriteDoubleMethod, null);
                    success = true;
                }
                else success = false;
            }
            else if (type == typeof(string))
            {
                il.EmitCall(OpCodes.Call, WriteStringMethod, null);
                success = true;
            }
            else if (type.IsEnum)
            {
                il.EmitCall(OpCodes.Call, WriteEnumMethod.MakeGenericMethod(type), null);
                success = true;
            }
            else if (type == typeof(DateTime))
            {
                il.EmitCall(OpCodes.Call, WriteDateTimeMethod, null);
                success = true;
            }
            else if (type == typeof(TimeSpan))
            {
                il.EmitCall(OpCodes.Call, WriteTimeSpanMethod, null);
                success = true;
            }
            else if (type == typeof(Guid))
            {
                il.EmitCall(OpCodes.Call, WriteGUIDMethod, null);
                success = true;
            }
            else if (type == typeof(Vector2))
            {
                il.EmitCall(OpCodes.Call, WriteVector2Method, null);
                success = true;
            }
            else if (type == typeof(Vector3))
            {
                il.EmitCall(OpCodes.Call, WriteVector3Method, null);
                success = true;
            }
            else if (type == typeof(Vector4))
            {
                il.EmitCall(OpCodes.Call, WriteVector4Method, null);
                success = true;
            }
            else if (type == typeof(Quaternion))
            {
                il.EmitCall(OpCodes.Call, WriteQuaternionMethod, null);
                success = true;
            }
            else if (type == typeof(Color))
            {
                il.EmitCall(OpCodes.Call, WriteColorMethod, null);
                success = true;
            }
            else if (type == typeof(Color32))
            {
                il.EmitCall(OpCodes.Call, WriteColor32Method, null);
                success = true;
            }
            else if (type.IsArray)
            {
                Type elemType = type.GetElementType();
                if (elemType == typeof(ulong))
                {
                    il.EmitCall(OpCodes.Call, WriteUInt64ArrayMethod, null);
                    success = true;
                }
                else if (elemType == typeof(float))
                {
                    il.EmitCall(OpCodes.Call, WriteFloatArrayMethod, null);
                    success = true;
                }
                else if (elemType == typeof(long))
                {
                    il.EmitCall(OpCodes.Call, WriteInt64ArrayMethod, null);
                    success = true;
                }
                else if (elemType == typeof(ushort))
                {
                    il.EmitCall(OpCodes.Call, WriteUInt16ArrayMethod, null);
                    success = true;
                }
                else if (elemType == typeof(short))
                {
                    il.EmitCall(OpCodes.Call, WriteInt16ArrayMethod, null);
                    success = true;
                }
                else if (elemType == typeof(byte))
                {
                    il.EmitCall(OpCodes.Call, WriteByteArrayMethod, null);
                    success = true;
                }
                else if (elemType == typeof(int))
                {
                    il.EmitCall(OpCodes.Call, WriteInt32ArrayMethod, null);
                    success = true;
                }
                else if (elemType == typeof(uint))
                {
                    il.EmitCall(OpCodes.Call, WriteUInt32ArrayMethod, null);
                    success = true;
                }
                else if (elemType == typeof(bool))
                {
                    il.EmitCall(OpCodes.Call, WriteBooleanArrayMethod, null);
                    success = true;
                }
                else if (elemType == typeof(sbyte))
                {
                    il.EmitCall(OpCodes.Call, WriteInt8ArrayMethod, null);
                    success = true;
                }
                else if (elemType == typeof(decimal))
                {
                    il.EmitCall(OpCodes.Call, WriteDecimalArrayMethod, null);
                    success = true;
                }
                else if (elemType == typeof(double))
                {
                    il.EmitCall(OpCodes.Call, WriteDoubleArrayMethod, null);
                    success = true;
                }
                else if (elemType == typeof(string))
                {
                    il.EmitCall(OpCodes.Call, WriteStringArrayMethod, null);
                    success = true;
                }
                else success = false;
            }
            if (!success)
            {
                writer = null;
                return success;
            }
            il.Emit(OpCodes.Ret);
            try
            {
                writer = method.CreateDelegate(typeof(Writer<>).MakeGenericType(type));
                return true;
            }
            catch (InvalidProgramException ex)
            {
                Logging.LogError(ex);
                writer = null;
                return false;
            }
        }
        public static Writer<T1> GetWriter<T1>() => (Writer<T1>)GetWriter(typeof(T1));
        public static bool TryGetWriter<T1>(out Writer<T1> writer)
        {
            if (TryGetWriter(typeof(T1), out Delegate writer2))
            {
                writer = (Writer<T1>)writer2;
                return true;
            }
            writer = null;
            return false;
        }
        public static int GetMinimumSize(Type type)
        {
            if (type.IsPointer) return IntPtr.Size;
            try
            {
                return Marshal.SizeOf(type);
            }
            catch (ArgumentException)
            {
                return 0;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetMinimumSize<T>() => GetMinimumSize(typeof(T));
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

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
    public class ByteReader
    {
        public delegate T Reader<T>(ByteReader reader);
        protected byte[] _buffer;
        public byte[] InternalBuffer { get => _buffer; set => _buffer = value; }
        protected int index;
        protected bool failure = false;
        public bool HasFailed { get => failure; }
        private readonly bool _isBigEndian;
        public ByteReader()
        {
            this._isBigEndian = !BitConverter.IsLittleEndian;
        }
        public void LoadNew(byte[] bytes)
        {
            this._buffer = bytes;
            this.index = 0;
            this.failure = false;
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
        public byte[] ReadBlock(int length)
        {
            if (index + length > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadBlock) + " at index " + index.ToString());
                return default;
            }
            byte[] rtn = new byte[length];
            Buffer.BlockCopy(_buffer, index, rtn, 0, length);
            index += length;
            return rtn;
        }
        public unsafe T ReadStruct<T>() where T : unmanaged
        {
            int size = sizeof(T);
            if (size == -1 || index + size > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadStruct) + "<" + typeof(T).Name + "> sizeof: " + size + " at index " + index.ToString());
                return default;
            }
            return Read<T>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] ReadUInt8Array() => ReadBytes();

        private static readonly MethodInfo ReadByteArrayMethod = typeof(ByteReader).GetMethod(nameof(ReadBytes), BindingFlags.Instance | BindingFlags.Public);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public byte[] ReadBytes()
        {
            ushort length = ReadUInt16();
            if (index + length > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadBlock) + " at index " + index.ToString());
                return default;
            }
            byte[] rtn = new byte[length];
            Buffer.BlockCopy(_buffer, index, rtn, 0, length);
            index += length;
            return rtn;
        }
        public byte[] ReadLongBytes()
        {
            int length = ReadInt32();
            if (index + length > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadBlock) + " at index " + index.ToString());
                return default;
            }
            byte[] rtn = new byte[length];
            Buffer.BlockCopy(_buffer, index, rtn, 0, length);
            index += length;
            return rtn;
        }

        private static readonly MethodInfo ReadInt32Method = typeof(ByteReader).GetMethod(nameof(ReadInt32), BindingFlags.Instance | BindingFlags.Public);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public int ReadInt32()
        {
            if (index + sizeof(int) > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadInt32) + " at index " + index.ToString());
                return default;
            }

            return Read<int>();
        }
        private static readonly MethodInfo ReadUInt32Method = typeof(ByteReader).GetMethod(nameof(ReadUInt32), BindingFlags.Instance | BindingFlags.Public);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public uint ReadUInt32()
        {
            if (index + sizeof(uint) > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadUInt32) + " at index " + index.ToString());
                return default;
            }
            return Read<uint>();
        }

        private static readonly MethodInfo ReadUInt8Method = typeof(ByteReader).GetMethod(nameof(ReadUInt8), BindingFlags.Instance | BindingFlags.Public);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public byte ReadUInt8()
        {
            if (index + 1 > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadUInt8) + " at index " + index.ToString());
                return default;
            }
            byte rtn = _buffer[index];
            index += 1;
            return rtn;
        }

        private static readonly MethodInfo ReadInt8Method = typeof(ByteReader).GetMethod(nameof(ReadInt8), BindingFlags.Instance | BindingFlags.Public);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public sbyte ReadInt8()
        {
            if (index + 1 > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadInt8) + " at index " + index.ToString());
                return default;
            }
            sbyte rtn = unchecked((sbyte)_buffer[index]);
            index += 1;
            return rtn;
        }


        private static readonly MethodInfo ReadBoolMethod = typeof(ByteReader).GetMethod(nameof(ReadBool), BindingFlags.Instance | BindingFlags.Public);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool ReadBool()
        {
            if (index + 1 > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadBool) + " at index " + index.ToString());
                return default;
            }
            bool rtn = _buffer[index] > 0;
            index += 1;
            return rtn;
        }

        private static readonly MethodInfo ReadInt64Method = typeof(ByteReader).GetMethod(nameof(ReadInt64), BindingFlags.Instance | BindingFlags.Public);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public long ReadInt64()
        {
            if (index + sizeof(long) > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadInt64) + " at index " + index.ToString());
                return default;
            }
            return Read<long>();
        }

        private static readonly MethodInfo ReadUInt64Method = typeof(ByteReader).GetMethod(nameof(ReadUInt64), BindingFlags.Instance | BindingFlags.Public);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public ulong ReadUInt64()
        {
            if (index + sizeof(ulong) > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadUInt64) + " at index " + index.ToString());
                return default;
            }
            return Read<ulong>();
        }

        private static readonly MethodInfo ReadInt16Method = typeof(ByteReader).GetMethod(nameof(ReadInt16), BindingFlags.Instance | BindingFlags.Public);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public short ReadInt16()
        {
            if (index + sizeof(short) > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadInt16) + " at index " + index.ToString());
                return default;
            }
            return Read<short>();
        }

        private static readonly MethodInfo ReadUInt16Method = typeof(ByteReader).GetMethod(nameof(ReadUInt16), BindingFlags.Instance | BindingFlags.Public);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public ushort ReadUInt16()
        {
            if (index + sizeof(ushort) > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadUInt16) + " at index " + index.ToString());
                return default;
            }
            return Read<ushort>();
        }

        private static readonly MethodInfo ReadFloatMethod = typeof(ByteReader).GetMethod(nameof(ReadFloat), BindingFlags.Instance | BindingFlags.Public);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public float ReadFloat()
        {
            if (index + sizeof(float) > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadFloat) + " at index " + index.ToString());
                return default;
            }
            return Read<float>();
        }

        private static readonly MethodInfo ReadDecimalMethod = typeof(ByteReader).GetMethod(nameof(ReadDecimal), BindingFlags.Instance | BindingFlags.Public);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public decimal ReadDecimal()
        {
            if (index + sizeof(decimal) > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadDecimal) + " at index " + index.ToString());
                return default;
            }

            return Read<decimal>();
        }

        private static readonly MethodInfo ReadDoubleMethod = typeof(ByteReader).GetMethod(nameof(ReadDouble), BindingFlags.Instance | BindingFlags.Public);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public double ReadDouble()
        {
            if (index + sizeof(double) > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadDouble) + " at index " + index.ToString());
                return default;
            }
            return Read<double>();
        }
        private unsafe T Read<T>() where T : unmanaged
        {
            T rtn;
            fixed (byte* ptr = _buffer)
            {
                byte* ptr2 = ptr + index;
                EndianCheck(ptr2, sizeof(T));
                rtn = *(T*)ptr2;
            }
            index += sizeof(T);
            return rtn;
        }
        private unsafe T ReadNoAdd<T>(int index) where T : unmanaged
        {
            T rtn;
            fixed (byte* ptr = _buffer)
            {
                byte* ptr2 = ptr + index;
                EndianCheck(ptr2, sizeof(T));
                rtn = *(T*)ptr2;
            }
            return rtn;
        }

        private static readonly MethodInfo ReadCharMethod = typeof(ByteReader).GetMethod(nameof(ReadChar), BindingFlags.Instance | BindingFlags.Public);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public char ReadChar()
        {
            if (index + sizeof(char) > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadChar) + " at index " + index.ToString());
                return default;
            }
            return Read<char>();
        }

        private static readonly MethodInfo ReadStringMethod = typeof(ByteReader).GetMethod(nameof(ReadString), BindingFlags.Instance | BindingFlags.Public);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public string ReadString()
        {
            ushort length = ReadUInt16();
            if (length == 0) return string.Empty;
            if (index + length > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadString) + " at index " + index.ToString());
                return string.Empty;
            }
            string str = System.Text.Encoding.UTF8.GetString(_buffer, index, length);
            index += length;
            return str;
        }
        public string ReadShortString()
        {
            byte length = ReadUInt8();
            if (length == 0) return string.Empty;
            if (index + length > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadShortString) + " at index " + index.ToString());
                return string.Empty;
            }
            string str = System.Text.Encoding.UTF8.GetString(_buffer, index, length);
            index += length;
            return str;
        }

        private static readonly MethodInfo ReadDateTimeMethod = typeof(ByteReader).GetMethod(nameof(ReadDateTime), BindingFlags.Instance | BindingFlags.Public);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public DateTime ReadDateTime() => new DateTime(ReadInt64());

        private static readonly MethodInfo ReadTimeSpanMethod = typeof(ByteReader).GetMethod(nameof(ReadTimeSpan), BindingFlags.Instance | BindingFlags.Public);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public TimeSpan ReadTimeSpan() => new TimeSpan(ReadInt64());

        private static readonly MethodInfo ReadGUIDMethod = typeof(ByteReader).GetMethod(nameof(ReadGUID), BindingFlags.Instance | BindingFlags.Public);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Guid ReadGUID()
        {
            if (index + 16 > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadGUID) + " at index " + index.ToString());
                return default;
            }
            byte[] guid = new byte[16];
            Buffer.BlockCopy(_buffer, index, guid, 0, 16);
            index += 16;
            return new Guid(guid);
        }
        public static readonly Vector2 V2NaN = new Vector2(float.NaN, float.NaN);
        public static readonly Vector3 V3NaN = new Vector3(float.NaN, float.NaN, float.NaN);
        public static readonly Vector4 V4NaN = new Vector4(float.NaN, float.NaN, float.NaN, float.NaN);
        public static readonly Quaternion QNaN = new Quaternion(float.NaN, float.NaN, float.NaN, float.NaN);
        public static readonly Color CNaN = new Color(float.NaN, float.NaN, float.NaN, float.NaN);
        public static readonly Color32 C32NaN = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);

        private static readonly MethodInfo ReadVector2Method = typeof(ByteReader).GetMethod(nameof(ReadVector2), BindingFlags.Instance | BindingFlags.Public);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Vector2 ReadVector2()
        {
            if (index + sizeof(float) * 2 > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadVector2) + " at index " + index.ToString());
                return V2NaN;
            }
            return new Vector2(Read<float>(), Read<float>());
        }

        private static readonly MethodInfo ReadVector3Method = typeof(ByteReader).GetMethod(nameof(ReadVector3), BindingFlags.Instance | BindingFlags.Public);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Vector3 ReadVector3()
        {
            if (index + sizeof(float) * 3 > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadVector3) + " at index " + index.ToString());
                return V3NaN;
            }
            return new Vector3(Read<float>(), Read<float>(), Read<float>());
        }

        private static readonly MethodInfo ReadVector4Method = typeof(ByteReader).GetMethod(nameof(ReadVector4), BindingFlags.Instance | BindingFlags.Public);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Vector4 ReadVector4()
        {
            if (index + sizeof(float) * 4 > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadVector4) + " at index " + index.ToString());
                return V4NaN;
            }
            return new Vector4(Read<float>(), Read<float>(), Read<float>(), Read<float>());
        }

        private static readonly MethodInfo ReadQuaternionMethod = typeof(ByteReader).GetMethod(nameof(ReadQuaternion), BindingFlags.Instance | BindingFlags.Public);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Quaternion ReadQuaternion()
        {
            if (index + sizeof(float) * 4 > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadQuaternion) + " at index " + index.ToString());
                return QNaN;
            }
            return new Quaternion(Read<float>(), Read<float>(), Read<float>(), Read<float>());
        }

        private static readonly MethodInfo ReadColorMethod = typeof(ByteReader).GetMethod(nameof(ReadColor), BindingFlags.Instance | BindingFlags.Public);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Color ReadColor()
        {
            if (index + sizeof(float) * 4 > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadColor) + " at index " + index.ToString());
                return CNaN;
            }
            return new Color(Read<float>(), Read<float>(), Read<float>(), Read<float>());
        }

        private static readonly MethodInfo ReadColor32Method = typeof(ByteReader).GetMethod(nameof(ReadColor32), BindingFlags.Instance | BindingFlags.Public);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe Color32 ReadColor32()
        {
            if (index + 4 > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadColor32) + " at index " + index.ToString());
                return C32NaN;
            }

            Color32 q;
            fixed (byte* ptr = _buffer)
            {
                q = new Color32(*(ptr + index), *(ptr + index + 1), *(ptr + index + 2), *(ptr + index + 3));
            }

            index += 4;
            return q;
        }
        
        private static readonly MethodInfo ReadEnumMethod = typeof(ByteReader).GetMethod(nameof(ReadEnum), BindingFlags.Instance | BindingFlags.Public);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe TEnum ReadEnum<TEnum>() where TEnum : unmanaged, Enum
        {
            if (index + sizeof(TEnum) > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadEnum) + "<" + typeof(TEnum).Name + "> at index " + index.ToString());
                return default;
            }
            return Read<TEnum>();
        }
        private static readonly MethodInfo ReadInt32ArrayMethod = typeof(ByteReader).GetMethod(nameof(ReadInt32Array), BindingFlags.Instance | BindingFlags.Public);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public int[] ReadInt32Array()
        {
            ushort len = ReadUInt16();
            int size = len * sizeof(int);
            if (index + size > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadInt32Array) + " at index " + index.ToString());
                return default;
            }
            int[] rtn = new int[len];
            for (int i = 0; i < len; i++)
                rtn[i] = BitConverter.ToInt32(_buffer, index + i * sizeof(int));
            index += size;
            return rtn;
        }

        private static readonly MethodInfo ReadUInt32ArrayMethod = typeof(ByteReader).GetMethod(nameof(ReadUInt32Array), BindingFlags.Instance | BindingFlags.Public);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public uint[] ReadUInt32Array()
        {
            ushort len = ReadUInt16();
            int size = len * sizeof(uint);
            if (index + size > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadUInt32Array) + " at index " + index.ToString());
                return default;
            }
            uint[] rtn = new uint[len];
            for (int i = 0; i < len; i++)
                rtn[i] = BitConverter.ToUInt32(_buffer, index + i * sizeof(uint));
            index += size;
            return rtn;
        }

        private static readonly MethodInfo ReadInt8ArrayMethod = typeof(ByteReader).GetMethod(nameof(ReadInt8Array), BindingFlags.Instance | BindingFlags.Public);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public sbyte[] ReadInt8Array()
        {
            ushort len = ReadUInt16();
            if (index + len > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadInt8Array) + " at index " + index.ToString());
                return default;
            }
            sbyte[] rtn = new sbyte[len];
            for (int i = 0; i < len; i++)
                rtn[i] = unchecked((sbyte)_buffer[index + i]);
            index += len;
            return rtn;
        }

        private static readonly MethodInfo ReadBoolArrayMethod = typeof(ByteReader).GetMethod(nameof(ReadBoolArray), BindingFlags.Instance | BindingFlags.Public);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe bool[] ReadBoolArray()
        {
            ushort len = ReadUInt16();
            if (len < 1) return new bool[0];
            int blen = (int)Math.Ceiling(len / 8f);
            if (index + blen > _buffer.Length)
            {
                failure |= true;
                Console.WriteLine("Failed to run " + nameof(ReadBoolArray) + " at index " + index.ToString() + " for an array of " + len + " elements.");
                return default;
            }
            bool[] rtn = new bool[len];
            fixed (byte* ptr = _buffer)
            {
                byte* ptr2 = ptr + index;
                byte current = *ptr2;
                for (int i = 0; i < len; i++)
                {
                    byte mod = (byte)(i % 8);
                    if (mod == 0 & i != 0)
                    {
                        ptr2++;
                        current = *ptr2;
                    }
                    rtn[i] = (1 & (current >> mod)) == 1;
                }
                return rtn;
            }
        }

        private static readonly MethodInfo ReadInt64ArrayMethod = typeof(ByteReader).GetMethod(nameof(ReadInt64Array), BindingFlags.Instance | BindingFlags.Public);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public long[] ReadInt64Array()
        {
            ushort len = ReadUInt16();
            int size = len * sizeof(long);
            if (index + size > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadInt64Array) + " at index " + index.ToString());
                return default;
            }
            long[] rtn = new long[len];
            for (int i = 0; i < len; i++)
                rtn[i] = BitConverter.ToInt64(_buffer, index + i * sizeof(long));
            index += size;
            return rtn;
        }

        private static readonly MethodInfo ReadUInt64ArrayMethod = typeof(ByteReader).GetMethod(nameof(ReadUInt64Array), BindingFlags.Instance | BindingFlags.Public);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public ulong[] ReadUInt64Array()
        {
            ushort len = ReadUInt16();
            int size = len * sizeof(ulong);
            if (index + size > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadUInt64Array) + " at index " + index.ToString());
                return default;
            }
            ulong[] rtn = new ulong[len];
            for (int i = 0; i < len; i++)
                rtn[i] = BitConverter.ToUInt64(_buffer, index + i * sizeof(ulong));
            index += size;
            return rtn;
        }

        private static readonly MethodInfo ReadInt16ArrayMethod = typeof(ByteReader).GetMethod(nameof(ReadInt16Array), BindingFlags.Instance | BindingFlags.Public);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public short[] ReadInt16Array()
        {
            ushort len = ReadUInt16();
            int size = len * sizeof(short);
            if (index + size > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadInt16Array) + " at index " + index.ToString());
                return default;
            }
            short[] rtn = new short[len];
            for (int i = 0; i < len; i++)
                rtn[i] = BitConverter.ToInt16(_buffer, index + i * sizeof(short));
            index += size;
            return rtn;
        }

        private static readonly MethodInfo ReadUInt16ArrayMethod = typeof(ByteReader).GetMethod(nameof(ReadUInt16Array), BindingFlags.Instance | BindingFlags.Public);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public ushort[] ReadUInt16Array()
        {
            ushort len = ReadUInt16();
            int size = len * sizeof(ushort);
            if (index + size > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadUInt16Array) + " at index " + index.ToString());
                return default;
            }
            ushort[] rtn = new ushort[len];
            for (int i = 0; i < len; i++)
                rtn[i] = BitConverter.ToUInt16(_buffer, index + i * sizeof(ushort));
            index += size;
            return rtn;
        }

        private static readonly MethodInfo ReadFloatArrayMethod = typeof(ByteReader).GetMethod(nameof(ReadFloatArray), BindingFlags.Instance | BindingFlags.Public);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public float[] ReadFloatArray()
        {
            ushort len = ReadUInt16();
            int size = len * sizeof(float);
            if (index + size > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadFloatArray) + " at index " + index.ToString());
                return default;
            }
            float[] rtn = new float[len];
            for (int i = 0; i < len; i++)
                rtn[i] = BitConverter.ToSingle(_buffer, index + i * sizeof(float));
            index += size;
            return rtn;
        }

        private static readonly MethodInfo ReadDecimalArrayMethod = typeof(ByteReader).GetMethod(nameof(ReadDecimalArray), BindingFlags.Instance | BindingFlags.Public);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public decimal[] ReadDecimalArray()
        {
            ushort len = ReadUInt16();
            int size = len * sizeof(double);
            if (index + size > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadDecimalArray) + " at index " + index.ToString());
                return default;
            }
            decimal[] rtn = new decimal[len];
            for (int i = 0; i < len; i++)
                rtn[i] = Convert.ToDecimal(BitConverter.ToDouble(_buffer, index + i * sizeof(double)));
            index += size;
            return rtn;
        }

        private static readonly MethodInfo ReadDoubleArrayMethod = typeof(ByteReader).GetMethod(nameof(ReadDoubleArray), BindingFlags.Instance | BindingFlags.Public);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public double[] ReadDoubleArray()
        {
            ushort len = ReadUInt16();
            int size = len * sizeof(double);
            if (index + size > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadDoubleArray) + " at index " + index.ToString());
                return default;
            }
            double[] rtn = new double[len];
            for (int i = 0; i < len; i++)
                rtn[i] = BitConverter.ToDouble(_buffer, index + i * sizeof(double));
            index += size;
            return rtn;
        }

        private static readonly MethodInfo ReadCharArrayMethod = typeof(ByteReader).GetMethod(nameof(ReadCharArray), BindingFlags.Instance | BindingFlags.Public);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public char[] ReadCharArray()
        {
            ushort length = ReadUInt16();
            if (length == 0) return new char[0];
            if (index + length > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadCharArray) + " at index " + index.ToString());
                return new char[0];
            }
            char[] rtn = System.Text.Encoding.UTF8.GetChars(_buffer, index, length);
            index += length;
            return rtn;
        }

        private static readonly MethodInfo ReadStringArrayMethod = typeof(ByteReader).GetMethod(nameof(ReadStringArray), BindingFlags.Instance | BindingFlags.Public);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public string[] ReadStringArray()
        {
            string[] rtn = new string[ReadUInt16()];
            for (int i = 0; i < rtn.Length; i++)
                rtn[i] = ReadString();
            return rtn;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void VerifyType<T>(int typeIndex = -1) => VerifyType(typeof(T), typeIndex);
        protected static void VerifyType(Type type, int typeIndex = -1)
        {
            if (!NetFactory.IsValidAutoType(type))
                throw new InvalidDynamicTypeException(type, typeIndex, true);
        }
        public T InvokeReader<T>(Reader<T> reader) => reader.Invoke(this);
        private static readonly Type[] parameters = new Type[1] { typeof(ByteReader) };
        public static Reader<T1> GetReader<T1>() => (Reader<T1>)GetReader(typeof(T1));
        public T Read<T>(T writer) where T : IReadWrite, new()
        {
            T r = new T();
            r.Read(this);
            return r;
        }
        public static Delegate GetReader(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            DynamicMethod method = new DynamicMethod("Read" + type.Name, type, parameters, typeof(ByteReader).Module, false);
            ILGenerator il = method.GetILGenerator();
            method.DefineParameter(1, ParameterAttributes.In, "value");

            il.Emit(OpCodes.Ldarg_0);

            if (type.IsPrimitive)
            {
                if (type == typeof(ulong))
                    il.EmitCall(OpCodes.Call, ReadUInt64Method, null);
                else if (type == typeof(float))
                    il.EmitCall(OpCodes.Call, ReadFloatMethod, null);
                else if (type == typeof(long))
                    il.EmitCall(OpCodes.Call, ReadInt64Method, null);
                else if (type == typeof(ushort))
                    il.EmitCall(OpCodes.Call, ReadUInt16Method, null);
                else if (type == typeof(short))
                    il.EmitCall(OpCodes.Call, ReadInt16Method, null);
                else if (type == typeof(byte))
                    il.EmitCall(OpCodes.Call, ReadUInt8Method, null);
                else if (type == typeof(int))
                    il.EmitCall(OpCodes.Call, ReadInt32Method, null);
                else if (type == typeof(uint))
                    il.EmitCall(OpCodes.Call, ReadUInt32Method, null);
                else if (type == typeof(bool))
                    il.EmitCall(OpCodes.Call, ReadBoolMethod, null);
                else if (type == typeof(char))
                    il.EmitCall(OpCodes.Call, ReadCharMethod, null);
                else if (type == typeof(sbyte))
                    il.EmitCall(OpCodes.Call, ReadInt8Method, null);
                else if (type == typeof(double))
                    il.EmitCall(OpCodes.Call, ReadDoubleMethod, null);
                else throw new ArgumentException($"Can not convert that type ({type.Name})!", nameof(type));
            }
            else if (type == typeof(string))
            {
                il.EmitCall(OpCodes.Call, ReadStringMethod, null);
            }
            else if (type.IsEnum)
            {
                il.EmitCall(OpCodes.Call, ReadEnumMethod.MakeGenericMethod(type), null);
            }
            else if (type == typeof(decimal))
            {
                il.EmitCall(OpCodes.Call, ReadDecimalMethod, null);
            }
            else if (type == typeof(DateTime))
            {
                il.EmitCall(OpCodes.Call, ReadDateTimeMethod, null);
            }
            else if (type == typeof(TimeSpan))
            {
                il.EmitCall(OpCodes.Call, ReadTimeSpanMethod, null);
            }
            else if (type == typeof(Guid))
            {
                il.EmitCall(OpCodes.Call, ReadGUIDMethod, null);
            }
            else if (type == typeof(Vector2))
            {
                il.EmitCall(OpCodes.Call, ReadVector2Method, null);
            }
            else if (type == typeof(Vector3))
            {
                il.EmitCall(OpCodes.Call, ReadVector3Method, null);
            }
            else if (type == typeof(Vector4))
            {
                il.EmitCall(OpCodes.Call, ReadVector4Method, null);
            }
            else if (type == typeof(Quaternion))
            {
                il.EmitCall(OpCodes.Call, ReadQuaternionMethod, null);
            }
            else if (type == typeof(Color))
            {
                il.EmitCall(OpCodes.Call, ReadColorMethod, null);
            }
            else if (type == typeof(Color32))
            {
                il.EmitCall(OpCodes.Call, ReadColor32Method, null);
            }
            else if (type.IsArray)
            {
                Type elemType = type.GetElementType();
                if (elemType == typeof(ulong))
                    il.EmitCall(OpCodes.Call, ReadUInt64ArrayMethod, null);
                else if (elemType == typeof(float))
                    il.EmitCall(OpCodes.Call, ReadFloatArrayMethod, null);
                else if (elemType == typeof(long))
                    il.EmitCall(OpCodes.Call, ReadInt64ArrayMethod, null);
                else if (elemType == typeof(ushort))
                    il.EmitCall(OpCodes.Call, ReadUInt16ArrayMethod, null);
                else if (elemType == typeof(short))
                    il.EmitCall(OpCodes.Call, ReadInt16ArrayMethod, null);
                else if (elemType == typeof(byte))
                    il.EmitCall(OpCodes.Call, ReadByteArrayMethod, null);
                else if (elemType == typeof(int))
                    il.EmitCall(OpCodes.Call, ReadInt32ArrayMethod, null);
                else if (elemType == typeof(uint))
                    il.EmitCall(OpCodes.Call, ReadUInt32ArrayMethod, null);
                else if (elemType == typeof(bool))
                    il.EmitCall(OpCodes.Call, ReadBoolArrayMethod, null);
                else if (elemType == typeof(sbyte))
                    il.EmitCall(OpCodes.Call, ReadInt8ArrayMethod, null);
                else if (elemType == typeof(decimal))
                    il.EmitCall(OpCodes.Call, ReadDecimalArrayMethod, null);
                else if (elemType == typeof(char))
                    il.EmitCall(OpCodes.Call, ReadCharArrayMethod, null);
                else if (elemType == typeof(double))
                    il.EmitCall(OpCodes.Call, ReadDoubleArrayMethod, null);
                else if (elemType == typeof(string))
                    il.EmitCall(OpCodes.Call, ReadStringArrayMethod, null);
                else throw new ArgumentException($"Can not convert that array type ({type.Name})!", nameof(type));
            }
            else throw new ArgumentException($"Can not convert that type ({type.Name})!", nameof(type));
            il.Emit(OpCodes.Ret);
            try
            {
                return method.CreateDelegate(typeof(Reader<>).MakeGenericType(type));
            }
            catch (InvalidProgramException ex)
            {
                Logging.LogError(ex);
                return null;
            }
            catch (ArgumentException ex)
            {
                Logging.LogError("Failed to create reader delegate for type " + type.FullName);
                Logging.LogError(ex);
                return null;
            }
        }
        public static bool TryGetReader(Type type, out Delegate reader)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            DynamicMethod method = new DynamicMethod("Read" + type.Name, type, parameters, typeof(ByteReader).Module, false);
            ILGenerator il = method.GetILGenerator();

            method.DefineParameter(1, ParameterAttributes.In, "value");

            il.Emit(OpCodes.Ldarg_0);
            bool success = false;
            if (type.IsPrimitive)
            {
                if (type == typeof(ulong))
                {
                    il.EmitCall(OpCodes.Call, ReadUInt64Method, null);
                    success = true;
                }
                else if (type == typeof(float))
                {
                    il.EmitCall(OpCodes.Call, ReadFloatMethod, null);
                    success = true;
                }
                else if (type == typeof(long))
                {
                    il.EmitCall(OpCodes.Call, ReadInt64Method, null);
                    success = true;
                }
                else if (type == typeof(ushort))
                {
                    il.EmitCall(OpCodes.Call, ReadUInt16Method, null);
                    success = true;
                }
                else if (type == typeof(short))
                {
                    il.EmitCall(OpCodes.Call, ReadInt16Method, null);
                    success = true;
                }
                else if (type == typeof(byte))
                {
                    il.EmitCall(OpCodes.Call, ReadUInt8Method, null);
                    success = true;
                }
                else if (type == typeof(int))
                {
                    il.EmitCall(OpCodes.Call, ReadInt32Method, null);
                    success = true;
                }
                else if (type == typeof(uint))
                {
                    il.EmitCall(OpCodes.Call, ReadUInt32Method, null);
                    success = true;
                }
                else if (type == typeof(bool))
                {
                    il.EmitCall(OpCodes.Call, ReadBoolMethod, null);
                    success = true;
                }
                else if (type == typeof(char))
                {
                    il.EmitCall(OpCodes.Call, ReadCharMethod, null);
                    success = true;
                }
                else if (type == typeof(sbyte))
                {
                    il.EmitCall(OpCodes.Call, ReadInt8Method, null);
                    success = true;
                }
                else if (type == typeof(double))
                {
                    il.EmitCall(OpCodes.Call, ReadDoubleMethod, null);
                    success = true;
                }
                else success = false;
            }
            else if (type == typeof(string))
            {
                il.EmitCall(OpCodes.Call, ReadStringMethod, null);
                success = true;
            }
            else if (type.IsEnum)
            {
                il.EmitCall(OpCodes.Call, ReadEnumMethod.MakeGenericMethod(type), null);
                success = true;
            }
            else if (type == typeof(decimal))
            {
                il.EmitCall(OpCodes.Call, ReadDecimalMethod, null);
                success = true;
            }
            else if (type == typeof(DateTime))
            {
                il.EmitCall(OpCodes.Call, ReadDateTimeMethod, null);
                success = true;
            }
            else if (type == typeof(TimeSpan))
            {
                il.EmitCall(OpCodes.Call, ReadTimeSpanMethod, null);
                success = true;
            }
            else if (type == typeof(Guid))
            {
                il.EmitCall(OpCodes.Call, ReadGUIDMethod, null);
                success = true;
            }
            else if (type == typeof(Vector2))
            {
                il.EmitCall(OpCodes.Call, ReadVector2Method, null);
                success = true;
            }
            else if (type == typeof(Vector3))
            {
                il.EmitCall(OpCodes.Call, ReadVector3Method, null);
                success = true;
            }
            else if (type == typeof(Vector4))
            {
                il.EmitCall(OpCodes.Call, ReadVector4Method, null);
                success = true;
            }
            else if (type == typeof(Quaternion))
            {
                il.EmitCall(OpCodes.Call, ReadQuaternionMethod, null);
                success = true;
            }
            else if (type == typeof(Color))
            {
                il.EmitCall(OpCodes.Call, ReadColorMethod, null);
                success = true;
            }
            else if (type == typeof(Color32))
            {
                il.EmitCall(OpCodes.Call, ReadColor32Method, null);
                success = true;
            }
            else if (type.IsArray)
            {
                Type elemType = type.GetElementType();
                if (elemType == typeof(ulong))
                {
                    il.EmitCall(OpCodes.Call, ReadUInt64ArrayMethod, null);
                    success = true;
                }
                else if (elemType == typeof(float))
                {
                    il.EmitCall(OpCodes.Call, ReadFloatArrayMethod, null);
                    success = true;
                }
                else if (elemType == typeof(long))
                {
                    il.EmitCall(OpCodes.Call, ReadInt64ArrayMethod, null);
                    success = true;
                }
                else if (elemType == typeof(ushort))
                {
                    il.EmitCall(OpCodes.Call, ReadUInt16ArrayMethod, null);
                    success = true;
                }
                else if (elemType == typeof(short))
                {
                    il.EmitCall(OpCodes.Call, ReadInt16ArrayMethod, null);
                    success = true;
                }
                else if (elemType == typeof(byte))
                {
                    il.EmitCall(OpCodes.Call, ReadByteArrayMethod, null);
                    success = true;
                }
                else if (elemType == typeof(int))
                {
                    il.EmitCall(OpCodes.Call, ReadInt32ArrayMethod, null);
                    success = true;
                }
                else if (elemType == typeof(uint))
                {
                    il.EmitCall(OpCodes.Call, ReadUInt32ArrayMethod, null);
                    success = true;
                }
                else if (elemType == typeof(bool))
                {
                    il.EmitCall(OpCodes.Call, ReadBoolArrayMethod, null);
                    success = true;
                }
                else if (elemType == typeof(sbyte))
                {
                    il.EmitCall(OpCodes.Call, ReadInt8ArrayMethod, null);
                    success = true;
                }
                else if (elemType == typeof(decimal))
                {
                    il.EmitCall(OpCodes.Call, ReadDecimalArrayMethod, null);
                    success = true;
                }
                else if (elemType == typeof(char))
                {
                    il.EmitCall(OpCodes.Call, ReadCharArrayMethod, null);
                    success = true;
                }
                else if (elemType == typeof(double))
                {
                    il.EmitCall(OpCodes.Call, ReadDoubleArrayMethod, null);
                    success = true;
                }
                else if (elemType == typeof(string))
                {
                    il.EmitCall(OpCodes.Call, ReadStringArrayMethod, null);
                    success = true;
                }
                else
                    success = false;
            }
            else success = false;
            if (!success)
            {
                reader = null;
                return success;
            }
            il.Emit(OpCodes.Ret);
            try
            {
                reader = method.CreateDelegate(typeof(Reader<>).MakeGenericType(type));
                return true;
            }
            catch (InvalidProgramException ex)
            {
                Logging.LogError(ex);
                reader = null;
                return false;
            }
            catch (ArgumentException ex)
            {
                Logging.LogError("Failed to create reader delegate for type " + type.FullName);
                Logging.LogError(ex);
                reader = null;
                return false;
            }
        }
        public static bool TryGetReader<T1>(out Reader<T1> reader)
        {
            if (TryGetReader(typeof(T1), out Delegate reader2))
            {
                reader = (Reader<T1>)reader2;
                return true;
            }
            reader = null;
            return false;
        }
        protected static class ReaderHelper<T>
        {
            public static readonly Reader<T> Reader;
            static ReaderHelper()
            {
                VerifyType<T>();
                Reader = GetReader<T>();
            }
        }
    }
    public sealed class ByteReaderRaw<T> : ByteReader
    {
        readonly Reader<T> reader;
        /// <summary>Leave <paramref name="reader"/> null to auto-fill.</summary>
        public ByteReaderRaw(Reader<T> reader)
        {
            _buffer = null;
            index = 0;
            this.reader = reader ?? ReaderHelper<T>.Reader;
            failure = false;
        }
        public bool Read(byte[] bytes, out T arg)
        {
            if (bytes != null)
                _buffer = bytes;
            index = 0;
            arg = reader.Invoke(this);
            bool oldfailure = failure;
            failure = false;
            return !oldfailure;
        }
    }
    public sealed class ByteReaderRaw<T1, T2> : ByteReader
    {
        readonly Reader<T1> reader1;
        readonly Reader<T2> reader2;
        /// <summary>Leave any reader null to auto-fill.</summary>
        public ByteReaderRaw(Reader<T1> reader1, Reader<T2> reader2)
        {
            _buffer = null;
            index = 0;
            this.reader1 = reader1 ?? ReaderHelper<T1>.Reader;
            this.reader2 = reader2 ?? ReaderHelper<T2>.Reader;
            failure = false;
        }
        public bool Read(byte[] bytes, out T1 arg1, out T2 arg2)
        {
            if (bytes != null)
                _buffer = bytes;
            index = 0;
            arg1 = reader1.Invoke(this);
            arg2 = reader2.Invoke(this);
            bool oldfailure = failure;
            failure = false;
            return !oldfailure;
        }
    }
    public sealed class ByteReaderRaw<T1, T2, T3> : ByteReader
    {
        readonly Reader<T1> reader1;
        readonly Reader<T2> reader2;
        readonly Reader<T3> reader3;
        /// <summary>Leave any reader null to auto-fill.</summary>
        public ByteReaderRaw(Reader<T1> reader1, Reader<T2> reader2, Reader<T3> reader3)
        {
            _buffer = null;
            index = 0;
            this.reader1 = reader1 ?? ReaderHelper<T1>.Reader;
            this.reader2 = reader2 ?? ReaderHelper<T2>.Reader;
            this.reader3 = reader3 ?? ReaderHelper<T3>.Reader;
            failure = false;
        }
        public bool Read(byte[] bytes, out T1 arg1, out T2 arg2, out T3 arg3)
        {
            if (bytes != null)
                _buffer = bytes;
            index = 0;
            arg1 = reader1.Invoke(this);
            arg2 = reader2.Invoke(this);
            arg3 = reader3.Invoke(this);
            bool oldfailure = failure;
            failure = false;
            return !oldfailure;
        }
    }
    public sealed class ByteReaderRaw<T1, T2, T3, T4> : ByteReader
    {
        readonly Reader<T1> reader1;
        readonly Reader<T2> reader2;
        readonly Reader<T3> reader3;
        readonly Reader<T4> reader4;
        /// <summary>Leave any reader null to auto-fill.</summary>
        public ByteReaderRaw(Reader<T1> reader1, Reader<T2> reader2, Reader<T3> reader3, Reader<T4> reader4)
        {
            _buffer = null;
            index = 0;
            this.reader1 = reader1 ?? ReaderHelper<T1>.Reader;
            this.reader2 = reader2 ?? ReaderHelper<T2>.Reader;
            this.reader3 = reader3 ?? ReaderHelper<T3>.Reader;
            this.reader4 = reader4 ?? ReaderHelper<T4>.Reader;
            failure = false;
        }
        public bool Read(byte[] bytes, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4)
        {
            if (bytes != null)
                _buffer = bytes;
            index = 0;
            arg1 = reader1.Invoke(this);
            arg2 = reader2.Invoke(this);
            arg3 = reader3.Invoke(this);
            arg4 = reader4.Invoke(this);
            bool oldfailure = failure;
            failure = false;
            return !oldfailure;
        }
    }
    public sealed class DynamicByteReader<T1> : ByteReader
    {
        static DynamicByteReader()
        {
            reader1 = ReaderHelper<T1>.Reader;
        }
        private static readonly Reader<T1> reader1;
        public DynamicByteReader() { }
        public bool Read(byte[] bytes, out T1 arg1)
        {
            if (bytes != null)
                _buffer = bytes;
            index = 0;
            arg1 = InvokeReader(reader1);
            bool oldfailure = failure;
            failure = false;
            return !oldfailure;
        }
    }
    public sealed class DynamicByteReader<T1, T2> : ByteReader
    {
        static DynamicByteReader()
        {
            reader1 = ReaderHelper<T1>.Reader;
            reader2 = ReaderHelper<T2>.Reader;
        }
        private static readonly Reader<T1> reader1;
        private static readonly Reader<T2> reader2;
        public DynamicByteReader() { }
        public bool Read(byte[] bytes, out T1 arg1, out T2 arg2)
        {
            if (bytes != null)
                _buffer = bytes;
            index = 0;
            arg1 = InvokeReader(reader1);
            arg2 = InvokeReader(reader2);
            bool oldfailure = failure;
            failure = false;
            return !oldfailure;
        }
    }
    public sealed class DynamicByteReader<T1, T2, T3> : ByteReader
    {
        static DynamicByteReader()
        {
            reader1 = ReaderHelper<T1>.Reader;
            reader2 = ReaderHelper<T2>.Reader;
            reader3 = ReaderHelper<T3>.Reader;
        }
        private static readonly Reader<T1> reader1;
        private static readonly Reader<T2> reader2;
        private static readonly Reader<T3> reader3;
        public DynamicByteReader() { }
        public bool Read(byte[] bytes, out T1 arg1, out T2 arg2, out T3 arg3)
        {
            if (bytes != null)
                _buffer = bytes;
            index = 0;
            arg1 = InvokeReader(reader1);
            arg2 = InvokeReader(reader2);
            arg3 = InvokeReader(reader3);
            bool oldfailure = failure;
            failure = false;
            return !oldfailure;
        }
    }
    public sealed class DynamicByteReader<T1, T2, T3, T4> : ByteReader
    {
        static DynamicByteReader()
        {
            reader1 = ReaderHelper<T1>.Reader;
            reader2 = ReaderHelper<T2>.Reader;
            reader3 = ReaderHelper<T3>.Reader;
            reader4 = ReaderHelper<T4>.Reader;
        }
        private static readonly Reader<T1> reader1;
        private static readonly Reader<T2> reader2;
        private static readonly Reader<T3> reader3;
        private static readonly Reader<T4> reader4;
        public DynamicByteReader() { }
        public bool Read(byte[] bytes, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4)
        {
            if (bytes != null)
                _buffer = bytes;
            index = 0;
            arg1 = InvokeReader(reader1);
            arg2 = InvokeReader(reader2);
            arg3 = InvokeReader(reader3);
            arg4 = InvokeReader(reader4);
            bool oldfailure = failure;
            failure = false;
            return !oldfailure;
        }
    }
    public sealed class DynamicByteReader<T1, T2, T3, T4, T5> : ByteReader
    {
        static DynamicByteReader()
        {
            reader1 = ReaderHelper<T1>.Reader;
            reader2 = ReaderHelper<T2>.Reader;
            reader3 = ReaderHelper<T3>.Reader;
            reader4 = ReaderHelper<T4>.Reader;
            reader5 = ReaderHelper<T5>.Reader;
        }
        private static readonly Reader<T1> reader1;
        private static readonly Reader<T2> reader2;
        private static readonly Reader<T3> reader3;
        private static readonly Reader<T4> reader4;
        private static readonly Reader<T5> reader5;
        public DynamicByteReader() { }
        public bool Read(byte[] bytes, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5)
        {
            if (bytes != null)
                _buffer = bytes;
            index = 0;
            arg1 = InvokeReader(reader1);
            arg2 = InvokeReader(reader2);
            arg3 = InvokeReader(reader3);
            arg4 = InvokeReader(reader4);
            arg5 = InvokeReader(reader5);
            bool oldfailure = failure;
            failure = false;
            return !oldfailure;
        }
    }
    public sealed class DynamicByteReader<T1, T2, T3, T4, T5, T6> : ByteReader
    {
        static DynamicByteReader()
        {
            reader1 = ReaderHelper<T1>.Reader;
            reader2 = ReaderHelper<T2>.Reader;
            reader3 = ReaderHelper<T3>.Reader;
            reader4 = ReaderHelper<T4>.Reader;
            reader5 = ReaderHelper<T5>.Reader;
            reader6 = ReaderHelper<T6>.Reader;
        }
        private static readonly Reader<T1> reader1;
        private static readonly Reader<T2> reader2;
        private static readonly Reader<T3> reader3;
        private static readonly Reader<T4> reader4;
        private static readonly Reader<T5> reader5;
        private static readonly Reader<T6> reader6;
        public DynamicByteReader() { }
        public bool Read(byte[] bytes, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5, out T6 arg6)
        {
            if (bytes != null)
                _buffer = bytes;
            index = 0;
            arg1 = InvokeReader(reader1);
            arg2 = InvokeReader(reader2);
            arg3 = InvokeReader(reader3);
            arg4 = InvokeReader(reader4);
            arg5 = InvokeReader(reader5);
            arg6 = InvokeReader(reader6);
            bool oldfailure = failure;
            failure = false;
            return !oldfailure;
        }
    }
    public sealed class DynamicByteReader<T1, T2, T3, T4, T5, T6, T7> : ByteReader
    {
        static DynamicByteReader()
        {
            reader1 = ReaderHelper<T1>.Reader;
            reader2 = ReaderHelper<T2>.Reader;
            reader3 = ReaderHelper<T3>.Reader;
            reader4 = ReaderHelper<T4>.Reader;
            reader5 = ReaderHelper<T5>.Reader;
            reader6 = ReaderHelper<T6>.Reader;
            reader7 = ReaderHelper<T7>.Reader;
        }
        private static readonly Reader<T1> reader1;
        private static readonly Reader<T2> reader2;
        private static readonly Reader<T3> reader3;
        private static readonly Reader<T4> reader4;
        private static readonly Reader<T5> reader5;
        private static readonly Reader<T6> reader6;
        private static readonly Reader<T7> reader7;
        public DynamicByteReader() { }
        public bool Read(byte[] bytes, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5, out T6 arg6, out T7 arg7)
        {
            if (bytes != null)
                _buffer = bytes;
            index = 0;
            arg1 = InvokeReader(reader1);
            arg2 = InvokeReader(reader2);
            arg3 = InvokeReader(reader3);
            arg4 = InvokeReader(reader4);
            arg5 = InvokeReader(reader5);
            arg6 = InvokeReader(reader6);
            arg7 = InvokeReader(reader7);
            bool oldfailure = failure;
            failure = false;
            return !oldfailure;
        }
    }
    public sealed class DynamicByteReader<T1, T2, T3, T4, T5, T6, T7, T8> : ByteReader
    {
        static DynamicByteReader()
        {
            reader1 = ReaderHelper<T1>.Reader;
            reader2 = ReaderHelper<T2>.Reader;
            reader3 = ReaderHelper<T3>.Reader;
            reader4 = ReaderHelper<T4>.Reader;
            reader5 = ReaderHelper<T5>.Reader;
            reader6 = ReaderHelper<T6>.Reader;
            reader7 = ReaderHelper<T7>.Reader;
            reader8 = ReaderHelper<T8>.Reader;
        }
        private static readonly Reader<T1> reader1;
        private static readonly Reader<T2> reader2;
        private static readonly Reader<T3> reader3;
        private static readonly Reader<T4> reader4;
        private static readonly Reader<T5> reader5;
        private static readonly Reader<T6> reader6;
        private static readonly Reader<T7> reader7;
        private static readonly Reader<T8> reader8;
        public DynamicByteReader() { }
        public bool Read(byte[] bytes, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5, out T6 arg6, out T7 arg7, out T8 arg8)
        {
            if (bytes != null)
                _buffer = bytes;
            index = 0;
            arg1 = InvokeReader(reader1);
            arg2 = InvokeReader(reader2);
            arg3 = InvokeReader(reader3);
            arg4 = InvokeReader(reader4);
            arg5 = InvokeReader(reader5);
            arg6 = InvokeReader(reader6);
            arg7 = InvokeReader(reader7);
            arg8 = InvokeReader(reader8);
            bool oldfailure = failure;
            failure = false;
            return !oldfailure;
        }
    }
    public sealed class DynamicByteReader<T1, T2, T3, T4, T5, T6, T7, T8, T9> : ByteReader
    {
        static DynamicByteReader()
        {
            reader1 = ReaderHelper<T1>.Reader;
            reader2 = ReaderHelper<T2>.Reader;
            reader3 = ReaderHelper<T3>.Reader;
            reader4 = ReaderHelper<T4>.Reader;
            reader5 = ReaderHelper<T5>.Reader;
            reader6 = ReaderHelper<T6>.Reader;
            reader7 = ReaderHelper<T7>.Reader;
            reader8 = ReaderHelper<T8>.Reader;
            reader9 = ReaderHelper<T9>.Reader;
        }
        private static readonly Reader<T1> reader1;
        private static readonly Reader<T2> reader2;
        private static readonly Reader<T3> reader3;
        private static readonly Reader<T4> reader4;
        private static readonly Reader<T5> reader5;
        private static readonly Reader<T6> reader6;
        private static readonly Reader<T7> reader7;
        private static readonly Reader<T8> reader8;
        private static readonly Reader<T9> reader9;
        public DynamicByteReader() { }
        public bool Read(byte[] bytes, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5, out T6 arg6, out T7 arg7, out T8 arg8, out T9 arg9)
        {
            if (bytes != null)
                _buffer = bytes;
            index = 0;
            arg1 = InvokeReader(reader1);
            arg2 = InvokeReader(reader2);
            arg3 = InvokeReader(reader3);
            arg4 = InvokeReader(reader4);
            arg5 = InvokeReader(reader5);
            arg6 = InvokeReader(reader6);
            arg7 = InvokeReader(reader7);
            arg8 = InvokeReader(reader8);
            arg9 = InvokeReader(reader9);
            bool oldfailure = failure;
            failure = false;
            return !oldfailure;
        }
    }
    public sealed class DynamicByteReader<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : ByteReader
    {
        static DynamicByteReader()
        {
            reader1 = ReaderHelper<T1>.Reader;
            reader2 = ReaderHelper<T2>.Reader;
            reader3 = ReaderHelper<T3>.Reader;
            reader4 = ReaderHelper<T4>.Reader;
            reader5 = ReaderHelper<T5>.Reader;
            reader6 = ReaderHelper<T6>.Reader;
            reader7 = ReaderHelper<T7>.Reader;
            reader8 = ReaderHelper<T8>.Reader;
            reader9 = ReaderHelper<T9>.Reader;
            reader10 = ReaderHelper<T10>.Reader;
        }
        private static readonly Reader<T1> reader1;
        private static readonly Reader<T2> reader2;
        private static readonly Reader<T3> reader3;
        private static readonly Reader<T4> reader4;
        private static readonly Reader<T5> reader5;
        private static readonly Reader<T6> reader6;
        private static readonly Reader<T7> reader7;
        private static readonly Reader<T8> reader8;
        private static readonly Reader<T9> reader9;
        private static readonly Reader<T10> reader10;
        public DynamicByteReader() { }
        public bool Read(byte[] bytes, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5, out T6 arg6, out T7 arg7, out T8 arg8, out T9 arg9, out T10 arg10)
        {
            if (bytes != null)
                _buffer = bytes;
            index = 0;
            arg1 = InvokeReader(reader1);
            arg2 = InvokeReader(reader2);
            arg3 = InvokeReader(reader3);
            arg4 = InvokeReader(reader4);
            arg5 = InvokeReader(reader5);
            arg6 = InvokeReader(reader6);
            arg7 = InvokeReader(reader7);
            arg8 = InvokeReader(reader8);
            arg9 = InvokeReader(reader9);
            arg10 = InvokeReader(reader10);
            bool oldfailure = failure;
            failure = false;
            return !oldfailure;
        }
    }
    public sealed class InvalidDynamicTypeException : Exception
    {
        public InvalidDynamicTypeException() { }
        public InvalidDynamicTypeException(Type arg, int typeNumber, bool reader) :
            base("Generic argument " + arg.Name + ": T" + typeNumber + " is not able to be " + (reader ? "read" : "written") + " automatically.") { }
    }
}

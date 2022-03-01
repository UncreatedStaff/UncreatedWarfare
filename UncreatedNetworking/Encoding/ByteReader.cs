using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public void LoadNew(byte[] bytes)
        {
            this._buffer = bytes;
            this.index = 0;
            this.failure = false;
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
        public byte[] ReadUInt8Array() => ReadBytes();
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
        public int ReadInt32()
        {
            if (index + sizeof(int) > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadInt32) + " at index " + index.ToString());
                return default;
            }
            int rtn = BitConverter.ToInt32(_buffer, index);
            index += sizeof(int);
            return rtn;
        }
        public uint ReadUInt32()
        {
            if (index + sizeof(uint) > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadUInt32) + " at index " + index.ToString());
                return default;
            }
            uint rtn = BitConverter.ToUInt32(_buffer, index);
            index += sizeof(uint);
            return rtn;
        }
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
        public long ReadInt64()
        {
            if (index + sizeof(long) > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadInt64) + " at index " + index.ToString());
                return default;
            }
            long rtn = BitConverter.ToInt64(_buffer, index);
            index += sizeof(long);
            return rtn;
        }
        public ulong ReadUInt64()
        {
            if (index + sizeof(ulong) > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadUInt64) + " at index " + index.ToString());
                return default;
            }
            ulong rtn = BitConverter.ToUInt64(_buffer, index);
            index += sizeof(ulong);
            return rtn;
        }
        public short ReadInt16()
        {
            if (index + sizeof(short) > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadInt16) + " at index " + index.ToString());
                return default;
            }
            short rtn = BitConverter.ToInt16(_buffer, index);
            index += sizeof(short);
            return rtn;
        }
        public ushort ReadUInt16()
        {
            if (index + sizeof(ushort) > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadUInt16) + " at index " + index.ToString());
                return default;
            }
            ushort rtn = BitConverter.ToUInt16(_buffer, index);
            index += sizeof(ushort);
            return rtn;
        }
        public float ReadFloat()
        {
            if (index + sizeof(float) > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadFloat) + " at index " + index.ToString());
                return default;
            }
            float rtn = BitConverter.ToSingle(_buffer, index);
            index += sizeof(float);
            return rtn;
        }
        public unsafe decimal ReadDecimal()
        {
            if (index + sizeof(decimal) > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadDecimal) + " at index " + index.ToString());
                return default;
            }

            decimal rtn;
            fixed (byte* ptr = _buffer)
            {
                rtn = *(decimal*)(ptr + index);
            }

            index += sizeof(decimal);
            return rtn;
        }
        public double ReadDouble()
        {
            if (index + sizeof(double) > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadDouble) + " at index " + index.ToString());
                return default;
            }
            double rtn = BitConverter.ToDouble(_buffer, index);
            index += sizeof(double);
            return rtn;
        }
        public char ReadChar()
        {
            byte length = ReadUInt8();
            if (length == 0) return '\0';
            if (index + length > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadChar) + " at index " + index.ToString());
                return default;
            }
            char[] chars = System.Text.Encoding.UTF8.GetChars(_buffer, index, length);
            index += length;
            if (chars.Length == 0) return default;
            else return chars[0];
        }
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
        public DateTime ReadDateTime() => new DateTime(ReadInt64());
        public TimeSpan ReadTimeSpan() => new TimeSpan(ReadInt64());
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
        public TEnum ReadEnum<TEnum>() where TEnum : struct, Enum
        {
            Type e = typeof(TEnum);
            if (!e.IsEnum)
            {
                failure |= true;
                Logging.LogWarning($"Tried to read {e.Name} as an enum.");
                return default;
            }
            Type underlying = Enum.GetUnderlyingType(e);
            if (underlying.IsPrimitive)
            {
                if (underlying == typeof(int))
                    return (TEnum)Enum.ToObject(e, ReadInt32());
                else if (underlying == typeof(byte))
                    return (TEnum)Enum.ToObject(e, ReadUInt8());
                else if (underlying == typeof(ulong))
                    return (TEnum)Enum.ToObject(e, ReadUInt64());
                else if (underlying == typeof(long))
                    return (TEnum)Enum.ToObject(e, ReadInt64());
                else if (underlying == typeof(ushort))
                    return (TEnum)Enum.ToObject(e, ReadUInt16());
                else if (underlying == typeof(short))
                    return (TEnum)Enum.ToObject(e, ReadInt16());
                else if (underlying == typeof(uint))
                    return (TEnum)Enum.ToObject(e, ReadUInt32());
                else if (underlying == typeof(sbyte))
                    return (TEnum)Enum.ToObject(e, ReadInt8());
                else
                {
                    failure |= true;
                    Logging.LogWarning($"Tried to write {e.Name} as an enum, but didn't have an integral underlying type ({underlying.Name}).");
                    return default;
                }
            }
            else
            {
                failure |= true;
                Logging.LogWarning($"Tried to write {e.Name} as an enum, but didn't have a proper underlying type ({underlying.Name}).");
                return default;
            }
        }
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
        public unsafe bool[] ReadBoolArray()
        {
            if (index + sizeof(ushort) > _buffer.Length)
            {
                failure |= true;
                Logging.LogWarning("Failed to run " + nameof(ReadBoolArray) + " at index " + index.ToString());
                return default;
            }

            fixed (byte* ptr = _buffer)
            {
                ushort len = *(ushort*)(ptr + index);
                index += sizeof(ushort);
                if (index + len > _buffer.Length)
                {
                    failure |= true;
                    Logging.LogWarning("Failed to run " + nameof(ReadBoolArray) + " at index " + index.ToString());
                    return default;
                }
                bool[] rtn = new bool[len];
                int offset = index - 1 + sizeof(ushort);
                for (int i = 0; i < len; i++)
                {
                    byte pos = (byte)(i % 8);
                    if (pos == 0) offset++;
                    rtn[i] = (byte)((*(ptr + offset) << 8 - pos) & 1) != 0;
                }
                index += offset;
                return rtn;
            }
        }
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
        public string[] ReadStringArray()
        {
            string[] rtn = new string[ReadUInt16()];
            for (int i = 0; i < rtn.Length; i++)
                rtn[i] = ReadString();
            return rtn;
        }
        public T InvokeReader<T>(Reader<T> reader) => reader.Invoke(this);
        public static Reader<T1> GetReader<T1>()
        {
            Type type = typeof(T1);
            if (type.IsPrimitive)
            {
                if (type == typeof(ulong))
                    return (r) => (T1)(object)r.ReadUInt64();
                else if (type == typeof(float))
                    return (r) => (T1)(object)r.ReadFloat();
                else if (type == typeof(long))
                    return (r) => (T1)(object)r.ReadInt64();
                else if (type == typeof(ushort))
                    return (r) => (T1)(object)r.ReadUInt16();
                else if (type == typeof(short))
                    return (r) => (T1)(object)r.ReadInt16();
                else if (type == typeof(byte))
                    return (r) => (T1)(object)r.ReadUInt8();
                else if (type == typeof(int))
                    return (r) => (T1)(object)r.ReadInt32();
                else if (type == typeof(uint))
                    return (r) => (T1)(object)r.ReadUInt32();
                else if (type == typeof(bool))
                    return (r) => (T1)(object)r.ReadBool();
                else if (type == typeof(sbyte))
                    return (r) => (T1)(object)r.ReadInt8();
                else if (type == typeof(decimal))
                    return (r) => (T1)(object)r.ReadDecimal();
                else if (type == typeof(double))
                    return (r) => (T1)(object)r.ReadDouble();
                else throw new ArgumentException($"Can not convert that type ({type.Name})!", nameof(type));
            }
            else if (type == typeof(string))
            {
                return (r) => (T1)(object)r.ReadString();
            }
            else if (type.IsEnum)
            {
                Type underlying = Enum.GetUnderlyingType(type);
                if (!underlying.IsEnum)
                {
                    if (underlying.IsPrimitive)
                    {
                        if (underlying == typeof(int))
                            return (r) => (T1)Enum.ToObject(type, r.ReadInt32());
                        else if (underlying == typeof(byte))
                            return (r) => (T1)Enum.ToObject(type, r.ReadUInt8());
                        else if (underlying == typeof(ulong))
                            return (r) => (T1)Enum.ToObject(type, r.ReadUInt64());
                        else if (underlying == typeof(long))
                            return (r) => (T1)Enum.ToObject(type, r.ReadInt64());
                        else if (underlying == typeof(ushort))
                            return (r) => (T1)Enum.ToObject(type, r.ReadUInt16());
                        else if (underlying == typeof(short))
                            return (r) => (T1)Enum.ToObject(type, r.ReadInt16());
                        else if (underlying == typeof(uint))
                            return (r) => (T1)Enum.ToObject(type, r.ReadUInt32());
                        else if (underlying == typeof(sbyte))
                            return (r) => (T1)Enum.ToObject(type, r.ReadInt8());
                        else throw new ArgumentException($"Can not convert that enum type ({type.Name}.{underlying.Name}), underlying is not an integral type!", nameof(type));
                    }
                    else throw new ArgumentException($"Can not convert that enum type ({type.Name}.{underlying.Name}), underlying is not primitive!", nameof(type));
                }
                else throw new ArgumentException($"Can not convert that enum type ({type.Name}.{underlying.Name}), type is not an Enum!", nameof(type));
            }
            else if (type == typeof(DateTime))
            {
                return (r) => (T1)(object)r.ReadDateTime();
            }
            else if (type == typeof(TimeSpan))
            {
                return (r) => (T1)(object)r.ReadTimeSpan();
            }
            else if (type == typeof(Guid))
            {
                return (r) => (T1)(object)r.ReadGUID();
            }
            else if (type.IsArray)
            {
                Type arrayType = type.GetElementType();
                if (arrayType == typeof(ulong))
                    return (r) => (T1)(object)r.ReadUInt64Array();
                else if (arrayType == typeof(float))
                    return (r) => (T1)(object)r.ReadFloatArray();
                else if (arrayType == typeof(long))
                    return (r) => (T1)(object)r.ReadInt64Array();
                else if (arrayType == typeof(ushort))
                    return (r) => (T1)(object)r.ReadUInt16Array();
                else if (arrayType == typeof(short))
                    return (r) => (T1)(object)r.ReadInt16Array();
                else if (arrayType == typeof(byte))
                    return (r) => (T1)(object)r.ReadUInt8Array();
                else if (arrayType == typeof(int))
                    return (r) => (T1)(object)r.ReadInt32Array();
                else if (arrayType == typeof(uint))
                    return (r) => (T1)(object)r.ReadUInt32Array();
                else if (arrayType == typeof(bool))
                    return (r) => (T1)(object)r.ReadBoolArray();
                else if (arrayType == typeof(sbyte))
                    return (r) => (T1)(object)r.ReadInt8Array();
                else if (arrayType == typeof(decimal))
                    return (r) => (T1)(object)r.ReadDecimalArray();
                else if (arrayType == typeof(double))
                    return (r) => (T1)(object)r.ReadDoubleArray();
                else if (arrayType == typeof(string))
                    return (r) => (T1)(object)r.ReadStringArray();
                else throw new ArgumentException($"Can not convert that array type ({type.Name})!", nameof(type));
            }
            else throw new ArgumentException($"Can not convert that type ({type.Name})!", nameof(type));
        }
        public static bool TryGetReader(Type type, out Reader<object> reader)
        {
            if (type.IsPrimitive)
            {
                if (type == typeof(ulong))
                {
                    reader = (r) => r.ReadUInt64();
                    return true;
                }
                else if (type == typeof(float))
                {
                    reader = (r) => r.ReadFloat();
                    return true;
                }
                else if (type == typeof(long))
                {
                    reader = (r) => r.ReadInt64();
                    return true;
                }
                else if (type == typeof(ushort))
                {
                    reader = (r) => r.ReadUInt16();
                    return true;
                }
                else if (type == typeof(short))
                {
                    reader = (r) => r.ReadInt16();
                    return true;
                }
                else if (type == typeof(byte))
                {
                    reader = (r) => r.ReadUInt8();
                    return true;
                }
                else if (type == typeof(int))
                {
                    reader = (r) => r.ReadInt32();
                    return true;
                }
                else if (type == typeof(uint))
                {
                    reader = (r) => r.ReadUInt32();
                    return true;
                }
                else if (type == typeof(bool))
                {
                    reader = (r) => r.ReadBool();
                    return true;
                }
                else if (type == typeof(sbyte))
                {
                    reader = (r) => r.ReadInt8();
                    return true;
                }
                else if (type == typeof(decimal))
                {
                    reader = (r) => r.ReadDecimal();
                    return true;
                }
                else if (type == typeof(double))
                {
                    reader = (r) => r.ReadDouble();
                    return true;
                }
                else
                {
                    reader = null;
                    return false;
                }
            }
            else if (type == typeof(string))
            {
                reader = (r) => r.ReadString();
                return false;
            }
            else if (type.IsEnum)
            {
                Type underlying = Enum.GetUnderlyingType(type);
                if (underlying.IsPrimitive)
                {
                    if (underlying == typeof(int))
                    {
                        reader = (r) => Enum.ToObject(type, r.ReadInt32());
                        return true;
                    }
                    else if (underlying == typeof(byte))
                    {
                        reader = (r) => Enum.ToObject(type, r.ReadUInt8());
                        return true;
                    }
                    else if (underlying == typeof(ulong))
                    {
                        reader = (r) => Enum.ToObject(type, r.ReadUInt64());
                        return true;
                    }
                    else if (underlying == typeof(long))
                    {
                        reader = (r) => Enum.ToObject(type, r.ReadInt64());
                        return true;
                    }
                    else if (underlying == typeof(ushort))
                    {
                        reader = (r) => Enum.ToObject(type, r.ReadUInt16());
                        return true;
                    }
                    else if (underlying == typeof(short))
                    {
                        reader = (r) => Enum.ToObject(type, r.ReadInt16());
                        return true;
                    }
                    else if (underlying == typeof(uint))
                    {
                        reader = (r) => Enum.ToObject(type, r.ReadUInt32());
                        return true;
                    }
                    else if (underlying == typeof(sbyte))
                    {
                        reader = (r) => Enum.ToObject(type, r.ReadInt8());
                        return true;
                    }
                    else
                    {
                        reader = null;
                        return false;
                    }
                }
                else
                {
                    reader = null;
                    return false;
                }
            }
            else if (type == typeof(DateTime))
            {
                reader = (r) => r.ReadDateTime();
                return true;
            }
            else if (type == typeof(TimeSpan))
            {
                reader = (r) => r.ReadTimeSpan();
                return true;
            }
            else if (type == typeof(Guid))
            {
                reader = (r) => r.ReadGUID();
                return true;
            }
            else if (type.IsArray)
            {
                Type arrayType = type.GetElementType();
                if (arrayType == typeof(ulong))
                {
                    reader = (r) => r.ReadUInt64Array();
                    return true;
                }
                else if (arrayType == typeof(float))
                {
                    reader = (r) => r.ReadFloatArray();
                    return true;
                }
                else if (arrayType == typeof(long))
                {
                    reader = (r) => r.ReadInt64Array();
                    return true;
                }
                else if (arrayType == typeof(ushort))
                {
                    reader = (r) => r.ReadUInt16Array();
                    return true;
                }
                else if (arrayType == typeof(short))
                {
                    reader = (r) => r.ReadInt16Array();
                    return true;
                }
                else if (arrayType == typeof(byte))
                {
                    reader = (r) => r.ReadUInt8Array();
                    return true;
                }
                else if (arrayType == typeof(int))
                {
                    reader = (r) => r.ReadInt32Array();
                    return true;
                }
                else if (arrayType == typeof(uint))
                {
                    reader = (r) => r.ReadUInt32Array();
                    return true;
                }
                else if (arrayType == typeof(bool))
                {
                    reader = (r) => r.ReadBoolArray();
                    return true;
                }
                else if (arrayType == typeof(sbyte))
                {
                    reader = (r) => r.ReadInt8Array();
                    return true;
                }
                else if (arrayType == typeof(decimal))
                {
                    reader = (r) => r.ReadDecimalArray();
                    return true;
                }
                else if (arrayType == typeof(double))
                {
                    reader = (r) => r.ReadDoubleArray();
                    return true;
                }
                else if (arrayType == typeof(string))
                {
                    reader = (r) => r.ReadStringArray();
                    return true;
                }
                else
                {
                    reader = null;
                    return false;
                }
            }
            else
            {
                reader = null;
                return false;
            }
        }
        public static bool TryGetReader<T1>(out Reader<T1> reader)
        {
            Type type = typeof(T1);
            if (type.IsPrimitive)
            {
                if (type == typeof(ulong))
                {
                    reader = (r) => (T1)(object)r.ReadUInt64();
                    return true;
                }
                else if (type == typeof(float))
                {
                    reader = (r) => (T1)(object)r.ReadFloat();
                    return true;
                }
                else if (type == typeof(long))
                {
                    reader = (r) => (T1)(object)r.ReadInt64();
                    return true;
                }
                else if (type == typeof(ushort))
                {
                    reader = (r) => (T1)(object)r.ReadUInt16();
                    return true;
                }
                else if (type == typeof(short))
                {
                    reader = (r) => (T1)(object)r.ReadInt16();
                    return true;
                }
                else if (type == typeof(byte))
                {
                    reader = (r) => (T1)(object)r.ReadUInt8();
                    return true;
                }
                else if (type == typeof(int))
                {
                    reader = (r) => (T1)(object)r.ReadInt32();
                    return true;
                }
                else if (type == typeof(uint))
                {
                    reader = (r) => (T1)(object)r.ReadUInt32();
                    return true;
                }
                else if (type == typeof(bool))
                {
                    reader = (r) => (T1)(object)r.ReadBool();
                    return true;
                }
                else if (type == typeof(sbyte))
                {
                    reader = (r) => (T1)(object)r.ReadInt8();
                    return true;
                }
                else if (type == typeof(decimal))
                {
                    reader = (r) => (T1)(object)r.ReadDecimal();
                    return true;
                }
                else if (type == typeof(double))
                {
                    reader = (r) => (T1)(object)r.ReadDouble();
                    return true;
                }
                else
                {
                    reader = null;
                    return false;
                }
            }
            else if (type == typeof(string))
            {
                reader = (r) => (T1)(object)r.ReadString();
                return false;
            }
            else if (type.IsEnum)
            {
                Type underlying = Enum.GetUnderlyingType(type);
                if (underlying.IsPrimitive)
                {
                    if (underlying == typeof(int))
                    {
                        reader = (r) => (T1)Enum.ToObject(type, r.ReadInt32());
                        return true;
                    }
                    else if (underlying == typeof(byte))
                    {
                        reader = (r) => (T1)Enum.ToObject(type, r.ReadUInt8());
                        return true;
                    }
                    else if (underlying == typeof(ulong))
                    {
                        reader = (r) => (T1)Enum.ToObject(type, r.ReadUInt64());
                        return true;
                    }
                    else if (underlying == typeof(long))
                    {
                        reader = (r) => (T1)Enum.ToObject(type, r.ReadInt64());
                        return true;
                    }
                    else if (underlying == typeof(ushort))
                    {
                        reader = (r) => (T1)Enum.ToObject(type, r.ReadUInt16());
                        return true;
                    }
                    else if (underlying == typeof(short))
                    {
                        reader = (r) => (T1)Enum.ToObject(type, r.ReadInt16());
                        return true;
                    }
                    else if (underlying == typeof(uint))
                    {
                        reader = (r) => (T1)Enum.ToObject(type, r.ReadUInt32());
                        return true;
                    }
                    else if (underlying == typeof(sbyte))
                    {
                        reader = (r) => (T1)Enum.ToObject(type, r.ReadInt8());
                        return true;
                    }
                    else
                    {
                        reader = null;
                        return false;
                    }
                }
                else
                {
                    reader = null;
                    return false;
                }
            }
            else if (type == typeof(DateTime))
            {
                reader = (r) => (T1)(object)r.ReadDateTime();
                return true;
            }
            else if (type == typeof(TimeSpan))
            {
                reader = (r) => (T1)(object)r.ReadTimeSpan();
                return true;
            }
            else if (type == typeof(Guid))
            {
                reader = (r) => (T1)(object)r.ReadGUID();
                return true;
            }
            else if (type.IsArray)
            {
                Type arrayType = type.GetElementType();
                if (arrayType == typeof(ulong))
                {
                    reader = (r) => (T1)(object)r.ReadUInt64Array();
                    return true;
                }
                else if (arrayType == typeof(float))
                {
                    reader = (r) => (T1)(object)r.ReadFloatArray();
                    return true;
                }
                else if (arrayType == typeof(long))
                {
                    reader = (r) => (T1)(object)r.ReadInt64Array();
                    return true;
                }
                else if (arrayType == typeof(ushort))
                {
                    reader = (r) => (T1)(object)r.ReadUInt16Array();
                    return true;
                }
                else if (arrayType == typeof(short))
                {
                    reader = (r) => (T1)(object)r.ReadInt16Array();
                    return true;
                }
                else if (arrayType == typeof(byte))
                {
                    reader = (r) => (T1)(object)r.ReadUInt8Array();
                    return true;
                }
                else if (arrayType == typeof(int))
                {
                    reader = (r) => (T1)(object)r.ReadInt32Array();
                    return true;
                }
                else if (arrayType == typeof(uint))
                {
                    reader = (r) => (T1)(object)r.ReadUInt32Array();
                    return true;
                }
                else if (arrayType == typeof(bool))
                {
                    reader = (r) => (T1)(object)r.ReadBoolArray();
                    return true;
                }
                else if (arrayType == typeof(sbyte))
                {
                    reader = (r) => (T1)(object)r.ReadInt8Array();
                    return true;
                }
                else if (arrayType == typeof(decimal))
                {
                    reader = (r) => (T1)(object)r.ReadDecimalArray();
                    return true;
                }
                else if (arrayType == typeof(double))
                {
                    reader = (r) => (T1)(object)r.ReadDoubleArray();
                    return true;
                }
                else if (arrayType == typeof(string))
                {
                    reader = (r) => (T1)(object)r.ReadStringArray();
                    return true;
                }
                else
                {
                    reader = null;
                    return false;
                }
            }
            else
            {
                reader = null;
                return false;
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
            this.reader = reader ?? (TryGetReader(out Reader<T> r) ? r : null);
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
            this.reader1 = reader1 ?? (TryGetReader(out Reader<T1> r1) ? r1 : null);
            this.reader2 = reader2 ?? (TryGetReader(out Reader<T2> r2) ? r2 : null);
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
            this.reader1 = reader1 ?? (TryGetReader(out Reader<T1> r1) ? r1 : null);
            this.reader2 = reader2 ?? (TryGetReader(out Reader<T2> r2) ? r2 : null);
            this.reader3 = reader3 ?? (TryGetReader(out Reader<T3> r3) ? r3 : null);
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
            this.reader1 = reader1 ?? (TryGetReader(out Reader<T1> r1) ? r1 : null);
            this.reader2 = reader2 ?? (TryGetReader(out Reader<T2> r2) ? r2 : null);
            this.reader3 = reader3 ?? (TryGetReader(out Reader<T3> r3) ? r3 : null);
            this.reader4 = reader4 ?? (TryGetReader(out Reader<T4> r4) ? r4 : null);
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
        private readonly Reader<T1> reader1;
        public DynamicByteReader()
        {
            reader1 = GetReader<T1>();
        }
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
        private readonly Reader<T1> reader1;
        private readonly Reader<T2> reader2;
        public DynamicByteReader()
        {
            reader1 = GetReader<T1>();
            reader2 = GetReader<T2>();
        }
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
        private readonly Reader<T1> reader1;
        private readonly Reader<T2> reader2;
        private readonly Reader<T3> reader3;
        public DynamicByteReader()
        {
            reader1 = GetReader<T1>();
            reader2 = GetReader<T2>();
            reader3 = GetReader<T3>();
        }
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
        private readonly Reader<T1> reader1;
        private readonly Reader<T2> reader2;
        private readonly Reader<T3> reader3;
        private readonly Reader<T4> reader4;
        public DynamicByteReader()
        {
            reader1 = GetReader<T1>();
            reader2 = GetReader<T2>();
            reader3 = GetReader<T3>();
            reader4 = GetReader<T4>();
        }
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
        private readonly Reader<T1> reader1;
        private readonly Reader<T2> reader2;
        private readonly Reader<T3> reader3;
        private readonly Reader<T4> reader4;
        private readonly Reader<T5> reader5;
        public DynamicByteReader()
        {
            reader1 = GetReader<T1>();
            reader2 = GetReader<T2>();
            reader3 = GetReader<T3>();
            reader4 = GetReader<T4>();
            reader5 = GetReader<T5>();
        }
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
        private readonly Reader<T1> reader1;
        private readonly Reader<T2> reader2;
        private readonly Reader<T3> reader3;
        private readonly Reader<T4> reader4;
        private readonly Reader<T5> reader5;
        private readonly Reader<T6> reader6;
        public DynamicByteReader()
        {
            reader1 = GetReader<T1>();
            reader2 = GetReader<T2>();
            reader3 = GetReader<T3>();
            reader4 = GetReader<T4>();
            reader5 = GetReader<T5>();
            reader6 = GetReader<T6>();
        }
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
        private readonly Reader<T1> reader1;
        private readonly Reader<T2> reader2;
        private readonly Reader<T3> reader3;
        private readonly Reader<T4> reader4;
        private readonly Reader<T5> reader5;
        private readonly Reader<T6> reader6;
        private readonly Reader<T7> reader7;
        public DynamicByteReader()
        {
            reader1 = GetReader<T1>();
            reader2 = GetReader<T2>();
            reader3 = GetReader<T3>();
            reader4 = GetReader<T4>();
            reader5 = GetReader<T5>();
            reader6 = GetReader<T6>();
            reader7 = GetReader<T7>();
        }
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
        private readonly Reader<T1> reader1;
        private readonly Reader<T2> reader2;
        private readonly Reader<T3> reader3;
        private readonly Reader<T4> reader4;
        private readonly Reader<T5> reader5;
        private readonly Reader<T6> reader6;
        private readonly Reader<T7> reader7;
        private readonly Reader<T8> reader8;
        public DynamicByteReader()
        {
            reader1 = GetReader<T1>();
            reader2 = GetReader<T2>();
            reader3 = GetReader<T3>();
            reader4 = GetReader<T4>();
            reader5 = GetReader<T5>();
            reader6 = GetReader<T6>();
            reader7 = GetReader<T7>();
            reader8 = GetReader<T8>();
        }
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
        private readonly Reader<T1> reader1;
        private readonly Reader<T2> reader2;
        private readonly Reader<T3> reader3;
        private readonly Reader<T4> reader4;
        private readonly Reader<T5> reader5;
        private readonly Reader<T6> reader6;
        private readonly Reader<T7> reader7;
        private readonly Reader<T8> reader8;
        private readonly Reader<T9> reader9;
        public DynamicByteReader()
        {
            reader1 = GetReader<T1>();
            reader2 = GetReader<T2>();
            reader3 = GetReader<T3>();
            reader4 = GetReader<T4>();
            reader5 = GetReader<T5>();
            reader6 = GetReader<T6>();
            reader7 = GetReader<T7>();
            reader8 = GetReader<T8>();
            reader9 = GetReader<T9>();
        }
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
        private readonly Reader<T1> reader1;
        private readonly Reader<T2> reader2;
        private readonly Reader<T3> reader3;
        private readonly Reader<T4> reader4;
        private readonly Reader<T5> reader5;
        private readonly Reader<T6> reader6;
        private readonly Reader<T7> reader7;
        private readonly Reader<T8> reader8;
        private readonly Reader<T9> reader9;
        private readonly Reader<T10> reader10;
        public DynamicByteReader()
        {
            reader1 = GetReader<T1>();
            reader2 = GetReader<T2>();
            reader3 = GetReader<T3>();
            reader4 = GetReader<T4>();
            reader5 = GetReader<T5>();
            reader6 = GetReader<T6>();
            reader7 = GetReader<T7>();
            reader8 = GetReader<T8>();
            reader9 = GetReader<T9>();
            reader10 = GetReader<T10>();
        }
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
}

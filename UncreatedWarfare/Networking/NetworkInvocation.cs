using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Networking.Invocations
{
    public class NetworkInvocationRaw<T>
    {
        private ECall call;
        private Action<byte[]> send;
        private Func<byte[], T> read_func;
        private Func<T, byte[]> write_func;
        public NetworkInvocationRaw(ECall call, Action<byte[]> send, Func<byte[], T> read, Func<T, byte[]> write)
        {
            this.call = call;
            this.send = send;
            this.read_func = read;
            this.write_func = write;
        }
        public void Invoke(T arg) => send?.Invoke(write_func.Invoke(arg).Callify(call));
        public bool Read(byte[] bytes, out T output)
        {
            try
            {
                output = read_func.Invoke(bytes);
                return true;
            }
            catch
            {
                output = default;
                return false;
            }
        }
    }
    public class NetworkInvocation
    {
        private readonly ECall call;
        private readonly Action<byte[]> send;

        public NetworkInvocation(ECall call, Action<byte[]> send)
        {
            this.call = call;
            this.send = send;
        }
        public void Invoke()
        {
            send?.Invoke(ByteMath.Callify(call));
        }
    }
    public class NetworkInvocation<T1>
    {
        private ECall call;
        private Action<byte[]> send;
        private readonly Func<object, byte[]> writer1;
        public NetworkInvocation(ECall call, Action<byte[]> send)
        {
            this.call = call;
            this.send = send;
            this.writer1 = ByteMath.GetByteFunction<T1>();
        }
        public void Invoke(T1 arg1)
        {
            send?.Invoke(writer1.Invoke(arg1).Callify(call));
        }
        public bool Read(byte[] bytes, out T1 arg1)
        {
            try
            {
                arg1 = ByteMath.ReadBytes<T1>(bytes, 0, out _);
                return true;
            }
            catch
            {
                arg1 = default;
                return false;
            }
        }
    }
    public class NetworkInvocation<T1, T2>
    {
        private ECall call;
        private Action<byte[]> send;
        private readonly Func<object, byte[]> writer1;
        private readonly Func<object, byte[]> writer2;
        private delegate T1 _reader1(byte[] bytes, int index, out int size);
        private delegate T2 _reader2(byte[] bytes, int index, out int size);
        private readonly _reader1 reader1;
        private readonly _reader2 reader2;
        public NetworkInvocation(ECall call, Action<byte[]> send)
        {
            this.call = call;
            this.send = send;
            this.writer1 = ByteMath.GetByteFunction<T1>();
            this.writer2 = ByteMath.GetByteFunction<T2>();
            this.reader1 = ByteMath.GetReadFunction<T1, _reader1>();
        }
        public void Invoke(T1 arg1, T2 arg2)
        {
            byte[] b1 = writer1.Invoke(arg1);
            byte[] b2 = writer2.Invoke(arg2);
            byte[] rtn = new byte[b1.Length + b2.Length];
            Array.Copy(b1, 0, rtn, 0, b1.Length);
            Array.Copy(b2, 0, rtn, b1.Length, b2.Length);
            send?.Invoke(rtn.Callify(call));
        }
        public bool Read(byte[] bytes, out T1 arg1, out T2 arg2)
        {
            try
            {
                int size = 0;
                arg1 = ByteMath.ReadBytes<T1>(bytes, 0, out size);
                arg2 = ByteMath.ReadBytes<T2>(bytes, size, out _);
                return true;
            }
            catch
            {
                arg1 = default;
                arg2 = default;
                return false;
            }
        }
    }
    public class NetworkInvocation<T1, T2, T3>
    {
        private ECall call;
        private Action<byte[]> send;
        private readonly Func<object, byte[]> writer1;
        private readonly Func<object, byte[]> writer2;
        private readonly Func<object, byte[]> writer3;
        public NetworkInvocation(ECall call, Action<byte[]> send)
        {
            this.call = call;
            this.send = send;
            this.writer1 = ByteMath.GetByteFunction<T1>();
            this.writer2 = ByteMath.GetByteFunction<T2>();
            this.writer3 = ByteMath.GetByteFunction<T3>();
        }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3)
        {
            byte[] b1 = writer1.Invoke(arg1);
            byte[] b2 = writer2.Invoke(arg2);
            byte[] b3 = writer3.Invoke(arg3);
            byte[] rtn = new byte[b1.Length + b2.Length + b3.Length];
            Array.Copy(b1, 0, rtn, 0, b1.Length);
            Array.Copy(b2, 0, rtn, b1.Length, b2.Length);
            Array.Copy(b3, 0, rtn, b1.Length + b2.Length, b3.Length);
            send?.Invoke(rtn.Callify(call));
        }
        public bool Read(byte[] bytes, out T1 arg1, out T2 arg2, out T3 arg3)
        {
            try
            {
                int i = 0, size = 0;
                arg1 = ByteMath.ReadBytes<T1>(bytes, 0, out size);
                i += size;
                arg2 = ByteMath.ReadBytes<T2>(bytes, i, out size);
                i += size;
                arg3 = ByteMath.ReadBytes<T3>(bytes, i, out _);
                return true;
            }
            catch
            {
                arg1 = default;
                arg2 = default;
                arg3 = default;
                return false;
            }
        }
    }
    public class NetworkInvocation<T1, T2, T3, T4>
    {
        private ECall call;
        private Action<byte[]> send;
        private readonly Func<object, byte[]> writer1;
        private readonly Func<object, byte[]> writer2;
        private readonly Func<object, byte[]> writer3;
        private readonly Func<object, byte[]> writer4;
        public NetworkInvocation(ECall call, Action<byte[]> send)
        {
            this.call = call;
            this.send = send;
            this.writer1 = ByteMath.GetByteFunction<T1>();
            this.writer2 = ByteMath.GetByteFunction<T2>();
            this.writer3 = ByteMath.GetByteFunction<T3>();
            this.writer4 = ByteMath.GetByteFunction<T4>();
        }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            byte[] b1 = writer1.Invoke(arg1);
            byte[] b2 = writer2.Invoke(arg2);
            byte[] b3 = writer3.Invoke(arg3);
            byte[] b4 = writer4.Invoke(arg4);
            byte[] rtn = new byte[b1.Length + b2.Length + b3.Length + b4.Length];
            int i = 0;
            Array.Copy(b1, 0, rtn, 0, b1.Length);
            i += b1.Length;
            Array.Copy(b2, 0, rtn, i, b2.Length);
            i += b2.Length;
            Array.Copy(b3, 0, rtn, i, b3.Length);
            i += b3.Length;
            Array.Copy(b4, 0, rtn, i, b4.Length);
            send?.Invoke(rtn.Callify(call));
        }
        public bool Read(byte[] bytes, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4)
        {
            try
            {
                int i = 0, size = 0;
                arg1 = ByteMath.ReadBytes<T1>(bytes, 0, out size);
                i += size;
                arg2 = ByteMath.ReadBytes<T2>(bytes, i, out size);
                i += size;
                arg3 = ByteMath.ReadBytes<T3>(bytes, i, out size);
                i += size;
                arg4 = ByteMath.ReadBytes<T4>(bytes, i, out _);
                return true;
            }
            catch
            {
                arg1 = default;
                arg2 = default;
                arg3 = default;
                arg4 = default;
                return false;
            }
        }
    }
    public class NetworkInvocation<T1, T2, T3, T4, T5>
    {
        private ECall call;
        private Action<byte[]> send;
        private readonly Func<object, byte[]> writer1;
        private readonly Func<object, byte[]> writer2;
        private readonly Func<object, byte[]> writer3;
        private readonly Func<object, byte[]> writer4;
        private readonly Func<object, byte[]> writer5;
        public NetworkInvocation(ECall call, Action<byte[]> send)
        {
            this.call = call;
            this.send = send;
            this.writer1 = ByteMath.GetByteFunction<T1>();
            this.writer2 = ByteMath.GetByteFunction<T2>();
            this.writer3 = ByteMath.GetByteFunction<T3>();
            this.writer4 = ByteMath.GetByteFunction<T4>();
            this.writer5 = ByteMath.GetByteFunction<T5>();
        }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            byte[] b1 = writer1.Invoke(arg1);
            byte[] b2 = writer2.Invoke(arg2);
            byte[] b3 = writer3.Invoke(arg3);
            byte[] b4 = writer4.Invoke(arg4);
            byte[] b5 = writer5.Invoke(arg5);
            byte[] rtn = new byte[b1.Length + b2.Length + b3.Length + b4.Length + b5.Length];
            int i = 0;
            Array.Copy(b1, 0, rtn, 0, b1.Length);
            i += b1.Length;
            Array.Copy(b2, 0, rtn, i, b2.Length);
            i += b2.Length;
            Array.Copy(b3, 0, rtn, i, b3.Length);
            i += b3.Length;
            Array.Copy(b4, 0, rtn, i, b4.Length);
            i += b4.Length;
            Array.Copy(b5, 0, rtn, i, b5.Length);
            send?.Invoke(rtn.Callify(call));
        }
        public bool Read(byte[] bytes, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5)
        {
            try
            {
                int i = 0, size = 0;
                arg1 = ByteMath.ReadBytes<T1>(bytes, 0, out size);
                i += size;
                arg2 = ByteMath.ReadBytes<T2>(bytes, i, out size);
                i += size;
                arg3 = ByteMath.ReadBytes<T3>(bytes, i, out size);
                i += size;
                arg4 = ByteMath.ReadBytes<T4>(bytes, i, out size);
                i += size;
                arg5 = ByteMath.ReadBytes<T5>(bytes, i, out _);
                return true;
            }
            catch
            {
                arg1 = default;
                arg2 = default;
                arg3 = default;
                arg4 = default;
                arg5 = default;
                return false;
            }
        }
    }
    public class NetworkInvocation<T1, T2, T3, T4, T5, T6>
    {
        private ECall call;
        private Action<byte[]> send;
        private readonly Func<object, byte[]> writer1;
        private readonly Func<object, byte[]> writer2;
        private readonly Func<object, byte[]> writer3;
        private readonly Func<object, byte[]> writer4;
        private readonly Func<object, byte[]> writer5;
        private readonly Func<object, byte[]> writer6;
        public NetworkInvocation(ECall call, Action<byte[]> send)
        {
            this.call = call;
            this.send = send;
            this.writer1 = ByteMath.GetByteFunction<T1>();
            this.writer2 = ByteMath.GetByteFunction<T2>();
            this.writer3 = ByteMath.GetByteFunction<T3>();
            this.writer4 = ByteMath.GetByteFunction<T4>();
            this.writer5 = ByteMath.GetByteFunction<T5>();
            this.writer6 = ByteMath.GetByteFunction<T6>();
        }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            byte[] b1 = writer1.Invoke(arg1);
            byte[] b2 = writer2.Invoke(arg2);
            byte[] b3 = writer3.Invoke(arg3);
            byte[] b4 = writer4.Invoke(arg4);
            byte[] b5 = writer5.Invoke(arg5);
            byte[] b6 = writer6.Invoke(arg6);
            byte[] rtn = new byte[b1.Length + b2.Length + b3.Length + b4.Length + b5.Length + b6.Length];
            int i = 0;
            Array.Copy(b1, 0, rtn, 0, b1.Length);
            i += b1.Length;
            Array.Copy(b2, 0, rtn, i, b2.Length);
            i += b2.Length;
            Array.Copy(b3, 0, rtn, i, b3.Length);
            i += b3.Length;
            Array.Copy(b4, 0, rtn, i, b4.Length);
            i += b4.Length;
            Array.Copy(b5, 0, rtn, i, b5.Length);
            i += b5.Length;
            Array.Copy(b6, 0, rtn, i, b6.Length);
            send?.Invoke(rtn.Callify(call));
        }
        public bool Read(byte[] bytes, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5, out T6 arg6)
        {
            try
            {
                int i = 0, size = 0;
                arg1 = ByteMath.ReadBytes<T1>(bytes, 0, out size);
                i += size;
                arg2 = ByteMath.ReadBytes<T2>(bytes, i, out size);
                i += size;
                arg3 = ByteMath.ReadBytes<T3>(bytes, i, out size);
                i += size;
                arg4 = ByteMath.ReadBytes<T4>(bytes, i, out size);
                i += size;
                arg5 = ByteMath.ReadBytes<T5>(bytes, i, out size);
                i += size;
                arg6 = ByteMath.ReadBytes<T6>(bytes, i, out _);
                return true;
            }
            catch
            {
                arg1 = default;
                arg2 = default;
                arg3 = default;
                arg4 = default;
                arg5 = default;
                arg6 = default;
                return false;
            }
        }
    }
    public class NetworkInvocation<T1, T2, T3, T4, T5, T6, T7>
    {
        private ECall call;
        private Action<byte[]> send;
        private readonly Func<object, byte[]> writer1;
        private readonly Func<object, byte[]> writer2;
        private readonly Func<object, byte[]> writer3;
        private readonly Func<object, byte[]> writer4;
        private readonly Func<object, byte[]> writer5;
        private readonly Func<object, byte[]> writer6;
        private readonly Func<object, byte[]> writer7;
        public NetworkInvocation(ECall call, Action<byte[]> send)
        {
            this.call = call;
            this.send = send;
            this.writer1 = ByteMath.GetByteFunction<T1>();
            this.writer2 = ByteMath.GetByteFunction<T2>();
            this.writer3 = ByteMath.GetByteFunction<T3>();
            this.writer4 = ByteMath.GetByteFunction<T4>();
            this.writer5 = ByteMath.GetByteFunction<T5>();
            this.writer6 = ByteMath.GetByteFunction<T6>();
            this.writer7 = ByteMath.GetByteFunction<T7>();
        }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            byte[] b1 = writer1.Invoke(arg1);
            byte[] b2 = writer2.Invoke(arg2);
            byte[] b3 = writer3.Invoke(arg3);
            byte[] b4 = writer4.Invoke(arg4);
            byte[] b5 = writer5.Invoke(arg5);
            byte[] b6 = writer6.Invoke(arg6);
            byte[] b7 = writer7.Invoke(arg7);
            byte[] rtn = new byte[b1.Length + b2.Length + b3.Length + b4.Length + b5.Length + b6.Length + b7.Length];
            int i = 0;
            Array.Copy(b1, 0, rtn, 0, b1.Length);
            i += b1.Length;
            Array.Copy(b2, 0, rtn, i, b2.Length);
            i += b2.Length;
            Array.Copy(b3, 0, rtn, i, b3.Length);
            i += b3.Length;
            Array.Copy(b4, 0, rtn, i, b4.Length);
            i += b4.Length;
            Array.Copy(b5, 0, rtn, i, b5.Length);
            i += b5.Length;
            Array.Copy(b6, 0, rtn, i, b6.Length);
            i += b6.Length;
            Array.Copy(b7, 0, rtn, i, b7.Length);
            send?.Invoke(rtn.Callify(call));
        }
        public bool Read(byte[] bytes, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5, out T6 arg6, out T7 arg7)
        {
            try
            {
                int i = 0, size = 0;
                arg1 = ByteMath.ReadBytes<T1>(bytes, 0, out size);
                i += size;
                arg2 = ByteMath.ReadBytes<T2>(bytes, i, out size);
                i += size;
                arg3 = ByteMath.ReadBytes<T3>(bytes, i, out size);
                i += size;
                arg4 = ByteMath.ReadBytes<T4>(bytes, i, out size);
                i += size;
                arg5 = ByteMath.ReadBytes<T5>(bytes, i, out size);
                i += size;
                arg6 = ByteMath.ReadBytes<T6>(bytes, i, out size);
                i += size;
                arg7 = ByteMath.ReadBytes<T7>(bytes, i, out _);
                return true;
            }
            catch
            {
                arg1 = default;
                arg2 = default;
                arg3 = default;
                arg4 = default;
                arg5 = default;
                arg6 = default;
                arg7 = default;
                return false;
            }
        }
    }
    public class NetworkInvocation<T1, T2, T3, T4, T5, T6, T7, T8>
    {
        private ECall call;
        private Action<byte[]> send;
        private readonly Func<object, byte[]> writer1;
        private readonly Func<object, byte[]> writer2;
        private readonly Func<object, byte[]> writer3;
        private readonly Func<object, byte[]> writer4;
        private readonly Func<object, byte[]> writer5;
        private readonly Func<object, byte[]> writer6;
        private readonly Func<object, byte[]> writer7;
        private readonly Func<object, byte[]> writer8;
        public NetworkInvocation(ECall call, Action<byte[]> send)
        {
            this.call = call;
            this.send = send;
            this.writer1 = ByteMath.GetByteFunction<T1>();
            this.writer2 = ByteMath.GetByteFunction<T2>();
            this.writer3 = ByteMath.GetByteFunction<T3>();
            this.writer4 = ByteMath.GetByteFunction<T4>();
            this.writer5 = ByteMath.GetByteFunction<T5>();
            this.writer6 = ByteMath.GetByteFunction<T6>();
            this.writer7 = ByteMath.GetByteFunction<T7>();
            this.writer8 = ByteMath.GetByteFunction<T8>();
        }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            byte[] b1 = writer1.Invoke(arg1);
            byte[] b2 = writer2.Invoke(arg2);
            byte[] b3 = writer3.Invoke(arg3);
            byte[] b4 = writer4.Invoke(arg4);
            byte[] b5 = writer5.Invoke(arg5);
            byte[] b6 = writer6.Invoke(arg6);
            byte[] b7 = writer7.Invoke(arg7);
            byte[] b8 = writer8.Invoke(arg8);
            byte[] rtn = new byte[b1.Length + b2.Length + b3.Length + b4.Length + b5.Length + b6.Length + b7.Length + b8.Length];
            int i = 0;
            Array.Copy(b1, 0, rtn, 0, b1.Length);
            i += b1.Length;
            Array.Copy(b2, 0, rtn, i, b2.Length);
            i += b2.Length;
            Array.Copy(b3, 0, rtn, i, b3.Length);
            i += b3.Length;
            Array.Copy(b4, 0, rtn, i, b4.Length);
            i += b4.Length;
            Array.Copy(b5, 0, rtn, i, b5.Length);
            i += b5.Length;
            Array.Copy(b6, 0, rtn, i, b6.Length);
            i += b6.Length;
            Array.Copy(b7, 0, rtn, i, b7.Length);
            i += b7.Length;
            Array.Copy(b8, 0, rtn, i, b8.Length);
            send?.Invoke(rtn.Callify(call));
        }
        public bool Read(byte[] bytes, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5, out T6 arg6, out T7 arg7, out T8 arg8)
        {
            try
            {
                int i = 0, size = 0;
                arg1 = ByteMath.ReadBytes<T1>(bytes, 0, out size);
                i += size;
                arg2 = ByteMath.ReadBytes<T2>(bytes, i, out size);
                i += size;
                arg3 = ByteMath.ReadBytes<T3>(bytes, i, out size);
                i += size;
                arg4 = ByteMath.ReadBytes<T4>(bytes, i, out size);
                i += size;
                arg5 = ByteMath.ReadBytes<T5>(bytes, i, out size);
                i += size;
                arg6 = ByteMath.ReadBytes<T6>(bytes, i, out size);
                i += size;
                arg7 = ByteMath.ReadBytes<T7>(bytes, i, out size);
                i += size;
                arg8 = ByteMath.ReadBytes<T8>(bytes, i, out _);
                return true;
            }
            catch
            {
                arg1 = default;
                arg2 = default;
                arg3 = default;
                arg4 = default;
                arg5 = default;
                arg6 = default;
                arg7 = default;
                arg8 = default;
                return false;
            }
        }
    }
    public class NetworkInvocation<T1, T2, T3, T4, T5, T6, T7, T8, T9>
    {
        private ECall call;
        private Action<byte[]> send;
        private readonly Func<object, byte[]> writer1;
        private readonly Func<object, byte[]> writer2;
        private readonly Func<object, byte[]> writer3;
        private readonly Func<object, byte[]> writer4;
        private readonly Func<object, byte[]> writer5;
        private readonly Func<object, byte[]> writer6;
        private readonly Func<object, byte[]> writer7;
        private readonly Func<object, byte[]> writer8;
        private readonly Func<object, byte[]> writer9;
        public NetworkInvocation(ECall call, Action<byte[]> send)
        {
            this.call = call;
            this.send = send;
            this.writer1 = ByteMath.GetByteFunction<T1>();
            this.writer2 = ByteMath.GetByteFunction<T2>();
            this.writer3 = ByteMath.GetByteFunction<T3>();
            this.writer4 = ByteMath.GetByteFunction<T4>();
            this.writer5 = ByteMath.GetByteFunction<T5>();
            this.writer6 = ByteMath.GetByteFunction<T6>();
            this.writer7 = ByteMath.GetByteFunction<T7>();
            this.writer8 = ByteMath.GetByteFunction<T8>();
            this.writer9 = ByteMath.GetByteFunction<T9>();
        }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
        {
            byte[] b1 = writer1.Invoke(arg1);
            byte[] b2 = writer2.Invoke(arg2);
            byte[] b3 = writer3.Invoke(arg3);
            byte[] b4 = writer4.Invoke(arg4);
            byte[] b5 = writer5.Invoke(arg5);
            byte[] b6 = writer6.Invoke(arg6);
            byte[] b7 = writer7.Invoke(arg7);
            byte[] b8 = writer8.Invoke(arg8);
            byte[] b9 = writer9.Invoke(arg9);
            byte[] rtn = new byte[b1.Length + b2.Length + b3.Length + b4.Length + b5.Length + b6.Length + b7.Length + b8.Length + b9.Length];
            int i = 0;
            Array.Copy(b1, 0, rtn, 0, b1.Length);
            i += b1.Length;
            Array.Copy(b2, 0, rtn, i, b2.Length);
            i += b2.Length;
            Array.Copy(b3, 0, rtn, i, b3.Length);
            i += b3.Length;
            Array.Copy(b4, 0, rtn, i, b4.Length);
            i += b4.Length;
            Array.Copy(b5, 0, rtn, i, b5.Length);
            i += b5.Length;
            Array.Copy(b6, 0, rtn, i, b6.Length);
            i += b6.Length;
            Array.Copy(b7, 0, rtn, i, b7.Length);
            i += b7.Length;
            Array.Copy(b8, 0, rtn, i, b8.Length);
            i += b8.Length;
            Array.Copy(b9, 0, rtn, i, b9.Length);
            send?.Invoke(rtn.Callify(call));
        }
        public bool Read(byte[] bytes, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5, out T6 arg6, out T7 arg7, out T8 arg8, out T9 arg9)
        {
            try
            {
                int i = 0, size = 0;
                arg1 = ByteMath.ReadBytes<T1>(bytes, 0, out size);
                i += size;
                arg2 = ByteMath.ReadBytes<T2>(bytes, i, out size);
                i += size;
                arg3 = ByteMath.ReadBytes<T3>(bytes, i, out size);
                i += size;
                arg4 = ByteMath.ReadBytes<T4>(bytes, i, out size);
                i += size;
                arg5 = ByteMath.ReadBytes<T5>(bytes, i, out size);
                i += size;
                arg6 = ByteMath.ReadBytes<T6>(bytes, i, out size);
                i += size;
                arg7 = ByteMath.ReadBytes<T7>(bytes, i, out size);
                i += size;
                arg8 = ByteMath.ReadBytes<T8>(bytes, i, out size);
                i += size;
                arg9 = ByteMath.ReadBytes<T9>(bytes, i, out _);
                return true;
            }
            catch
            {
                arg1 = default;
                arg2 = default;
                arg3 = default;
                arg4 = default;
                arg5 = default;
                arg6 = default;
                arg7 = default;
                arg8 = default;
                arg9 = default;
                return false;
            }
        }
    }    
    public class NetworkInvocation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
    {
        private ECall call;
        private Action<byte[]> send;
        private readonly Func<object, byte[]> writer1;
        private readonly Func<object, byte[]> writer2;
        private readonly Func<object, byte[]> writer3;
        private readonly Func<object, byte[]> writer4;
        private readonly Func<object, byte[]> writer5;
        private readonly Func<object, byte[]> writer6;
        private readonly Func<object, byte[]> writer7;
        private readonly Func<object, byte[]> writer8;
        private readonly Func<object, byte[]> writer9;
        private readonly Func<object, byte[]> writer10;
        public NetworkInvocation(ECall call, Action<byte[]> send)
        {
            this.call = call;
            this.send = send;
            this.writer1 = ByteMath.GetByteFunction<T1>();
            this.writer2 = ByteMath.GetByteFunction<T2>();
            this.writer3 = ByteMath.GetByteFunction<T3>();
            this.writer4 = ByteMath.GetByteFunction<T4>();
            this.writer5 = ByteMath.GetByteFunction<T5>();
            this.writer6 = ByteMath.GetByteFunction<T6>();
            this.writer7 = ByteMath.GetByteFunction<T7>();
            this.writer8 = ByteMath.GetByteFunction<T8>();
            this.writer9 = ByteMath.GetByteFunction<T9>();
            this.writer10 = ByteMath.GetByteFunction<T10>();
        }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
        {
            byte[] b1 = writer1.Invoke(arg1);
            byte[] b2 = writer2.Invoke(arg2);
            byte[] b3 = writer3.Invoke(arg3);
            byte[] b4 = writer4.Invoke(arg4);
            byte[] b5 = writer5.Invoke(arg5);
            byte[] b6 = writer6.Invoke(arg6);
            byte[] b7 = writer7.Invoke(arg7);
            byte[] b8 = writer8.Invoke(arg8);
            byte[] b9 = writer8.Invoke(arg9);
            byte[] b10 = writer10.Invoke(arg10);
            byte[] rtn = new byte[b1.Length + b2.Length + b3.Length + b4.Length + b5.Length + b6.Length + b7.Length + b8.Length + b9.Length + b10.Length];
            int i = 0;
            Array.Copy(b1, 0, rtn, 0, b1.Length);
            i += b1.Length;
            Array.Copy(b2, 0, rtn, i, b2.Length);
            i += b2.Length;
            Array.Copy(b3, 0, rtn, i, b3.Length);
            i += b3.Length;
            Array.Copy(b4, 0, rtn, i, b4.Length);
            i += b4.Length;
            Array.Copy(b5, 0, rtn, i, b5.Length);
            i += b5.Length;
            Array.Copy(b6, 0, rtn, i, b6.Length);
            i += b6.Length;
            Array.Copy(b7, 0, rtn, i, b7.Length);
            i += b7.Length;
            Array.Copy(b8, 0, rtn, i, b8.Length);
            i += b8.Length;
            Array.Copy(b9, 0, rtn, i, b9.Length);
            i += b9.Length;
            Array.Copy(b10, 0, rtn, i, b10.Length);
            send?.Invoke(rtn.Callify(call));
        }
        public bool Read(byte[] bytes, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5, out T6 arg6, out T7 arg7, out T8 arg8, out T9 arg9, out T10 arg10)
        {
            try
            {
                int i = 0, size = 0;
                arg1 = ByteMath.ReadBytes<T1>(bytes, 0, out size);
                i += size;
                arg2 = ByteMath.ReadBytes<T2>(bytes, i, out size);
                i += size;
                arg3 = ByteMath.ReadBytes<T3>(bytes, i, out size);
                i += size;
                arg4 = ByteMath.ReadBytes<T4>(bytes, i, out size);
                i += size;
                arg5 = ByteMath.ReadBytes<T5>(bytes, i, out size);
                i += size;
                arg6 = ByteMath.ReadBytes<T6>(bytes, i, out size);
                i += size;
                arg7 = ByteMath.ReadBytes<T7>(bytes, i, out size);
                i += size;
                arg8 = ByteMath.ReadBytes<T8>(bytes, i, out size);
                i += size;
                arg9 = ByteMath.ReadBytes<T9>(bytes, i, out size);
                i += size;
                arg10 = ByteMath.ReadBytes<T10>(bytes, i, out _);
                return true;
            }
            catch
            {
                arg1 = default;
                arg2 = default;
                arg3 = default;
                arg4 = default;
                arg5 = default;
                arg6 = default;
                arg7 = default;
                arg8 = default;
                arg9 = default;
                arg10 = default;
                return false;
            }
        }
    }
}

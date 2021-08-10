﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Uncreated.Networking.Invocations
{
    public delegate void SendTask(byte[] data);
    public abstract class NetworkCall
    {
        protected readonly ECall call;
        protected const int WAIT_TIMEOUT = 2000; // 2 seconds
        protected const int COUNTER_MAX = 5;
        public static Func<bool> NullCheck;
        static byte _id = 0;
        public static byte ID {
            get
            {
                _id++;
                if (_id >= byte.MaxValue) _id = 1;
                return _id;
            }
        }
        protected SendTask send { get => Client.Send; }
        public NetworkCall(ECall call)
        {
            this.call = call;
        }
        protected CancellationTokenSource AddPending(byte id)
        {
            CancellationTokenSource canceller = new CancellationTokenSource();
            Client.Waits.Add(id, new KeyValuePair<bool, CancellationTokenSource>(true, canceller));
            return canceller;
        }
        public static void RemovePending(byte id, bool dispose, bool cancel)
        {
            if (Client.Waits.TryGetValue(id, out KeyValuePair<bool, CancellationTokenSource> d))
            {
                if (cancel)
                    d.Value.Cancel();
                if (dispose)
                    d.Value.Dispose();
                Client.Waits.Remove(id);
            }
        }
        public static void RemovePending(byte id, CancellationTokenSource d, bool dispose, bool cancel)
        {
            if (cancel)
                d.Cancel();
            if(dispose)
                d.Dispose();
            Client.Waits.Remove(id);
        }
    }
    public class NetworkInvocationRaw<T> : NetworkCall
    {
        private readonly Reader<T> reader;
        private readonly Func<T, byte[]> writer;
        public NetworkInvocationRaw(ECall call, Reader<T> read, Func<T, byte[]> write) : base(call)
        {
            this.reader = read;
            this.writer = write;
        }
        public void Invoke(T arg) => send?.Invoke(GetBytes(arg, EReturnType.SEND_NO_RETURN));
        public byte[] GetBytes(T arg, EReturnType expectrtn, byte id = 0) => writer.Invoke(arg).Callify(call, expectrtn, id, 0);
        public bool Read(byte[] bytes, out T output)
        {
            try
            {
                output = reader.Invoke(bytes, 0, out _);
                return true;
            }
            catch
            {
                output = default;
                return false;
            }
        }
    }
    public delegate TOutput Reader<TOutput>(byte[] bytes, int index, out int size);
    public class NetworkInvocation : NetworkCall
    {
        public NetworkInvocation(ECall call) : base(call) { }
        public void Invoke()
        {
            send?.Invoke(GetBytes(EReturnType.SEND_NO_RETURN));
        }
        public byte[] GetBytes(EReturnType expectrtn, byte id = 0)
        {
            return ByteMath.Callify(call, expectrtn, id, 0);
        }
    }
    public class NetworkInvocation<T1> : NetworkCall
    {
        private readonly Func<object, byte[]> writer1;
        private readonly Reader<T1> reader1;
        public NetworkInvocation(ECall call) : base(call)
        {
            this.writer1 = ByteMath.GetWriteFunction<T1>();
            this.reader1 = ByteMath.GetReadFunction<T1>();
        }
        public void Invoke(T1 arg1)
        {
            send?.Invoke(GetBytes(arg1, EReturnType.SEND_NO_RETURN));
        }
        public byte[] GetBytes(T1 arg1, EReturnType expectrtn, byte id = 0)
        {
            return writer1.Invoke(arg1).Callify(call, expectrtn, id, 0);
        }
        public bool Read(byte[] bytes, out T1 arg1)
        {
            try
            {
                arg1 = reader1.Invoke(bytes, 0, out _);
                return true;
            }
            catch
            {
                arg1 = default;
                return false;
            }
        }
    }
    public class NetworkInvocation<T1, T2> : NetworkCall
    {
        private readonly Func<object, byte[]> writer1;
        private readonly Func<object, byte[]> writer2;
        private readonly Reader<T1> reader1;
        private readonly Reader<T2> reader2;
        public NetworkInvocation(ECall call) : base(call)
        {
            this.writer1 = ByteMath.GetWriteFunction<T1>();
            this.writer2 = ByteMath.GetWriteFunction<T2>();
            this.reader1 = ByteMath.GetReadFunction<T1>();
            this.reader2 = ByteMath.GetReadFunction<T2>();
        }
        public void Invoke(T1 arg1, T2 arg2)
        {
            send?.Invoke(GetBytes(arg1, arg2, EReturnType.SEND_NO_RETURN));
        }
        public byte[] GetBytes(T1 arg1, T2 arg2, EReturnType expectrtn, byte id = 0)
        {
            byte[] b1 = writer1.Invoke(arg1);
            byte[] b2 = writer2.Invoke(arg2);
            byte[] rtn = new byte[b1.Length + b2.Length];
            Array.Copy(b1, 0, rtn, 0, b1.Length);
            Array.Copy(b2, 0, rtn, b1.Length, b2.Length);
            return rtn.Callify(call, expectrtn, id, 0);
        }
        public bool Read(byte[] bytes, out T1 arg1, out T2 arg2)
        {
            try
            {
                arg1 = reader1.Invoke(bytes, 0, out int size);
                arg2 = reader2.Invoke(bytes, size, out _);
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
    public class NetworkInvocation<T1, T2, T3> : NetworkCall
    {
        private readonly Func<object, byte[]> writer1;
        private readonly Func<object, byte[]> writer2;
        private readonly Func<object, byte[]> writer3;
        private readonly Reader<T1> reader1;
        private readonly Reader<T2> reader2;
        private readonly Reader<T3> reader3;
        public NetworkInvocation(ECall call) : base(call)
        {
            this.writer1 = ByteMath.GetWriteFunction<T1>();
            this.writer2 = ByteMath.GetWriteFunction<T2>();
            this.writer3 = ByteMath.GetWriteFunction<T3>();
            this.reader1 = ByteMath.GetReadFunction<T1>();
            this.reader2 = ByteMath.GetReadFunction<T2>();
            this.reader3 = ByteMath.GetReadFunction<T3>();
        }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3)
        {
            send?.Invoke(GetBytes(arg1, arg2, arg3, EReturnType.SEND_NO_RETURN));
        }
        public byte[] GetBytes(T1 arg1, T2 arg2, T3 arg3, EReturnType expectrtn, byte id = 0)
        {
            byte[] b1 = writer1.Invoke(arg1);
            byte[] b2 = writer2.Invoke(arg2);
            byte[] b3 = writer3.Invoke(arg3);
            byte[] rtn = new byte[b1.Length + b2.Length + b3.Length];
            Array.Copy(b1, 0, rtn, 0, b1.Length);
            Array.Copy(b2, 0, rtn, b1.Length, b2.Length);
            Array.Copy(b3, 0, rtn, b1.Length + b2.Length, b3.Length);
            return rtn.Callify(call, expectrtn, id, 0);
        }
        public bool Read(byte[] bytes, out T1 arg1, out T2 arg2, out T3 arg3)
        {
            try
            {
                int i = 0;
                arg1 = reader1.Invoke(bytes, 0, out int size);
                i += size;
                arg2 = reader2.Invoke(bytes, i, out size);
                i += size;
                arg3 = reader3.Invoke(bytes, i, out _);
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
    public class NetworkInvocation<T1, T2, T3, T4> : NetworkCall
    {
        private readonly Func<object, byte[]> writer1;
        private readonly Func<object, byte[]> writer2;
        private readonly Func<object, byte[]> writer3;
        private readonly Func<object, byte[]> writer4;
        private readonly Reader<T1> reader1;
        private readonly Reader<T2> reader2;
        private readonly Reader<T3> reader3;
        private readonly Reader<T4> reader4;
        public NetworkInvocation(ECall call) : base(call)
        {
            this.writer1 = ByteMath.GetWriteFunction<T1>();
            this.writer2 = ByteMath.GetWriteFunction<T2>();
            this.writer3 = ByteMath.GetWriteFunction<T3>();
            this.writer4 = ByteMath.GetWriteFunction<T4>();
            this.reader1 = ByteMath.GetReadFunction<T1>();
            this.reader2 = ByteMath.GetReadFunction<T2>();
            this.reader3 = ByteMath.GetReadFunction<T3>();
            this.reader4 = ByteMath.GetReadFunction<T4>();
        }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            send?.Invoke(GetBytes(arg1, arg2, arg3, arg4, EReturnType.SEND_NO_RETURN));
        }
        public byte[] GetBytes(T1 arg1, T2 arg2, T3 arg3, T4 arg4, EReturnType expectrtn, byte id = 0)
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
            return rtn.Callify(call, expectrtn, id, 0);
        }
        public bool Read(byte[] bytes, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4)
        {
            try
            {
                int i = 0;
                arg1 = reader1.Invoke(bytes, 0, out int size);
                i += size;
                arg2 = reader2.Invoke(bytes, i, out size);
                i += size;
                arg3 = reader3.Invoke(bytes, i, out size);
                i += size;
                arg4 = reader4.Invoke(bytes, i, out _);
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
    public class NetworkInvocation<T1, T2, T3, T4, T5> : NetworkCall
    {
        private readonly Func<object, byte[]> writer1;
        private readonly Func<object, byte[]> writer2;
        private readonly Func<object, byte[]> writer3;
        private readonly Func<object, byte[]> writer4;
        private readonly Func<object, byte[]> writer5;
        private readonly Reader<T1> reader1;
        private readonly Reader<T2> reader2;
        private readonly Reader<T3> reader3;
        private readonly Reader<T4> reader4;
        private readonly Reader<T5> reader5;
        public NetworkInvocation(ECall call) : base(call)
        {
            this.writer1 = ByteMath.GetWriteFunction<T1>();
            this.writer2 = ByteMath.GetWriteFunction<T2>();
            this.writer3 = ByteMath.GetWriteFunction<T3>();
            this.writer4 = ByteMath.GetWriteFunction<T4>();
            this.writer5 = ByteMath.GetWriteFunction<T5>();
            this.reader1 = ByteMath.GetReadFunction<T1>();
            this.reader2 = ByteMath.GetReadFunction<T2>();
            this.reader3 = ByteMath.GetReadFunction<T3>();
            this.reader4 = ByteMath.GetReadFunction<T4>();
            this.reader5 = ByteMath.GetReadFunction<T5>();
        }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            send?.Invoke(GetBytes(arg1, arg2, arg3, arg4, arg5, EReturnType.SEND_NO_RETURN));
        }
        public byte[] GetBytes(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, EReturnType expectrtn, byte id = 0)
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
            return rtn.Callify(call, expectrtn, id, 0);
        }
        public bool Read(byte[] bytes, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5)
        {
            try
            {
                int i = 0;
                arg1 = reader1.Invoke(bytes, 0, out int size);
                i += size;
                arg2 = reader2.Invoke(bytes, i, out size);
                i += size;
                arg3 = reader3.Invoke(bytes, i, out size);
                i += size;
                arg4 = reader4.Invoke(bytes, i, out size);
                i += size;
                arg5 = reader5.Invoke(bytes, i, out _);
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
    public class NetworkInvocation<T1, T2, T3, T4, T5, T6> : NetworkCall
    {
        private readonly Func<object, byte[]> writer1;
        private readonly Func<object, byte[]> writer2;
        private readonly Func<object, byte[]> writer3;
        private readonly Func<object, byte[]> writer4;
        private readonly Func<object, byte[]> writer5;
        private readonly Func<object, byte[]> writer6;
        private readonly Reader<T1> reader1;
        private readonly Reader<T2> reader2;
        private readonly Reader<T3> reader3;
        private readonly Reader<T4> reader4;
        private readonly Reader<T5> reader5;
        private readonly Reader<T6> reader6;
        public NetworkInvocation(ECall call) : base(call)
        {
            this.writer1 = ByteMath.GetWriteFunction<T1>();
            this.writer2 = ByteMath.GetWriteFunction<T2>();
            this.writer3 = ByteMath.GetWriteFunction<T3>();
            this.writer4 = ByteMath.GetWriteFunction<T4>();
            this.writer5 = ByteMath.GetWriteFunction<T5>();
            this.writer6 = ByteMath.GetWriteFunction<T6>();
            this.reader1 = ByteMath.GetReadFunction<T1>();
            this.reader2 = ByteMath.GetReadFunction<T2>();
            this.reader3 = ByteMath.GetReadFunction<T3>();
            this.reader4 = ByteMath.GetReadFunction<T4>();
            this.reader5 = ByteMath.GetReadFunction<T5>();
            this.reader6 = ByteMath.GetReadFunction<T6>();
        }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            send?.Invoke(GetBytes(arg1, arg2, arg3, arg4, arg5, arg6, EReturnType.SEND_NO_RETURN));
        }
        public byte[] GetBytes(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, EReturnType expectrtn, byte id = 0)
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
            return rtn.Callify(call, expectrtn, id, 0);
        }
        public bool Read(byte[] bytes, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5, out T6 arg6)
        {
            try
            {
                int i = 0;
                arg1 = reader1.Invoke(bytes, 0, out int size);
                i += size;
                arg2 = reader2.Invoke(bytes, i, out size);
                i += size;
                arg3 = reader3.Invoke(bytes, i, out size);
                i += size;
                arg4 = reader4.Invoke(bytes, i, out size);
                i += size;
                arg5 = reader5.Invoke(bytes, i, out size);
                i += size;
                arg6 = reader6.Invoke(bytes, i, out _);
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
    public class NetworkInvocation<T1, T2, T3, T4, T5, T6, T7> : NetworkCall
    {
        private readonly Func<object, byte[]> writer1;
        private readonly Func<object, byte[]> writer2;
        private readonly Func<object, byte[]> writer3;
        private readonly Func<object, byte[]> writer4;
        private readonly Func<object, byte[]> writer5;
        private readonly Func<object, byte[]> writer6;
        private readonly Func<object, byte[]> writer7;
        private readonly Reader<T1> reader1;
        private readonly Reader<T2> reader2;
        private readonly Reader<T3> reader3;
        private readonly Reader<T4> reader4;
        private readonly Reader<T5> reader5;
        private readonly Reader<T6> reader6;
        private readonly Reader<T7> reader7;
        public NetworkInvocation(ECall call) : base(call)
        {
            this.writer1 = ByteMath.GetWriteFunction<T1>();
            this.writer2 = ByteMath.GetWriteFunction<T2>();
            this.writer3 = ByteMath.GetWriteFunction<T3>();
            this.writer4 = ByteMath.GetWriteFunction<T4>();
            this.writer5 = ByteMath.GetWriteFunction<T5>();
            this.writer6 = ByteMath.GetWriteFunction<T6>();
            this.writer7 = ByteMath.GetWriteFunction<T7>();
            this.reader1 = ByteMath.GetReadFunction<T1>();
            this.reader2 = ByteMath.GetReadFunction<T2>();
            this.reader3 = ByteMath.GetReadFunction<T3>();
            this.reader4 = ByteMath.GetReadFunction<T4>();
            this.reader5 = ByteMath.GetReadFunction<T5>();
            this.reader6 = ByteMath.GetReadFunction<T6>();
            this.reader7 = ByteMath.GetReadFunction<T7>();
        }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            send?.Invoke(GetBytes(arg1, arg2, arg3, arg4, arg5, arg6, arg7, EReturnType.SEND_NO_RETURN));
        }
        public byte[] GetBytes(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, EReturnType expectrtn, byte id = 0)
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
            return rtn.Callify(call, expectrtn, id, 0);
        }
        public bool Read(byte[] bytes, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5, out T6 arg6, out T7 arg7)
        {
            try
            {
                int i = 0;
                arg1 = reader1.Invoke(bytes, 0, out int size);
                i += size;
                arg2 = reader2.Invoke(bytes, i, out size);
                i += size;
                arg3 = reader3.Invoke(bytes, i, out size);
                i += size;
                arg4 = reader4.Invoke(bytes, i, out size);
                i += size;
                arg5 = reader5.Invoke(bytes, i, out size);
                i += size;
                arg6 = reader6.Invoke(bytes, i, out size);
                i += size;
                arg7 = reader7.Invoke(bytes, i, out _);
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
    public class NetworkInvocation<T1, T2, T3, T4, T5, T6, T7, T8> : NetworkCall
    {
        private readonly Func<object, byte[]> writer1;
        private readonly Func<object, byte[]> writer2;
        private readonly Func<object, byte[]> writer3;
        private readonly Func<object, byte[]> writer4;
        private readonly Func<object, byte[]> writer5;
        private readonly Func<object, byte[]> writer6;
        private readonly Func<object, byte[]> writer7;
        private readonly Func<object, byte[]> writer8;
        private readonly Reader<T1> reader1;
        private readonly Reader<T2> reader2;
        private readonly Reader<T3> reader3;
        private readonly Reader<T4> reader4;
        private readonly Reader<T5> reader5;
        private readonly Reader<T6> reader6;
        private readonly Reader<T7> reader7;
        private readonly Reader<T8> reader8;
        public NetworkInvocation(ECall call) : base(call)
        {
            this.writer1 = ByteMath.GetWriteFunction<T1>();
            this.writer2 = ByteMath.GetWriteFunction<T2>();
            this.writer3 = ByteMath.GetWriteFunction<T3>();
            this.writer4 = ByteMath.GetWriteFunction<T4>();
            this.writer5 = ByteMath.GetWriteFunction<T5>();
            this.writer6 = ByteMath.GetWriteFunction<T6>();
            this.writer7 = ByteMath.GetWriteFunction<T7>();
            this.writer8 = ByteMath.GetWriteFunction<T8>();
            this.reader1 = ByteMath.GetReadFunction<T1>();
            this.reader2 = ByteMath.GetReadFunction<T2>();
            this.reader3 = ByteMath.GetReadFunction<T3>();
            this.reader4 = ByteMath.GetReadFunction<T4>();
            this.reader5 = ByteMath.GetReadFunction<T5>();
            this.reader6 = ByteMath.GetReadFunction<T6>();
            this.reader7 = ByteMath.GetReadFunction<T7>();
            this.reader8 = ByteMath.GetReadFunction<T8>();
        }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            send?.Invoke(GetBytes(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, EReturnType.SEND_NO_RETURN));
        }
        public byte[] GetBytes(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, EReturnType expectrtn, byte id = 0)
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
            return rtn.Callify(call, expectrtn, id, 0);
        }
        public bool Read(byte[] bytes, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5, out T6 arg6, out T7 arg7, out T8 arg8)
        {
            try
            {
                int i = 0;
                arg1 = reader1.Invoke(bytes, 0, out int size);
                i += size;
                arg2 = reader2.Invoke(bytes, i, out size);
                i += size;
                arg3 = reader3.Invoke(bytes, i, out size);
                i += size;
                arg4 = reader4.Invoke(bytes, i, out size);
                i += size;
                arg5 = reader5.Invoke(bytes, i, out size);
                i += size;
                arg6 = reader6.Invoke(bytes, i, out size);
                i += size;
                arg7 = reader7.Invoke(bytes, i, out size);
                i += size;
                arg8 = reader8.Invoke(bytes, i, out _);
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
    public class NetworkInvocation<T1, T2, T3, T4, T5, T6, T7, T8, T9> : NetworkCall
    {
        private readonly Func<object, byte[]> writer1;
        private readonly Func<object, byte[]> writer2;
        private readonly Func<object, byte[]> writer3;
        private readonly Func<object, byte[]> writer4;
        private readonly Func<object, byte[]> writer5;
        private readonly Func<object, byte[]> writer6;
        private readonly Func<object, byte[]> writer7;
        private readonly Func<object, byte[]> writer8;
        private readonly Func<object, byte[]> writer9;
        private readonly Reader<T1> reader1;
        private readonly Reader<T2> reader2;
        private readonly Reader<T3> reader3;
        private readonly Reader<T4> reader4;
        private readonly Reader<T5> reader5;
        private readonly Reader<T6> reader6;
        private readonly Reader<T7> reader7;
        private readonly Reader<T8> reader8;
        private readonly Reader<T9> reader9;
        public NetworkInvocation(ECall call) : base(call)
        {
            this.writer1 = ByteMath.GetWriteFunction<T1>();
            this.writer2 = ByteMath.GetWriteFunction<T2>();
            this.writer3 = ByteMath.GetWriteFunction<T3>();
            this.writer4 = ByteMath.GetWriteFunction<T4>();
            this.writer5 = ByteMath.GetWriteFunction<T5>();
            this.writer6 = ByteMath.GetWriteFunction<T6>();
            this.writer7 = ByteMath.GetWriteFunction<T7>();
            this.writer8 = ByteMath.GetWriteFunction<T8>();
            this.writer9 = ByteMath.GetWriteFunction<T9>();
            this.reader1 = ByteMath.GetReadFunction<T1>();
            this.reader2 = ByteMath.GetReadFunction<T2>();
            this.reader3 = ByteMath.GetReadFunction<T3>();
            this.reader4 = ByteMath.GetReadFunction<T4>();
            this.reader5 = ByteMath.GetReadFunction<T5>();
            this.reader6 = ByteMath.GetReadFunction<T6>();
            this.reader7 = ByteMath.GetReadFunction<T7>();
            this.reader8 = ByteMath.GetReadFunction<T8>();
            this.reader9 = ByteMath.GetReadFunction<T9>();
        }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
        {
            send?.Invoke(GetBytes(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, EReturnType.SEND_NO_RETURN));
        }
        public byte[] GetBytes(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, EReturnType expectrtn, byte id = 0)
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
            return rtn.Callify(call, expectrtn, id, 0);
        }
        public bool Read(byte[] bytes, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5, out T6 arg6, out T7 arg7, out T8 arg8, out T9 arg9)
        {
            try
            {
                int i = 0;
                arg1 = reader1.Invoke(bytes, 0, out int size);
                i += size;
                arg2 = reader2.Invoke(bytes, i, out size);
                i += size;
                arg3 = reader3.Invoke(bytes, i, out size);
                i += size;
                arg4 = reader4.Invoke(bytes, i, out size);
                i += size;
                arg5 = reader5.Invoke(bytes, i, out size);
                i += size;
                arg6 = reader6.Invoke(bytes, i, out size);
                i += size;
                arg7 = reader7.Invoke(bytes, i, out size);
                i += size;
                arg8 = reader8.Invoke(bytes, i, out size);
                i += size;
                arg9 = reader9.Invoke(bytes, i, out _);
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
    public class NetworkInvocation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : NetworkCall
    {
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
        public NetworkInvocation(ECall call) : base(call)
        {
            this.writer1 = ByteMath.GetWriteFunction<T1>();
            this.writer2 = ByteMath.GetWriteFunction<T2>();
            this.writer3 = ByteMath.GetWriteFunction<T3>();
            this.writer4 = ByteMath.GetWriteFunction<T4>();
            this.writer5 = ByteMath.GetWriteFunction<T5>();
            this.writer6 = ByteMath.GetWriteFunction<T6>();
            this.writer7 = ByteMath.GetWriteFunction<T7>();
            this.writer8 = ByteMath.GetWriteFunction<T8>();
            this.writer9 = ByteMath.GetWriteFunction<T9>();
            this.writer10 = ByteMath.GetWriteFunction<T10>();
            this.reader1 = ByteMath.GetReadFunction<T1>();
            this.reader2 = ByteMath.GetReadFunction<T2>();
            this.reader3 = ByteMath.GetReadFunction<T3>();
            this.reader4 = ByteMath.GetReadFunction<T4>();
            this.reader5 = ByteMath.GetReadFunction<T5>();
            this.reader6 = ByteMath.GetReadFunction<T6>();
            this.reader7 = ByteMath.GetReadFunction<T7>();
            this.reader8 = ByteMath.GetReadFunction<T8>();
            this.reader9 = ByteMath.GetReadFunction<T9>();
            this.reader10 = ByteMath.GetReadFunction<T10>();
        }
        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
        {
            send?.Invoke(GetBytes(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, EReturnType.SEND_NO_RETURN));
        }
        public byte[] GetBytes(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, EReturnType expectrtn, byte id = 0)
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
            return rtn.Callify(call, expectrtn, id, 0);
        }
        public bool Read(byte[] bytes, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5, out T6 arg6, out T7 arg7, out T8 arg8, out T9 arg9, out T10 arg10)
        {
            try
            {
                int i = 0;
                arg1 = reader1.Invoke(bytes, 0, out int size);
                i += size;
                arg2 = reader2.Invoke(bytes, i, out size);
                i += size;
                arg3 = reader3.Invoke(bytes, i, out size);
                i += size;
                arg4 = reader4.Invoke(bytes, i, out size);
                i += size;
                arg5 = reader5.Invoke(bytes, i, out size);
                i += size;
                arg6 = reader6.Invoke(bytes, i, out size);
                i += size;
                arg7 = reader7.Invoke(bytes, i, out size);
                i += size;
                arg8 = reader8.Invoke(bytes, i, out size);
                i += size;
                arg9 = reader9.Invoke(bytes, i, out size);
                i += size;
                arg10 = reader10.Invoke(bytes, i, out _);
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
    public enum EReturnType : byte
    {
        SEND_NO_RETURN = 0,
        SEND_NO_RETURN_RELIABLE = 1,
        SEND_NO_RETURN_CONFIRMATION = 2,
        SEND_NO_RETURN_FAILED = 3,
        SEND_RETURN = 4,
        SEND_RETURN_VALUE = 5,
        SEND_RETURN_FAILED = 6
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Networking.Invocations
{
    public delegate Task SendTask(byte[] data);
    public class NetworkInvocationRaw<T>
    {
        private ECall call;
        private SendTask send { get => Client.Send; }
        private Reader<T> reader;
        private Func<T, byte[]> writer;
        public NetworkInvocationRaw(ECall call, Reader<T> read, Func<T, byte[]> write)
        {
            this.call = call;
            this.reader = read;
            this.writer = write;
        }
        public async Task Invoke(T arg) => await send?.Invoke(writer.Invoke(arg).Callify(call));
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
    public class NetworkInvocation
    {
        private readonly ECall call;
        private SendTask send { get => Client.Send; }
        public NetworkInvocation(ECall call)
        {
            this.call = call;
        }
        public async Task Invoke()
        {
            await send?.Invoke(ByteMath.Callify(call));
        }
    }
    public class NetworkInvocation<T1>
    {
        private ECall call;
        private SendTask send { get => Client.Send; }
        private readonly Func<object, byte[]> writer1;
        private readonly Reader<T1> reader1;
        public NetworkInvocation(ECall call)
        {
            this.call = call;
            this.writer1 = ByteMath.GetWriteFunction<T1>();
            this.reader1 = ByteMath.GetReadFunction<T1>();
        }
        public async Task Invoke(T1 arg1)
        {
            await send?.Invoke(writer1.Invoke(arg1).Callify(call));
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
    public class NetworkInvocation<T1, T2>
    {
        private ECall call;
        private SendTask send { get => Client.Send; }
        private readonly Func<object, byte[]> writer1;
        private readonly Func<object, byte[]> writer2;
        private readonly Reader<T1> reader1;
        private readonly Reader<T2> reader2;
        public NetworkInvocation(ECall call)
        {
            this.call = call;
            this.writer1 = ByteMath.GetWriteFunction<T1>();
            this.writer2 = ByteMath.GetWriteFunction<T2>();
            this.reader1 = ByteMath.GetReadFunction<T1>();
            this.reader2 = ByteMath.GetReadFunction<T2>();
        }
        public async Task Invoke(T1 arg1, T2 arg2)
        {
            byte[] b1 = writer1.Invoke(arg1);
            byte[] b2 = writer2.Invoke(arg2);
            byte[] rtn = new byte[b1.Length + b2.Length];
            Array.Copy(b1, 0, rtn, 0, b1.Length);
            Array.Copy(b2, 0, rtn, b1.Length, b2.Length);
            await send?.Invoke(rtn.Callify(call));
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
    public class NetworkInvocation<T1, T2, T3>
    {
        private ECall call;
        private SendTask send { get => Client.Send; }
        private readonly Func<object, byte[]> writer1;
        private readonly Func<object, byte[]> writer2;
        private readonly Func<object, byte[]> writer3;
        private readonly Reader<T1> reader1;
        private readonly Reader<T2> reader2;
        private readonly Reader<T3> reader3;
        public NetworkInvocation(ECall call)
        {
            this.call = call;
            this.writer1 = ByteMath.GetWriteFunction<T1>();
            this.writer2 = ByteMath.GetWriteFunction<T2>();
            this.writer3 = ByteMath.GetWriteFunction<T3>();
            this.reader1 = ByteMath.GetReadFunction<T1>();
            this.reader2 = ByteMath.GetReadFunction<T2>();
            this.reader3 = ByteMath.GetReadFunction<T3>();
        }
        public async Task Invoke(T1 arg1, T2 arg2, T3 arg3)
        {
            byte[] b1 = writer1.Invoke(arg1);
            byte[] b2 = writer2.Invoke(arg2);
            byte[] b3 = writer3.Invoke(arg3);
            byte[] rtn = new byte[b1.Length + b2.Length + b3.Length];
            Array.Copy(b1, 0, rtn, 0, b1.Length);
            Array.Copy(b2, 0, rtn, b1.Length, b2.Length);
            Array.Copy(b3, 0, rtn, b1.Length + b2.Length, b3.Length);
            await send?.Invoke(rtn.Callify(call));
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
    public class NetworkInvocation<T1, T2, T3, T4>
    {
        private ECall call;
        private SendTask send { get => Client.Send; }
        private readonly Func<object, byte[]> writer1;
        private readonly Func<object, byte[]> writer2;
        private readonly Func<object, byte[]> writer3;
        private readonly Func<object, byte[]> writer4;
        private readonly Reader<T1> reader1;
        private readonly Reader<T2> reader2;
        private readonly Reader<T3> reader3;
        private readonly Reader<T4> reader4;
        public NetworkInvocation(ECall call)
        {
            this.call = call;
            this.writer1 = ByteMath.GetWriteFunction<T1>();
            this.writer2 = ByteMath.GetWriteFunction<T2>();
            this.writer3 = ByteMath.GetWriteFunction<T3>();
            this.writer4 = ByteMath.GetWriteFunction<T4>();
            this.reader1 = ByteMath.GetReadFunction<T1>();
            this.reader2 = ByteMath.GetReadFunction<T2>();
            this.reader3 = ByteMath.GetReadFunction<T3>();
            this.reader4 = ByteMath.GetReadFunction<T4>();
        }
        public async Task Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
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
            await send?.Invoke(rtn.Callify(call));
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
    public class NetworkInvocation<T1, T2, T3, T4, T5>
    {
        private ECall call;
        private SendTask send { get => Client.Send; }
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
        public NetworkInvocation(ECall call)
        {
            this.call = call;
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
        public async Task Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
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
            await send?.Invoke(rtn.Callify(call));
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
    public class NetworkInvocation<T1, T2, T3, T4, T5, T6>
    {
        private ECall call;
        private SendTask send { get => Client.Send; }
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
        public NetworkInvocation(ECall call)
        {
            this.call = call;
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
        public async Task Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
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
            await send?.Invoke(rtn.Callify(call));
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
    public class NetworkInvocation<T1, T2, T3, T4, T5, T6, T7>
    {
        private ECall call;
        private SendTask send { get => Client.Send; }
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
        public NetworkInvocation(ECall call)
        {
            this.call = call;
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
        public async Task Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
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
            await send?.Invoke(rtn.Callify(call));
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
    public class NetworkInvocation<T1, T2, T3, T4, T5, T6, T7, T8>
    {
        private ECall call;
        private SendTask send { get => Client.Send; }
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
        public NetworkInvocation(ECall call)
        {
            this.call = call;
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
        public async Task Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
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
            await send?.Invoke(rtn.Callify(call));
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
    public class NetworkInvocation<T1, T2, T3, T4, T5, T6, T7, T8, T9>
    {
        private ECall call;
        private SendTask send { get => Client.Send; }
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
        public NetworkInvocation(ECall call)
        {
            this.call = call;
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
        public async Task Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
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
            await send?.Invoke(rtn.Callify(call));
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
    public class NetworkInvocation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
    {
        private ECall call;
        private SendTask send { get => Client.Send; }
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
        public NetworkInvocation(ECall call)
        {
            this.call = call;
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
        public async Task Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
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
            await send?.Invoke(rtn.Callify(call));
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
}

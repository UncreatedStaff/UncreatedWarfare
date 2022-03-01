using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Networking.Encoding;

namespace Uncreated.Networking
{
    public abstract class BaseNetCall
    {
        public readonly ushort ID;
        public readonly bool RequiresMethod;
        public BaseNetCall(ushort method, bool registerWithoutMethod = false)
        {
            this.ID = method;
            this.RequiresMethod = !registerWithoutMethod;
        }
        public BaseNetCall(Delegate method, bool registerWithoutMethod = false)
        {
            MethodInfo info = method.GetMethodInfo();
            IEnumerator<CustomAttributeData> attributes = info.CustomAttributes.GetEnumerator();
            while (attributes.MoveNext())
            {
                if (attributes.Current.AttributeType == typeof(NetCallAttribute))
                {
                    if (attributes.Current.ConstructorArguments.Count > 1 &&
                        attributes.Current.ConstructorArguments[1].Value is ushort u)
                        this.ID = u;
                    else if (attributes.Current.ConstructorArguments.Count > 1 &&
                        attributes.Current.ConstructorArguments[1].Value is ENetCall e)
                        this.ID = (ushort)e;
                    break;
                }
            }
            attributes.Dispose();
            if (this.ID == default)
            {
                throw new ArgumentException($"Method provided for {info.Name} does not contain " +
                    $"a {nameof(NetCallAttribute)} attribute.", nameof(method));
            }
            this.RequiresMethod = !registerWithoutMethod;
        }
        public abstract bool Read(byte[] message, out object[] parameters);
        public NetTask Listen(int TimeoutMS = NetTask.DEFAULT_TIMEOUT_MS)
        {
            NetTask task = new NetTask(this, TimeoutMS);
            task.RegisterListener(this);
            return task;
        }
    }
    /// <summary> For querying only </summary>
    public abstract class NetCallRaw : BaseNetCall
    {
        public NetCallRaw(ushort method, bool registerWithoutMethod = false) : base(method, registerWithoutMethod) { }
        public NetCallRaw(Delegate method, bool registerWithoutMethod = false) : base(method, registerWithoutMethod) { }
    }
    /// <summary> For querying only </summary>
    public abstract class DynamicNetCall : BaseNetCall
    {
        public DynamicNetCall(ushort method, bool registerWithoutMethod = false) : base(method, registerWithoutMethod) { }
        public DynamicNetCall(Delegate method, bool registerWithoutMethod = false) : base(method, registerWithoutMethod) { }
    }
    public sealed class NetCall : BaseNetCall
    {
        public delegate void Method(IConnection connection);
        public delegate Task MethodAsync(IConnection connection);
        public NetCall(ushort method, bool registerWithoutMethod = false) : base(method, registerWithoutMethod) { }
        public NetCall(Method method, bool registerWithoutMethod = false) : base(method, registerWithoutMethod) { }
        public NetCall(MethodAsync method, bool registerWithoutMethod = false) : base(method, registerWithoutMethod) { }
        public void Invoke(IConnection connection)
        {
            byte[] id = BitConverter.GetBytes(ID);
            connection.Send(new byte[] { id[0], id[1], 0, 0, 0, 0 });
        }
        public void Invoke(IEnumerable<IConnection> connections)
        {
            foreach (IConnection connection in connections)
            {
                Invoke(connection);
            }
        }
        public bool Read(byte[] message) => true;
        public override bool Read(byte[] message, out object[] parameters)
        {
            parameters = new object[0];
            return true;
        }
        public NetTask Request(BaseNetCall listener, IConnection connection, int TimeoutMS = NetTask.DEFAULT_TIMEOUT_MS)
        {
            NetTask task = listener.Listen(TimeoutMS);
            this.Invoke(connection);
            return task;
        }
    }
    public sealed class NetCallRaw<T> : NetCallRaw
    {
        private readonly ByteReaderRaw<T> _reader;
        private readonly ByteWriterRaw<T> _writer;
        public delegate void Method(IConnection connection, T arg1);
        public delegate Task MethodAsync(IConnection connection, T arg1);
        /// <summary>Leave <paramref name="reader"/> or <paramref name="writer"/> null to auto-fill.</summary>
        public NetCallRaw(ushort method, ByteReader.Reader<T> reader, ByteWriter.Writer<T> writer, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            this._writer = new ByteWriterRaw<T>(method, writer, capacity: capacity);
            this._reader = new ByteReaderRaw<T>(reader);
        }
        /// <summary>Leave <paramref name="reader"/> or <paramref name="writer"/> null to auto-fill.</summary>
        public NetCallRaw(Method method, ByteReader.Reader<T> reader, ByteWriter.Writer<T> writer, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            _writer = new ByteWriterRaw<T>(this.ID, writer, capacity: capacity);
            _reader = new ByteReaderRaw<T>(reader);
        }
        /// <summary>Leave <paramref name="reader"/> or <paramref name="writer"/> null to auto-fill.</summary>
        public NetCallRaw(MethodAsync method, ByteReader.Reader<T> reader, ByteWriter.Writer<T> writer, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            _writer = new ByteWriterRaw<T>(this.ID, writer, capacity: capacity);
            _reader = new ByteReaderRaw<T>(reader);
        }
        public void Invoke(IConnection connection, T arg)
        {
            if (connection == null)
            {
                Logging.LogError($"Error sending method {ID} to null connection.");
                return;
            }
            try
            {
                connection.Send(_writer.Get(arg));
            }
            catch (Exception ex)
            {
                Logging.LogError($"Error sending method {ID} to connection {connection.NetworkID}.");
                Logging.LogError(ex);
            }
        }
        public void Invoke(IEnumerable<IConnection> connections, T arg)
        {
            foreach (IConnection connection in connections)
            {
                Invoke(connection, arg);
            }
        }
        public bool Read(byte[] message, out T arg)
        {
            try
            {
                return _reader.Read(message, out arg);
            }
            catch (Exception ex)
            {
                Logging.LogError($"Error reading method {ID}.");
                Logging.LogError(ex);
                arg = default;
                return false;
            }
        }
        public override bool Read(byte[] message, out object[] parameters)
        {
            bool success = Read(message, out T a1);
            parameters = new object[1] { a1 };
            return success;
        }
        public NetTask Request(BaseNetCall listener, IConnection connection, T arg, int TimeoutMS = NetTask.DEFAULT_TIMEOUT_MS)
        {
            NetTask task = listener.Listen(TimeoutMS);
            this.Invoke(connection, arg);
            return task;
        }
    }
    /// <summary>Leave any reader or writer null to auto-fill.</summary>
    public sealed class NetCallRaw<T1, T2> : NetCallRaw
    {
        private readonly ByteReaderRaw<T1, T2> _reader;
        private readonly ByteWriterRaw<T1, T2> _writer;
        public delegate void Method(IConnection connection, T1 arg1, T2 arg2);
        public delegate Task MethodAsync(IConnection connection, T1 arg1, T2 arg2);
        /// <summary>Leave any of the readers or writers null to auto-fill.</summary>
        public NetCallRaw(ushort method, ByteReader.Reader<T1> reader1, ByteReader.Reader<T2> reader2, ByteWriter.Writer<T1> writer1, ByteWriter.Writer<T2> writer2, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            this._writer = new ByteWriterRaw<T1, T2>(method, writer1, writer2, capacity: capacity);
            this._reader = new ByteReaderRaw<T1, T2>(reader1, reader2);
        }
        /// <summary>Leave any of the readers or writers null to auto-fill.</summary>
        public NetCallRaw(Method method, ByteReader.Reader<T1> reader1, ByteReader.Reader<T2> reader2, ByteWriter.Writer<T1> writer1, ByteWriter.Writer<T2> writer2, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            this._writer = new ByteWriterRaw<T1, T2>(this.ID, writer1, writer2, capacity: capacity);
            this._reader = new ByteReaderRaw<T1, T2>(reader1, reader2);
        }
        /// <summary>Leave any of the readers or writers null to auto-fill.</summary>
        public NetCallRaw(MethodAsync method, ByteReader.Reader<T1> reader1, ByteReader.Reader<T2> reader2, ByteWriter.Writer<T1> writer1, ByteWriter.Writer<T2> writer2, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            this._writer = new ByteWriterRaw<T1, T2>(this.ID, writer1, writer2, capacity: capacity);
            this._reader = new ByteReaderRaw<T1, T2>(reader1, reader2);
        }
        public void Invoke(IConnection connection, T1 arg1, T2 arg2)
        {
            if (connection == null)
            {
                Logging.LogError($"Error sending method {ID} to null connection.");
                return;
            }
            try
            {
                connection.Send(_writer.Get(arg1, arg2));
            }
            catch (Exception ex)
            {
                Logging.LogError($"Error sending method {ID} to connection {connection.NetworkID}.");
                Logging.LogError(ex);
            }
        }
        public void Invoke(IEnumerable<IConnection> connections, T1 arg1, T2 arg2)
        {
            foreach (IConnection connection in connections)
            {
                Invoke(connection, arg1, arg2);
            }
        }
        public bool Read(byte[] message, out T1 arg1, out T2 arg2)
        {
            try
            {
                return _reader.Read(message, out arg1, out arg2);
            }
            catch (Exception ex)
            {
                Logging.LogError($"Error reading method {ID}.");
                Logging.LogError(ex);
                arg1 = default;
                arg2 = default;
                return false;
            }
        }
        public override bool Read(byte[] message, out object[] parameters)
        {
            bool success = Read(message, out T1 a1, out T2 a2);
            parameters = new object[2] { a1, a2 };
            return success;
        }
        public NetTask Request(BaseNetCall listener, IConnection connection, T1 arg1, T2 arg2, int TimeoutMS = NetTask.DEFAULT_TIMEOUT_MS)
        {
            NetTask task = listener.Listen(TimeoutMS);
            this.Invoke(connection, arg1, arg2);
            return task;
        }
    }
    /// <summary>Leave any reader or writer null to auto-fill.</summary>
    public sealed class NetCallRaw<T1, T2, T3> : NetCallRaw
    {
        private readonly ByteReaderRaw<T1, T2, T3> _reader;
        private readonly ByteWriterRaw<T1, T2, T3> _writer;
        public delegate void Method(IConnection connection, T1 arg1, T2 arg2, T3 arg3);
        public delegate Task MethodAsync(IConnection connection, T1 arg1, T2 arg2, T3 arg3);
        /// <summary>Leave any of the readers or writers null to auto-fill.</summary>
        public NetCallRaw(ushort method, ByteReader.Reader<T1> reader1, ByteReader.Reader<T2> reader2, ByteReader.Reader<T3> reader3, ByteWriter.Writer<T1> writer1, ByteWriter.Writer<T2> writer2, ByteWriter.Writer<T3> writer3, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            this._writer = new ByteWriterRaw<T1, T2, T3>(method, writer1, writer2, writer3, capacity: capacity);
            this._reader = new ByteReaderRaw<T1, T2, T3>(reader1, reader2, reader3);
        }
        /// <summary>Leave any of the readers or writers null to auto-fill.</summary>
        public NetCallRaw(Method method, ByteReader.Reader<T1> reader1, ByteReader.Reader<T2> reader2, ByteReader.Reader<T3> reader3, ByteWriter.Writer<T1> writer1, ByteWriter.Writer<T2> writer2, ByteWriter.Writer<T3> writer3, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            this._writer = new ByteWriterRaw<T1, T2, T3>(this.ID, writer1, writer2, writer3, capacity: capacity);
            this._reader = new ByteReaderRaw<T1, T2, T3>(reader1, reader2, reader3);
        }
        /// <summary>Leave any of the readers or writers null to auto-fill.</summary>
        public NetCallRaw(MethodAsync method, ByteReader.Reader<T1> reader1, ByteReader.Reader<T2> reader2, ByteReader.Reader<T3> reader3, ByteWriter.Writer<T1> writer1, ByteWriter.Writer<T2> writer2, ByteWriter.Writer<T3> writer3, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            this._writer = new ByteWriterRaw<T1, T2, T3>(this.ID, writer1, writer2, writer3, capacity: capacity);
            this._reader = new ByteReaderRaw<T1, T2, T3>(reader1, reader2, reader3);
        }
        public void Invoke(IConnection connection, T1 arg1, T2 arg2, T3 arg3)
        {
            if (connection == null)
            {
                Logging.LogError($"Error sending method {ID} to null connection.");
                return;
            }
            try
            {
                connection.Send(_writer.Get(arg1, arg2, arg3));
            }
            catch (Exception ex)
            {
                Logging.LogError($"Error sending method {ID} to connection {connection.NetworkID}.");
                Logging.LogError(ex);
            }
        }
        public void Invoke(IEnumerable<IConnection> connections, T1 arg1, T2 arg2, T3 arg3)
        {
            foreach (IConnection connection in connections)
            {
                Invoke(connection, arg1, arg2, arg3);
            }
        }
        public bool Read(byte[] message, out T1 arg1, out T2 arg2, out T3 arg3)
        {
            try
            {
                return _reader.Read(message, out arg1, out arg2, out arg3);
            }
            catch (Exception ex)
            {
                Logging.LogError($"Error reading method {ID}.");
                Logging.LogError(ex);
                arg1 = default;
                arg2 = default;
                arg3 = default;
                return false;
            }
        }
        public override bool Read(byte[] message, out object[] parameters)
        {
            bool success = Read(message, out T1 a1, out T2 a2, out T3 a3);
            parameters = new object[3] { a1, a2, a3 };
            return success;
        }
        public NetTask Request(BaseNetCall listener, IConnection connection, T1 arg1, T2 arg2, T3 arg3, int TimeoutMS = NetTask.DEFAULT_TIMEOUT_MS)
        {
            NetTask task = listener.Listen(TimeoutMS);
            this.Invoke(connection, arg1, arg2, arg3);
            return task;
        }
    }
    /// <summary>Leave any reader or writer null to auto-fill.</summary>
    public sealed class NetCallRaw<T1, T2, T3, T4> : NetCallRaw
    {
        private readonly ByteReaderRaw<T1, T2, T3, T4> _reader;
        private readonly ByteWriterRaw<T1, T2, T3, T4> _writer;
        public delegate void Method(IConnection connection, T1 arg1, T2 arg2, T3 arg3, T4 arg4);
        public delegate Task MethodAsync(IConnection connection, T1 arg1, T2 arg2, T3 arg3, T4 arg4);
        /// <summary>Leave any of the readers or writers null to auto-fill.</summary>
        public NetCallRaw(ushort method, ByteReader.Reader<T1> reader1, ByteReader.Reader<T2> reader2, ByteReader.Reader<T3> reader3, ByteReader.Reader<T4> reader4, ByteWriter.Writer<T1> writer1, ByteWriter.Writer<T2> writer2, ByteWriter.Writer<T3> writer3, ByteWriter.Writer<T4> writer4, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            this._writer = new ByteWriterRaw<T1, T2, T3, T4>(method, writer1, writer2, writer3, writer4, capacity: capacity);
            this._reader = new ByteReaderRaw<T1, T2, T3, T4>(reader1, reader2, reader3, reader4);
        }
        /// <summary>Leave any of the readers or writers null to auto-fill.</summary>
        public NetCallRaw(Method method, ByteReader.Reader<T1> reader1, ByteReader.Reader<T2> reader2, ByteReader.Reader<T3> reader3, ByteReader.Reader<T4> reader4, ByteWriter.Writer<T1> writer1, ByteWriter.Writer<T2> writer2, ByteWriter.Writer<T3> writer3, ByteWriter.Writer<T4> writer4, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            this._writer = new ByteWriterRaw<T1, T2, T3, T4>(this.ID, writer1, writer2, writer3, writer4, capacity: capacity);
            this._reader = new ByteReaderRaw<T1, T2, T3, T4>(reader1, reader2, reader3, reader4);
        }
        /// <summary>Leave any of the readers or writers null to auto-fill.</summary>
        public NetCallRaw(MethodAsync method, ByteReader.Reader<T1> reader1, ByteReader.Reader<T2> reader2, ByteReader.Reader<T3> reader3, ByteReader.Reader<T4> reader4, ByteWriter.Writer<T1> writer1, ByteWriter.Writer<T2> writer2, ByteWriter.Writer<T3> writer3, ByteWriter.Writer<T4> writer4, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            this._writer = new ByteWriterRaw<T1, T2, T3, T4>(this.ID, writer1, writer2, writer3, writer4, capacity: capacity);
            this._reader = new ByteReaderRaw<T1, T2, T3, T4>(reader1, reader2, reader3, reader4);
        }
        public void Invoke(IConnection connection, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (connection == null)
            {
                Logging.LogError($"Error sending method {ID} to null connection.");
                return;
            }
            try
            {
                connection.Send(_writer.Get(arg1, arg2, arg3, arg4));
            }
            catch (Exception ex)
            {
                Logging.LogError($"Error sending method {ID} to connection {connection.NetworkID}.");
                Logging.LogError(ex);
            }
        }
        public void Invoke(IEnumerable<IConnection> connections, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            foreach (IConnection connection in connections)
            {
                Invoke(connection, arg1, arg2, arg3, arg4);
            }
        }
        public bool Read(byte[] message, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4)
        {
            try
            {
                return _reader.Read(message, out arg1, out arg2, out arg3, out arg4);
            }
            catch (Exception ex)
            {
                Logging.LogError($"Error reading method {ID}.");
                Logging.LogError(ex);
                arg1 = default;
                arg2 = default;
                arg3 = default;
                arg4 = default;
                return false;
            }
        }
        public override bool Read(byte[] message, out object[] parameters)
        {
            bool success = Read(message, out T1 a1, out T2 a2, out T3 a3, out T4 a4);
            parameters = new object[4] { a1, a2, a3, a4 };
            return success;
        }
        public NetTask Request(BaseNetCall listener, IConnection connection, T1 arg1, T2 arg2, T3 arg3, T4 arg4, int TimeoutMS = NetTask.DEFAULT_TIMEOUT_MS)
        {
            NetTask task = listener.Listen(TimeoutMS);
            this.Invoke(connection, arg1, arg2, arg3, arg4);
            return task;
        }
    }
    public sealed class NetCall<T> : DynamicNetCall
    {
        private readonly DynamicByteReader<T> _reader;
        private readonly DynamicByteWriter<T> _writer;
        public delegate void Method(IConnection connection, T arg1);
        public delegate Task MethodAsync(IConnection connection, T arg1);
        public NetCall(ushort method, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            _reader = new DynamicByteReader<T>();
            _writer = new DynamicByteWriter<T>(method, capacity: capacity);
        }
        public NetCall(Method method, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            _reader = new DynamicByteReader<T>();
            _writer = new DynamicByteWriter<T>(this.ID, capacity: capacity);
        }
        public NetCall(MethodAsync method, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            _reader = new DynamicByteReader<T>();
            _writer = new DynamicByteWriter<T>(this.ID, capacity: capacity);
        }
        public void Invoke(IConnection connection, T arg1)
        {
            if (connection == null)
            {
                Logging.LogError($"Error sending method {ID} to null connection.");
                return;
            }
            try
            {
                connection.Send(_writer.Get(arg1));
            }
            catch (Exception ex)
            {
                Logging.LogError($"Error sending method {ID} to connection {connection.NetworkID}.");
                Logging.LogError(ex);
            }
        }
        public void Invoke(IEnumerable<IConnection> connections, T arg1)
        {
            foreach (IConnection connection in connections)
            {
                Invoke(connection, arg1);
            }
        }
        public bool Read(byte[] message, out T arg1)
        {
            try
            {
                return _reader.Read(message, out arg1);
            }
            catch (Exception ex)
            {
                Logging.LogError($"Error reading method {ID}.");
                Logging.LogError(ex);
                arg1 = default;
                return false;
            }
        }
        public override bool Read(byte[] message, out object[] parameters)
        {
            bool success = Read(message, out T a1);
            parameters = new object[1] { a1 };
            return success;
        }
        public NetTask Request(BaseNetCall listener, IConnection connection, T arg1, int TimeoutMS = NetTask.DEFAULT_TIMEOUT_MS)
        {
            NetTask task = listener.Listen(TimeoutMS);
            this.Invoke(connection, arg1);
            return task;
        }
    }
    public sealed class NetCall<T1, T2> : DynamicNetCall
    {
        private readonly DynamicByteReader<T1, T2> _reader;
        private readonly DynamicByteWriter<T1, T2> _writer;
        public delegate void Method(IConnection connection, T1 arg1, T2 arg2);
        public delegate Task MethodAsync(IConnection connection, T1 arg1, T2 arg2);
        public NetCall(ushort method, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            _reader = new DynamicByteReader<T1, T2>();
            _writer = new DynamicByteWriter<T1, T2>(method, capacity: capacity);
        }
        public NetCall(Method method, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            _reader = new DynamicByteReader<T1, T2>();
            _writer = new DynamicByteWriter<T1, T2>(this.ID, capacity: capacity);
        }
        public NetCall(MethodAsync method, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            _reader = new DynamicByteReader<T1, T2>();
            _writer = new DynamicByteWriter<T1, T2>(this.ID, capacity: capacity);
        }
        public void Invoke(IConnection connection, T1 arg1, T2 arg2)
        {if (connection == null)
            {
                Logging.LogError($"Error sending method {ID} to null connection.");
                return;
            }
            try
            {
                connection.Send(_writer.Get(arg1, arg2));
            }
            catch (Exception ex)
            {
                Logging.LogError($"Error sending method {ID} to connection {connection.NetworkID}.");
                Logging.LogError(ex);
            }
        }
        public void Invoke(IEnumerable<IConnection> connections, T1 arg1, T2 arg2)
        {
            foreach (IConnection connection in connections)
            {
                Invoke(connection, arg1, arg2);
            }
        }
        public bool Read(byte[] message, out T1 arg1, out T2 arg2)
        {
            try
            {
                return _reader.Read(message, out arg1, out arg2);
            }
            catch (Exception ex)
            {
                Logging.LogError($"Error reading method {ID}.");
                Logging.LogError(ex);
                arg1 = default;
                arg2 = default;
                return false;
            }
        }
        public override bool Read(byte[] message, out object[] parameters)
        {
            bool success = Read(message, out T1 a1, out T2 a2);
            parameters = new object[2] { a1, a2 };
            return success;
        }
        public NetTask Request(BaseNetCall listener, IConnection connection, T1 arg1, T2 arg2, int TimeoutMS = NetTask.DEFAULT_TIMEOUT_MS)
        {
            NetTask task = listener.Listen(TimeoutMS);
            this.Invoke(connection, arg1, arg2);
            return task;
        }
    }
    public sealed class NetCall<T1, T2, T3> : DynamicNetCall
    {
        private readonly DynamicByteReader<T1, T2, T3> _reader;
        private readonly DynamicByteWriter<T1, T2, T3> _writer;
        public delegate void Method(IConnection connection, T1 arg1, T2 arg2, T3 arg3);
        public delegate Task MethodAsync(IConnection connection, T1 arg1, T2 arg2, T3 arg3);
        public NetCall(ushort method, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            _reader = new DynamicByteReader<T1, T2, T3>();
            _writer = new DynamicByteWriter<T1, T2, T3>(method, capacity: capacity);
        }
        public NetCall(Method method, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            _reader = new DynamicByteReader<T1, T2, T3>();
            _writer = new DynamicByteWriter<T1, T2, T3>(this.ID, capacity: capacity);
        }
        public NetCall(MethodAsync method, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            _reader = new DynamicByteReader<T1, T2, T3>();
            _writer = new DynamicByteWriter<T1, T2, T3>(this.ID, capacity: capacity);
        }
        public void Invoke(IConnection connection, T1 arg1, T2 arg2, T3 arg3)
        {
            if (connection == null)
            {
                Logging.LogError($"Error sending method {ID} to null connection.");
                return;
            }
            try
            {
                connection.Send(_writer.Get(arg1, arg2, arg3));
            }
            catch (Exception ex)
            {
                Logging.LogError($"Error sending method {ID} to connection {connection.NetworkID}.");
                Logging.LogError(ex);
            }
        }
        public void Invoke(IEnumerable<IConnection> connections, T1 arg1, T2 arg2, T3 arg3)
        {
            foreach (IConnection connection in connections)
            {
                Invoke(connection, arg1, arg2, arg3);
            }
        }
        public bool Read(byte[] message, out T1 arg1, out T2 arg2, out T3 arg3)
        {
            try
            {
                return _reader.Read(message, out arg1, out arg2, out arg3);
            }
            catch (Exception ex)
            {
                Logging.LogError($"Error reading method {ID}.");
                Logging.LogError(ex);
                arg1 = default;
                arg2 = default;
                arg3 = default;
                return false;
            }
        }
        public override bool Read(byte[] message, out object[] parameters)
        {
            bool success = Read(message, out T1 arg1, out T2 arg2, out T3 arg3);
            parameters = new object[3] { arg1, arg2, arg3 };
            return success;
        }
        public NetTask Request(BaseNetCall listener, IConnection connection, T1 arg1, T2 arg2, T3 arg3, int TimeoutMS = NetTask.DEFAULT_TIMEOUT_MS)
        {
            NetTask task = listener.Listen(TimeoutMS);
            this.Invoke(connection, arg1, arg2, arg3);
            return task;
        }
    }
    public sealed class NetCall<T1, T2, T3, T4> : DynamicNetCall
    {
        private readonly DynamicByteReader<T1, T2, T3, T4> _reader;
        private readonly DynamicByteWriter<T1, T2, T3, T4> _writer;
        public delegate void Method(IConnection connection, T1 arg1, T2 arg2, T3 arg3, T4 arg4);
        public delegate Task MethodAsync(IConnection connection, T1 arg1, T2 arg2, T3 arg3, T4 arg4);
        public NetCall(ushort method, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            _reader = new DynamicByteReader<T1, T2, T3, T4>();
            _writer = new DynamicByteWriter<T1, T2, T3, T4>(method, capacity: capacity);
        }
        public NetCall(Method method, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            _reader = new DynamicByteReader<T1, T2, T3, T4>();
            _writer = new DynamicByteWriter<T1, T2, T3, T4>(this.ID, capacity: capacity);
        }
        public NetCall(MethodAsync method, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            _reader = new DynamicByteReader<T1, T2, T3, T4>();
            _writer = new DynamicByteWriter<T1, T2, T3, T4>(this.ID, capacity: capacity);
        }
        public void Invoke(IConnection connection, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (connection == null)
            {
                Logging.LogError($"Error sending method {ID} to null connection.");
                return;
            }
            try
            {
                connection.Send(_writer.Get(arg1, arg2, arg3, arg4));
            }
            catch (Exception ex)
            {
                Logging.LogError($"Error sending method {ID} to connection {connection.NetworkID}.");
                Logging.LogError(ex);
            }
        }
        public void Invoke(IEnumerable<IConnection> connections, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            foreach (IConnection connection in connections)
            {
                Invoke(connection, arg1, arg2, arg3, arg4);
            }
        }
        public bool Read(byte[] message, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4)
        {
            try
            {
                return _reader.Read(message, out arg1, out arg2, out arg3, out arg4);
            }
            catch (Exception ex)
            {
                Logging.LogError($"Error reading method {ID}.");
                Logging.LogError(ex);
                arg1 = default;
                arg2 = default;
                arg3 = default;
                arg4 = default;
                return false;
            }
        }
        public override bool Read(byte[] message, out object[] parameters)
        {
            bool success = Read(message, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4);
            parameters = new object[4] { arg1, arg2, arg3, arg4 };
            return success;
        }
        public NetTask Request(BaseNetCall listener, IConnection connection, T1 arg1, T2 arg2, T3 arg3, T4 arg4, int TimeoutMS = NetTask.DEFAULT_TIMEOUT_MS)
        {
            NetTask task = listener.Listen(TimeoutMS);
            this.Invoke(connection, arg1, arg2, arg3, arg4);
            return task;
        }
    }
    public sealed class NetCall<T1, T2, T3, T4, T5> : DynamicNetCall
    {
        private readonly DynamicByteReader<T1, T2, T3, T4, T5> _reader;
        private readonly DynamicByteWriter<T1, T2, T3, T4, T5> _writer;
        public delegate void Method(IConnection connection, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
        public delegate Task MethodAsync(IConnection connection, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
        public NetCall(ushort method, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            _reader = new DynamicByteReader<T1, T2, T3, T4, T5>();
            _writer = new DynamicByteWriter<T1, T2, T3, T4, T5>(method, capacity: capacity);
        }
        public NetCall(Method method, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            _reader = new DynamicByteReader<T1, T2, T3, T4, T5>();
            _writer = new DynamicByteWriter<T1, T2, T3, T4, T5>(this.ID, capacity: capacity);
        }
        public NetCall(MethodAsync method, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            _reader = new DynamicByteReader<T1, T2, T3, T4, T5>();
            _writer = new DynamicByteWriter<T1, T2, T3, T4, T5>(this.ID, capacity: capacity);
        }
        public void Invoke(IConnection connection, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            if (connection == null)
            {
                Logging.LogError($"Error sending method {ID} to null connection.");
                return;
            }
            try
            {
                connection.Send(_writer.Get(arg1, arg2, arg3, arg4, arg5));
            }
            catch (Exception ex)
            {
                Logging.LogError($"Error sending method {ID} to connection {connection.NetworkID}.");
                Logging.LogError(ex);
            }
        }
        public void Invoke(IEnumerable<IConnection> connections, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            foreach (IConnection connection in connections)
            {
                Invoke(connection, arg1, arg2, arg3, arg4, arg5);
            }
        }
        public bool Read(byte[] message, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5)
        {
            try
            {
                return _reader.Read(message, out arg1, out arg2, out arg3, out arg4, out arg5);
            }
            catch (Exception ex)
            {
                Logging.LogError($"Error reading method {ID}.");
                Logging.LogError(ex);
                arg1 = default;
                arg2 = default;
                arg3 = default;
                arg4 = default;
                arg5 = default;
                return false;
            }
        }
        public override bool Read(byte[] message, out object[] parameters)
        {
            bool success = Read(message, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5);
            parameters = new object[5] { arg1, arg2, arg3, arg4, arg5 };
            return success;
        }
        public NetTask Request(BaseNetCall listener, IConnection connection, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, int TimeoutMS = NetTask.DEFAULT_TIMEOUT_MS)
        {
            NetTask task = listener.Listen(TimeoutMS);
            this.Invoke(connection, arg1, arg2, arg3, arg4, arg5);
            return task;
        }
    }
    public sealed class NetCall<T1, T2, T3, T4, T5, T6> : DynamicNetCall
    {
        private readonly DynamicByteReader<T1, T2, T3, T4, T5, T6> _reader;
        private readonly DynamicByteWriter<T1, T2, T3, T4, T5, T6> _writer;
        public delegate void Method(IConnection connection, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
        public delegate Task MethodAsync(IConnection connection, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
        public NetCall(ushort method, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            _reader = new DynamicByteReader<T1, T2, T3, T4, T5, T6>();
            _writer = new DynamicByteWriter<T1, T2, T3, T4, T5, T6>(method, capacity: capacity);
        }
        public NetCall(Method method, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            _reader = new DynamicByteReader<T1, T2, T3, T4, T5, T6>();
            _writer = new DynamicByteWriter<T1, T2, T3, T4, T5, T6>(this.ID, capacity: capacity);
        }
        public NetCall(MethodAsync method, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            _reader = new DynamicByteReader<T1, T2, T3, T4, T5, T6>();
            _writer = new DynamicByteWriter<T1, T2, T3, T4, T5, T6>(this.ID, capacity: capacity);
        }
        public void Invoke(IConnection connection, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            if (connection == null)
            {
                Logging.LogError($"Error sending method {ID} to null connection.");
                return;
            }
            try
            {
                connection.Send(_writer.Get(arg1, arg2, arg3, arg4, arg5, arg6));
            }
            catch (Exception ex)
            {
                Logging.LogError($"Error sending method {ID} to connection {connection.NetworkID}.");
                Logging.LogError(ex);
            }
        }
        public void Invoke(IEnumerable<IConnection> connections, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            foreach (IConnection connection in connections)
            {
                Invoke(connection, arg1, arg2, arg3, arg4, arg5, arg6);
            }
        }
        public bool Read(byte[] message, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5, out T6 arg6)
        {
            try
            {
                return _reader.Read(message, out arg1, out arg2, out arg3, out arg4, out arg5, out arg6);
            }
            catch (Exception ex)
            {
                Logging.LogError($"Error reading method {ID}.");
                Logging.LogError(ex);
                arg1 = default;
                arg2 = default;
                arg3 = default;
                arg4 = default;
                arg5 = default;
                arg6 = default;
                return false;
            }
        }
        public override bool Read(byte[] message, out object[] parameters)
        {
            bool success = Read(message, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5, out T6 arg6);
            parameters = new object[6] { arg1, arg2, arg3, arg4, arg5, arg6 };
            return success;
        }
        public NetTask Request(BaseNetCall listener, IConnection connection, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, int TimeoutMS = NetTask.DEFAULT_TIMEOUT_MS)
        {
            NetTask task = listener.Listen(TimeoutMS);
            this.Invoke(connection, arg1, arg2, arg3, arg4, arg5, arg6);
            return task;
        }
    }
    public sealed class NetCall<T1, T2, T3, T4, T5, T6, T7> : DynamicNetCall
    {
        private readonly DynamicByteReader<T1, T2, T3, T4, T5, T6, T7> _reader;
        private readonly DynamicByteWriter<T1, T2, T3, T4, T5, T6, T7> _writer;
        public delegate void Method(IConnection connection, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);
        public delegate Task MethodAsync(IConnection connection, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);
        public NetCall(ushort method, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            _reader = new DynamicByteReader<T1, T2, T3, T4, T5, T6, T7>();
            _writer = new DynamicByteWriter<T1, T2, T3, T4, T5, T6, T7>(method, capacity: capacity);
        }
        public NetCall(Method method, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            _reader = new DynamicByteReader<T1, T2, T3, T4, T5, T6, T7>();
            _writer = new DynamicByteWriter<T1, T2, T3, T4, T5, T6, T7>(this.ID, capacity: capacity);
        }
        public NetCall(MethodAsync method, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            _reader = new DynamicByteReader<T1, T2, T3, T4, T5, T6, T7>();
            _writer = new DynamicByteWriter<T1, T2, T3, T4, T5, T6, T7>(this.ID, capacity: capacity);
        }
        public void Invoke(IConnection connection, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            if (connection == null)
            {
                Logging.LogError($"Error sending method {ID} to null connection.");
                return;
            }
            try
            {
                connection.Send(_writer.Get(arg1, arg2, arg3, arg4, arg5, arg6, arg7));
            }
            catch (Exception ex)
            {
                Logging.LogError($"Error sending method {ID} to connection {connection.NetworkID}.");
                Logging.LogError(ex);
            }
        }
        public void Invoke(IEnumerable<IConnection> connections, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            foreach (IConnection connection in connections)
            {
                Invoke(connection, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            }
        }
        public bool Read(byte[] message, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5, out T6 arg6, out T7 arg7)
        {
            try
            {
                return _reader.Read(message, out arg1, out arg2, out arg3, out arg4, out arg5, out arg6, out arg7);
            }
            catch (Exception ex)
            {
                Logging.LogError($"Error reading method {ID}.");
                Logging.LogError(ex);
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
        public override bool Read(byte[] message, out object[] parameters)
        {
            bool success = Read(message, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5, out T6 arg6, out T7 arg7);
            parameters = new object[7] { arg1, arg2, arg3, arg4, arg5, arg6, arg7 };
            return success;
        }
        public NetTask Request(BaseNetCall listener, IConnection connection, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, int TimeoutMS = NetTask.DEFAULT_TIMEOUT_MS)
        {
            NetTask task = listener.Listen(TimeoutMS);
            this.Invoke(connection, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            return task;
        }
    }
    public sealed class NetCall<T1, T2, T3, T4, T5, T6, T7, T8> : DynamicNetCall
    {
        private readonly DynamicByteReader<T1, T2, T3, T4, T5, T6, T7, T8> _reader;
        private readonly DynamicByteWriter<T1, T2, T3, T4, T5, T6, T7, T8> _writer;
        public delegate void Method(IConnection connection, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8);
        public delegate Task MethodAsync(IConnection connection, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8);
        public NetCall(ushort method, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            _reader = new DynamicByteReader<T1, T2, T3, T4, T5, T6, T7, T8>();
            _writer = new DynamicByteWriter<T1, T2, T3, T4, T5, T6, T7, T8>(method, capacity: capacity);
        }
        public NetCall(Method method, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            _reader = new DynamicByteReader<T1, T2, T3, T4, T5, T6, T7, T8>();
            _writer = new DynamicByteWriter<T1, T2, T3, T4, T5, T6, T7, T8>(this.ID, capacity: capacity);
        }
        public NetCall(MethodAsync method, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            _reader = new DynamicByteReader<T1, T2, T3, T4, T5, T6, T7, T8>();
            _writer = new DynamicByteWriter<T1, T2, T3, T4, T5, T6, T7, T8>(this.ID, capacity: capacity);
        }
        public void Invoke(IConnection connection, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            if (connection == null)
            {
                Logging.LogError($"Error sending method {ID} to null connection.");
                return;
            }
            try
            {
                connection.Send(_writer.Get(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8));
            }
            catch (Exception ex)
            {
                Logging.LogError($"Error sending method {ID} to connection {connection.NetworkID}.");
                Logging.LogError(ex);
            }
        }
        public void Invoke(IEnumerable<IConnection> connections, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            foreach (IConnection connection in connections)
            {
                Invoke(connection, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
            }
        }
        public bool Read(byte[] message, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5, out T6 arg6, out T7 arg7, out T8 arg8)
        {
            try
            {
                return _reader.Read(message, out arg1, out arg2, out arg3, out arg4, out arg5, out arg6, out arg7, out arg8);
            }
            catch (Exception ex)
            {
                Logging.LogError($"Error reading method {ID}.");
                Logging.LogError(ex);
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
        public override bool Read(byte[] message, out object[] parameters)
        {
            bool success = Read(message, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5, out T6 arg6, out T7 arg7, out T8 arg8);
            parameters = new object[8] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8 };
            return success;
        }
        public NetTask Request(BaseNetCall listener, IConnection connection, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, int TimeoutMS = NetTask.DEFAULT_TIMEOUT_MS)
        {
            NetTask task = listener.Listen(TimeoutMS);
            this.Invoke(connection, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
            return task;
        }
    }
    public sealed class NetCall<T1, T2, T3, T4, T5, T6, T7, T8, T9> : DynamicNetCall
    {
        private readonly DynamicByteReader<T1, T2, T3, T4, T5, T6, T7, T8, T9> _reader;
        private readonly DynamicByteWriter<T1, T2, T3, T4, T5, T6, T7, T8, T9> _writer;
        public delegate void Method(IConnection connection, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9);
        public delegate Task MethodAsync(IConnection connection, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9);
        public NetCall(ushort method, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            _reader = new DynamicByteReader<T1, T2, T3, T4, T5, T6, T7, T8, T9>();
            _writer = new DynamicByteWriter<T1, T2, T3, T4, T5, T6, T7, T8, T9>(method, capacity: capacity);
        }
        public NetCall(Method method, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            _reader = new DynamicByteReader<T1, T2, T3, T4, T5, T6, T7, T8, T9>();
            _writer = new DynamicByteWriter<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this.ID, capacity: capacity);
        }
        public NetCall(MethodAsync method, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            _reader = new DynamicByteReader<T1, T2, T3, T4, T5, T6, T7, T8, T9>();
            _writer = new DynamicByteWriter<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this.ID, capacity: capacity);
        }
        public void Invoke(IConnection connection, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
        {
            if (connection == null)
            {
                Logging.LogError($"Error sending method {ID} to null connection.");
                return;
            }
            try
            {
                connection.Send(_writer.Get(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9));
            }
            catch (Exception ex)
            {
                Logging.LogError($"Error sending method {ID} to connection {connection.NetworkID}.");
                Logging.LogError(ex);
            }
        }
        public void Invoke(IEnumerable<IConnection> connections, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
        {
            foreach (IConnection connection in connections)
            {
                Invoke(connection, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
            }
        }
        public bool Read(byte[] message, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5, out T6 arg6, out T7 arg7, out T8 arg8, out T9 arg9)
        {
            try
            {
                return _reader.Read(message, out arg1, out arg2, out arg3, out arg4, out arg5, out arg6, out arg7, out arg8, out arg9);
            }
            catch (Exception ex)
            {
                Logging.LogError($"Error reading method {ID}.");
                Logging.LogError(ex);
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
        public override bool Read(byte[] message, out object[] parameters)
        {
            bool success = Read(message, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5, out T6 arg6, out T7 arg7, out T8 arg8, out T9 arg9);
            parameters = new object[9] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9 };
            return success;
        }
        public NetTask Request(BaseNetCall listener, IConnection connection, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, int TimeoutMS = NetTask.DEFAULT_TIMEOUT_MS)
        {
            NetTask task = listener.Listen(TimeoutMS);
            this.Invoke(connection, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
            return task;
        }
    }
    public sealed class NetCall<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : DynamicNetCall
    {
        private readonly DynamicByteReader<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> _reader;
        private readonly DynamicByteWriter<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> _writer;
        public delegate void Method(IConnection connection, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10);
        public delegate Task MethodAsync(IConnection connection, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10);
        public NetCall(ushort method, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            _reader = new DynamicByteReader<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>();
            _writer = new DynamicByteWriter<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(method, capacity: capacity);
        }
        public NetCall(Method method, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            _reader = new DynamicByteReader<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>();
            _writer = new DynamicByteWriter<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this.ID, capacity: capacity);
        }
        public NetCall(MethodAsync method, int capacity = 0, bool registerWithoutMethod = false) : base(method, registerWithoutMethod)
        {
            _reader = new DynamicByteReader<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>();
            _writer = new DynamicByteWriter<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this.ID, capacity: capacity);
        }
        public void Invoke(IConnection connection, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
        {
            if (connection == null)
            {
                Logging.LogError($"Error sending method {ID} to null connection.");
                return;
            }
            try
            {
                connection.Send(_writer.Get(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10));
            }
            catch (Exception ex)
            {
                Logging.LogError($"Error sending method {ID} to connection {connection.NetworkID}.");
                Logging.LogError(ex);
            }
        }
        public void Invoke(IEnumerable<IConnection> connections, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
        {
            foreach (IConnection connection in connections)
            {
                Invoke(connection, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
            }
        }
        public bool Read(byte[] message, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5, out T6 arg6, out T7 arg7, out T8 arg8, out T9 arg9, out T10 arg10)
        {
            try
            {
                return _reader.Read(message, out arg1, out arg2, out arg3, out arg4, out arg5, out arg6, out arg7, out arg8, out arg9, out arg10);
            }
            catch (Exception ex)
            {
                Logging.LogError($"Error reading method {ID}.");
                Logging.LogError(ex);
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
        public override bool Read(byte[] message, out object[] parameters)
        {
            bool success = Read(message, out T1 arg1, out T2 arg2, out T3 arg3, out T4 arg4, out T5 arg5, out T6 arg6, out T7 arg7, out T8 arg8, out T9 arg9, out T10 arg10);
            parameters = new object[10] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10 };
            return success;
        }
        public NetTask Request(BaseNetCall listener, IConnection connection, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, int TimeoutMS = NetTask.DEFAULT_TIMEOUT_MS)
        {
            NetTask task = listener.Listen(TimeoutMS);
            this.Invoke(connection, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
            return task;
        }
    }
}

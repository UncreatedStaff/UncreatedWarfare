using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Networking.Encoding;
using UnityEngine;

namespace Uncreated.Networking
{
    public class NetFactory : IDisposable
    {
        public static NetFactory Instance;
        private readonly Dictionary<ushort, MethodInfo> _registry = new Dictionary<ushort, MethodInfo>();
        private readonly Dictionary<ushort, BaseNetCall> _invokers = new Dictionary<ushort, BaseNetCall>();
        private readonly Dictionary<ushort, List<NetTask>> _listeners = new Dictionary<ushort, List<NetTask>>();
        private bool ParseInternal(byte[] message, IConnection connection)
        {
            ushort id = BitConverter.ToUInt16(message, 0);
            if (id == 0)
            {
                Logging.LogError($"{message.Length} byte message read as 0.");
                return false;
            }
            bool a;
            int size = BitConverter.ToInt32(message, sizeof(ushort));
            byte[] data = new byte[size];
            Buffer.BlockCopy(message, sizeof(ushort) + sizeof(int), data, 0, size);
            if (_invokers.TryGetValue(id, out BaseNetCall call))
            {
                if (call == null || !_registry.TryGetValue(id, out MethodInfo method) || method == null)
                {
                    if (_listeners.TryGetValue(id, out List<NetTask> listeners))
                    {
                        bool read = call.Read(data, out object[] parameters);
                        if (!read)
                        {
                            Logging.LogWarning($"Unable to read incomming message for message type {id}\n{string.Join(", ", data)}.");
                            a = false;
                        }
                        else
                        {
                            object[] newparams = new object[parameters.Length + 1];
                            newparams[0] = connection;
                            Array.Copy(parameters, 0, newparams, 1, parameters.Length);
                            for (int i = 0; i < listeners.Count; i++)
                            {
                                if (!listeners[i].isCompleted)
                                    listeners[i].TellCompleted(newparams);
                            }
                            listeners.Clear();
                            _listeners.Remove(id);
                            a = true;
                        }
                    }
                    else
                    {
                        a = false;
                        Logging.LogWarning($"Unable to find method matching ID {id}.");
                    }
                }
                else
                {
                    bool read = call.Read(data, out object[] parameters);
                    if (!read)
                    {
                        Logging.LogWarning($"Unable to read incomming message for message type {id}\n{string.Join(", ", data)}.");
                        a = false;
                    }
                    else
                    {
                        try
                        {
                            object[] newparams = new object[parameters.Length + 1];
                            newparams[0] = connection;
                            Array.Copy(parameters, 0, newparams, 1, parameters.Length);
                            try
                            {
                                object b = method.Invoke(null, newparams);
                                if (b is Task task) task.ConfigureAwait(false);
                            }
                            catch (TargetInvocationException ex)
                            {
                                Logging.LogError($"Error invoking target message {id}: ");
                                Logging.LogError(ex.InnerException ?? ex);
                                a = true;
                            }
                            catch (Exception ex)
                            {
                                Logging.LogError($"Error executing message {id}: ");
                                Logging.LogError(ex);
                                a = true;
                            }
                            if (_listeners.TryGetValue(id, out List<NetTask> listeners))
                            {
                                for (int i = 0; i < listeners.Count; i++)
                                {
                                    if (!listeners[i].isCompleted)
                                        listeners[i].TellCompleted(newparams);
                                }
                                listeners.Clear();
                                _listeners.Remove(id);
                            }
                            a = true;
                        }
                        catch (Exception ex)
                        {
                            Logging.LogError($"Error executing message {id}: ");
                            Logging.LogError(ex);
                            a = true;
                        }
                    }
                }
            }
            else
            {
                Logging.LogError($"No invoker found for message {id}.");
                a = false;
            }
            // in case two messages get combined in one listen
            int msg1size = size + sizeof(ushort) + sizeof(int);
            if (msg1size < message.Length)
            {
                byte[] nextmsg = new byte[message.Length - msg1size];
                Buffer.BlockCopy(message, msg1size, nextmsg, 0, nextmsg.Length);
                return ParseInternal(nextmsg, connection);
            }
            else
            {
                return a;
            }
        }
        internal void RegisterListener(NetTask task, BaseNetCall caller)
        {
            if (_listeners.TryGetValue(caller.ID, out List<NetTask> listeners))
            {
                listeners.Add(task);
            } else
            {
                _listeners.Add(caller.ID, new List<NetTask>(1) { task });
            }
        }
        internal void TimeoutListener(NetTask task, BaseNetCall caller)
        {
            if (_listeners.TryGetValue(caller.ID, out List<NetTask> listeners))
            {
                listeners.Remove(task);
            }
        }
        public static bool Parse(byte[] message, IConnection connection) => Instance.ParseInternal(message, connection);
        public NetFactory(ENetCall side)
        {
            if (Instance != null)
            {
                Instance.Dispose();
            }
            Instance = this;
            RegisterNetMethodsInternal(Assembly.GetExecutingAssembly(), side);
        }
        public static void ClearRegistry()
        {
            Instance._invokers.Clear();
            Instance._registry.Clear();
        }
        public static void RegisterNetMethods(Assembly assembly, ENetCall search) 
        {
            if (Instance == null)
                new NetFactory(search);
            Instance.RegisterNetMethodsInternal(assembly, search);
        }
        private void RegisterNetMethodsInternal(Assembly assembly, ENetCall search)
        {
            int invCount = _invokers.Count;
            int methodCount = _registry.Count;
            Type[] types;
            try
            {
                types = assembly.GetTypes();//.Where(x => !x.IsNested).ToArray();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t != null).ToArray();
                Logging.LogWarning("The following errors were encountered finding the types in assembly " + assembly.FullName + ": ");
                for (int i = 0; i < ex.LoaderExceptions.Count(); i++)
                    Logging.LogWarning(ex.LoaderExceptions[i].Message);
                Logging.LogWarning("Net method registration will continue without the errored types, perhaps an assembly reference is missing that is used in that type.");
            }
            int duplicates = 0;
            int duplicateinvokers = 0;
            for (int t = 0; t < types.Length; t++)
            {
                MethodInfo[] methods = types[t].GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
                for (int m = methods.Length - 1; m >= 0; m--)
                {
                    if (!methods[m].IsStatic) continue;
                    Attribute at = Attribute.GetCustomAttribute(methods[m], typeof(NetCallAttribute));
                    if (at is NetCallAttribute netcall && (netcall.Type == search || netcall.Type == ENetCall.FROM_EITHER))
                    {
                        netcall.MethodName = methods[m].Name;
                        if (_registry.TryGetValue(netcall.MethodID, out MethodInfo duplicate))
                        {
                            Logging.LogWarning($"Duplicate method id between method \"{duplicate.DeclaringType.FullName}.{duplicate.Name}\" ({duplicate.GetHashCode()}) " +
                                $"and \"{methods[m].DeclaringType.FullName}.{methods[m].Name}\" ({methods[m].GetHashCode()}). ({netcall.MethodID})");
                            duplicates++;
                        }
                        else
                        {
                            ParameterInfo[] p = methods[m].GetParameters();
                            if (p.Length < 1)
                            {
                                Logging.LogWarning($"Method \"{methods[m].DeclaringType.FullName}.{methods[m].Name}\" has the wrong parameters to be a net method. The first parameter must be: {nameof(IConnection)}.");
                            } 
                            else if (p[0].ParameterType.Name == nameof(IConnection) + "&" || p[0].ParameterType == typeof(IConnection))
                            {
                                _registry.Add(netcall.MethodID, methods[m]);
                            }
                            else
                            {
                                Logging.LogWarning($"Method \"{methods[m].DeclaringType.FullName}.{methods[m].Name}\" has the wrong parameters to be a net method. The first parameter must be: {nameof(IConnection)}.");
                            }
                        }
                    }
                }
                FieldInfo[] fields = types[t].GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static).Where(x => !x.IsSpecialName).ToArray();
                for (int f = 0; f < fields.Length; f++)
                {
                    if (fields[f].FieldType.IsSubclassOf(typeof(BaseNetCall)))
                    {
                        if (fields[f].GetValue(null) is BaseNetCall call && call != null)
                        {
                            if (_invokers.ContainsKey(call.ID))
                            {
                                Logging.LogWarning($"Duplicate method id in invoker \"{fields[f].DeclaringType.FullName}.{fields[f].Name}\". ({call.ID})");
                                duplicateinvokers++;
                            } else if (_registry.TryGetValue(call.ID, out MethodInfo info))
                            {
                                Type calltype = call.GetType();
                                ParameterInfo[] p = info.GetParameters();
                                if (calltype == typeof(NetCall) && p.Length == 1)
                                {
                                    if (p.Length == 1)
                                    {
                                        _invokers.Add(call.ID, call);
                                    }
                                    else
                                    {
                                        Logging.LogWarning($"Method \"{info.DeclaringType.FullName}.{info.Name}\" has the wrong parameters for invoker \"{fields[f].DeclaringType.FullName}.{fields[f].Name}\" ({nameof(IConnection)}).");
                                        _registry.Remove(call.ID);
                                    }
                                } else if (calltype.IsSubclassOf(typeof(NetCallRaw)))
                                {
                                    Type[] generics = calltype.GetGenericArguments();
                                    if (generics.Length == 1)
                                    {
                                        if (p.Length == 2 && generics.Length > 0 && p[1].ParameterType == generics[0])
                                        {
                                            _invokers.Add(call.ID, call);
                                        }
                                        else
                                        {
                                            Logging.LogWarning($"Method \"{info.DeclaringType.FullName}.{info.Name}\" has the wrong parameters for invoker \"{fields[f].DeclaringType.FullName}.{fields[f].Name}\" " +
                                                $"({nameof(IConnection)}, {(generics.Length > 0 ? generics[0].Name : "?")}).");
                                            _invokers.Add(call.ID, call);
                                        }
                                    } else if (p.Length - 1 == generics.Length)
                                    {
                                        bool good = true;
                                        for (int i = 0; i < generics.Length; i++)
                                        {
                                            if (p[i + 1].ParameterType != generics[i])
                                            {
                                                good = false;
                                                break;
                                            }
                                        }
                                        _invokers.Add(call.ID, call);
                                        if (!good)
                                        {
                                            Logging.LogWarning($"Method \"{info.DeclaringType.FullName}.{info.Name}\" has the wrong parameters for invoker \"{fields[f].DeclaringType.FullName}.{fields[f].Name}\" " +
                                                $"({nameof(IConnection)}, {string.Join(", ", generics.Select(x => x.Name))}).");
                                        }
                                    }
                                    else
                                    {
                                        Logging.LogWarning($"Method \"{info.DeclaringType.FullName}.{info.Name}\" has the wrong parameters for invoker \"{fields[f].DeclaringType.FullName}.{fields[f].Name}\" " +
                                            $"({nameof(IConnection)}, {(generics.Length < 1 ? "?" : string.Join(", ", generics.Select(x => x.Name)))}).");
                                        _invokers.Add(call.ID, call);
                                    }
                                }
                                else if (calltype.IsSubclassOf(typeof(DynamicNetCall)))
                                {
                                    Type[] generics = calltype.GetGenericArguments();

                                    if (p.Length - 1 == generics.Length)
                                    {
                                        bool good = true;
                                        for (int i = 0; i < generics.Length; i++)
                                        {
                                            if (p[i + 1].ParameterType != generics[i])
                                            {
                                                good = false;
                                                break;
                                            }
                                        }
                                        _invokers.Add(call.ID, call);
                                        if (!good)
                                        {
                                            Logging.LogWarning($"Method \"{info.DeclaringType.FullName}.{info.Name}\" has the wrong parameters for invoker \"{fields[f].DeclaringType.FullName}.{fields[f].Name}\" " +
                                                $"({nameof(IConnection)}, {string.Join(", ", generics.Select(x => x.Name))}).");
                                        }
                                    }
                                    else
                                    {
                                        Logging.LogWarning($"Method \"{info.DeclaringType.FullName}.{info.Name}\" has the wrong parameters for invoker \"{fields[f].DeclaringType.FullName}.{fields[f].Name}\" " +
                                            $"({nameof(IConnection)}, {string.Join(", ", generics.Select(x => x.Name))}).");
                                        _invokers.Add(call.ID, call);
                                    }
                                } 
                                else
                                {
                                    _invokers.Add(call.ID, call);
                                }
                            }
                            else
                            {
                                _invokers.Add(call.ID, call);
                            }
                        }
                    }
                }
            }
            methodCount = _registry.Count - methodCount;
            invCount = _invokers.Count - invCount;
            Logging.Log($"[{assembly.GetName().Name}] - {methodCount} network method{S(methodCount)} registered.{(duplicates > 0 ? $"\n!$! - {duplicates} duplicate id{S(duplicates)} found, check warning{S(duplicates)} above - !$!" : string.Empty)}", ConsoleColor.DarkYellow);
            Logging.Log($"[{assembly.GetName().Name}] - {invCount} network invoker{S(invCount)} registered.{(duplicateinvokers > 0 ? $"\n!$! - {duplicateinvokers} duplicate id{S(duplicateinvokers)} found, check warning{S(duplicateinvokers)} above - !$!" : string.Empty)}", ConsoleColor.DarkYellow);
        }
        static string S<T>(T n) where T : IComparable => n.CompareTo(1) == 0 ? string.Empty : "s";
        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _registry.Clear();
                    _invokers.Clear();
                }
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal static readonly Type[] ValidTypes = new Type[]
        {
            typeof(ulong), typeof(float), typeof(long), typeof(ushort), typeof(short), typeof(byte), typeof(int), typeof(uint), typeof(bool), typeof(char), typeof(sbyte), typeof(double),
            typeof(string), typeof(decimal), typeof(DateTime), typeof(TimeSpan), typeof(Guid), typeof(Vector2), typeof(Vector3), typeof(Vector4), typeof(Quaternion), typeof(Color),
            typeof(Color32)
        };
        internal static readonly Type[] ValidArrayTypes = new Type[]
        {
            typeof(ulong), typeof(float), typeof(long), typeof(ushort), typeof(short), typeof(byte), typeof(int), typeof(uint), typeof(bool), typeof(sbyte), typeof(decimal), typeof(char),
            typeof(double), typeof(string)
        };
        internal static bool IsValidAutoType(Type type)
        {
            if (type.IsEnum) return true;
            if (type.IsArray)
            {
                type = type.GetElementType();
                return ValidArrayTypes.Contains(type);
            }
            return ValidTypes.Contains(type) || type.GetInterfaces().Contains(typeof(IReadWrite));
        }
    }
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class NetCallAttribute : Attribute
    {
        readonly ENetCall type;
        readonly ushort methodId;
        public NetCallAttribute(ENetCall type, ushort methodId)
        {
            this.type = type;
            this.methodId = methodId;
        }
        public ENetCall Type { get => type; }
        public string MethodName { get; internal set; }
        public ushort MethodID { get => methodId; }
    }
    public enum ENetCall : byte
    {
        FROM_SERVER = 0,
        FROM_CLIENT = 1,
        FROM_EITHER = 2,
        NONE = 3
    }
}

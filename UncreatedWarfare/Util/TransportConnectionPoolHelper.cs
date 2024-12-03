using DanielWillett.ReflectionTools.Emit;
using System;
using System.Reflection;

namespace Uncreated.Warfare.Util;

public static class TransportConnectionPoolHelper
{
    private static Func<PooledTransportConnectionList>? _pullFromTransportConnectionListPool;

    private static Func<int, PooledTransportConnectionList>? _fallbackCreateNewList;

    /// <summary>
    /// Either create a new list or get it from the pool if on the game thread.
    /// </summary>
    public static PooledTransportConnectionList Claim(int capacity = -1)
    {
        if (!GameThread.IsCurrent)
        {
            return FallbackCreateConnectionList(capacity <= 0 ? 32 : capacity);
        }

        if (_pullFromTransportConnectionListPool is not null)
        {
            return _pullFromTransportConnectionListPool();
        }

        CreatePoolDelegate();

        return _pullFromTransportConnectionListPool is null
            ? FallbackCreateConnectionList(capacity <= 0 ? 32 : capacity)
            : _pullFromTransportConnectionListPool();
    }

    private static PooledTransportConnectionList FallbackCreateConnectionList(int capacity)
    {
        if (_fallbackCreateNewList is not null)
        {
            return _fallbackCreateNewList(capacity);
        }

        CreateFallbackMethod();

        if (_fallbackCreateNewList is null)
            throw new Exception("Unable to create fallback transport connection handler.");

        return _fallbackCreateNewList(capacity);
    }

    private static void CreateFallbackMethod()
    {
        ConstructorInfo? ctor = typeof(PooledTransportConnectionList).GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, [typeof(int)], null);
        if (ctor == null)
        {
            throw new AggregateException("Unable to create pooled transport connection, constructor not found.");
        }

        DynamicMethodInfo<Func<int, PooledTransportConnectionList>> mtd = DynamicMethodHelper.Create<Func<int, PooledTransportConnectionList>>("CreatePooledList", typeof(TransportConnectionPoolHelper), initLocals: false);

        mtd.DefineParameter(1, ParameterAttributes.None, "capacity");

        IOpCodeEmitter emitter = mtd.GetEmitter(streamSize: 32);

        emitter.LoadArgument(0);
        emitter.CreateObject(ctor);
        emitter.Return();

        _fallbackCreateNewList = mtd.CreateDelegate();
    }

    private static void CreatePoolDelegate()
    {
        try
        {
            MethodInfo? method = typeof(Provider).Assembly
                .GetType("SDG.Unturned.TransportConnectionListPool", true, false)?.GetMethod("Get",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (method != null)
            {
                _pullFromTransportConnectionListPool = (Func<PooledTransportConnectionList>)method.CreateDelegate(typeof(Func<PooledTransportConnectionList>));
            }
            else
            {
                WarfareModule.Singleton.GlobalLogger.LogWarning("Couldn't find Get in TransportConnectionListPool, list pooling will not be used.");
            }
        }
        catch (Exception ex)
        {
            WarfareModule.Singleton.GlobalLogger.LogWarning(ex, "Couldn't get Get from TransportConnectionListPool, list pooling will not be used.");
        }
    }
}

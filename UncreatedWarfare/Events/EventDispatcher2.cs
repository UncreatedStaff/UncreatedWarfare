using Cysharp.Threading.Tasks;
using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using Microsoft.Extensions.DependencyInjection;
using SDG.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Uncreated.Warfare.Services;

namespace Uncreated.Warfare.Events;

/// <summary>
/// Handles dispatching <see cref="IEventListener{TEventArgs}"/> and <see cref="IAsyncEventListener{TEventArgs}"/> objects.
/// </summary>
public partial class EventDispatcher2 : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly CancellationToken _unloadToken;
    private readonly ILogger<EventDispatcher2> _logger;
    private readonly Dictionary<EventListenerCacheKey, EventListenerInfo> _listeners = new Dictionary<EventListenerCacheKey, EventListenerInfo>();

    public EventDispatcher2(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<EventDispatcher2>>();
        _unloadToken = serviceProvider.GetRequiredService<WarfareModule>().UnloadToken;
    }

    UniTask IHostedService.StartAsync(CancellationToken token)
    {
        /* Provider */
        Provider.onServerConnected += ProviderOnServerConnected;
        Provider.onServerDisconnected += ProviderOnServerDisconnected;
        Provider.onBattlEyeKick += ProviderOnBattlEyeKick;

        /* Barricades */
        BarricadeManager.onDeployBarricadeRequested += BarricadeManagerOnDeployBarricadeRequested;
        BarricadeManager.onBarricadeSpawned += BarricadeManagerOnBarricadeSpawned;
        BarricadeDrop.OnSalvageRequested_Global += BarricadeDropOnSalvageRequested;
        BarricadeManager.onModifySignRequested += BarricadeManagerOnModifySignRequested;

        /* Structures */
        StructureManager.onDeployStructureRequested += StructureManagerOnDeployStructureRequested;
        StructureManager.onStructureSpawned += StructureManagerOnStructureSpawned;
        StructureDrop.OnSalvageRequested_Global += StructureDropOnSalvageRequested;

        /* Vehicles */
        VehicleManager.OnToggleVehicleLockRequested += VehicleManagerOnToggleVehicleLockRequested;
        VehicleManager.OnToggledVehicleLock += VehicleManagerOnToggledVehicleLock;

        return UniTask.CompletedTask;
    }

    UniTask IHostedService.StopAsync(CancellationToken token)
    {
        /* Provider */
        Provider.onServerConnected -= ProviderOnServerConnected;
        Provider.onServerDisconnected -= ProviderOnServerDisconnected;
        Provider.onBattlEyeKick -= ProviderOnBattlEyeKick;

        /* Barricades */
        BarricadeManager.onDeployBarricadeRequested -= BarricadeManagerOnDeployBarricadeRequested;
        BarricadeManager.onBarricadeSpawned -= BarricadeManagerOnBarricadeSpawned;
        BarricadeDrop.OnSalvageRequested_Global -= BarricadeDropOnSalvageRequested;
        BarricadeManager.onModifySignRequested -= BarricadeManagerOnModifySignRequested;

        /* Structures */
        StructureManager.onDeployStructureRequested -= StructureManagerOnDeployStructureRequested;
        StructureManager.onStructureSpawned -= StructureManagerOnStructureSpawned;
        StructureDrop.OnSalvageRequested_Global -= StructureDropOnSalvageRequested;

        /* Vehicles */
        VehicleManager.OnToggleVehicleLockRequested -= VehicleManagerOnToggleVehicleLockRequested;
        VehicleManager.OnToggledVehicleLock -= VehicleManagerOnToggledVehicleLock;

        return UniTask.CompletedTask;
    }

    private ILogger GetLogger(Type declaringType, string eventName)
    {
        EventInfo? eventInfo = declaringType.GetEvent(eventName, BindingFlags.Public | BindingFlags.NonPublic);
        if (eventInfo == null)
        {
            return (ILogger)_serviceProvider.GetService(typeof(ILogger<>).MakeGenericType(declaringType));
        }

        return _serviceProvider
            .GetService<ILoggerFactory>()
            .CreateLogger(Accessor.Formatter.Format(eventInfo, includeAccessors: false, includeEventKeyword: false));
    }

    /// <summary>
    /// Invoke an event with the given arguments.
    /// </summary>
    /// <returns>If the action should continue if <paramref name="eventArgs"/> is <see cref="ICancellable"/>, otherwise <see langword="true"/>.</returns>
    public async UniTask<bool> DispatchEventAsync<TEventArgs>(TEventArgs eventArgs, CancellationToken token = default)
    {
        using CombinedTokenSources tokens = token.CombineTokensIfNeeded(_unloadToken);

        await UniTask.SwitchToMainThread(token);

        List<EventListenerResult> eventListeners = ListPool<EventListenerResult>.claim();

        // get all event listeners from the service provider, then get all IEventListenerProviders and get all listeners from them.

        // IServiceProvider
        foreach (IEventListener<TEventArgs> eventListener in _serviceProvider.GetServices<IEventListener<TEventArgs>>())
        {
            eventListeners.Add(new EventListenerResult { Listener = eventListener });
        }

        foreach (IAsyncEventListener<TEventArgs> eventListener in _serviceProvider.GetServices<IAsyncEventListener<TEventArgs>>())
        {
            eventListeners.Add(new EventListenerResult { Flags = 1, Listener = eventListener });
        }

        // IEventListenerProviders
        foreach (IEventListenerProvider provider in _serviceProvider.GetServices<IEventListenerProvider>())
        {
            foreach (IEventListener<TEventArgs> eventListener in provider.EnumerateNormalListeners<TEventArgs>())
            {
                eventListeners.Add(new EventListenerResult { Listener = eventListener });
            }

            foreach (IAsyncEventListener<TEventArgs> eventListener in provider.EnumerateAsyncListeners<TEventArgs>())
            {
                eventListeners.Add(new EventListenerResult { Flags = 1, Listener = eventListener });
            }
        }

        int ct = eventListeners.Count;

        if (ct == 0)
        {
            return true;
        }

        EventListenerResult[] underlying = eventListeners.GetUnderlyingArray();

        FillResults<TEventArgs>(underlying, ct);

        Array.Sort(underlying, 0, ct, PriorityComparer.Instance);

#if DEBUG
        _logger.LogDebug("Dispatching event for {0} listeners: {1}.", ct, Accessor.Formatter.Format(typeof(TEventArgs)));
#endif

        for (int i = 0; i < ct; i++)
        {
            try
            {
                if ((underlying[i].Flags & 4) != 0 && Environment.CurrentManagedThreadId != WarfareModule.GameThreadId)
                {
                    await UniTask.SwitchToMainThread(token);
                }

                if (eventArgs is ICancellable { IsCancelled: true })
                    break;

                if ((underlying[i].Flags & 1) != 0)
                {
                    await ((IAsyncEventListener<TEventArgs>)underlying[i].Listener).HandleEventAsync(eventArgs, _serviceProvider, token);
                }
                else
                {
                    ((IEventListener<TEventArgs>)underlying[i].Listener).HandleEvent(eventArgs, _serviceProvider);
                }
            }
            catch (ControlException) { }
            catch (Exception ex)
            {
                if (Environment.CurrentManagedThreadId != WarfareModule.GameThreadId)
                {
                    await UniTask.SwitchToMainThread(CancellationToken.None);
                }

                Type listenerType = underlying[i].Listener.GetType();

                GetInfo<TEventArgs>((underlying[i].Flags & 1) != 0 ? typeof(IAsyncEventListener<TEventArgs>) : typeof(IEventListener<TEventArgs>), listenerType, out EventListenerInfo info);

                ILogger logger = (ILogger)_serviceProvider.GetService(typeof(ILogger<>).MakeGenericType(listenerType));
                if (ex is OperationCanceledException && token.IsCancellationRequested)
                {
                    logger.LogInformation(ex, "Execution of event handler {0} cancelled by CancellationToken.", Accessor.Formatter.Format(info.Method));
                }
                else
                {
                    logger.LogError(ex, "Error executing event handler: {0}.", Accessor.Formatter.Format(info.Method));
                }

                if (eventArgs is not ICancellable c)
                    continue;

                c.Cancel();
                logger.LogInformation("Cancelling event handler {0} due to exception described above.", Accessor.Formatter.Format(info.Method));
                break;
            }
        }

        // release list back to pool
        if (Thread.CurrentThread.IsGameThread())
        {
            ListPool<EventListenerResult>.release(eventListeners);
        }
        else
        {
            List<EventListenerResult> toRelease = eventListeners;
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();
                ListPool<EventListenerResult>.release(toRelease);
            });
        }

        return eventArgs is not ICancellable { IsActionCancelled: true };
    }

    private class PriorityComparer : IComparer<EventListenerResult>
    {
        public static readonly PriorityComparer Instance = new PriorityComparer();
        private PriorityComparer() { }
        public int Compare(EventListenerResult a, EventListenerResult b)
        {
                                                    // avoid extra branching for performance
            return (a.Flags & 2) != (b.Flags & 2) ? (a.Flags & 2) - 1 : b.Priority.CompareTo(a.Priority);
        }
    }

    private void FillResults<TEventArgs>(EventListenerResult[] eventListeners, int ct)
    {
        // separate method because async functions can't have ref locals.
        Type asyncType = typeof(IAsyncEventListener<TEventArgs>),
             normalType = typeof(IEventListener<TEventArgs>);

        for (int i = 0; i < ct; ++i)
        {
            ref EventListenerResult result = ref eventListeners[i];
            bool isAsync = (result.Flags & 1) != 0;
            GetInfo<TEventArgs>(isAsync ? asyncType : normalType, result.Listener.GetType(), out EventListenerInfo info);

            result.Priority = info.Priority;
            // ReSharper disable RedundantCast
            result.Flags |= info.MustRunInstantly & !isAsync ? (byte)2 : (byte)0;
            result.Flags |= info.EnsureMainThread ? (byte)4 : (byte)0;
            // ReSharper restore RedundantCast
        }
    }

    private void GetInfo<TEventArgs>(Type interfaceType, Type listenerType, out EventListenerInfo info)
    {
        EventListenerCacheKey key = default;
        key.InterfaceType = interfaceType;
        key.ListenerType = listenerType;

        if (_listeners.TryGetValue(key, out info))
            return;

        bool isAsync = interfaceType == typeof(IAsyncEventListener<TEventArgs>);
        MethodInfo interfaceMethod = isAsync
            ? InterfaceMethodCache<TEventArgs>.Async
            : InterfaceMethodCache<TEventArgs>.Normal;

        MethodInfo? implementedMethod = Accessor.GetImplementedMethod(listenerType, interfaceMethod);
        EventListenerAttribute? attribute = implementedMethod?.GetAttributeSafe<EventListenerAttribute>();

        info = default;
        info.Priority = attribute?.Priority ?? 0;
        info.EnsureMainThread = attribute is not { HasRequiredMainThread: true } ? !isAsync : attribute.RequiresMainThread;
        info.MustRunInstantly = attribute?.MustRunInstantly ?? false;

        if (isAsync && info.MustRunInstantly)
        {
            throw new NotSupportedException("Async event listeners can not use the 'MustRunInstantly' property.");
        }

        info.Method = implementedMethod ?? interfaceMethod;
        _listeners.Add(key, info);
    }

    private static class InterfaceMethodCache<TEventArgs>
    {
        public static readonly MethodInfo Normal;
        public static readonly MethodInfo Async;
        static InterfaceMethodCache()
        {
            Normal = typeof(IEventListener<TEventArgs>).GetMethod(nameof(IEventListener<object>.HandleEvent), BindingFlags.Instance | BindingFlags.Public)
                ?? throw new InvalidOperationException($"Unable to find method {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(IEventListener<object>.HandleEvent))
                    .DeclaredIn<IEventListener<TEventArgs>>(isStatic: false)
                    .WithParameter<TEventArgs>("e")
                    .ReturningVoid())
                }.");

            Async = typeof(IAsyncEventListener<TEventArgs>).GetMethod(nameof(IAsyncEventListener<object>.HandleEventAsync), BindingFlags.Instance | BindingFlags.Public)
                    ?? throw new InvalidOperationException($"Unable to find method {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(IAsyncEventListener<object>.HandleEventAsync))
                        .DeclaredIn<IAsyncEventListener<TEventArgs>>(isStatic: false)
                        .WithParameter<TEventArgs>("e")
                        .ReturningVoid())
                    }.");
        }
    }

    private struct EventListenerInfo
    {
        public int Priority;
        public bool EnsureMainThread;
        public bool MustRunInstantly;
        public MethodInfo Method;
    }

    private struct EventListenerResult
    {
        public object Listener;
        // to save struct size, trying to keep performance good for these
        // bits: 0: IsAsyncListener
        //       1: MustRunInstantly
        //       2: EnsureMainThread
        public byte Flags;
        public int Priority;
    }

    private struct EventListenerCacheKey : IEquatable<EventListenerCacheKey>
    {
        public Type ListenerType;
        public Type InterfaceType;
        public override int GetHashCode()
        {
            return HashCode.Combine(ListenerType, InterfaceType);
        }
        public bool Equals(EventListenerCacheKey other)
        {
            return ListenerType == other.ListenerType && InterfaceType == other.InterfaceType;
        }
        public override bool Equals(object? obj)
        {
            return obj is EventListenerCacheKey key && Equals(key);
        }
    }
}
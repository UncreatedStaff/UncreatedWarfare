using Cysharp.Threading.Tasks;
using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SDG.Framework.Utilities;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Uncreated.Warfare.Services;

namespace Uncreated.Warfare.Events;
public partial class EventDispatcher2 : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly CancellationToken _unloadToken;
    private readonly Dictionary<EventListenerCacheKey, EventListenerInfo> _listeners = new Dictionary<EventListenerCacheKey, EventListenerInfo>();

    public EventDispatcher2(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _unloadToken = serviceProvider.GetRequiredService<WarfareModule>().UnloadToken;
    }

    UniTask IHostedService.StartAsync(CancellationToken token)
    {
        /* Provider */
        Provider.onServerConnected += ProviderOnServerConnected;

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
        IEnumerable<EventListenerResult> eventListenerEnum = _serviceProvider
            .GetServices<IEventListener<TEventArgs>>()
            .Select(listener => new EventListenerResult { IsAsyncListener = false, Listener = listener })
            .Concat(_serviceProvider
                .GetServices<IAsyncEventListener<TEventArgs>>()
                .Select(listener => new EventListenerResult { IsAsyncListener = true, Listener = listener }))
            .Concat(_serviceProvider
                .GetServices<IEventListenerProvider>()
                .SelectMany(provider => provider
                    .EnumerateNormalListeners<TEventArgs>()
                    .Select(listener => new EventListenerResult { IsAsyncListener = false, Listener = listener })
                    .Concat(provider.EnumerateAsyncListeners<TEventArgs>()
                        .Select(listener => new EventListenerResult { IsAsyncListener = true, Listener = listener })
                    )
                )
            );

        eventListeners.AddRange(eventListenerEnum);

        if (eventListeners.Count == 0)
            return true;

        Type asyncType = typeof(IAsyncEventListener<TEventArgs>),
             normalType = typeof(IEventListener<TEventArgs>);

        for (int i = 0; i < eventListeners.Count; i++)
        {
            EventListenerResult result = eventListeners[i];
            GetInfo<TEventArgs>(result.IsAsyncListener ? asyncType : normalType, result.Listener.GetType(), out EventListenerInfo info);
            result.Priority = info.Priority;
            result.EnsureMainThread = info.EnsureMainThread;
            eventListeners[i] = result;
        }

        eventListeners.Sort((a, b) => b.Priority.CompareTo(a.Priority));

        for (int i = 0; i < eventListeners.Count; i++)
        {
            EventListenerResult result = eventListeners[i];

            try
            {
                if (result.EnsureMainThread && Environment.CurrentManagedThreadId != WarfareModule.GameThreadId)
                {
                    await UniTask.SwitchToMainThread(token);
                }

                if (eventArgs is ICancellable { IsCancelled: true })
                    break;

                if (result.IsAsyncListener)
                {
                    await ((IAsyncEventListener<TEventArgs>)result.Listener).HandleEventAsync(eventArgs, token);
                }
                else
                {
                    ((IEventListener<TEventArgs>)result.Listener).HandleEvent(eventArgs);
                }
            }
            catch (Exception ex)
            {
                Type listenerType = result.Listener.GetType();
                ILogger logger = (ILogger)_serviceProvider.GetService(typeof(ILogger<>).MakeGenericType(listenerType));

                if (Environment.CurrentManagedThreadId != WarfareModule.GameThreadId)
                {
                    await UniTask.SwitchToMainThread(CancellationToken.None);
                }

                GetInfo<TEventArgs>(result.IsAsyncListener ? asyncType : normalType, listenerType, out EventListenerInfo info);
                if (ex is OperationCanceledException && token.IsCancellationRequested)
                {
                    logger.LogInformation(ex, "Execution of event handler {0} cancelled.", Accessor.Formatter.Format(info.Method));
                }
                else
                {
                    logger.LogError(ex, "Error executing event handler: {0}.", Accessor.Formatter.Format(info.Method));
                }
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
        public MethodInfo Method;
    }

    private struct EventListenerResult
    {
        public object Listener;
        public bool IsAsyncListener;
        public int Priority;
        public bool EnsureMainThread;
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
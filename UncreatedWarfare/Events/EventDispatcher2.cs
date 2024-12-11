#if DEBUG
#define LOG_SYNCHRONIZATION_STEPS
#endif

using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using Microsoft.Extensions.DependencyInjection;
using SDG.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.List;

namespace Uncreated.Warfare.Events;

/// <summary>
/// Handles dispatching <see cref="IEventListener{TEventArgs}"/> and <see cref="IAsyncEventListener{TEventArgs}"/> objects.
/// </summary>
public partial class EventDispatcher2 : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly WarfareModule _warfare;
    private readonly IPlayerService _playerService;
    private readonly CancellationToken _unloadToken;
    private readonly ILogger<EventDispatcher2> _logger;
    private WarfareTimeComponent _timeComponent;
    private readonly Dictionary<EventListenerCacheKey, EventListenerInfo> _listeners = new Dictionary<EventListenerCacheKey, EventListenerInfo>();
    private readonly Dictionary<Type, EventModelAttribute?> _modelInfo = new Dictionary<Type, EventModelAttribute?>();

    private readonly Dictionary<string, PlayerDictionary<SynchronizationBucket>> _tagPlayerSynchronizations = new Dictionary<string, PlayerDictionary<SynchronizationBucket>>();
    private readonly Dictionary<string, SynchronizationBucket> _tagSynchronizations = new Dictionary<string, SynchronizationBucket>();

    private readonly Dictionary<Type, PlayerDictionary<SynchronizationBucket>> _typePlayerSynchronizations = new Dictionary<Type, PlayerDictionary<SynchronizationBucket>>();
    private readonly Dictionary<Type, SynchronizationBucket> _typeSynchronizations = new Dictionary<Type, SynchronizationBucket>();

    public EventDispatcher2(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<EventDispatcher2>>();
        _unloadToken = serviceProvider.GetRequiredService<WarfareModule>().UnloadToken;

        _playerService = serviceProvider.GetRequiredService<IPlayerService>();

        _timeComponent = serviceProvider.GetRequiredService<WarfareTimeComponent>();

        _warfare = serviceProvider.GetRequiredService<WarfareModule>();
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
        BarricadeManager.onDamageBarricadeRequested += BarricadeManagerOnDamageBarricadeRequested;

        /* Structures */
        StructureManager.onDeployStructureRequested += StructureManagerOnDeployStructureRequested;
        StructureManager.onStructureSpawned += StructureManagerOnStructureSpawned;
        StructureDrop.OnSalvageRequested_Global += StructureDropOnSalvageRequested;
        StructureManager.onDamageStructureRequested += StructureManagerOnDamageStructureRequested;

        /* Vehicles */
        VehicleManager.OnToggleVehicleLockRequested += VehicleManagerOnToggleVehicleLockRequested;
        VehicleManager.OnToggledVehicleLock += VehicleManagerOnToggledVehicleLock;
        VehicleManager.OnVehicleExploded += VehicleManagerOnVehicleExploded;
        VehicleManager.onExitVehicleRequested += VehicleManagerOnPassengerExitRequested;
        VehicleManager.onSwapSeatRequested += VehicleManagerOnSwapSeatRequested;
        VehicleManager.OnPreDestroyVehicle += VehicleManagerOnPreDestroyVehicle;

        /* Items */
        ItemManager.onTakeItemRequested += ItemManagerOnTakeItemRequested;

        /* Players */
        DamageTool.damagePlayerRequested += DamageToolOnPlayerDamageRequested;
        UseableConsumeable.onPerformingAid += UseableConsumeableOnPlayerPerformingAid;
        PlayerEquipment.OnPunch_Global += PlayerEquipmentOnPlayerPunch;
        PlayerQuests.onGroupChanged += PlayerQuestsOnGroupChanged;
        PlayerEquipment.OnUseableChanged_Global += PlayerEquipmentUseableChanged;

        /* Objects */
        ObjectManager.OnQuestObjectUsed += ObjectManagerOnQuestObjectUsed;

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
        BarricadeManager.onDamageBarricadeRequested -= BarricadeManagerOnDamageBarricadeRequested;

        /* Structures */
        StructureManager.onDeployStructureRequested -= StructureManagerOnDeployStructureRequested;
        StructureManager.onStructureSpawned -= StructureManagerOnStructureSpawned;
        StructureDrop.OnSalvageRequested_Global -= StructureDropOnSalvageRequested;
        StructureManager.onDamageStructureRequested -= StructureManagerOnDamageStructureRequested;

        /* Vehicles */
        VehicleManager.OnToggleVehicleLockRequested -= VehicleManagerOnToggleVehicleLockRequested;
        VehicleManager.OnToggledVehicleLock -= VehicleManagerOnToggledVehicleLock;
        VehicleManager.OnVehicleExploded -= VehicleManagerOnVehicleExploded;
        VehicleManager.onExitVehicleRequested -= VehicleManagerOnPassengerExitRequested;
        VehicleManager.onSwapSeatRequested -= VehicleManagerOnSwapSeatRequested;
        VehicleManager.OnPreDestroyVehicle -= VehicleManagerOnPreDestroyVehicle;

        /* Items */
        ItemManager.onTakeItemRequested -= ItemManagerOnTakeItemRequested;

        /* Players */
        DamageTool.damagePlayerRequested -= DamageToolOnPlayerDamageRequested;
        UseableConsumeable.onPerformingAid -= UseableConsumeableOnPlayerPerformingAid;
        PlayerEquipment.OnPunch_Global -= PlayerEquipmentOnPlayerPunch;
        PlayerQuests.onGroupChanged -= PlayerQuestsOnGroupChanged;
        PlayerEquipment.OnUseableChanged_Global -= PlayerEquipmentUseableChanged;

        _timeComponent = null!;
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

    private EventModelAttribute? GetModelInfo(Type type)
    {
        if (_modelInfo.TryGetValue(type, out EventModelAttribute? attribute))
        {
            return attribute;
        }

        attribute = type.GetAttributeSafe<EventModelAttribute>();
        _modelInfo.Add(type, attribute);
        return attribute;
    }

    /// <summary>
    /// Invoke an event with the given arguments.
    /// </summary>
    /// <returns>If the action should continue if <paramref name="eventArgs"/> is <see cref="ICancellable"/>, otherwise <see langword="true"/>.</returns>
    public async UniTask<bool> DispatchEventAsync<TEventArgs>(TEventArgs eventArgs, CancellationToken token = default, bool allowAsync = true) where TEventArgs : class
    {
        using CombinedTokenSources tokens = token.CombineTokensIfNeeded(_unloadToken);

        await UniTask.SwitchToMainThread(token);

        List<EventListenerResult> eventListeners = ListPool<EventListenerResult>.claim();

        Type type = typeof(TEventArgs);
        EventModelAttribute? modelInfo = GetModelInfo(type);

        // get all event listeners from the service provider, then get all IEventListenerProviders and get all listeners from them.

        ILifetimeScope scope = _warfare.IsLayoutActive() ? _warfare.ScopedProvider : _warfare.ServiceProvider;

        IServiceProvider serviceProvider = scope.Resolve<IServiceProvider>();

        // IServiceProvider
        foreach (IEventListener<TEventArgs> eventListener in scope.Resolve<IEnumerable<IEventListener<TEventArgs>>>())
        {
            eventListeners.Add(new EventListenerResult { Listener = eventListener });
        }

        foreach (IAsyncEventListener<TEventArgs> eventListener in scope.Resolve<IEnumerable<IAsyncEventListener<TEventArgs>>>())
        {
            eventListeners.Add(new EventListenerResult { Flags = 1, Listener = eventListener });
        }

        // IEventListenerProviders
        foreach (IEventListenerProvider provider in scope.Resolve<IEnumerable<IEventListenerProvider>>())
        {
            foreach (IEventListener<TEventArgs> eventListener in provider.EnumerateNormalListeners(eventArgs))
            {
                eventListeners.Add(new EventListenerResult { Listener = eventListener });
            }

            foreach (IAsyncEventListener<TEventArgs> eventListener in provider.EnumerateAsyncListeners(eventArgs))
            {
                eventListeners.Add(new EventListenerResult { Flags = 1, Listener = eventListener });
            }
        }

        int ct = eventListeners.Count;

#if DEBUG
        _logger.LogDebug("Invoke {0} - Dispatching event for {1} listener(s).", type, ct);
#endif

        List<SynchronizationBucket>? buckets = null;
        List<Task>? tasks = null;

        if (allowAsync)
        {
            if (modelInfo != null && modelInfo.SynchronizationContext != EventSynchronizationContext.None)
            {
                buckets = ListPool<SynchronizationBucket>.claim();
                tasks = ListPool<Task>.claim();
                EnterSynchronizationBuckets(eventArgs, modelInfo, type, buckets, tasks, token);
#if LOG_SYNCHRONIZATION_STEPS
                _logger.LogDebug("Invoke {0} - Synchronizing with {1} bucket(s).", type, buckets.Count);
#endif
                tasks.RemoveAll(x => x.IsCompleted);

                if (tasks.Count > 0)
                {
#if LOG_SYNCHRONIZATION_STEPS
                    _logger.LogDebug("Invoke {0} - Awaiting {1} bucket(s).", type, buckets.Count);
#endif
                    await Task.WhenAll(tasks).ConfigureAwait(false);
#if LOG_SYNCHRONIZATION_STEPS
                    _logger.LogDebug("Invoke {0} - Done awaiting buckets.", type);
#endif
                }
            }
        }

        try
        {
            if (ct == 0)
            {
                return true;
            }

            EventListenerResult[] underlying = eventListeners.GetUnderlyingArray();

            FillResults<TEventArgs>(underlying, ct);

            Array.Sort(underlying, 0, ct, PriorityComparer.Instance);

            bool layoutActive = _warfare.IsLayoutActive();

            for (int i = 0; i < ct; i++)
            {
                try
                {
                    if (!layoutActive && (underlying[i].Flags & 8) != 0)
                    {
                        continue;
                    }

                    if ((underlying[i].Flags & 4) != 0 && !GameThread.IsCurrent)
                    {
                        await UniTask.SwitchToMainThread(token);
                    }

                    if (eventArgs is ICancellable { IsCancelled: true })
                        break;

                    if ((underlying[i].Flags & 1) != 0)
                    {
                        if (!allowAsync)
                            throw new InvalidOperationException($"Async event listeners not supported for {Accessor.ExceptionFormatter.Format<TEventArgs>()}.");
                        await ((IAsyncEventListener<TEventArgs>)underlying[i].Listener).HandleEventAsync(eventArgs, serviceProvider, token);
                    }
                    else
                    {
                        ((IEventListener<TEventArgs>)underlying[i].Listener).HandleEvent(eventArgs, serviceProvider);
                    }
                }
                catch (ControlException) { }
                catch (Exception ex)
                {
                    if (!GameThread.IsCurrent)
                    {
                        await UniTask.SwitchToMainThread(CancellationToken.None);
                    }

                    Type listenerType = underlying[i].Listener.GetType();

                    GetInfo<TEventArgs>((underlying[i].Flags & 1) != 0 ? typeof(IAsyncEventListener<TEventArgs>) : typeof(IEventListener<TEventArgs>), listenerType, out EventListenerInfo info);

                    ILogger logger = (ILogger)scope.Resolve(typeof(ILogger<>).MakeGenericType(listenerType));
                    if (ex is OperationCanceledException && token.IsCancellationRequested)
                    {
                        logger.LogInformation(ex, "Execution of event handler {0} cancelled by CancellationToken.", info.Method);
                    }
                    else
                    {
                        logger.LogError(ex, "Error executing event handler: {0}.", info.Method);
                    }

                    if (eventArgs is not ICancellable c)
                        continue;

                    c.Cancel();
                    logger.LogInformation("Cancelling event handler {0} due to exception described above.", info.Method);
                    break;
                }
            }

            return eventArgs is not ICancellable { IsActionCancelled: true };
        }
        finally
        {
            await UniTask.SwitchToMainThread();

            if (buckets != null)
            {
#if LOG_SYNCHRONIZATION_STEPS
                _logger.LogDebug("Invoke {0} - Releasing {1} bucket(s).", type, buckets.Count);
#endif
                foreach (SynchronizationBucket bucket in buckets)
                {
#if LOG_SYNCHRONIZATION_STEPS
                    _logger.LogDebug("Invoke {0} - Releasing bucket: \"{1}\".", type, bucket);
#endif
                    bucket.Semaphore.Release();
                }
            }

            ListPool<EventListenerResult>.release(eventListeners);
            if (buckets != null)
                ListPool<SynchronizationBucket>.release(buckets);
            if (tasks != null)
                ListPool<Task>.release(tasks);
        }
    }

    private void EnterSynchronizationBuckets(object eventArgs, EventModelAttribute modelInfo, Type type, List<SynchronizationBucket> buckets, List<Task> tasks, CancellationToken token)
    {
        if (modelInfo.SynchronizationContext != EventSynchronizationContext.PerPlayer)
        {
            // global sync buckets + all players
            if (!_typeSynchronizations.TryGetValue(type, out SynchronizationBucket bucket))
            {
                bucket = new SynchronizationBucket(type, false);
                _typeSynchronizations.Add(type, bucket);
            }

            buckets.Add(bucket);
            tasks.Add(bucket.Semaphore.WaitAsync(token));
#if LOG_SYNCHRONIZATION_STEPS
            _logger.LogDebug("Invoke {0} - Locking on type {0}.", type);
#endif

            if (_typePlayerSynchronizations.TryGetValue(type, out PlayerDictionary<SynchronizationBucket> dict))
            {
#if LOG_SYNCHRONIZATION_STEPS
                _logger.LogDebug("Invoke {0} - Locking on type {0} for all players:", type);
#endif
                foreach (SynchronizationBucket b in dict.Values)
                {
                    buckets.Add(b);
                    tasks.Add(b.Semaphore.WaitAsync(token));
#if LOG_SYNCHRONIZATION_STEPS
                    _logger.LogDebug("Invoke {0} - Locking on type {0} for player {1}.", type, b.Player?.Steam64.m_SteamID.ToString(CultureInfo.InvariantCulture) ?? "null");
#endif
                }
            }

            if (modelInfo.RequestModel != null)
            {
                if (!_typeSynchronizations.TryGetValue(modelInfo.RequestModel, out bucket))
                {
                    bucket = new SynchronizationBucket(modelInfo.RequestModel, false);
                    _typeSynchronizations.Add(modelInfo.RequestModel, bucket);
                }

                buckets.Add(bucket);
                tasks.Add(bucket.Semaphore.WaitAsync(token));
#if LOG_SYNCHRONIZATION_STEPS
                _logger.LogDebug("Invoke {0} - Locking on request type {1}.", type, modelInfo.RequestModel);
#endif

                if (_typePlayerSynchronizations.TryGetValue(modelInfo.RequestModel, out dict))
                {
#if LOG_SYNCHRONIZATION_STEPS
                    _logger.LogDebug("Invoke {0} - Locking on request type {1} for all players:", type, modelInfo.RequestModel);
#endif
                    foreach (SynchronizationBucket b in dict.Values)
                    {
                        buckets.Add(b);
                        tasks.Add(b.Semaphore.WaitAsync(token));
#if LOG_SYNCHRONIZATION_STEPS
                        _logger.LogDebug("Invoke {0} - Locking on request type {1} for player {2}.", type, modelInfo.RequestModel, b.Player?.Steam64.m_SteamID.ToString(CultureInfo.InvariantCulture) ?? "null");
#endif
                    }
                }
            }

            if (modelInfo.SynchronizedModelTags == null)
                return;
            
            for (int i = 0; i < modelInfo.SynchronizedModelTags.Length; ++i)
            {
                string tag = modelInfo.SynchronizedModelTags[i];
                if (!_tagSynchronizations.TryGetValue(tag, out bucket))
                {
                    bucket = new SynchronizationBucket(type, false);
                    _tagSynchronizations.Add(tag, bucket);
                }

                buckets.Add(bucket);
                tasks.Add(bucket.Semaphore.WaitAsync(token));
#if LOG_SYNCHRONIZATION_STEPS
                _logger.LogDebug("Invoke {0} - Locking on tag \"{1}\".", type, tag);
#endif

                if (!_tagPlayerSynchronizations.TryGetValue(tag, out dict))
                    continue;

#if LOG_SYNCHRONIZATION_STEPS
                _logger.LogDebug("Invoke {0} - Locking on tag \"{1}\" for all players.", type, tag);
#endif
                foreach (SynchronizationBucket b in dict.Values)
                {
                    buckets.Add(b);
                    tasks.Add(b.Semaphore.WaitAsync(token));
#if LOG_SYNCHRONIZATION_STEPS
                    _logger.LogDebug("Invoke {0} - Locking on tag \"{1}\" for player {2}.", type, tag, b.Player?.Steam64.m_SteamID.ToString(CultureInfo.InvariantCulture) ?? "null");
#endif
                }
            }
            
        }
        else if (eventArgs is IPlayerEvent playerArgs)
        {
            if (_typeSynchronizations.TryGetValue(type, out SynchronizationBucket? bucket) && bucket.Semaphore.CurrentCount < 1)
            {
                buckets.Add(bucket);
                tasks.Add(bucket.Semaphore.WaitAsync(token));
#if LOG_SYNCHRONIZATION_STEPS
                _logger.LogDebug("Invoke {0} - Locking on type {0} (already locked).", type);
#endif
            }

            if (_typePlayerSynchronizations.TryGetValue(type, out PlayerDictionary<SynchronizationBucket> dict))
            {
                if (!dict.TryGetValue(playerArgs.Steam64, out bucket))
                {
                    bucket = new SynchronizationBucket(type, false, playerArgs.Player);
                    dict.Add(playerArgs.Steam64, bucket);
                }
            }
            else
            {
                dict = new PlayerDictionary<SynchronizationBucket>();
                bucket = new SynchronizationBucket(type, false, playerArgs.Player);
                dict.Add(playerArgs.Steam64, bucket);
                _typePlayerSynchronizations.Add(type, dict);
            }

            buckets.Add(bucket);
            tasks.Add(bucket.Semaphore.WaitAsync(token));
#if LOG_SYNCHRONIZATION_STEPS
            _logger.LogDebug("Invoke {0} - Locking on type {0} for player {1}.", type, bucket.Player?.Steam64.m_SteamID.ToString(CultureInfo.InvariantCulture) ?? "null");
#endif

            if (modelInfo.RequestModel != null)
            {
                if (_typeSynchronizations.TryGetValue(modelInfo.RequestModel, out bucket) && bucket.Semaphore.CurrentCount < 1)
                {
                    buckets.Add(bucket);
                    tasks.Add(bucket.Semaphore.WaitAsync(token));
#if LOG_SYNCHRONIZATION_STEPS
                    _logger.LogDebug("Invoke {0} - Locking on request type {1} (already locked).", type, modelInfo.RequestModel);
#endif
                }

                if (_typePlayerSynchronizations.TryGetValue(modelInfo.RequestModel, out dict))
                {
                    if (!dict.TryGetValue(playerArgs.Steam64, out bucket))
                    {
                        bucket = new SynchronizationBucket(modelInfo.RequestModel, false, playerArgs.Player);
                        dict.Add(playerArgs.Steam64, bucket);
                    }
                }
                else
                {
                    dict = new PlayerDictionary<SynchronizationBucket>();
                    bucket = new SynchronizationBucket(modelInfo.RequestModel, false, playerArgs.Player);
                    dict.Add(playerArgs.Steam64, bucket);
                    _typePlayerSynchronizations.Add(modelInfo.RequestModel, dict);
                }

                buckets.Add(bucket);
                tasks.Add(bucket.Semaphore.WaitAsync(token));
#if LOG_SYNCHRONIZATION_STEPS
                _logger.LogDebug("Invoke {0} - Locking on request type {1} for player {2}.", type, modelInfo.RequestModel, bucket.Player?.Steam64.m_SteamID.ToString(CultureInfo.InvariantCulture) ?? "null");
#endif
            }

            if (modelInfo.SynchronizedModelTags == null)
                return;

            for (int i = 0; i < modelInfo.SynchronizedModelTags.Length; ++i)
            {
                string tag = modelInfo.SynchronizedModelTags[i];

                if (_tagSynchronizations.TryGetValue(tag, out bucket) && bucket.Semaphore.CurrentCount < 1)
                {
                    buckets.Add(bucket);
                    tasks.Add(bucket.Semaphore.WaitAsync(token));
#if LOG_SYNCHRONIZATION_STEPS
                    _logger.LogDebug("Invoke {0} - Locking on tag \"{1}\" (already locked).", type, tag);
#endif
                }

                if (_tagPlayerSynchronizations.TryGetValue(tag, out dict))
                {
                    if (!dict.TryGetValue(playerArgs.Steam64, out bucket))
                    {
                        bucket = new SynchronizationBucket(type, false, playerArgs.Player);
                        dict.Add(playerArgs.Steam64, bucket);
                    }
                }
                else
                {
                    dict = new PlayerDictionary<SynchronizationBucket>();
                    bucket = new SynchronizationBucket(type, false, playerArgs.Player);
                    dict.Add(playerArgs.Steam64, bucket);
                    _tagPlayerSynchronizations.Add(tag, dict);
                }

                buckets.Add(bucket);
                tasks.Add(bucket.Semaphore.WaitAsync(token));
#if LOG_SYNCHRONIZATION_STEPS
                _logger.LogDebug("Invoke {0} - Locking on tag \"{1}\" for player {2}.", type, tag, bucket.Player?.Steam64.m_SteamID.ToString(CultureInfo.InvariantCulture) ?? "null");
#endif
            }
        }
        else
        {
            _logger.LogWarning("Event arg {0} has Per-Player synchronization setting but doesn't implement {1}.", type, typeof(IPlayerEvent));
        }
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
            result.Flags |= info.RequireActiveLayout ? (byte)8 : (byte)0;
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
        info.RequireActiveLayout = attribute?.RequireActiveLayout ?? false;

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
        public bool RequireActiveLayout;
        public bool EnsureMainThread;
        public bool MustRunInstantly;
        public MethodInfo Method;
    }

    private struct EventListenerResult
    {
        public object Listener;
        // to save struct size, trying to keep performance good for these
        // bits: 0 (1): IsAsyncListener
        //       1 (2): MustRunInstantly
        //       2 (4): EnsureMainThread
        //       3 (8): RequireActiveLayout
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
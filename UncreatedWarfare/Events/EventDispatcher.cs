#if DEBUG
//#define LOG_SYNCHRONIZATION_STEPS
//#define LOG_EVENT_LISTENERS
//#define LOG_RESOLVE_STEPS
//#define LOG_TRACE_TO_FILE
#endif

using Autofac.Core;
using Autofac.Core.Lifetime;
using DanielWillett.ReflectionTools;
using DanielWillett.ReflectionTools.Formatting;
using Microsoft.Extensions.DependencyInjection;
using SDG.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Management;
using Uncreated.Warfare.Projectiles;
using Uncreated.Warfare.Services;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.List;
using Uncreated.Warfare.Vehicles;
using Service = Autofac.Core.Service;

namespace Uncreated.Warfare.Events;

/// <summary>
/// Handles dispatching <see cref="IEventListener{TEventArgs}"/> and <see cref="IAsyncEventListener{TEventArgs}"/> objects.
/// </summary>
public partial class EventDispatcher : IHostedService, IDisposable
{
    // bits for EventListenerResult.Flags
    private const int BitIsAsync = 1;
    // CompareEventListenerResults relies on this being 2, check if changing
    private const int BitMustRunInstantly = 2;
    private const int BitEnsureMainThread = 4;
    private const int BitRequireActiveLayout = 8;
    private const int BitRequireNextFrame = 16;
    private const int BitMustRunLast = 32;

    private int _activeEvents;
    private readonly WarfareModule _warfare;
    private readonly IPlayerService _playerService;
    private readonly ProjectileSolver _projectileSolver;
    private readonly CancellationToken _unloadToken;
    private readonly ILogger<EventDispatcher> _logger;
    [field: CanBeNull]
    private VehicleService VehicleService => field ??= _warfare.ServiceProvider.Resolve<VehicleService>();
    private IServiceProvider? _scopedServiceProvider;
    private readonly ILoggerFactory _loggerFactory;
    private readonly Dictionary<EventListenerCacheKey, EventListenerInfo> _listeners = new Dictionary<EventListenerCacheKey, EventListenerInfo>(128);
    private readonly Dictionary<Type, EventInvocationListenerCache> _listenerCaches = new Dictionary<Type, EventInvocationListenerCache>(128);
    private readonly Dictionary<Type, MethodInfo> _syncInvokeMethods = new Dictionary<Type, MethodInfo>(64);
    private readonly Dictionary<Type, MethodInfo> _asyncInvokeMethods = new Dictionary<Type, MethodInfo>(64);
    private readonly Dictionary<Type, Action<object, object, IServiceProvider>> _syncGeneratedInvokeMethods
        = new Dictionary<Type, Action<object, object, IServiceProvider>>(16);
    private readonly Dictionary<Type, Func<object, object, IServiceProvider, CancellationToken, UniTask>> _asyncGeneratedInvokeMethods
        = new Dictionary<Type, Func<object, object, IServiceProvider, CancellationToken, UniTask>>(16);
    private readonly List<IEventListenerProvider> _eventProviders;

    private readonly CancellationTokenSource _cancellationTokenSource;

    private readonly Dictionary<string, PlayerDictionary<SynchronizationBucket>> _tagPlayerSynchronizations = new Dictionary<string, PlayerDictionary<SynchronizationBucket>>();
    private readonly Dictionary<string, SynchronizationBucket> _tagSynchronizations = new Dictionary<string, SynchronizationBucket>();

    private readonly Dictionary<Type, PlayerDictionary<SynchronizationBucket>> _typePlayerSynchronizations = new Dictionary<Type, PlayerDictionary<SynchronizationBucket>>();
    private readonly Dictionary<Type, SynchronizationBucket> _typeSynchronizations = new Dictionary<Type, SynchronizationBucket>();

#if LOG_TRACE_TO_FILE
    private readonly StreamWriter _eventFileWriter = new StreamWriter("./Logs/eventlogs.txt", false);
#endif

    public EventDispatcher(ILoggerFactory loggerFactory,
        ProjectileSolver projectileSolver,
        IPlayerService playerService,
        WarfareModule module)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _unloadToken = _cancellationTokenSource.Token;

        _logger = loggerFactory.CreateLogger<EventDispatcher>();
        _loggerFactory = loggerFactory;
        _projectileSolver = projectileSolver;
        _playerService = playerService;
        _warfare = module;

        _warfare.LayoutStarted += OnLayoutStarted;

        _eventProviders = new List<IEventListenerProvider>(8);
        FindEventListenerProviders(_warfare.IsLayoutActive() ? _warfare.ScopedProvider : _warfare.ServiceProvider, _eventProviders);

#if LOG_TRACE_TO_FILE
        _eventFileWriter.AutoFlush = true;
        _eventFileWriter.WriteLine("Startup");
#endif
    }
    void IDisposable.Dispose()
    {
        _warfare.LayoutStarted -= OnLayoutStarted;

#if LOG_TRACE_TO_FILE
        _eventFileWriter.WriteLine("Shutdown");
        _eventFileWriter.Dispose();
#endif
    }

    private void SubscribePlayerEvents(WarfarePlayer player)
    {
        player.UnturnedPlayer.life.onHurt += OnPlayerHurt;
    }

    public async Task WaitForEvents(CancellationToken token = default)
    {
        if (_activeEvents <= 0)
        {
            return;
        }

        DateTime spinStart = DateTime.UtcNow;
        while (_activeEvents > 0)
        {
            await Task.Delay(25, token);
            if ((DateTime.UtcNow - spinStart).TotalSeconds < 10)
                continue;

            _logger.LogWarning("Timed out waiting for events to finish.");
            break;
        }
    }

    UniTask IHostedService.StartAsync(CancellationToken token)
    {
        /* Provider */
        Provider.onEnemyConnected += ProviderOnServerConnectedEarly;
        Provider.onServerConnected += ProviderOnServerConnected;
        Provider.onServerDisconnected += ProviderOnServerDisconnected;
        Provider.onBattlEyeKick += ProviderOnBattlEyeKick;
        Provider.onLoginSpawning = OnPlayerChooseSpawnAfterLogin;

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
        VehicleManager.onDamageVehicleRequested += OnDamageVehicleRequested;
        VehicleManager.OnVehicleExploded += VehicleManagerOnVehicleExploded;
        VehicleManager.onExitVehicleRequested += VehicleManagerOnPassengerExitRequested;
        VehicleManager.onSwapSeatRequested += VehicleManagerOnSwapSeatRequested;
        VehicleManager.OnPreDestroyVehicle += VehicleManagerOnPreDestroyVehicle;

        /* Items */
        ItemManager.onTakeItemRequested += ItemManagerOnTakeItemRequested;
        PlayerCrafting.onCraftBlueprintRequested += PlayerCraftingCraftBlueprintRequested;

        /* Players */
        DamageTool.damagePlayerRequested += DamageToolOnPlayerDamageRequested;
        UseableConsumeable.onPerformingAid += UseableConsumeableOnPlayerPerformingAid;
        UseableConsumeable.onPerformedAid += UseableConsumeableOnPlayerPerformedAid;
        PlayerQuests.onGroupChanged += PlayerQuestsOnGroupChanged;
        PlayerEquipment.OnUseableChanged_Global += PlayerEquipmentUseableChanged;
        PlayerLife.OnSelectingRespawnPoint += OnPlayerChooseSpawnAfterDeath;
        PlayerLife.OnPreDeath += PlayerLifeOnOnPreDeath;
        _playerService.SubscribeToPlayerEvent<PlayerEquipRequestHandler>((player, value) => player.UnturnedPlayer.equipment.onEquipRequested += value, OnPlayerEquipRequested);
        _playerService.SubscribeToPlayerEvent<PlayerDequipRequestHandler>((player, value) => player.UnturnedPlayer.equipment.onDequipRequested += value, OnPlayerDequipRequested);

        /* Projectiles */
        UseableGun.onProjectileSpawned += OnProjectileSpawned;
        
        /* Throwables */
        UseableThrowable.onThrowableSpawned += OnThrowableSpawned;

        /* Objects */
        ObjectManager.OnQuestObjectUsed += ObjectManagerOnQuestObjectUsed;
        NPCEventManager.onEvent += NPCEventManagerOnEvent;

        return UniTask.CompletedTask;
    }

    async UniTask IHostedService.StopAsync(CancellationToken token)
    {
        /* Provider */
        Provider.onServerConnected -= ProviderOnServerConnected;
        Provider.onServerDisconnected -= ProviderOnServerDisconnected;
        Provider.onBattlEyeKick -= ProviderOnBattlEyeKick;
        Provider.onLoginSpawning = null;

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
        VehicleManager.onDamageVehicleRequested -= OnDamageVehicleRequested;
        VehicleManager.OnVehicleExploded -= VehicleManagerOnVehicleExploded;
        VehicleManager.onExitVehicleRequested -= VehicleManagerOnPassengerExitRequested;
        VehicleManager.onSwapSeatRequested -= VehicleManagerOnSwapSeatRequested;
        VehicleManager.OnPreDestroyVehicle -= VehicleManagerOnPreDestroyVehicle;

        /* Items */
        ItemManager.onTakeItemRequested -= ItemManagerOnTakeItemRequested;
        PlayerCrafting.onCraftBlueprintRequested -= PlayerCraftingCraftBlueprintRequested;

        /* Players */
        DamageTool.damagePlayerRequested -= DamageToolOnPlayerDamageRequested;
        UseableConsumeable.onPerformingAid -= UseableConsumeableOnPlayerPerformingAid;
        UseableConsumeable.onPerformedAid -= UseableConsumeableOnPlayerPerformedAid;
        PlayerQuests.onGroupChanged -= PlayerQuestsOnGroupChanged;
        PlayerEquipment.OnUseableChanged_Global -= PlayerEquipmentUseableChanged;
        PlayerLife.OnSelectingRespawnPoint -= OnPlayerChooseSpawnAfterDeath;
        PlayerLife.OnPreDeath -= PlayerLifeOnOnPreDeath;
        _playerService.UnsubscribeFromPlayerEvent<PlayerEquipRequestHandler>((player, value) => player.UnturnedPlayer.equipment.onEquipRequested -= value, OnPlayerEquipRequested);
        _playerService.UnsubscribeFromPlayerEvent<PlayerDequipRequestHandler>((player, value) => player.UnturnedPlayer.equipment.onDequipRequested -= value, OnPlayerDequipRequested);

        /* Projectiles */
        UseableGun.onProjectileSpawned -= OnProjectileSpawned;
        
        /* Throwables */
        UseableThrowable.onThrowableSpawned -= OnThrowableSpawned;

        /* Objects */
        ObjectManager.OnQuestObjectUsed -= ObjectManagerOnQuestObjectUsed;
        NPCEventManager.onEvent -= NPCEventManagerOnEvent;

        await WaitForEvents(CancellationToken.None);

        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
    }

    /// <summary>
    /// Create a logger for a handler of a specific <see cref="EventInfo"/> from a declaring type and name.
    /// </summary>
    public ILogger GetLogger(Type declaringType, string eventName)
    {
        EventInfo? eventInfo = declaringType.GetEvent(eventName, BindingFlags.Public | BindingFlags.NonPublic);
        return eventInfo == null
            ? _loggerFactory.CreateLogger(declaringType)
            : _loggerFactory.CreateLogger(Accessor.Formatter.Format(eventInfo, includeAccessors: false, includeEventKeyword: false));
    }

    private void OnLayoutStarted()
    {
        GameThread.AssertCurrent();

        // clears the cache for all event types
        lock (_listenerCaches)
        {
            ILifetimeScope scope = _warfare.IsLayoutActive() ? _warfare.ScopedProvider : _warfare.ServiceProvider;
            _scopedServiceProvider = null;
            foreach (EventInvocationListenerCache cache in _listenerCaches.Values)
            {
                ListPool<EventListenerResult>.release(cache.Results);
            }
            _listenerCaches.Clear();
            _eventProviders.Clear();
            FindEventListenerProviders(scope, _eventProviders);
        }
    }

#if LOG_TRACE_TO_FILE
    private static int _traceNum;
#endif

    /// <summary>
    /// Invoke an event with the given arguments.
    /// </summary>
    /// <returns>If the action should continue if <paramref name="eventArgs"/> is <see cref="ICancellable"/>, otherwise <see langword="true"/>.</returns>
    public async UniTask<bool> DispatchEventAsync<TEventArgs>(TEventArgs eventArgs, CancellationToken token = default, bool allowAsync = true) where TEventArgs : class
    {
#if LOG_TRACE_TO_FILE
        int traceNum = Interlocked.Increment(ref _traceNum);
#endif

        await UniTask.SwitchToMainThread(token);

        Type type = typeof(TEventArgs);

        EventInvocationListenerCache cache = GetEventListenersCache<TEventArgs>(out IServiceProvider serviceProvider);

        List<EventListenerResult> eventListeners = ListPool<EventListenerResult>.claim();
        eventListeners.AddRange(cache.Results);

        // IEventListenerProviders
        foreach (IEventListenerProvider provider in _eventProviders)
        {
            foreach (IEventListener<TEventArgs> eventListener in provider.EnumerateNormalListeners(eventArgs))
            {
                AddProviderResults(eventListener, eventListeners, false, type);
            }

            foreach (IAsyncEventListener<TEventArgs> eventListener in provider.EnumerateAsyncListeners(eventArgs))
            {
                AddProviderResults(eventListener, eventListeners, true, type);
            }
        }

        int ct = eventListeners.Count;

        _logger.LogDebug($"Invoke {type} - Dispatching event for {ct} listener(s).");
#if LOG_TRACE_TO_FILE
        _eventFileWriter.WriteLine("{0}  Invoking {1} for {2} listeners.", traceNum, Accessor.Formatter.Format(type), ct);
#endif

#if LOG_EVENT_LISTENERS
        using (_logger.BeginScope(typeof(TEventArgs)))
        {
            int index = -1;
            foreach (EventListenerResult result in eventListeners)
            {
                if (result.Model == null)
                    _logger.LogDebug("#{0} listener {1} (priority: {2} f:0b{3}) hash {4} returned from listener provider.", ++index, result.Listener.GetType(), result.Priority, Convert.ToString(result.Flags, 2), result.Listener.GetHashCode());
                else
                    _logger.LogDebug("#{0} listener {1} (priority: {2} f:0b{3}) hash {4} of {5} cached.", ++index, result.Listener.GetType(), result.Priority, Convert.ToString(result.Flags, 2), result.Listener.GetHashCode(), result.Model);
            }
        }
#endif

#if LOG_TRACE_TO_FILE
        {
            int index = -1;
            foreach (EventListenerResult result in eventListeners)
            {
                if (result.Model == null)
                    _eventFileWriter.WriteLine("{0}  #{1} listener {2} (priority: {3} f:0b{4}) hash {5} returned from listener provider.", traceNum, ++index, result.Listener.GetType(), result.Priority, Convert.ToString(result.Flags, 2), result.Listener.GetHashCode());
                else
                    _eventFileWriter.WriteLine("{0}  #{1} listener {2} (priority: {3} f:0b{4}) hash {5} of {6} cached.", traceNum, ++index, result.Listener.GetType(), result.Priority, Convert.ToString(result.Flags, 2), result.Listener.GetHashCode(), result.Model);
            }
        }
#endif

        List<SynchronizationBucket>? buckets = null;
        List<Task>? tasks = null;

        // enter sync buckets
        if (allowAsync)
        {
            EventModelAttribute? modelInfo = cache.ModelInfo;
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

        Interlocked.Increment(ref _activeEvents);

        try
        {
            if (ct == 0)
            {
                return true;
            }

            EventListenerResult[] underlying = eventListeners.GetUnderlyingArray();

            // remove async events for any listeners that don't match the arg type exactly, otherwise throw an exception.
            if (!allowAsync)
            {
                for (int i = 0; i < ct; ++i)
                {
                    ref EventListenerResult result = ref underlying[i];
                    if ((result.Flags & BitIsAsync) == 0)
                        continue;

                    if (result.Model == type)
                    {
                        throw new InvalidOperationException($"Async event listeners not supported for event model {Accessor.ExceptionFormatter.Format<TEventArgs>()}, as with listener {Accessor.ExceptionFormatter.Format(result.Listener.GetType())}.");
                    }
                    
                    _logger.LogDebug("Skipping event invocation of listener type {0} for event model {1} (listening model type {2}) because async event handlers are not allowed.", result.Listener.GetType(), type, result.Model);
                    result.Model = null;
                }
            }

            bool hasSkippedToNextFrame = false;
            for (int i = 0; i < ct; i++)
            {
                // skipped
                if (underlying[i].Model is null)
                    continue;

                try
                {
                    if (eventArgs is ICancellable { IsCancelled: true })
                    {
#if LOG_TRACE_TO_FILE
                        _eventFileWriter.WriteLine("{0}  Cancelled", traceNum);
#endif
                        // check if a MustRunLast cancelled (they shouldn't be allowed to)
                        if (i > 0 && (underlying[i - 1].Flags & BitMustRunLast) != 0)
                            throw new InvalidOperationException($"Event cancelled by a listener using 'MustRunLast': {underlying[i - 1].Model} in {underlying[i - 1].Listener.GetType()}.");
                        break;
                    }

                    // RequireNextFrame
                    if (!hasSkippedToNextFrame && (underlying[i].Flags & BitRequireNextFrame) != 0)
                    {
#if LOG_TRACE_TO_FILE
                        _eventFileWriter.WriteLine("{0}  RequireNextFrame", traceNum);
#endif
                        hasSkippedToNextFrame = true;
                        await UniTask.NextFrame(token, cancelImmediately: false);
                        token.ThrowIfCancellationRequested();
                    }

                    // EnsureMainThread
                    if ((underlying[i].Flags & BitEnsureMainThread) != 0 && !GameThread.IsCurrent)
                    {
#if LOG_TRACE_TO_FILE
                        _eventFileWriter.WriteLine("{0}  EnsureMainThread", traceNum);
#endif
                        await UniTask.SwitchToMainThread(token);
                        token.ThrowIfCancellationRequested();
                    }

                    // RequireActiveLayout
                    if ((underlying[i].Flags & BitRequireActiveLayout) != 0 && !_warfare.IsLayoutActive())
                    {
#if LOG_TRACE_TO_FILE
                        _eventFileWriter.WriteLine("{0}  BitRequireActiveLayout", traceNum);
#endif
                        continue;
                    }

                    // Invoke handler
#if LOG_TRACE_TO_FILE
                    _eventFileWriter.WriteLine("{0}  Invoking {1} ({2}).", traceNum, Accessor.Formatter.Format(underlying[i].Listener.GetType()!), Accessor.Formatter.Format(underlying[i].Model!));
#endif
                    UniTask invokeResult = InvokeListener(ref underlying[i], eventArgs, serviceProvider, token);
                    if (invokeResult.Status != UniTaskStatus.Succeeded)
                    {
                        await invokeResult;
#if LOG_TRACE_TO_FILE
                        _eventFileWriter.WriteLine("{0}  Done invoking {1} ({2}).", traceNum, Accessor.Formatter.Format(underlying[i].Listener.GetType()!), Accessor.Formatter.Format(underlying[i].Model!));
#endif
                        token.ThrowIfCancellationRequested();
                    }
                    else
                    {
#if LOG_TRACE_TO_FILE
                        _eventFileWriter.WriteLine("{0}  Done invoking {1} ({2}).", traceNum, Accessor.Formatter.Format(underlying[i].Listener.GetType()!), Accessor.Formatter.Format(underlying[i].Model!));
#endif
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

                    GetInfo(underlying[i].Model!, (underlying[i].Flags & BitIsAsync) != 0, listenerType, out EventListenerInfo info);

                    ILogger logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(listenerType);
                    bool cancelled;
                    if (ex is OperationCanceledException)
                    {
                        cancelled = true;
                        logger.LogInformation(ex, "Execution of event handler {0} cancelled by CancellationToken.", info.Method);
                    }
                    else
                    {
                        cancelled = false;
                        logger.LogError(ex, "Error executing event handler: {0}.", info.Method);
                    }

                    if (eventArgs is not ICancellable c)
                    {
                        if (cancelled)
                            break;

                        continue;
                    }

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

            Interlocked.Decrement(ref _activeEvents);

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

    private static readonly MethodInfo InvokeAsyncOpenGenericMtd = typeof(EventDispatcher).GetMethod(nameof(InvokeAsyncOpenGeneric), BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static readonly MethodInfo InvokeNormalOpenGenericMtd = typeof(EventDispatcher).GetMethod(nameof(InvokeNormalOpenGeneric), BindingFlags.NonPublic | BindingFlags.Instance)!;

    private UniTask InvokeListener<TEventArgs>(ref EventListenerResult result, TEventArgs args, IServiceProvider serviceProvider, CancellationToken token) where TEventArgs : class
    {
        // create and cache generic versions of InvokeAsyncOpenGeneric and InvokeNormalOpenGeneric (below)
        //  to invoke different levels of listeners
        if ((result.Flags & BitIsAsync) != 0)
        {
            // shortcut for highest level (will be used most)
            if (result.Model == typeof(TEventArgs))
            {
                return ((IAsyncEventListener<TEventArgs>)result.Listener).HandleEventAsync(args, serviceProvider, token);
            }

            if (!_asyncGeneratedInvokeMethods.TryGetValue(result.Model!, out Func<object, object, IServiceProvider, CancellationToken, UniTask> invoker))
            {
                _asyncGeneratedInvokeMethods.Add(result.Model!,
                    invoker = (Func<object, object, IServiceProvider, CancellationToken, UniTask>)InvokeAsyncOpenGenericMtd
                        .MakeGenericMethod(result.Model)
                        .CreateDelegate(typeof(Func<object, object, IServiceProvider, CancellationToken, UniTask>), this)
                );
            }

            return invoker!(result.Listener, args, serviceProvider, token);
        }
        else
        {
            // shortcut for highest level (will be used most)
            if (result.Model == typeof(TEventArgs))
            {
                ((IEventListener<TEventArgs>)result.Listener).HandleEvent(args, serviceProvider);
                return UniTask.CompletedTask;
            }

            if (!_syncGeneratedInvokeMethods.TryGetValue(result.Model!, out Action<object, object, IServiceProvider> invoker))
            {
                _syncGeneratedInvokeMethods.Add(result.Model!,
                    invoker = (Action<object, object, IServiceProvider>)InvokeNormalOpenGenericMtd
                        .MakeGenericMethod(result.Model)
                        .CreateDelegate(typeof(Action<object, object, IServiceProvider>), this)
                );
            }

            invoker!(result.Listener, args, serviceProvider);
            return UniTask.CompletedTask;
        }
    }

    [UsedImplicitly]
    private UniTask InvokeAsyncOpenGeneric<TEventArgs>(object listener, object args, IServiceProvider serviceProvider, CancellationToken token) where TEventArgs : class
    {
        return ((IAsyncEventListener<TEventArgs>)listener).HandleEventAsync((TEventArgs)args, serviceProvider, token);
    }

    [UsedImplicitly]
    private void InvokeNormalOpenGeneric<TEventArgs>(object listener, object args, IServiceProvider serviceProvider) where TEventArgs : class
    {
        ((IEventListener<TEventArgs>)listener).HandleEvent((TEventArgs)args, serviceProvider);
    }

    private EventInvocationListenerCache GetEventListenersCache<TEventArgs>(out IServiceProvider serviceProvider) where TEventArgs : class
    {
        Type type = typeof(TEventArgs);
        EventInvocationListenerCache cache;

        lock (_listenerCaches)
        {
            bool isScope = _warfare.IsLayoutActive();
            ILifetimeScope scope = isScope ? _warfare.ScopedProvider : _warfare.ServiceProvider;

            _scopedServiceProvider ??= scope.Resolve<IServiceProvider>();
            serviceProvider = _scopedServiceProvider;

            if (_listenerCaches.TryGetValue(type, out cache))
            {
                return cache;
            }

            cache.ModelInfo = type.GetAttributeSafe<EventModelAttribute>();

            List<EventListenerResult> eventListeners = ListPool<EventListenerResult>.claim();

            // find all services assignable from ILayoutPhaseListener<phase.GetType()>
            FindServices<TEventArgs>(scope, eventListeners);

            cache.Results = eventListeners;

            _listenerCaches.Add(type, cache);
        }

        return cache;
    }

    // checks to see if a service registration can be created from the given service provider scope
    private static bool InScope(ILifetimeScope scope, IComponentRegistration registration, out ILifetimeScope applicableScope)
    {
        applicableScope = scope;
        ISharingLifetimeScope? sharingScope = scope as ISharingLifetimeScope;
        switch (registration.Lifetime)
        {
            case MatchingScopeLifetime matchingScopeLifetime:
                if (sharingScope == null)
                    return false;
                for (ILifetimeScope? childScope = scope; childScope != null; childScope = (childScope as ISharingLifetimeScope)?.ParentLifetimeScope)
                {
                    if (childScope.Tag != null && matchingScopeLifetime.TagsToMatch.Contains(childScope.Tag))
                    {
                        applicableScope = childScope;
                        return true;
                    }
                }
                return false;

            case RootScopeLifetime or CurrentScopeLifetime:
                if (sharingScope != null)
                    applicableScope = sharingScope.RootLifetimeScope;
                return true;

            default:
                if (sharingScope == null)
                    return false;
                
                try
                {
                    if (registration.Lifetime.FindScope(sharingScope) is { } s)
                    {
                        applicableScope = s;
                        return true;
                    }

                    return false;
                }
                catch
                {
                    return false;
                }
        }
    }

    // finds all registered IEventListenerProvider types and caches them in a list.
    private void FindEventListenerProviders(ILifetimeScope scope, List<IEventListenerProvider> providers)
    {
        // find all IEventListenerProvider services but ignore unscoped ones
        IEnumerable<Parameter> parameters = Array.Empty<Parameter>();

        Type? noOpType = typeof(ILifetimeScope).Assembly.GetType("Autofac.Core.Registration.ExternalComponentRegistration+NoOpActivator");

        List<ILifetimeScope> scopes = GetScopes(scope);
        for (int j = 0; j < scopes.Count; ++j)
        {
            IComponentRegistry compReg = scopes[j].ComponentRegistry;
            foreach (IComponentRegistration serviceRegistration in compReg.Registrations)
            {
                if (noOpType is not null && noOpType.IsInstanceOfType(serviceRegistration.Activator))
                    continue;

                if (!InScope(scope, serviceRegistration, out ILifetimeScope applicableScope))
                    continue;

                Service? service = serviceRegistration.Services.FirstOrDefault(x => x is IServiceWithType t && t.ServiceType == typeof(IEventListenerProvider));
                if (service == null)
                    continue;
                
                Type? concreteType = serviceRegistration.Services
                    .OfType<IServiceWithType>()
                    .Where(x => !x.ServiceType.IsInterface)
                    .OrderByDescending(x => x.ServiceType, TypeComparer.Instance)
                    .FirstOrDefault()?.ServiceType;

                IEventListenerProvider? implementation = null;
                try
                {
                    // resolve component from its registration
                    if (concreteType != null)
                    {
                        for (Type? baseType = concreteType; baseType != null && typeof(IEventListenerProvider).IsAssignableFrom(baseType); baseType = baseType.BaseType)
                        {
                            implementation = (IEventListenerProvider?)scope.ResolveOptional(baseType);
#if LOG_RESOLVE_STEPS
                            if (implementation != null)
                            {
                                _logger.LogConditional("Resolved concrete listener provider {0}: {1} - {2}", baseType, implementation.GetType(), implementation.GetHashCode());
                                break;
                            }
#endif
                        }

                        if (implementation == null)
                        {
                            _logger.LogWarning("Unable to resolve service {0} for listener provider.", serviceRegistration.Activator);
                            continue;
                        }
                    }
                    else
                    {
                        ServiceRegistration reg = new ServiceRegistration(serviceRegistration.ResolvePipeline, serviceRegistration);
                        implementation = (IEventListenerProvider)applicableScope.ResolveComponent(new ResolveRequest(service, reg, parameters, serviceRegistration));
                        
                        if (serviceRegistration.Sharing != InstanceSharing.None)
                            _logger.LogWarning("Resolved listener provider from service (may cause duplicating service issues): {0} - {1}", implementation.GetType(), implementation.GetHashCode());
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error resolving service {0} for listener provider.", serviceRegistration.Activator);
                    continue;
                }

                int priority = implementation.GetType().GetPriority();
                for (int i = 0; i < providers.Count; ++i)
                {
                    if (providers[i].GetType().GetPriority() >= priority)
                    {
                        continue;
                    }

                    providers.Insert(i, implementation);
                    break;
                }

                providers.Add(implementation);
            }
        }

        _logger.LogDebug("Reset event caches. Found new listener providers: {0}.", providers.Select(x => x.GetType()));
        ListPool<ILifetimeScope>.release(scopes);
    }

    // enumerates through the scope and all it's parent scopes because Autofac makes it awful to loop through all services
    private static List<ILifetimeScope> GetScopes(ILifetimeScope scope)
    {
        List<ILifetimeScope> scopes = ListPool<ILifetimeScope>.claim();
        scopes.Add(scope);
        if (scope is not ISharingLifetimeScope s)
            return scopes;

        for (ISharingLifetimeScope? next = s.ParentLifetimeScope; next != null; next = next.ParentLifetimeScope)
        {
            scopes.Add(next);
        }

        return scopes;
    }

    // find and instantiate all handler services for this event model. This does not include handlers returned by IEventListenerProvider services
    private void FindServices<TEventArgs>(ILifetimeScope scope, List<EventListenerResult> eventListeners) where TEventArgs : class
    {
        IEnumerable<Parameter> parameters = Array.Empty<Parameter>();

        Type? noOpType = typeof(ILifetimeScope).Assembly.GetType("Autofac.Core.Registration.ExternalComponentRegistration+NoOpActivator");

        List<ILifetimeScope> scopes = GetScopes(scope);
        for (int j = 0; j < scopes.Count; ++j)
        {
            ILifetimeScope scopeLevel = scopes[j];
            IComponentRegistry compReg = scopeLevel.ComponentRegistry;
            foreach (IComponentRegistration serviceRegistration in compReg.Registrations)
            {
                if (noOpType is not null && noOpType.IsInstanceOfType(serviceRegistration.Activator))
                    continue;

#if DEBUG
                using IDisposable? logScope = _logger.BeginScope(serviceRegistration.Activator);
#endif
                if (!InScope(scope, serviceRegistration, out ILifetimeScope applicableScope))
                    continue;

                object? implementation = null;
                foreach (Service service in serviceRegistration.Services)
                {
                    if (service is not IServiceWithType serviceWithType)
                        continue;

                    Type serviceType = serviceWithType.ServiceType;
                    if (!serviceType.IsInterface || !serviceType.IsConstructedGenericType)
                        continue;

                    // check generic type
                    Type genericTypeDef = serviceType.GetGenericTypeDefinition();
                    bool isAsync = false;
                    if (genericTypeDef != typeof(IEventListener<>))
                    {
                        if (genericTypeDef != typeof(IAsyncEventListener<>))
                            continue;

                        if (!typeof(IAsyncEventListener<TEventArgs>).IsAssignableFrom(serviceType))
                            continue;

                        isAsync = true;
                    }
                    else if (!typeof(IEventListener<TEventArgs>).IsAssignableFrom(serviceType))
                        continue;

                    if (implementation == null)
                    {
                        Type? concreteType = serviceRegistration.Services
                            .OfType<IServiceWithType>()
                            .Where(x => !x.ServiceType.IsInterface)
                            .OrderByDescending(x => x.ServiceType, TypeComparer.Instance)
                            .FirstOrDefault()?.ServiceType;
                        
                        try
                        {
                            // resolve component from its registration
                            if (concreteType != null)
                            {
                                for (Type? baseType = concreteType; baseType != null && serviceType.IsAssignableFrom(baseType); baseType = baseType.BaseType)
                                {
                                    implementation = scope.ResolveOptional(baseType);
                                    if (implementation != null)
                                    {
#if LOG_RESOLVE_STEPS
                                        _logger.LogInformation("Resolved concrete {0}: {1} - {2}", baseType, implementation.GetType(), implementation.GetHashCode());
#endif
                                        break;
                                    }
                                }

                                if (implementation == null)
                                {
                                    _logger.LogWarning("Unable to resolve service {0} for event args {1}.", serviceRegistration.Activator, typeof(TEventArgs));
                                    continue;
                                }
                            }
                            else
                            {
                                ServiceRegistration reg = new ServiceRegistration(serviceRegistration.ResolvePipeline, serviceRegistration);
                                implementation = applicableScope.ResolveComponent(new ResolveRequest(service, reg, parameters, serviceRegistration));

                                if (serviceRegistration.Sharing != InstanceSharing.None)
                                    _logger.LogWarning("Resolved from service (may cause duplicating service issues): {0} - {1}", implementation.GetType(), implementation.GetHashCode());
                            }

                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error resolving service {0} for event args {1}.", serviceRegistration.Activator, typeof(TEventArgs));
                            continue;
                        }
                    }

                    EventListenerResult result = new EventListenerResult { Listener = implementation, Flags = (byte)((isAsync ? 1 : 0) * BitIsAsync), Model = serviceType.GetGenericArguments()[0] };
                    FillResults(ref result);
                    InsertEventListener(ref result, eventListeners);
                }
            }
        }

        ListPool<ILifetimeScope>.release(scopes);
    }

    private static void InsertEventListener(ref EventListenerResult result, List<EventListenerResult> eventListeners)
    {
        // insert an event listener from a IEventListenerProvider that has already been filled
        //  this avoids resorting the entire lisst

        EventListenerResult[] underlying = eventListeners.GetUnderlyingArray();
        for (int i = 0; i < underlying.Length; ++i)
        {
            if (CompareEventListenerResults(ref underlying[i], ref result) <= 0)
            {
                continue;
            }

            eventListeners.Insert(i, result);
            return;
        }

        eventListeners.Add(result);
    }
    
    // allows a few different methods for syncronizing 'on request' events so multiple can't be running at once
    //   ex. two requests to place a fob item at the same time would possibly lead to outdated 'do we have enough build supply' checks
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
                    _logger.LogDebug("Invoke {0} - Locking on type {0} for player {1}.", type, b.Player?.Steam64.m_SteamID.ToString() ?? "null");
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
                        _logger.LogDebug("Invoke {0} - Locking on request type {1} for player {2}.", type, modelInfo.RequestModel, b.Player?.Steam64.m_SteamID.ToString() ?? "null");
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
                    _logger.LogDebug("Invoke {0} - Locking on tag \"{1}\" for player {2}.", type, tag, b.Player?.Steam64.m_SteamID.ToString() ?? "null");
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
            _logger.LogDebug("Invoke {0} - Locking on type {0} for player {1}.", type, bucket.Player?.Steam64.m_SteamID.ToString() ?? "null");
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
                _logger.LogDebug("Invoke {0} - Locking on request type {1} for player {2}.", type, modelInfo.RequestModel, bucket.Player?.Steam64.m_SteamID.ToString() ?? "null");
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
                _logger.LogDebug("Invoke {0} - Locking on tag \"{1}\" for player {2}.", type, tag, bucket.Player?.Steam64.m_SteamID.ToString() ?? "null");
#endif
            }
        }
        else
        {
            _logger.LogWarning("Event arg {0} has Per-Player synchronization setting but doesn't implement {1}.", type, typeof(IPlayerEvent));
        }
    }

    // adds results from IEventListenerProvider services to the working eventListeners list and initializes them
    //  if any returned listener has more than one applicable interface, it is added multiple times, in order of reverse hierarchy ('object', 'IPlayerEvent', 'PlayerJoined')
    private void AddProviderResults(object eventListner, List<EventListenerResult> eventListeners, bool isAsync, Type argType)
    {
        byte flag = (byte)((isAsync ? 1 : 0) * BitIsAsync);
        Type?[] interfaces = eventListner.GetType().GetInterfaces();
        int ct = 0, index = -1;
        for (int i = 0; i < interfaces.Length; ++i)
        {
            Type intx = interfaces[i]!;
            if (!intx.IsConstructedGenericType)
            {
                interfaces[i] = null;
                continue;
            }

            Type argTypeIntx = intx.GetGenericArguments()[0];
            if (!argTypeIntx.IsAssignableFrom(argType))
            {
                interfaces[i] = null;
                continue;
            }

            interfaces[i] = argTypeIntx;
            ++ct;
            index = i;
        }

        // interfaces now contains all listening arg types that are assignable from argType

        if (ct == 1)
        {
            // take a shortcut if only one is found
            EventListenerResult newResult = new EventListenerResult { Listener = eventListner, Flags = flag, Model = interfaces[index] };
            FillResults(ref newResult);
            InsertEventListener(ref newResult, eventListeners);
            return;
        }

        // continuously remove the 'lowest' type argument in the class hierarchy, ordering them in order of class hierarchy
        for (int c = 0; c < ct; ++c)
        {
            Type? lowestType = null;
            int lowestTypeIndex = -1;
            for (int i = 0; i < interfaces.Length; ++i)
            {
                Type? argTypeIntx = interfaces[i];
                if (argTypeIntx is null)
                    continue;

                if (lowestTypeIndex == -1 || argTypeIntx.IsAssignableFrom(lowestType))
                {
                    lowestType = argTypeIntx;
                    lowestTypeIndex = i;
                }
            }

            if (lowestTypeIndex == -1)
                break;

            interfaces[lowestTypeIndex] = null;
            EventListenerResult newResult = new EventListenerResult { Listener = eventListner, Flags = flag, Model = lowestType };
            FillResults(ref newResult);
            InsertEventListener(ref newResult, eventListeners);
        }
    }

    // set up flags and priority
    private void FillResults(ref EventListenerResult result)
    {
        bool isAsync = (result.Flags & BitIsAsync) != 0;
        GetInfo(result.Model!, isAsync, result.Listener.GetType(), out EventListenerInfo info);

        result.Priority = info.Priority;
        result.Flags = (byte)(result.Flags
                              | ((info.MustRunInstantly & !isAsync ? 1 : 0) * BitMustRunInstantly)
                              | ((info.EnsureMainThread ? 1 : 0) * BitEnsureMainThread)
                              | ((info.RequireActiveLayout ? 1 : 0) * BitRequireActiveLayout)
                              | ((info.RequireNextFrame ? 1 : 0) * BitRequireNextFrame)
                              | ((info.MustRunLast ? 1 : 0) * BitMustRunLast)
                              );
    }

    private void GetInfo(Type modelType, bool isAsync, Type listenerType, out EventListenerInfo info)
    {
        EventListenerCacheKey key = default;
        key.ModelType = modelType;
        key.ListenerType = listenerType;
        key.IsAsync = isAsync;

        if (_listeners.TryGetValue(key, out info))
            return;
        
        // caches the MethodInfos for the methods in the event listener interfaces
        Dictionary<Type, MethodInfo> methodCache = isAsync ? _asyncInvokeMethods : _syncInvokeMethods;

        if (!methodCache.TryGetValue(modelType, out MethodInfo? interfaceMethod))
        {
            methodCache.Add(modelType, interfaceMethod = isAsync ? GetAsyncHandlerMethod(modelType) : GetNormalHandlerMethod(modelType));
        }

        // find the MethodInfo for the method in the listener class, not the one in the interface so we can get the attribute from that
        MethodInfo? implementedMethod = Accessor.GetImplementedMethod(listenerType, interfaceMethod);
        EventListenerAttribute? attribute = implementedMethod?.GetAttributeSafe<EventListenerAttribute>();

        info = default;
        info.EnsureMainThread = attribute is not { HasRequiredMainThread: true } ? !isAsync : attribute.RequiresMainThread;
        if (attribute != null)
        {
            info.Priority = attribute.Priority;
            info.MustRunInstantly = attribute.MustRunInstantly;
            info.RequireActiveLayout = attribute.RequireActiveLayout;
            info.RequireNextFrame = attribute.RequireNextFrame;
            info.MustRunLast = attribute.MustRunLast;
        }

        if (info is { MustRunLast: true, MustRunInstantly: true })
        {
            throw new NotSupportedException("Event listeners can not use the 'MustRunLast' and 'MustRunInstantly' properties together.");
        }

        if (info is { RequireNextFrame: true, MustRunInstantly: true })
        {
            throw new NotSupportedException("Event listeners can not use the 'RequireNextFrame' and 'MustRunInstantly' properties together.");
        }

        if (isAsync && info.MustRunInstantly)
        {
            throw new NotSupportedException("Async event listeners can not use the 'MustRunInstantly' property.");
        }

        info.Method = implementedMethod ?? interfaceMethod;
        _listeners.Add(key, info);
    }

    private static MethodInfo GetNormalHandlerMethod(Type eventArgsType)
    {
        Type declType = typeof(IEventListener<>).MakeGenericType(eventArgsType);
        return declType.GetMethod(nameof(IEventListener<object>.HandleEvent), BindingFlags.Instance | BindingFlags.Public)
               ?? throw new InvalidOperationException($"Unable to find method {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(IEventListener<object>.HandleEvent))
                   .DeclaredIn(declType, isStatic: false)
                   .WithParameter(eventArgsType, "e")
                   .WithParameter<IServiceProvider>("serviceProvider")
                   .ReturningVoid())}."
                );
    }
    private static MethodInfo GetAsyncHandlerMethod(Type eventArgsType)
    {
        Type declType = typeof(IAsyncEventListener<>).MakeGenericType(eventArgsType);
        return declType.GetMethod(nameof(IAsyncEventListener<object>.HandleEventAsync), BindingFlags.Instance | BindingFlags.Public)
               ?? throw new InvalidOperationException($"Unable to find method {Accessor.ExceptionFormatter.Format(new MethodDefinition(nameof(IAsyncEventListener<object>.HandleEventAsync))
                   .DeclaredIn(declType, isStatic: false)
                   .WithParameter(eventArgsType, "e")
                   .WithParameter<IServiceProvider>("serviceProvider")
                   .WithParameter<CancellationToken>("token")
                   .ReturningVoid())}."
                );
    }

    private class TypeComparer : IComparer<Type>
    {
        public static readonly TypeComparer Instance = new TypeComparer();

        public int Compare(Type x, Type y)
        {
            if (x.IsSubclassOf(y))
                return 1;
            if (y.IsSubclassOf(x))
                return -1;

            return 0;
        }
    }

    private static int CompareEventListenerResults(ref EventListenerResult a, ref EventListenerResult b)
    {
        // handle MustRunLast
        if ((a.Flags & BitMustRunLast) != 0)
        {
            if ((b.Flags & BitMustRunLast) == 0)
                return -1;

            int cmp = b.Priority.CompareTo(a.Priority);
            if (cmp != 0)
                return cmp;
        }
        else if ((b.Flags & BitMustRunLast) != 0)
        {
            return 1;
        }
        else
        {
            // handle MustRunInstantly then compare priority
            int cmp = (a.Flags & BitMustRunInstantly) != (b.Flags & BitMustRunInstantly) ? (b.Flags & BitMustRunInstantly) - 1 : b.Priority.CompareTo(a.Priority);
            if (cmp != 0)
                return cmp;
        }


        if (a.Model is not null && b.Model is not null && a.Model != b.Model)
        {
            // IEventListener<IPlayerEvent> comes before IEventListener<PlayerXxxArgs : PlayerEvent>
            int cmp = !b.Model.IsAssignableFrom(a.Model) ? -(a.Model.IsAssignableFrom(b.Model) ? 1 : 0) : 1;
            if (cmp != 0)
                return cmp;
        }

        // main thread and not in same class, main thread comes first
        if ((a.Flags & BitEnsureMainThread) != (b.Flags & BitEnsureMainThread))
            return (b.Flags & BitEnsureMainThread) != 0 ? 1 : -1;

        return 0;
    }

    private struct EventInvocationListenerCache
    {
        public List<EventListenerResult> Results;
        public EventModelAttribute? ModelInfo;
    }

    private struct EventListenerInfo
    {
        public int Priority;
        public bool RequireActiveLayout;
        public bool EnsureMainThread;
        public bool MustRunInstantly;
        public bool RequireNextFrame;
        public bool MustRunLast;
        public MethodInfo Method;
    }

    private struct EventListenerResult
    {
        public object Listener;
        public Type? Model;

        // to save struct size, trying to keep performance good for these
        // bits: 0 (1 ): IsAsyncListener
        //       1 (2 ): MustRunInstantly
        //       2 (4 ): EnsureMainThread
        //       3 (8 ): RequireActiveLayout
        //       4 (16): RequireNextFrame
        //       5 (32): MustRunLast
        public byte Flags;
        public int Priority;
    }

    private struct EventListenerCacheKey : IEquatable<EventListenerCacheKey>
    {
        public Type ListenerType;
        public Type ModelType;
        public bool IsAsync;
        public override readonly int GetHashCode()
        {
            return HashCode.Combine(ListenerType, ModelType) * (IsAsync ? -1 : 1);
        }
        public readonly bool Equals(EventListenerCacheKey other)
        {
            return IsAsync == other.IsAsync && ListenerType == other.ListenerType && ModelType == other.ModelType;
        }
        public override readonly bool Equals(object? obj)
        {
            return obj is EventListenerCacheKey key && Equals(key);
        }
    }
}
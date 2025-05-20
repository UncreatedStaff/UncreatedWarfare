#if DEBUG
//#define LOG_SYNCHRONIZATION_STEPS
//#define LOG_EVENT_LISTENERS
//#define LOG_RESOLVE_STEPS
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
using Uncreated.Warfare.Vehicles;
using Service = Autofac.Core.Service;
#if TELEMETRY
using System.Diagnostics;
#endif

namespace Uncreated.Warfare.Events;

/// <summary>
/// Handles dispatching <see cref="IEventListener{TEventArgs}"/> and <see cref="IAsyncEventListener{TEventArgs}"/> objects.
/// </summary>
public partial class EventDispatcher : IHostedService, IDisposable
{
#if TELEMETRY
    private readonly ActivitySource _activitySource;
#endif

    // bits for EventListenerResult.Flags
    private const int BitIsAsync = 1;
    // CompareEventListenerResults relies on this being 2, check if changing
    private const int BitMustRunInstantly = 2;
    private const int BitEnsureMainThread = 4;
    private const int BitRequireActiveLayout = 8;
    private const int BitRequireNextFrame = 16;
    private const int BitMustRunLast = 32;
    private const int BitSkipAtRuntime = 64;

    private bool _isTryingToShutDown;
    private long _eventId;

    private readonly EventSynchronizer _eventSynchronizer;

    private readonly WarfareModule _warfare;
    private readonly IPlayerService _playerService;
    private readonly ProjectileSolver _projectileSolver;
    private readonly CancellationToken _unloadToken;
    private readonly ILogger<EventDispatcher> _logger;
    private readonly List<object> _listenerBuffer;

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

    public EventDispatcher(ILoggerFactory loggerFactory,
        ProjectileSolver projectileSolver,
        IPlayerService playerService,
        EventSynchronizer eventSynchronizer,
        WarfareModule module)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _unloadToken = _cancellationTokenSource.Token;

        _logger = loggerFactory.CreateLogger<EventDispatcher>();
        _loggerFactory = loggerFactory;
        _projectileSolver = projectileSolver;
        _playerService = playerService;
        _eventSynchronizer = eventSynchronizer;
        _warfare = module;

        _listenerBuffer = new List<object>(128);

        _warfare.LayoutStarted += OnLayoutStarted;

        _eventProviders = new List<IEventListenerProvider>(8);
        FindEventListenerProviders(_warfare.IsLayoutActive() ? _warfare.ScopedProvider : _warfare.ServiceProvider, _eventProviders);

#if TELEMETRY
        _activitySource = WarfareModule.CreateActivitySource();
#endif
    }
    void IDisposable.Dispose()
    {
        _warfare.LayoutStarted -= OnLayoutStarted;
#if TELEMETRY
        _activitySource.Dispose();
#endif
    }

    private void SubscribePlayerEvents(WarfarePlayer player)
    {
        player.UnturnedPlayer.life.onHurt += OnPlayerHurt;
    }

    public async UniTask WaitForEvents()
    {
        _isTryingToShutDown = true;

        await UniTask.SwitchToThreadPool();

        SpinWait.SpinUntil(_eventSynchronizer.IsCleared, TimeSpan.FromSeconds(10));
    }

    UniTask IHostedService.StartAsync(CancellationToken token)
    {
        ListPool<EventListenerResult>.warmup(8);
        ListPool<InProgressEventTask>.warmup(32);

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
        PlayerCrafting.OnCraftBlueprintRequestedV2 += PlayerCraftingCraftBlueprintRequested;

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

    UniTask IHostedService.StopAsync(CancellationToken token)
    {
        /* Provider */
        Provider.onEnemyConnected -= ProviderOnServerConnectedEarly;
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
        PlayerCrafting.OnCraftBlueprintRequestedV2 -= PlayerCraftingCraftBlueprintRequested;

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

        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();

        return UniTask.CompletedTask;
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

    /// <summary>
    /// Invoke an event with the given arguments.
    /// </summary>
    /// <returns>If the action should continue if <paramref name="eventArgs"/> is <see cref="ICancellable"/>, otherwise <see langword="true"/>.</returns>
    public async UniTask<bool> DispatchEventAsync<TEventArgs>(TEventArgs eventArgs, CancellationToken token = default, bool allowAsync = true) where TEventArgs : class
    {
        if (_isTryingToShutDown)
        {
            if (eventArgs is ICancellable c)
            {
                c.Cancel();
            }

            _logger.LogWarning($"Event {typeof(TEventArgs)} dispatched after shutdown has started.");

            return false;
        }

#if TELEMETRY
        string stackTrace = new StackTrace().ToString();
#endif
        await UniTask.SwitchToMainThread(token);

#if TELEMETRY
        Activity.Current = null;
        using Activity? activity = _activitySource.StartActivity($"Invoke {Accessor.Formatter.Format(typeof(TEventArgs))}.");
        activity?.SetTag("model-type", Accessor.Formatter.Format(typeof(TEventArgs)));
        activity?.SetTag("cancellable", eventArgs is ICancellable);
        activity?.SetTag("player", eventArgs is IPlayerEvent playerArgs ? playerArgs.Steam64.m_SteamID : 0ul);
        activity?.AddTag("allow-async", allowAsync);
        activity?.AddTag("has-cancellation-token", token.CanBeCanceled);
        activity?.AddTag("source", stackTrace);
#endif

        Type type = typeof(TEventArgs);

        EventInvocationListenerCache cache = GetEventListenersCache<TEventArgs>(out IServiceProvider serviceProvider);

        bool isPure = allowAsync && cache.ModelInfo is { SynchronizationContext: EventSynchronizationContext.Pure };
        if (isPure && eventArgs is ICancellable)
        {
#if TELEMETRY
            activity?.SetStatus(ActivityStatusCode.Error, "Pure cancellable not supported.");
#endif
            throw new InvalidOperationException($"A pure ICancellable not supported for event model {Accessor.ExceptionFormatter.Format<TEventArgs>()}.");
        }
#if TELEMETRY
        activity?.SetTag("pure", isPure);
#endif

        List<EventListenerResult> eventListeners = ListPool<EventListenerResult>.claim();
        eventListeners.AddRange(cache.Results);

        // IEventListenerProviders
        try
        {
            foreach (IEventListenerProvider provider in _eventProviders)
            {
                provider.AppendListeners(eventArgs, _listenerBuffer);
            }

            foreach (object listener in _listenerBuffer)
            {
                AddProviderResults(listener, eventListeners, type);
            }
        }
        finally
        {
            _listenerBuffer.Clear();
        }

        int ct = eventListeners.Count;

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
#if TELEMETRY
        activity?.AddTag("event-listener-ct", eventListeners.Count);
        for (int i = 0; i < eventListeners.Count; i++)
        {
            EventListenerResult result = eventListeners[i];
            activity?.AddTag($"event-listener-{i}", $"{{ \"listener\": \"{Accessor.Formatter.Format(result.Listener.GetType())}\", \"model\": \"{Accessor.Formatter.Format(result.Model)}\" }}");
        }
#endif

        SynchronizationEntry? syncEntry = null;
        EventModelAttribute? modelInfo = null;

        long eventId = Interlocked.Increment(ref _eventId);

        // enter sync buckets
        if (allowAsync)
        {
            modelInfo = cache.ModelInfo;
            if (modelInfo is { SynchronizationContext: EventSynchronizationContext.Global or EventSynchronizationContext.PerPlayer })
            {
                _logger.LogTrace($"  {eventId}  Locking {type}...");
#if TELEMETRY
                activity?.AddEvent(new ActivityEvent("Locking..."));
#endif
                syncEntry = await _eventSynchronizer.EnterEvent(eventArgs, eventId, modelInfo);
#if TELEMETRY
                activity?.AddEvent(new ActivityEvent("Locked..."));
                activity?.AddTag("locked", true);
#endif
            }
#if TELEMETRY
            else
            {
                activity?.AddTag("locked", false);
            }
#endif
        }
        else if (cache.ModelInfo is { SynchronizationContext: EventSynchronizationContext.Global or EventSynchronizationContext.PerPlayer })
        {
#if TELEMETRY
            activity?.SetStatus(ActivityStatusCode.Error, "SynchronizationContext not supported.");
#endif
            throw new InvalidOperationException($"SynchronizationContext not supported for event model {Accessor.ExceptionFormatter.Format<TEventArgs>()} when allowAsync = false.");
        }
#if TELEMETRY
        else
        {
            activity?.AddTag("locked", false);
        }
#endif


        _logger.LogTrace($"  {eventId}  Invoke {type} - Dispatching event for {ct} listener(s), " +
                         $"locked: {syncEntry != null} ({syncEntry?.WaitCount ?? 0} waiting).");

        List<InProgressEventTask>? sequentialTaskList = isPure ? ListPool<InProgressEventTask>.claim() : null;

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
                    result.Flags |= BitSkipAtRuntime;
                }
            }

            bool hasSkippedToNextFrame = false;

            if (eventArgs is ICancellable { IsCancelled: true } cancellable)
            {
                _logger.LogTrace($"  {eventId}  Stopping ({Accessor.Formatter.Format(type)}): Cancelled at startup.");
                return !cancellable.IsActionCancelled;
            }

            int purePriority = 0;

            for (int i = 0; i < ct; i++)
            {
                EventListenerResult result = underlying[i];
                if (isPure && purePriority != result.Priority && sequentialTaskList!.Count > 0)
                {
                    await WaitForPureEvents(sequentialTaskList, underlying, serviceProvider, token, eventId);
                }

#if TELEMETRY
                Activity? invokeActivity = _activitySource.CreateActivity(
                    $"Invoke listener {Accessor.Formatter.Format(result.Listener.GetType())} for model {Accessor.Formatter.Format(type)}.",
                    ActivityKind.Internal,
                    parentContext: activity?.Context ?? default
                );

                if (invokeActivity != null)
                {
                    invokeActivity.AddTag("flags", Convert.ToString(result.Flags, 2));
                    invokeActivity.AddTag("priority", result.Priority);
                    invokeActivity.AddTag("listener-type", Accessor.Formatter.Format(result.Listener.GetType()));
                    invokeActivity.AddTag("model-type", Accessor.Formatter.Format(result.Model));
                }
#endif
                // skipped
                if ((result.Flags & BitSkipAtRuntime) != 0)
                {
                    _logger.LogTrace($"  {eventId}  Skipped: {result.Listener.GetType()}.");
#if TELEMETRY
                    invokeActivity?.Start();
                    invokeActivity?.SetStatus(ActivityStatusCode.Error, "Child async event listener, skipped.");
                    invokeActivity?.Dispose();
#endif
                    continue;
                }

#if TELEMETRY
                bool disposeActivity = true;
#endif
                try
                {
                    // RequireNextFrame
                    if (!hasSkippedToNextFrame && (result.Flags & BitRequireNextFrame) != 0)
                    {
                        _logger.LogTrace($"  {eventId}  Waiting for {result.Listener.GetType()} ({result.Model}): RequireNextFrame.");
                        hasSkippedToNextFrame = true;
#if TELEMETRY
                        invokeActivity?.AddEvent(new ActivityEvent("Skipping to next frame"));
#endif
                        await UniTask.NextFrame(token, cancelImmediately: false);
#if TELEMETRY
                        invokeActivity?.AddEvent(new ActivityEvent("Skipped to next frame"));
#endif
                        token.ThrowIfCancellationRequested();
                    }

                    // EnsureMainThread
                    if ((result.Flags & BitEnsureMainThread) != 0 && !GameThread.IsCurrent)
                    {
                        _logger.LogTrace($"  {eventId}  Not invoking {result.Listener.GetType()} ({result.Model}): EnsureMainThread.");
#if TELEMETRY
                        invokeActivity?.AddEvent(new ActivityEvent("Switching to main thread"));
#endif
                        await UniTask.SwitchToMainThread(token);
#if TELEMETRY
                        invokeActivity?.AddEvent(new ActivityEvent("Switched to main thread"));
#endif
                        token.ThrowIfCancellationRequested();
                    }

                    // RequireActiveLayout
                    if ((result.Flags & BitRequireActiveLayout) != 0 && !_warfare.IsLayoutActive())
                    {
                        _logger.LogTrace($"  {eventId}  Not invoking {result.Listener.GetType()} ({result.Model}): RequireActiveLayout.");
#if TELEMETRY
                        invokeActivity?.Start();
                        invokeActivity?.SetStatus(ActivityStatusCode.Error, "Requires active layout, skipped.");
#endif
                        continue;
                    }

                    // Invoke handler
                    _logger.LogTrace($"  {eventId}  Invoking {result.Listener.GetType()} ({result.Model}).");
#if TELEMETRY
                    bool previousActionCancelled = eventArgs is ICancellable { IsActionCancelled: true };
                    invokeActivity?.Start();
                    invokeActivity?.AddEvent(new ActivityEvent("Invoking"));
#endif
                    UniTask invokeResult = InvokeListener(ref result, eventArgs, serviceProvider, token);
                    if (invokeResult.Status != UniTaskStatus.Succeeded)
                    {
                        if (isPure)
                        {
                            InProgressEventTask task = default;
                            task.Index = i;
                            task.Task = invokeResult;
#if TELEMETRY
                            task.Activity = invokeActivity;
#endif
                            sequentialTaskList!.Add(task);
                            purePriority = result.Priority;
#if TELEMETRY
                            disposeActivity = false;
                            invokeActivity?.AddEvent(new ActivityEvent("Invoked with continuation, queued simultaniously"));
#endif
                        }
                        else
                        {
                            await invokeResult;
#if TELEMETRY
                            invokeActivity?.AddEvent(new ActivityEvent("Invoked with continuation"));
#endif
                            _logger.LogTrace($"  {eventId}  Invoked with continuation {result.Listener.GetType()} ({result.Model}).");
                            token.ThrowIfCancellationRequested();
                        }
                    }
                    else
                    {
#if TELEMETRY
                        invokeActivity?.AddEvent(new ActivityEvent("Invoked without continuation"));
#endif
                        _logger.LogTrace($"  {eventId}  Invoked without continuation {result.Listener.GetType()} ({result.Model}).");
                    }

#if TELEMETRY
                    if (!previousActionCancelled && eventArgs is ICancellable { IsActionCancelled: true })
                    {
                        invokeActivity?.AddEvent(new ActivityEvent("Action cancelled"));
                    }
#endif

                    if (eventArgs is ICancellable { IsCancelled: true })
                    {
#if TELEMETRY
                        invokeActivity?.AddEvent(new ActivityEvent("Event cancelled"));
                        invokeActivity?.SetStatus(ActivityStatusCode.Ok, "Will stop.");
#endif
                        _logger.LogTrace($"  {eventId}  Stopping at {result.Listener.GetType()} ({Accessor.Formatter.Format(result.Model)}): Cancelled.");
                        // check if a MustRunLast cancelled (they shouldn't be allowed to)
                        if ((result.Flags & BitMustRunLast) != 0)
                            throw new InvalidOperationException($"Event cancelled by a listener using 'MustRunLast': {
                                Accessor.ExceptionFormatter.Format(result.Model)} in {
                                    Accessor.ExceptionFormatter.Format(result.Listener.GetType())}."
                            );
                        break;
                    }
#if TELEMETRY
                    invokeActivity?.SetStatus(ActivityStatusCode.Ok, "Will continue.");
#endif
                }
                catch (ControlException) { }
                catch (OperationCanceledException ex) when (token.IsCancellationRequested)
                {
#if TELEMETRY
                    invokeActivity?.AddEvent(new ActivityEvent("Cancelled by cancellation token"));
                    invokeActivity?.AddTag("exception", Accessor.Formatter.Format(ex.GetType()));
                    invokeActivity?.AddTag("exception-info", ex.ToString());
#endif
                    Type listenerType = result.Listener.GetType();
                    ILogger logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(listenerType);

                    logger.LogInformation(ex, $"Execution of event handler {Accessor.Formatter.Format(listenerType)} for {Accessor.Formatter.Format(result.Model)} cancelled by CancellationToken.");

                    if (eventArgs is not ICancellable c)
                    {
#if TELEMETRY
                        invokeActivity?.SetStatus(ActivityStatusCode.Error, "Cancellation not supported.");
#endif
                        break;
                    }

                    c.Cancel();
#if TELEMETRY
                    invokeActivity?.SetStatus(ActivityStatusCode.Ok, "Will stop.");
#endif
                    break;
                }
                catch (Exception ex)
                {
#if TELEMETRY
                    invokeActivity?.AddEvent(new ActivityEvent("Exception thrown"));
                    invokeActivity?.AddTag("exception", Accessor.Formatter.Format(ex.GetType()));
                    invokeActivity?.AddTag("exception-info", ex.ToString());
#endif
                    _logger.LogTrace($"  {eventId}  Threw exception: {result.Listener.GetType()} ({result.Model}).");
                    await UniTask.SwitchToMainThread(CancellationToken.None);

                    Type listenerType = result.Listener.GetType();
                    ILogger logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(listenerType);

                    logger.LogError(ex, $"Execution of event handler {eventId} {Accessor.Formatter.Format(listenerType)} for {Accessor.Formatter.Format(result.Model)} threw an error.");

#if TELEMETRY
                    invokeActivity?.SetStatus(ActivityStatusCode.Error, "Error thrown.");
#endif

                    if (eventArgs is not ICancellable c)
                        continue;
                    
                    c.Cancel();
                    break;
                }
#if TELEMETRY
                finally
                {
                    if (disposeActivity)
                        invokeActivity?.Dispose();
                }
#endif
            }

            if (isPure && sequentialTaskList!.Count > 0)
            {
                await WaitForPureEvents(sequentialTaskList, underlying, serviceProvider, token, eventId);
            }

            bool canContinue = eventArgs is not ICancellable { IsActionCancelled: true };
#if TELEMETRY
            activity?.SetTag("can-continue", canContinue);
            activity?.SetStatus(ActivityStatusCode.Ok, "Exited.");
#endif
            return canContinue;
        }
        finally
        {
            _logger.LogTrace($"  finally invoked - eventId: {eventId}.");
#if TELEMETRY
            activity?.AddEvent(new ActivityEvent("Event invocation ended"));
#endif
            await UniTask.SwitchToMainThread();
            
            if (syncEntry != null)
            {
                _eventSynchronizer.ExitEvent(syncEntry, modelInfo);
#if TELEMETRY
                activity?.AddEvent(new ActivityEvent("Unlocked"));
#endif
            }

            if (sequentialTaskList != null)
                ListPool<InProgressEventTask>.release(sequentialTaskList);
            ListPool<EventListenerResult>.release(eventListeners);
            _logger.LogTrace($"  Exited event {type}, eventId: {eventId}.");
#if TELEMETRY
            activity?.AddEvent(new ActivityEvent("Event exited"));
#endif
        }
    }

    // pure events are events where the handlers would have no effect on each other, consequencly they can be ran sequentially with no ill effects
    // they will be grouped by priority, however, so only one set of priorities will be started at once
    private async UniTask WaitForPureEvents(List<InProgressEventTask> sequentialTaskList, EventListenerResult[] results, IServiceProvider serviceProvider, CancellationToken token, long eventId)
    {
        UniTask[] tasks = new UniTask[sequentialTaskList.Count];
        for (int j = 0; j < sequentialTaskList.Count; ++j)
            tasks[j] = TryWrap(sequentialTaskList, j);

        // wait for pending tasks
        await UniTask.WhenAll(tasks);

        // check for exceptions
        for (int j = 0; j < sequentialTaskList.Count; ++j)
        {
            InProgressEventTask eventTask = sequentialTaskList[j];
            switch (eventTask.Exception)
            {
                case null:
#if TELEMETRY
                    eventTask.Activity?.SetStatus(ActivityStatusCode.Ok, "Will continue.");
#endif
                    break;

                // cancelled
                case OperationCanceledException ex when token.IsCancellationRequested:

                    ref EventListenerResult result = ref results[eventTask.Index];

#if TELEMETRY
                    eventTask.Activity?.AddEvent(new ActivityEvent("Cancelled by cancellation token"));
                    eventTask.Activity?.AddTag("exception", Accessor.Formatter.Format(ex.GetType()));
                    eventTask.Activity?.AddTag("exception-info", ex.ToString());
#endif
                    _logger.LogTrace($"  {eventId}  Threw cancellation: {result.Listener.GetType()} ({result.Model}).");

                    Type listenerType = result.Listener.GetType();
                    ILogger logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(listenerType);

                    logger.LogInformation(ex, $"Execution of event handler {Accessor.Formatter.Format(listenerType)} for {Accessor.Formatter.Format(result.Model)} cancelled by CancellationToken.");

#if TELEMETRY
                    eventTask.Activity?.SetStatus(ActivityStatusCode.Error, "Cancellation not supported.");
#endif
                    break;

                // exception thrown
                case var ex:

                    result = ref results[eventTask.Index];

#if TELEMETRY
                    eventTask.Activity?.AddEvent(new ActivityEvent("Exception thrown"));
                    eventTask.Activity?.AddTag("exception", Accessor.Formatter.Format(ex.GetType()));
                    eventTask.Activity?.AddTag("exception-info", ex.ToString());
#endif
                    _logger.LogTrace($"  {eventId}  Threw exception: {result.Listener.GetType()} ({result.Model}).");

                    listenerType = result.Listener.GetType();
                    logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(listenerType);

                    logger.LogError(ex, $"Execution of event handler {eventId} {Accessor.Formatter.Format(listenerType)} for {Accessor.Formatter.Format(result.Model)} threw an error.");

#if TELEMETRY
                    eventTask.Activity?.SetStatus(ActivityStatusCode.Error, "Error thrown.");
#endif
                    break;
            }
#if TELEMETRY
            eventTask.Activity?.Dispose();
#endif
        }

        sequentialTaskList.Clear();
    }

    private static async UniTask TryWrap(List<InProgressEventTask> sequentialTaskList, int i)
    {
        InProgressEventTask task = sequentialTaskList[i];
        try
        {
            await task.Task;
        }
        catch (Exception ex)
        {
            task.Exception = ex;
            sequentialTaskList[i] = task;
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

            if (!_asyncGeneratedInvokeMethods.TryGetValue(result.Model, out Func<object, object, IServiceProvider, CancellationToken, UniTask> invoker))
            {
                _asyncGeneratedInvokeMethods.Add(result.Model,
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

            if (!_syncGeneratedInvokeMethods.TryGetValue(result.Model, out Action<object, object, IServiceProvider> invoker))
            {
                _syncGeneratedInvokeMethods.Add(result.Model,
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
            bool isCacheEligable = FindServices<TEventArgs>(scope, eventListeners);

            cache.Results = eventListeners;

            if (isCacheEligable)
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

    /// <returns><see langword="true"/> if the result can be cached.</returns>
    private bool FindServices<TEventArgs>(ILifetimeScope scope, List<EventListenerResult> eventListeners) where TEventArgs : class
    {
        IEnumerable<Parameter> parameters = Array.Empty<Parameter>();

        Type? noOpType = typeof(ILifetimeScope).Assembly.GetType("Autofac.Core.Registration.ExternalComponentRegistration+NoOpActivator");

        bool cacheEligable = true;
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
                            cacheEligable = false;
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

        return cacheEligable;
    }

    private static void InsertEventListener(ref EventListenerResult result, List<EventListenerResult> eventListeners)
    {
        // insert an event listener from a IEventListenerProvider that has already been filled
        //  this avoids resorting the entire lisst

        for (int i = 0; i < eventListeners.Count; i++)
        {
            EventListenerResult r = eventListeners[i];
            if (CompareEventListenerResults(in r, in result) <= 0)
            {
                continue;
            }

            eventListeners.Insert(i, result);
            return;
        }

        eventListeners.Add(result);
    }

    private readonly Dictionary<TypePair, CachedInterfaceInfo[]> _interfaceCache = new Dictionary<TypePair, CachedInterfaceInfo[]>(32);

    private struct TypePair : IEquatable<TypePair>
    {
        public Type Listener;
        public Type Model;

        public override bool Equals(object? obj)
        {
            return obj is TypePair tp && tp.Listener == Listener && tp.Model == Model;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Listener, Model);
        }

        public bool Equals(TypePair other)
        {
            return Listener == other.Listener && Model == other.Model;
        }
    }

    // adds results from IEventListenerProvider services to the working eventListeners list and initializes them
    //  if any returned listener has more than one applicable interface, it is added multiple times, in order of reverse hierarchy ('object', 'IPlayerEvent', 'PlayerJoined')
    private void AddProviderResults(object eventListener, List<EventListenerResult> eventListeners, Type argType)
    {
        Type listenerType = eventListener.GetType();

        TypePair tp = default;
        tp.Listener = listenerType;
        tp.Model = argType;

        if (_interfaceCache.TryGetValue(tp, out CachedInterfaceInfo[] interfaces))
        {
            for (int i = 0; i < interfaces.Length; ++i)
            {
                ref CachedInterfaceInfo info = ref interfaces[i];
                EventListenerResult newResult = new EventListenerResult { Listener = eventListener, Flags = info.Flag, Model = info.Model };
                FillResults(ref newResult);
                InsertEventListener(ref newResult, eventListeners);
            }

            return;
        }

        Type?[] typeInterfaces = eventListener.GetType().GetInterfaces();
        CachedInterfaceInfo[]? cachedInfo = null;
        bool firstIsAsync = false;
        int ct = 0, index = -1;
        for (int i = 0; i < typeInterfaces.Length; ++i)
        {
            Type intx = typeInterfaces[i]!;
            if (!intx.IsConstructedGenericType)
            {
                typeInterfaces[i] = null;
                continue;
            }

            Type genDef = intx.GetGenericTypeDefinition();

            Type argTypeIntx = intx.GetGenericArguments()[0];
            bool isAsync = genDef == typeof(IAsyncEventListener<>);
            if (!argTypeIntx.IsAssignableFrom(argType) || !isAsync && genDef != typeof(IEventListener<>))
            {
                typeInterfaces[i] = null;
                continue;
            }

            typeInterfaces[i] = argTypeIntx;
            if (ct == 1)
            {
                cachedInfo = new CachedInterfaceInfo[typeInterfaces.Length];
                cachedInfo[index].Model = typeInterfaces[index]!;
                cachedInfo[index].Flag = (byte)((firstIsAsync ? 1 : 0) * BitIsAsync);
            }
            if (ct > 0)
            {
                cachedInfo![i].Model = argTypeIntx;
                cachedInfo[i].Flag = (byte)((isAsync ? 1 : 0) * BitIsAsync);
            }
            ++ct;
            index = i;
            firstIsAsync = isAsync;
        }

        // interfaces now contains all listening arg types that are assignable from argType

        if (ct == 1)
        {
            CachedInterfaceInfo info = default;
            info.Model = typeInterfaces[index]!;
            info.Flag = (byte)((firstIsAsync ? 1 : 0) * BitIsAsync);

            CachedInterfaceInfo[] cache = [ info ];
            _interfaceCache.Add(tp, cache);
            // take a shortcut if only one is found
            EventListenerResult newResult = new EventListenerResult { Listener = eventListener, Flags = info.Flag, Model = info.Model };
            FillResults(ref newResult);
            InsertEventListener(ref newResult, eventListeners);
            return;
        }

        CachedInterfaceInfo[] outputArray = new CachedInterfaceInfo[ct];

        // continuously remove the 'lowest' type argument in the class hierarchy, ordering them in order of class hierarchy
        for (int c = 0; c < ct; ++c)
        {
            Type? lowestType = null;
            int lowestTypeIndex = -1;
            for (int i = 0; i < typeInterfaces.Length; ++i)
            {
                Type? argTypeIntx = typeInterfaces[i];
                if (argTypeIntx is null)
                    continue;

                if (lowestTypeIndex == -1 || argTypeIntx.IsAssignableFrom(lowestType))
                {
                    lowestType = argTypeIntx;
                    lowestTypeIndex = i;
                }
            }

            if (lowestTypeIndex == -1)
            {
                Array.Resize(ref outputArray, c);
                break;
            }

            ref CachedInterfaceInfo info = ref cachedInfo![lowestTypeIndex];
            outputArray[c] = info;
            typeInterfaces[lowestTypeIndex] = null;
            EventListenerResult newResult = new EventListenerResult { Listener = eventListener, Flags = info.Flag, Model = lowestType! };
            FillResults(ref newResult);
            InsertEventListener(ref newResult, eventListeners);
        }

        _interfaceCache.Add(tp, outputArray);
    }

    // set up flags and priority
    private void FillResults(ref EventListenerResult result)
    {
        bool isAsync = (result.Flags & BitIsAsync) != 0;
        GetInfo(result.Model, isAsync, result.Listener.GetType(), out EventListenerInfo info);

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

    private static int CompareEventListenerResults(in EventListenerResult a, in EventListenerResult b)
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

        // main thread and not in same class, main thread comes first
        if ((a.Flags & BitEnsureMainThread) != (b.Flags & BitEnsureMainThread))
            return (b.Flags & BitEnsureMainThread) != 0 ? 1 : -1;

        // require next frame and not in same class, main thread comes first
        if ((a.Flags & BitRequireNextFrame) != (b.Flags & BitRequireNextFrame))
            return (a.Flags & BitRequireNextFrame) != 0 ? 1 : -1;

        if (a.Model != b.Model)
        {
            // IEventListener<IPlayerEvent> comes before IEventListener<PlayerXxxArgs : PlayerEvent>
            int cmp = !b.Model.IsAssignableFrom(a.Model) ? -(a.Model.IsAssignableFrom(b.Model) ? 1 : 0) : 1;
            if (cmp != 0)
                return cmp;
        }

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
    private struct CachedInterfaceInfo
    {
        public Type Model;
        public byte Flag;
    }

    private struct InProgressEventTask
    {
        public int Index;
        public UniTask Task;
        public Exception? Exception;
#if TELEMETRY
        public Activity? Activity;
#endif
    }

    private struct EventListenerResult
    {
        public object Listener;
        public Type Model;

        // to save struct size, trying to keep performance good for these
        // bits: 0 (1 ): IsAsyncListener
        //       1 (2 ): MustRunInstantly
        //       2 (4 ): EnsureMainThread
        //       3 (8 ): RequireActiveLayout
        //       4 (16): RequireNextFrame
        //       5 (32): MustRunLast
        //       6 (64): Skipped at runtime
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
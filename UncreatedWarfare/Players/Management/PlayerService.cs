using DanielWillett.ReflectionTools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Players.PendingTasks;
using Uncreated.Warfare.Plugins;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Util.List;

namespace Uncreated.Warfare.Players.Management;

/// <summary>
/// Handles keeping track of online <see cref="WarfarePlayer"/>'s.
/// </summary>
/// <remarks>Inject <see cref="IPlayerService"/>.</remarks>
public class PlayerService : IPlayerService
{
    private readonly List<PlayerEventSubscription> _eventSubscriptions = new List<PlayerEventSubscription>(16);

    /// <summary>
    /// All types will be added to a player on join and removed on leave.
    /// They dont necessarily have to be <see cref="MonoBehaviour"/>'s but must implement <see cref="IPlayerComponent"/>.
    /// </summary>
    /// <remarks>
    /// Components can implement <see cref="IDisposable"/> if desired to run code before the player leaves.
    /// <see cref="MonoBehaviour"/>'s will be automatically destroyed.
    /// 
    /// Components can receive events, but any <see cref="IPlayerEvent"/> args will only be received if they're about the player that owns the component.
    /// </remarks>
    public readonly Type[] PlayerComponents;

    /// <summary>
    /// All types here will be created when a player starts to join (<see cref="PlayerPending"/>).
    /// They must implement <see cref="IPlayerPendingTask"/> and can not be <see cref="MonoBehaviour"/> components.
    /// </summary>
    public readonly Type[] PlayerTasks;

    // keep up with a separate array that's replaced every time so the value can be used in multi-threaded operations
    private WarfarePlayer[] _threadsafeList;
    private readonly TrackingList<WarfarePlayer> _onlinePlayers;
    private readonly PlayerDictionary<WarfarePlayer> _onlinePlayersDictionary;
    private readonly WarfareModule _warfare;

    // makes sure only one player is ever joining at once.
    internal readonly SemaphoreSlim PlayerJoinLock = new SemaphoreSlim(0, 1);

    /// <summary>
    /// Keeps track of data that's fetched during the connecting event.
    /// </summary>
    internal readonly List<PlayerTaskData> PendingTasks = new List<PlayerTaskData>(4);

    /// <inheritdoc />
    public ReadOnlyTrackingList<WarfarePlayer> OnlinePlayers
    {
        get
        {
            GameThread.AssertCurrent();
            return _readOnlyOnlinePlayers;
        }
    }

    private readonly ILoggerFactory _loggerFactory; 
    private readonly IServiceProvider _serviceProvider;
    private readonly ILifetimeScope _container;
    private IUserDataService? _userDataService;
    private readonly ReadOnlyTrackingList<WarfarePlayer> _readOnlyOnlinePlayers;

    public PlayerService(ILoggerFactory loggerFactory, ILifetimeScope lifetimeScope)
    {
        _container = lifetimeScope;
        _serviceProvider = lifetimeScope.Resolve<IServiceProvider>();
        _onlinePlayers = new TrackingList<WarfarePlayer>();
        _threadsafeList = Array.Empty<WarfarePlayer>();
        _onlinePlayersDictionary = new PlayerDictionary<WarfarePlayer>(Provider.maxPlayers);
        _readOnlyOnlinePlayers = _onlinePlayers.AsReadOnly();
        _loggerFactory = loggerFactory;

        _warfare = lifetimeScope.Resolve<WarfareModule>();

        List<Type> allTypes = Accessor.GetTypesSafe(lifetimeScope.Resolve<WarfarePluginLoader>().AllAssemblies);

        PlayerComponents = allTypes.Where(x => !x.IsIgnored() && !x.IsDefinedSafe<CompilerGeneratedAttribute>() && !x.IsAbstract && typeof(IPlayerComponent).IsAssignableFrom(x) && x.IsDefinedSafe<PlayerComponentAttribute>()).ToArray();
        PlayerTasks = allTypes.Where(x => !x.IsIgnored() && !x.IsDefinedSafe<CompilerGeneratedAttribute>() && !x.IsAbstract && typeof(IPlayerPendingTask).IsAssignableFrom(x) && x.IsDefinedSafe<PlayerTaskAttribute>()).ToArray();
    }

    /// <inheritdoc />
    public async ValueTask<IPlayer> CreateOfflinePlayerAsync(CSteamID steam64, CancellationToken token = default)
    {
        if (GetOnlinePlayerOrNullThreadSafe(steam64.m_SteamID) is { } pl)
            return pl;

        PlayerNames names = await (_userDataService ??= _container.Resolve<IUserDataService>())
            .GetUsernamesAsync(steam64.m_SteamID, token)
            .ConfigureAwait(false);

        return names.WasFound ? new OfflinePlayer(in names) : new OfflinePlayer(steam64);
    }

    /// <inheritdoc />
    public IReadOnlyList<WarfarePlayer> GetThreadsafePlayerList()
    {
        if (GameThread.IsCurrent)
        {
            return OnlinePlayers;
        }

        return _threadsafeList;
    }

    /// <inheritdoc />
    public Task TakePlayerConnectionLock(CancellationToken token)
    {
        return PlayerJoinLock.WaitAsync(token);
    }

    /// <inheritdoc />
    public void ReleasePlayerConnectionLock()
    {
        PlayerJoinLock.Release();
    }

    internal void ReinitializeScopedPlayerComponentServices()
    {
        IServiceProvider serviceProvider = _warfare.ScopedProvider.Resolve<IServiceProvider>();
        foreach (WarfarePlayer player in OnlinePlayers)
        {
            foreach (IPlayerComponent component in player.Components)
            {
                component.Init(serviceProvider, false);
            }
        }
    }

    internal WarfarePlayer CreateWarfarePlayer(Player player, in PlayerTaskData taskData)
    {
        lock (_onlinePlayersDictionary)
        {
            if (_onlinePlayersDictionary.ContainsPlayer(player))
            {
                throw new ArgumentException("This player has already been added to PlayerService.", nameof(player));
            }

            ILogger logger = _loggerFactory.CreateLogger(
                player.channel.owner.playerID.steamID.m_SteamID.ToString("D17", CultureInfo.InvariantCulture)
            );

            IPlayerComponent[] components = AddComponents(player);

            WarfarePlayer joined = new WarfarePlayer(this, player, in taskData, taskData.Player, logger, components, _serviceProvider);

            // copy so components dont mess with it
            PlayerEventSubscription[] subs = _eventSubscriptions.ToArray();

            _onlinePlayers.Add(joined);
            _onlinePlayersDictionary.Add(joined, joined);

            WarfarePlayer[] newList = new WarfarePlayer[_threadsafeList.Length + 1];
            Array.Copy(_threadsafeList, 0, newList, 0, _threadsafeList.Length);
            newList[^1] = joined;
            _threadsafeList = newList;

            IServiceProvider serviceProvider = _warfare.ScopedProvider.Resolve<IServiceProvider>();

            for (int i = 0; i < components.Length; ++i)
            {
                IPlayerComponent component = components[i];

                component.Init(serviceProvider, true);
            }

            foreach (PlayerEventSubscription sub in subs)
            {
                sub.Apply(joined);
            }

            return joined;
        }
    }

    internal WarfarePlayer OnPlayerLeft(WarfarePlayer player)
    {
        lock (_onlinePlayersDictionary)
        {
            RemoveComponents(player);

            _onlinePlayers.Remove(player);
            _onlinePlayersDictionary.Remove(player.Steam64);

            for (int i = 0; i < _threadsafeList.Length; ++i)
            {
                WarfarePlayer pl = _threadsafeList[i];
                if (!ReferenceEquals(pl, player))
                    continue;
                
                WarfarePlayer[] newList = new WarfarePlayer[_threadsafeList.Length - 1];
                if (i != 0)
                    Array.Copy(_threadsafeList, 0, newList, 0, i);
                if (i != _threadsafeList.Length - 1)
                    Array.Copy(_threadsafeList, i + 1, newList, i, _threadsafeList.Length - i - 1);
                
                _threadsafeList = newList;
                break;
            }

            player.ApplyOfflineState();
            return player;
        }
    }

    private IPlayerComponent[] AddComponents(Player player)
    {
        IPlayerComponent[] components = new IPlayerComponent[PlayerComponents.Length];
        for (int i = 0; i < PlayerComponents.Length; i++)
        {
            Type type = PlayerComponents[i];
            if (!typeof(IPlayerComponent).IsAssignableFrom(type))
            {
                throw new InvalidOperationException($"Type {Accessor.ExceptionFormatter.Format(type)} does not " +
                                                    $"implement {Accessor.ExceptionFormatter.Format<IPlayerComponent>()}.");
            }

            IPlayerComponent component;
            if (type.IsSubclassOf(typeof(Component)))
            {
                component = (IPlayerComponent)player.gameObject.AddComponent(type);
            }
            else
            {
                component = (IPlayerComponent)ReflectionUtility.CreateInstanceFixed(_serviceProvider, type, [ player ]);
            }

            components[i] = component;
        }

        return components;
    }

    private static void RemoveComponents(WarfarePlayer player)
    {
        foreach (IPlayerComponent component in player.Components)
        {
            if (component is IDisposable disposable)
            {
                disposable.Dispose();
            }

            if (component is Component unityComponent && unityComponent != null)
            {
                Object.Destroy(unityComponent);
            }
        }
    }

    internal PlayerTaskData StartPendingPlayerTasks(PlayerPending args, CancellationTokenSource src, CancellationToken token)
    {
        ILifetimeScope scope = _container.BeginLifetimeScope();
        IServiceProvider sp = scope.Resolve<IServiceProvider>();

        IPlayerPendingTask[] playerTasks = new IPlayerPendingTask[PlayerTasks.Length];
        Task<bool>[] tasks = new Task<bool>[playerTasks.Length];
        for (int i = 0; i < PlayerTasks.Length; i++)
        {
            Type type = PlayerTasks[i];
            if (!typeof(IPlayerPendingTask).IsAssignableFrom(type))
            {
                throw new InvalidOperationException($"Type {Accessor.ExceptionFormatter.Format(type)} does not " +
                                                    $"implement {Accessor.ExceptionFormatter.Format<IPlayerPendingTask>()}.");
            }

            IPlayerPendingTask task = (IPlayerPendingTask)ReflectionUtility.CreateInstanceFixed(sp, type, [args]);
            playerTasks[i] = task;
            Task<bool> t = task.RunAsync(args, token);

            if (task.CanReject)
            {
                tasks[i] = t.ContinueWith(static (task, src) =>
                {
                    if (task.IsCanceled)
                        return false;

                    if (task.IsFaulted || !task.Result)
                    {
                        ((CancellationTokenSource)src).Cancel();
                    }

                    // forces exceptions to bubble up
                    return task.GetAwaiter().GetResult();
                }, src, token);
            }
            else
            {
                tasks[i] = t;
            }
        }

        return new PlayerTaskData(args, src, playerTasks, tasks, scope);
    }

    /// <inheritdoc />
    public bool IsPlayerOnline(ulong steam64)
    {
        GameThread.AssertCurrent();

        return _onlinePlayersDictionary.ContainsPlayer(steam64);
    }

    /// <inheritdoc />
    public bool IsPlayerOnlineThreadSafe(ulong steam64)
    {
        lock (_onlinePlayersDictionary)
        {
            return _onlinePlayersDictionary.ContainsPlayer(steam64);
        }
    }

    /// <inheritdoc />
    public WarfarePlayer GetOnlinePlayer(ulong steam64)
    {
        GameThread.AssertCurrent();

        // ReSharper disable once InconsistentlySynchronizedField
        if (!_onlinePlayersDictionary.TryGetValue(Unsafe.As<ulong, CSteamID>(ref steam64), out WarfarePlayer? player))
            throw new PlayerOfflineException(steam64);

        return player;
    }

    /// <inheritdoc />
    public WarfarePlayer? GetOnlinePlayerOrNull(ulong steam64)
    {
        GameThread.AssertCurrent();

        // ReSharper disable once InconsistentlySynchronizedField
        _onlinePlayersDictionary.TryGetValue(Unsafe.As<ulong, CSteamID>(ref steam64), out WarfarePlayer? player);
        return player;
    }

    /// <inheritdoc />
    public WarfarePlayer GetOnlinePlayerThreadSafe(ulong steam64)
    {
        CSteamID steamId = Unsafe.As<ulong, CSteamID>(ref steam64);
        if (GameThread.IsCurrent)
        {
            if (!_onlinePlayersDictionary.TryGetValue(steamId, out WarfarePlayer? player))
                throw new PlayerOfflineException(steam64);

            return player;
        }

        lock (_onlinePlayersDictionary)
        {
            if (!_onlinePlayersDictionary.TryGetValue(steamId, out WarfarePlayer? player))
                throw new PlayerOfflineException(steam64);

            return player;
        }
    }

    /// <inheritdoc />
    public WarfarePlayer? GetOnlinePlayerOrNullThreadSafe(ulong steam64)
    {
        CSteamID steamId = Unsafe.As<ulong, CSteamID>(ref steam64);
        if (GameThread.IsCurrent)
        {
            _onlinePlayersDictionary.TryGetValue(steamId, out WarfarePlayer? player);
            return player;
        }

        lock (_onlinePlayersDictionary)
        {
            _onlinePlayersDictionary.TryGetValue(steamId, out WarfarePlayer? player);
            return player;
        }
    }

    internal record struct PlayerTaskData(
        PlayerPending Player,
        CancellationTokenSource TokenSource,
        IPlayerPendingTask[] PendingTasks,
        Task<bool>[] Tasks,
        ILifetimeScope? Scope
    );

    /// <inheritdoc />
    public void SubscribeToPlayerEvent<TDelegate>(Action<WarfarePlayer, TDelegate> subscribe, TDelegate value) where TDelegate : MulticastDelegate
    {
        if (subscribe == null)
            throw new ArgumentNullException(nameof(subscribe));

        if (value == null)
            return;

        if (GameThread.IsCurrent)
        {
            if (_eventSubscriptions.Exists(x => x.Equals(value)))
                return;

            _eventSubscriptions.Add(new PlayerEventSubscription<TDelegate>(subscribe, value));
            foreach (WarfarePlayer player in _onlinePlayers)
            {
                subscribe(player, value);
            }
        }
        else
        {
            Action<WarfarePlayer, TDelegate> a1 = subscribe;
            TDelegate v1 = value;
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();

                SubscribeToPlayerEvent(a1, v1);
            });
        }
    }

    /// <inheritdoc />
    public void UnsubscribeFromPlayerEvent<TDelegate>(Action<WarfarePlayer, TDelegate> unsubscribe, TDelegate value) where TDelegate : MulticastDelegate
    {
        if (unsubscribe == null)
            throw new ArgumentNullException(nameof(unsubscribe));

        if (value == null)
            return;

        if (GameThread.IsCurrent)
        {
            int index = _eventSubscriptions.FindIndex(x => x.Equals(value));
            if (index < 0)
                return;

            _eventSubscriptions.RemoveAt(index);
            foreach (WarfarePlayer player in _onlinePlayers)
            {
                unsubscribe(player, value);
            }
        }
        else
        {
            Action<WarfarePlayer, TDelegate> a1 = unsubscribe;
            TDelegate v1 = value;
            UniTask.Create(async () =>
            {
                await UniTask.SwitchToMainThread();

                UnsubscribeFromPlayerEvent(a1, v1);
            });
        }
    }

    private abstract class PlayerEventSubscription
    {
        public abstract void Apply(WarfarePlayer player);
        public abstract bool Equals(Delegate dele);
    }

    private class PlayerEventSubscription<TDelegate> : PlayerEventSubscription where TDelegate : MulticastDelegate
    {
        private readonly Action<WarfarePlayer, TDelegate> _subscribeCallback;
        private readonly TDelegate _handler;

        /// <inheritdoc />
        public PlayerEventSubscription(Action<WarfarePlayer, TDelegate> subscribeCallback, TDelegate handler)
        {
            _subscribeCallback = subscribeCallback;
            _handler = handler;
        }

        /// <inheritdoc />
        public override void Apply(WarfarePlayer player)
        {
            _subscribeCallback(player, _handler);
        }

        /// <inheritdoc />
        public override bool Equals(Delegate dele)
        {
            return dele == _handler;
        }
    }
}

/// <summary>
/// Thrown when a player can't be found in <see cref="IPlayerService"/>.
/// </summary>
public class PlayerOfflineException : Exception
{
    public PlayerOfflineException(ulong steam64) : base(
        $"Could not get WarfarePlayer '{steam64.ToString("D17", CultureInfo.InvariantCulture)}' " +
        $"because they were not found in the list of online players.")
    {

    }
}
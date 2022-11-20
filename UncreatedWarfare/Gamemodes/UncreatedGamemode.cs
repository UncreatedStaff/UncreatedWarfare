using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Players;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Commands.VanillaRework;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Items;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Gamemodes.Flags.Hardpoint;
using Uncreated.Warfare.Gamemodes.Flags.Invasion;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Gamemodes.UI;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Ranks;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Traits;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using Action = System.Action;

namespace Uncreated.Warfare.Gamemodes;

public abstract class Gamemode : BaseAsyncSingletonComponent, IGamemode, ILevelStartListenerAsync, IReloadableSingleton, ITranslationArgument
{
    public const float MATCH_PRESENT_THRESHOLD = 0.65f;
    public const string GAMEMODE_RELOAD_KEY = "gamemode";
    protected static readonly Vector3 BLOCKER_SPAWN_ROTATION = new Vector3(270f, 0f, 180f);
    public static readonly List<KeyValuePair<Type, float>> GamemodeRotation = new List<KeyValuePair<Type, float>>();
    public static readonly List<KeyValuePair<string, Type>> Gamemodes = new List<KeyValuePair<string, Type>>
    {
        new KeyValuePair<string, Type>("TeamCTF", typeof(TeamCTF)),
        new KeyValuePair<string, Type>("Invasion", typeof(Invasion)),
        new KeyValuePair<string, Type>("TDM", typeof(TeamDeathmatch.TeamDeathmatch)),
        new KeyValuePair<string, Type>("Insurgency", typeof(Insurgency.Insurgency)),
        new KeyValuePair<string, Type>("Conquest", typeof(Conquest)),
        new KeyValuePair<string, Type>("Hardpoint", typeof(Hardpoint))
    };
    internal static GamemodeConfig ConfigObj;
    public static WinToastUI WinToastUI;
    public Whitelister Whitelister;
    public CooldownManager Cooldowns;
    public Tips Tips;
    public Coroutine EventLoopCoroutine;
    public bool isPendingCancel;
    public event Action? StagingPhaseOver;
    internal string shutdownMessage = string.Empty;
    internal bool shutdownAfterGame = false;
    internal ulong shutdownPlayer = 0;
    protected readonly string _name;
    protected float _startTime = 0f;
    protected long _gameID;
    protected int _ticks = 0;
    protected float _stagingSeconds;
    protected EState _state;
    private float _eventLoopSpeed;
    private bool useEventLoop;
    private bool _isPreLoading;
    private List<IUncreatedSingleton> _singletons;
    private bool wasLevelLoadedOnStart;
    private bool _hasOnReadyRan = false;
    private bool _hasTimeSynced = false;
    public event Action? OnGameTick;
    public bool LoadAsynchronous => true;
    public override bool AwaitLoad => true;
    public EState State => _state;
    public float StartTime => _startTime;
    public float StagingSeconds => _stagingSeconds;
    public float SecondsSinceStart => Time.realtimeSinceStartup - _startTime;
    public long GameID => _gameID;
    public static GamemodeConfigData Config => ConfigObj.Data;
    public string Name => _name;
    public float EventLoopSpeed => _eventLoopSpeed;
    public bool EveryMinute => _ticks % Mathf.RoundToInt(60f / _eventLoopSpeed) == 0;
    public bool Every30Seconds => _ticks % Mathf.RoundToInt(30f / _eventLoopSpeed) == 0;
    public bool Every15Seconds => _ticks % Mathf.RoundToInt(15f / _eventLoopSpeed) == 0;
    public bool Every10Seconds => _ticks % Mathf.RoundToInt(10f / _eventLoopSpeed) == 0;
    public string ReloadKey => GAMEMODE_RELOAD_KEY;
    public virtual bool UseWhitelist => true;
    public abstract string DisplayName { get; }
    public virtual bool TransmitMicWhileNotActive => true;
    public virtual bool ShowXPUI => true;
    public virtual bool ShowOFPUI => true;
    public virtual bool UseTips => true;
    public virtual bool AllowCosmetics => false;
    public virtual EGamemode GamemodeType => EGamemode.UNDEFINED;
    protected bool HasOnReadyRan => _hasOnReadyRan;
    public bool EndScreenUp => this is IEndScreen es && es.IsScreenUp;
    protected Gamemode(string name, float eventLoopSpeed)
    {
        this._name = name;
        this._eventLoopSpeed = eventLoopSpeed;
        this.useEventLoop = eventLoopSpeed > 0;
        this._state = EState.LOADING;
    }
    public void SetTiming(float newSpeed)
    {
        this._eventLoopSpeed = newSpeed;
        this.useEventLoop = newSpeed > 0;
    }
    public void AdvanceDelays(float seconds)
    {
        _startTime -= seconds;
        VehicleSpawner.UpdateSigns();
        TraitSigns.BroadcastAllTraitSigns();
        TimeSync();
    }
    protected virtual void OnAdvanceDelays(float seconds) { }
    public override async Task LoadAsync()
    {
        await UCWarfare.ToUpdate();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking(Name + " Load Sequence");
#endif
        if (this._singletons is null)
            this._singletons = new List<IUncreatedSingleton>(16);
        else
            this._singletons.Clear();
        _hasOnReadyRan = false;
        wasLevelLoadedOnStart = Level.isLoaded;
        _isPreLoading = true;
        InternalPreInit();
        await PreInit().ThenToUpdate();

        _isPreLoading = false;
        await Data.Singletons.LoadSingletonsInOrderAsync(_singletons).ConfigureAwait(false);
        await UCWarfare.ToUpdate();
        ThreadUtil.assertIsGameThread();

        InternalSubscribe();
        Subscribe();
        InternalPostInit();
        Task task = PostInit();
        if (!task.IsCompleted)
        {
            await task.ConfigureAwait(false);
            await UCWarfare.ToUpdate();
            ThreadUtil.assertIsGameThread();
        }
        if (this is IKitRequests)
        {
            Commands.ReloadCommand.ReloadKits();
        }
        Type[] interfaces = this.GetType().GetInterfaces();
        for (int i = 0; i < interfaces.Length; i++)
        {
            Type intx = interfaces[i];
            if (intx.IsGenericType && intx.GetGenericTypeDefinition() == typeof(IImplementsLeaderboard<,>) && intx.GenericTypeArguments.Length > 1)
            {
                Type tracker = intx.GenericTypeArguments[1];
                MethodInfo? method = intx.GetProperty("WarstatsTracker", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetSetMethod(true);
                method?.Invoke(this, new object[] { gameObject.AddComponent(tracker) });
                break;
            }
        }
        if (wasLevelLoadedOnStart)
        {
            for (int i = 0; i < _singletons.Count; ++i)
            {
                IUncreatedSingleton singleton = _singletons[i];
                if (singleton is ILevelStartListener l1)
                    l1.OnLevelReady();
                if (singleton is ILevelStartListenerAsync l2)
                {
                    task = l2.OnLevelReady();
                    if (!task.IsCompleted)
                    {
                        await task.ConfigureAwait(false);
                        await UCWarfare.ToUpdate();
                    }
                }
            }
            ThreadUtil.assertIsGameThread();
            InternalOnReady();
            task = OnReady();
            if (!task.IsCompleted)
            {
                await task.ConfigureAwait(false);
                await UCWarfare.ToUpdate();
                ThreadUtil.assertIsGameThread();
            }
            await PostOnReady().ConfigureAwait(false);
            await UCWarfare.ToUpdate();
            _hasOnReadyRan = true;
        }
    }
    public async Task ReloadAsync()
    {
        await UnloadAsync().ConfigureAwait(false);
        await UCWarfare.ToUpdate();
        ConfigObj.Reload();
        await LoadAsync().ConfigureAwait(false);
    }
    public void Reload() => throw new NotImplementedException();
    public override async Task UnloadAsync()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking(Name + " Unload Sequence");
#endif
        await UCWarfare.ToUpdate();
        Unsubscribe();
        InternalUnsubscribe();
        Task task = PreDispose();
        if (!task.IsCompleted)
        {
            await task.ConfigureAwait(false);
            await UCWarfare.ToUpdate();
            ThreadUtil.assertIsGameThread();
        }
        InternalPreDispose();
        await Data.Singletons.UnloadSingletonsInOrderAsync(_singletons).ConfigureAwait(false);
        await UCWarfare.ToUpdate();
        ThreadUtil.assertIsGameThread();
        InternalPostDispose();
        task = PostDispose();
        if (!task.IsCompleted)
        {
            await task.ConfigureAwait(false);
            await UCWarfare.ToUpdate();
            ThreadUtil.assertIsGameThread();
        }
    }

    /// <summary>Use to add <see cref="IUncreatedSingleton"/>s to be loaded.</summary>
    /// <remarks>Abstract</remarks>
    protected abstract Task PreInit();

    /// <summary>Called after all <see cref="IUncreatedSingleton"/>s have been loaded.</summary>
    /// <remarks>Abstract</remarks>
    protected virtual Task PostInit() => Task.CompletedTask;

    /// <summary>Called just before all <see cref="IUncreatedSingleton"/>s are unloaded.</summary>
    /// <remarks>Abstract</remarks>
    protected virtual Task PreDispose() => Task.CompletedTask;

    /// <summary>Called just after all <see cref="IUncreatedSingleton"/>s have been unloaded.</summary>
    /// <remarks>No base</remarks>
    protected virtual Task PostDispose() => Task.CompletedTask;

    /// <summary>If the level is already loaded, called after <see cref="PostInit"/>, otherwise called when the level is loaded.</summary>
    /// <remarks>No base, guaranteed to be called after all registered <see cref="ILevelStartListener.OnLevelReady"/>'s have been called.</remarks>
    protected virtual Task OnReady() => Task.CompletedTask;

    /// <summary>Called when a player tries to craft something.</summary>
    /// <remarks>No base, guaranteed to be called before all registered <see cref="ICraftingSettingsOverride.OnCraftRequested(CraftRequested)"/>'s have been called.</remarks>
    protected virtual void OnCraftRequested(CraftRequested e) { }

    /// <summary>Runs just before a game starts.</summary>
    /// <param name="isOnLoad">Whether this is the first game played on this singleton since running <see cref="LoadAsync"/>.</param>
    /// <remarks>Called from <see cref="StartNextGame(bool)"/></remarks>
    protected virtual Task PreGameStarting(bool isOnLoad) => Task.CompletedTask;

    /// <summary>Runs just after a game starts.</summary>
    /// <param name="isOnLoad">Whether this is the first game played on this singleton since running <see cref="LoadAsync"/>.</param>
    /// <remarks>Called from <see cref="StartNextGame(bool)"/></remarks>
    protected virtual Task PostGameStarting(bool isOnLoad) => Task.CompletedTask;

    /// <summary>Runs after all players have been initialized.</summary>
    /// <param name="isOnLoad">Whether this is the first game played on this singleton since running <see cref="LoadAsync"/>.</param>
    /// <remarks>Called from <see cref="StartNextGame(bool)"/></remarks>
    protected virtual Task PostPlayerInit(bool isOnLoad) => Task.CompletedTask;

    /// <summary>Ran when a player joins or per online player after the game starts.</summary>
    /// <remarks>No base</remarks>
    public virtual Task PlayerInit(UCPlayer player, bool wasAlreadyOnline) => Task.CompletedTask;

    /// <summary>Run in <see cref="EventLoopAction"/>, returns true if <param name="seconds"/> ago it would've also returned true. Based on tick speed and number of ticks.</summary>
    /// <remarks>Returns true if the second mark passed between the end of last tick and the start of this tick. Inlined when possible.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool EveryXSeconds(float seconds) => seconds <= 0f || _ticks * _eventLoopSpeed % seconds < _eventLoopSpeed;
    private void InternalPreInit()
    {
        AddSingletonRequirement(ref Cooldowns);
        if (UseWhitelist)
            AddSingletonRequirement(ref Whitelister);
        if (UseTips)
            AddSingletonRequirement(ref Tips);
    }
    private async Task InternalPlayerInit(UCPlayer player, bool wasAlreadyOnline)
    {
        ThreadUtil.assertIsGameThread();
        if (!player.IsOnline)
            return;
        Task task;
        player.HasInitedOnce = true;
        for (int i = 0; i < _singletons.Count; ++i)
        {
            IUncreatedSingleton singleton = _singletons[i];
            if (singleton is IPlayerPreInitListener l1)
                l1.OnPrePlayerInit(player, wasAlreadyOnline);
            if (singleton is ILevelStartListenerAsync l2)
            {
                task = l2.OnLevelReady();
                if (!task.IsCompleted)
                {
                    await task.ConfigureAwait(false);
                    await UCWarfare.ToUpdate();
                    if (!player.IsOnline)
                        return;
                }
            }
        }
        ThreadUtil.assertIsGameThread();
        if (!wasAlreadyOnline)
        {
            Task t2 = Points.UpdatePointsAsync(player, false);
            Task t3 = player.DownloadKits(false);
            Task t4 = OffenseManager.ApplyMuteSettings(player);
            await Data.DatabaseManager.RegisterLogin(player.Player).ConfigureAwait(false);
            await t2.ConfigureAwait(false);
            await t3.ConfigureAwait(false);
            await t4.ConfigureAwait(false);
        }
        await UCWarfare.ToUpdate();
        ThreadUtil.assertIsGameThread();
        if (!player.IsOnline)
            return;
        if (!wasAlreadyOnline)
            _ = Data.DatabaseManager.UpdateUsernames(player.Name);
        task = PlayerInit(player, wasAlreadyOnline);
        if (!task.IsCompleted)
            await task.ConfigureAwait(false);
        for (int i = 0; i < _singletons.Count; ++i)
        {
            IUncreatedSingleton singleton = _singletons[i];
            if (singleton is IPlayerPostInitListener l1)
                l1.OnPostPlayerInit(player);
            if (singleton is IPlayerPostInitListenerAsync l2)
            {
                task = l2.OnPostPlayerInit(player);
                if (!task.IsCompleted)
                {
                    await task.ConfigureAwait(false);
                    await UCWarfare.ToUpdate();
                    if (!player.IsOnline)
                        return;
                }
            }
        }
    }
    private void InternalPreDispose()
    {
        if (_stagingPhaseTimer is not null)
            StopCoroutine(_stagingPhaseTimer);
        if (_state == EState.STAGING)
        {
            _stagingSeconds = 0;
            EndStagingPhase();
            _stagingPhaseTimer = null;
        }
    }
    private void InternalOnReady()
    {
        ThreadUtil.assertIsGameThread();
        if (this is IFOBs)
            RepairManager.LoadRepairStations();

        if (this is ISquads)
            RallyManager.WipeAllRallies();

        ReplaceBarricadesAndStructures();
        _hasTimeSynced = false;
        if (useEventLoop)
        {
            EventLoopCoroutine = StartCoroutine(EventLoop());
        }
    }
    private Task PostOnReady() => StartNextGame(true);
    private void InternalPostDispose()
    {
        if (this is IGameStats stats && stats is Component beh)
        {
            Destroy(beh);
            if (beh is BaseStatTracker<BasePlayerStats> tracker)
                tracker.ClearAllStats();
        }
        CTFUI.StagingUI.ClearFromAllPlayers();
    }
    private void InternalPostInit()
    {
        _ticks = 0;
        if (!UCWarfare.Config.DisableDailyQuests)
        {
            QuestManager.Init();
            DailyQuests.Load();
        }
    }
    public async Task OnLevelReady()
    {
        if (!wasLevelLoadedOnStart)
        {
            await UCWarfare.ToUpdate();
            Task task;
            for (int i = 0; i < _singletons.Count; ++i)
            {
                IUncreatedSingleton singleton = _singletons[i];
                if (singleton is ILevelStartListener l1)
                    l1.OnLevelReady();
                if (singleton is ILevelStartListenerAsync l2)
                {
                    task = l2.OnLevelReady();
                    if (!task.IsCompleted)
                    {
                        await task.ConfigureAwait(false);
                        await UCWarfare.ToUpdate();
                    }
                }
            }
            ThreadUtil.assertIsGameThread();
            InternalOnReady();
            task = OnReady();
            if (!task.IsCompleted)
            {
                await task.ConfigureAwait(false);
                await UCWarfare.ToUpdate();
                ThreadUtil.assertIsGameThread();
            }

            await PostOnReady().ConfigureAwait(false);
            _hasOnReadyRan = true;
        }
        _hasTimeSynced = false;
    }
    internal void OnCraftRequestedIntl(CraftRequested e)
    {
        OnCraftRequested(e);
        if (!e.CanContinue)
            return;

        for (int i = 0; i < _singletons.Count; i++)
        {
            if (_singletons[i] is ICraftingSettingsOverride craftingOverride)
            {
                craftingOverride.OnCraftRequested(e);
                if (!e.CanContinue)
                    return;
            }
        }
    }
    private void InternalSubscribe()
    {
        EventDispatcher.GroupChanged += OnGroupChangedIntl;
        EventDispatcher.PlayerDied += OnPlayerDeath;
    }
    private void InternalUnsubscribe()
    {
        EventDispatcher.PlayerDied -= OnPlayerDeath;
        EventDispatcher.GroupChanged -= OnGroupChangedIntl;
    }
    /// <summary>Adds a singleton to be loaded at the end of PreInit</summary>
    /// <typeparam name="T">Type of singleton to be loaded.</typeparam>
    /// <exception cref="InvalidOperationException">Thrown if this function isn't called from pre-init.</exception>
    /// <exception cref="NotSupportedException">Thrown if this function isn't called from the game thread.</exception>
    /// <exception cref="SingletonLoadException">Thrown if the singleton fails to load.</exception>
    protected void AddSingletonRequirement<T>(ref T field) where T : class, IUncreatedSingleton
    {
        if (!_isPreLoading) throw new InvalidOperationException("You can only add Singleton requirements from PreInit()");
        Data.Singletons.PopulateSingleton(ref field, true);
        _singletons.Add(field);
    }
    public void OnStagingComplete()
    {
        for (int i = 0; i < _singletons.Count; ++i)
            if (_singletons[i] is IStagingPhaseOverListener staging)
                staging.OnStagingPhaseOver();
        if (StagingPhaseOver != null)
        {
            try
            {
                StagingPhaseOver.Invoke();
            }
            catch (Exception ex)
            {
                L.LogError("Error invoking void StagingPhaseOver():");
                L.LogError(ex);
            }
        }
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
    }
    protected abstract void EventLoopAction();
    private IEnumerator<WaitForSecondsRealtime> EventLoop()
    {
        while (!isPendingCancel)
        {
#if DEBUG
            IDisposable profiler = ProfilingUtils.StartTracking(Name + " Gamemode Event Loop");
#endif
            for (int i = PlayerManager.OnlinePlayers.Count - 1; i >= 0; i--)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                if (pl.IsOnline)
                {
                    try
                    {
                        _ = pl.Player.transform;
                        continue;
                    }
                    catch (NullReferenceException) { }
                }
                L.Log($"Kicking {pl.Name.PlayerName} ({pl.Steam64}) for null transform.", ConsoleColor.Cyan);
                Provider.kick(pl.CSteamID, Localization.Translate(T.NullTransformKickMessage, pl, UCWarfare.Config.DiscordInviteCode));
            }
            try
            {
                EventLoopAction();
                OnGameTick?.Invoke();
                for (int i = 0; i < _singletons.Count; ++i)
                    if (_singletons[i] is IGameTickListener ticker)
                        ticker.Tick();
            }
            catch (Exception ex)
            {
                L.LogError("Error in " + Name + " gamemode in the event loop:");
                L.LogError(ex);
            }

            if (!_hasTimeSynced)
            {
                TimeSync();
                _hasTimeSynced = true;
            }
            
            QuestManager.OnGameTick();
#if DEBUG
            profiler.Dispose();
            if (EveryXSeconds(150))
            {
                F.SaveProfilingData();
            }
#endif
            _ticks++;
            yield return new WaitForSecondsRealtime(_eventLoopSpeed);
        }
    }

    /// <summary>
    /// Used to line up all 'animated' sections of the plugin.<br/>
    /// Seconds tick on vehicle signs at the same time as they do on the staging phase UI, for example.
    /// </summary>
    protected virtual void TimeSync()
    {
        float time = Time.realtimeSinceStartup;
        for (int i = 0; i < _singletons.Count; ++i)
        {
            if (_singletons[i] is ITimeSyncListener ts)
                ts.TimeSync(time);
        }
        TraitSigns.TimeSync();
        VehicleSigns.TimeSync();
    }

    string ITranslationArgument.Translate(string language, string? format, UCPlayer? target, ref TranslationFlags flags) => DisplayName;
    public void ShutdownAfterGame(string reason, ulong player)
    {
        shutdownAfterGame = true;
        shutdownMessage = reason;
        shutdownPlayer = player;
    }
    public void CancelShutdownAfterGame()
    {
        shutdownAfterGame = false;
        shutdownMessage = string.Empty;
        shutdownPlayer = 0;
    }

    public virtual async Task DeclareWin(ulong winner)
    {
        try
        {
            ThreadUtil.assertIsGameThread();
            this._state = EState.FINISHED;
            L.Log(TeamManager.TranslateName(winner, 0) + " just won the game!", ConsoleColor.Cyan);
            for (int i = 0; i < _singletons.Count; ++i)
            {
                IUncreatedSingleton singleton = _singletons[i];
                if (singleton is IDeclareWinListener l1)
                    l1.OnWinnerDeclared(winner);
                if (singleton is IDeclareWinListenerAsync l2)
                {
                    Task task = l2.OnWinnerDeclared(winner);
                    if (!task.IsCompleted)
                    {
                        await task.ConfigureAwait(false);
                        await UCWarfare.ToUpdate();
                    }
                }
            }
            ThreadUtil.assertIsGameThread();

            QuestManager.OnGameOver(winner);

            ActionLogger.Add(EActionLogType.TEAM_WON, TeamManager.TranslateName(winner, 0));

            Chat.Broadcast(T.TeamWin, TeamManager.GetFaction(winner));

            for (int i = 0; i < Provider.clients.Count; i++)
                Provider.clients[i].player.movement.forceRemoveFromVehicle();

            if (this is IGameStats { GameStats: BaseStatTracker<BasePlayerStats> tps })
            {
                foreach (IStats played in tps.stats.OfType<IStats>())
                {
                    switch (played)
                    {
                        // Any player who was online for 65% of the match will be awarded a win or punished with a loss
                        case ITeamPresenceStats ps when tps.GetPresence(ps, 1) >= MATCH_PRESENT_THRESHOLD:
                        {
                            if (winner == 1)
                                StatsManager.ModifyStats(played.Steam64, s => s.Wins++, false);
                            else
                                StatsManager.ModifyStats(played.Steam64, s => s.Losses++, false);
                            break;
                        }
                        case ITeamPresenceStats ps:
                        {
                            if (tps.GetPresence(ps, 2) >= MATCH_PRESENT_THRESHOLD)
                            {
                                if (winner == 2)
                                    StatsManager.ModifyStats(played.Steam64, s => s.Wins++, false);
                                else
                                    StatsManager.ModifyStats(played.Steam64, s => s.Losses++, false);
                            }

                            break;
                        }
                        case IPresenceStats ps2:
                        {
                            if (tps.GetPresence(ps2) >= MATCH_PRESENT_THRESHOLD)
                            {
                                if (IsWinner(played.Player))
                                    StatsManager.ModifyStats(played.Steam64, s => s.Wins++, false);
                                else
                                    StatsManager.ModifyStats(played.Steam64, s => s.Losses++, false);
                            }

                            break;
                        }
                    }
                }
            }

            StatsManager.ModifyTeam(winner, t => t.Wins++, false);
            StatsManager.ModifyTeam(TeamManager.Other(winner), t => t.Losses++, false);
        }
        catch (Exception ex)
        {
            L.LogError("Error declaring winner as " + winner + " for gamemode " + DisplayName + ".");
            L.LogError(ex);
        }
    }

    internal virtual bool IsWinner(UCPlayer player) =>
        throw new NotImplementedException("IsWinner is not overridden by a non-team gamemode.");
    public static async Task<bool> TryLoadGamemode(Type type)
    {
        if (type is not null && typeof(Gamemode).IsAssignableFrom(type))
        {
            if (Data.Gamemode is not null)
            {
                Data.Gamemode._state = EState.DISCARD;
                await Data.Singletons.UnloadSingletonAsync(Data.Gamemode).ConfigureAwait(false);
                Data.Gamemode = null!;
                await UCWarfare.ToUpdate();
            }
            SingletonLoadException? ex = null;
            try
            {
                IUncreatedSingleton sgl = null!;
                Data.Singletons.PopulateSingleton(ref sgl, type, true);
                Data.Gamemode = (sgl as Gamemode)!;
                if (Data.Gamemode is null)
                    goto error;
                L.Log("Chosen new gamemode " + Data.Gamemode.DisplayName, ConsoleColor.DarkCyan);
                await Data.Singletons.LoadSingletonAsync(Data.Gamemode).ConfigureAwait(false);
                ActionLogger.Add(EActionLogType.GAMEMODE_CHANGED_AUTO, Data.Gamemode.DisplayName);
                return true;
            }
            catch (SingletonLoadException ex2)
            {
                ex = ex2;
            }
            error:
            await FailToLoadGame(ex).ConfigureAwait(false);
            return false;
        }
        await FailToLoadGame(new Exception("Invalid type: " + (type?.Name ?? "<null>"))).ConfigureAwait(false);
        return false;
    }

    internal static async Task FailToLoadGame(Exception? ex)
    {
        L.LogError("Failed to load gamemode , shutting down in 10 seconds.");
        if (ex is not null)
        {
            ShutdownCommand.ShutdownIn(10,
                "There was a fatal error in the server: " +
                (ex is SingletonLoadException ? ex.InnerException?.GetType().Name ?? nameof(SingletonLoadException) : ex.GetType().Name) +
                ". It will restart in 10 seconds.");
            L.NetCalls.SendFatalException.NetInvoke((ex.InnerException ?? ex).ToString());
        }
        else
        {
            ShutdownCommand.ShutdownIn(10, "There was a fatal error in the server. It will restart in 10 seconds.");
        }

        EffectManager.askEffectClearAll();
        await Data.Singletons.UnloadAllAsync().ConfigureAwait(false);
        Data.Gamemode = null!;
        UCWarfare.ForceUnload();
    }

    protected virtual async Task EndGame()
    {
        try
        {
            await CommandHandler.LetCommandsFinish().ConfigureAwait(false);
            Type? nextMode = GetNextGamemode();
            if (GetType() != nextMode)
            {
                await TryLoadGamemode(nextMode!).ConfigureAwait(false);
                return;
            }
            await Data.Singletons.ReloadSingletonAsync(ReloadKey).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            L.LogError("Error ending game: " + DisplayName + ".");
            L.LogError(ex);
            await FailToLoadGame(ex).ConfigureAwait(false);
        }
    }
    public async Task StartNextGame(bool onLoad = false)
    {
        ThreadUtil.assertIsGameThread();
        Task task = PreGameStarting(onLoad);
        if (!task.IsCompleted)
        {
            await task.ConfigureAwait(false);
            await UCWarfare.ToUpdate();
            ThreadUtil.assertIsGameThread();
        }
        if (!onLoad)
        {
            foreach (LanguageSet set in LanguageSet.All())
            {
                string val = T.LoadingGamemode.Translate(set.Language, this);
                while (set.MoveNext())
                {
                    UCPlayer pl = set.Next;
                    pl.Player.enablePluginWidgetFlag(EPluginWidgetFlags.Modal);
                    UCPlayer.LoadingUI.SendToPlayer(pl.Connection, val);
                }
            }
        }
        for (int i = 0; i < _singletons.Count; ++i)
        {
            IUncreatedSingleton singleton = _singletons[i];
            if (singleton is IGameStartListener l1)
                l1.OnGameStarting(onLoad);
            if (singleton is IGameStartListenerAsync l2)
            {
                task = l2.OnGameStarting(onLoad);
                if (!task.IsCompleted)
                {
                    await task.ConfigureAwait(false);
                    await UCWarfare.ToUpdate();
                }
            }
        }

        ThreadUtil.assertIsGameThread();
        CooldownManager.OnGameStarting();
        L.Log($"Loading new {DisplayName} game.", ConsoleColor.Cyan);
        _state = EState.ACTIVE;
        _gameID = DateTime.UtcNow.Ticks;
        _startTime = Time.realtimeSinceStartup;
        for (int i = 0; i < Provider.clients.Count; i++)
            if (PlayerManager.HasSave(Provider.clients[i].playerID.steamID.m_SteamID, out PlayerSave save)) save.LastGame = _gameID;
        PlayerManager.ApplyToOnline();
        if (!onLoad)
        {
            for (int i = 0; i < _singletons.Count; ++i)
            {
                IUncreatedSingleton singleton = _singletons[i];
                if (singleton is ILevelStartListener l1)
                    l1.OnLevelReady();
                if (singleton is ILevelStartListenerAsync l2)
                {
                    task = l2.OnLevelReady();
                    if (!task.IsCompleted)
                    {
                        await task.ConfigureAwait(false);
                        await UCWarfare.ToUpdate();
                    }
                }
            }
            ThreadUtil.assertIsGameThread();
        }
        task = PostGameStarting(onLoad);
        if (!task.IsCompleted)
        {
            await task;
            await UCWarfare.ToUpdate();
        }
        ThreadUtil.assertIsGameThread();
        await Points.UpdateAllPointsAsync().ConfigureAwait(false);
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
        {
            UCPlayer pl = PlayerManager.OnlinePlayers[i];
            await pl.PurchaseSync.WaitAsync().ConfigureAwait(false);
            try
            {
                await UCWarfare.ToUpdate();
                await Data.Gamemode.InternalPlayerInit(pl, pl.HasInitedOnce).ConfigureAwait(false);
            }
            finally
            {
                pl.PurchaseSync.Release();
            }
        }

        await UCWarfare.ToUpdate();
        ThreadUtil.assertIsGameThread();
        if (!onLoad)
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                PlayerManager.OnlinePlayers[i].Player.disablePluginWidgetFlag(EPluginWidgetFlags.Modal);
            UCPlayer.LoadingUI.ClearFromAllPlayers();
        }
        await PostPlayerInit(onLoad).ConfigureAwait(false);
    }
    public void AnnounceMode()
    {
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            ToastMessage.QueueMessage(PlayerManager.OnlinePlayers[i], new ToastMessage(string.Empty, DisplayName, EToastMessageSeverity.BIG));
    }
    internal async Task OnPlayerJoined(UCPlayer player)
    {
        ThreadUtil.assertIsGameThread();
        for (int i = 0; i < _singletons.Count; ++i)
        {
            IUncreatedSingleton singleton = _singletons[i];
            if (singleton is IPlayerConnectListener l1)
                l1.OnPlayerConnecting(player);
            if (singleton is IPlayerConnectListenerAsync l2)
            {
                Task task = l2.OnPlayerConnecting(player);
                if (!task.IsCompleted)
                {
                    await task.ConfigureAwait(false);
                    await UCWarfare.ToUpdate();
                    if (!player.IsOnline)
                        return;
                }
            }
        }
        await InternalPlayerInit(player, false).ConfigureAwait(false);
    }
    public virtual void OnGroupChanged(GroupChanged e) { }
    private void OnGroupChangedIntl(GroupChanged e)
    {
        if (State == EState.STAGING)
        {
            if (e.NewTeam is < 1 or > 2)
                ClearStagingUI(e.Player);
            else
                ShowStagingUI(e.Player);
        }
        OnGroupChanged(e);
    }
    public virtual void PlayerLeave(UCPlayer player)
    {
        if (State is not EState.ACTIVE or EState.STAGING && PlayerSave.TryReadSaveFile(player.Steam64, out PlayerSave save))
        {
            save.ShouldRespawnOnJoin = true;
            PlayerSave.WriteToSaveFile(save);
        }
        foreach (IPlayerDisconnectListener listener in _singletons.OfType<IPlayerDisconnectListener>())
            listener.OnPlayerDisconnecting(player);
    }

    public virtual void OnPlayerDeath(PlayerDied e)
    {
        Points.OnPlayerDeath(e);
        if (e.Player.Player.TryGetPlayerData(out UCPlayerData c))
            c.LastGunShot = default;
    }
    public static Type? FindGamemode(string name)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int i = 0; i < Gamemodes.Count; ++i)
        {
            if (Gamemodes[i].Key.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                Type type = Gamemodes[i].Value;
                if (type is null || !type.IsSubclassOf(typeof(Gamemode))) return null;
                return type;
            }
        }
        if (name.Length > 2)
        {
            for (int i = 0; i < Gamemodes.Count; ++i)
            {
                if (Gamemodes[i].Key.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1)
                {
                    Type type = Gamemodes[i].Value;
                    if (type is null || !type.IsSubclassOf(typeof(Gamemode))) return null;
                    return type;
                }
            }
        }
        return null;
    }
    public virtual void Subscribe() { }
    public virtual void Unsubscribe() { }
    protected Coroutine? _stagingPhaseTimer;
    public virtual void StartStagingPhase(float seconds)
    {
        _stagingSeconds = seconds;
        _state = EState.STAGING;

        _stagingPhaseTimer = StartCoroutine(StagingPhaseLoop());
    }
    public void SkipStagingPhase()
    {
        _stagingSeconds = 0;
    }
    public IEnumerator<WaitForSeconds> StagingPhaseLoop()
    {
        ShowStagingUIForAll();

        while (StagingSeconds > 0)
        {
            if (State != EState.STAGING)
            {
                EndStagingPhase();
                _stagingPhaseTimer = null;
                yield break;
            }

            UpdateStagingUIForAll();

            yield return new WaitForSeconds(1f);
            _stagingSeconds--;
        }
        EndStagingPhase();
        _stagingPhaseTimer = null;
    }
    public virtual void ShowStagingUI(UCPlayer player)
    {
        CTFUI.StagingUI.SendToPlayer(player.Connection);
        CTFUI.StagingUI.Top.SetText(player.Connection, Localization.Translate(T.PhaseBriefing, player));
    }
    public void ClearStagingUI(UCPlayer player)
    {
        CTFUI.StagingUI.ClearFromPlayer(player.Connection);
    }
    public void ShowStagingUIForAll()
    {
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
        {
            UCPlayer player = PlayerManager.OnlinePlayers[i];
            if (!player.HasUIHidden)
                ShowStagingUI(player);
        }
    }
    public void UpdateStagingUI(UCPlayer player, TimeSpan timeleft)
    {
        CTFUI.StagingUI.Bottom.SetText(player.Connection, $"{timeleft.Minutes}:{timeleft.Seconds:D2}");
    }
    public void UpdateStagingUIForAll()
    {
        TimeSpan timeLeft = TimeSpan.FromSeconds(StagingSeconds);
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
        {
            UCPlayer player = PlayerManager.OnlinePlayers[i];
            ulong team = player.GetTeam();
            if (team is 1 or 2)
                UpdateStagingUI(player, timeLeft);
        }
    }
    protected virtual void EndStagingPhase()
    {
        CTFUI.StagingUI.ClearFromAllPlayers();
        _state = EState.ACTIVE;
        OnStagingComplete();
    }
    public void ReplaceBarricadesAndStructures()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (StructureManager.regions is null)
            L.LogWarning("Structure regions have not been initialized.");
        if (BarricadeManager.regions is null)
            L.LogWarning("Barricade regions have not been initialized.");
        try
        {
            //bool isStruct = this is IStructureSaving;
            int fails = 0;
            StructureSaver? saver = Data.Singletons.GetSingleton<StructureSaver>();
            for (byte x = 0; x < Regions.WORLD_SIZE; x++)
            {
                for (byte y = 0; y < Regions.WORLD_SIZE; y++)
                {
                    try
                    {
                        if (BarricadeManager.regions is not null)
                        {
                            BarricadeRegion barricadeRegion = BarricadeManager.regions[x, y];
                            for (int i = barricadeRegion.drops.Count - 1; i >= 0; i--)
                            {
                                BarricadeDrop drop = barricadeRegion.drops[i];
                                if (!((saver != null && saver.IsLoaded && saver.TryGetSave(drop, out SavedStructure _)) || (RequestSigns.Loaded && RequestSigns.SignExists(drop.instanceID, out _))))
                                {
                                    if (drop.model.TryGetComponent(out FOBComponent fob))
                                    {
                                        fob.Parent.IsWipedByAuthority = true;
                                    }
                                    if (drop.model.transform.TryGetComponent(out InteractableStorage storage))
                                        storage.despawnWhenDestroyed = true;
                                    BarricadeManager.destroyBarricade(drop, x, y, ushort.MaxValue);
                                }
                            }
                        }

                        if (StructureManager.regions is not null)
                        {
                            StructureRegion structureRegion = StructureManager.regions[x, y];
                            for (int i = structureRegion.drops.Count - 1; i >= 0; i--)
                            {
                                StructureDrop drop = structureRegion.drops[i];
                                if (!(saver != null && saver.IsLoaded && saver.TryGetSave(drop, out SavedStructure _)))
                                    StructureManager.destroyStructure(drop, x, y, Vector3.zero);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        L.LogError($"Failed to clear barricades/structures of region ({x}, {y}):");
                        L.LogError(ex);
                        ++fails;
                        if (fails > 5)
                            throw new SingletonLoadException(ESingletonLoadType.LOAD, this, ex);
                    }
                }
            }
            if (RequestSigns.Loaded)
                RequestSigns.DropAllSigns();
            IconManager.OnLevelLoaded();
        }
        catch (Exception ex)
        {
            L.LogError("Failed to clear barricades/structures:");
            L.LogError(ex);
        }
    }

    public static void ReadGamemodes()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (GamemodeRotation.Count > 0) GamemodeRotation.Clear();
        if (UCWarfare.Config.GamemodeRotation == null)
        {
            GamemodeRotation.Add(new KeyValuePair<Type, float>(typeof(TeamCTF), 1.0f));
            return;
        }

        List<KeyValuePair<string?, float>> gms = new List<KeyValuePair<string?, float>>();
        using (IEnumerator<char> iter = UCWarfare.Config.GamemodeRotation.GetEnumerator())
        {
            StringBuilder current = new StringBuilder(32);
            string? name = null;
            bool inName = true;
            float weight;
            while (iter.MoveNext())
            {
                char c = iter.Current;
                if (c == ' ') continue;
                if (inName)
                {
                    if (c == ':')
                    {
                        name = current.ToString();
                        current.Clear();
                        inName = false;
                    }
                    else if (c == ',')
                    {
                        gms.Add(new KeyValuePair<string?, float>(current.ToString(), 1f));
                    }
                    else if (current.Length < 32)
                    {
                        current.Append(c);
                    }
                }
                else
                {
                    if (c == ',')
                    {
                        if (float.TryParse(current.ToString(), System.Globalization.NumberStyles.Any, Data.Locale,
                                out weight))
                            gms.Add(new KeyValuePair<string?, float>(name, weight));
                        else
                            gms.Add(new KeyValuePair<string?, float>(name, 1f));
                        name = null;
                        current.Clear();
                        inName = true;
                    }
                    else if (current.Length < 32)
                    {
                        current.Append(c);
                    }
                }
            }

            if (name != null && float.TryParse(current.ToString(), System.Globalization.NumberStyles.Any, Data.Locale,
                    out weight))
                gms.Add(new KeyValuePair<string?, float>(name, weight));
        }

        for (int j = 0; j < gms.Count; ++j)
        {
            for (int i = 0; i < Gamemodes.Count; ++i)
            {
                if (Gamemodes[i].Key.Equals(gms[j].Key, StringComparison.OrdinalIgnoreCase))
                {
                    GamemodeRotation.Add(new KeyValuePair<Type, float>(Gamemodes[i].Value, gms[j].Value));
                    break;
                }
            }
        }
    }

    public static Type? GetNextGamemode()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        float total = 0f;
        for (int i = 0; i < GamemodeRotation.Count; i++)
            total += GamemodeRotation[i].Value;

        float sel = UnityEngine.Random.Range(0f, total);
        total = 0f;
        for (int i = 0; i < GamemodeRotation.Count; i++)
        {
            total += GamemodeRotation[i].Value;
            if (sel < total)
                return GamemodeRotation[i].Key;
        }
        return null;
    }

    internal virtual string DumpState()
    {
        return "Mode: " + DisplayName;
    }

    internal async Task OnQuestCompleted(QuestCompleted e)
    {
        for (int i = 0; i < _singletons.Count; ++i)
        {
            IUncreatedSingleton singleton = _singletons[i];
            if (singleton is IQuestCompletedListener l1)
                l1.OnQuestCompleted(e);
            if (singleton is IQuestCompletedListenerAsync l2)
            {
                Task task = l2.OnQuestCompleted(e);
                if (!task.IsCompleted)
                {
                    await task.ConfigureAwait(false);
                    await UCWarfare.ToUpdate();
                    if (!e.Player.IsOnline)
                        return;
                }
            }
        }
    }
    internal async Task HandleQuestCompleted(QuestCompleted e)
    {
        if (!RankManager.OnQuestCompleted(e) && !KitManager.OnQuestCompleted(e))
        {
            for (int i = 0; i < _singletons.Count; ++i)
            {
                IUncreatedSingleton singleton = _singletons[i];
                if (singleton is IQuestCompletedHandler l1)
                {
                    l1.OnQuestCompleted(e);
                    if (!e.CanContinue)
                        return;
                }
                if (singleton is IQuestCompletedHandlerAsync l2)
                {
                    Task task = l2.OnQuestCompleted(e);
                    if (!task.IsCompleted)
                    {
                        await task.ConfigureAwait(false);
                        await UCWarfare.ToUpdate();
                        if (!e.Player.IsOnline)
                            return;
                    }
                    if (!e.CanContinue)
                        return;
                }
            }
        }
        else e.Break();
    }
}
public enum EState : byte
{
    ACTIVE,
    PAUSED,
    FINISHED,
    LOADING,
    STAGING,
    DISCARD
}

[Translatable("Gamemode Type")]
public enum EGamemode : byte
{
    [Translatable("Vanilla")]
    UNDEFINED,
    [Translatable("Advance and Secure")]
    TEAM_CTF,
    INVASION,
    INSURGENCY,
    CONQUEST,
    HARDPOINT
}

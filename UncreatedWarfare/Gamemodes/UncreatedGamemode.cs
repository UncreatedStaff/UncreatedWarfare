using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
using Uncreated.Warfare.Gamemodes.Insurgency;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Gamemodes.UI;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Ranks;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Tickets;
using Uncreated.Warfare.Traits;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using Action = System.Action;

namespace Uncreated.Warfare.Gamemodes;

[SingletonDependency(typeof(VehicleBay))]
[SingletonDependency(typeof(VehicleSpawner))]
[SingletonDependency(typeof(TraitManager))]
[SingletonDependency(typeof(KitManager))]
public abstract class Gamemode : BaseAsyncSingletonComponent, IGamemode, ILevelStartListenerAsync, IReloadableSingleton, ITranslationArgument
{
    public const float MatchPresentThreshold = 0.65f;
    public const string GamemodeReloadKey = "gamemode";
    protected static readonly Vector3 BlockerSpawnRotation = new Vector3(270f, 0f, 180f);
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
    public static Action? OnStateUpdated;
    public static WinToastUI WinToastUI;
    public Whitelister Whitelister;
    public CooldownManager Cooldowns;
    public Tips Tips;
    public Signs Signs;
    public Coroutine EventLoopCoroutine;
    public bool IsPendingCancel;
    public event Action? StagingPhaseOver;
    internal string ShutdownMessage = string.Empty;
    internal bool ShouldShutdownAfterGame;
    internal ulong ShutdownPlayer;
    protected Coroutine? StagingPhaseTimer;
    protected int Ticks;
    private float _eventLoopSpeed;
    private bool _useEventLoop;
    private bool _isPreLoading;
    private List<IUncreatedSingleton> _singletons;
    private IReadOnlyList<IUncreatedSingleton> _singletonsRl;
    private bool _wasLevelLoadedOnStart;
    private volatile bool _hasOnReadyRan;
    private bool _hasTimeSynced;
    public event Action? OnGameTick;
    private CancellationTokenSource _tokenSrc;
    protected IReadOnlyList<IUncreatedSingleton> Singletons => _singletonsRl;
    public bool LoadAsynchronous => true;
    public override bool AwaitLoad => true;
    public State State { get; private set; }
    public float StartTime { get; private set; }
    public float StagingSeconds { get; private set; }
    public float SecondsSinceStart => Time.realtimeSinceStartup - StartTime;
    public long GameID { get; private set; }
    public static GamemodeConfigData Config => ConfigObj.Data;
    public string Name { get; }
    public float EventLoopSpeed => _eventLoopSpeed;
    public bool EveryMinute => Ticks % Mathf.RoundToInt(60f / _eventLoopSpeed) == 0;
    public bool Every30Seconds => Ticks % Mathf.RoundToInt(30f / _eventLoopSpeed) == 0;
    public bool Every15Seconds => Ticks % Mathf.RoundToInt(15f / _eventLoopSpeed) == 0;
    public bool Every10Seconds => Ticks % Mathf.RoundToInt(10f / _eventLoopSpeed) == 0;
    public bool EverySecond => Ticks % Mathf.RoundToInt(1f / _eventLoopSpeed) == 0;
    public string ReloadKey => GamemodeReloadKey;
    public virtual bool UseWhitelist => true;
    public abstract string DisplayName { get; }
    public virtual bool TransmitMicWhileNotActive => true;
    public virtual bool ShowXPUI => true;
    public virtual bool ShowOFPUI => true;
    public virtual bool UseTips => true;
    public virtual bool AllowCosmetics => false;
    public virtual bool CustomSigns => true;
    public virtual GamemodeType GamemodeType => GamemodeType.Undefined;
    protected bool HasOnReadyRan => _hasOnReadyRan;
    public CancellationToken UnloadToken => _tokenSrc == null ? CancellationToken.None : _tokenSrc.Token;
    protected Gamemode(string name, float eventLoopSpeed)
    {
        Name = name;
        _eventLoopSpeed = eventLoopSpeed;
        _useEventLoop = eventLoopSpeed > 0;
        State = State.Loading;
        OnStateUpdated?.Invoke();
    }
    public void SetTiming(float newSpeed)
    {
        _eventLoopSpeed = newSpeed;
        _useEventLoop = newSpeed > 0;
    }
    public void AdvanceDelays(float seconds)
    {
        StartTime -= seconds;
        Signs.UpdateAllSigns();
        TimeSync();
    }
    protected virtual void OnAdvanceDelays(float seconds) { }
    public override async Task LoadAsync(CancellationToken token)
    {
        _tokenSrc = new CancellationTokenSource();
        token.CombineIfNeeded(UnloadToken);
        await UCWarfare.ToUpdate(token);
        if (!isActiveAndEnabled)
            throw new Exception("Gamemode object has been destroyed!");
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking(Name + " Load Sequence");
#endif
        if (_singletons is null)
        {
            _singletons = new List<IUncreatedSingleton>(16);
            _singletonsRl = _singletons.AsReadOnly();
        }
        else
            _singletons.Clear();
        _hasOnReadyRan = false;
        _wasLevelLoadedOnStart = Level.isLoaded;
        _isPreLoading = true;
        InternalPreInit();
        await PreInit(token).ConfigureAwait(false);

        _isPreLoading = false;
        await Data.Singletons.LoadSingletonsInOrderAsync(_singletons, token).ConfigureAwait(false);
        await UCWarfare.ToUpdate(token);
        ThreadUtil.assertIsGameThread();

        InternalSubscribe();
        Subscribe();
        InternalPostInit();
        Task task = PostInit(token);
        if (!task.IsCompleted)
        {
            await task.ConfigureAwait(false);
            await UCWarfare.ToUpdate(token);
            ThreadUtil.assertIsGameThread();
        }
        Type[] interfaces = GetType().GetInterfaces();
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
        if (_wasLevelLoadedOnStart)
        {
            await InvokeSingletonEvent<ILevelStartListener, ILevelStartListenerAsync>
                (x => x.OnLevelReady(), x => x.OnLevelReady(token), token).ConfigureAwait(false);
            await UCWarfare.ToUpdate(token);
            ThreadUtil.assertIsGameThread();
            await InternalOnReady(token).ConfigureAwait(false);
            await UCWarfare.ToUpdate(token);
            task = OnReady(token);
            if (!task.IsCompleted)
            {
                await task.ConfigureAwait(false);
                await UCWarfare.ToUpdate(token);
                ThreadUtil.assertIsGameThread();
            }
            await PostOnReady(token).ConfigureAwait(false);
            await UCWarfare.ToUpdate(token);
            _hasOnReadyRan = true;
        }
    }
    public async Task ReloadAsync(CancellationToken token)
    {
        await UnloadAsync(token).ConfigureAwait(false);
        await UCWarfare.ToUpdate(token);
        ConfigObj.Reload();
        await LoadAsync(token).ConfigureAwait(false);
    }
    public void Reload() => throw new NotImplementedException();
    public override async Task UnloadAsync(CancellationToken token)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking(Name + " Unload Sequence");
#endif
        await UCWarfare.ToUpdate(token);
#if DEBUG
        IDisposable profiler1 = ProfilingUtils.StartTracking(Name + " Unsubscribe");
#endif
        L.LogDebug("Unsub:");
        Unsubscribe();
        InternalUnsubscribe();
#if DEBUG
        profiler1.Dispose();
#endif
#if DEBUG
        IDisposable profiler2 = ProfilingUtils.StartTracking(Name + " Pre Dispose");
#endif
        L.LogDebug("PreDispose:");
        Task task = PreDispose(token);
        if (!task.IsCompleted)
        {
            await task.ConfigureAwait(false);
            await UCWarfare.ToUpdate(token);
            ThreadUtil.assertIsGameThread();
        }
        InternalPreDispose();
#if DEBUG
        profiler2.Dispose();
#endif
#if DEBUG
        IDisposable profiler3 = ProfilingUtils.StartTracking(Name + " Dispose");
#endif
        L.LogDebug("Unload singletons:");
        await Data.Singletons.UnloadSingletonsInOrderAsync(_singletons, token).ConfigureAwait(false);
        await UCWarfare.ToUpdate(token);
        ThreadUtil.assertIsGameThread();
#if DEBUG
        profiler3.Dispose();
#endif
#if DEBUG
        IDisposable profiler4 = ProfilingUtils.StartTracking(Name + " Post Dispose");
#endif
        L.LogDebug("PostDsposeIntl:");
        InternalPostDispose();
        L.LogDebug("PostDspose:");
        task = PostDispose(token);
        if (!task.IsCompleted)
        {
            await task.ConfigureAwait(false);
            await UCWarfare.ToUpdate(token);
            ThreadUtil.assertIsGameThread();
        }
#if DEBUG
        profiler4.Dispose();
#endif

        UCWarfare.I.ProcessTasks = false;
        try
        {
            L.LogDebug("Let Tasks Unload:");
            await UCWarfare.I.LetTasksUnload(token);
            _tokenSrc.Cancel();
            L.LogDebug("Done:");
        }
        finally
        {
            UCWarfare.I.ProcessTasks = true;
        }
    }

    /// <summary>Use to add <see cref="IUncreatedSingleton"/>s to be loaded.</summary>
    /// <remarks>Abstract</remarks>
    protected abstract Task PreInit(CancellationToken token);

    /// <summary>Called after all <see cref="IUncreatedSingleton"/>s have been loaded.</summary>
    /// <remarks>Abstract</remarks>
    protected virtual Task PostInit(CancellationToken token) => Task.CompletedTask;

    /// <summary>Called just before all <see cref="IUncreatedSingleton"/>s are unloaded.</summary>
    /// <remarks>Abstract</remarks>
    protected virtual Task PreDispose(CancellationToken token) => Task.CompletedTask;

    /// <summary>Called just after all <see cref="IUncreatedSingleton"/>s have been unloaded.</summary>
    /// <remarks>No base</remarks>
    protected virtual Task PostDispose(CancellationToken token) => Task.CompletedTask;

    /// <summary>If the level is already loaded, called after <see cref="PostInit"/>, otherwise called when the level is loaded.</summary>
    /// <remarks>No base, guaranteed to be called after all registered <see cref="ILevelStartListener.OnLevelReady"/>'s have been called.</remarks>
    protected virtual Task OnReady(CancellationToken token) => Task.CompletedTask;

    /// <summary>Called when a player tries to craft something.</summary>
    /// <remarks>No base, guaranteed to be called before all registered <see cref="ICraftingSettingsOverride.OnCraftRequested(CraftRequested)"/>'s have been called.</remarks>
    protected virtual void OnCraftRequested(CraftRequested e) { }

    /// <summary>Runs just before a game starts.</summary>
    /// <param name="isOnLoad">Whether this is the first game played on this singleton since running <see cref="LoadAsync"/>.</param>
    /// <remarks>Called from <see cref="StartNextGame(CancellationToken, bool)"/></remarks>
    protected virtual Task PreGameStarting(bool isOnLoad, CancellationToken token) => Task.CompletedTask;

    /// <summary>Runs just after a game starts.</summary>
    /// <param name="isOnLoad">Whether this is the first game played on this singleton since running <see cref="LoadAsync"/>.</param>
    /// <remarks>Called from <see cref="StartNextGame(CancellationToken, bool)"/></remarks>
    protected virtual Task PostGameStarting(bool isOnLoad, CancellationToken token) => Task.CompletedTask;

    /// <summary>Runs after all players have been initialized.</summary>
    /// <param name="isOnLoad">Whether this is the first game played on this singleton since running <see cref="LoadAsync"/>.</param>
    /// <remarks>Called from <see cref="StartNextGame(CancellationToken, bool)"/></remarks>
    protected virtual Task PostPlayerInit(bool isOnLoad, CancellationToken token) => Task.CompletedTask;

    /// <summary>Ran when a player joins or per online player after the game starts.</summary>
    /// <remarks>No base</remarks>
    protected virtual Task PlayerInit(UCPlayer player, bool wasAlreadyOnline, CancellationToken token) => Task.CompletedTask;

    /// <summary>Ran when a player's UI needs to regenerate (i.e. after closing a menu).</summary>
    /// <remarks>No base</remarks>
    protected virtual void ReloadUI(UCPlayer player) { }

    /// <summary>Ran when a player's language changes, <see cref="ReloadUI"/> is also called.</summary>
    /// <remarks>No base</remarks>
    protected virtual void OnLanguageChanged(UCPlayer player) { }

    /// <summary>Run in <see cref="EventLoopAction"/>, returns true if <param name="seconds"/> ago it would've also returned true. Based on tick speed and number of ticks.</summary>
    /// <remarks>Returns true if the second mark passed between the end of last tick and the start of this tick. Inlined when possible.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool EveryXSeconds(float seconds) => seconds <= 0f || Ticks * _eventLoopSpeed % seconds < _eventLoopSpeed;
    private void InternalPreInit()
    {
        AddSingletonRequirement(ref Cooldowns);
        if (UseWhitelist)
            AddSingletonRequirement(ref Whitelister);
        if (UseTips)
            AddSingletonRequirement(ref Tips);
        if (CustomSigns)
            AddSingletonRequirement(ref Signs);
    }
    private async Task InternalPlayerInit(UCPlayer player, bool wasAlreadyOnline, CancellationToken token)
    {
        ThreadUtil.assertIsGameThread();
        if (!player.IsOnline)
            return;
        if (player.Save.LastGame != GameID)
        {
            player.Save.LastGame = GameID;
            PlayerSave.WriteToSaveFile(player.Save);
        }
        player.HasInitedOnce = true;
        await InvokeSingletonEvent<IPlayerPreInitListener, IPlayerPreInitListenerAsync>
            (x => x.OnPrePlayerInit(player, wasAlreadyOnline),
                x => x.OnPrePlayerInit(player, wasAlreadyOnline, token), token, onlineCheck: player)
            .ConfigureAwait(false);

        await UCWarfare.ToUpdate(token);

        if (player.PendingVehicleSwapRequest.RespondToken is { IsCancellationRequested: false })
            player.PendingVehicleSwapRequest.RespondToken.Cancel();

        ThreadUtil.assertIsGameThread();
        if (!wasAlreadyOnline)
        {
            Task t2 = Points.UpdatePointsAsync(player, false, token);
            Task t3 = KitManager.DownloadPlayerKitData(player, false, token);
            Task t4 = OffenseManager.ApplyMuteSettings(player, token);
            await Data.DatabaseManager.RegisterLogin(player.Player, token).ConfigureAwait(false);
            await t2.ConfigureAwait(false);
            await t3.ConfigureAwait(false);
            await t4.ConfigureAwait(false);
        }
        await UCWarfare.ToUpdate(token);
        ThreadUtil.assertIsGameThread();
        if (!player.IsOnline)
            return;
        PlayerInitIntl(player, wasAlreadyOnline);
        Task task = PlayerInit(player, wasAlreadyOnline, token);
        if (!task.IsCompleted)
            await task.ConfigureAwait(false);
        await UCWarfare.ToUpdate(token);
        await InvokeSingletonEvent<IPlayerPostInitListener, IPlayerPostInitListenerAsync>
                (x => x.OnPostPlayerInit(player), x => x.OnPostPlayerInit(player, token), token, onlineCheck: player)
            .ConfigureAwait(false);
    }
    private void PlayerInitIntl(UCPlayer player, bool wasAlreadyOnline)
    {
        if (!AllowCosmetics)
            player.SetCosmeticStates(false);
        if (!wasAlreadyOnline)
        {
            StatsManager.RegisterPlayer(player.CSteamID.m_SteamID);
            UCWarfare.RunTask(Data.DatabaseManager.UpdateUsernames, player.Name, ctx: "Updaing usernames.", token: player.DisconnectToken);
        }
        StatsManager.ModifyStats(player.CSteamID.m_SteamID, s => s.LastOnline = DateTime.UtcNow.Ticks);
    }
    private void InternalPreDispose()
    {
        try
        {
            if (StagingPhaseTimer is not null)
                StopCoroutine(StagingPhaseTimer);
            if (State == State.Staging)
            {
                StagingSeconds = 0;
                EndStagingPhase();
                StagingPhaseTimer = null;
            }

            if (this is IImplementsLeaderboard { Leaderboard: MonoBehaviour { isActiveAndEnabled: true } b })
            {
                Destroy(b);
            }
        }
        catch (Exception ex)
        {
            L.LogError("Error in Insurgency Dispose.");
            L.LogError(ex);
        }
    }
    private async Task InternalOnReady(CancellationToken token = default)
    {
        ThreadUtil.assertIsGameThread();
        if (Data.Singletons.TryGetSingleton(out ZoneList list))
        {
            await list.DownloadAll(token).ConfigureAwait(false);
            await UCWarfare.ToUpdate(token);
        }

        if (this is ISquads)
            RallyManager.WipeAllRallies();

        ItemManager.askClearAllItems();
        EventFunctions.OnClearAllItems();
        ReplaceBarricadesAndStructures();
        Signs.CheckAllSigns();

        _hasTimeSynced = false;
        if (_useEventLoop)
        {
            if (EventLoopCoroutine != null)
            {
                L.LogWarning("An ADDITIONAL Gamemode event loop is about to be instantiated, stopping the old one...");

                StopCoroutine(EventLoopCoroutine);
            }

            EventLoopCoroutine = StartCoroutine(EventLoop());
        }
    }
    private Task PostOnReady(CancellationToken token) => StartNextGame(token, true);
    private void InternalPostDispose()
    {
        if (this is IGameStats stats)
        {
            stats.GameStats.ClearAllStats();
            if (stats.GameStats is Component beh)
                Destroy(beh);
        }
        CTFUI.StagingUI.ClearFromAllPlayers();
    }
    private void InternalPostInit()
    {
        Ticks = 0;
        if (!UCWarfare.Config.DisableDailyQuests)
        {
            QuestManager.Init();
            DailyQuests.Load();
        }
    }
    public async Task OnLevelReady(CancellationToken token)
    {
        token.CombineIfNeeded(UnloadToken);
        if (!_wasLevelLoadedOnStart)
        {
            await UCWarfare.ToUpdate(token);
            await InvokeSingletonEvent<ILevelStartListener, ILevelStartListenerAsync>
                (x => x.OnLevelReady(), x => x.OnLevelReady(token), token).ConfigureAwait(false);
            await UCWarfare.ToUpdate(token);
            ThreadUtil.assertIsGameThread();
            await InternalOnReady(token).ConfigureAwait(false);
            await UCWarfare.ToUpdate(token);
            Task task = OnReady(token);
            if (!task.IsCompleted)
            {
                await task.ConfigureAwait(false);
                await UCWarfare.ToUpdate(token);
                ThreadUtil.assertIsGameThread();
            }

            await PostOnReady(token).ConfigureAwait(false);
            _hasOnReadyRan = true;
        }
        _hasTimeSynced = false;
    }
    internal void OnCraftRequestedIntl(CraftRequested e)
    {
        OnCraftRequested(e);
        if (!e.CanContinue)
            return;

        InvokeSingletonEvent<ICraftingSettingsOverride>(x => x.OnCraftRequested(e), e);
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
        OnStateUpdated?.Invoke();
        InvokeSingletonEvent<IStagingPhaseOverListener>(x => x.OnStagingPhaseOver());
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
    internal void CancelAll()
    {
        IsPendingCancel = true;
        
    }
    protected abstract void EventLoopAction();
    private IEnumerator<WaitForSecondsRealtime> EventLoop()
    {
        while (!IsPendingCancel)
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
                if (State is State.Staging or State.Active)
                {
                    OnGameTick?.Invoke();
                    InvokeSingletonEvent<IGameTickListener>(x => x.Tick());
                }
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
            Ticks++;
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
        InvokeSingletonEvent<ITimeSyncListener>(x => x.TimeSync(time));
    }
    string ITranslationArgument.Translate(LanguageInfo language, string? format, UCPlayer? target, CultureInfo? culture,
        ref TranslationFlags flags) => DisplayName;
    public void ShutdownAfterGame(string reason, ulong player)
    {
        ShouldShutdownAfterGame = true;
        ShutdownMessage = reason;
        ShutdownPlayer = player;
    }
    public void CancelShutdownAfterGame()
    {
        ShouldShutdownAfterGame = false;
        ShutdownMessage = string.Empty;
        ShutdownPlayer = 0;
    }
    public virtual async Task DeclareWin(ulong winner, CancellationToken token)
    {
        try
        {
            token.CombineIfNeeded(UnloadToken);
            ThreadUtil.assertIsGameThread();
            State = State.Finished;
            OnStateUpdated?.Invoke();
            L.Log(TeamManager.TranslateName(winner) + " just won the game!", ConsoleColor.Cyan);
            await InvokeSingletonEvent<IDeclareWinListener, IDeclareWinListenerAsync>
                (x => x.OnWinnerDeclared(winner), x => x.OnWinnerDeclared(winner, token), token)
                .ConfigureAwait(false);
            await UCWarfare.ToUpdate(token);
            ThreadUtil.assertIsGameThread();

            QuestManager.OnGameOver(winner);

            ActionLog.Add(ActionLogType.TeamWon, TeamManager.TranslateName(winner));

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
                        case ITeamPresenceStats ps when tps.GetPresence(ps, 1) >= MatchPresentThreshold:
                        {
                            if (winner == 1)
                                StatsManager.ModifyStats(played.Steam64, s => s.Wins++, false);
                            else
                                StatsManager.ModifyStats(played.Steam64, s => s.Losses++, false);
                            break;
                        }
                        case ITeamPresenceStats ps:
                        {
                            if (tps.GetPresence(ps, 2) >= MatchPresentThreshold)
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
                            if (tps.GetPresence(ps2) >= MatchPresentThreshold)
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
    public static async Task<bool> TryLoadGamemode(Type type, CancellationToken token)
    {
        if (type is not null && typeof(Gamemode).IsAssignableFrom(type))
        {
            if (Data.Gamemode is not null)
            {
                Data.Gamemode.State = State.Discarded;
                await Data.Singletons.UnloadSingletonAsync(Data.Gamemode, token: token).ConfigureAwait(false);
                Data.Gamemode = null!;
                await UCWarfare.ToUpdate(token);
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
                await Data.Singletons.LoadSingletonAsync(Data.Gamemode, token: token).ConfigureAwait(false);
                ActionLog.Add(ActionLogType.GamemodeChangedAuto, Data.Gamemode.DisplayName);
                return true;
            }
            catch (SingletonLoadException ex2)
            {
                ex = ex2;
            }
            error:
            await FailToLoadGame(ex, token).ConfigureAwait(false);
            return false;
        }
        await FailToLoadGame(new Exception("Invalid type: " + (type?.Name ?? "<null>")), token).ConfigureAwait(false);
        return false;
    }
    internal static async Task FailToLoadGame(Exception? ex, CancellationToken token)
    {
        await UCWarfare.ToUpdate(token);
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
        await Data.Singletons.UnloadAllAsync(token).ConfigureAwait(false);
        await UCWarfare.ToUpdate(token);
        Data.Gamemode = null!;
        UCWarfare.ForceUnload();
    }
    protected virtual async Task EndGame(CancellationToken token)
    {
        try
        {
            await CommandHandler.LetCommandsFinish().ConfigureAwait(false);
            Type? nextMode = GetNextGamemode();
            await TryLoadGamemode(nextMode!, token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            L.LogError("Error ending game: " + DisplayName + ".");
            L.LogError(ex);
            UCWarfare.RunTask(FailToLoadGame(ex, default), ctx: "Unloading game after error.");
        }
    }
    public async Task StartNextGame(CancellationToken token, bool onLoad = false)
    {
        bool waited = UCWarfare.I.FullyLoaded;
        if (waited)
            await UCWarfare.I.PlayerJoinLock.WaitAsync(token);
        try
        {
            ThreadUtil.assertIsGameThread();
            token.CombineIfNeeded(UnloadToken);
            Task task = PreGameStarting(onLoad, token);
            if (!task.IsCompleted)
            {
                await task.ConfigureAwait(false);
                await UCWarfare.ToUpdate(token);
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
                        if (UCPlayer.LoadingUI.IsValid)
                            UCPlayer.LoadingUI.SendToPlayer(pl.Connection, val);
                    }
                }
            }
            await InvokeSingletonEvent<IGameStartListener, IGameStartListenerAsync>
                (x => x.OnGameStarting(onLoad), x => x.OnGameStarting(onLoad, token), token).ConfigureAwait(false);
            await UCWarfare.ToUpdate(token);

            ThreadUtil.assertIsGameThread();
            CooldownManager.OnGameStarting();
            IconManager.OnGamemodeReloaded(onLoad);
            L.Log($"Loading new {DisplayName} game.", ConsoleColor.Cyan);
            State = State.Active;
            GameID = DateTime.UtcNow.Ticks;
            StartTime = Time.realtimeSinceStartup;
            PlayerManager.ApplyToOnline();
            OnStateUpdated?.Invoke();
            if (!onLoad)
            {
                await InvokeSingletonEvent<ILevelStartListener, ILevelStartListenerAsync>
                    (x => x.OnLevelReady(), x => x.OnLevelReady(token), token).ConfigureAwait(false);
                await UCWarfare.ToUpdate(token);
            }
            task = PostGameStarting(onLoad, token);
            if (!task.IsCompleted)
            {
                await task;
                await UCWarfare.ToUpdate(token);
            }
            ThreadUtil.assertIsGameThread();
            await Points.UpdateAllPointsAsync(token).ConfigureAwait(false);
            await UCWarfare.ToUpdate(token);
            await KitManager.DownloadPlayersKitData(PlayerManager.OnlinePlayers, true, token).ConfigureAwait(false);
            await UCWarfare.ToUpdate(token);
            foreach (UCPlayer pl in PlayerManager.OnlinePlayers.ToList())
            {
                CancellationToken tk2 = token;
                tk2.CombineIfNeeded(pl.DisconnectToken);
                try
                {
                    await pl.PurchaseSync.WaitAsync(tk2).ConfigureAwait(false);
                    try
                    {
                        await UCWarfare.ToUpdate(tk2);
                        await Data.Gamemode.InternalPlayerInit(pl, pl.HasInitedOnce, tk2).ConfigureAwait(false);
                    }
                    finally
                    {
                        pl.PurchaseSync.Release();
                    }
                }
                catch (TaskCanceledException) when (tk2.IsCancellationRequested) { }
            }

            await UCWarfare.ToUpdate(token);
            ThreadUtil.assertIsGameThread();
            if (!onLoad)
            {
                for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
                {
                    UCPlayer pl = PlayerManager.OnlinePlayers[i];
                    pl.Player.disablePluginWidgetFlag(EPluginWidgetFlags.Modal);
                    if (pl.Player.quests.isMarkerPlaced)
                        pl.Player.quests.replicateSetMarker(false, Vector3.zero, string.Empty);
                }
                if (UCPlayer.LoadingUI.IsValid)
                    UCPlayer.LoadingUI.ClearFromAllPlayers();
            }
            await PostPlayerInit(onLoad, token).ConfigureAwait(false);
        }
        finally
        {
            if (waited)
                UCWarfare.I.PlayerJoinLock.Release();
        }
    }
    public void AnnounceMode()
    {
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            ToastMessage.QueueMessage(PlayerManager.OnlinePlayers[i], new ToastMessage(ToastMessageStyle.Large, new string[] { string.Empty, DisplayName, string.Empty }));
    }
    internal async Task OnPlayerJoined(UCPlayer player, CancellationToken token)
    {
        ThreadUtil.assertIsGameThread();
        token.CombineIfNeeded(UnloadToken);
        await InvokeSingletonEvent<IPlayerConnectListener, IPlayerConnectListenerAsync>
            (x => x.OnPlayerConnecting(player), x => x.OnPlayerConnecting(player, token), token, onlineCheck: player)
            .ConfigureAwait(false);
        await UCWarfare.ToUpdate(token);
        await InternalPlayerInit(player, false, token).ConfigureAwait(false);
        await UCWarfare.ToUpdate(token);
        if (player.IsOnline)
        {
            if (UCPlayer.LoadingUI.IsValid)
                UCPlayer.LoadingUI.ClearFromPlayer(player.Connection);
            if (!player.ModalNeeded)
                player.Player.disablePluginWidgetFlag(EPluginWidgetFlags.Modal);
        }
        player.Save.Apply(player);
    }
    public virtual void OnGroupChanged(GroupChanged e) { }
    private void OnGroupChangedIntl(GroupChanged e)
    {
        if (State == State.Staging)
        {
            if (e.NewTeam is < 1 or > 2)
                ClearStagingUI(e.Player);
            else
                ShowStagingUI(e.Player);
        }
        OnGroupChanged(e);
    }
    internal void InvokeLanguageChanged(UCPlayer player)
    {
        OnLanguageChanged(player);
        InvokeSingletonEvent<ILanguageChangedListener>(x => x.OnLanguageChanged(player));
    }
    public virtual void PlayerLeave(UCPlayer player)
    {
        if (State is not State.Active and not State.Staging)
        {
            player.Save.ShouldRespawnOnJoin = true;
            player.Save.LastGame = GameID;
            PlayerSave.WriteToSaveFile(player.Save);
        }

        if (player.Save.LastGame != GameID)
        {
            player.Save.LastGame = GameID;
            PlayerSave.WriteToSaveFile(player.Save);
        }
        InvokeSingletonEvent<IPlayerDisconnectListener>(x => x.OnPlayerDisconnecting(player));
    }
    public virtual void OnPlayerDeath(PlayerDied e)
    {
        Points.OnPlayerDeath(e);
        if (e.Player.Player.TryGetPlayerData(out UCPlayerData c))
            c.LastGunShot = default;
        InvokeSingletonEvent<IPlayerDeathListener>(x => x.OnPlayerDeath(e), e);
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
    public virtual void StartStagingPhase(float seconds)
    {
        StagingSeconds = seconds;
        State = State.Staging;
        OnStateUpdated?.Invoke();
        StagingPhaseTimer = StartCoroutine(StagingPhaseLoop());
    }
    public void SkipStagingPhase()
    {
        StagingSeconds = 0;
    }
    public IEnumerator<WaitForSeconds> StagingPhaseLoop()
    {
        ShowStagingUIForAll();

        while (StagingSeconds > 0)
        {
            if (State != State.Staging)
            {
                EndStagingPhase();
                StagingPhaseTimer = null;
                yield break;
            }

            UpdateStagingUIForAll();

            yield return new WaitForSeconds(1f);
            --StagingSeconds;
        }
        EndStagingPhase();
        StagingPhaseTimer = null;
    }
    public virtual void ShowStagingUI(UCPlayer player)
    {
        TimeSpan timeleft = TimeSpan.FromSeconds(StagingSeconds);
        CTFUI.StagingUI.SendToPlayer(player.Connection, T.PhaseBriefing.Translate(player), $"{timeleft.Minutes}:{timeleft.Seconds:D2}");
        UpdateStagingUI(player, timeleft);
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
        State = State.Active;
        OnStagingComplete();
    }
    public void ReplaceBarricadesAndStructures()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ThreadUtil.assertIsGameThread();
        L.Log("Destroying unknown barricades and structures...", ConsoleColor.Magenta);
        if (StructureManager.regions is null)
            L.LogWarning("Structure regions have not been initialized.");
        if (BarricadeManager.regions is null)
            L.LogWarning("Barricade regions have not been initialized.");
        if (this is IFOBs fobs)
            fobs.FOBManager.DestroyAllFOBs();
        try
        {
            //bool isStruct = this is IStructureSaving;
            int fails = 0;
            StructureSaver? saver = Data.Singletons.GetSingleton<StructureSaver>();
            List<(object Drop, byte X, byte Y)> toDestroy = new List<(object, byte, byte)>(16);
            saver?.WriteWait();
            try
            {
                for (byte x = 0; x < Regions.WORLD_SIZE; x++)
                {
                    for (byte y = 0; y < Regions.WORLD_SIZE; y++)
                    {
                        try
                        {
                            if (BarricadeManager.regions is not null)
                            {
                                BarricadeRegion barricadeRegion = BarricadeManager.regions[x, y];
                                for (int i = barricadeRegion.drops.Count - 1; i >= 0; --i)
                                {
                                    BarricadeDrop drop = barricadeRegion.drops[i];
                                    if (!(saver is { IsLoaded: true } && saver.TryGetSaveNoWriteLock(drop, out SavedStructure _)))
                                    {
                                        toDestroy.Add((drop, x, y));
                                    }
                                }
                            }

                            if (StructureManager.regions is not null)
                            {
                                StructureRegion structureRegion = StructureManager.regions[x, y];
                                for (int i = structureRegion.drops.Count - 1; i >= 0; --i)
                                {
                                    StructureDrop drop = structureRegion.drops[i];
                                    if (!(saver is { IsLoaded: true } && saver.TryGetSaveNoWriteLock(drop, out SavedStructure _)))
                                    {
                                        toDestroy.Add((drop, x, y));
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            L.LogError($"Failed to clear barricades/structures of region ({x}, {y}):");
                            L.LogError(ex);
                            ++fails;
                            if (fails > 5)
                                throw new SingletonLoadException(SingletonLoadType.Load, this, ex);
                        }
                    }
                }
            }
            finally
            {
                saver?.WriteRelease();
            }
            for (int i = 0; i < toDestroy.Count; ++i)
            {
                (object drop, byte x, byte y) = toDestroy[i];
                if (drop is BarricadeDrop bdrop)
                {
                    if (bdrop.model.TryGetComponent(out ISalvageInfo fob))
                        fob.IsSalvaged = true;
                    if (bdrop.interactable is InteractableStorage storage)
                        storage.despawnWhenDestroyed = true;
                    BarricadeManager.destroyBarricade(bdrop, x, y, ushort.MaxValue);
                }
                else if (drop is StructureDrop sdrop)
                    StructureManager.destroyStructure(sdrop, x, y, Vector3.zero);
            }
            CacheLocationsEditCommand.Drops.Clear();
            IconManager.DeleteAllIcons();
            IconManager.CheckExistingBuildables();
        }
        catch (Exception ex)
        {
            L.LogError("Failed to clear barricades/structures:");
            L.LogError(ex);
        }
    }

    // todo rewrite this is awful
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
                        gms.Add(new KeyValuePair<string?, float>(name, float.TryParse(current.ToString(), NumberStyles.Any, Data.AdminLocale, out weight) ? weight : 1f));
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

            if (name != null && float.TryParse(current.ToString(), NumberStyles.Any, Data.AdminLocale, out weight))
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
        string m = "Mode: " + DisplayName;
        if (this is ITickets tickets)
            m += Environment.NewLine + "Tickets 1: " + tickets.TicketManager.Team1Tickets + ", 2: " + tickets.TicketManager.Team2Tickets + ".";

        return m;
    }
    internal async Task OnQuestCompleted(QuestCompleted e, CancellationToken token)
    {
        token.CombineIfNeeded(UnloadToken, e.Player.DisconnectToken);
        await InvokeSingletonEvent<IQuestCompletedListener, IQuestCompletedListenerAsync>
            (x => x.OnQuestCompleted(e), x => x.OnQuestCompleted(e, token), token, e)
            .ConfigureAwait(false);
    }
    internal async Task HandleQuestCompleted(QuestCompleted e, CancellationToken token)
    {
        if (!RankManager.OnQuestCompleted(e))
        {
            token.CombineIfNeeded(UnloadToken, e.Player.DisconnectToken);
            await InvokeSingletonEvent<IQuestCompletedHandler, IQuestCompletedHandlerAsync>
                (x => x.OnQuestCompleted(e), x => x.OnQuestCompleted(e, token), token, e)
                .ConfigureAwait(false);
        }
        else e.Break();
    }
    internal virtual bool CanRefillAmmoAt(ItemBarricadeAsset barricade)
    {
        return Config.BarricadeAmmoCrate.MatchGuid(barricade.GUID);
    }

    protected void InvokeSingletonEvent<T>(Action<T> action, EventState? e = null)
    {
        /*
        if (typeof(T) != typeof(IGameTickListener))
            L.LogDebug("Invoke sync singleton event " + typeof(T).Name);
        */
        ThreadUtil.assertIsGameThread();
#if DEBUG
        using IDisposable profiler = ProfilingUtils
            .StartTracking("Singleton event: " + typeof(T).Name + " in " + DisplayName + ".");
#endif
        bool tmFound = false;
        for (int i = 0; i < _singletons.Count; ++i)
        {
            if (_singletons[i] is T t)
            {
                tmFound |= t is TicketManager;
                action(t);
                if (e is { CanContinue: false })
                    return;
            }
        }
        if (!tmFound && this is ITickets tickets && tickets.TicketManager.Provider is T t3)
        {
            action(t3);
            if (e is { CanContinue: false })
                return;
        }
        if (this is IGameStats stats)
        {
            if (stats.GameStats is T t)
            {
                action(t);
                if (e is { CanContinue: false })
                    return;
            }
            if (stats.GameStats is BaseStatTracker<BasePlayerStats> b)
            {
                for (int i = 0; i < b.stats.Count; ++i)
                {
                    if (b.stats[i] is T t2)
                        action(t2);
                    else break;
                    if (e is { CanContinue: false })
                        return;
                }
            }
        }
        for (int i = 0; i < Data.GamemodeListeners.Length; ++i)
        {
            if (Data.GamemodeListeners[i] is T t)
            {
                action(t);
                if (e is { CanContinue: false })
                    return;
            }
        }
    }
    protected async Task InvokeSingletonEvent<TSync, TAsync>(Action<TSync> action1, Func<TAsync, Task> action2, CancellationToken token = default, EventState? args = null, UCPlayer? onlineCheck = null)
    {
        // L.LogDebug("Invoke async singleton event " + typeof(TSync).Name + "/" + typeof(TAsync).Name);
        if (!UCWarfare.IsMainThread)
            await UCWarfare.ToUpdate(token);
        ThreadUtil.assertIsGameThread();
#if DEBUG
        using IDisposable profiler = ProfilingUtils
            .StartTracking("Async singleton event: " + typeof(TSync).Name + " / " +
                           typeof(TAsync).Name + " in " + DisplayName + ".");
#endif
        bool tmFound = false;
        
        void CheckContinue(EventState? args, UCPlayer? onlineCheck)
        {
#if DEBUG
            ThreadUtil.assertIsGameThread();
#endif
            token.ThrowIfCancellationRequested();
            if (args != null)
            {
                if (!args.CanContinue ||
                    args is PlayerEvent { Player.IsOnline: false } or BreakablePlayerEvent { Player.IsOnline: false })
                    throw new OperationCanceledException();
            }

            if (onlineCheck is { IsOnline: false })
                throw new OperationCanceledException();
        }
        for (int i = 0; i < _singletons.Count; ++i)
        {
            if (_singletons[i] is TSync t)
            {
                tmFound |= t is TicketManager;
                action1(t);
                CheckContinue(args, onlineCheck);
            }
            else if (_singletons[i] is TAsync t2)
            {
                tmFound |= t2 is TicketManager;
                Task task = action2(t2);
                if (!task.IsCompleted)
                {
                    await task.ConfigureAwait(false);
                    await UCWarfare.ToUpdate(token);
                    if (onlineCheck is { IsOnline: false })
                        return;
                    CheckContinue(args, onlineCheck);
                }
            }
        }
        if (!tmFound && this is ITickets tickets && tickets.TicketManager.Provider != null)
        {
            if (tickets.TicketManager.Provider is TSync t)
            {
                action1(t);
                CheckContinue(args, onlineCheck);
            }
            else if (tickets.TicketManager.Provider is TAsync t2)
            {
                Task task = action2(t2);
                if (!task.IsCompleted)
                {
                    await task.ConfigureAwait(false);
                    await UCWarfare.ToUpdate(token);
                    CheckContinue(args, onlineCheck);
                }
            }
        }
        if (this is IGameStats stats)
        {
            if (stats.GameStats is TSync t)
            {
                action1(t);
                CheckContinue(args, onlineCheck);
            }
            else if (stats.GameStats is TAsync t2)
            {
                Task task = action2(t2);
                if (!task.IsCompleted)
                {
                    await task.ConfigureAwait(false);
                    await UCWarfare.ToUpdate(token);
                    CheckContinue(args, onlineCheck);
                }
            }
            if (stats.GameStats is BaseStatTracker<BasePlayerStats> b)
            {
                for (int i = 0; i < b.stats.Count; ++i)
                {
                    if (b.stats[i] is TSync t2)
                    {
                        action1(t2);
                        CheckContinue(args, onlineCheck);
                    }
                    else if (b.stats[i] is TAsync t3)
                    {
                        Task task = action2(t3);
                        if (!task.IsCompleted)
                        {
                            await task.ConfigureAwait(false);
                            await UCWarfare.ToUpdate(token);
                            CheckContinue(args, onlineCheck);
                        }
                    }
                    else break;
                }
            }
        }
        for (int i = 0; i < Data.GamemodeListeners.Length; ++i)
        {
            if (Data.GamemodeListeners[i] is TSync t)
            {
                action1(t);
                CheckContinue(args, onlineCheck);
            }
            else if (Data.GamemodeListeners[i] is TAsync t3)
            {
                Task task = action2(t3);
                if (!task.IsCompleted)
                {
                    await task.ConfigureAwait(false);
                    await UCWarfare.ToUpdate(token);
                    CheckContinue(args, onlineCheck);
                }
            }
        }
    }
}
public enum State : byte
{
    Active,
    Finished,
    Loading,
    Staging,
    Discarded
}

[Translatable("Gamemode Type")]
public enum GamemodeType : byte
{
    [Translatable(Languages.ChineseSimplified, "原版")]
    [Translatable("Vanilla")]
    Undefined,
    [Translatable(Languages.ChineseSimplified, "推进并肃清")]
    [Translatable("Advance and Secure")]
    TeamCTF,
    [Translatable(Languages.ChineseSimplified, "进攻")]
    Invasion,
    [Translatable(Languages.ChineseSimplified, "叛乱")]
    Insurgency,
    [Translatable(Languages.ChineseSimplified, "征服")]
    Conquest,
    [Translatable(Languages.ChineseSimplified, "攻坚")]
    Hardpoint
}

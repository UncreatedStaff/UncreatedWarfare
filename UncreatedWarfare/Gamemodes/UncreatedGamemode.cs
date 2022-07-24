using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Players;
using Uncreated.Warfare.Commands.VanillaRework;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Gamemodes.Flags.Invasion;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Gamemodes.UI;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Tickets;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes;

public delegate Task TeamWinDelegate(ulong team);
public abstract class Gamemode : BaseSingletonComponent, IGamemode, ILevelStartListener, IReloadableSingleton, ITranslationArgument
{
    protected const float MATCH_PRESENT_THRESHOLD = 0.65f;
    public const string GAMEMODE_RELOAD_KEY = "gamemode";
    internal static readonly GamemodeConfig ConfigObj = new GamemodeConfig();
    public static readonly WinToastUI WinToastUI = new WinToastUI();
    public static readonly Vector3 BLOCKER_SPAWN_ROTATION = new Vector3(270f, 0f, 180f);
    public static readonly List<KeyValuePair<Type, float>> GAMEMODE_ROTATION = new List<KeyValuePair<Type, float>>();
    public static readonly Dictionary<string, Type> GAMEMODES = new Dictionary<string, Type>
    {
        { "TeamCTF", typeof(TeamCTF) },
        { "Invasion", typeof(Invasion) },
        { "TDM", typeof(TeamDeathmatch.TeamDeathmatch) },
        { "Insurgency", typeof(Insurgency.Insurgency) }
    };
    public event TeamWinDelegate OnTeamWin;
    public Whitelister Whitelister;
    public CooldownManager Cooldowns;
    public Tips Tips;
    public Coroutine EventLoopCoroutine;
    public bool isPendingCancel;
    internal string shutdownMessage = string.Empty;
    internal bool shutdownAfterGame = false;
    internal ulong shutdownPlayer = 0;
    protected readonly string _name;
    protected float _startTime = 0f;
    protected long _gameID;
    protected int _ticks = 0;
    protected int _stagingSeconds;
    protected EState _state;
    private float _eventLoopSpeed;
    private bool useEventLoop;
    private bool _isPreLoading;
    private List<IUncreatedSingleton> _singletons;
    private bool wasLevelLoadedOnStart;
    private bool _hasOnReadyRan = false;
    public EState State => _state;
    public float StartTime => _startTime;
    public int StagingSeconds => _stagingSeconds;
    public float SecondsSinceStart => Time.realtimeSinceStartup - _startTime;
    public long GameID => _gameID;
    public static GamemodeConfigData Config => ConfigObj.Data;
    public string Name { get => _name; }
    public float EventLoopSpeed => _eventLoopSpeed;
    public bool EveryMinute => _ticks % Mathf.RoundToInt(60f / _eventLoopSpeed) == 0;
    public bool Every30Seconds => _ticks % Mathf.RoundToInt(30f / _eventLoopSpeed) == 0;
    public bool Every15Seconds => _ticks % Mathf.RoundToInt(15f / _eventLoopSpeed) == 0;
    public bool Every10Seconds => _ticks % Mathf.RoundToInt(10f / _eventLoopSpeed) == 0;
    public string? ReloadKey => GAMEMODE_RELOAD_KEY;
    public virtual bool UseWhitelist => true;
    public abstract string DisplayName { get; }
    public virtual bool TransmitMicWhileNotActive => true;
    public virtual bool ShowXPUI => true;
    public virtual bool ShowOFPUI => true;
    public virtual bool UseTips => true;
    public virtual bool AllowCosmetics => true;
    public virtual EGamemode GamemodeType => EGamemode.UNDEFINED;
    protected bool HasOnReadyRan => _hasOnReadyRan;
    public Gamemode(string Name, float EventLoopSpeed)
    {
        this._name = Name;
        this._eventLoopSpeed = EventLoopSpeed;
        this.useEventLoop = EventLoopSpeed > 0;
        this._state = EState.LOADING;
    }
    public void SetTiming(float NewSpeed)
    {
        this._eventLoopSpeed = NewSpeed;
        this.useEventLoop = NewSpeed > 0;
    }
    public override void Load()
    {
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
        PreInit();
        _isPreLoading = false;
        Data.Singletons.LoadSingletonsInOrder(_singletons);
        InternalSubscribe();
        Subscribe();
        InternalPostInit();
        PostInit();
        if (wasLevelLoadedOnStart)
        {
            foreach (ILevelStartListener listener in _singletons.OfType<ILevelStartListener>())
                listener.OnLevelReady();
            InternalOnReady();
            OnReady();
            PostOnReady();
            _hasOnReadyRan = true;
        }
    }
    public void Reload()
    {
        Unload();
        ConfigObj.Reload();
        Load();
    }
    public override void Unload()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking(Name + " Unload Sequence");
#endif
        Unsubscribe();
        InternalUnsubscribe();
        PreDispose();
        InternalPreDispose();
        Data.Singletons.UnloadSingletonsInOrder(_singletons);
        PostDispose();
    }
    /// <summary>Use to add <see cref="IUncreatedSingleton"/>s to be loaded.</summary>
    /// <remarks>Abstract</remarks>
    protected abstract void PreInit();
    
    /// <summary>Called after all <see cref="IUncreatedSingleton"/>s have been loaded.</summary>
    /// <remarks>Abstract</remarks>
    protected abstract void PostInit();

    /// <summary>Called just before all <see cref="IUncreatedSingleton"/>s are unloaded.</summary>
    /// <remarks>Abstract</remarks>
    protected abstract void PreDispose();

    /// <summary>Called just after all <see cref="IUncreatedSingleton"/>s have been unloaded.</summary>
    /// <remarks>No base</remarks>
    protected virtual void PostDispose() { }

    /// <summary>If the level is already loaded, called after <see cref="PostInit"/>, otherwise called when the level is loaded.</summary>
    /// <remarks>No base, guranteed to be called after all registered <see cref="ILevelStartListener.OnLevelReady"/>'s have been called.</remarks>
    protected virtual void OnReady() { }

    /// <summary>Runs just before a game starts.</summary>
    /// <param name="isOnLoad">Whether this is the first game played on this singleton since running <see cref="Load"/>.</param>
    /// <remarks>Called from <see cref="StartNextGame(bool)"/></remarks>
    protected virtual void PreGameStarting(bool isOnLoad) { }

    /// <summary>Runs just after a game starts.</summary>
    /// <param name="isOnLoad">Whether this is the first game played on this singleton since running <see cref="Load"/>.</param>
    /// <remarks>Called from <see cref="StartNextGame(bool)"/></remarks>
    protected virtual void PostGameStarting(bool isOnLoad) { }

    /// <summary>Runs after all players have been initialized.</summary>
    /// <param name="isOnLoad">Whether this is the first game played on this singleton since running <see cref="Load"/>.</param>
    /// <remarks>Called from <see cref="StartNextGame(bool)"/></remarks>
    protected virtual void PostPlayerInit(bool isOnLoad) { }

    /// <summary>Ran when a player joins or per online player after the game starts.</summary>
    /// <remarks>No base</remarks>
    public virtual void PlayerInit(UCPlayer player, bool wasAlreadyOnline) { }

    /// <summary>Ran after a player joins once all async functions have been ran. Good for things that need to know about kit access, xp, credits, etc.</summary>
    /// <remarks>No base</remarks>
    protected virtual void OnAsyncInitComplete(UCPlayer player) { }

    ///<summary>Run in <see cref="EventLoopAction"/>, returns true if <param name="seconds"/> ago it would've also returned true. Based on tick speed and number of ticks.</summary>
    public bool EveryXSeconds(float seconds) => _ticks % Mathf.RoundToInt(seconds / _eventLoopSpeed) == 0;
    private void InternalPreInit()
    {
        AddSingletonRequirement(ref Cooldowns);
        if (UseWhitelist)
            AddSingletonRequirement(ref Whitelister);
        if (UseTips)
            AddSingletonRequirement(ref Tips);
    }
    private void InternalPlayerInit(UCPlayer player, bool wasAlreadyOnline)
    {
        foreach (IPlayerInitListener listener in _singletons.OfType<IPlayerInitListener>())
            listener.OnPlayerInit(player, wasAlreadyOnline);
        PlayerInit(player, wasAlreadyOnline);
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
        ReplaceBarricadesAndStructures();
        if (useEventLoop)
        {
            EventLoopCoroutine = StartCoroutine(EventLoop());
        }
    }
    private void PostOnReady()
    {
        StartNextGame(true);
    }
    private void InternalPostInit()
    {
        _ticks = 0;
    }
    public void OnLevelReady()
    {
        if (!wasLevelLoadedOnStart)
        {
            foreach (ILevelStartListener listener in _singletons.OfType<ILevelStartListener>())
                listener.OnLevelReady();
            InternalOnReady();
            OnReady();
            PostOnReady();
            _hasOnReadyRan = true;
        }
    }

    private void InternalSubscribe()
    {
        EventDispatcher.OnGroupChanged += OnGroupChangedIntl;
    }
    private void InternalUnsubscribe()
    {
        EventDispatcher.OnGroupChanged -= OnGroupChangedIntl;
    }
    protected void InvokeOnTeamWin(ulong winner)
    {
        if (OnTeamWin != null)
            OnTeamWin.Invoke(winner);
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
    public static void OnStagingComplete()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (VehicleSpawner.Loaded)
            VehicleSpawner.UpdateSignsWhere(spawn => VehicleBay.VehicleExists(spawn.VehicleID, out VehicleData data) && data.HasDelayType(EDelayType.OUT_OF_STAGING));
    }
    protected abstract void EventLoopAction();
    private IEnumerator<WaitForSeconds> EventLoop()
    {
        while (!isPendingCancel)
        {
            _ticks++;
            yield return new WaitForSeconds(_eventLoopSpeed);
#if DEBUG
            IDisposable profiler = ProfilingUtils.StartTracking(Name + " Gamemode Event Loop");
#endif
            DateTime start = DateTime.Now;
            for (int i = 0; i < Provider.clients.Count; i++)
            {
                SteamPlayer sp = Provider.clients[i];
                try
                {
                    if (sp.player.transform == null)
                    {
                        L.Log($"Kicking {F.GetPlayerOriginalNames(sp).PlayerName} ({sp.playerID.steamID.m_SteamID}) for null transform.", ConsoleColor.Cyan);
                        Provider.kick(sp.playerID.steamID, Localization.Translate("null_transform_kick_message", sp, UCWarfare.Config.DiscordInviteCode));
                        continue;
                    }
                }
                catch (NullReferenceException)
                {
                    L.Log($"Kicking {F.GetPlayerOriginalNames(sp).PlayerName} ({sp.playerID.steamID.m_SteamID}) for null transform.", ConsoleColor.Cyan);
                    Provider.kick(sp.playerID.steamID, Localization.Translate("null_transform_kick_message", sp, UCWarfare.Config.DiscordInviteCode));
                    continue;
                }
                /*
                // TODO: Fix
                if (Data.Is(out ITeams t) && t.UseTeamSelector && Teams.TeamManager.LobbyZone.IsInside(sp.player.transform.position) && sp.player.movement.getVehicle() == null &&
                    UCPlayer.FromSteamPlayer(sp) is UCPlayer pl && pl.GetTeam() != 3 && !t.JoinManager.IsInLobby(pl))
                {
                    L.Log($"{pl.Steam64} was stuck in lobby and was auto-rejoined.");
                    t.JoinManager.OnPlayerDisconnected(pl);
                    t.JoinManager.CloseUI(pl);
                    t.JoinManager.OnPlayerConnected(pl, true);
                }*/
            }
            try
            {
                EventLoopAction();
            }
            catch (Exception ex)
            {
                L.LogError("Error in " + Name + " gamemode in the event loop:");
                L.LogError(ex);
            }

            Quests.QuestManager.OnGameTick();
#if DEBUG
            profiler.Dispose();
            if (EveryXSeconds(150))
            {
                F.SaveProfilingData();
            }
#endif
            if (UCWarfare.I.CoroutineTiming)
                L.Log(Name + " Eventloop: " + (DateTime.Now - start).TotalMilliseconds.ToString(Data.Locale) + "ms.");
        }
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
    public abstract void DeclareWin(ulong winner);
    public static bool TryLoadGamemode(Type type)
    {
        if (type is not null && typeof(Gamemode).IsAssignableFrom(type))
        {
            if (Data.Gamemode is not null)
            {
                Data.Gamemode._state = EState.DISCARD;
                Data.Singletons.UnloadSingleton(ref Data.Gamemode);
            }
            SingletonLoadException? ex = null;
            try
            {
                IUncreatedSingleton sgl = null!;
                Data.Singletons.PopulateSingleton(ref sgl, type, true);
                Data.Gamemode = (sgl as Gamemode)!;
                if (Data.Gamemode is null)
                    goto error;
                Data.Singletons.LoadSingleton(Data.Gamemode);
                ActionLogger.Add(EActionLogType.GAMEMODE_CHANGED_AUTO, Data.Gamemode.DisplayName);
                L.Log("Chosen new gamemode " + Data.Gamemode.DisplayName, ConsoleColor.DarkCyan);
                return true;
            }
            catch (SingletonLoadException ex2)
            {
                ex = ex2;
                goto error;
            }
        error:
            L.LogError("Failed to load gamemode, shutting down in 10 seconds.");
            if (ex is not null)
            {
                ShutdownCommand.ShutdownIn(10, "There was a fatal error in the server: " + (ex.InnerException?.GetType()?.Name ?? nameof(SingletonLoadException)) + ". It will restart in 10 seconds.");
                L.NetCalls.SendFatalException.NetInvoke((ex.InnerException ?? ex).ToString());
            }
            else
            {
                ShutdownCommand.ShutdownIn(10, "There was a fatal error in the server. It will restart in 10 seconds.");
            }
            EffectManager.askEffectClearAll();
            Data.Singletons.UnloadAll();
            Data.Gamemode = null!;
            UCWarfare.ForceUnload();
            return false;
        }
        return false;
    }
    protected virtual void EndGame()
    {
        Type? nextMode = GetNextGamemode();
        if (this.GetType() != nextMode)
            TryLoadGamemode(nextMode!);
        else
            Data.Singletons.ReloadSingleton(ReloadKey!);
    }
    public void StartNextGame(bool onLoad = false)
    {
        PreGameStarting(onLoad);
        foreach (IGameStartListener listener in _singletons.OfType<IGameStartListener>())
            listener.OnGameStarting(onLoad);
        CooldownManager.OnGameStarting();
        L.Log($"Loading new {DisplayName} game.", ConsoleColor.Cyan);
        _state = EState.ACTIVE;
        _gameID = DateTime.Now.Ticks;
        _startTime = Time.realtimeSinceStartup;
        for (int i = 0; i < Provider.clients.Count; i++)
            if (PlayerManager.HasSave(Provider.clients[i].playerID.steamID.m_SteamID, out PlayerSave save)) save.LastGame = _gameID;
        PlayerManager.ApplyToOnline();
        foreach (ILevelStartListener listener in _singletons.OfType<ILevelStartListener>())
            listener.OnLevelReady();
        PostGameStarting(onLoad);
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            Data.Gamemode.InternalPlayerInit(PlayerManager.OnlinePlayers[i], true);
        PostPlayerInit(onLoad);
    }
    public void AnnounceMode()
    {
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            ToastMessage.QueueMessage(PlayerManager.OnlinePlayers[i], new ToastMessage("", DisplayName, EToastMessageSeverity.BIG));
    }
    public void OnPlayerJoined(UCPlayer player)
    {
        foreach (IPlayerConnectListener listener in _singletons.OfType<IPlayerConnectListener>())
            listener.OnPlayerConnecting(player);
        InternalPlayerInit(player, false);
    }
    internal void InternalOnAsyncInitComplete(UCPlayer player)
    {
        foreach (IPlayerAsyncInitListener listener in _singletons.OfType<IPlayerAsyncInitListener>())
            listener.OnAsyncInitComplete(player);
        OnAsyncInitComplete(player);
    }
    public virtual void OnGroupChanged(GroupChanged e) { }
    private void OnGroupChangedIntl(GroupChanged e)
    {
        OnGroupChanged(e);
    }
    public virtual void PlayerLeave(UCPlayer player)
    {
        if (State is not EState.ACTIVE or EState.STAGING && PlayerSave.TryReadSaveFile(player, out PlayerSave save))
        {
            save.ShouldRespawnOnJoin = true;
            PlayerSave.WriteToSaveFile(save);
        }
        foreach (IPlayerDisconnectListener listener in _singletons.OfType<IPlayerDisconnectListener>())
            listener.OnPlayerDisconnecting(player);
    }
    public virtual void OnPlayerDeath(PlayerDied e) { }
    public static Type? FindGamemode(string name)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        try
        {
            if (GAMEMODES.TryGetValue(name, out Type type))
            {
                if (type is null || !type.IsSubclassOf(typeof(Gamemode))) return null;
                return type;
            }
            else return null;
        }
        catch (Exception ex)
        {
            L.LogWarning("Exception when finding gamemode: \"" + name + '\"');
            L.LogError(ex, ConsoleColor.Yellow);
            return null;
        }
    }
    public virtual void Subscribe() { }
    public virtual void Unsubscribe() { }
    protected Coroutine? _stagingPhaseTimer;
    public virtual void StartStagingPhase(int seconds)
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
        CTFUI.StagingUI.Top.SetText(player.Connection, Localization.Translate("phases_briefing", player));
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
            bool isStruct = this is IStructureSaving;
            int fails = 0;
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
                                uint instid = drop.instanceID;
                                if (!(isStruct && (StructureSaver.StructureExists(instid, EStructType.BARRICADE, out _) || RequestSigns.SignExists(instid, out _))))
                                {
                                    if (drop.model.TryGetComponent(out FOBComponent fob))
                                    {
                                        fob.parent.IsWipedByAuthority = true;
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
                                uint instid = drop.instanceID;
                                if (!(isStruct && StructureSaver.StructureExists(instid, EStructType.STRUCTURE, out _)))
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
            RequestSigns.DropAllSigns();
            StructureSaver.DropAllStructures();
            IconManager.OnLevelLoaded();
        }
        catch (Exception ex)
        {
            L.LogError($"Failed to clear barricades/structures:");
            L.LogError(ex);
        }
    }
    public static void ReadGamemodes()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (GAMEMODE_ROTATION.Count > 0) GAMEMODE_ROTATION.Clear();
        if (UCWarfare.Config.GamemodeRotation == null)
        {
            GAMEMODE_ROTATION.Add(new KeyValuePair<Type, float>(typeof(TeamCTF), 1.0f));
            return;
        }
        List<KeyValuePair<string?, float>> gms = new List<KeyValuePair<string?, float>>();
        using (IEnumerator<char> iter = UCWarfare.Config.GamemodeRotation.GetEnumerator())
        {
            StringBuilder current = new StringBuilder(32);
            string? name = null;
            bool inName = true;
            float weight = 1f;
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
                        if (float.TryParse(current.ToString(), System.Globalization.NumberStyles.Any, Data.Locale, out weight))
                            gms.Add(new KeyValuePair<string?, float>(name, weight));
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
            if (name != null && float.TryParse(current.ToString(), System.Globalization.NumberStyles.Any, Data.Locale, out weight))
                gms.Add(new KeyValuePair<string?, float>(name, weight));
        }
        using (IEnumerator<KeyValuePair<string?, float>> iter = gms.GetEnumerator())
        {
            while (iter.MoveNext())
            {
                if (iter.Current.Key != null && GAMEMODES.TryGetValue(iter.Current.Key, out Type GamemodeType))
                    GAMEMODE_ROTATION.Add(new KeyValuePair<Type, float>(GamemodeType, iter.Current.Value));
            }
        }
    }
    public static Type? GetNextGamemode()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        using (IEnumerator<KeyValuePair<Type, float>> iter = GAMEMODE_ROTATION.GetEnumerator())
        {
            float total = 0f;
            while (iter.MoveNext())
            {
                total += iter.Current.Value;
            }
            float sel = UnityEngine.Random.Range(0f, total);
            iter.Reset();
            total = 0f;
            while (iter.MoveNext())
            {
                total += iter.Current.Value;
                if (sel < total)
                {
                    return iter.Current.Key;
                }
            }
        }
        return null;
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
    INSURGENCY
}

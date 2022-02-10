using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Gamemodes.Interfaces;
using UnityEngine;
using Rocket.Unturned.Player;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Flags.Invasion;
using System.Text;
using Uncreated.Players;
using Uncreated.Warfare.Tickets;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Gamemodes
{
    public enum EGamemode : byte
    {
        UNDEFINED,
        TEAM_CTF,
        INVASION,
        INSURGENCY
    }

    public delegate Task TeamWinDelegate(ulong team);
    public abstract class Gamemode : MonoBehaviour, IDisposable, IGamemode
    {
        protected const float MATCH_PRESENT_THRESHOLD = 0.65f;
        public static readonly Vector3 BLOCKER_SPAWN_ROTATION = new Vector3(270f, 0f, 180f);
        public virtual EGamemode GamemodeType { get => EGamemode.UNDEFINED; }
        public static readonly Dictionary<string, Type> GAMEMODES = new Dictionary<string, Type>
        {
            { "TeamCTF", typeof(TeamCTF) },
            { "Invasion", typeof(Invasion) },
            { "TDM", typeof(TeamDeathmatch.TeamDeathmatch) },
            { "Insurgency", typeof(Insurgency.Insurgency) }
        };
        internal static readonly Config<GamemodeConfigs> ConfigObj = new Config<GamemodeConfigs>(Data.DATA_DIRECTORY, "gamemode_settings.json");
        public static GamemodeConfigs Config => ConfigObj.data;
        public static readonly List<KeyValuePair<Type, float>> GAMEMODE_ROTATION = new List<KeyValuePair<Type, float>>();
        protected readonly string _name;
        public string Name { get => _name; }
        private float _eventLoopSpeed;
        public float EventLoopSpeed => _eventLoopSpeed;
        public bool EveryMinute => _ticks % Mathf.RoundToInt(60f / _eventLoopSpeed) == 0;
        public bool Every30Seconds => _ticks % Mathf.RoundToInt(30f / _eventLoopSpeed) == 0;
        public bool Every15Seconds => _ticks % Mathf.RoundToInt(15f / _eventLoopSpeed) == 0;
        public bool Every10Seconds => _ticks % Mathf.RoundToInt(10f / _eventLoopSpeed) == 0;
        public bool EveryXSeconds(float seconds) => _ticks % Mathf.RoundToInt(seconds / _eventLoopSpeed) == 0;
        protected float _startTime = 0f;
        public float StartTime => _startTime;
        public float SecondsSinceStart => Time.realtimeSinceStartup - _startTime;
        private bool useEventLoop;
        public event TeamWinDelegate OnTeamWin;
        public PlayerManager LogoutSaver;
        public Whitelister Whitelister;
        public CooldownManager Cooldowns;
        public virtual bool UseWhitelist { get => true; }
        protected EState _state;
        public EState State { get => _state; }
        protected string shutdownMessage = string.Empty;
        protected bool shutdownAfterGame = false;
        protected ulong shutdownPlayer = 0;
        public Coroutine EventLoopCoroutine;
        public bool isPendingCancel;
        public abstract string DisplayName { get; }
        public virtual bool TransmitMicWhileNotActive { get => true; }
        public virtual bool ShowXPUI { get => true; }
        public virtual bool ShowOFPUI { get => true; }
        public virtual bool AllowCosmetics { get => true; }
        protected int _ticks = 0;
        protected int _stagingSeconds { get; set; }
        public int StagingSeconds { get => _stagingSeconds; }

        protected long _gameID;
        public long GameID { get => _gameID; }
        public Gamemode(string Name, float EventLoopSpeed)
        {
            this._name = Name;
            this._eventLoopSpeed = EventLoopSpeed;
            this.useEventLoop = EventLoopSpeed > 0;
            this._state = EState.LOADING;
        }
        protected void SetTiming(float NewSpeed)
        {
            this._eventLoopSpeed = NewSpeed;
            this.useEventLoop = NewSpeed > 0;
        }
        public void CancelCoroutine()
        {
            isPendingCancel = true;
            if (EventLoopCoroutine == null)
                return;
            StopCoroutine(EventLoopCoroutine);
            L.Log("Event loop stopped", ConsoleColor.DarkGray);
        }
        public virtual void Init()
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            LogoutSaver = new PlayerManager();
            for (int i = 0; i < Provider.clients.Count; i++)
                PlayerManager.InvokePlayerConnected(UnturnedPlayer.FromSteamPlayer(Provider.clients[i]));
            Cooldowns = new CooldownManager();
            if (UseWhitelist)
                Whitelister = new Whitelister();
            Subscribe();
            _ticks = 0;
        }
        protected void InvokeOnTeamWin(ulong winner)
        {
            if (OnTeamWin != null)
                OnTeamWin.Invoke(winner);
        }
        public static void OnStagingComplete()
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            for (int i = 0; i < VehicleSpawner.ActiveObjects.Count; i++)
            {
                Vehicles.VehicleSpawn spawn = VehicleSpawner.ActiveObjects[i];
                if (VehicleBay.VehicleExists(spawn.VehicleID, out VehicleData data) && data.HasDelayType(EDelayType.OUT_OF_STAGING))
                {
                    spawn.UpdateSign();
                }
            }
        }
        protected abstract void EventLoopAction();
        private IEnumerator<WaitForSeconds> EventLoop()
        {
            while (!isPendingCancel)
            {
                _ticks++;
                yield return new WaitForSeconds(_eventLoopSpeed);
                IDisposable profiler = ProfilingUtils.StartTracking(Name + " Gamemode Event Loop");
                DateTime start = DateTime.Now;
                for (int i = 0; i < Provider.clients.Count; i++)
                {
                    try
                    {
                        if (Provider.clients[i].player.transform == null)
                        {
                            L.Log($"Kicking {F.GetPlayerOriginalNames(Provider.clients[i]).PlayerName} ({Provider.clients[i].playerID.steamID.m_SteamID}) for null transform.", ConsoleColor.Cyan);
                            Provider.kick(Provider.clients[i].playerID.steamID, Translation.Translate("null_transform_kick_message", Provider.clients[i], UCWarfare.Config.DiscordInviteCode));
                            continue;
                        }
                    }
                    catch (NullReferenceException)
                    {
                        L.Log($"Kicking {F.GetPlayerOriginalNames(Provider.clients[i]).PlayerName} ({Provider.clients[i].playerID.steamID.m_SteamID}) for null transform.", ConsoleColor.Cyan);
                        Provider.kick(Provider.clients[i].playerID.steamID, Translation.Translate("null_transform_kick_message", Provider.clients[i], UCWarfare.Config.DiscordInviteCode));
                        continue;
                    }
                    // TODO: Fix
                    if (Data.Is(out ITeams t) && Teams.TeamManager.LobbyZone.IsInside(Provider.clients[i].player.transform.position) && 
                        t.UseJoinUI && UCPlayer.FromSteamPlayer(Provider.clients[i]) is UCPlayer pl && !t.JoinManager.IsInLobby(pl))
                    {
                        L.Log($"{pl.Steam64} was stuck in lobby and was auto-rejoined.");
                        t.JoinManager.OnPlayerDisconnected(pl);
                        t.JoinManager.CloseUI(pl);
                        t.JoinManager.OnPlayerConnected(pl, true);
                    }
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
                profiler.Dispose();
                if (EveryXSeconds(150))
                {
                    F.SaveProfilingData();
                }
                if (UCWarfare.I.CoroutineTiming)
                    L.Log(Name + " Eventloop: " + (DateTime.Now - start).TotalMilliseconds.ToString(Data.Locale) + "ms.");
            }
        }
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
        public bool KeepGamemode()
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            Type nextMode = GetNextGamemode();
            if (this.GetType() != nextMode)
            {
                Gamemode gamemode = UCWarfare.I.gameObject.AddComponent(nextMode) as Gamemode;
                if (gamemode != null)
                {
                    this.Dispose();
                    Data.Gamemode = gamemode;
                    gamemode.Init();
                    gamemode.OnLevelLoaded();
                    //Chat.Broadcast("force_loaded_gamemode", Data.Gamemode.DisplayName);
                    for (int i = 0; i < Provider.clients.Count; i++)
                        gamemode.OnPlayerJoined(UCPlayer.FromSteamPlayer(Provider.clients[i]), true, true);
                    L.Log("Chosen new gameode " + gamemode.DisplayName, ConsoleColor.DarkCyan);
                    _state = EState.DISCARD;
                    Destroy(this);
                    return false;
                }
            }
            for (int i = 0; i < Provider.clients.Count; i++)
            {
                Data.Gamemode.OnPlayerJoined(UCPlayer.FromSteamPlayer(Provider.clients[i]), true, true);
            }

            return true;
        }
        protected virtual void EndGame()
        {
            if (KeepGamemode())
                StartNextGame(false);
        }
        public virtual void StartNextGame(bool onLoad = false)
        {
            CooldownManager.OnGameStarting();
            L.Log($"Loading new {DisplayName} game.", ConsoleColor.Cyan);
            _state = EState.ACTIVE;
            _gameID = DateTime.Now.Ticks;
            _startTime = Time.realtimeSinceStartup;
            for (int i = 0; i < Provider.clients.Count; i++)
                if (PlayerManager.HasSave(Provider.clients[i].playerID.steamID.m_SteamID, out PlayerSave save)) save.LastGame = _gameID;
            PlayerManager.ApplyToOnline();
            if (this is IVehicles && VehicleSpawner.ActiveObjects != null)
            {
                for (int i = 0; i < VehicleSpawner.ActiveObjects.Count; i++)
                    VehicleSpawner.UpdateSigns(VehicleSpawner.ActiveObjects[i].VehicleID);
            }
        }
        public void AnnounceMode()
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
                ToastMessage.QueueMessage(PlayerManager.OnlinePlayers[i], new ToastMessage("", DisplayName, EToastMessageSeverity.BIG));
        }
        public virtual void OnGroupChanged(UCPlayer player, ulong oldGroup, ulong newGroup, ulong oldteam, ulong newteam)
        { }
        public virtual void OnPlayerJoined(UCPlayer player, bool wasAlreadyOnline, bool shouldRespawn)
        {
            Points.OnPlayerJoined(player, wasAlreadyOnline);
        }
        public virtual void OnPlayerLeft(UCPlayer player)
        { }
        public virtual void OnPlayerDeath(UCWarfare.DeathEventArgs args)
        { }
        public virtual void OnLevelLoaded()
        {
            ReplaceBarricadesAndStructures();
            StartNextGame(true);
            if (useEventLoop)
            {
                EventLoopCoroutine = StartCoroutine(EventLoop());
            }
        }
        public static Gamemode FindGamemode(string name)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            try
            {
                if (GAMEMODES.TryGetValue(name, out Type type))
                {
                    if (type == default) return null;
                    if (!type.IsSubclassOf(typeof(Gamemode))) return null;
                    Gamemode gamemode = UCWarfare.I.gameObject.AddComponent(type) as Gamemode;
                    return gamemode;
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
        public virtual void Subscribe()
        { }
        public virtual void Unsubscribe()
        { }
        protected Coroutine _stagingPhaseTimer;
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

                yield return new WaitForSeconds(1);
                _stagingSeconds--;
            }
            EndStagingPhase();
            _stagingPhaseTimer = null;
        }
        public virtual void ShowStagingUI(UCPlayer player)
        {
            EffectManager.sendUIEffect(CTFUI.headerID, CTFUI.headerKey, player.connection, true);
            EffectManager.sendUIEffectText(CTFUI.headerKey, player.connection, true, "Top", Translation.Translate("phases_briefing", player));
        }
        public void ClearStagingUI(UCPlayer player)
        {
            EffectManager.askEffectClearByID(CTFUI.headerID, player.connection);
        }
        public void ShowStagingUIForAll()
        {
            foreach (UCPlayer player in PlayerManager.OnlinePlayers)
                ShowStagingUI(player);
        }
        public void UpdateStagingUI(UCPlayer player, TimeSpan timeleft)
        {
            EffectManager.sendUIEffectText(CTFUI.headerKey, player.connection, true, "Bottom", $"{timeleft.Minutes}:{timeleft.Seconds:D2}");
        }
        public void UpdateStagingUIForAll()
        {
            TimeSpan timeLeft = TimeSpan.FromSeconds(StagingSeconds);
            foreach (UCPlayer player in PlayerManager.OnlinePlayers)
            {
                ulong team = player.GetTeam();
                if (team == 1 || team == 2)
                    UpdateStagingUI(player, timeLeft);
            }
        }
        protected virtual void EndStagingPhase()
        {
            if (this is ITickets)
                TicketManager.OnStagingPhaseEnded();
            EffectManager.ClearEffectByID_AllPlayers(CTFUI.headerID);
            _state = EState.ACTIVE;
            OnStagingComplete();
        }
        public virtual void Dispose()
        {
            if (_stagingPhaseTimer != null)
                StopCoroutine(_stagingPhaseTimer);
            Unsubscribe();
            CancelCoroutine();
            Whitelister?.Dispose();
            if (_state == EState.STAGING)
            {
                if (_stagingPhaseTimer != null)
                    StopCoroutine(_stagingPhaseTimer);
                _stagingSeconds = 0;
                EndStagingPhase();
                _stagingPhaseTimer = null;
            }
        }
        public void ReplaceBarricadesAndStructures()
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            try
            {
                bool isStruct = this is IStructureSaving;
                for (byte x = 0; x < Regions.WORLD_SIZE; x++)
                {
                    for (byte y = 0; y < Regions.WORLD_SIZE; y++)
                    {
                        try
                        {
                            for (int i = BarricadeManager.regions[x, y].drops.Count - 1; i >= 0; i--)
                            {
                                uint instid = BarricadeManager.regions[x, y].drops[i].instanceID;
                                if (!(isStruct && (StructureSaver.StructureExists(instid, EStructType.BARRICADE, out _) || RequestSigns.SignExists(instid, out _))))
                                {
                                    if (BarricadeManager.regions[x, y].drops[i].model.TryGetComponent(out Components.FOBComponent fob))
                                    {
                                        fob.parent.IsWipedByAuthority = true;
                                    }

                                    if (BarricadeManager.regions[x, y].drops[i].model.transform.TryGetComponent(out InteractableStorage storage))
                                        storage.despawnWhenDestroyed = true;
                                    BarricadeManager.destroyBarricade(BarricadeManager.regions[x, y].drops[i], x, y, ushort.MaxValue);
                                }
                            }
                            for (int i = StructureManager.regions[x, y].drops.Count - 1; i >= 0; i--)
                            {
                                uint instid = StructureManager.regions[x, y].drops[i].instanceID;
                                if (!(isStruct && StructureSaver.StructureExists(instid, EStructType.STRUCTURE, out _)))
                                    StructureManager.destroyStructure(StructureManager.regions[x, y].drops[i], x, y, Vector3.zero);
                            }
                        }
                        catch (Exception ex)
                        {
                            L.LogError($"Failed to clear barricades/structures of region ({x}, {y}):");
                            L.LogError(ex);
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
            using IDisposable profiler = ProfilingUtils.StartTracking();
            if (GAMEMODE_ROTATION.Count > 0) GAMEMODE_ROTATION.Clear();
            if (UCWarfare.Config.GamemodeRotation == null)
            {
                GAMEMODE_ROTATION.Add(new KeyValuePair<Type, float>(typeof(TeamCTF), 1.0f));
                return;
            }
            List<KeyValuePair<string, float>> gms = new List<KeyValuePair<string, float>>();
            using (IEnumerator<char> iter = UCWarfare.Config.GamemodeRotation.GetEnumerator())
            {
                StringBuilder current = new StringBuilder(32);
                string name = null;
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
                            gms.Add(new KeyValuePair<string, float>(current.ToString(), 1f));
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
                                gms.Add(new KeyValuePair<string, float>(name, weight));
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
                    gms.Add(new KeyValuePair<string, float>(name, weight));
            }
            using (IEnumerator<KeyValuePair<string, float>> iter = gms.GetEnumerator())
            {
                while (iter.MoveNext())
                {
                    if (GAMEMODES.TryGetValue(iter.Current.Key, out Type GamemodeType))
                        GAMEMODE_ROTATION.Add(new KeyValuePair<Type, float>(GamemodeType, iter.Current.Value));
                }
            }
        }
        public static Type GetNextGamemode()
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
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
                        L.Log($"    Chosen: {iter.Current.Key.Name}");
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
}

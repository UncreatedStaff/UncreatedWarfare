using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Officers;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Gamemodes.Interfaces;
using UnityEngine;
using Rocket.Unturned.Player;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Flags.Invasion;
using Uncreated.Warfare.Gamemodes.TeamDeathmatch;
using System.Text;
using Uncreated.Players;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Gamemodes
{
    public delegate Task TeamWinDelegate(ulong team);
    public abstract class Gamemode : MonoBehaviour, IDisposable, IGamemode
    {
        public static readonly Dictionary<string, Type> GAMEMODES = new Dictionary<string, Type>
        {
            { "TeamCTF", typeof(TeamCTF) },
            { "Invasion", typeof(Invasion) },
            { "TDM", typeof(TeamDeathmatch.TeamDeathmatch) },
            { "Insurgency", typeof(Insurgency) }
        };
        public static readonly List<KeyValuePair<Type, float>> GAMEMODE_ROTATION = new List<KeyValuePair<Type, float>>();
        protected readonly string _name;
        public string Name { get => _name; }
        private float EventLoopSpeed;
        private bool useEventLoop;
        public event TeamWinDelegate OnTeamWin;
        public OfficerManager OfficerManager;
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
        public virtual float Weight { get => 1.0f; }

        protected long _gameID;
        public long GameID { get => _gameID; }
        public Gamemode(string Name, float EventLoopSpeed)
        {
            this._name = Name;
            this.EventLoopSpeed = EventLoopSpeed;
            this.useEventLoop = EventLoopSpeed > 0;
            this._state = EState.LOADING;
        }
        protected void SetTiming(float NewSpeed)
        {
            this.EventLoopSpeed = NewSpeed;
            this.useEventLoop = NewSpeed > 0;
        }
        public void CancelCoroutine()
        {
            isPendingCancel = true;
            if (EventLoopCoroutine == null)
                return;
            StopCoroutine(EventLoopCoroutine);
            F.Log("Event loop stopped", ConsoleColor.DarkGray);
        }
        public virtual void Init()
        {
            LogoutSaver = new PlayerManager();
            for (int i = 0; i < Provider.clients.Count; i++)
                PlayerManager.InvokePlayerConnected(UnturnedPlayer.FromSteamPlayer(Provider.clients[i]));
            OfficerManager = new OfficerManager();
            Cooldowns = new CooldownManager();
            if (UseWhitelist)
                Whitelister = new Whitelister();
            Subscribe();
        }
        protected void InvokeOnTeamWin(ulong winner)
        {
            if (OnTeamWin != null)
                OnTeamWin.Invoke(winner);
        }
        protected abstract void EventLoopAction();
        private IEnumerator<WaitForSeconds> EventLoop()
        {
            while (!isPendingCancel)
            {
                yield return new WaitForSeconds(EventLoopSpeed);
                DateTime start = DateTime.Now;
                for (int i = 0; i < Provider.clients.Count; i++)
                {
                    try
                    {
                        if (Provider.clients[i].player.transform == null)
                        {
                            F.Log($"Kicking {F.GetPlayerOriginalNames(Provider.clients[i]).PlayerName} ({Provider.clients[i].playerID.steamID.m_SteamID}) for null transform.", ConsoleColor.Cyan);
                            Provider.kick(Provider.clients[i].playerID.steamID,
                                $"Your character is bugged, which messes up our zone plugin. Rejoin or contact a Director if this continues. (discord.gg/{UCWarfare.Config.DiscordInviteCode}).");
                        }
                    }
                    catch (NullReferenceException)
                    {
                        F.Log($"Kicking {F.GetPlayerOriginalNames(Provider.clients[i]).PlayerName} ({Provider.clients[i].playerID.steamID.m_SteamID}) for null transform.", ConsoleColor.Cyan);
                        Provider.kick(Provider.clients[i].playerID.steamID,
                            $"Your character is bugged, which messes up our zone plugin. Rejoin or contact a Director if this continues. (discord.gg/{UCWarfare.Config.DiscordInviteCode}).");
                    }
                }
                try
                {
                    EventLoopAction();
                }
                catch (Exception ex)
                {
                    F.LogError("Error in " + Name + " gamemode in the event loop:");
                    F.LogError(ex);
                }
                if (UCWarfare.I.CoroutineTiming)
                    F.Log(Name + " Eventloop: " + (DateTime.Now - start).TotalMilliseconds.ToString(Data.Locale) + "ms.");
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
        public virtual void StartNextGame(bool onLoad = false)
        {
            if (!onLoad)
            {
                Type nextMode = GetNextGamemode();
                if (this.GetType() != nextMode)
                {
                    this.Dispose();
                    Gamemode gamemode = UCWarfare.I.gameObject.AddComponent(nextMode) as Gamemode;
                    if (gamemode != null)
                    {
                        gamemode.Init();
                        gamemode.OnLevelLoaded();
                        //F.Broadcast("force_loaded_gamemode", Data.Gamemode.DisplayName);
                        for (int i = 0; i < Provider.clients.Count; i++)
                            gamemode.OnPlayerJoined(UCPlayer.FromSteamPlayer(Provider.clients[i]), true);
                        F.Log("Chosen new gameode " + gamemode.DisplayName, ConsoleColor.DarkCyan);
                        Data.Gamemode = gamemode;
                        _state = EState.DISCARD;
                        Destroy(this);
                        return;
                    }
                }
            }
            F.Log($"Loading new {DisplayName} game.", ConsoleColor.Cyan);
            _state = EState.ACTIVE;
            _gameID = DateTime.Now.Ticks;
            for (int i = 0; i < Provider.clients.Count; i++)
                if (PlayerManager.HasSave(Provider.clients[i].playerID.steamID.m_SteamID, out PlayerSave save)) save.LastGame = _gameID;
            PlayerManager.ApplyToOnline();
            AnnounceMode();
        }
        private void AnnounceMode()
        {
            foreach (UCPlayer player in PlayerManager.OnlinePlayers)
            {
                ToastMessage.QueueMessage(player, "", DisplayName, ToastMessageSeverity.BIG);
            }
        }
        public virtual void OnGroupChanged(SteamPlayer player, ulong oldGroup, ulong newGroup, ulong oldteam, ulong newteam)
        { }
        public virtual void OnPlayerJoined(UCPlayer player, bool wasAlreadyOnline = false)
        {

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
                F.LogWarning("Exception when finding gamemode: \"" + name + '\"');
                F.LogError(ex, ConsoleColor.Yellow);
                return null;
            }
        }
        public virtual void Subscribe()
        { }
        public virtual void Unsubscribe()
        { }
        public virtual void Dispose()
        {
            Unsubscribe();
            CancelCoroutine();
            Whitelister?.Dispose();
        }
        public void ReplaceBarricadesAndStructures()
        {
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
                            F.LogError($"Failed to clear barricades/structures of region ({x}, {y}):");
                            F.LogError(ex);
                        }
                    }
                }
                RequestSigns.DropAllSigns();
                StructureSaver.DropAllStructures();
            }
            catch (Exception ex)
            {
                F.LogError($"Failed to clear barricades/structures:");
                F.LogError(ex);
            }
        }
        public static void ReadGamemodes()
        {
            if (GAMEMODE_ROTATION.Count > 0) GAMEMODE_ROTATION.Clear();
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
}

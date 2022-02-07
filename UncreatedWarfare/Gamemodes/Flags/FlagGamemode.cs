using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uncreated.Warfare.Gamemodes.Interfaces;

namespace Uncreated.Warfare.Gamemodes.Flags
{
    public abstract class FlagGamemode : TeamGamemode, IFlagRotation
    {
        protected List<Flag> _rotation = new List<Flag>();
        public List<Flag> Rotation { get => _rotation; }
        protected List<Flag> _allFlags = new List<Flag>();
        public List<Flag> LoadedFlags { get => _allFlags; }
        public Dictionary<ulong, int> _onFlag = new Dictionary<ulong, int>();
        public Dictionary<ulong, int> OnFlag { get => _onFlag; }
        protected int _counter;
        protected int _counter2;
        protected abstract bool TimeToCheck();
        public FlagGamemode(string Name, float EventLoopSpeed) : base(Name, EventLoopSpeed)
        { }
        public override void Init()
        {
            base.Init();
            this._state = EState.PAUSED;
        }
        protected override void EventLoopAction()
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            bool ttc = TimeToCheck();

            for (int i = 0; i < _rotation.Count; i++)
            {
                if (_rotation[i] == null) continue;
                _rotation[i].GetUpdatedPlayers(out List<Player> newPlayers, out List<Player> departingPlayers);
                foreach (Player player in departingPlayers)
                    RemovePlayerFromFlag(player, _rotation[i]);
                foreach (Player player in newPlayers)
                    AddPlayerOnFlag(player, _rotation[i]);
            }
            if (ttc)
            {
                EvaluatePoints();
                if (EnableAMC)
                    CheckPlayersAMC();
                OnEvaluate();
                Teams.TeamManager.EvaluateBases();
            }
        }
        protected void ConvertFlags()
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            List<FlagData> data = JSONMethods.LoadFlags();
            _allFlags.Clear();
            _allFlags.Capacity = data.Count;
            for (int i = 0; i < data.Count; i++)
                _allFlags.Add(new Flag(data[i], this) { index = -1 });
            _allFlags.Sort((Flag a, Flag b) => a.ID.CompareTo(b.ID));
        }
        public virtual void OnEvaluate()
        { }
        public void LoadAllFlags()
        {
            ConvertFlags();
            L.Log("Loaded " + _allFlags.Count.ToString(Data.Locale) + " flags into memory and cleared any existing old flags.", ConsoleColor.Magenta);
        }
        public virtual void PrintFlagRotation()
        {
            StringBuilder sb = new StringBuilder(_rotation.Count.ToString(Data.Locale) + " flags:\n");
            for (int i = 0; i < _rotation.Count; i++)
            {
                sb.Append(i.ToString(Data.Locale) + ") " + _rotation[i].Name + '\n');
            }
            L.Log(sb.ToString(), ConsoleColor.Green);
        }
        public abstract void LoadRotation();
        protected virtual void EvaluatePoints()
        {
            if (_state == EState.ACTIVE)
                for (int i = 0; i < _rotation.Count; i++)
                    _rotation[i].EvaluatePoints();
        }
        public virtual void InitFlag(Flag flag)
        {
            flag.OnPlayerEntered += PlayerEnteredFlagRadius;
            flag.OnPlayerLeft += PlayerLeftFlagRadius;
            flag.OnOwnerChanged += FlagOwnerChanged;
            flag.OnPointsChanged += FlagPointsChanged;
        }
        public virtual void ResetFlags()
        {
            foreach (Flag flag in _rotation)
            {
                flag.OnPlayerEntered -= PlayerEnteredFlagRadius;
                flag.OnPlayerLeft -= PlayerLeftFlagRadius;
                flag.OnOwnerChanged -= FlagOwnerChanged;
                flag.OnPointsChanged -= FlagPointsChanged;
                flag.ResetFlag();
            }
            _rotation.Clear();
        }
        protected virtual void RemovePlayerFromFlag(Player player, Flag flag)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            if (_onFlag.ContainsKey(player.channel.owner.playerID.steamID.m_SteamID) && _onFlag[player.channel.owner.playerID.steamID.m_SteamID] == flag.ID)
            {
                _onFlag.Remove(player.channel.owner.playerID.steamID.m_SteamID);
                flag.ExitPlayer(player);
            }
        }
        public virtual void AddPlayerOnFlag(Player player, Flag flag)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            if (_onFlag.ContainsKey(player.channel.owner.playerID.steamID.m_SteamID))
            {
                if (_onFlag[player.channel.owner.playerID.steamID.m_SteamID] != flag.ID)
                {
                    Flag oldFlag = _rotation.FirstOrDefault(f => f.ID == _onFlag[player.channel.owner.playerID.steamID.m_SteamID]);
                    if (oldFlag == default(Flag)) _onFlag.Remove(player.channel.owner.playerID.steamID.m_SteamID);
                    else RemovePlayerFromFlag(player, oldFlag); // remove the player from their old flag first in the case of teleporting from one flag to another.
                    _onFlag.Add(player.channel.owner.playerID.steamID.m_SteamID, flag.ID);
                }
            }
            else _onFlag.Add(player.channel.owner.playerID.steamID.m_SteamID, flag.ID);
            flag.EnterPlayer(player);
        }
        protected abstract void PlayerEnteredFlagRadius(Flag flag, Player player);
        protected abstract void PlayerLeftFlagRadius(Flag flag, Player player);
        protected abstract void FlagOwnerChanged(ulong OldOwner, ulong NewOwner, Flag flag);
        protected abstract void FlagPointsChanged(float NewPoints, float OldPoints, Flag flag);
        public override void OnLevelLoaded()
        {
            LoadAllFlags();
            base.OnLevelLoaded();
        }
        public override void Dispose()
        {
            ResetFlags();
            _onFlag.Clear();
            _rotation.Clear();
            _counter = 0;
            _counter2 = 0;
            base.Dispose();
        }
    }
}

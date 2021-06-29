using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Flags
{
    public class PlayerEventArgs : EventArgs { public Player player; }
    public class DiscoveredEventArgs : EventArgs { public ulong Team; }
    public class Flag : IDisposable
    {
        public int index = -1;
        public const int MaxPoints = 64;
        public Zone ZoneData { get; private set; }
        public FlagManager Manager { get; private set; }
        public int Level { get => _level; }
        private readonly int _level;
        public int ObjectivePlayerCount 
        { 
            get
            {
                if (T1Obj) return Team1TotalPlayers;
                else if (T2Obj) return Team2TotalPlayers;
                else return 0;
            } 
        }
        public Vector3 Position
        {
            get => _position;
            set
            {
                _position = value;
                _x = _position.x;
                _y = _position.y;
                _z = _position.z;
                _position2d = new Vector2(_x, _z);
            }
        }
        private Vector3 _position;
        public Vector2 Position2D
        {
            get => _position2d;
            set
            {
                _position2d = value;
                _x = _position2d.x;
                _z = _position2d.y;
                _position = new Vector3(_x, _y, _z);
            }
        }
        private Vector2 _position2d;
        public int ID { get => _id; }
        private readonly int _id;
        public string Name { get => _name; }
        private readonly string _name;
        public float X
        {
            get => _x;
            set
            {
                _x = value;
                _position = new Vector3(_x, _y, _z);
                _position2d = new Vector2(_x, _z);
            }
        }
        public float Y
        {
            get => _y;
            set
            {
                _y = value;
                _position = new Vector3(_x, _y, _z);
            }
        }
        public float Z
        {
            get => _z;
            set
            {
                _z = value;
                _position = new Vector3(_x, _y, _z);
                _position2d = new Vector2(_x, _z);
            }
        }
        private float _x;
        private float _y;
        private float _z;
        public string ColorString { get => _color; set => _color = value; }
        private string _color;
        public Color Color { get => _color.Hex(); }
        public Color TeamSpecificColor
        {
            get
            {
                if (_owner == 1)
                    return UCWarfare.GetColor("team_1_color");
                else if (_owner == 2)
                    return UCWarfare.GetColor("team_2_color");
                else return UCWarfare.GetColor("neutral_color");
            }
        }
        public string TeamSpecificHexColor
        {
            get
            {
                if (_owner == 1)
                    return UCWarfare.GetColorHex("team_1_color");
                else if (_owner == 2)
                    return UCWarfare.GetColorHex("team_2_color");
                else return UCWarfare.GetColorHex("neutral_color");
            }
        }
        public async Task ResetFlag()
        {
            await SetOwner(0, false);
            _points = 0;
            Hide(1);
            Hide(2);
            SynchronizationContext rtn = await ThreadTool.SwitchToGameThread();
            if(OnReset != null)
                OnReset.Invoke(this, EventArgs.Empty);
            await rtn;
        }
        public void Dispose()
        {
            OnDisposed?.Invoke(this, EventArgs.Empty);
            GC.SuppressFinalize(this);
        }
        private ulong _owner = 0;
        public ulong Owner {
            get => _owner;
        }
        public async Task SetOwner(ulong value, bool invokeEvent = true)
        {
            if (_owner != value)
            {
                ulong oldowner = _owner;
                _owner = value;
                if(invokeEvent)
                    await OnOwnerChanged?.Invoke(oldowner, _owner, this);
            }
        }
        public void SetOwnerNoEventInvocation(ulong newOwner)
        {
            _owner = newOwner;
        }
        public float SizeX { get => _sizeX; set => _sizeX = value; }
        public float SizeZ { get => _sizeZ; set => _sizeZ = value; }
        private float _sizeX;
        private float _sizeZ;
        public List<Player> PlayersOnFlagTeam1;
        public int Team1TotalPlayers;
        public List<Player> PlayersOnFlagTeam2;
        public int Team2TotalPlayers;
        public void RecalcCappers(bool RecalcOnFlag = false) => RecalcCappers(Provider.clients, RecalcOnFlag);
        public void RecalcCappers(List<SteamPlayer> OnlinePlayers, bool RecalcOnFlag = true)
        {
            if (RecalcOnFlag)
            {
                PlayersOnFlag.Clear();
                foreach (SteamPlayer player in OnlinePlayers.Where(p => PlayerInRange(p)))
                    PlayersOnFlag.Add(player.player);
            }
            PlayersOnFlagTeam1 = PlayersOnFlag.Where(player => player.quests.groupID.m_SteamID == TeamManager.Team1ID).ToList();
            Team1TotalPlayers = PlayersOnFlagTeam1.Count;
            PlayersOnFlagTeam2 = PlayersOnFlag.Where(player => player.quests.groupID.m_SteamID == TeamManager.Team2ID).ToList();
            Team2TotalPlayers = PlayersOnFlagTeam2.Count;
        }
        /// <param name="NewPlayers">Players that have entered the flag since last check.</param>
        /// <returns>Players that have left the flag since last check.</returns>
        public List<Player> GetUpdatedPlayers(List<SteamPlayer> OnlinePlayers, out List<Player> NewPlayers)
        {
            List<Player> OldPlayers = PlayersOnFlag.ToList();
            RecalcCappers(OnlinePlayers, true);
            // gets the players that aren't in oldplayers
            NewPlayers = PlayersOnFlag.Where(p => !OldPlayers.Exists(p2 => p.channel.owner.playerID.steamID.m_SteamID == p2.channel.owner.playerID.steamID.m_SteamID)).ToList();
            return OldPlayers.Where(p => !PlayersOnFlag.Exists(p2 => p.channel.owner.playerID.steamID.m_SteamID == p2.channel.owner.playerID.steamID.m_SteamID)).ToList();
        }
        private int _points;
        public int LastDeltaPoints { get; protected set; }
        public int Points
        {
            get => _points;
        }
        public async Task SetPoints(int value)
        {
            int OldPoints = _points;
            if (value > MaxPoints) _points = MaxPoints;
            else if (value < -MaxPoints) _points = -MaxPoints;
            else _points = value;
            if (OldPoints != _points)
            {
                LastDeltaPoints = _points - OldPoints;
                await OnPointsChanged?.Invoke(_points, OldPoints, this);
            }
        }
        public event EventHandler<PlayerEventArgs> OnPlayerEntered;
        public event EventHandler<PlayerEventArgs> OnPlayerLeft;
        public delegate Task PointsChangedDelegate(int NewPoints, int OldPoints, Flag flag);
        public event PointsChangedDelegate OnPointsChanged;
        public delegate Task OwnerChangedDelegate(ulong OldOwner, ulong NewOwner, Flag flag);
        public event OwnerChangedDelegate OnOwnerChanged;
        public event EventHandler<DiscoveredEventArgs> OnDiscovered;
        public event EventHandler<DiscoveredEventArgs> OnHidden;
        public event EventHandler OnDisposed;
        public event EventHandler OnReset;
        public List<Player> PlayersOnFlag { get; private set; }
        public Flag(FlagData data, FlagManager manager)
        {
            this.Manager = manager;
            this._id = data.id;
            this._x = data.x;
            this._y = data.y;
            this._position2d = data.Position2D;
            this._level = data.level;
            this.LastDeltaPoints = 0;
            this._name = data.name;
            this._color = data.color;
            this._owner = 0;
            PlayersOnFlag = new List<Player>();
            this.ZoneData = ComplexifyZone(data);
        }
        public static Zone ComplexifyZone(FlagData data)
        {
            switch (data.zone.type)
            {
                case "rectangle":
                    return new RectZone(data.Position2D, data.zone, data.use_map_size_multiplier, data.name);
                case "circle":
                    return new CircleZone(data.Position2D, data.zone, data.use_map_size_multiplier, data.name);
                case "polygon":
                    return new PolygonZone(data.Position2D, data.zone, data.use_map_size_multiplier, data.name);
                default:
                    F.LogError("Invalid zone type \"" + data.zone.type + "\" at flag ID: " + data.id.ToString(Data.Locale) + ", name: " + data.name);
                    return new RectZone(data.Position2D, new ZoneData("circle", "50"), data.use_map_size_multiplier, data.name);
            }
        }
        public bool IsFriendly(UnturnedPlayer player) => IsFriendly(player.Player.quests.groupID.m_SteamID);
        public bool IsFriendly(SteamPlayer player) => IsFriendly(player.player.quests.groupID.m_SteamID);
        public bool IsFriendly(Player player) => IsFriendly(player.quests.groupID.m_SteamID);
        public bool IsFriendly(CSteamID groupID) => IsFriendly(groupID.m_SteamID);
        public bool IsFriendly(ulong groupID) => groupID == _owner;
        public bool PlayerInRange(Vector3 PlayerPosition) => ZoneData.IsInside(PlayerPosition);
        public bool PlayerInRange(Vector2 PlayerPosition) => ZoneData.IsInside(PlayerPosition);
        public bool PlayerInRange(UnturnedPlayer player) => PlayerInRange(player.Position);
        public bool PlayerInRange(SteamPlayer player) => PlayerInRange(player.player.transform.position);
        public bool PlayerInRange(Player player) => PlayerInRange(player.transform.position);
        public void EnterPlayer(Player player)
        {
            OnPlayerEntered?.Invoke(this, new PlayerEventArgs { player = player });
            if (!PlayersOnFlag.Exists(p => p.channel.owner.playerID.steamID.m_SteamID == player.channel.owner.playerID.steamID.m_SteamID)) PlayersOnFlag.Add(player);
        }
        public void ExitPlayer(Player player)
        {
            OnPlayerLeft?.Invoke(this, new PlayerEventArgs { player = player });
            PlayersOnFlag.Remove(player);
        }
        public bool IsNeutral() => _points == 0;
        public async Task CapT1(int amount)
        {
            await SetPoints(Points + amount);
            if (Points >= MaxPoints)
                await SetOwner(1);
        }
        public async Task CapT1()
        {
            await SetPoints(MaxPoints);
            await SetOwner(1);
        }
        public async Task CapT2(int amount)
        {
            await SetPoints(Points - amount);
            if (Points <= -MaxPoints)
                await SetOwner(2);
        }
        public async Task CapT2()
        {
            await SetPoints(-MaxPoints);
            await SetOwner(2);
        }
        public async Task Cap(ulong team, int amount)
        {
            if (team == 1) await CapT1(amount);
            else if (team == 2) await CapT2(amount);
        }
        public async Task Cap(ulong team)
        {
            if (team == 1) await CapT1();
            else if (team == 2) await CapT2();
        }
        public bool T1Obj { get => ID == Data.FlagManager.ObjectiveTeam1.ID; }
        public bool T2Obj { get => ID == Data.FlagManager.ObjectiveTeam2.ID; }
        public bool IsObj(ulong team)
        {
            if (team == 1) return T1Obj;
            else if (team == 2) return T2Obj;
            else return false;
        }
        public bool IsAnObj { get => T1Obj || T2Obj; }
        public bool DiscoveredT1 {
            get => _discovered1;
            protected set
            {
                if (value == true && _discovered1 == false)
                {
                    OnDiscovered?.Invoke(this, new DiscoveredEventArgs { Team = 1 });
                    _discovered1 = true;
                    return;
                }
                if(value == false && _discovered1 == true)
                {
                    OnHidden?.Invoke(this, new DiscoveredEventArgs { Team = 1 });
                    _discovered1 = false;
                    return;
                }
            } 
        }
        public bool DiscoveredT2
        {
            get => _discovered2;
            protected set
            {
                if (value == true && _discovered2 == false)
                {
                    OnDiscovered?.Invoke(this, new DiscoveredEventArgs { Team = 2 });
                    _discovered2 = true;
                    return;
                }
                if (value == false && _discovered2 == true)
                {
                    OnHidden?.Invoke(this, new DiscoveredEventArgs { Team = 2 });
                    _discovered2 = false;
                    return;
                }
            }
        }
        protected bool _discovered1;
        protected bool _discovered2;
        public bool Discovered(ulong team)
        {
            if (!UCWarfare.Config.FlagSettings.HideUnknownFlags) return true;
            if (team == 1) return _discovered1;
            else if (team == 2) return _discovered2;
            else return false;
        }
        public bool Hidden(ulong team) => !Discovered(team);
        public void Discover(ulong team)
        {
            if (team == 1) DiscoveredT1 = true;
            else if (team == 2) DiscoveredT2 = true;
        }
        public void Hide(ulong team)
        {
            if (team == 1) DiscoveredT1 = false;
            else if (team == 2) DiscoveredT2 = false;
        }
        public ulong WhosObj()
        {
            if (T1Obj && T2Obj) return 3;
            else if (T1Obj) return 1;
            else if (T2Obj) return 2;
            else return 0;
        }
        public bool IsContested(out ulong winner)
        {
            if ((T1Obj && T2Obj) || (T1Obj && Owner == 2) || (T2Obj && Owner == 1)) // must be objective for both teams
            {
                if (Team1TotalPlayers == 0 && Team2TotalPlayers == 0)
                {
                    winner = 0;
                    return false;
                }
                else if (Team1TotalPlayers == Team2TotalPlayers)
                {
                    winner = 0;
                }
                else if (Team1TotalPlayers == 0 && Team2TotalPlayers > 0)
                {
                    winner = 2;
                }
                else if (Team2TotalPlayers == 0 && Team1TotalPlayers > 0)
                {
                    winner = 1;
                }
                else if (Team1TotalPlayers > Team2TotalPlayers)
                {
                    if (Team1TotalPlayers - UCWarfare.Config.FlagSettings.RequiredPlayerDifferenceToCapture >= Team2TotalPlayers)
                    {
                        winner = 1;
                    }
                    else
                    {
                        winner = 0;
                    }
                }
                else
                {
                    if (Team2TotalPlayers - UCWarfare.Config.FlagSettings.RequiredPlayerDifferenceToCapture >= Team1TotalPlayers)
                    {
                        winner = 2;
                    }
                    else
                    {
                        winner = 0;
                    }
                }
                return winner == 0;
            }
            else
            {
                if (ObjectivePlayerCount == 0) winner = 0;
                else winner = WhosObj();
                if (!IsObj(winner)) winner = 0;
                return false;
            }
        }
        public async Task EvaluatePoints(bool overrideInactiveCheck = false)
        {
            if (Manager.State == EState.ACTIVE || overrideInactiveCheck)
            {
                if (IsAnObj)
                {
                    if (!IsContested(out ulong winner))
                    {
                        if (IsObj(winner))
                        {
                            if (winner == 1 || winner == 2)
                            {
                                await Cap(winner, 1);
                            }
                        }
                    }
                    else
                    {
                        // invoke points updated method to show contested.
                        this.LastDeltaPoints = 0;
                        await OnPointsChanged?.Invoke(_points, _points, this);
                    }
                }
            }
        }
    }
}

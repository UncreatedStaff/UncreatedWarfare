using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UncreatedWarfare.Teams;
using UnityEngine;

namespace UncreatedWarfare.Flags
{
    public class PlayerEventArgs : EventArgs { public Player player; }
    public class CaptureChangeEventArgs : EventArgs { public int NewPoints; public int OldPoints; }
    public class OwnerChangeEventArgs : EventArgs { public Team OldOwner; public Team NewOwner; }
    public class Flag
    {
        public const int MaxPoints = 64;
        public Zone ZoneData { get; private set; }
        public Vector3 Position { 
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
        private int _id;
        public string Name { get => _name; }
        private string _name;
        public float X { 
            get => _x; 
            set
            {
                _x = value;
                _position = new Vector3(_x, _y, _z);
                _position2d = new Vector2(_x, _z);
            }
        }
        public float Y { 
            get => _y;
            set
            {
                _y = value;
                _position = new Vector3(_x, _y, _z);
            }
        }
        public float Z { 
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
        public Color TeamSpecificColor { 
            get
            {
                if (_owner.ID == UCWarfare.I.T1.ID)
                    return UCWarfare.I.Colors["team_1_color"];
                else if (_owner.ID == UCWarfare.I.T2.ID)
                    return UCWarfare.I.Colors["team_2_color"];
                else return UCWarfare.I.Colors["neutral_color"];
            } 
        }
        private Team _owner;
        public Team Owner { get => _owner; set => _owner = value; }
        public float SizeX { get => _sizeX; set => _sizeX = value; }
        public float SizeZ { get => _sizeZ; set => _sizeZ = value; }
        private float _sizeX;
        private float _sizeZ;
        public List<Player> PlayersOnFlagTeam1;
        public int Team1TotalPlayers;
        public List<Player> PlayersOnFlagTeam2;
        public int Team2TotalPlayers;
        public void RecalcCappers(bool RecalcOnFlag = false) => RecalcCappers(Provider.clients, RecalcOnFlag);
        public void RecalcCappers(List<SteamPlayer> OnlinePlayers, bool RecalcOnFlag = false)
        {
            if(RecalcOnFlag)
            {
                PlayersOnFlag.Clear();
                foreach(SteamPlayer player in OnlinePlayers.Where(p => PlayerInRange(p)))
                {
                    PlayersOnFlag.Add(player.player);
                }
            }
            PlayersOnFlagTeam1 = PlayersOnFlag.Where(player => player.quests.groupID.m_SteamID == UCWarfare.I.T1.ID).ToList();
            Team1TotalPlayers = PlayersOnFlagTeam1.Count;
            PlayersOnFlagTeam2 = PlayersOnFlag.Where(player => player.quests.groupID.m_SteamID == UCWarfare.I.T2.ID).ToList();
            Team2TotalPlayers = PlayersOnFlagTeam2.Count;
        }
        /// <param name="NewPlayers">New Players</param>
        /// <returns>Old Players</returns>
        public List<Player> GetUpdatedPlayers(List<SteamPlayer> OnlinePlayers, out List<Player> NewPlayers)
        {
            List<Player> OldPlayers = PlayersOnFlag.ToList();
            RecalcCappers(OnlinePlayers, true);
            NewPlayers = PlayersOnFlag.Where(p => !OldPlayers.Exists(p2 => p.channel.owner.playerID.steamID.m_SteamID == p2.channel.owner.playerID.steamID.m_SteamID)).ToList();
            return OldPlayers.Where(p => !PlayersOnFlag.Exists(p2 => p.channel.owner.playerID.steamID.m_SteamID == p2.channel.owner.playerID.steamID.m_SteamID)).ToList();
        }
        public Team FullOwner { 
            get
            {
                if (_points >= MaxPoints)
                    return UCWarfare.Instance.T1;
                else if (_points <= MaxPoints * -1)
                    return UCWarfare.Instance.T2;
                else return Team.Neutral;
            } 
            set
            {
                if (value == null)
                {
                    CommandWindow.LogError($"Tried to set owner of flag {_id} to a null team.");
                    return;
                }
                if (UCWarfare.Instance.T1.ID == value.ID)
                    Points = MaxPoints;
                else if (UCWarfare.Instance.T2.ID == value.ID)
                    Points = MaxPoints * -1;
                else CommandWindow.LogError($"Tried to set owner of flag {_id} to an invalid team: {value.ID}.");
            }
        }
        private int _points;
        public int Points { 
            get => _points; 
            set
            {
                Team OldOwner;
                int OldPoints = _points;
                if (_points >= MaxPoints)
                    OldOwner = UCWarfare.Instance.T1;
                else if (_points <= MaxPoints * -1)
                    OldOwner = UCWarfare.Instance.T2;
                else OldOwner = Team.Neutral;
                if (value > MaxPoints) _points = MaxPoints;
                else if (value < MaxPoints * -1) _points = MaxPoints * -1;
                else _points = value;
                if(OldPoints != _points)
                {
                    OnPointsChanged?.Invoke(this, new CaptureChangeEventArgs { NewPoints = _points, OldPoints = OldPoints });
                    Team NewOwner;
                    if (_points >= MaxPoints)
                        NewOwner = UCWarfare.Instance.T1;
                    else if (_points <= MaxPoints * -1)
                        NewOwner = UCWarfare.Instance.T2;
                    else NewOwner = Team.Neutral;
                    if (OldOwner.ID != NewOwner.ID) OnOwnerChanged?.Invoke(this, new OwnerChangeEventArgs { OldOwner = OldOwner, NewOwner = NewOwner });
                }
            }
        }
        public event EventHandler<PlayerEventArgs> OnPlayerEntered;
        public event EventHandler<PlayerEventArgs> OnPlayerLeft;
        public event EventHandler<CaptureChangeEventArgs> OnPointsChanged;
        public event EventHandler<OwnerChangeEventArgs> OnOwnerChanged;
        public List<Player> PlayersOnFlag { get; private set; }
        public Flag(FlagData data)
        {
            this._id = data.id;
            this._x = data.x;
            this._y = data.y;
            this._position2d = data.Position2D;
            this._name = data.name;
            this._color = data.color;
            this._owner = Team.Neutral;
            PlayersOnFlag = new List<Player>();
            switch(data.zone.type)
            {
                case "rectangle":
                    ZoneData = new RectZone(data.Position2D, data.zone);
                    break;
                case "circle":
                    ZoneData = new CircleZone(data.Position2D, data.zone);
                    break;
                case "polygon":
                    ZoneData = new PolygonZone(data.Position2D, data.zone);
                    break;
                default:
                    ZoneData = new RectZone(data.Position2D, new ZoneData("rectangle", "100, 100"));
                    break;
            }
            CommandWindow.LogWarning($"{_id}, {X}, {Y}, {_name}, {_color}, {TeamSpecificColor}, {_owner}, {ZoneData.SucessfullyParsed}, {ZoneData.GetType()}, {data.zone.data}, {data.zone.type}.");
        }
        public bool PlayerInRange(Vector3 PlayerPosition) => ZoneData.IsInside(PlayerPosition);
        public bool PlayerInRange(Vector2 PlayerPosition) => ZoneData.IsInside(PlayerPosition);
        public bool PlayerInRange(UnturnedPlayer player) => PlayerInRange(player.Position);
        public bool PlayerInRange(SteamPlayer player) => PlayerInRange(player.player.transform.position);
        public bool PlayerInRange(Player player) => PlayerInRange(player.transform.position);
        public void EnterPlayer(Player player)
        {
            OnPlayerEntered?.Invoke(this, new PlayerEventArgs { player = player });
            if(!PlayersOnFlag.Exists(p => p.channel.owner.playerID.steamID.m_SteamID == player.channel.owner.playerID.steamID.m_SteamID)) PlayersOnFlag.Add(player);
        }
        public void ExitPlayer(Player player)
        {
            OnPlayerLeft?.Invoke(this, new PlayerEventArgs { player = player });
            PlayersOnFlag.Remove(player);
        }
        public bool IsFriendly(ulong GroupID) => GroupID == FullOwner.ID;
        public bool IsFriendly(Player player) => IsFriendly(player.quests.groupID.m_SteamID);
        public bool IsFriendly(SteamPlayer player) => IsFriendly(player.player.quests.groupID.m_SteamID);
        public bool IsFriendly(UnturnedPlayer player) => IsFriendly(player.Player.quests.groupID.m_SteamID);
        public bool IsNeutral() => FullOwner.ID == Team.Neutral.ID;
        public void CapT1(int amount = 1)
        {
            Points += amount;
        }
        public void CapT2(int amount = 1)
        {
            Points -= amount;
        }
        public bool T1Obj() => ID == UCWarfare.I.FlagManager.ObjectiveTeam1.ID;
        public bool T2Obj() => ID == UCWarfare.I.FlagManager.ObjectiveTeam2.ID;
        public void EvaluatePoints(List<SteamPlayer> PlayersOnFlag)
        {
            if(T1Obj())
            {
                if(Team1TotalPlayers - UCWarfare.Config.FlagSettings.RequiredPlayerDifferenceToCapture >= Team2TotalPlayers || (Team1TotalPlayers > 0 && Team2TotalPlayers == 0))
                {
                    CapT1();
                } else if (
                    (Team2TotalPlayers - UCWarfare.Config.FlagSettings.RequiredPlayerDifferenceToCapture >= Team1TotalPlayers || 
                    (Team2TotalPlayers > 0 && Team1TotalPlayers == 0)) &&
                    Owner.ID == UCWarfare.Config.Team2ID && _points > -1 * MaxPoints)
                {
                    CapT2();
                }
            }
            if(T2Obj())
            {
                if(Team2TotalPlayers - UCWarfare.Config.FlagSettings.RequiredPlayerDifferenceToCapture >= Team2TotalPlayers || (Team2TotalPlayers > 0 && Team1TotalPlayers == 0))
                {
                    CapT2();
                } else if (
                    (Team1TotalPlayers - UCWarfare.Config.FlagSettings.RequiredPlayerDifferenceToCapture >= Team2TotalPlayers ||
                    (Team1TotalPlayers > 0 && Team2TotalPlayers == 0)) && 
                    Owner.ID == UCWarfare.Config.Team1ID && _points < MaxPoints)
                {
                    CapT1();
                }
            }
        }
    }
}

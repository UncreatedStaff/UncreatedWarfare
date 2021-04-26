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
    public class CaptureChangeEventArgs : EventArgs { public int capture; }
    public class OwnerChangeEventArgs : EventArgs { public Team OldOwner; public Team NewOwner; }
    public class Flag
    {
        public const int MaxPoints = 64;
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
        public float SizeX { get => _sizeX; set => _sizeX = value; }
        public float SizeZ { get => _sizeZ; set => _sizeZ = value; }
        private float _sizeX;
        private float _sizeZ;
        public Team Owner { 
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
                    _points = MaxPoints;
                else if (UCWarfare.Instance.T2.ID == value.ID)
                    _points = MaxPoints * -1;
                else CommandWindow.LogError($"Tried to set owner of flag {_id} to an invalid team: {value.ID}.");
            }
        }
        private int _points;
        public int Points { 
            get => _points; 
            set
            {
                Team OldOwner;
                if (_points >= MaxPoints)
                    OldOwner = UCWarfare.Instance.T1;
                else if (_points <= MaxPoints * -1)
                    OldOwner = UCWarfare.Instance.T2;
                else OldOwner = Team.Neutral;
                _points = value;
                OnPointsChanged?.Invoke(this, new CaptureChangeEventArgs { capture = _points });
                Team NewOwner;
                if (_points >= MaxPoints)
                    NewOwner = UCWarfare.Instance.T1;
                else if (_points <= MaxPoints * -1)
                    NewOwner = UCWarfare.Instance.T2;
                else NewOwner = Team.Neutral;
                if (OldOwner.ID != NewOwner.ID) OnOwnerChanged?.Invoke(this, new OwnerChangeEventArgs { OldOwner = OldOwner, NewOwner = NewOwner });
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
            this._z = data.z;
            this._position = data.Position;
            this._position2d = data.Position2D;
            this._name = data.name;
            this._color = data.color;
            this._sizeX = data.sizeX;
            this._sizeZ = data.sizeY;
        }
        public bool PlayerInRange(Vector3 PlayerPosition) => PlayerPosition.x > _x - _sizeX / 2 && PlayerPosition.x < _x + _sizeX / 2 && PlayerPosition.z > _z - _sizeZ / 2 && PlayerPosition.z < _z + _sizeZ / 2;
        public bool PlayerInRange(Vector2 PlayerPosition) => PlayerPosition.x > _x - _sizeX / 2 && PlayerPosition.x < _x + _sizeX / 2 && PlayerPosition.y > _z - _sizeZ / 2 && PlayerPosition.y < _x + _sizeZ / 2;
        public bool PlayerInRange(UnturnedPlayer player) => PlayerInRange(player.Position);
        public bool PlayerInRange(SteamPlayer player) => PlayerInRange(player.player.transform.position);
        public bool PlayerInRange(Player player) => PlayerInRange(player.transform.position);
        public void EnterPlayer(Player player)
        {
            OnPlayerEntered?.Invoke(this, new PlayerEventArgs { player = player });
            PlayersOnFlag.Add(player);
        }
        public void ExitPlayer(Player player)
        {
            OnPlayerLeft?.Invoke(this, new PlayerEventArgs { player = player });
            PlayersOnFlag.Remove(player);
        }
    }
}

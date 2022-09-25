using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Traits.Buffs;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Flags;

public delegate void DiscoveryDelegate(ulong team);
public class Flag : IDisposable, ITranslationArgument, IObjective
{
    public delegate void EvaluatePointsDelegate(Flag flag, bool overrideInactiveCheck = false);
    public delegate bool IsContestedDelegate(Flag flag, out ulong winner);
    public int index = -1;
    public const float MAX_POINTS = 64f;
    public AdjacentFlagData[] Adjacencies;
    public static float CaptureMultiplier = 1.0f;
    private Vector3 _position;
    private Vector2 _position2d;
    private readonly int _id;
    private readonly string _name;
    private readonly string _shortName;
    private float _x;
    private float _y;
    private float _z;
    private string _color;
    private ulong _owner = 0;
    public List<UCPlayer> PlayersOnFlagTeam1;
    public int Team1TotalPlayers;
    public EvaluatePointsDelegate? EvaluatePointsOverride = null;
    public IsContestedDelegate? IsContestedOverride = null;
    public int Team1TotalCappers;
    public List<UCPlayer> PlayersOnFlagTeam2;
    public int Team2TotalPlayers;
    public int Team2TotalCappers;
    private float _points;
    protected bool _discovered1;
    protected bool _discovered2;
    public bool HasBeenCapturedT1;
    public bool HasBeenCapturedT2;
    private ulong _lastOwner;

    public event PlayerDelegate OnPlayerEntered;
    public event PlayerDelegate OnPlayerLeft;
    public delegate void PointsChangedDelegate(float points, float previousPoints, Flag flag);
    public delegate void PlayerDelegate(Flag flag, Player player);
    public event PointsChangedDelegate OnPointsChanged;
    public delegate void OwnerChangedDelegate(ulong lastOwner, ulong newOwner, Flag flag);
    public event OwnerChangedDelegate OnOwnerChanged;
    public event DiscoveryDelegate OnDiscovered;
    public event DiscoveryDelegate OnHidden;
    public event EventHandler OnDisposed;
    public event EventHandler OnReset;

    public List<UCPlayer> PlayersOnFlag { get; private set; }
    public Zone ZoneData { get; protected set; }
    public IFlagRotation Manager { get; protected set; }
    public int ObjectivePlayerCount
    {
        get
        {
            if (T1Obj) return Team1TotalPlayers;
            else if (T2Obj) return Team2TotalPlayers;
            else return 0;
        }
    }
    public int ObjectivePlayerCountCappers
    {
        get
        {
            if (T1Obj) return Team1TotalCappers;
            else if (T2Obj) return Team2TotalCappers;
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
    public int ID => _id;
    public string Name => _name;
    public string ShortName => _shortName;
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
    public string ColorHex { get => _color; set => _color = value; }
    public Color Color => _color.Hex();
    public string TeamSpecificHexColor => TeamManager.GetTeamHexColor(_owner);
    public Color TeamSpecificColor => TeamManager.GetTeamColor(_owner);
    public ulong Owner => _owner;
    public float LastDeltaPoints { get; protected set; }
    public float Points => _points;
    public bool T1Obj { get => Manager is IFlagTeamObjectiveGamemode ctf && ctf.ObjectiveTeam1 != null && ctf.ObjectiveTeam1.ID == ID; }
    public bool T2Obj { get => Manager is IFlagTeamObjectiveGamemode ctf && ctf.ObjectiveTeam2 != null && ctf.ObjectiveTeam2.ID == ID; }
    public bool IsAnObj { get => T1Obj || T2Obj; }
    public bool DiscoveredT1
    {
        get => _discovered1;
        protected set
        {
            if (value == true && _discovered1 == false)
            {
                OnDiscovered?.Invoke(1);
                _discovered1 = true;
                return;
            }
            if (value == false && _discovered1 == true)
            {
                OnHidden?.Invoke(1);
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
                OnDiscovered?.Invoke(2);
                _discovered2 = true;
                return;
            }
            if (value == false && _discovered2 == true)
            {
                OnHidden?.Invoke(2);
                _discovered2 = false;
                return;
            }
        }
    }
    public ulong LastOwner => _lastOwner;
    public void ResetFlag()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        SetOwner(0, false);
        _points = 0;
        _lastOwner = 0;
        HasBeenCapturedT1 = false;
        HasBeenCapturedT2 = false;
        EvaluatePointsOverride = null;
        IsContestedOverride = null;
        Hide(1);
        Hide(2);
        if (OnReset != null)
            OnReset.Invoke(this, EventArgs.Empty);
        RecalcCappers();
    }
    public void Dispose()
    {
        OnDisposed?.Invoke(this, EventArgs.Empty);
        GC.SuppressFinalize(this);
    }
    public void SetOwner(ulong value, bool invokeEvent = true)
    {
        if (_owner != value)
        {
            _owner = value;
            if (invokeEvent)
            {
                OnOwnerChanged?.Invoke(_lastOwner, _owner, this);
                if (value == 1)
                    HasBeenCapturedT1 = true;
                else if (value == 2)
                    HasBeenCapturedT2 = true;
            }
            if (_owner != 0)
                _lastOwner = _owner;
        }
    }
    public void RecalcCappers()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Team1TotalPlayers = 0;
        Team1TotalCappers = 0;
        Team2TotalPlayers = 0;
        Team2TotalCappers = 0;
        PlayersOnFlag.Clear();
        PlayersOnFlagTeam1.Clear();
        PlayersOnFlagTeam2.Clear();
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
        {
            UCPlayer p = PlayerManager.OnlinePlayers[i];
            if (p.Player.life.isDead) continue;
            if (PlayerInRange(p.Player.transform.position))
            {
                PlayersOnFlag.Add(p);
                ulong team = p.GetTeam();
                if (team == 1)
                {
                    PlayersOnFlagTeam1.Add(p);
                    Team1TotalPlayers++;
                    if (Manager.AllowPassengersToCapture || p.Player.movement.getVehicle() == null)
                        Team1TotalCappers++;
                }
                else if (team == 2)
                {
                    PlayersOnFlagTeam2.Add(p);
                    Team2TotalPlayers++;
                    if (Manager.AllowPassengersToCapture || p.Player.movement.getVehicle() == null)
                        Team2TotalCappers++;
                }
            }
        }
    }
    /// <param name="newPlayers">Players that have entered the flag since last check.</param>
    /// <param name="departedPlayers">Players that have left the flag since last check.</param>
    public void GetUpdatedPlayers(out List<Player> newPlayers, out List<Player> departedPlayers)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        UCPlayer[] _prevPlayers = PlayersOnFlag.ToArray();
        RecalcCappers();
        newPlayers = new List<Player>(2);
        departedPlayers = new List<Player>(2);
        for (int i = 0; i < PlayersOnFlag.Count; i++)
        {
            UCPlayer player = PlayersOnFlag[i];
            for (int j = 0; j < _prevPlayers.Length; j++)
                if (player.Steam64 == _prevPlayers[j].Steam64)
                    goto done;
            newPlayers.Add(player);
        done:;
        }
        for (int i = 0; i < _prevPlayers.Length; i++)
        {
            UCPlayer player = _prevPlayers[i];
            for (int j = 0; j < PlayersOnFlag.Count; j++)
                if (player.Steam64 == PlayersOnFlag[j].Steam64)
                    goto done;
            departedPlayers.Add(player);
        done:;
        }
    }
    public void SetPoints(float value, bool skipEvent = false, bool skipDeltaPoints = false)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        float OldPoints = _points;
        if (value > MAX_POINTS) _points = MAX_POINTS;
        else if (value < -MAX_POINTS) _points = -MAX_POINTS;
        else _points = value;
        if (!skipDeltaPoints)
            LastDeltaPoints = _points - OldPoints;
        if (!skipEvent)
            OnPointsChanged?.Invoke(_points, OldPoints, this);
    }
    public Flag(Zone zone, IFlagRotation manager)
    {
        this.Manager = manager;
        this._id = zone.Id;
        this._x = zone.Center.x;
        this._y = zone.Center3D.y;
        this._z = zone.Center.y;
        this._position2d = new Vector2(_x, _z);
        this._position = new Vector3(_x, _y, _z);
        this._name = zone.Name;
        if (string.IsNullOrEmpty(zone.ShortName))
            this._shortName = _name;
        else
            this._shortName = zone.ShortName!;
        this._color = UCWarfare.GetColorHex("default");
        this._owner = 0;
        PlayersOnFlag = new List<UCPlayer>(48);
        PlayersOnFlagTeam1 = new List<UCPlayer>(24);
        PlayersOnFlagTeam2 = new List<UCPlayer>(24);
        this.ZoneData = zone;
        this.Adjacencies = zone.Data.Adjacencies;
    }
    public bool IsFriendly(SteamPlayer player) => IsFriendly(player.player.quests.groupID.m_SteamID);
    public bool IsFriendly(Player player) => IsFriendly(player.quests.groupID.m_SteamID);
    public bool IsFriendly(CSteamID groupID) => IsFriendly(groupID.m_SteamID);
    public bool IsFriendly(ulong groupID) => groupID == _owner;
    public bool PlayerInRange(Vector3 position) => ZoneData.IsInside(position);
    public bool PlayerInRange(Vector2 position) => ZoneData.IsInside(position);
    public bool PlayerInRange(SteamPlayer player) => PlayerInRange(player.player.transform.position);
    public bool PlayerInRange(Player player) => PlayerInRange(player.transform.position);
    public void EnterPlayer(Player player)
    {
        OnPlayerEntered?.Invoke(this, player);
    }
    public void ExitPlayer(Player player)
    {
        OnPlayerLeft?.Invoke(this, player);
    }
    public bool IsNeutral() => _points == 0;
    public void CapT1(float amount)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        amount *= CaptureMultiplier;
        float amt = Points + amount;
        if (Points > 0 && amt < 0 || Points < 0 && amt > 0) // if sign will be changing
            amt = 0;
        if (amt >= MAX_POINTS)
            CapT1();
        else if (amt <= -MAX_POINTS)
            CapT2();
        else
            SetPoints(amt);
    }
    public void CapT1()
    {
        SetPoints(MAX_POINTS);
        SetOwner(1);
    }
    public void CapT2(float amount)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        amount *= CaptureMultiplier;
        float amt = Points - amount;
        if (Points > 0 && amt < 0 || Points < 0 && amt > 0) // if sign will be changing
            amt = 0;
        if (amt >= MAX_POINTS)
            CapT1();
        else if (amt <= -MAX_POINTS)
            CapT2();
        else
            SetPoints(amt);
    }
    public void CapT2()
    {
        SetPoints(-MAX_POINTS);
        SetOwner(2);
    }
    public bool IsFull(ulong team)
    {
        if (team == 1) return Points >= MAX_POINTS;
        else if (team == 2) return Points <= -MAX_POINTS;
        else return false;
    }
    public void Cap(ulong team, float amount)
    {
        amount *= CaptureMultiplier;
        if (team == 1) CapT1(amount);
        else if (team == 2) CapT2(amount);
    }
    public void Cap(ulong team)
    {
        if (team == 1) CapT1();
        else if (team == 2) CapT2();
    }
    public bool IsObj(ulong team)
    {
        if (team == 1) return T1Obj;
        else if (team == 2) return T2Obj;
        else return false;
    }
    public bool IsAttackSite(ulong team) => Manager.IsAttackSite(team, this);
    public bool IsDefenseSite(ulong team) => Manager.IsDefenseSite(team, this);
    public bool Discovered(ulong team)
    {
        return team switch
        {
            1 => _discovered1,
            2 => _discovered2,
            3 => true,
            _ => false
        };
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
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (IsContestedOverride != null)
            return IsContestedOverride(this, out winner);
        if ((T1Obj && T2Obj) || (T1Obj && Owner == 2) || (T2Obj && Owner == 1)) // must be objective for both teams
        {
            if (Team1TotalCappers == 0 && Team2TotalCappers == 0)
            {
                winner = 0;
                return false;
            }
            else if (Team1TotalCappers == Team2TotalCappers)
            {
                winner = Intimidation.CheckSquadsForContestBoost(this);
            }
            else if (Team1TotalCappers == 0 && Team2TotalCappers > 0)
            {
                winner = 2;
            }
            else if (Team2TotalCappers == 0 && Team1TotalCappers > 0)
            {
                winner = 1;
            }
            else if (Team1TotalCappers > Team2TotalCappers)
            {
                if (Team1TotalCappers - Gamemode.Config.AASRequiredCapturingPlayerDifference >= Team2TotalCappers)
                {
                    winner = 1;
                }
                else
                {
                    winner = Intimidation.CheckSquadsForContestBoost(this);
                }
            }
            else
            {
                if (Team2TotalCappers - Gamemode.Config.AASRequiredCapturingPlayerDifference >= Team1TotalCappers)
                {
                    winner = 2;
                }
                else
                {
                    winner = Intimidation.CheckSquadsForContestBoost(this);
                }
            }

            return winner == 0ul;
        }
        else
        {
            if (ObjectivePlayerCountCappers == 0) winner = 0;
            else winner = WhosObj();
            if (!IsObj(winner)) winner = 0;
            return false;
        }
    }
    public void EvaluatePoints(bool overrideInactiveCheck = false)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (EvaluatePointsOverride != null)
        {
            EvaluatePointsOverride(this, overrideInactiveCheck);
            return;
        }
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
                            Cap(winner, GetCaptureAmount(Gamemode.Config.AASCaptureScale, winner));
                        }
                    }
                }
                else
                {
                    // invoke points updated method to show contested.
                    this.LastDeltaPoints = 0;
                    OnPointsChanged?.Invoke(_points, _points, this);
                }
            }
        }
    }

    internal float GetCaptureAmount(float captureScale, ulong team)
    {
        return captureScale * Mathf.Log10((team == 0 ? Math.Max(Team1TotalCappers, Team2TotalCappers) : (team == 1 ? Team1TotalCappers : Team2TotalCappers)) + 1);
    }

    [FormatDisplay("Colored Name")]
    [FormatDisplay(typeof(Zone), "Name")]
    public const string COLOR_NAME_FORMAT = "nc";

    [FormatDisplay("Name")]
    [FormatDisplay(typeof(Zone), "Name")]
    public const string NAME_FORMAT = "n";

    [FormatDisplay("Colored Short Name")]
    [FormatDisplay(typeof(Zone), "Short Name")]
    public const string COLOR_SHORT_NAME_FORMAT = "sc";

    [FormatDisplay("Short Name")]
    [FormatDisplay(typeof(Zone), "Short Name")]
    public const string SHORT_NAME_FORMAT = "s";

    [FormatDisplay("Colored Name (Discovered Check)")]
    [FormatDisplay(typeof(Zone), "Name")]
    public const string COLOR_NAME_DISCOVER_FORMAT = "ncd";

    [FormatDisplay("Name (Discovered Check)")]
    [FormatDisplay(typeof(Zone), "Name")]
    public const string NAME_DISCOVER_FORMAT = "nd";

    [FormatDisplay("Colored Short Name (Discovered Check)")]
    [FormatDisplay(typeof(Zone), "Short Name")]
    public const string COLOR_SHORT_NAME_DISCOVER_FORMAT = "scd";

    [FormatDisplay("Short Name (Discovered Check)")]
    [FormatDisplay(typeof(Zone), "Short Name")]
    public const string SHORT_NAME_DISCOVER_FORMAT = "sd";
    string ITranslationArgument.Translate(string language, string? format, UCPlayer? target,
        ref TranslationFlags flags)
    {
        if (format is null) goto end;
        if (format.Equals(COLOR_NAME_FORMAT, StringComparison.Ordinal))
            return Localization.Colorize(TeamSpecificHexColor, Name, flags);
        if (format.Equals(NAME_FORMAT, StringComparison.Ordinal))
            return Localization.Colorize(TeamSpecificHexColor, Name, flags);
        if (format.Equals(COLOR_SHORT_NAME_FORMAT, StringComparison.Ordinal))
            return Localization.Colorize(TeamSpecificHexColor, ShortName, flags);
        if (format.Equals(SHORT_NAME_FORMAT, StringComparison.Ordinal))
            return ShortName;

        ulong team;
        if (target is null)
        {
            if ((flags & TranslationFlags.Team1) == TranslationFlags.Team1) team = 1;
            else if ((flags & TranslationFlags.Team2) == TranslationFlags.Team2) team = 2;
            else if ((flags & TranslationFlags.Team3) == TranslationFlags.Team3) team = 3;
            else team = 0;
        }
        else team = target.GetTeam();

        if (format.Equals(COLOR_NAME_DISCOVER_FORMAT, StringComparison.Ordinal))
            return team == 0 || Discovered(team)
                ? Localization.Colorize(TeamSpecificHexColor, Name, flags)
                : Localization.Colorize(UCWarfare.GetColorHex("undiscovered_flag"),
                    Localization.Translate(T.UndiscoveredFlagNoColor, target), flags);

        if (format.Equals(NAME_DISCOVER_FORMAT, StringComparison.Ordinal))
            return team == 0 || Discovered(team)
                ? Name
                : Localization.Translate(T.UndiscoveredFlagNoColor, target);

        if (format.Equals(COLOR_SHORT_NAME_DISCOVER_FORMAT, StringComparison.Ordinal))
            return team == 0 || Discovered(team)
                ? Localization.Colorize(TeamSpecificHexColor, ShortName, flags)
                : Localization.Colorize(UCWarfare.GetColorHex("undiscovered_flag"),
                    Localization.Translate(T.UndiscoveredFlagNoColor, target), flags);

        if (format.Equals(SHORT_NAME_DISCOVER_FORMAT, StringComparison.Ordinal))
            return team == 0 || Discovered(team)
                ? Name
                : Localization.Translate(T.UndiscoveredFlagNoColor, target);
        end:
        return Name;
    }
}

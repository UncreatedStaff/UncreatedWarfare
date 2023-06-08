using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Globalization;
using SDG.Framework.Utilities;
using Uncreated.Framework;
using Uncreated.SQL;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Traits.Buffs;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Flags;

public delegate void DiscoveryDelegate(ulong team);
public class Flag : IDisposable, IObjective
{
    public delegate void EvaluatePointsDelegate(Flag flag, bool overrideInactiveCheck = false);
    public delegate bool IsContestedDelegate(Flag flag, out ulong winner);
    public int Index = -1;
    public const float MaxPoints = 64f;
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
    public EvaluatePointsDelegate? EvaluatePointsOverride;
    public IsContestedDelegate? IsContestedOverride;
    public int Team1TotalCappers;
    public List<UCPlayer> PlayersOnFlagTeam2;
    public int Team2TotalPlayers;
    public int Team2TotalCappers;
    private float _points;
    protected bool Discovered1;
    protected bool Discovered2;
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

    public List<UCPlayer> PlayersOnFlag { get; private set; }
    public SqlItem<Zone> ZoneData { get; protected set; }
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
        get => Discovered1;
        protected set => Discovered1 = value;
    }
    public bool DiscoveredT2
    {
        get => Discovered2;
        protected set => Discovered2 = value;
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
        RecalcCappers();
    }
    public void Dispose()
    {
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
    public PlayerChange GetUpdatedPlayers()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        UCPlayer[] prevPlayers = PlayersOnFlag.ToArray();
        RecalcCappers();
        PlayerChange change = PlayerChange.Claim();
        for (int i = 0; i < PlayersOnFlag.Count; i++)
        {
            UCPlayer player = PlayersOnFlag[i];
            for (int j = 0; j < prevPlayers.Length; j++)
                if (player.Steam64 == prevPlayers[j].Steam64)
                    goto done;
            change.NewPlayers.Add(player);
            done:;
        }
        for (int i = 0; i < prevPlayers.Length; i++)
        {
            UCPlayer player = prevPlayers[i];
            for (int j = 0; j < PlayersOnFlag.Count; j++)
                if (player.Steam64 == PlayersOnFlag[j].Steam64)
                    goto done;
            if (player.IsOnline)
                change.DepartingPlayers.Add(player);
            done:;
        }

        return change;
    }
    public readonly ref struct PlayerChange
    {
        public readonly List<Player> NewPlayers;
        public readonly List<Player> DepartingPlayers;

        private PlayerChange(List<Player> newPlayers, List<Player> departingPlayers)
        {
            NewPlayers = newPlayers;
            DepartingPlayers = departingPlayers;
        }

        public static PlayerChange Claim()
        {
            List<Player> newPlayers = ListPool<Player>.claim();
            List<Player> departedPlayers = ListPool<Player>.claim();
            return new PlayerChange(newPlayers, departedPlayers);
        }

        public void Release()
        {
            ListPool<Player>.release(NewPlayers);
            ListPool<Player>.release(DepartingPlayers);
        }
    }

    public void SetPoints(float value, bool skipEvent = false, bool skipDeltaPoints = false)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        float oldPts = _points;
        if (value > MaxPoints) _points = MaxPoints;
        else if (value < -MaxPoints) _points = -MaxPoints;
        else _points = value;
        if (!skipDeltaPoints)
            LastDeltaPoints = _points - oldPts;
        if (!skipEvent)
            OnPointsChanged?.Invoke(_points, oldPts, this);
    }
    public Flag(SqlItem<Zone> zone, IFlagRotation manager)
    {
        Manager = manager;
        Zone? z = zone.Item;
        if (z is null)
            throw new ArgumentNullException(nameof(zone));
        _id = z.PrimaryKey;
        _x = z.Spawn.x;
        _y = z.Spawn3D.y;
        _z = z.Spawn.y;
        _position2d = new Vector2(_x, _z);
        _position = new Vector3(_x, _y, _z);
        _name = z.Name;
        _shortName = string.IsNullOrEmpty(z.ShortName) ? _name : z.ShortName!;
        _color = UCWarfare.GetColorHex("default");
        _owner = 0;
        PlayersOnFlag = new List<UCPlayer>(48);
        PlayersOnFlagTeam1 = new List<UCPlayer>(24);
        PlayersOnFlagTeam2 = new List<UCPlayer>(24);
        ZoneData = zone;
        Adjacencies = z.Data.Adjacencies;
    }
    public bool IsFriendly(SteamPlayer player) => IsFriendly(player.player.quests.groupID.m_SteamID);
    public bool IsFriendly(Player player) => IsFriendly(player.quests.groupID.m_SteamID);
    public bool IsFriendly(CSteamID groupID) => IsFriendly(groupID.m_SteamID);
    public bool IsFriendly(ulong groupID) => groupID == _owner;
    public bool PlayerInRange(Vector3 position) => ZoneData is { Item: { } z } && z.IsInside(position);
    public bool PlayerInRange(Vector2 position) => ZoneData is { Item: { } z } && z.IsInside(position);
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
        if (amt >= MaxPoints)
            CapT1();
        else if (amt <= -MaxPoints)
            CapT2();
        else
            SetPoints(amt);
    }
    public void CapT1()
    {
        SetPoints(MaxPoints);
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
        if (amt >= MaxPoints)
            CapT1();
        else if (amt <= -MaxPoints)
            CapT2();
        else
            SetPoints(amt);
    }
    public void CapT2()
    {
        SetPoints(-MaxPoints);
        SetOwner(2);
    }
    public bool IsFull(ulong team)
    {
        if (team == 1) return Points >= MaxPoints;
        else if (team == 2) return Points <= -MaxPoints;
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
    public bool IsCapturable(ulong team)
    {
        if (Manager is not IFlagTeamObjectiveGamemode ctf)
            return IsAttackSite(team) || IsDefenseSite(team);

        if (IsAttackSite(team) || IsDefenseSite(team))
            return true;

        // double neutral feature

        if (team == 2)
        {
            if (ctf.ObjectiveTeam2 != null &&
                ctf.ObjectiveTeam2.Owner == 0 && 
                ctf.ObjectiveTeam2.Points > 0 && 
                Index == ctf.ObjectiveT2Index - 1 &&
                Points > 0)
            // if team 2's objective is neutralized and being lost to team 1, and this flag is team 2's next attack target and owned by team 1 
            // i.e.:
            // if our objective is neutralized and being lost to the enemy, and this flag is enemy controlled and our next attack target
            {
                return true;
            }
        }
        else if (team == 1) 
        {
            if (ctf.ObjectiveTeam1 != null &&
                ctf.ObjectiveTeam1.Owner == 0 &&
                ctf.ObjectiveTeam1.Points < 0 &&
                Index == ctf.ObjectiveT1Index + 1 &&
                Points < 0)
                // if team 1's objective is neutralized and being lost to team 2, and this flag is team 1's next attack target and owned by team 2 
                // i.e.:
                // if our objective is neutralized and being lost to the enemy, and this flag is enemy controlled and our next attack target
            {
                return true;
            }
        }

        return false;
    }
    public bool IsAttackSite(ulong team) => Manager.IsAttackSite(team, this);
    public bool IsDefenseSite(ulong team) => Manager.IsDefenseSite(team, this);
    public bool Discovered(ulong team)
    {
        return team switch
        {
            1 => Discovered1,
            2 => Discovered2,
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

        //if (IsCapturable(1) || IsCapturable(2)) // must be objective for both teams
        //{
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

            if (!IsCapturable(winner))
                return false;

            return winner == 0ul;
        //}
        //else
        //{
        //    if (ObjectivePlayerCountCappers == 0) winner = 0;
        //    else winner = WhosObj();
        //    if (!IsObj(winner)) winner = 0;
        //    return false;
        //}
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
        if (Manager.State == State.Active || overrideInactiveCheck)
        {
            if (!IsContested(out ulong winner))
            {
                if (IsCapturable(winner))
                {
                    if (winner == 1 || winner == 2)
                    {
                        Cap(winner, GetCaptureAmount(Gamemode.Config.AASCaptureScale, winner));
                    }
                }
            }
            else if (IsAnObj)
            {
                // invoke points updated method to show contested.
                LastDeltaPoints = 0;
                OnPointsChanged?.Invoke(_points, _points, this);
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
        CultureInfo? culture,
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
    public override string ToString() => Name;
}

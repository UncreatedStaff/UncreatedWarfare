using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uncreated.Warfare.Gamemodes.Interfaces;

namespace Uncreated.Warfare.Gamemodes.Flags;

public abstract class FlagGamemode : TeamGamemode, IFlagRotation
{
    protected int _counter;
    protected int _counter2;
    protected List<Flag> _rotation = new List<Flag>();
    protected List<Flag> _allFlags = new List<Flag>();
    public Dictionary<ulong, int> _onFlag = new Dictionary<ulong, int>();
    public List<Flag> Rotation { get => _rotation; }
    public List<Flag> LoadedFlags { get => _allFlags; }
    public Dictionary<ulong, int> OnFlag { get => _onFlag; }
    public virtual bool AllowPassengersToCapture => false;
    public FlagGamemode(string Name, float EventLoopSpeed) : base(Name, EventLoopSpeed)
    { }
    protected abstract bool TimeToCheck();
    protected override void PostDispose()
    {
        ResetFlags();
        _onFlag.Clear();
        _rotation.Clear();
        _counter = 0;
        _counter2 = 0;
        base.PostDispose();
    }
    protected override void OnReady()
    {
        LoadAllFlags();
        base.OnReady();
    }
    protected override void PreGameStarting(bool isOnLoad)
    {
        LoadRotation();
        base.PreGameStarting(isOnLoad);
    }
    protected override void EventLoopAction()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
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
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        _allFlags.Clear();
        _allFlags.Capacity = Data.ZoneProvider.Zones.Count;
        for (int i = 0; i < Data.ZoneProvider.Zones.Count; i++)
        {
            if (Data.ZoneProvider.Zones[i].Data.UseCase == EZoneUseCase.FLAG)
            {
                _allFlags.Add(new Flag(Data.ZoneProvider.Zones[i], this) { index = -1 });
            }
        }
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
            sb.Append(i.ToString(Data.Locale) + ") " + _rotation[i].Name);
            if (_rotation[i].DiscoveredT1) sb.Append(" T1");
            if (_rotation[i].DiscoveredT2) sb.Append(" T2");
            if (i != _rotation.Count - 1) sb.Append('\n');
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
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (_onFlag.ContainsKey(player.channel.owner.playerID.steamID.m_SteamID) && _onFlag[player.channel.owner.playerID.steamID.m_SteamID] == flag.ID)
        {
            _onFlag.Remove(player.channel.owner.playerID.steamID.m_SteamID);
            flag.ExitPlayer(player);
        }
    }
    public virtual void AddPlayerOnFlag(Player player, Flag flag)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
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
    public abstract bool IsAttackSite(ulong team, Flag flag);
    public abstract bool IsDefenseSite(ulong team, Flag flag);
}

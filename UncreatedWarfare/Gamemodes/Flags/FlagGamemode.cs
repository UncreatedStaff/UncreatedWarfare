using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Teams;

namespace Uncreated.Warfare.Gamemodes.Flags;

public abstract class FlagGamemode : TeamGamemode, IFlagRotation
{
    protected List<Flag> _rotation = new List<Flag>();
    protected List<Flag> _allFlags = new List<Flag>();
    public Dictionary<ulong, int> _onFlag = new Dictionary<ulong, int>();
    public List<Flag> Rotation => _rotation;
    public List<Flag> LoadedFlags => _allFlags;
    public Dictionary<ulong, int> OnFlag => _onFlag;
    public virtual bool AllowPassengersToCapture => false;
    protected FlagGamemode(string Name, float EventLoopSpeed) : base(Name, EventLoopSpeed)
    { }
    protected abstract bool TimeToEvaluatePoints();
    protected override Task PostDispose()
    {
        ThreadUtil.assertIsGameThread();
        ResetFlags();
        _onFlag.Clear();
        _rotation.Clear();
        return base.PostDispose();
    }
    protected override Task OnReady()
    {
        ThreadUtil.assertIsGameThread();
        LoadAllFlags();
        return base.OnReady();
    }
    protected override Task PreGameStarting(bool isOnLoad)
    {
        ThreadUtil.assertIsGameThread();
        LoadRotation();
        return base.PreGameStarting(isOnLoad);
    }
    protected override void EventLoopAction()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int i = 0; i < _rotation.Count; i++)
        {
            Flag f = _rotation[i];
            if (f == null) continue;
            f.GetUpdatedPlayers(out List<Player> newPlayers, out List<Player> departingPlayers);
            foreach (Player player in departingPlayers)
                RemovePlayerFromFlag(player.channel.owner.playerID.steamID.m_SteamID, player, f);
            foreach (Player player in newPlayers)
                AddPlayerOnFlag(player, f);
        }
        if (TimeToEvaluatePoints())
        {
            EvaluatePoints();
            if (EnableAMC)
                CheckMainCampZones();
            OnEvaluate();
            TeamManager.EvaluateBases();
        }
    }
    protected void ConvertFlags()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Data.ZoneProvider.Reload();
        TeamManager.OnReloadFlags();
        _allFlags.Clear();
        _allFlags.Capacity = Data.ZoneProvider.Zones.Count;
        for (int i = 0; i < Data.ZoneProvider.Zones.Count; i++)
        {
            if (Data.ZoneProvider.Zones[i].Data.UseCase == EZoneUseCase.FLAG)
            {
                _allFlags.Add(new Flag(Data.ZoneProvider.Zones[i], this) { index = -1 });
            }
        }
        _allFlags.Sort((a, b) => a.ID.CompareTo(b.ID));
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
    protected virtual void RemovePlayerFromFlag(ulong playerId, Player? player, Flag flag)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (flag == null)
            return;
        if (_onFlag.TryGetValue(playerId, out int fid) && fid == flag.ID)
        {
            _onFlag.Remove(playerId);
            if (player != null)
                flag.ExitPlayer(player);
        }
    }
    public virtual void AddPlayerOnFlag(Player player, Flag flag)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (_onFlag.TryGetValue(player.channel.owner.playerID.steamID.m_SteamID, out int fid))
        {
            if (fid != flag.ID)
            {
                Flag? oldFlag = _rotation.FirstOrDefault(f => f.ID == fid);
                if (oldFlag == null) _onFlag.Remove(player.channel.owner.playerID.steamID.m_SteamID);
                else RemovePlayerFromFlag(player.channel.owner.playerID.steamID.m_SteamID, player, oldFlag); // remove the player from their old flag first in the case of teleporting from one flag to another.
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
    internal override string DumpState()
    {
        StringBuilder flags = new StringBuilder();
        for (int f = 0; f < _rotation.Count; f++)
        {
            if (f == 0) flags.Append('\n');
            Flag flag = _rotation[f];
            flags.Append(flag.Name).Append("\nOwner: ").Append(flag.Owner).Append(" Players: \n1: ")
                .Append(string.Join(",", flag.PlayersOnFlagTeam1.Select(x => F.GetPlayerOriginalNames(x).PlayerName))).Append("\n2: ")
                .Append(string.Join(",", flag.PlayersOnFlagTeam2.Select(x => F.GetPlayerOriginalNames(x).PlayerName)))
                .Append("\nPoints: ").Append(flag.Points).Append(" State: ").Append(flag.LastDeltaPoints).Append('\n');
        }

        return flags.ToString();
    }
}

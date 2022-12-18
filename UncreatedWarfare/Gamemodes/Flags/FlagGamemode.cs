using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Traits.Buffs;

namespace Uncreated.Warfare.Gamemodes.Flags;

public abstract class FlagGamemode : TeamGamemode, IFlagRotation
{
    protected List<Flag> FlagRotation = new List<Flag>();
    protected List<Flag> AllFlags = new List<Flag>();
    public Dictionary<ulong, int> OnFlagDict = new Dictionary<ulong, int>();
    public List<Flag> Rotation => FlagRotation;
    public List<Flag> LoadedFlags => AllFlags;
    public Dictionary<ulong, int> OnFlag => OnFlagDict;
    public virtual bool AllowPassengersToCapture => false;
    protected FlagGamemode(string name, float eventLoopSpeed) : base(name, eventLoopSpeed)
    { }
    protected abstract bool TimeToEvaluatePoints();
    protected override Task PostDispose(CancellationToken token)
    {
        token.CombineIfNeeded(UnloadToken);
        ThreadUtil.assertIsGameThread();
        ResetFlags();
        OnFlagDict.Clear();
        FlagRotation.Clear();
        return base.PostDispose(token);
    }
    protected override Task OnReady(CancellationToken token)
    {
        token.CombineIfNeeded(UnloadToken);
        ThreadUtil.assertIsGameThread();
        LoadAllFlags();
        return base.OnReady(token);
    }
    protected override Task PreGameStarting(bool isOnLoad, CancellationToken token)
    {
        token.CombineIfNeeded(UnloadToken);
        ThreadUtil.assertIsGameThread();
        LoadRotation();
        return base.PreGameStarting(isOnLoad, token);
    }
    protected override void EventLoopAction()
    {
        base.EventLoopAction();
        FlagCheck();
    }
    protected virtual void FlagCheck()
    {
        for (int i = 0; i < FlagRotation.Count; i++)
        {
            Flag f = FlagRotation[i];
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
        }
    }
    protected void ConvertFlags()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        TeamManager.OnReloadFlags();
        AllFlags.Clear();
        AllFlags.Capacity = Data.ZoneProvider.Zones.Count;
        for (int i = 0; i < Data.ZoneProvider.Zones.Count; i++)
        {
            if (Data.ZoneProvider.Zones[i].Data.UseCase == EZoneUseCase.FLAG)
            {
                AllFlags.Add(new Flag(Data.ZoneProvider.Zones[i], this) { index = -1 });
            }
        }
        AllFlags.Sort((a, b) => a.ID.CompareTo(b.ID));
    }
    public virtual void OnEvaluate()
    { }
    public void LoadAllFlags()
    {
        ConvertFlags();
        L.Log("Loaded " + AllFlags.Count.ToString(Data.AdminLocale) + " flags into memory and cleared any existing old flags.", ConsoleColor.Magenta);
    }
    public virtual void PrintFlagRotation()
    {
        StringBuilder sb = new StringBuilder(FlagRotation.Count.ToString(Data.AdminLocale) + " flags:\n");
        for (int i = 0; i < FlagRotation.Count; i++)
        {
            sb.Append(i.ToString(Data.AdminLocale) + ") " + FlagRotation[i].Name);
            if (FlagRotation[i].DiscoveredT1) sb.Append(" T1");
            if (FlagRotation[i].DiscoveredT2) sb.Append(" T2");
            if (i != FlagRotation.Count - 1) sb.Append('\n');
        }
        L.Log(sb.ToString(), ConsoleColor.Green);
    }
    public abstract void LoadRotation();
    protected virtual void EvaluatePoints()
    {
        if (_state == State.Active)
            for (int i = 0; i < FlagRotation.Count; i++)
                FlagRotation[i].EvaluatePoints();
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
        foreach (Flag flag in FlagRotation)
        {
            flag.OnPlayerEntered -= PlayerEnteredFlagRadius;
            flag.OnPlayerLeft -= PlayerLeftFlagRadius;
            flag.OnOwnerChanged -= FlagOwnerChanged;
            flag.OnPointsChanged -= FlagPointsChanged;
            flag.ResetFlag();
        }
        FlagRotation.Clear();
    }
    protected virtual void RemovePlayerFromFlag(ulong playerId, Player? player, Flag flag)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (flag == null)
            return;
        if (OnFlagDict.TryGetValue(playerId, out int fid) && fid == flag.ID)
        {
            OnFlagDict.Remove(playerId);
            if (player != null)
                flag.ExitPlayer(player);
        }
    }
    public virtual void AddPlayerOnFlag(Player player, Flag flag)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (OnFlagDict.TryGetValue(player.channel.owner.playerID.steamID.m_SteamID, out int fid))
        {
            if (fid != flag.ID)
            {
                Flag? oldFlag = FlagRotation.FirstOrDefault(f => f.ID == fid);
                if (oldFlag == null) OnFlagDict.Remove(player.channel.owner.playerID.steamID.m_SteamID);
                else RemovePlayerFromFlag(player.channel.owner.playerID.steamID.m_SteamID, player, oldFlag); // remove the player from their old flag first in the case of teleporting from one flag to another.
                OnFlagDict.Add(player.channel.owner.playerID.steamID.m_SteamID, flag.ID);
            }
        }
        else OnFlagDict.Add(player.channel.owner.playerID.steamID.m_SteamID, flag.ID);
        flag.EnterPlayer(player);
    }
    protected abstract void PlayerEnteredFlagRadius(Flag flag, Player player);
    protected abstract void PlayerLeftFlagRadius(Flag flag, Player player);
    protected abstract void FlagOwnerChanged(ulong oldOwner, ulong newOwner, Flag flag);
    protected abstract void FlagPointsChanged(float newPts, float oldPts, Flag flag);
    public abstract bool IsAttackSite(ulong team, Flag flag);
    public abstract bool IsDefenseSite(ulong team, Flag flag);
    internal override string DumpState()
    {
        StringBuilder flags = new StringBuilder();
        for (int f = 0; f < FlagRotation.Count; f++)
        {
            if (f == 0) flags.Append('\n');
            Flag flag = FlagRotation[f];
            flags.Append(flag.Name).Append("\nOwner: ").Append(flag.Owner).Append(" Players: \n1: ")
                .Append(string.Join(",", flag.PlayersOnFlagTeam1.Select(x => x.Name.PlayerName))).Append("\n2: ")
                .Append(string.Join(",", flag.PlayersOnFlagTeam2.Select(x => x.Name.PlayerName)))
                .Append("\nPoints: ").Append(flag.Points).Append(" State: ").Append(flag.LastDeltaPoints).Append('\n');
        }

        return flags.ToString();
    }
    protected static bool ConventionalIsContested(Flag flag, out ulong winner)
    {
        int t1 = flag.Team1TotalCappers, t2 = flag.Team2TotalCappers;
        if (t1 == 0 && t2 == 0)
        {
            winner = 0;
            return false;
        }
        if (t1 == t2)
            winner = Intimidation.CheckSquadsForContestBoost(flag);
        else if (t1 == 0 && t2 > 0)
            winner = 2;
        else if (t2 == 0 && t1 > 0)
            winner = 1;
        else if (t1 > t2)
            winner = t1 - Config.AASRequiredCapturingPlayerDifference >= t2 ? 1ul : Intimidation.CheckSquadsForContestBoost(flag);
        else
            winner = t2 - Config.AASRequiredCapturingPlayerDifference >= t1 ? 2ul : Intimidation.CheckSquadsForContestBoost(flag);

        return winner == 0;
    }
}

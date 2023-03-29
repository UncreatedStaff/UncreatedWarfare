using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SDG.Framework.Utilities;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Traits.Buffs;
using UnityEngine;

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
    public virtual ElectricalGridBehaivor ElectricalGridBehavior => ElectricalGridBehaivor.EnabledWhenInRotation;
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
        Flag? obj1 = null;
        Flag? obj2 = null;
        if (this is IFlagObjectiveGamemode gm1)
            obj1 = gm1.Objective;
        else if (this is IFlagTeamObjectiveGamemode gm2)
        {
            obj1 = gm2.ObjectiveTeam1;
            obj2 = gm2.ObjectiveTeam2;
        }
        LoadRotation();
        OnRotationUpdated();
        if (ElectricalGridBehavior != ElectricalGridBehaivor.EnabledWhenInRotation)
        {
            SetPowerForAllInGrid((TeamManager.Team2Main.Data.GridObjects ?? Array.Empty<GridObject>())
                .Concat(TeamManager.Team2Main.Data.GridObjects ?? Array.Empty<GridObject>()), true);
        }
        if (this is IFlagObjectiveGamemode gm3)
        {
            if (gm3.Objective != obj1)
                OnObjectiveChangedPowerHandler(obj1, gm3.Objective);
        }
        else if (this is IFlagTeamObjectiveGamemode gm4)
        {
            if (gm4.ObjectiveTeam1 != obj1)
                OnObjectiveChangedPowerHandler(obj1, gm4.ObjectiveTeam1);
            if (gm4.ObjectiveTeam2 != obj2)
                OnObjectiveChangedPowerHandler(obj2, gm4.ObjectiveTeam2);
        }
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
            if (f != null) CheckFlagForPlayerChanges(f);
        }
        if (TimeToEvaluatePoints())
        {
            EvaluatePoints();
            if (EnableAMC)
                CheckMainCampZones();
            OnEvaluate();
        }
    }
    protected void CheckFlagForPlayerChanges(Flag f)
    {
        Flag.PlayerChange change = f.GetUpdatedPlayers();

        List<Player> list = change.DepartingPlayers;
        for (int j = 0; j < list.Count; j++)
        {
            Player player = list[j];
            RemovePlayerFromFlag(player.channel.owner.playerID.steamID.m_SteamID, player, f);
        }

        list = change.NewPlayers;
        for (int j = 0; j < list.Count; j++)
        {
            Player player = list[j];
            AddPlayerOnFlag(player, f);
        }

        change.Release();
    }
    protected void ConvertFlags()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        TeamManager.OnReloadFlags();
        int c = AllFlags.Count == 0 ? 48 : AllFlags.Count;
        AllFlags.Clear();
        AllFlags.Capacity = c;
        ZoneList? singleton = Data.Singletons.GetSingleton<ZoneList>();
        if (singleton == null) throw new SingletonUnloadedException(typeof(ZoneList));
        singleton.WriteWait();
        try
        {
            for (int i = 0; i < singleton.Items.Count; ++i)
            {
                if (singleton[i] is { Item.Data.UseCase: ZoneUseCase.Flag } proxy)
                    AllFlags.Add(new Flag(proxy, this) { Index = -1 });
            }
        }
        finally
        {
            singleton.WriteRelease();
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
        if (State == State.Active)
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

        return base.DumpState() + Environment.NewLine + flags;
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
    internal virtual bool IsInteractableEnabled(Interactable interactable)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Vector3 pos = interactable.transform.position;
        if (TeamManager.IsInAnyMainOrAMCOrLobby(pos))
            return true;
        switch (ElectricalGridBehavior)
        {
            case ElectricalGridBehaivor.AllEnabled: return true;
            case ElectricalGridBehaivor.EnabledWhenInRotation:
                if (Rotation != null)
                {
                    for (int i = 0; i < Rotation.Count; ++i)
                    {
                        if (Rotation[i].PlayerInRange(pos))
                            return true;
                    }
                }
                goto default;
            case ElectricalGridBehaivor.EnabledWhenObjective:
                if (this is IFlagObjectiveGamemode obj1)
                {
                    Flag? flag = obj1.Objective;
                    if (flag != null && flag.PlayerInRange(pos))
                        return true;
                }
                else if (this is IFlagTeamObjectiveGamemode obj2)
                {
                    Flag? flag = obj2.ObjectiveTeam1;
                    if (flag != null && flag.PlayerInRange(pos))
                        return true;
                    flag = obj2.ObjectiveTeam2;
                    if (flag != null && flag.PlayerInRange(pos))
                        return true;
                }
                goto default;
            default:
                return false;
        }
    }
    internal virtual bool IsPowerObjectEnabled(InteractableObject obj)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (CheckFlag(TeamManager.Team1Main.Data.GridObjects) ||
            CheckFlag(TeamManager.Team2Main.Data.GridObjects) ||
            CheckFlag(TeamManager.Team1AMC.Data.GridObjects) ||
            CheckFlag(TeamManager.Team2AMC.Data.GridObjects))
            return true;
        switch (ElectricalGridBehavior)
        {
            case ElectricalGridBehaivor.AllEnabled:
                return true;
            case ElectricalGridBehaivor.EnabledWhenInRotation:
                if (Rotation != null)
                {
                    for (int i = 0; i < Rotation.Count; ++i)
                    {
                        GridObject[]? grid = Rotation[i].ZoneData?.Item?.Data.GridObjects;
                        if (grid is not { Length: > 0 })
                            continue;
                        if (CheckFlag(grid))
                            return true;
                    }
                }
                break;
            case ElectricalGridBehaivor.EnabledWhenObjective:
                if (this is IFlagObjectiveGamemode obj1)
                {
                    GridObject[]? grid = obj1.Objective?.ZoneData?.Item?.Data.GridObjects;
                    if (grid is { Length: > 0 })
                        if (CheckFlag(grid))
                            return true;
                }
                else if (this is IFlagTeamObjectiveGamemode obj2)
                {
                    GridObject[]? grid = obj2.ObjectiveTeam1?.ZoneData?.Item?.Data.GridObjects;
                    if (grid is { Length: > 0 })
                        if (CheckFlag(grid))
                            return true;
                    grid = obj2.ObjectiveTeam2?.ZoneData?.Item?.Data.GridObjects;
                    if (grid is { Length: > 0 })
                        if (CheckFlag(grid))
                            return true;
                }
                break;
        }
        return false;
        bool CheckFlag(GridObject[] grid)
        {
            if (grid == null) return false;
            GameObject go = obj.gameObject;
            for (int g = 0; g < grid.Length; ++g)
            {
                GridObject @object = grid[g];
                GameObject? obj2 = @object.Object?.transform.gameObject;
                if (obj2 == go)
                    return true;
            }

            return false;
        }
    }
    protected virtual void OnObjectiveChangedPowerHandler(Flag? oldObj, Flag? newObj)
    {
        if (!Data.UseElectricalGrid) return;
        if (ElectricalGridBehavior != ElectricalGridBehaivor.EnabledWhenObjective)
            return;
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        GridObject[]? arr = oldObj?.ZoneData?.Item?.Data.GridObjects;
        if (arr is { Length: > 0 })
        {
            SetPowerForAllInGrid(arr, false);
        }
        arr = newObj?.ZoneData?.Item?.Data.GridObjects;
        if (arr is { Length: > 0 })
        {
            SetPowerForAllInGrid(arr, true);
        }

        CheckPowerForAllBarricades();
    }
    protected virtual void OnRotationUpdated()
    {
        if (!Data.UseElectricalGrid) return;
        if (ElectricalGridBehavior != ElectricalGridBehaivor.EnabledWhenInRotation)
            return;
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        SetPowerForAllInGrid(OutOfRotationGridObjects, false);
        SetPowerForAllInGrid(RotationGridObjects, true);

        CheckPowerForAllBarricades();
    }

    protected IEnumerable<GridObject> RotationGridObjects
    {
        get => Rotation
            .SelectMany(x => x?.ZoneData?.Item?.Data.GridObjects ?? Array.Empty<GridObject>())
            .Concat(TeamManager.Team1Main.Data.GridObjects ?? Array.Empty<GridObject>())
            .Concat(TeamManager.Team2Main.Data.GridObjects ?? Array.Empty<GridObject>());
    }
    protected IEnumerable<GridObject> OutOfRotationGridObjects
    {
        get => AllFlags
            .Where(x => !Rotation.Contains(x))
            .SelectMany(x => x?.ZoneData?.Item?.Data.GridObjects ?? Array.Empty<GridObject>());
    }
    protected static void CheckPowerForAllBarricades()
    {
        if (Data.RefreshIsConnectedToPower != null)
        {
            foreach (BarricadeDrop drop in UCBarricadeManager.AllBarricades)
            {
                if (drop.interactable is InteractablePower power)
                    Data.RefreshIsConnectedToPower(power);
            }
        }
    }
    protected static void SetPowerForAllInGrid(IEnumerable<GridObject>? arr, bool state)
    {
        if (!Data.UseElectricalGrid || arr == null) return;
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        foreach (GridObject obj in arr)
        {
            if (obj.Object is { interactable: { } inx } && inx != null && inx.objectAsset.interactability == EObjectInteractability.BINARY_STATE)
            {
                if (inx.objectAsset.interactabilityHint is EObjectInteractabilityHint.SWITCH or EObjectInteractabilityHint.FIRE
                    or EObjectInteractabilityHint.GENERATOR)
                {
                    ObjectManager.forceObjectBinaryState(inx.transform, state);
                }

                Data.RefreshIsConnectedToPower?.Invoke(inx);
            }
        }
    }
    public void OnZoneElectricalGridObjectsUpdated(Zone zone, List<GridObject>? added, List<GridObject>? removed)
    {
        if (!Data.UseElectricalGrid) return;
        if (ElectricalGridBehavior is ElectricalGridBehaivor.EnabledWhenInRotation or ElectricalGridBehaivor.AllEnabled)
        {
            RemoveDuplicatesFromRemoving(RotationGridObjects);
            if (ElectricalGridBehavior == ElectricalGridBehaivor.AllEnabled)
            {
                RemoveDuplicatesFromRemoving(OutOfRotationGridObjects);
            }
        }
        else if (ElectricalGridBehavior == ElectricalGridBehaivor.EnabledWhenObjective)
        {
            if (this is IFlagObjectiveGamemode obj1)
            {
                GridObject[]? grid = obj1.Objective?.ZoneData?.Item?.Data.GridObjects;
                if (grid is { Length: > 0 })
                    RemoveDuplicatesFromRemoving(grid);
            }
            else if (this is IFlagTeamObjectiveGamemode obj2)
            {
                GridObject[]? grid = obj2.ObjectiveTeam1?.ZoneData?.Item?.Data.GridObjects;
                if (grid is { Length: > 0 })
                    RemoveDuplicatesFromRemoving(grid);
                grid = obj2.ObjectiveTeam2?.ZoneData?.Item?.Data.GridObjects;
                if (grid is { Length: > 0 })
                    RemoveDuplicatesFromRemoving(grid);
            }
        }

        void RemoveDuplicatesFromRemoving(IEnumerable<GridObject> grid)
        {
            if (removed is not { Count: > 0 }) return;
            foreach (GridObject obj in grid)
            {
                int ind = removed.FindIndex(x => x.ObjectInstanceId == obj.ObjectInstanceId);
                if (ind != -1)
                    removed.RemoveAtFast(ind);
            }
        }
        if (zone.Data.UseCase is ZoneUseCase.Team1Main or ZoneUseCase.Team2Main or ZoneUseCase.Team1MainCampZone or ZoneUseCase.Team2MainCampZone)
        {
            SetPowerForAllInGrid(added, true);
            SetPowerForAllInGrid(removed, false);
            return;
        }
        if (ElectricalGridBehavior == ElectricalGridBehaivor.Disabled)
            return;
        Flag? flag = AllFlags.FirstOrDefault(x => x.ZoneData is not null && x.ZoneData.LastPrimaryKey == zone.PrimaryKey);

        if (flag != null)
        {
            switch (ElectricalGridBehavior)
            {
                case ElectricalGridBehaivor.AllEnabled:
                    SetPowerForAllInGrid(added, true);
                    SetPowerForAllInGrid(removed, false);
                    break;
                case ElectricalGridBehaivor.EnabledWhenInRotation:
                    if (Rotation.Contains(flag))
                    {
                        SetPowerForAllInGrid(added, true);
                        SetPowerForAllInGrid(removed, false);
                    }

                    break;
                case ElectricalGridBehaivor.EnabledWhenObjective:
                    if (flag.IsAnObj)
                    {
                        SetPowerForAllInGrid(added, true);
                        SetPowerForAllInGrid(removed, false);
                    }

                    break;
            }
        }
    }
    public enum ElectricalGridBehaivor : byte
    {
        Disabled,
        AllEnabled,
        EnabledWhenObjective,
        EnabledWhenInRotation
    }
}

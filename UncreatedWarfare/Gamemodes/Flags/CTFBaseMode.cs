using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Warfare.Actions;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Revives;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Traits;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using static Uncreated.Warfare.Gamemodes.Flags.UI.CaptureUI;
using XPReward = Uncreated.Warfare.Levels.XPReward;

namespace Uncreated.Warfare.Gamemodes.Flags;

public delegate void ObjectiveChangedDelegate(Flag oldObjective, Flag newObjective, ulong team, int oldObjectiveIndex, int newObjectiveIndex);
public delegate void FlagCapturedHandler(Flag flag, ulong capturedTeam, ulong lostTeam);
public delegate void FlagNeutralizedHandler(Flag flag, ulong capturedTeam, ulong lostTeam);
public abstract class CTFBaseMode<Leaderboard, Stats, StatTracker, TTicketProvider> :
    TicketFlagGamemode<TTicketProvider>,
    IFlagTeamObjectiveGamemode,
    IVehicles,
    IFOBs,
    IKitRequests,
    IRevives,
    ISquads,
    IImplementsLeaderboard<Stats, StatTracker>,
    IStagingPhase,
    IGameStats,
    ITraits
    where Leaderboard : BaseCTFLeaderboard<Stats, StatTracker>
    where Stats : BaseCTFStats
    where StatTracker : BaseCTFTracker<Stats>
    where TTicketProvider : BaseCTFTicketProvider, new()
{
    // vars
    protected int _objectiveT1Index;
    protected int _objectiveT2Index;
    protected VehicleSpawner _vehicleSpawner;
    protected VehicleBay _vehicleBay;
    protected FOBManager _FOBManager;
    protected KitManager _kitManager;
    protected ReviveManager _reviveManager;
    protected SquadManager _squadManager;
    protected StructureSaver _structureSaver;
    protected TraitManager _traitManager;
    protected ActionManager _actionManager;
    protected Leaderboard? _endScreen;
    private StatTracker _gameStats;
    protected Transform? _blockerBarricadeT1 = null;
    protected Transform? _blockerBarricadeT2 = null;
    private bool _isScreenUp;
    public int ObjectiveT1Index => _objectiveT1Index;
    public int ObjectiveT2Index => _objectiveT2Index;
    public Flag? ObjectiveTeam1 => _objectiveT1Index >= 0 && _objectiveT1Index < FlagRotation.Count ? FlagRotation[_objectiveT1Index] : null;
    public Flag? ObjectiveTeam2 => _objectiveT2Index >= 0 && _objectiveT2Index < FlagRotation.Count ? FlagRotation[_objectiveT2Index] : null;
    public override bool EnableAMC => true;
    public override bool ShowOFPUI => true;
    public override bool ShowXPUI => true;
    public override bool TransmitMicWhileNotActive => true;
    public override bool UseTeamSelector => true;
    public override bool UseWhitelist => true;
    public override bool AllowCosmetics => UCWarfare.Config.AllowCosmetics;
    public VehicleSpawner VehicleSpawner => _vehicleSpawner;
    public VehicleBay VehicleBay => _vehicleBay;
    public FOBManager FOBManager => _FOBManager;
    public KitManager KitManager => _kitManager;
    public ReviveManager ReviveManager => _reviveManager;
    public SquadManager SquadManager => _squadManager;
    public StructureSaver StructureSaver => _structureSaver;
    public TraitManager TraitManager => _traitManager;
    public ActionManager ActionManager => _actionManager;
    ILeaderboard<Stats, StatTracker>? IImplementsLeaderboard<Stats, StatTracker>.Leaderboard => _endScreen;
    ILeaderboard? IImplementsLeaderboard.Leaderboard => _endScreen;
    public bool IsScreenUp => _isScreenUp;
    StatTracker IImplementsLeaderboard<Stats, StatTracker>.WarstatsTracker { get => _gameStats; set => _gameStats = value; }
    IStatTracker IGameStats.GameStats => ((IImplementsLeaderboard<Stats, StatTracker>)this).WarstatsTracker;
    protected CTFBaseMode(string name, float timing) : base(name, timing)
    {

    }
    protected override Task PreInit(CancellationToken token)
    {
        AddSingletonRequirement(ref _vehicleSpawner);
        AddSingletonRequirement(ref _vehicleBay);
        AddSingletonRequirement(ref _FOBManager);
        AddSingletonRequirement(ref _kitManager);
        AddSingletonRequirement(ref _reviveManager);
        AddSingletonRequirement(ref _squadManager);
        AddSingletonRequirement(ref _structureSaver);
        AddSingletonRequirement(ref _traitManager);
        if (UCWarfare.Config.EnableActionMenu)
            AddSingletonRequirement(ref _actionManager);
        return base.PreInit(token);
    }
    protected override bool TimeToEvaluatePoints() => EveryXSeconds(Config.AASFlagTickSeconds);
    public override Task DeclareWin(ulong winner, CancellationToken token)
    {
        ThreadUtil.assertIsGameThread();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        StartCoroutine(EndGameCoroutine(winner));
        return base.DeclareWin(winner, token);
    }
    private IEnumerator<WaitForSeconds> EndGameCoroutine(ulong winner)
    {
        yield return new WaitForSeconds(Config.GeneralLeaderboardDelay);

#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ReplaceBarricadesAndStructures();
        Commands.ClearCommand.WipeVehicles();
        Commands.ClearCommand.ClearItems();

        _endScreen = UCWarfare.I.gameObject.AddComponent<Leaderboard>();
        _endScreen.OnLeaderboardExpired += OnShouldStartNewGame;
        _endScreen.SetShutdownConfig(ShouldShutdownAfterGame, ShutdownMessage);
        _isScreenUp = true;
        _endScreen.StartLeaderboard(winner, _gameStats);
    }
    private void OnShouldStartNewGame()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (_endScreen != null)
        {
            _endScreen.OnLeaderboardExpired -= OnShouldStartNewGame;
            Destroy(_endScreen);
            _endScreen = null!;
        }
        _isScreenUp = false;
        UCWarfare.RunTask(EndGame, UCWarfare.UnloadCancel, ctx: "Starting next gamemode.");
    }
    protected override Task PostGameStarting(bool isOnLoad, CancellationToken token)
    {
        using CombinedTokenSources tokens = token.CombineTokensIfNeeded(UnloadToken);
        ThreadUtil.assertIsGameThread();
        _gameStats.Reset();
        CTFUI.ClearCaptureUI();
        RallyManager.WipeAllRallies();
        return base.PostGameStarting(isOnLoad, token);
    }
    protected void LoadFlagsIntoRotation()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ResetFlags();
        OnFlagDict.Clear();

        do
        {
            FlagRotation.Clear();
            if (!ObjectivePathing.TryPath(FlagRotation))
            {
                L.LogError("Failed to path...");
                throw new InvalidOperationException("Invalid pathing data entered.");
            }
        }
        while (FlagRotation.Count > CTFUI.ListUI.Rows.Length);
    }
    public override void LoadRotation()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (AllFlags == null || AllFlags.Count == 0) return;
        try
        {
            LoadFlagsIntoRotation();
        }
        catch (InvalidOperationException ex)
        {
            FlagRotation.Clear();
            L.LogError(ex);
        }
        if (FlagRotation.Count < 1)
        {
            L.LogError("No flags were put into rotation!!");
        }
        _objectiveT1Index = 0;
        _objectiveT2Index = FlagRotation.Count - 1;
        if (Config.AASDiscoveryForesight < 1)
        {
            L.LogWarning("Discovery Foresight is set to 0 in Flag Settings. The players can not see their next flags.");
        }
        else
        {
            for (int i = 0; i < Config.AASDiscoveryForesight; i++)
            {
                if (i >= FlagRotation.Count || i < 0) break;
                FlagRotation[i].Discover(1);
            }
            for (int i = FlagRotation.Count - 1; i > FlagRotation.Count - 1 - Config.AASDiscoveryForesight; i--)
            {
                if (i >= FlagRotation.Count || i < 0) break;
                FlagRotation[i].Discover(2);
            }
        }
        foreach (Flag flag in FlagRotation)
        {
            InitFlag(flag); //subscribe to abstract events.
        }
        foreach (UCPlayer player in PlayerManager.OnlinePlayers)
        {
            CTFUI.ClearFlagList(player);
            CTFUI.SendFlagList(player);
        }
        PrintFlagRotation();
        EvaluatePoints();
    }
    public override void PrintFlagRotation()
    {
        L.Log("Team 1 objective: " + (ObjectiveTeam1?.Name ?? "null") + ", Team 2 objective: " + (ObjectiveTeam2?.Name ?? "null"), ConsoleColor.Green);
        base.PrintFlagRotation();
    }
    protected virtual void InvokeOnObjectiveChanged(Flag oldFlag, Flag newFlag, ulong team, int oldObj, int newObj)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (team != 0)
        {
            if (_gameStats != null)
                _gameStats.flagOwnerChanges++;
            L.Log("Team 1 objective: " + (ObjectiveTeam1?.Name ?? "null") + ", Team 2 objective: " + (ObjectiveTeam2?.Name ?? "null"), ConsoleColor.Green);
            if (Config.AASDiscoveryForesight > 0)
            {
                if (team == 1)
                {
                    for (int i = newFlag.Index; i < newFlag.Index + Config.AASDiscoveryForesight; i++)
                    {
                        if (i >= FlagRotation.Count || i < 0) break;
                        FlagRotation[i].Discover(1);
                        if (this is Invasion.Invasion)
                            Invasion.InvasionUI.ReplicateFlagUpdate(FlagRotation[i]);
                        else
                            CTFUI.ReplicateFlagUpdate(FlagRotation[i]);
                    }
                }
                else if (team == 2)
                {
                    for (int i = newFlag.Index; i > newFlag.Index - Config.AASDiscoveryForesight; i--)
                    {
                        if (i >= FlagRotation.Count || i < 0) break;
                        FlagRotation[i].Discover(2);
                        if (this is Invasion.Invasion)
                            Invasion.InvasionUI.ReplicateFlagUpdate(FlagRotation[i]);
                        else
                            CTFUI.ReplicateFlagUpdate(FlagRotation[i]);
                    }
                }
            }

            OnObjectiveChangedPowerHandler(oldFlag, newFlag);
        }
    }
    protected override void EventLoopAction()
    {
        base.EventLoopAction();

        if (EveryXSeconds(20f))
        {
            float mult = GetCaptureXPMultiplier();
            for (int i = 0; i < FlagRotation.Count; i++)
            {
                Flag flag = FlagRotation[i];
                if (flag.LastDeltaPoints > 0 && flag.Owner != 1)
                {
                    for (int j = 0; j < flag.PlayersOnFlagTeam1.Count; j++)
                        Points.AwardXP(flag.PlayersOnFlagTeam1[j], XPReward.AttackingFlag, mult);
                }
                else if (flag.LastDeltaPoints < 0 && flag.Owner != 2)
                {
                    for (int j = 0; j < flag.PlayersOnFlagTeam2.Count; j++)
                        Points.AwardXP(flag.PlayersOnFlagTeam2[j], XPReward.AttackingFlag, mult);
                }
                else if (flag.Owner == 1 && flag.IsObj(2) && flag.Team2TotalCappers == 0)
                {
                    for (int j = 0; j < flag.PlayersOnFlagTeam1.Count; j++)
                        Points.AwardXP(flag.PlayersOnFlagTeam1[j], XPReward.DefendingFlag, mult);
                }
                else if (flag.Owner == 2 && flag.IsObj(1) && flag.Team1TotalCappers == 0)
                {
                    for (int j = 0; j < flag.PlayersOnFlagTeam2.Count; j++)
                        Points.AwardXP(flag.PlayersOnFlagTeam2[j], XPReward.DefendingFlag, mult);
                }
            }
        }
    }

    protected virtual float GetCaptureXPMultiplier() => 1f;
    protected virtual void InvokeOnFlagCaptured(Flag flag, ulong capturedTeam, ulong lostTeam)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        // StatsManager.OnFlagCaptured(flag, capturedTeam, lostTeam);
        for (int i = 0; i < Singletons.Count; ++i)
        {
            if (Singletons[i] is IFlagCapturedListener f)
                f.OnFlagCaptured(flag, capturedTeam, lostTeam);
        }
        List<UCPlayer> playerList = capturedTeam == 1ul ? flag.PlayersOnFlagTeam1 : flag.PlayersOnFlagTeam2;
        if (capturedTeam != 0)
            QuestManager.OnObjectiveCaptured(playerList.Select(x => x.Steam64).ToArray());

        ActionLog.Add(ActionLogType.TeamCapturedObjective, TeamManager.TranslateName(capturedTeam) + " CAPTURED " + flag.Name + " FROM " + TeamManager.TranslateName(lostTeam));
        float mult = GetCaptureXPMultiplier();
        foreach (UCPlayer player in playerList)
        {
            Points.AwardXP(player, XPReward.FlagCaptured, mult);
        }
    }
    protected virtual void InvokeOnFlagNeutralized(Flag flag, ulong capturedTeam, ulong lostTeam)
    {
        if (capturedTeam == 1)
            QuestManager.OnFlagNeutralized(flag.PlayersOnFlagTeam1.Select(x => x.Steam64).ToArray(), capturedTeam);
        else if (capturedTeam == 2)
            QuestManager.OnFlagNeutralized(flag.PlayersOnFlagTeam2.Select(x => x.Steam64).ToArray(), capturedTeam);
        for (int i = 0; i < Singletons.Count; ++i)
        {
            if (Singletons[i] is IFlagNeutralizedListener f)
                f.OnFlagNeutralized(flag, capturedTeam, lostTeam);
        }
        List<UCPlayer> playerList = capturedTeam == 1ul ? flag.PlayersOnFlagTeam1 : flag.PlayersOnFlagTeam2;
        QuestManager.OnFlagNeutralized(playerList.Select(x => x.Steam64).ToArray(), capturedTeam);

        ActionLog.Add(ActionLogType.TeamCapturedObjective, TeamManager.TranslateName(capturedTeam) + " NEUTRALIZED " + flag.Name + " FROM " + TeamManager.TranslateName(lostTeam));
        float mult = GetCaptureXPMultiplier();
        foreach (UCPlayer player in playerList)
            Points.AwardXP(player, XPReward.FlagNeutralized, mult);
    }
    protected override void PlayerEnteredFlagRadius(Flag flag, UCPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        L.LogDebug("Player " + player.Name.PlayerName + " entered flag " + flag.Name, ConsoleColor.White);
        player.SendChat(T.EnteredCaptureRadius, flag);
        UpdateFlag(flag);
    }
    protected override void PlayerLeftFlagRadius(Flag flag, UCPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ITransportConnection channel = player.Connection;
        L.LogDebug("Player " + player.Name.PlayerName + " left flag " + flag.Name, ConsoleColor.White);
        player.SendChat(T.LeftCaptureRadius, flag);
        CTFUI.ClearCaptureUI(channel);
        UpdateFlag(flag);
    }
    private void UpdateFlag(Flag flag)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        CaptureUIParameters t1 = default;
        CaptureUIParameters t2 = default;
        CaptureUIParameters t1V = default;
        CaptureUIParameters t2V = default;
        if (flag.Team1TotalCappers > 0)
            t1 = CTFUI.RefreshStaticUI(1, flag, false);
        if (flag.Team1TotalPlayers - flag.Team1TotalCappers > 0)
            t1V = CTFUI.RefreshStaticUI(1, flag, true);
        if (flag.Team2TotalCappers > 0)
            t2 = CTFUI.RefreshStaticUI(2, flag, false);
        if (flag.Team2TotalPlayers - flag.Team2TotalCappers > 0)
            t2V = CTFUI.RefreshStaticUI(2, flag, true);
        for (int i = 0; i < flag.PlayersOnFlag.Count; i++)
        {
            UCPlayer capper = flag.PlayersOnFlag[i];
            ulong t = capper.GetTeam();
            if (t == 1)
            {
                if (!capper.IsInVehicle)
                    CTFUI.CaptureUI.Send(capper, in t1);
                else
                    CTFUI.CaptureUI.Send(capper, in t1V);
            }
            else if (t == 2)
            {
                if (!capper.IsInVehicle)
                    CTFUI.CaptureUI.Send(capper, in t2);
                else
                    CTFUI.CaptureUI.Send(capper, in t2V);
            }
        }
    }
    protected override void FlagOwnerChanged(ulong oldOwner, ulong newOwner, Flag flag)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (newOwner == 1)
        {
            ActionLog.Add(ActionLogType.TeamCapturedObjective, TeamManager.TranslateName(1));
            if (_objectiveT1Index >= FlagRotation.Count - 1) // if t1 just capped the last flag
            {
                UCWarfare.RunTask(Data.Gamemode.DeclareWin, 1ul, default, ctx: "Lose game, flags fully captured by team 1.");
                _objectiveT1Index = FlagRotation.Count - 1;
                return;
            }
            else
            {
                _objectiveT1Index = flag.Index + 1;
                InvokeOnObjectiveChanged(flag, FlagRotation[_objectiveT1Index], 1, flag.Index, _objectiveT1Index);
                InvokeOnFlagCaptured(flag, 1, oldOwner);
                for (int i = 0; i < flag.PlayersOnFlagTeam1.Count; i++)
                {
                    if (flag.PlayersOnFlagTeam1[i].Player.TryGetPlayerData(out Components.UCPlayerData c) && c.Stats is IFlagStats fg)
                        fg.AddCapture();
                }
            }
        }
        else if (newOwner == 2)
        {
            ActionLog.Add(ActionLogType.TeamCapturedObjective, TeamManager.TranslateName(2));
            if (_objectiveT2Index < 1) // if t2 just capped the last flag
            {
                UCWarfare.RunTask(Data.Gamemode.DeclareWin, 2ul, default, ctx: "Lose game, flags fully captured by team 2.");
                _objectiveT2Index = 0;
                return;
            }
            else
            {

                _objectiveT2Index = flag.Index - 1;
                InvokeOnObjectiveChanged(flag, FlagRotation[_objectiveT2Index], 2, flag.Index, _objectiveT2Index);
                InvokeOnFlagCaptured(flag, 2, oldOwner);
                for (int i = 0; i < flag.PlayersOnFlagTeam2.Count; i++)
                {
                    if (flag.PlayersOnFlagTeam2[i].Player.TryGetPlayerData(out Components.UCPlayerData c) && c.Stats is IFlagStats fg)
                        fg.AddCapture();
                }
            }
        }
        else
        {
            if (oldOwner == 1)
            {
                int oldindex = _objectiveT1Index;
                _objectiveT1Index = flag.Index;
                if (oldindex != flag.Index)
                {
                    InvokeOnObjectiveChanged(FlagRotation[oldindex], flag, 0, oldindex, flag.Index);
                }
                InvokeOnFlagNeutralized(flag, 2, 1);
            }
            else if (oldOwner == 2)
            {
                int oldindex = _objectiveT2Index;
                _objectiveT2Index = flag.Index;
                if (oldindex != flag.Index)
                {
                    InvokeOnObjectiveChanged(FlagRotation[oldindex], flag, 0, oldindex, flag.Index);
                }
                InvokeOnFlagNeutralized(flag, 1, 2);
            }
        }

        UpdateFlag(flag);
        if (newOwner == 0)
        {
            Chat.Broadcast(LanguageSet.OnTeam(1), T.FlagNeutralized, flag);
            Chat.Broadcast(LanguageSet.OnTeam(2), T.FlagNeutralized, flag);
        }
        else
        {
            FactionInfo? info = TeamManager.GetFactionSafe(newOwner);
            Chat.Broadcast(LanguageSet.OnTeam(1), T.TeamCaptured, info!, flag);
            Chat.Broadcast(LanguageSet.OnTeam(2), T.TeamCaptured, info!, flag);
        }
    }
    protected override void FlagPointsChanged(float newPts, float oldPts, Flag flag)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (newPts == 0)
            flag.SetOwner(0);
        UpdateFlag(flag);
    }
    public override void PlayerLeave(UCPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (OnFlagDict.TryGetValue(player.Steam64, out int index))
            FlagRotation[index].RecalcCappers();

        base.PlayerLeave(player);
    }
}

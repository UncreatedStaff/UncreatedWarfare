﻿using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Warfare.Actions;
using Uncreated.Warfare.Events.Players;
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
using Uncreated.Warfare.Tickets;
using Uncreated.Warfare.Traits;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using static Uncreated.Warfare.Gamemodes.Flags.UI.CaptureUI;

namespace Uncreated.Warfare.Gamemodes.Flags;
public sealed partial class Conquest :
    TicketFlagGamemode<TeamCTFTicketProvider>,
    IFlagRotation,
    IVehicles,
    IFOBs,
    IKitRequests,
    IRevives,
    ISquads,
    IImplementsLeaderboard<ConquestStats, ConquestStatTracker>,
    IStagingPhase,
    IGameStats,
    ITraits
{
    private VehicleSpawner _vehicleSpawner;
    private VehicleBay _vehicleBay;
    private FOBManager _fobManager;
    private KitManager _kitManager;
    private ReviveManager _reviveManager;
    private SquadManager _squadManager;
    private StructureSaver _structureSaver;
    private ConquestLeaderboard? _endScreen;
    private ConquestStatTracker _gameStats;
    private TraitManager _traitManager;
    private ActionManager _actionManager;
    private bool _isScreenUp;
    public override bool EnableAMC => true;
    public override bool ShowOFPUI => true;
    public override bool ShowXPUI => true;
    public override bool TransmitMicWhileNotActive => true;
    public override bool UseTeamSelector => true;
    public override bool UseWhitelist => true;
    public override bool AllowCosmetics => UCWarfare.Config.AllowCosmetics;
    public override bool AllowPassengersToCapture => true;
    public VehicleSpawner VehicleSpawner => _vehicleSpawner;
    public VehicleBay VehicleBay => _vehicleBay;
    public FOBManager FOBManager => _fobManager;
    public KitManager KitManager => _kitManager;
    public ReviveManager ReviveManager => _reviveManager;
    public SquadManager SquadManager => _squadManager;
    public StructureSaver StructureSaver => _structureSaver;
    public TraitManager TraitManager => _traitManager;
    public ActionManager ActionManager => _actionManager;
    public ILeaderboard<ConquestStats, ConquestStatTracker>? Leaderboard => _endScreen;
    ILeaderboard? IImplementsLeaderboard.Leaderboard => _endScreen;
    ConquestStatTracker IImplementsLeaderboard<ConquestStats, ConquestStatTracker>.WarstatsTracker { get => _gameStats; set => _gameStats = value; }
    public bool IsScreenUp => _isScreenUp;
    IStatTracker IGameStats.GameStats => _gameStats;
    public override string DisplayName => "Conquest";
    public override GamemodeType GamemodeType => GamemodeType.Conquest;
    public Conquest() : base(nameof(Conquest), Config.AASEvaluateTime) { }
    protected override Task PreInit(CancellationToken token)
    {
        token.CombineIfNeeded(UnloadToken);
        AddSingletonRequirement(ref _vehicleSpawner);
        AddSingletonRequirement(ref _vehicleBay);
        AddSingletonRequirement(ref _fobManager);
        AddSingletonRequirement(ref _kitManager);
        AddSingletonRequirement(ref _reviveManager);
        AddSingletonRequirement(ref _squadManager);
        AddSingletonRequirement(ref _structureSaver);
        AddSingletonRequirement(ref _traitManager);
        if (UCWarfare.Config.EnableActionMenu)
            AddSingletonRequirement(ref _actionManager);
        return base.PreInit(token);
    }
    protected override bool TimeToEvaluatePoints() => EveryXSeconds(Config.ConquestFlagTickSeconds);
    public override bool IsAttackSite(ulong team, Flag flag) => true;
    public override bool IsDefenseSite(ulong team, Flag flag) => true;
    public override Task DeclareWin(ulong winner, CancellationToken token)
    {
        ThreadUtil.assertIsGameThread();

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

        _endScreen = gameObject.AddComponent<ConquestLeaderboard>();
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
            _endScreen = null;
        }
        _isScreenUp = false;
        UCWarfare.RunTask(EndGame, UCWarfare.UnloadCancel, ctx: "Starting next gamemode.");
    }
    protected override Task PostGameStarting(bool isOnLoad, CancellationToken token)
    {
        token.CombineIfNeeded(UnloadToken);
        ThreadUtil.assertIsGameThread();
        _gameStats.Reset();
        CTFUI.ClearCaptureUI();
        RallyManager.WipeAllRallies();
        if (Config.ConquestStagingPhaseSeconds > 0)
        {
            SpawnBlockers();
            StartStagingPhase(Config.ConquestStagingPhaseSeconds);
        }
        return base.PostGameStarting(isOnLoad, token);
    }
    protected override void EndStagingPhase()
    {
        base.EndStagingPhase();
        DestroyBlockers();
    }
    public override void LoadRotation()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (AllFlags == null || AllFlags.Count == 0) throw new InvalidOperationException("Flags have not yet been loaded!");
        IntlLoadRotation();
        if (FlagRotation.Count < 1)
        {
            L.LogError("No flags were put into rotation!!");
            throw new Exception("Error loading Conquest: No flags were loaded.");
        }
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
        {
            UCPlayer player = PlayerManager.OnlinePlayers[i];
            ConquestUI.SendFlagList(player);
        }
        PrintFlagRotation();
        EvaluatePoints();
    }

    protected override void EventLoopAction()
    {
        if (State == State.Active && EveryXSeconds(20f))
        {
            for (int j = 0; j < FlagRotation.Count; ++j)
            {
                Flag flag = FlagRotation[j];
                for (int i = 0; i < flag.PlayersOnFlag.Count; ++i)
                    Points.AwardXP(flag.PlayersOnFlag[i], flag.PlayersOnFlag[i].GetTeam() == flag.Owner ? Levels.XPReward.DefendingFlag : Levels.XPReward.AttackingFlag, 0.75f);
            }
        }
        base.EventLoopAction();
    }

    public override void InitFlag(Flag flag)
    {
        base.InitFlag(flag);
        flag.Discover(1);
        flag.Discover(2);
        flag.IsContestedOverride = ConventionalIsContested;
        flag.EvaluatePointsOverride = EvaluatePoints;
    }
    private void EvaluatePoints(Flag flag, bool overrideInactiveCheck)
    {
        if (State == State.Active || overrideInactiveCheck)
        {
            if (!flag.IsContested(out ulong winner))
            {
                if (winner == 1 || winner == 2)
                {
                    flag.Cap(winner, flag.GetCaptureAmount(Config.ConquestCaptureScale, winner));
                }
            }
            else flag.SetPoints(flag.Points);
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
            for (int i = 0; i < flag.PlayersOnFlagTeam1.Count; i++)
            {
                if (flag.PlayersOnFlagTeam1[i].Player.TryGetPlayerData(out Components.UCPlayerData c) && c.Stats is IFlagStats fg)
                    fg.AddCapture();
            }
        }
        else if (newOwner == 2)
        {
            ActionLog.Add(ActionLogType.TeamCapturedObjective, TeamManager.TranslateName(2));
            for (int i = 0; i < flag.PlayersOnFlagTeam2.Count; i++)
            {
                if (flag.PlayersOnFlagTeam2[i].Player.TryGetPlayerData(out Components.UCPlayerData c) && c.Stats is IFlagStats fg)
                    fg.AddCapture();
            }
        }
        
        UpdateFlag(flag);
        ConquestUI.UpdateFlag(flag);

        if (newOwner == 0)
            OnFlagNeutralized(flag, TeamManager.Other(oldOwner), oldOwner);
        else
            OnFlagCaptured(flag, newOwner, oldOwner);
    }

    private void OnFlagNeutralized(Flag flag, ulong neutralizingTeam, ulong lostTeam)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Chat.Broadcast(T.FlagNeutralized, flag);
        for (int i = 0; i < Singletons.Count; ++i)
        {
            if (Singletons[i] is IFlagNeutralizedListener f)
                f.OnFlagNeutralized(flag, neutralizingTeam, lostTeam);
        }
        List<UCPlayer> playerList = neutralizingTeam == 1ul ? flag.PlayersOnFlagTeam1 : flag.PlayersOnFlagTeam2;
        QuestManager.OnFlagNeutralized(playerList.Select(x => x.Steam64).ToArray(), neutralizingTeam);

        ActionLog.Add(ActionLogType.TeamCapturedObjective, TeamManager.TranslateName(neutralizingTeam) + " NEUTRALIZED " + flag.Name + " FROM " + TeamManager.TranslateName(lostTeam));
        foreach (UCPlayer player in playerList)
        {
            Points.AwardXP(player, Levels.XPReward.FlagNeutralized, 0.25f);
        }
    }
    private void OnFlagCaptured(Flag flag, ulong capturedTeam, ulong lostTeam)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        L.Log("Team " + capturedTeam + " captured " + flag.Name + ".", ConsoleColor.Green);
        if (_gameStats != null)
            _gameStats.flagOwnerChanges++;
        Chat.Broadcast(T.TeamCaptured, TeamManager.GetFactionSafe(capturedTeam)!, flag);
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
        foreach (UCPlayer player in playerList)
        {
            Points.AwardXP(player, Levels.XPReward.FlagCaptured, 0.25f);
        }
    }
    private void UpdateFlag(Flag flag)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        CaptureUIParameters t1 = default;
        CaptureUIParameters t2 = default;
        if (flag.Team1TotalCappers > 0)
            t1 = ConquestUI.ComputeUI(1, flag);
        if (flag.Team2TotalCappers > 0)
            t2 = ConquestUI.ComputeUI(2, flag);
        for (int i = 0; i < flag.PlayersOnFlag.Count; i++)
        {
            UCPlayer capper = flag.PlayersOnFlag[i];
            ulong t = capper.GetTeam();
            if (t == 1)
                CTFUI.CaptureUI.Send(capper, in t1);
            else if (t == 2)
                CTFUI.CaptureUI.Send(capper, in t2);
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
        L.LogDebug("Player " + player.Name.PlayerName + " left flag " + flag.Name, ConsoleColor.White);
        player.SendChat(T.LeftCaptureRadius, flag);
        CTFUI.ClearCaptureUI(player.Connection);
        UpdateFlag(flag);
    }
    protected override void InitUI(UCPlayer player)
    {
        ConquestUI.SendFlagList(player);
    }
    public override void OnGroupChanged(GroupChanged e)
    {
        if (e.NewTeam is > 0 and < 4)
            ConquestUI.SendFlagList(e.Player);
        else
            CTFUI.ClearFlagList(e.Player);
        base.OnGroupChanged(e);
    }
}

[Obsolete]
public class ConquestTicketProvider : BaseTicketProvider, IFlagCapturedListener, IFlagNeutralizedListener
{
    private int _t1Bleed;
    private int _t2Bleed;
    public override void OnGameStarting(bool isOnLoaded)
    {
        Manager.Team1Tickets = Gamemode.Config.ConquestStartingTickets;
        Manager.Team2Tickets = Gamemode.Config.ConquestStartingTickets;
    }
    public override void GetDisplayInfo(ulong team, out string message, out string tickets, out string bleed)
    {
        int intlBld = GetTeamBleed(team);
        tickets = (team switch { 1 => Manager.Team1Tickets, 2 => Manager.Team2Tickets, _ => 0 }).ToString(Data.LocalLocale);
        if (intlBld < 0)
        {
            message = $"{intlBld} per minute".Colorize("eb9898");
            bleed = intlBld.ToString(Data.LocalLocale);
        }
        else
            bleed = message = string.Empty;
    }
    private void UpdateTeamBleeds()
    {
        if (Data.Is(out IFlagRotation fr))
        {
            int t1 = 0, t2 = 0;
            for (int i = 0; i < fr.Rotation.Count; ++i)
            {
                ulong owner = fr.Rotation[i].Owner;
                if (owner == 1)
                    ++t1;
                else if (owner == 2)
                    ++t2;
            }

            if (_t1Bleed != -t2)
            {
                _t1Bleed = -t2;
                Manager.UpdateUI(1ul);
            }
            if (_t2Bleed != -t1)
            {
                _t2Bleed = -t1;
                Manager.UpdateUI(2ul);
            }
        }
    }
    public override int GetTeamBleed(ulong team)
    {
        return team == 1 ? _t1Bleed : (team == 2 ? _t2Bleed : 0);
    }
    public override void OnTicketsChanged(ulong team, int oldValue, int newValue, ref bool updateUI)
    {
        if (oldValue > 0 && newValue <= 0)
            UCWarfare.RunTask(Data.Gamemode.DeclareWin, TeamManager.Other(team), default, ctx: "Lose game, tickets reached 0.");
    }
    public override void Tick()
    {
        if (Data.Gamemode != null && Data.Gamemode.State == State.Active)
        {
            if (Data.Gamemode.EveryXSeconds(Gamemode.Config.ConquestPointCountLowPop * Gamemode.Config.ConquestTicketBleedIntervalPerPoint))
            {
                if (_t1Bleed < 0)
                    Manager.Team1Tickets += _t1Bleed;
                if (_t2Bleed < 0)
                    Manager.Team2Tickets += _t2Bleed;
            }
        }
    }
    public void OnFlagCaptured(Flag flag, ulong newOwner, ulong oldOwner) => UpdateTeamBleeds();
    public void OnFlagNeutralized(Flag flag, ulong newOwner, ulong oldOwner) => UpdateTeamBleeds();
}
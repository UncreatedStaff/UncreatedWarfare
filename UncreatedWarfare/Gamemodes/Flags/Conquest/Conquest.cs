using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Actions;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Revives;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Tickets;
using Uncreated.Warfare.Traits;
using Uncreated.Warfare.Traits.Buffs;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using static Uncreated.Warfare.Gamemodes.Flags.UI.CaptureUI;

namespace Uncreated.Warfare.Gamemodes.Flags;
public sealed partial class Conquest :
    TicketGamemode<ConquestTicketProvider>,
    IFlagRotation,
    IVehicles,
    IFOBs,
    IKitRequests,
    IRevives,
    ISquads,
    IImplementsLeaderboard<ConquestStats, ConquestStatTracker>,
    IStructureSaving,
    IStagingPhase,
    IGameStats,
    ITraits
{
    private VehicleSpawner _vehicleSpawner;
    private VehicleBay _vehicleBay;
    private VehicleSigns _vehicleSigns;
    private FOBManager _FOBManager;
    private RequestSigns _requestSigns;
    private KitManager _kitManager;
    private ReviveManager _reviveManager;
    private SquadManager _squadManager;
    private StructureSaver _structureSaver;
    private ConquestLeaderboard? _endScreen;
    private ConquestStatTracker _gameStats;
    private TraitManager _traitManager;
    private ActionManager _actionManager;
    private bool _isScreenUp = false;
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
    public VehicleSigns VehicleSigns => _vehicleSigns;
    public FOBManager FOBManager => _FOBManager;
    public RequestSigns RequestSigns => _requestSigns;
    public KitManager KitManager => _kitManager;
    public ReviveManager ReviveManager => _reviveManager;
    public SquadManager SquadManager => _squadManager;
    public StructureSaver StructureSaver => _structureSaver;
    public TraitManager TraitManager => _traitManager;
    public ActionManager ActionManager => _actionManager;
    Leaderboard<ConquestStats, ConquestStatTracker>? IImplementsLeaderboard<ConquestStats, ConquestStatTracker>.Leaderboard => _endScreen;
    public bool IsScreenUp => _isScreenUp;
    public ConquestStatTracker WarstatsTracker => _gameStats;
    object IGameStats.GameStats => _gameStats;
    public override string DisplayName => "Conquest";
    public override EGamemode GamemodeType => EGamemode.CONQUEST;
    public Conquest() : base(nameof(Conquest), Config.AASEvaluateTime) { }
    protected override void PreInit()
    {
        AddSingletonRequirement(ref _vehicleSpawner);
        AddSingletonRequirement(ref _vehicleBay);
        AddSingletonRequirement(ref _vehicleSigns);
        AddSingletonRequirement(ref _FOBManager);
        AddSingletonRequirement(ref _kitManager);
        AddSingletonRequirement(ref _reviveManager);
        AddSingletonRequirement(ref _squadManager);
        AddSingletonRequirement(ref _structureSaver);
        AddSingletonRequirement(ref _requestSigns);
        AddSingletonRequirement(ref _traitManager);
        if (UCWarfare.Config.EnableActionMenu)
            AddSingletonRequirement(ref _actionManager);
        base.PreInit();
    }
    protected override void PostInit()
    {
        Commands.ReloadCommand.ReloadKits();
        _gameStats = gameObject.AddComponent<ConquestStatTracker>();
    }
    protected override void OnReady()
    {
        RepairManager.LoadRepairStations();
        RallyManager.WipeAllRallies();
        base.OnReady();
    }
    protected override void PostDispose()
    {
        CTFUI.StagingUI.ClearFromAllPlayers();
        Destroy(_gameStats);
        base.PostDispose();
    }
    protected override void EventLoopAction()
    {
        base.EventLoopAction();

        if (EveryXSeconds(5f))
            FOBManager.Tick();
    }
    private void GetTeamBleed(out int t1Bleed, out int t2Bleed)
    {
        t1Bleed = 0;
        t2Bleed = 0;
        for (int i = 0; i < _rotation.Count; ++i)
        {
            Flag f = _rotation[i];
            if (f.Owner == 1)
                ++t2Bleed;
            else if (f.Owner == 2)
                ++t1Bleed;
        }
    }
    protected override bool TimeToEvaluatePoints() => EveryXSeconds(Config.ConquestFlagTickSeconds);
    public override bool IsAttackSite(ulong team, Flag flag) => true;
    public override bool IsDefenseSite(ulong team, Flag flag) => true;
    public override void DeclareWin(ulong winner)
    {
        base.DeclareWin(winner);
        StartCoroutine(EndGameCoroutine(winner));
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
        _endScreen.OnLeaderboardExpired = OnShouldStartNewGame;
        _endScreen.SetShutdownConfig(shutdownAfterGame, shutdownMessage);
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
            _endScreen.OnLeaderboardExpired = null;
            Destroy(_endScreen);
            _endScreen = null;
        }
        _isScreenUp = false;
        EndGame();
    }
    protected override void PostGameStarting(bool isOnLoad)
    {
        _gameStats.Reset();
        CTFUI.ClearCaptureUI();
        RallyManager.WipeAllRallies();
        SpawnBlockers();
        StartStagingPhase(Config.ConquestStagingPhaseSeconds);
        base.PostGameStarting(isOnLoad);
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
        if (_allFlags == null || _allFlags.Count == 0) throw new InvalidOperationException("Flags have not yet been loaded!");
        IntlLoadRotation();
        if (_rotation.Count < 1)
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
    public override void InitFlag(Flag flag)
    {
        base.InitFlag(flag);
        flag.Discover(1);
        flag.Discover(2);
        flag.IsContestedOverride = IsContested;
        flag.EvaluatePointsOverride = EvaluatePoints;
    }
    private void EvaluatePoints(Flag flag, bool overrideInactiveCheck)
    {
        if (State == EState.ACTIVE || overrideInactiveCheck)
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
    private static bool IsContested(Flag flag, out ulong winner)
    {
        if (flag.Team1TotalCappers == 0 && flag.Team2TotalCappers == 0)
        {
            winner = 0;
            return false;
        }
        else if (flag.Team1TotalCappers == flag.Team2TotalCappers)
            winner = Intimidation.CheckSquadsForContestBoost(flag);
        else if (flag.Team1TotalCappers == 0 && flag.Team2TotalCappers > 0)
            winner = 2;
        else if (flag.Team2TotalCappers == 0 && flag.Team1TotalCappers > 0)
            winner = 1;
        else if (flag.Team1TotalCappers > flag.Team2TotalCappers)
        {
            if (flag.Team1TotalCappers - Config.AASRequiredCapturingPlayerDifference >= flag.Team2TotalCappers)
                winner = 1;
            else
                winner = Intimidation.CheckSquadsForContestBoost(flag);
        }
        else
        {
            if (flag.Team2TotalCappers - Config.AASRequiredCapturingPlayerDifference >= flag.Team1TotalCappers)
                winner = 2;
            else
                winner = Intimidation.CheckSquadsForContestBoost(flag);
        }

        return winner == 0;
    }

    protected override void FlagOwnerChanged(ulong oldOwner, ulong newOwner, Flag flag)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (newOwner == 1)
        {
            ActionLogger.Add(EActionLogType.TEAM_CAPTURED_OBJECTIVE, TeamManager.TranslateName(1, 0));
            for (int i = 0; i < flag.PlayersOnFlagTeam1.Count; i++)
            {
                if (flag.PlayersOnFlagTeam1[i].Player.TryGetPlayerData(out Components.UCPlayerData c) && c.stats is IFlagStats fg)
                    fg.AddCapture();
            }
        }
        else if (newOwner == 2)
        {
            ActionLogger.Add(EActionLogType.TEAM_CAPTURED_OBJECTIVE, TeamManager.TranslateName(2, 0));
            for (int i = 0; i < flag.PlayersOnFlagTeam2.Count; i++)
            {
                if (flag.PlayersOnFlagTeam2[i].Player.TryGetPlayerData(out Components.UCPlayerData c) && c.stats is IFlagStats fg)
                    fg.AddCapture();
            }
        }

        VehicleSigns.OnFlagCaptured();
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
        if (neutralizingTeam == 1)
            QuestManager.OnFlagNeutralized(flag.PlayersOnFlagTeam1.Select(x => x.Steam64).ToArray(), neutralizingTeam);
        else if (neutralizingTeam == 2)
            QuestManager.OnFlagNeutralized(flag.PlayersOnFlagTeam2.Select(x => x.Steam64).ToArray(), neutralizingTeam);
        if (TicketManager.Provider is IFlagNeutralizedListener fnl)
            fnl.OnFlagNeutralized(flag, neutralizingTeam, lostTeam);
    }
    private void OnFlagCaptured(Flag flag, ulong capturedTeam, ulong lostTeam)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        L.Log("Team " + capturedTeam + " captured " + flag.Name + ".", ConsoleColor.Green);
        if (_gameStats != null)
            _gameStats.flagOwnerChanges++;
        string c2 = TeamManager.GetTeamHexColor(capturedTeam);
        Chat.Broadcast(T.TeamCaptured, TeamManager.GetFactionSafe(capturedTeam)!, flag);
        StatsManager.OnFlagCaptured(flag, capturedTeam, lostTeam);
        VehicleSigns.OnFlagCaptured();
        QuestManager.OnObjectiveCaptured((capturedTeam == 1 ? flag.PlayersOnFlagTeam1 : flag.PlayersOnFlagTeam2)
            .Select(x => x.Steam64).ToArray());
        TicketManager.OnFlagCaptured(flag, capturedTeam, lostTeam);
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
            Player capper = flag.PlayersOnFlag[i];
            ulong t = capper.GetTeam();
            if (t == 1)
                CTFUI.CaptureUI.Send(capper, in t1);
            else if (t == 2)
                CTFUI.CaptureUI.Send(capper, in t2);
        }
    }
    protected override void FlagPointsChanged(float NewPoints, float OldPoints, Flag flag)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (NewPoints == 0)
            flag.SetOwner(0);
        UpdateFlag(flag);
    }
    protected override void PlayerEnteredFlagRadius(Flag flag, Player player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ulong team = player.GetTeam();
        L.LogDebug("Player " + player.channel.owner.playerID.playerName + " entered flag " + flag.Name, ConsoleColor.White);
        player.SendChat(T.EnteredCaptureRadius, flag);
        UpdateFlag(flag);
    }
    protected override void PlayerLeftFlagRadius(Flag flag, Player player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ulong team = player.GetTeam();
        L.LogDebug("Player " + player.channel.owner.playerID.playerName + " left flag " + flag.Name, ConsoleColor.White);
        player.SendChat(T.LeftCaptureRadius, flag);
        CTFUI.ClearCaptureUI(player.channel.owner.transportConnection);
        UpdateFlag(flag);
    }
    public override void PlayerInit(UCPlayer player, bool wasAlreadyOnline)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!KitManager.KitExists(player.KitName, out Kit kit) || (!kit.IsLoadout && kit.IsLimited(out int currentPlayers, out int allowedPlayers, player.GetTeam())) || (kit.IsLoadout && kit.IsClassLimited(out currentPlayers, out allowedPlayers, player.GetTeam())))
        {
            if (!KitManager.TryGiveRiflemanKit(player))
                KitManager.TryGiveUnarmedKit(player);
        }
        ulong team = player.GetTeam();
        if (!AllowCosmetics)
            player.SetCosmeticStates(false);

        if (UCWarfare.Config.ModifySkillLevels)
            Skillset.SetDefaultSkills(player);

        StatsManager.RegisterPlayer(player.CSteamID.m_SteamID);
        StatsManager.ModifyStats(player.CSteamID.m_SteamID, s => s.LastOnline = DateTime.Now.Ticks);
        base.PlayerInit(player, wasAlreadyOnline);
    }
    public override void OnJoinTeam(UCPlayer player, ulong team)
    {
        if (team is 1 or 2)
        {
            if (KitManager.KitExists(team == 1 ? TeamManager.Team1UnarmedKit : TeamManager.Team2UnarmedKit, out Kit unarmed))
                KitManager.GiveKit(player, unarmed);
            else if (KitManager.KitExists(TeamManager.DefaultKit, out unarmed)) KitManager.GiveKit(player, unarmed);
            else L.LogWarning("Unable to give " + player.CharacterName + " a kit.");
        }
        else
        {
            if (KitManager.KitExists(TeamManager.DefaultKit, out Kit @default)) KitManager.GiveKit(player, @default);
            else L.LogWarning("Unable to give " + player.CharacterName + " a kit.");
        }
        _gameStats.OnPlayerJoin(player);
        if (IsScreenUp && _endScreen != null)
            _endScreen.OnPlayerJoined(player);
        else
        {
            TicketManager.SendUI(player);
            ConquestUI.SendFlagList(player);
        }
        base.OnJoinTeam(player, team);
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
        tickets = (team switch { 1 => Manager.Team1Tickets, 2 => Manager.Team2Tickets, _ => 0 }).ToString(Data.Locale);
        if (intlBld < 0)
        {
            message = $"{intlBld} per minute".Colorize("eb9898");
            bleed = intlBld.ToString(Data.Locale);
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
            Data.Gamemode.DeclareWin(TeamManager.Other(team));
    }
    public override void Tick()
    {
        if (Data.Gamemode.State == EState.ACTIVE)
        {
            if (Data.Gamemode.EveryXSeconds(Gamemode.Config.ConquestPointCount * Gamemode.Config.ConquestTicketBleedIntervalPerPoint))
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
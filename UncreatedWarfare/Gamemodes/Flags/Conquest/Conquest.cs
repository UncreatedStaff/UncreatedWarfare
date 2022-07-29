using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Revives;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Tickets;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using static Uncreated.Warfare.Gamemodes.Flags.UI.CaptureUI;

namespace Uncreated.Warfare.Gamemodes.Flags;
public partial class Conquest :
    TicketGamemode,
    IFlagRotation,
    IVehicles,
    IFOBs,
    IKitRequests,
    IRevives,
    ISquads,
    IImplementsLeaderboard<ConquestStats, ConquestStatTracker>,
    IStructureSaving,
    IStagingPhase,
    IGameStats
{
    protected VehicleSpawner _vehicleSpawner;
    protected VehicleBay _vehicleBay;
    protected VehicleSigns _vehicleSigns;
    protected FOBManager _FOBManager;
    protected RequestSigns _requestSigns;
    protected KitManager _kitManager;
    protected ReviveManager _reviveManager;
    protected SquadManager _squadManager;
    protected StructureSaver _structureSaver;
    protected ConquestLeaderboard? _endScreen;
    private ConquestStatTracker _gameStats;
    protected Transform? _blockerBarricadeT1 = null;
    protected Transform? _blockerBarricadeT2 = null;
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
    Leaderboard<ConquestStats, ConquestStatTracker>? IImplementsLeaderboard<ConquestStats, ConquestStatTracker>.Leaderboard => _endScreen;
    public bool IsScreenUp => _isScreenUp;
    public ConquestStatTracker WarstatsTracker => _gameStats;
    object IGameStats.GameStats => _gameStats;
    public override string DisplayName => "Conquest";
    public override EGamemode GamemodeType => EGamemode.CONQUEST;
    public Conquest() : base(nameof(Conquest), Config.TeamCTF.EvaluateTime)
    {
    }
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
    protected override void EvaluateTickets()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (_state == EState.ACTIVE)
        {
            if (EveryXSeconds(Config.Conquest.PointCount * 12f))
            {
                GetTeamBleed(out int t1Bleed, out int t2Bleed);

                if (t1Bleed > 0)
                {
                    TicketManager.Team1Tickets -= t1Bleed;
                    TicketManager.UpdateUI(1ul, -t1Bleed);
                }
                if (t2Bleed > 0)
                {
                    TicketManager.Team2Tickets -= t2Bleed;
                    TicketManager.UpdateUI(2ul, -t2Bleed);
                }

                TicketManager.BroadcastUI();
            }
        }
        if (EveryXSeconds(5))
            _FOBManager.Tick();

        base.EvaluateTickets();
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
    protected override bool TimeToCheck() => EveryXSeconds(Config.Conquest.FlagTickSeconds);
    public override bool IsAttackSite(ulong team, Flag flag) => true;
    public override bool IsDefenseSite(ulong team, Flag flag) => true;
    public override void DeclareWin(ulong winner)
    {
        base.DeclareWin(winner);
        StartCoroutine(EndGameCoroutine(winner));
    }
    private IEnumerator<WaitForSeconds> EndGameCoroutine(ulong winner)
    {
        yield return new WaitForSeconds(Config.GeneralConfig.LeaderboardDelay);

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
        StartStagingPhase(Config.Conquest.StagingPhaseSeconds);
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
                    flag.Cap(winner, flag.GetCaptureAmount(Config.Conquest.CaptureScale, winner));
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
            winner = 0;
        else if (flag.Team1TotalCappers == 0 && flag.Team2TotalCappers > 0)
            winner = 2;
        else if (flag.Team2TotalCappers == 0 && flag.Team1TotalCappers > 0)
            winner = 1;
        else if (flag.Team1TotalCappers > flag.Team2TotalCappers)
        {
            if (flag.Team1TotalCappers - Config.TeamCTF.RequiredPlayerDifferenceToCapture >= flag.Team2TotalCappers)
                winner = 1;
            else
                winner = 0;
        }
        else
        {
            if (flag.Team2TotalCappers - Config.TeamCTF.RequiredPlayerDifferenceToCapture >= flag.Team1TotalCappers)
                winner = 2;
            else
                winner = 0;
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
                if (flag.PlayersOnFlagTeam1[i].TryGetPlayerData(out Components.UCPlayerData c) && c.stats is IFlagStats fg)
                    fg.AddCapture();
            }
        }
        else if (newOwner == 2)
        {
            ActionLogger.Add(EActionLogType.TEAM_CAPTURED_OBJECTIVE, TeamManager.TranslateName(2, 0));
            for (int i = 0; i < flag.PlayersOnFlagTeam2.Count; i++)
            {
                if (flag.PlayersOnFlagTeam2[i].TryGetPlayerData(out Components.UCPlayerData c) && c.stats is IFlagStats fg)
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
        foreach (LanguageSet set in Localization.EnumerateLanguageSets())
            Chat.Broadcast(set, "flag_neutralized", flag.Name, flag.TeamSpecificHexColor);
        TicketManager.OnFlagNeutralized(flag, neutralizingTeam, lostTeam);
        if (neutralizingTeam == 1)
            QuestManager.OnFlagNeutralized(flag.PlayersOnFlagTeam1.Select(x => x.channel.owner.playerID.steamID.m_SteamID).ToArray(), neutralizingTeam);
        else if (neutralizingTeam == 2)
            QuestManager.OnFlagNeutralized(flag.PlayersOnFlagTeam2.Select(x => x.channel.owner.playerID.steamID.m_SteamID).ToArray(), neutralizingTeam);
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
        foreach (LanguageSet set in Localization.EnumerateLanguageSets())
            Chat.Broadcast(set, "team_capture", TeamManager.TranslateName(capturedTeam, set.Language), c2, flag.Name, flag.TeamSpecificHexColor);
        StatsManager.OnFlagCaptured(flag, capturedTeam, lostTeam);
        VehicleSigns.OnFlagCaptured();
        QuestManager.OnObjectiveCaptured((capturedTeam == 1 ? flag.PlayersOnFlagTeam1 : flag.PlayersOnFlagTeam2)
            .Select(x => x.channel.owner.playerID.steamID.m_SteamID).ToArray());
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
        player.SendChat("entered_cap_radius", UCWarfare.GetColor(team == 1 ? "entered_cap_radius_team_1" : (team == 2 ? "entered_cap_radius_team_2" : "default")), flag.Name, flag.ColorHex);
        UpdateFlag(flag);
    }
    protected override void PlayerLeftFlagRadius(Flag flag, Player player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ulong team = player.GetTeam();
        L.LogDebug("Player " + player.channel.owner.playerID.playerName + " left flag " + flag.Name, ConsoleColor.White);
        player.SendChat("left_cap_radius", UCWarfare.GetColor(team == 1 ? "left_cap_radius_team_1" : (team == 2 ? "left_cap_radius_team_2" : "default")), flag.Name, flag.ColorHex);
        CTFUI.ClearCaptureUI(player.channel.owner.transportConnection);
        UpdateFlag(flag);
    }
    public override void PlayerInit(UCPlayer player, bool wasAlreadyOnline)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (KitManager.KitExists(player.KitName, out Kit kit))
        {
            if ((!kit.IsLoadout && kit.IsLimited(out int currentPlayers, out int allowedPlayers, player.GetTeam())) || (kit.IsLoadout && kit.IsClassLimited(out currentPlayers, out allowedPlayers, player.GetTeam())))
            {
                if (!KitManager.TryGiveRiflemanKit(player))
                    KitManager.TryGiveUnarmedKit(player);
            }
        }
        ulong team = player.GetTeam();
        if (!AllowCosmetics)
        {
            player.Player.clothing.ServerSetVisualToggleState(EVisualToggleType.COSMETIC, false);
            player.Player.clothing.ServerSetVisualToggleState(EVisualToggleType.MYTHIC, false);
            player.Player.clothing.ServerSetVisualToggleState(EVisualToggleType.SKIN, false);
        }
        if (UCWarfare.Config.ModifySkillLevels)
        {
            player.Player.skills.ServerSetSkillLevel((int)EPlayerSpeciality.OFFENSE, (int)EPlayerOffense.SHARPSHOOTER, 7);
            player.Player.skills.ServerSetSkillLevel((int)EPlayerSpeciality.OFFENSE, (int)EPlayerOffense.PARKOUR, 2);
            player.Player.skills.ServerSetSkillLevel((int)EPlayerSpeciality.OFFENSE, (int)EPlayerOffense.EXERCISE, 1);
            player.Player.skills.ServerSetSkillLevel((int)EPlayerSpeciality.OFFENSE, (int)EPlayerOffense.CARDIO, 5);
            player.Player.skills.ServerSetSkillLevel((int)EPlayerSpeciality.DEFENSE, (int)EPlayerDefense.VITALITY, 5);
        }
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
/*
 * Rotation: FLAG_T1, MID_T1 (adjacent to one of the t1 adjacencies), MID * n-4 (not adjacent to t1 adj, t2 adj), MID_T2 (adjacent to one of the t2 adjacencies), FLAG_T2
 * 
 * 
 * 
 * 
 */
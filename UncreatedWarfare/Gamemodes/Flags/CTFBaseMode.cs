using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Point;
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

public delegate void ObjectiveChangedDelegate(Flag OldFlagObj, Flag NewFlagObj, ulong Team, int OldObj, int NewObj);
public delegate void FlagCapturedHandler(Flag flag, ulong capturedTeam, ulong lostTeam);
public delegate void FlagNeutralizedHandler(Flag flag, ulong capturedTeam, ulong lostTeam);
public abstract class CTFBaseMode<Leaderboard, Stats, StatTracker, TTicketProvider> :
    TicketGamemode<TTicketProvider>,
    IFlagTeamObjectiveGamemode,
    IVehicles,
    IFOBs,
    IKitRequests,
    IRevives,
    ISquads,
    IImplementsLeaderboard<Stats, StatTracker>,
    IStructureSaving,
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
    protected VehicleSigns _vehicleSigns;
    protected FOBManager _FOBManager;
    protected RequestSigns _requestSigns;
    protected KitManager _kitManager;
    protected ReviveManager _reviveManager;
    protected SquadManager _squadManager;
    protected StructureSaver _structureSaver;
    protected TraitManager _traitManager;
    protected Leaderboard? _endScreen;
    private StatTracker _gameStats;
    protected Transform? _blockerBarricadeT1 = null;
    protected Transform? _blockerBarricadeT2 = null;
    private bool _isScreenUp = false;
    public int ObjectiveT1Index => _objectiveT1Index;
    public int ObjectiveT2Index => _objectiveT2Index;
    public Flag? ObjectiveTeam1 => _objectiveT1Index >= 0 && _objectiveT1Index < _rotation.Count ? _rotation[_objectiveT1Index] : null;
    public Flag? ObjectiveTeam2 => _objectiveT2Index >= 0 && _objectiveT2Index < _rotation.Count ? _rotation[_objectiveT2Index] : null;
    public override bool EnableAMC => true;
    public override bool ShowOFPUI => true;
    public override bool ShowXPUI => true;
    public override bool TransmitMicWhileNotActive => true;
    public override bool UseTeamSelector => true;
    public override bool UseWhitelist => true;
    public override bool AllowCosmetics => UCWarfare.Config.AllowCosmetics;
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
    Leaderboard<Stats, StatTracker>? IImplementsLeaderboard<Stats, StatTracker>.Leaderboard => _endScreen;
    public bool IsScreenUp => _isScreenUp;
    public StatTracker WarstatsTracker => _gameStats;
    object IGameStats.GameStats => _gameStats;
    public CTFBaseMode(string name, float timing) : base(name, timing)
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
        AddSingletonRequirement(ref _traitManager);
        base.PreInit();
    }
    protected override void PostInit()
    {
        Commands.ReloadCommand.ReloadKits();
        _gameStats = gameObject.AddComponent<StatTracker>();
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
    protected override bool TimeToCheck()
    {
        if (_counter > Config.TeamCTF.FlagTickInterval)
        {
            _counter = 0;
            return true;
        }
        else
        {
            _counter++;
            return false;
        }
    }
    public override void DeclareWin(ulong winner)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
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

        _endScreen = UCWarfare.I.gameObject.AddComponent<Leaderboard>();
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
            _endScreen = null!;
        }
        _isScreenUp = false;
        EndGame();
    }
    protected override void PostGameStarting(bool isOnLoad)
    {
        _gameStats.Reset();
        CTFUI.ClearCaptureUI();
        RallyManager.WipeAllRallies();
        base.PostGameStarting(isOnLoad);
    }
    protected void LoadFlagsIntoRotation()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ResetFlags();
        _onFlag.Clear();

        do
        {
            _rotation.Clear();
            if (!ObjectivePathing.TryPath(_rotation))
            {
                L.LogError("Failed to path...");
                throw new InvalidOperationException("Invalid pathing data entered.");
            }
            //_rotation = ObjectivePathing.PathWithAdjacents(_allFlags, Config.MapConfig.Team1Adjacencies, Config.MapConfig.Team2Adjacencies);
        }
        while (_rotation.Count > CTFUI.ListUI.Parents.Length);
    }
    public override void LoadRotation()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (_allFlags == null || _allFlags.Count == 0) return;
        LoadFlagsIntoRotation();
        if (_rotation.Count < 1)
        {
            L.LogError("No flags were put into rotation!!");
        }
        _objectiveT1Index = 0;
        _objectiveT2Index = _rotation.Count - 1;
        if (Config.TeamCTF.DiscoveryForesight < 1)
        {
            L.LogWarning("Discovery Foresight is set to 0 in Flag Settings. The players can not see their next flags.");
        }
        else
        {
            for (int i = 0; i < Config.TeamCTF.DiscoveryForesight; i++)
            {
                if (i >= _rotation.Count || i < 0) break;
                _rotation[i].Discover(1);
            }
            for (int i = _rotation.Count - 1; i > _rotation.Count - 1 - Config.TeamCTF.DiscoveryForesight; i--)
            {
                if (i >= _rotation.Count || i < 0) break;
                _rotation[i].Discover(2);
            }
        }
        foreach (Flag flag in _rotation)
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
    protected virtual void InvokeOnObjectiveChanged(Flag OldFlagObj, Flag NewFlagObj, ulong Team, int OldObj, int NewObj)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (Team != 0)
        {
            if (_gameStats != null)
                _gameStats.flagOwnerChanges++;
            L.Log("Team 1 objective: " + (ObjectiveTeam1?.Name ?? "null") + ", Team 2 objective: " + (ObjectiveTeam2?.Name ?? "null"), ConsoleColor.Green);
            if (Config.TeamCTF.DiscoveryForesight > 0)
            {
                if (Team == 1)
                {
                    for (int i = NewFlagObj.index; i < NewFlagObj.index + Config.TeamCTF.DiscoveryForesight; i++)
                    {
                        if (i >= _rotation.Count || i < 0) break;
                        _rotation[i].Discover(1);
                        if (this is Invasion.Invasion)
                            Invasion.InvasionUI.ReplicateFlagUpdate(_rotation[i]);
                        else
                            CTFUI.ReplicateFlagUpdate(_rotation[i]);
                    }
                }
                else if (Team == 2)
                {
                    for (int i = NewFlagObj.index; i > NewFlagObj.index - Config.TeamCTF.DiscoveryForesight; i--)
                    {
                        if (i >= _rotation.Count || i < 0) break;
                        _rotation[i].Discover(2);
                        if (this is Invasion.Invasion)
                            Invasion.InvasionUI.ReplicateFlagUpdate(_rotation[i]);
                        else
                            CTFUI.ReplicateFlagUpdate(_rotation[i]);
                    }
                }
            }
        }
    }
    protected override void EventLoopAction()
    {
        base.EventLoopAction();

        if (EveryXSeconds(5f))
            FOBManager.Tick();

        if (EveryXSeconds(20f))
        {
            for (int i = 0; i < _rotation.Count; i++)
            {
                Flag flag = _rotation[i];
                if (flag.LastDeltaPoints > 0 && flag.Owner != 1)
                {
                    for (int j = 0; j < flag.PlayersOnFlagTeam1.Count; j++)
                        Points.AwardXP(flag.PlayersOnFlagTeam1[j],
                            Points.XPConfig.FlagAttackXP,
                            T.XPToastFlagAttackTick.Translate(flag.PlayersOnFlagTeam1[j].channel.owner.playerID.steamID.m_SteamID));
                }
                else if (flag.LastDeltaPoints < 0 && flag.Owner != 2)
                {
                    for (int j = 0; j < flag.PlayersOnFlagTeam2.Count; j++)
                        Points.AwardXP(flag.PlayersOnFlagTeam2[j],
                            Points.XPConfig.FlagAttackXP,
                            T.XPToastFlagAttackTick.Translate(flag.PlayersOnFlagTeam2[j].channel.owner.playerID.steamID.m_SteamID));
                }
                else if (flag.Owner == 1 && flag.IsObj(2) && flag.Team2TotalCappers == 0)
                {
                    for (int j = 0; j < flag.PlayersOnFlagTeam1.Count; j++)
                        Points.AwardXP(flag.PlayersOnFlagTeam1[j],
                            Points.XPConfig.FlagDefendXP,
                            T.XPToastFlagAttackTick.Translate(flag.PlayersOnFlagTeam1[j].channel.owner.playerID.steamID.m_SteamID));
                }
                else if (flag.Owner == 2 && flag.IsObj(1) && flag.Team1TotalCappers == 0)
                {
                    for (int j = 0; j < flag.PlayersOnFlagTeam2.Count; j++)
                        Points.AwardXP(flag.PlayersOnFlagTeam2[j],
                            Points.XPConfig.FlagDefendXP,
                            T.XPToastFlagAttackTick.Translate(flag.PlayersOnFlagTeam2[j].channel.owner.playerID.steamID.m_SteamID));
                }
            }
        }
    }
    protected virtual void InvokeOnFlagCaptured(Flag flag, ulong capturedTeam, ulong lostTeam)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        VehicleSigns.OnFlagCaptured();
        StatsManager.OnFlagCaptured(flag, capturedTeam, lostTeam);
        TicketManager.OnFlagCaptured(flag, capturedTeam, lostTeam);
        QuestManager.OnObjectiveCaptured((capturedTeam == 1 ? flag.PlayersOnFlagTeam1 : flag.PlayersOnFlagTeam2)
            .Select(x => x.channel.owner.playerID.steamID.m_SteamID).ToArray());
    }
    protected virtual void InvokeOnFlagNeutralized(Flag flag, ulong capturedTeam, ulong lostTeam)
    {
        if (capturedTeam == 1)
            QuestManager.OnFlagNeutralized(flag.PlayersOnFlagTeam1.Select(x => x.channel.owner.playerID.steamID.m_SteamID).ToArray(), capturedTeam);
        else if (capturedTeam == 2)
            QuestManager.OnFlagNeutralized(flag.PlayersOnFlagTeam2.Select(x => x.channel.owner.playerID.steamID.m_SteamID).ToArray(), capturedTeam);
        if (TicketManager.Provider is IFlagNeutralizedListener fnl)
            fnl.OnFlagNeutralized(flag, capturedTeam, lostTeam);
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
        ITransportConnection channel = player.channel.owner.transportConnection;
        ulong team = player.GetTeam();
        L.LogDebug("Player " + player.channel.owner.playerID.playerName + " left flag " + flag.Name, ConsoleColor.White);
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
        CaptureUIParameters t1v = default;
        CaptureUIParameters t2v = default;
        if (flag.Team1TotalCappers > 0)
            t1 = CTFUI.RefreshStaticUI(1, flag, false);
        if (flag.Team1TotalPlayers - flag.Team1TotalCappers > 0)
            t1v = CTFUI.RefreshStaticUI(1, flag, true);
        if (flag.Team2TotalCappers > 0)
            t2 = CTFUI.RefreshStaticUI(2, flag, false);
        if (flag.Team2TotalPlayers - flag.Team2TotalCappers > 0)
            t2v = CTFUI.RefreshStaticUI(2, flag, true);
        for (int i = 0; i < flag.PlayersOnFlag.Count; i++)
        {
            Player capper = flag.PlayersOnFlag[i];
            ulong t = capper.GetTeam();
            if (t == 1)
            {
                if (capper.movement.getVehicle() == null)
                    CTFUI.CaptureUI.Send(capper, in t1);
                else
                    CTFUI.CaptureUI.Send(capper, in t1v);
            }
            else if (t == 2)
            {
                if (capper.movement.getVehicle() == null)
                    CTFUI.CaptureUI.Send(capper, in t2);
                else
                    CTFUI.CaptureUI.Send(capper, in t2v);
            }
        }
    }
    protected override void FlagOwnerChanged(ulong lastOwner, ulong newOwner, Flag flag)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (newOwner == 1)
        {
            ActionLogger.Add(EActionLogType.TEAM_CAPTURED_OBJECTIVE, TeamManager.TranslateName(1, 0));
            if (_objectiveT1Index >= _rotation.Count - 1) // if t1 just capped the last flag
            {
                DeclareWin(1);
                _objectiveT1Index = _rotation.Count - 1;
                return;
            }
            else
            {
                _objectiveT1Index = flag.index + 1;
                InvokeOnObjectiveChanged(flag, _rotation[_objectiveT1Index], 1, flag.index, _objectiveT1Index);
                InvokeOnFlagCaptured(flag, 1, lastOwner);
                for (int i = 0; i < flag.PlayersOnFlagTeam1.Count; i++)
                {
                    if (flag.PlayersOnFlagTeam1[i].TryGetPlayerData(out Components.UCPlayerData c) && c.stats is IFlagStats fg)
                        fg.AddCapture();
                }
            }
        }
        else if (newOwner == 2)
        {
            ActionLogger.Add(EActionLogType.TEAM_CAPTURED_OBJECTIVE, TeamManager.TranslateName(2, 0));
            if (_objectiveT2Index < 1) // if t2 just capped the last flag
            {
                DeclareWin(2);
                _objectiveT2Index = 0;
                return;
            }
            else
            {

                _objectiveT2Index = flag.index - 1;
                InvokeOnObjectiveChanged(flag, _rotation[_objectiveT2Index], 2, flag.index, _objectiveT2Index);
                InvokeOnFlagCaptured(flag, 2, lastOwner);
                for (int i = 0; i < flag.PlayersOnFlagTeam2.Count; i++)
                {
                    if (flag.PlayersOnFlagTeam2[i].TryGetPlayerData(out Components.UCPlayerData c) && c.stats is IFlagStats fg)
                        fg.AddCapture();
                }
            }
        }
        if (lastOwner == 1)
        {
            int oldindex = _objectiveT1Index;
            _objectiveT1Index = flag.index;
            if (oldindex != flag.index)
            {
                InvokeOnObjectiveChanged(_rotation[oldindex], flag, 0, oldindex, flag.index);
                InvokeOnFlagNeutralized(flag, 2, 1);
            }
        }
        else if (lastOwner == 2)
        {
            int oldindex = _objectiveT2Index;
            _objectiveT2Index = flag.index;
            if (oldindex != flag.index)
            {
                InvokeOnObjectiveChanged(_rotation[oldindex], flag, 0, oldindex, flag.index);
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
    protected override void FlagPointsChanged(float NewPoints, float OldPoints, Flag flag)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (NewPoints == 0)
            flag.SetOwner(0);
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
        {
            _endScreen.OnPlayerJoined(player);
        }
        else
        {
            TicketManager.SendUI(player);
            InitUI(player);
        }
        base.OnJoinTeam(player, team);
    }

    protected abstract void InitUI(UCPlayer player);
    public override void PlayerLeave(UCPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (_onFlag.TryGetValue(player.Steam64, out int id))
        {
            for (int i = 0; i < _rotation.Count; i++)
            {
                if (_rotation[i].ID == id)
                {
                    _rotation[i].RecalcCappers();
                    break;
                }
            }
        }
        StatsCoroutine.previousPositions.Remove(player.Player.channel.owner.playerID.steamID.m_SteamID);
        _reviveManager.OnPlayerDisconnected(player.Player.channel.owner);
        StatsManager.DeregisterPlayer(player.CSteamID.m_SteamID);
        base.PlayerLeave(player);
    }
}

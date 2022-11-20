using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Actions;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Revives;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Tickets;
using Uncreated.Warfare.Traits;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Flags.Hardpoint;
public sealed class Hardpoint : TicketFlagGamemode<HardpointTicketProvider>,
    IFlagObjectiveGamemode,
    IFlagTeamObjectiveGamemode,
    IFOBs,
    IVehicles,
    IKitRequests,
    IRevives,
    ISquads,
    IImplementsLeaderboard<HardpointPlayerStats, HardpointTracker>,
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
    private HardpointTracker _gameStats;
    private HardpointLeaderboard? _endScreen;
    private TraitManager _traitManager;
    private ActionManager _actionManager;
    public int _objIndex = -1;
    private float nextObjectivePickTime;
    private ulong objectiveOwner;
    private bool _isScreenUp;
    public override EGamemode GamemodeType => EGamemode.HARDPOINT;
    public override string DisplayName => "Hardpoint";
    public override bool EnableAMC => true;
    public override bool ShowOFPUI => true;
    public override bool ShowXPUI => true;
    public override bool TransmitMicWhileNotActive => true;
    public override bool UseTeamSelector => true;
    public override bool UseWhitelist => true;
    public override bool AllowCosmetics => UCWarfare.Config.AllowCosmetics;
    public Flag Objective => _rotation[_objIndex];
    Flag? IFlagObjectiveGamemode.Objective => _objIndex < 0 || _objIndex >= _rotation.Count ? null : _rotation[_objIndex];
    public int ObjectiveIndex => _objIndex;
    public bool IsScreenUp => _isScreenUp;
    public HardpointTracker WarstatsTracker => _gameStats;
    HardpointTracker IImplementsLeaderboard<HardpointPlayerStats, HardpointTracker>.WarstatsTracker { get => _gameStats; set => _gameStats = value; }
    Leaderboard<HardpointPlayerStats, HardpointTracker>? IImplementsLeaderboard<HardpointPlayerStats, HardpointTracker>.Leaderboard => _endScreen;
    object IGameStats.GameStats => _gameStats;
    /// <summary>0 = clear, 1 = t1, 2 = t2, 3 = contested</summary>
    public ulong ObjectiveState => objectiveOwner;
    Flag? IFlagTeamObjectiveGamemode.ObjectiveTeam1 => Objective;
    Flag? IFlagTeamObjectiveGamemode.ObjectiveTeam2 => Objective;
    int IFlagTeamObjectiveGamemode.ObjectiveT1Index => _objIndex;
    int IFlagTeamObjectiveGamemode.ObjectiveT2Index => _objIndex;
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
    public Hardpoint() : base(nameof(Hardpoint), 0.25f) { }
    protected override Task PreInit()
    {
        _objIndex = -1;
        AddSingletonRequirement(ref _squadManager);
        AddSingletonRequirement(ref _kitManager);
        AddSingletonRequirement(ref _vehicleSpawner);
        AddSingletonRequirement(ref _reviveManager);
        AddSingletonRequirement(ref _vehicleBay);
        AddSingletonRequirement(ref _FOBManager);
        AddSingletonRequirement(ref _structureSaver);
        AddSingletonRequirement(ref _vehicleSigns);
        AddSingletonRequirement(ref _requestSigns);
        AddSingletonRequirement(ref _traitManager);
        if (UCWarfare.Config.EnableActionMenu)
            AddSingletonRequirement(ref _actionManager);
        return base.PreInit();
    }
    public override void LoadRotation()
    {
        if (_allFlags == null || _allFlags.Count == 0)
            throw new InvalidOperationException("No flags are loaded to path with.");

        LoadFlagsIntoRotation();
        if (_rotation.Count < 1)
        {
            L.LogError("No flags were put into rotation!!");
        }

        PickObjective(true);
        Chat.Broadcast(T.HardpointFirstObjective, Objective, nextObjectivePickTime - Time.realtimeSinceStartup);

        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            SendListUI(PlayerManager.OnlinePlayers[i]);
    }
    protected override void EventLoopAction()
    {
        if (State == EState.ACTIVE && nextObjectivePickTime < Time.realtimeSinceStartup)
        {
            PickObjective(false);
            Chat.Broadcast(T.HardpointObjectiveChanged, Objective, nextObjectivePickTime - Time.realtimeSinceStartup);
        }
        base.EventLoopAction();
    }
    private void PickObjective(bool first)
    {
        float oldTime = nextObjectivePickTime;
        float v = 0f;
        if (Config.HardpointObjectiveChangeTimeTolerance != 0)
        {
            v = QuestJsonEx.RoundNumber(0f, 5, UnityEngine.Random.Range(0, Mathf.Abs(Config.HardpointObjectiveChangeTimeTolerance)));
            if (UnityEngine.Random.value < 0.5f)
                v = -v;
        }
        nextObjectivePickTime = Time.realtimeSinceStartup + Config.HardpointObjectiveChangeTime + v;
        if (State == EState.STAGING)
            nextObjectivePickTime += StagingSeconds;
        objectiveOwner = 0ul;
        int old = _objIndex;
        int highest = _rotation.Count;
        if (!first)
            --highest;
        int newobj = UnityEngine.Random.Range(0, highest);
        if (!first && newobj >= old)
            ++newobj;
        if (newobj < 0 || newobj >= _rotation.Count)
        {
            L.LogError("Failed to pick a valid objective!!! (picked " + newobj + " out of " + _rotation.Count + " loaded flags).");
            if (first)
                throw new SingletonLoadException(ESingletonLoadType.LOAD, this, "Failed to pick a valid objective.");
            else
            {
                nextObjectivePickTime = oldTime;
                return;
            }
        }
        _objIndex = newobj;
        if (!first)
        {
            StopUsingPoint(_rotation[old]);
            UpdateListUI(old);
            UpdateListUI(newobj);
            TicketManager.UpdateUI();
        }
    }
    private void StopUsingPoint(Flag flag)
    {
        flag.RecalcCappers();
        for (int i = flag.PlayersOnFlag.Count - 1; i >= 0; --i)
        {
            UCPlayer pl = flag.PlayersOnFlag[i];
            RemovePlayerFromFlag(pl.Steam64, pl.Player, flag);
            flag.ExitPlayer(pl.Player);
        }

        flag.PlayersOnFlag.Clear();
        flag.PlayersOnFlagTeam1.Clear();
        flag.PlayersOnFlagTeam2.Clear();
    }
    private void LoadFlagsIntoRotation()
    {
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
        }
        while (_rotation.Count > Config.HardpointFlagAmount + Config.HardpointFlagTolerance && _rotation.Count < Config.HardpointFlagAmount - Config.HardpointFlagTolerance);
    }
    private void OnObjectiveStateUpdated(ulong oldState)
    {
        UpdateListUI(_objIndex);
        switch (objectiveOwner)
        {
            case 1ul:
            case 2ul:
                L.LogDebug("Owner Changed: " + objectiveOwner + " for " + Objective.Name + ".");
                FactionInfo faction = TeamManager.GetFaction(objectiveOwner);
                ActionLogger.Add(EActionLogType.TEAM_CAPTURED_OBJECTIVE, Objective.Name + " - " + faction.GetName(L.DEFAULT));
                Chat.Broadcast(T.HardpointObjectiveStateCaptured, Objective, faction);
                break;
            case 3ul:
                L.LogDebug("Contested: " + Objective.Name + ".");
                ActionLogger.Add(EActionLogType.TEAM_CAPTURED_OBJECTIVE, Objective.Name + " - " + "CONTESTED");
                Chat.Broadcast(T.HardpointObjectiveStateContested, Objective);
                break;
            default:
                L.LogDebug("Cleared: " + Objective.Name + ".");
                ActionLogger.Add(EActionLogType.TEAM_CAPTURED_OBJECTIVE, Objective.Name + " - " + "CLEAR");
                if (oldState is 1ul or 2ul)
                    Chat.Broadcast(T.HardpointObjectiveStateLost, Objective, TeamManager.GetFaction(oldState));
                else
                    Chat.Broadcast(T.HardpointObjectiveStateLostContest, Objective);
                break;
        }
    }
    public override void InitFlag(Flag flag)
    {
        flag.EvaluatePointsOverride = EvaluatePointsOverride;
        flag.Discover(1);
        flag.Discover(2);
        base.InitFlag(flag);
    }
    public override void OnJoinTeam(UCPlayer player, ulong team)
    {
        KitManager.TryGiveKitOnJoinTeam(player);
        _gameStats.OnPlayerJoin(player);
        if (IsScreenUp && _endScreen != null)
            _endScreen.OnPlayerJoined(player);
        else
        {
            TicketManager.SendUI(player);
            SendListUI(player);
        }
        base.OnJoinTeam(player, team);
    }
    public override Task DeclareWin(ulong winner)
    {
        _objIndex = -1;
        StartCoroutine(EndGameCoroutine(winner));
        return base.DeclareWin(winner);
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

        _endScreen = gameObject.AddComponent<HardpointLeaderboard>();
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
        Task.Run(EndGame);
    }
    private void EvaluatePointsOverride(Flag flag, bool overrideInactiveCheck) { }
    protected override void PlayerEnteredFlagRadius(Flag flag, Player player) { }
    protected override Task PostGameStarting(bool isOnLoad)
    {
        _gameStats.Reset();
        CTFUI.ClearCaptureUI();
        RallyManager.WipeAllRallies();
        if (Config.HardpointStagingPhaseSeconds > 0)
        {
            SpawnBlockers();
            StartStagingPhase(Config.HardpointStagingPhaseSeconds);
        }
        return base.PostGameStarting(isOnLoad);
    }
    protected override void EndStagingPhase()
    {
        base.EndStagingPhase();
        DestroyBlockers();
    }
    protected override void PlayerLeftFlagRadius(Flag flag, Player player) { }
    protected override void FlagCheck()
    {
        if (_objIndex < 0 || _objIndex >= _rotation.Count || State != EState.ACTIVE)
            return;

        Flag f = this.Objective;
        f.RecalcCappers();
        UpdateObjectiveState();
        if (EveryXSeconds(Config.HardpointFlagTickSeconds))
        {
            if (EnableAMC)
                CheckMainCampZones();
        }
    }
    private void UpdateObjectiveState()
    {
        if (_objIndex < 0 || _objIndex >= _rotation.Count || State != EState.ACTIVE)
            return;
        ulong old = objectiveOwner;
        objectiveOwner = ConventionalIsContested(Objective, out ulong winner) ? 3ul : winner;
        if (old != objectiveOwner)
        {
            OnObjectiveStateUpdated(old);
        }
    }
    protected override void FlagOwnerChanged(ulong OldOwner, ulong NewOwner, Flag flag) { }
    protected override void FlagPointsChanged(float NewPoints, float OldPoints, Flag flag) { }
    public override bool IsAttackSite(ulong team, Flag flag) => flag == Objective;
    public override bool IsDefenseSite(ulong team, Flag flag) => flag == Objective;
    protected override bool TimeToEvaluatePoints() => false;
    public void UpdateListUI(int index)
    {
        if (index < 0) return;
        if (_rotation.Count <= index)
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                if (!pl.HasUIHidden)
                    CTFUI.ListUI.Parents[index].SetVisibility(pl.Connection, false);
            }

            return;
        }
        bool obj = index == _objIndex;
        string s1 = $"<color=#{(obj ? GetObjectiveColor() : UCWarfare.GetColorHex("undiscovered_flag"))}>{_rotation[index].ShortName}</color>";
        string s2 = obj ? $"<color=#{UCWarfare.GetColorHex("attack_icon_color")}>{Config.UIIconAttack}</color>" : string.Empty;
        foreach (LanguageSet set in LanguageSet.All())
        {
            while (set.MoveNext())
            {
                ITransportConnection c = set.Next.Connection;
                CTFUI.ListUI.Names[index].SetText(c, s1);
                CTFUI.ListUI.Icons[index].SetText(c, s2);
                CTFUI.ListUI.Parents[index].SetVisibility(c, true);
            }
        }
    }
    public void SendListUI(UCPlayer player)
    {
        if (player.HasUIHidden) return;
        ITransportConnection c = player.Connection;
        CTFUI.ListUI.SendToPlayer(c);
        CTFUI.ListUI.Header.SetVisibility(c, true);
        CTFUI.ListUI.Header.SetText(c, T.FlagsHeader.Translate(player));
        string c1 = UCWarfare.GetColorHex("undiscovered_flag");
        string c2 = UCWarfare.GetColorHex("attack_icon_color");
        string c3 = GetObjectiveColor();
        for (int i = 0; i < CTFUI.ListUI.Parents.Length; i++)
        {
            if (_rotation.Count <= i)
            {
                CTFUI.ListUI.Parents[i].SetVisibility(c, false);
            }
            else
            {
                CTFUI.ListUI.Parents[i].SetVisibility(c, true);
                Flag flag = _rotation[i];
                if (i == _objIndex)
                {
                    CTFUI.ListUI.Names[i].SetText(c, $"<color=#{c3}>{flag.Name}</color>");
                    CTFUI.ListUI.Icons[i].SetText(c, $"<color=#{c2}>{Config.UIIconAttack}</color>");
                }
                else
                {
                    CTFUI.ListUI.Names[i].SetText(c, $"<color=#{c1}>{flag.Name}</color>");
                }
            }
        }
    }
    private string GetObjectiveColor()
        => objectiveOwner switch
    {
        1 => TeamManager.Team1ColorHex,
        2 => TeamManager.Team2ColorHex,
        3 => UCWarfare.GetColorHex("contested"),
        _ => UCWarfare.GetColorHex("neutral_color")
    };
}

public class HardpointTicketProvider : BaseTicketProvider
{
    public override void GetDisplayInfo(ulong team, out string message, out string tickets, out string bleed)
    {
        tickets = (team switch { 1 => Manager.Team1Tickets, 2 => Manager.Team2Tickets, _ => 0 }).ToString(Data.Locale);
        Flag? obj = (Data.Gamemode as IFlagObjectiveGamemode)?.Objective;
        TranslationFlags flg = (team == 1 
                                   ? TranslationFlags.Team1 
                                   : (team == 2 
                                       ? TranslationFlags.Team2 
                                       : 0)) | TranslationFlags.UnityUI;
        message = obj is null ? string.Empty : ("Objective: " + (obj as ITranslationArgument).Translate(L.DEFAULT, Flag.COLOR_SHORT_NAME_FORMAT, null, ref flg));
        int bld = GetTeamBleed(team);
        bleed = bld == 0 ? string.Empty : bld.ToString(Data.Locale);
    }
    public override int GetTeamBleed(ulong team) => Data.Gamemode is not Hardpoint hp || hp.ObjectiveState >= team ? 0 : -1;
    public override void OnGameStarting(bool isOnLoaded) => Manager.Team1Tickets = Manager.Team2Tickets = Gamemode.Config.HardpointStartingTickets;
    public override void OnTicketsChanged(ulong team, int oldValue, int newValue, ref bool updateUI)
    {
        if (oldValue > 0 && newValue <= 0)
            _ = Data.Gamemode.DeclareWin(TeamManager.Other(team));
    }
    public override void Tick()
    {
        if (Data.Gamemode is not Hardpoint hp) return;
        if (hp.EveryXSeconds(Gamemode.Config.HardpointTicketTickSeconds))
        {
            switch (hp.ObjectiveState)
            {
                case 1ul:
                    --Manager.Team2Tickets;
                    break;
                case 2ul:
                    --Manager.Team1Tickets;
                    break;
                case 0ul:
                    --Manager.Team1Tickets;
                    --Manager.Team2Tickets;
                    break;
            }
        }
    }
}
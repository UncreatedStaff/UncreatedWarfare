using SDG.NetTransport;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Warfare.Actions;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Flags.UI;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Revives;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Squads;
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
    private FOBManager _fobManager;
    private KitManager _kitManager;
    private ReviveManager _reviveManager;
    private SquadManager _squadManager;
    private StructureSaver _structureSaver;
    private HardpointTracker _gameStats;
    private HardpointLeaderboard? _endScreen;
    private TraitManager _traitManager;
    private ActionManager _actionManager;
    private int _objIndex = -1;
    private float _nextObjectivePickTime;
    private ulong _objectiveOwner;
    private bool _isScreenUp;
    public override GamemodeType GamemodeType => GamemodeType.Hardpoint;
    public override string DisplayName => "Hardpoint";
    public override bool EnableAMC => true;
    public override bool ShowOFPUI => true;
    public override bool ShowXPUI => true;
    public override bool TransmitMicWhileNotActive => true;
    public override bool UseTeamSelector => true;
    public override bool UseWhitelist => true;
    public override bool AllowCosmetics => UCWarfare.Config.AllowCosmetics;
    public override bool ConsumeFlagUseCaseZones => false;
    public Flag Objective => FlagRotation[_objIndex];
    Flag? IFlagObjectiveGamemode.Objective => _objIndex < 0 || _objIndex >= FlagRotation.Count ? null : FlagRotation[_objIndex];
    public int ObjectiveIndex => _objIndex;
    public bool IsScreenUp => _isScreenUp;
    public HardpointTracker WarstatsTracker => _gameStats;
    HardpointTracker IImplementsLeaderboard<HardpointPlayerStats, HardpointTracker>.WarstatsTracker { get => _gameStats; set => _gameStats = value; }
    public ILeaderboard<HardpointPlayerStats, HardpointTracker>? Leaderboard => _endScreen;
    ILeaderboard? IImplementsLeaderboard.Leaderboard => _endScreen;
    IStatTracker IGameStats.GameStats => _gameStats;
    /// <summary>0 = clear, 1 = t1, 2 = t2, 3 = contested</summary>
    public ulong ObjectiveState => _objectiveOwner;
    Flag IFlagTeamObjectiveGamemode.ObjectiveTeam1 => Objective;
    Flag IFlagTeamObjectiveGamemode.ObjectiveTeam2 => Objective;
    int IFlagTeamObjectiveGamemode.ObjectiveT1Index => _objIndex;
    int IFlagTeamObjectiveGamemode.ObjectiveT2Index => _objIndex;
    public VehicleSpawner VehicleSpawner => _vehicleSpawner;
    public VehicleBay VehicleBay => _vehicleBay;
    public FOBManager FOBManager => _fobManager;
    public KitManager KitManager => _kitManager;
    public ReviveManager ReviveManager => _reviveManager;
    public SquadManager SquadManager => _squadManager;
    public StructureSaver StructureSaver => _structureSaver;
    public TraitManager TraitManager => _traitManager;
    public ActionManager ActionManager => _actionManager;
    public Hardpoint() : base(nameof(Hardpoint), 0.25f) { }
    protected override Task PreInit(CancellationToken token)
    {
        using CombinedTokenSources tokens = token.CombineTokensIfNeeded(UnloadToken);
        _objIndex = -1;
        AddSingletonRequirement(ref _squadManager);
        AddSingletonRequirement(ref _kitManager);
        AddSingletonRequirement(ref _vehicleSpawner);
        AddSingletonRequirement(ref _reviveManager);
        AddSingletonRequirement(ref _vehicleBay);
        AddSingletonRequirement(ref _fobManager);
        AddSingletonRequirement(ref _structureSaver);
        AddSingletonRequirement(ref _traitManager);
        if (UCWarfare.Config.EnableActionMenu)
            AddSingletonRequirement(ref _actionManager);
        return base.PreInit(token);
    }
    public override void LoadRotation()
    {
        if (AllFlags == null || AllFlags.Count == 0)
            throw new InvalidOperationException("No flags are loaded to path with.");

        LoadFlagsIntoRotation();
        if (FlagRotation.Count < 1)
        {
            L.LogError("No flags were put into rotation!!");
        }

        PickObjective(true);
        Chat.Broadcast(T.HardpointFirstObjective, Objective, _nextObjectivePickTime - Time.realtimeSinceStartup);

        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            SendListUI(PlayerManager.OnlinePlayers[i]);
    }
    protected override void EventLoopAction()
    {
        if (State == State.Active && _nextObjectivePickTime < Time.realtimeSinceStartup)
        {
            PickObjective(_objIndex == -1);
            Chat.Broadcast(T.HardpointObjectiveChanged, Objective, _nextObjectivePickTime - Time.realtimeSinceStartup);
        }

        if (State == State.Active && EveryXSeconds(20f))
        {
            Flag flag = Objective;
            for (int i = 0; i < flag.PlayersOnFlag.Count; ++i)
            {
                Points.AwardXP(flag.PlayersOnFlag[i], Levels.XPReward.AttackingFlag, 0.75f);
            }
        }
        base.EventLoopAction();
    }

    public void ForceNextObjective() => PickObjective(_objIndex == -1);
    private void PickObjective(bool first)
    {
        float oldTime = _nextObjectivePickTime;
        float v = 0f;
        if (Config.HardpointObjectiveChangeTimeTolerance != 0)
        {
            v = QuestJsonEx.RoundNumber(0f, 5, UnityEngine.Random.Range(0, Mathf.Abs(Config.HardpointObjectiveChangeTimeTolerance)));
            if (UnityEngine.Random.value < 0.5f)
                v = -v;
        }
        _nextObjectivePickTime = Time.realtimeSinceStartup + Config.HardpointObjectiveChangeTime + v;
        if (State == State.Staging)
            _nextObjectivePickTime += StagingSeconds;
        _objectiveOwner = 0ul;
        int old = _objIndex;
        int highest = FlagRotation.Count;
        if (!first)
            --highest;
        int newobj = UnityEngine.Random.Range(0, highest);
        if (!first && newobj >= old)
            ++newobj;
        if (newobj < 0 || newobj >= FlagRotation.Count)
        {
            L.LogError("Failed to pick a valid objective!!! (picked " + newobj + " out of " + FlagRotation.Count + " loaded flags).");
            if (first)
                throw new SingletonLoadException(SingletonLoadType.Load, this, "Failed to pick a valid objective.");

            _nextObjectivePickTime = oldTime;
            return;
        }
        _objIndex = newobj;
        if (!first)
        {
            StopUsingPoint(FlagRotation[old]);
            UpdateListUI(old);
            UpdateListUI(newobj);
            TicketManager.UpdateUI();
        }
    }
    private void StopUsingPoint(Flag flag)
    {
        CheckFlagForPlayerChanges(flag);
        for (int i = flag.PlayersOnFlag.Count - 1; i >= 0; --i)
        {
            UCPlayer pl = flag.PlayersOnFlag[i];
            RemovePlayerFromFlag(pl.Steam64, pl, flag);
        }

        flag.PlayersOnFlag.Clear();
        flag.PlayersOnFlagTeam1.Clear();
        flag.PlayersOnFlagTeam2.Clear();
    }
    private void LoadFlagsIntoRotation()
    {
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
            if (!(FlagRotation.Count > Config.HardpointFlagAmount + Config.HardpointFlagTolerance && FlagRotation.Count < Config.HardpointFlagAmount - Config.HardpointFlagTolerance))
            {
                L.LogWarning("Objective pathing for hardpoint failed to create the correct amount of flags (" +
                             (Config.HardpointFlagAmount - Config.HardpointFlagTolerance).ToString(Data.AdminLocale) +
                             " to " +
                             (Config.HardpointFlagAmount + Config.HardpointFlagTolerance).ToString(Data.AdminLocale) +
                             ").");
            }
        }
        while (FlagRotation.Count > Config.HardpointFlagAmount + Config.HardpointFlagTolerance && FlagRotation.Count < Config.HardpointFlagAmount - Config.HardpointFlagTolerance);
    }
    private void OnObjectiveStateUpdated(ulong oldState)
    {
        UpdateListUI(_objIndex);
        switch (_objectiveOwner)
        {
            case 1ul:
            case 2ul:
                L.LogDebug("Owner Changed: " + _objectiveOwner + " for " + Objective.Name + ".");
                FactionInfo faction = TeamManager.GetFaction(_objectiveOwner);
                ActionLog.Add(ActionLogType.TeamCapturedObjective, Objective.Name + " - " + faction.GetName(null));
                Chat.Broadcast(T.HardpointObjectiveStateCaptured, Objective, faction);
                break;
            case 3ul:
                L.LogDebug("Contested: " + Objective.Name + ".");
                ActionLog.Add(ActionLogType.TeamCapturedObjective, Objective.Name + " - " + "CONTESTED");
                Chat.Broadcast(T.HardpointObjectiveStateContested, Objective);
                break;
            default:
                L.LogDebug("Cleared: " + Objective.Name + ".");
                ActionLog.Add(ActionLogType.TeamCapturedObjective, Objective.Name + " - " + "CLEAR");
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

    protected override void InitUI(UCPlayer player)
    {
        SendListUI(player);
    }
    public override Task DeclareWin(ulong winner, CancellationToken token)
    {
        using CombinedTokenSources tokens = token.CombineTokensIfNeeded(UnloadToken);
        _objIndex = -1;
        StartCoroutine(EndGameCoroutine(winner));
        return base.DeclareWin(winner, token);
    }
    private IEnumerator<WaitForSeconds> EndGameCoroutine(ulong winner)
    {
        yield return new WaitForSeconds(Config.GeneralLeaderboardDelay);

        ReplaceBarricadesAndStructures();
        Commands.ClearCommand.WipeVehicles();
        Commands.ClearCommand.ClearItems();

        _endScreen = gameObject.AddComponent<HardpointLeaderboard>();
        _endScreen.OnLeaderboardExpired += OnShouldStartNewGame;
        _endScreen.SetShutdownConfig(ShouldShutdownAfterGame, ShutdownMessage);
        _isScreenUp = true;
        _endScreen.StartLeaderboard(winner, _gameStats);
    }
    private void OnShouldStartNewGame()
    {
        if (_endScreen != null)
        {
            _endScreen.OnLeaderboardExpired -= OnShouldStartNewGame;
            Destroy(_endScreen);
            _endScreen = null!;
        }
        _isScreenUp = false;
        UCWarfare.RunTask(EndGame, UCWarfare.UnloadCancel, ctx: "Starting next gamemode.");
    }
    private static void EvaluatePointsOverride(Flag flag, bool overrideInactiveCheck) { }

    private void SendCaptureUI(UCPlayer player)
    {
        if (Objective != null && Objective.PlayerInRange(player.Position))
        {
            ulong team = player.GetTeam();
            EFlagStatus status = team switch
            {
                1 => _objectiveOwner switch
                {
                    1 => EFlagStatus.SECURED,
                    2 => EFlagStatus.LOST,
                    3 => EFlagStatus.CONTESTED,
                    _ => EFlagStatus.BLANK
                },
                2 => _objectiveOwner switch
                {
                    1 => EFlagStatus.LOST,
                    2 => EFlagStatus.SECURED,
                    3 => EFlagStatus.CONTESTED,
                    _ => EFlagStatus.BLANK
                },
                _ => EFlagStatus.BLANK
            };
            CTFUI.CaptureUI.Send(player, new CaptureUI.CaptureUIParameters(team, status, Objective));
        }
        else
        {
            CTFUI.CaptureUI.ClearFromPlayer(player.Connection);
        }
    }
    protected override void ReloadUI(UCPlayer player)
    {
        SendCaptureUI(player);
        SendListUI(player);
    }

    protected override void PlayerEnteredFlagRadius(Flag flag, UCPlayer player)
    {
        L.LogDebug(player + " entered " + flag + ".");
        SendCaptureUI(player);
    }

    protected override void PlayerLeftFlagRadius(Flag flag, UCPlayer player)
    {
        L.LogDebug(player + " left " + flag + ".");
        SendCaptureUI(player);
    }

    protected override Task PostGameStarting(bool isOnLoad, CancellationToken token)
    {
        using CombinedTokenSources tokens = token.CombineTokensIfNeeded(UnloadToken);
        _gameStats.Reset();
        CTFUI.ClearCaptureUI();
        RallyManager.WipeAllRallies();
        if (Config.HardpointStagingPhaseSeconds > 0)
        {
            SpawnBlockers();
            StartStagingPhase(Config.HardpointStagingPhaseSeconds);
        }
        return base.PostGameStarting(isOnLoad, token);
    }
    protected override void EndStagingPhase()
    {
        base.EndStagingPhase();
        DestroyBlockers();
    }
    protected override void FlagCheck()
    {
        if (_objIndex < 0 || _objIndex >= FlagRotation.Count || State != State.Active)
            return;

        Flag f = Objective;
        CheckFlagForPlayerChanges(f);
        UpdateObjectiveState();
        if (EnableAMC && EveryXSeconds(Config.HardpointFlagTickSeconds))
        {
            CheckMainCampZones();
        }
    }
    private void UpdateObjectiveState()
    {
        if (_objIndex < 0 || _objIndex >= FlagRotation.Count || State != State.Active)
            return;
        ulong old = _objectiveOwner;
        _objectiveOwner = ConventionalIsContested(Objective, out ulong winner) ? 3ul : winner;
        if (old != _objectiveOwner)
        {
            OnObjectiveStateUpdated(old);
        }
    }
    protected override void FlagOwnerChanged(ulong oldOwner, ulong newOwner, Flag flag) { }
    protected override void FlagPointsChanged(float newPts, float oldPts, Flag flag) { }
    public override bool IsAttackSite(ulong team, Flag flag) => flag == Objective;
    public override bool IsDefenseSite(ulong team, Flag flag) => flag == Objective;
    protected override bool TimeToEvaluatePoints() => false;
    public void UpdateListUI(int index)
    {
        if (index < 0) return;
        if (FlagRotation.Count <= index)
        {
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; ++i)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                if (!pl.HasUIHidden)
                    CTFUI.ListUI.Rows[index].Root.SetVisibility(pl.Connection, false);
            }

            return;
        }
        bool obj = index == _objIndex;
        string s1 = $"<color=#{(obj ? GetObjectiveColor() : UCWarfare.GetColorHex("undiscovered_flag"))}>{FlagRotation[index].ShortName}</color>";
        string s2 = obj ? $"<color=#{UCWarfare.GetColorHex("attack_icon_color")}>{Config.UIIconAttack}</color>" : string.Empty;
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
        {
            UCPlayer player = PlayerManager.OnlinePlayers[i];
            if (player.HasUIHidden) continue;
            ITransportConnection c = player.Connection;
            FlagListUI.FlagListRow row = CTFUI.ListUI.Rows[index];
            row.Name.SetText(c, s1);
            row.Icon.SetText(c, s2);
            row.Root.SetVisibility(c, true);
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
        for (int i = 0; i < CTFUI.ListUI.Rows.Length; i++)
        {
            FlagListUI.FlagListRow row = CTFUI.ListUI.Rows[i];
            if (FlagRotation.Count <= i)
            {
                row.Root.SetVisibility(c, false);
            }
            else
            {
                row.Root.SetVisibility(c, true);
                Flag flag = FlagRotation[i];
                if (i == _objIndex)
                {
                    row.Name.SetText(c, $"<color=#{c3}>{flag.Name}</color>");
                    row.Icon.SetText(c, $"<color=#{c2}>{Config.UIIconAttack}</color>");
                }
                else
                {
                    row.Name.SetText(c, $"<color=#{c1}>{flag.Name}</color>");
                }
            }
        }
    }
    private string GetObjectiveColor()
        => _objectiveOwner switch
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
        tickets = (team switch { 1 => Manager.Team1Tickets, 2 => Manager.Team2Tickets, _ => 0 }).ToString(Data.LocalLocale);
        Flag? obj = (Data.Gamemode as IFlagObjectiveGamemode)?.Objective;
        TranslationFlags flg = (team switch
        {
            1 => TranslationFlags.Team1,
            2 => TranslationFlags.Team2,
            _ => 0
        }) | TranslationFlags.UnityUI;
        message = obj is null ? string.Empty : ("Objective: " + (obj as ITranslationArgument).Translate(Localization.GetDefaultLanguage(),
            Flag.COLOR_SHORT_NAME_FORMAT, null, Data.LocalLocale, ref flg));
        int bld = GetTeamBleed(team);
        bleed = bld == 0 ? string.Empty : bld.ToString(Data.LocalLocale);
    }
    public override int GetTeamBleed(ulong team) => Data.Gamemode is not Hardpoint hp || hp.ObjectiveState >= team ? 0 : -1;
    public override void OnGameStarting(bool isOnLoaded) => Manager.Team1Tickets = Manager.Team2Tickets = Gamemode.Config.HardpointStartingTickets;
    public override void OnTicketsChanged(ulong team, int oldValue, int newValue, ref bool updateUI)
    {
        if (oldValue > 0 && newValue <= 0)
            UCWarfare.RunTask(Data.Gamemode.DeclareWin, TeamManager.Other(team), default, ctx: "Lose game, tickets reached 0.");
    }
    public override void Tick()
    {
        if (Data.Gamemode is not Hardpoint hp) return;
        if (hp.EveryXSeconds(Gamemode.Config.HardpointTicketTickSeconds))
        {
            bool t = true;
            switch (hp.ObjectiveState)
            {
                case 1ul:
                    --Manager.Team2Tickets;
                    break;
                case 2ul:
                    --Manager.Team1Tickets;
                    break;
                case 0ul:
                    // draw, keeps the game running until someone can win
                    if (Manager.Team1Tickets == 1 && Manager.Team2Tickets == 1)
                        return;
                    --Manager.Team1Tickets;
                    --Manager.Team2Tickets;
                    break;
                default:
                    t = false;
                    break;
            }
            if (t && hp.Objective is { } obj)
            {
                for (int i = 0; i < hp.WarstatsTracker.stats.Count; ++i)
                {
                    HardpointPlayerStats stats = hp.WarstatsTracker.stats[i];
                    UCPlayer pl = stats.Player;
                    if (pl is not { IsOnline: true }) continue;
                    if (obj.PlayerInRange(pl.Player))
                    {
                        ++stats.Hardpoints;
                    }
                }
            }
        }
    }
}
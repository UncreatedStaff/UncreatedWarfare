using System.Collections.Generic;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Revives;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.TeamDeathmatch;

public class TeamDeathmatch : TeamGamemode, IKitRequests, IVehicles, IFOBs, ISquads, IRevives, ITeamScore
{
    public TeamDeathmatch() : base(nameof(TeamDeathmatch), 0f)
    {

    }

    public override string DisplayName => "Team Deathmatch";
    public override bool UseWhitelist => true;
    public override bool UseJoinUI => true;
    public override bool TransmitMicWhileNotActive => true;
    public override bool EnableAMC => true;
    public override bool ShowXPUI => true;
    public override bool ShowOFPUI => true;
    protected VehicleSpawner _vehicleSpawner;
    public VehicleSpawner VehicleSpawner => _vehicleSpawner;
    protected VehicleBay _vehicleBay;
    public VehicleBay VehicleBay => _vehicleBay;
    protected VehicleSigns _vehicleSigns;
    public VehicleSigns VehicleSigns => _vehicleSigns;
    protected FOBManager _FOBManager;
    public FOBManager FOBManager => _FOBManager;
    protected RequestSigns _requestSigns;
    public RequestSigns RequestSigns => _requestSigns;
    protected KitManager _kitManager;
    public KitManager KitManager => _kitManager;
    protected ReviveManager _reviveManager;
    public ReviveManager ReviveManager => _reviveManager;
    protected SquadManager _squadManager;
    public SquadManager SquadManager => _squadManager;
    protected StructureSaver _structureSaver;
    public StructureSaver StructureSaver => _structureSaver;
    public int Team1Score => _t1score;
    public int Team2Score => _t2score;
    protected int _t1score = 0;
    protected int _t2score = 0;
    protected override void PreInit()
    {
        base.PreInit();
        AddSingletonRequirement(ref _squadManager);
        AddSingletonRequirement(ref _kitManager);
        AddSingletonRequirement(ref _vehicleSpawner);
        AddSingletonRequirement(ref _vehicleBay);
        AddSingletonRequirement(ref _requestSigns);
        AddSingletonRequirement(ref _vehicleSigns);
        AddSingletonRequirement(ref _structureSaver);
        AddSingletonRequirement(ref _reviveManager);
        AddSingletonRequirement(ref _FOBManager);
    }
    protected override void PostInit()
    {
        Commands.ReloadCommand.ReloadKits();
    }
    protected override void OnReady()
    {
        base.OnReady();
        RepairManager.LoadRepairStations();
        RallyManager.WipeAllRallies();
        VehicleSigns.InitAllSigns();
    }
    public override void Subscribe()
    {
        base.Subscribe();
        EventDispatcher.OnPlayerDied += OnDeath;
    }
    protected override void PostDispose()
    {
        base.PostDispose();
    }
    public override void Unsubscribe()
    {
        EventDispatcher.OnPlayerDied -= OnDeath;
        base.Unsubscribe();
    }
    public override void DeclareWin(ulong winner)
    {
        if (this._state == EState.FINISHED) return;
        this._state = EState.FINISHED;

        QuestManager.OnGameOver(winner);
        ActionLog.Add(EActionLogType.TEAM_WON, Teams.TeamManager.TranslateName(winner, 0));
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

        InvokeOnTeamWin(winner);
        EndGame();
    }
    private void OnScoreUpdated()
    {
        // todo update ui?
    }
    protected override void PreGameStarting(bool isOnLoad)
    {
        _t1score = 0;
        _t2score = 0;
    }
    protected override void EventLoopAction() 
    {

    }
    private void OnDeath(PlayerDied e)
    {
        if (e.Killer is not null)
        {
            ulong team = e.Killer.GetTeam();
            if (team == e.Player.GetTeam())
                return;
            if (team == 1) ++_t1score;
            else if (team == 2) ++_t2score;
            else return;
            OnScoreUpdated();
        }
    }
}

using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Revives;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

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
        if (_stagingPhaseTimer != null)
            StopCoroutine(_stagingPhaseTimer);
        Destroy(_gameStats);
        base.PostDispose();
    }

    protected override bool TimeToCheck() => EveryXSeconds(Config.Conquest.FlagTickSeconds);
    protected override bool TimeToTicket() => EveryXSeconds(Config.Conquest.TicketTickSeconds);
    public override bool IsAttackSite(ulong team, Flag flag) => true;
    public override bool IsDefenseSite(ulong team, Flag flag) => true;
    public override void DeclareWin(ulong winner)
    {
        throw new NotImplementedException();
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

    protected override void FlagOwnerChanged(ulong OldOwner, ulong NewOwner, Flag flag)
    {
        throw new NotImplementedException();
    }
    protected override void FlagPointsChanged(float NewPoints, float OldPoints, Flag flag)
    {
        throw new NotImplementedException();
    }
    protected override void PlayerEnteredFlagRadius(Flag flag, Player player)
    {
        throw new NotImplementedException();
    }
    protected override void PlayerLeftFlagRadius(Flag flag, Player player)
    {
        throw new NotImplementedException();
    }
}
/*
 * Rotation: FLAG_T1, MID_T1 (adjacent to one of the t1 adjacencies), MID * n-4 (not adjacent to t1 adj, t2 adj), MID_T2 (adjacent to one of the t2 adjacencies), FLAG_T2
 * 
 * 
 * 
 * 
 */
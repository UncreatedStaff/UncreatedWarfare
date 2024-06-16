using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Warfare.Actions;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Revives;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Traits;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.TeamDeathmatch;

// todo make use tickets instead, would just make more sense
public class TeamDeathmatch : TeamGamemode, IKitRequests, IVehicles, IFOBs, ISquads, IRevives, ITeamScore, ITraits
{
    protected TraitManager _traitManager;
    protected ActionManager _actionManager;
    protected VehicleSpawner _vehicleSpawner;
    protected VehicleBay _vehicleBay;
    protected FOBManager _FOBManager;
    protected KitManager _kitManager;
    protected ReviveManager _reviveManager;
    protected SquadManager _squadManager;
    protected StructureSaver _structureSaver;
    public TeamDeathmatch() : base(nameof(TeamDeathmatch), 0f)
    {

    }

    public override string DisplayName => "Team Deathmatch";
    public override bool UseWhitelist => true;
    public override bool UseTeamSelector => true;
    public override bool TransmitMicWhileNotActive => true;
    public override bool EnableAMC => true;
    public override bool ShowXPUI => true;
    public override bool ShowOFPUI => true;
    public VehicleSpawner VehicleSpawner => _vehicleSpawner;
    public VehicleBay VehicleBay => _vehicleBay;
    public FOBManager FOBManager => _FOBManager;
    public KitManager KitManager => _kitManager;
    public ReviveManager ReviveManager => _reviveManager;
    public SquadManager SquadManager => _squadManager;
    public StructureSaver StructureSaver => _structureSaver;
    public TraitManager TraitManager => _traitManager;
    public ActionManager ActionManager => _actionManager;
    public int Team1Score => _t1score;
    public int Team2Score => _t2score;
    protected int _t1score;
    protected int _t2score;
    protected override Task PreInit(CancellationToken token)
    {
        using CombinedTokenSources tokens = token.CombineTokensIfNeeded(UnloadToken);
        AddSingletonRequirement(ref _squadManager);
        AddSingletonRequirement(ref _kitManager);
        AddSingletonRequirement(ref _vehicleSpawner);
        AddSingletonRequirement(ref _vehicleBay);
        AddSingletonRequirement(ref _structureSaver);
        AddSingletonRequirement(ref _reviveManager);
        AddSingletonRequirement(ref _FOBManager);
        AddSingletonRequirement(ref _traitManager);
        if (UCWarfare.Config.EnableActionMenu)
            AddSingletonRequirement(ref _actionManager);
        return base.PreInit(token);
    }
    public override void Subscribe()
    {
        base.Subscribe();
        EventDispatcher.PlayerDied += OnDeath;
    }
    public override void Unsubscribe()
    {
        EventDispatcher.PlayerDied -= OnDeath;
        base.Unsubscribe();
    }
    public override Task DeclareWin(ulong winner, CancellationToken token)
    {
        using CombinedTokenSources tokens = token.CombineTokensIfNeeded(UnloadToken);
        ThreadUtil.assertIsGameThread();
        StartCoroutine(EndGameCoroutine());
        return base.DeclareWin(winner, token);
    }
    private IEnumerator<WaitForSeconds> EndGameCoroutine()
    {
        yield return new WaitForSeconds(Config.GeneralLeaderboardDelay);
        ReplaceBarricadesAndStructures();
        Commands.ClearCommand.WipeVehicles();
        Commands.ClearCommand.ClearItems();
        UCWarfare.RunTask(EndGame, UCWarfare.UnloadCancel, ctx: "Starting next gamemode.");
    }

    protected override void InitUI(UCPlayer player)
    {
        OnScoreUpdated();
    }

    private void OnScoreUpdated()
    {
        // todo make this use a ticket provider instead
    }
    protected override Task PreGameStarting(bool isOnLoad, CancellationToken token)
    {
        _t1score = 0;
        _t2score = 0;
        return base.PreGameStarting(isOnLoad, token);
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

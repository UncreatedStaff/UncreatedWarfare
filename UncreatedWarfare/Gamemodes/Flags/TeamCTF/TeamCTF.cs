using SDG.Unturned;
using System.Linq;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Singletons;
using UnityEngine;
using static Uncreated.Warfare.Gamemodes.Flags.UI.CaptureUI;

namespace Uncreated.Warfare.Gamemodes.Flags.TeamCTF;

public class TeamCTF : CTFBaseMode<TeamCTFLeaderboard, BaseCTFStats, TeamCTFTracker, TeamCTFTicketProvider>
{
    public override string DisplayName => "Advance and Secure";
    public override EGamemode GamemodeType => EGamemode.TEAM_CTF;
    public TeamCTF() : base(nameof(TeamCTF), Config.AASEvaluateTime) { }
    protected override void PostDispose()
    {
        foreach (SteamPlayer player in Provider.clients)
        {
            CTFUI.ClearFlagList(player.transportConnection);
            if (player.player.TryGetPlayerData(out Components.UCPlayerData c))
                c.stats = null!;
        }
        CTFUI.CaptureUI.ClearFromAllPlayers();
        base.PostDispose();
    }
    protected override void PostGameStarting(bool isOnLoad)
    {
        base.PostGameStarting(isOnLoad);
        SpawnBlockers();
        StartStagingPhase(Config.AASStagingTime);
    }
    protected override void EndStagingPhase()
    {
        base.EndStagingPhase();
        DestroyBlockers();
    }
    protected override void InvokeOnObjectiveChanged(Flag OldFlagObj, Flag NewFlagObj, ulong Team, int OldObj, int NewObj)
    {
        base.InvokeOnObjectiveChanged(OldFlagObj, NewFlagObj, Team, OldObj, NewObj);
        CTFUI.ReplicateFlagUpdate(OldFlagObj, false);
        CTFUI.ReplicateFlagUpdate(NewFlagObj, false);
    }
    protected override void InvokeOnFlagCaptured(Flag flag, ulong capturedTeam, ulong lostTeam)
    {
        base.InvokeOnFlagCaptured(flag, capturedTeam, lostTeam);
        CTFUI.ReplicateFlagUpdate(flag, true);
    }
    protected override void InvokeOnFlagNeutralized(Flag flag, ulong capturedTeam, ulong lostTeam)
    {
        base.InvokeOnFlagNeutralized(flag, capturedTeam, lostTeam);
        CTFUI.ReplicateFlagUpdate(flag, true);
    }
    public override void OnGroupChanged(GroupChanged e)
    {
        CTFUI.ClearFlagList(e.Player);
        if (_onFlag.TryGetValue(e.Player.Steam64, out int id))
        {
            CaptureUIParameters p = CTFUI.RefreshStaticUI(e.NewTeam, _rotation.FirstOrDefault(x => x.ID == id)
                                                                          ?? _rotation[0], e.Player.Player.movement.getVehicle() != null);
            CTFUI.CaptureUI.Send(e.Player, in p);
        }
        CTFUI.SendFlagList(e.Player);
        base.OnGroupChanged(e);
    }
    protected override void InitUI(UCPlayer player)
    {
        CTFUI.SendFlagList(player);
    }
    public override bool IsAttackSite(ulong team, Flag flag) => flag.IsObj(team);
    public override bool IsDefenseSite(ulong team, Flag flag) => flag.T1Obj && team == 2 && flag.Owner == 2 || flag.T2Obj && team == 1 && flag.Owner == 1;
}

public class TeamCTFLeaderboard : BaseCTFLeaderboard<BaseCTFStats, TeamCTFTracker>
{

}

public sealed class TeamCTFTicketProvider : BaseCTFTicketProvider, IFlagCapturedListener
{
    public override int GetTeamBleed(ulong team)
    {
        if (!Data.Is(out IFlagRotation fg)) return 0;
        float enemyRatio = (float)fg.Rotation.Count(f => f.Owner != team && f.Owner != 0) / fg.Rotation.Count;

        return enemyRatio switch
        {
            > 0.85f => -3,
            > 0.75f => -2,
            > 0.6f => -1,
            _ => 0
        };
    }
    public override void OnGameStarting(bool isOnLoaded)
    {
        Manager.Team1Tickets = Gamemode.Config.AASStartingTickets;
        Manager.Team2Tickets = Gamemode.Config.AASStartingTickets;
    }
    public void OnFlagCaptured(Flag flag, ulong newOwner, ulong oldOwner)
    {
        if (newOwner == 1) Manager.Team1Tickets += Gamemode.Config.AASTicketsFlagCaptured;
        else if (newOwner == 2) Manager.Team2Tickets += Gamemode.Config.AASTicketsFlagCaptured;

        if (oldOwner == 1) Manager.Team1Tickets += Gamemode.Config.AASTicketsFlagLost;
        else if (oldOwner == 2) Manager.Team2Tickets += Gamemode.Config.AASTicketsFlagLost;
    }
}
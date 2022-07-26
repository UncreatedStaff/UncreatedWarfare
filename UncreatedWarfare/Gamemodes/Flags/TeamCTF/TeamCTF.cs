using SDG.Unturned;
using System;
using System.Linq;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Quests;
using static Uncreated.Warfare.Gamemodes.Flags.UI.CaptureUI;

namespace Uncreated.Warfare.Gamemodes.Flags.TeamCTF;

public class TeamCTF : CTFBaseMode<TeamCTFLeaderboard, BaseCTFStats, TeamCTFTracker>
{
    public override string DisplayName => "Advance and Secure";
    public override EGamemode GamemodeType => EGamemode.TEAM_CTF;
    public TeamCTF() : base(nameof(TeamCTF), Config.TeamCTF.EvaluateTime) { }
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
        StartStagingPhase(Config.TeamCTF.StagingTime);
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
        if (_onFlag.TryGetValue(e.Player, out int id))
        {
            CaptureUIParameters p = CTFUI.RefreshStaticUI(e.NewTeam, _rotation.FirstOrDefault(x => x.ID == id)
                                                                          ?? _rotation[0], e.Player.Player.movement.getVehicle() != null);
            CTFUI.CaptureUI.Send(e.Player, ref p);
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

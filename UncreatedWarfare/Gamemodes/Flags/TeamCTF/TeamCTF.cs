using SDG.Unturned;
using System;
using System.Linq;
using Uncreated.Warfare.Quests;

namespace Uncreated.Warfare.Gamemodes.Flags.TeamCTF;

public class TeamCTF :
    CTFBaseMode<TeamCTFLeaderboard, BaseCTFStats, TeamCTFTracker>
{
    public override string DisplayName => "Advance and Secure";
    public override EGamemode GamemodeType => EGamemode.TEAM_CTF;
    public TeamCTF() : base(nameof(TeamCTF), Config.TeamCTF.EvaluateTime) { }
    protected override void PostDispose()
    {
        foreach (SteamPlayer player in Provider.clients)
        {
            CTFUI.ClearFlagList(player.transportConnection);
            SendUIParameters.Nil.SendToPlayer(player);
            if (player.player.TryGetPlaytimeComponent(out Components.PlaytimeComponent c))
                c.stats = null!;
        }
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
        QuestManager.OnObjectiveCaptured((capturedTeam == 1 ? flag.PlayersOnFlagTeam1 : flag.PlayersOnFlagTeam2)
                    .Select(x => x.channel.owner.playerID.steamID.m_SteamID).ToArray());
    }
    protected override void InvokeOnFlagNeutralized(Flag flag, ulong capturedTeam, ulong lostTeam)
    {
        base.InvokeOnFlagNeutralized(flag, capturedTeam, lostTeam);
        CTFUI.ReplicateFlagUpdate(flag, true);
        if (capturedTeam == 1)
            QuestManager.OnFlagNeutralized(flag.PlayersOnFlagTeam1.Select(x => x.channel.owner.playerID.steamID.m_SteamID).ToArray(), capturedTeam);
        else if (capturedTeam == 2)
            QuestManager.OnFlagNeutralized(flag.PlayersOnFlagTeam2.Select(x => x.channel.owner.playerID.steamID.m_SteamID).ToArray(), capturedTeam);
    }
    public override void OnGroupChanged(UCPlayer player, ulong oldGroup, ulong newGroup, ulong oldteam, ulong newteam)
    {
        CTFUI.ClearFlagList(player);
        if (_onFlag.TryGetValue(player.Player.channel.owner.playerID.steamID.m_SteamID, out int id))
            CTFUI.RefreshStaticUI(newteam, _rotation.FirstOrDefault(x => x.ID == id)
                ?? _rotation[0], player.Player.movement.getVehicle() != null).SendToPlayer(player.Player.channel.owner);
        CTFUI.SendFlagList(player);
        base.OnGroupChanged(player, oldGroup, newGroup, oldteam, newteam);
    }
    public override void PlayerInit(UCPlayer player, bool wasAlreadyOnline)
    {
        base.PlayerInit(player, wasAlreadyOnline);
        if (isScreenUp && _endScreen != null)
        {
            _endScreen.OnPlayerJoined(player);
        }
        else
        {
            CTFUI.SendFlagList(player);
            if (State == EState.STAGING)
                this.ShowStagingUI(player);
        }
    }
}

public class TeamCTFLeaderboard : BaseCTFLeaderboard<BaseCTFStats, TeamCTFTracker>
{

}

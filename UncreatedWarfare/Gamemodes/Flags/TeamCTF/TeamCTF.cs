using SDG.Unturned;
using System;
using System.Linq;
using Uncreated.Warfare.Quests;

namespace Uncreated.Warfare.Gamemodes.Flags.TeamCTF
{
    public class TeamCTF : CTFBaseMode<TeamCTFLeaderboard, BaseCTFStats, TeamCTFTracker>
    {
        public override string DisplayName => "Advance and Secure";
        public override EGamemode GamemodeType => EGamemode.TEAM_CTF;
        public TeamCTF() : base(nameof(TeamCTF), Config.TeamCTF.EvaluateTime) { }
        protected override void EndStagingPhase()
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            base.EndStagingPhase();
            DestroyBlockers();
        }
        public override void StartNextGame(bool onLoad = false)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            base.StartNextGame(onLoad);
            SpawnBlockers();
            StartStagingPhase(Config.TeamCTF.StagingTime);
        }
        protected override void InvokeOnObjectiveChanged(Flag OldFlagObj, Flag NewFlagObj, ulong Team, int OldObj, int NewObj)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            base.InvokeOnObjectiveChanged(OldFlagObj, NewFlagObj, Team, OldObj, NewObj);
            CTFUI.ReplicateFlagUpdate(OldFlagObj, false);
            CTFUI.ReplicateFlagUpdate(NewFlagObj, false);
        }
        protected override void InvokeOnFlagCaptured(Flag flag, ulong capturedTeam, ulong lostTeam)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            base.InvokeOnFlagCaptured(flag, capturedTeam, lostTeam);
            CTFUI.ReplicateFlagUpdate(flag, true);
            QuestManager.OnObjectiveCaptured((capturedTeam == 1 ? flag.PlayersOnFlagTeam1 : flag.PlayersOnFlagTeam2)
                        .Select(x => x.channel.owner.playerID.steamID.m_SteamID).ToArray());
        }
        protected override void InvokeOnFlagNeutralized(Flag flag, ulong capturedTeam, ulong lostTeam)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            base.InvokeOnFlagNeutralized(flag, capturedTeam, lostTeam);
            CTFUI.ReplicateFlagUpdate(flag, true);
            if (capturedTeam == 1)
                QuestManager.OnFlagNeutralized(flag.PlayersOnFlagTeam1.Select(x => x.channel.owner.playerID.steamID.m_SteamID).ToArray(), capturedTeam);
            else if (capturedTeam == 2)
                QuestManager.OnFlagNeutralized(flag.PlayersOnFlagTeam2.Select(x => x.channel.owner.playerID.steamID.m_SteamID).ToArray(), capturedTeam);
        }
        public override void OnGroupChanged(UCPlayer player, ulong oldGroup, ulong newGroup, ulong oldteam, ulong newteam)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            CTFUI.ClearFlagList(player);
            if (_onFlag.TryGetValue(player.Player.channel.owner.playerID.steamID.m_SteamID, out int id))
                CTFUI.RefreshStaticUI(newteam, _rotation.FirstOrDefault(x => x.ID == id)
                    ?? _rotation[0], player.Player.movement.getVehicle() != null).SendToPlayer(player.Player.channel.owner);
            CTFUI.SendFlagList(player);
            base.OnGroupChanged(player, oldGroup, newGroup, oldteam, newteam);
        }
        public override void OnPlayerJoined(UCPlayer player, bool wasAlreadyOnline, bool shouldRespawn)
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            base.OnPlayerJoined(player, wasAlreadyOnline, shouldRespawn);
            if (isScreenUp && _endScreen != null)
            {
                _endScreen.SendLeaderboard(player, Teams.TeamManager.GetTeamHexColor(player.GetTeam()));
            }
            else
            {
                CTFUI.SendFlagList(player);
                if (State == EState.STAGING)
                    this.ShowStagingUI(player);
            }
        }
        public override void Dispose()
        {
            using IDisposable profiler = ProfilingUtils.StartTracking();
            foreach (SteamPlayer player in Provider.clients)
            {
                CTFUI.ClearFlagList(player.transportConnection);
                SendUIParameters.Nil.SendToPlayer(player); // clear all capturing uis
                if (player.player.TryGetPlaytimeComponent(out Components.PlaytimeComponent c))
                    c.stats = null;
            }
            base.Dispose();
        }
    }

    public class TeamCTFLeaderboard : BaseCTFLeaderboard<BaseCTFStats, TeamCTFTracker>
    {
        protected override Guid GUID => Gamemode.Config.UI.CTFLeaderboardGUID;
    }
}

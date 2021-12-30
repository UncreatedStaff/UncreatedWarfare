using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Linq;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes.Interfaces;

namespace Uncreated.Warfare.Gamemodes.Flags.Invasion
{
    public class Invasion : CTFBaseMode<InvasionLeaderboard, BaseCTFStats, InvasionTracker>
    {
        public override string DisplayName => "Invasion";

        protected ulong _attackTeam;
        protected ulong _defenseTeam;
        public ulong AttackingTeam { get => _attackTeam; }
        public ulong DefendingTeam { get => _defenseTeam; }

        protected SpecialFOB _vcp;
        public SpecialFOB FirstPointFOB { get => _vcp; }

        public override void StartNextGame(bool onLoad = false)
        {
            PickTeams();
            base.StartNextGame(onLoad);


            if (_attackTeam == 1)
                SpawnBlockerOnT1();
            else 
                SpawnBlockerOnT2();

            SpawnBlockers();
            Flag firstFlag = null;
            if (DefendingTeam == 1)
                firstFlag = Rotation.Last();
            else if (DefendingTeam == 2)
                firstFlag = Rotation.First();

            _vcp = FOBManager.RegisterNewSpecialFOB(Config.Invasion.SpecialFOBName, firstFlag.ZoneData.Center3DAbove, _defenseTeam, UCWarfare.GetColorHex("invasion_special_fob"), true);
            StartStagingPhase(Config.Invasion.StagingTime);
        }

        // SHOULD BE RECHECKED
        protected override void EvaluateTickets()
        {
            base.EvaluateTickets();
        }
        protected void PickTeams()
        {
            _attackTeam = (ulong)UnityEngine.Random.Range(1, 3);
            if (_attackTeam == 1)
                _defenseTeam = 2;
            else if (_attackTeam == 2)
                _defenseTeam = 1;
        }
        public override void LoadRotation()
        {
            if (_allFlags == null || _allFlags.Count == 0) return;
            LoadFlagsIntoRotation();
            if (_rotation.Count < 1)
            {
                L.LogError("No flags were put into rotation!!");
            }
            if (_attackTeam == 1)
            {
                _objectiveT1Index = _rotation.Count - 1;
                _objectiveT2Index = -1;
            }
            else
            {
                _objectiveT1Index = -1;
                _objectiveT2Index = 0;
            }
            if (Config.Invasion.DiscoveryForesight < 1)
            {
                L.LogWarning("Discovery Foresight is set to 0 in Flag Settings. The players can not see their next flags.");
            }
            else
            {
                for (int i = 0; i < _rotation.Count; i++)
                {
                    _rotation[i].Discover(_defenseTeam);
                }
                if (_attackTeam == 1)
                {
                    for (int i = 0; i < Config.Invasion.DiscoveryForesight; i++)
                    {
                        if (i >= _rotation.Count || i < 0) break;
                        _rotation[i].Discover(1);
                    }
                }
                else if (_attackTeam == 2)
                {
                    for (int i = _rotation.Count - 1; i > _rotation.Count - 1 - Config.Invasion.DiscoveryForesight; i--)
                    {
                        if (i >= _rotation.Count || i < 0) break;
                        _rotation[i].Discover(2);
                    }
                }
            }
            for (int i = 0; i < _rotation.Count; i++)
            {
                InitFlag(_rotation[i]); //subscribe to abstract events.
            }
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                UCPlayer pl = PlayerManager.OnlinePlayers[i];
                CTFUI.ClearFlagList(pl);
                InvasionUI.SendFlagList(pl);
            }
            PrintFlagRotation();
            EvaluatePoints();
        }
        public override void InitFlag(Flag flag)
        {
            base.InitFlag(flag);
            flag.EvaluatePointsOverride = FlagCheck;
            flag.IsContestedOverride = ContestedCheck;
            flag.SetOwnerNoEventInvocation(_defenseTeam);
            flag.SetPoints(_attackTeam == 2 ? Flag.MAX_POINTS : -Flag.MAX_POINTS, true, true);
        }
        private void FlagCheck(Flag flag, bool overrideInactiveCheck = false)
        {
            if (State == EState.ACTIVE || overrideInactiveCheck)
            {
                if (flag.ID == (AttackingTeam == 1ul ? ObjectiveTeam1.ID : ObjectiveTeam2.ID))
                {
                    //bool atkOnFlag = (AttackingTeam == 1ul && flag.Team1TotalCappers > 0) || (AttackingTeam == 2ul && flag.Team2TotalCappers > 0);
                    if (!flag.IsContested(out ulong winner))
                    {
                        if (winner == AttackingTeam || AttackingTeam != flag.Owner)
                        {
                            flag.Cap(winner, 1f);
                        }
                        else
                        {
                            // invoke points updated method to show secured.
                            flag.SetPoints(flag.Points);
                        }
                    }
                    else
                    {
                        // invoke points updated method to show contested.
                        flag.SetPoints(flag.Points);
                    }
                }
            }
        }
        private bool ContestedCheck(Flag flag, out ulong winner)
        {
            if (flag.IsObj(_attackTeam))
            {
                if (flag.Team1TotalCappers == 0 && flag.Team2TotalCappers == 0)
                {
                    winner = 0;
                    return false;
                }
                else if (flag.Team1TotalCappers == flag.Team2TotalCappers)
                {
                    winner = 0;
                }
                else if (flag.Team1TotalCappers == 0 && flag.Team2TotalCappers > 0)
                {
                    winner = 2;
                }
                else if (flag.Team2TotalCappers == 0 && flag.Team1TotalCappers > 0)
                {
                    winner = 1;
                }
                else if (flag.Team1TotalCappers > flag.Team2TotalCappers)
                {
                    if (flag.Team1TotalCappers - Config.TeamCTF.RequiredPlayerDifferenceToCapture >= flag.Team2TotalCappers)
                    {
                        winner = 1;
                    }
                    else
                    {
                        winner = 0;
                    }
                }
                else
                {
                    if (flag.Team2TotalCappers - Config.TeamCTF.RequiredPlayerDifferenceToCapture >= flag.Team1TotalCappers)
                    {
                        winner = 2;
                    }
                    else
                    {
                        winner = 0;
                    }
                }
                return winner == 0;
            }
            else
            {
                if (flag.ObjectivePlayerCountCappers == 0) winner = 0;
                else winner = flag.WhosObj();
                if (!flag.IsObj(winner)) winner = 0;
                return false;
            }
        }
        protected override void PlayerEnteredFlagRadius(Flag flag, Player player)
        {
            ulong team = player.GetTeam();
            L.LogDebug("Player " + player.channel.owner.playerID.playerName + " entered flag " + flag.Name, ConsoleColor.White);
            player.SendChat("entered_cap_radius", UCWarfare.GetColor(team == 1 ? "entered_cap_radius_team_1" : (team == 2 ? "entered_cap_radius_team_2" : "default")), flag.Name, flag.ColorString);
            SendUIParameters t1 = SendUIParameters.Nil;
            SendUIParameters t2 = SendUIParameters.Nil;
            SendUIParameters t1v = SendUIParameters.Nil;
            SendUIParameters t2v = SendUIParameters.Nil;
            if (flag.Team1TotalCappers > 0)
                t1 = InvasionUI.RefreshStaticUI(1, flag, false, _attackTeam);
            if (flag.Team1TotalPlayers - flag.Team1TotalCappers > 0)
                t1v = InvasionUI.RefreshStaticUI(1, flag, true, _attackTeam);
            if (flag.Team2TotalCappers > 0)
                t2 = InvasionUI.RefreshStaticUI(2, flag, false, _attackTeam);
            if (flag.Team2TotalPlayers - flag.Team2TotalCappers > 0)
                t2v = InvasionUI.RefreshStaticUI(2, flag, true, _attackTeam);
            foreach (Player capper in flag.PlayersOnFlag)
            {
                ulong t = capper.GetTeam();

                if (t == 1)
                {
                    if (capper.movement.getVehicle() == null)
                        t1.SendToPlayer(capper.channel.owner);
                    else
                        t1v.SendToPlayer(capper.channel.owner);
                }
                else if (t == 2)
                {
                    if (capper.movement.getVehicle() == null)
                        t2.SendToPlayer(capper.channel.owner);
                    else
                        t2v.SendToPlayer(capper.channel.owner);
                }
            }
        }
        protected override void PlayerLeftFlagRadius(Flag flag, Player player)
        {
            ITransportConnection Channel = player.channel.owner.transportConnection;
            ulong team = player.GetTeam();
            L.LogDebug("Player " + player.channel.owner.playerID.playerName + " left flag " + flag.Name, ConsoleColor.White);
            player.SendChat("left_cap_radius", UCWarfare.GetColor(team == 1 ? "left_cap_radius_team_1" : (team == 2 ? "left_cap_radius_team_2" : "default")), flag.Name, flag.ColorString);
            CTFUI.ClearCaptureUI(player.channel.owner.transportConnection);
            SendUIParameters t1 = SendUIParameters.Nil;
            SendUIParameters t2 = SendUIParameters.Nil;
            SendUIParameters t1v = SendUIParameters.Nil;
            SendUIParameters t2v = SendUIParameters.Nil;
            if (flag.Team1TotalCappers > 0)
                t1 = InvasionUI.RefreshStaticUI(1, flag, false, AttackingTeam);
            if (flag.Team1TotalPlayers - flag.Team1TotalCappers > 0)
                t1v = InvasionUI.RefreshStaticUI(1, flag, true, AttackingTeam);
            if (flag.Team2TotalCappers > 0)
                t2 = InvasionUI.RefreshStaticUI(2, flag, false, AttackingTeam);
            if (flag.Team2TotalPlayers - flag.Team2TotalCappers > 0)
                t2v = InvasionUI.RefreshStaticUI(2, flag, true, AttackingTeam);
            foreach (Player capper in flag.PlayersOnFlag)
            {
                ulong t = capper.GetTeam();
                if (t == 1)
                {
                    if (capper.movement.getVehicle() == null)
                        t1.SendToPlayer(capper.channel.owner);
                    else
                        t1v.SendToPlayer(capper.channel.owner);
                }
                else if (t == 2)
                {
                    if (capper.movement.getVehicle() == null)
                        t2.SendToPlayer(capper.channel.owner);
                    else
                        t2v.SendToPlayer(capper.channel.owner);
                }
            }
        }
        protected override void FlagOwnerChanged(ulong OldOwner, ulong NewOwner, Flag flag)
        {
            if (NewOwner == 1)
            {
                if (_attackTeam == 1 && _objectiveT1Index >= _rotation.Count - 1) // if t1 just capped the last flag
                {
                    DeclareWin(1);
                    _objectiveT1Index = 0;
                    return;
                }
                else if (_attackTeam == 1)
                {
                    _objectiveT1Index = flag.index + 1;
                    InvokeOnObjectiveChanged(flag, _rotation[ObjectiveT1Index], NewOwner, flag.index, ObjectiveT1Index);
                    InvokeOnFlagCaptured(flag, NewOwner, OldOwner);
                    for (int i = 0; i < flag.PlayersOnFlagTeam1.Count; i++)
                    {
                        if (F.TryGetPlaytimeComponent(flag.PlayersOnFlagTeam1[i], out Components.PlaytimeComponent c) && c.stats is IFlagStats fg)
                            fg.AddCapture();
                    }
                }
            }
            else if (NewOwner == 2)
            {
                if (ObjectiveT2Index < 1) // if t2 just capped the last flag
                {
                    DeclareWin(2);
                    _objectiveT2Index = _rotation.Count - 1;
                    return;
                }
                else if (_attackTeam == 2)
                {
                    _objectiveT2Index = flag.index - 1;
                    InvokeOnObjectiveChanged(flag, _rotation[ObjectiveT2Index], NewOwner, flag.index, ObjectiveT2Index);
                    InvokeOnFlagCaptured(flag, NewOwner, OldOwner);
                    for (int i = 0; i < flag.PlayersOnFlagTeam2.Count; i++)
                    {
                        if (F.TryGetPlaytimeComponent(flag.PlayersOnFlagTeam2[i], out Components.PlaytimeComponent c) && c.stats is IFlagStats fg)
                            fg.AddCapture();
                    }
                }
            }
            else
            {
                if (OldOwner == _defenseTeam)
                {
                    if (OldOwner == 1)
                    {
                        int oldindex = ObjectiveT1Index;
                        _objectiveT1Index = flag.index;
                        if (oldindex != flag.index)
                        {
                            //InvokeOnObjectiveChanged(flag, flag, 0, oldindex, flag.index);
                            InvokeOnFlagNeutralized(flag, 2, 1);
                        }
                    }
                    else if (OldOwner == 2)
                    {
                        int oldindex = ObjectiveT2Index;
                        _objectiveT2Index = flag.index;
                        if (oldindex != flag.index)
                        {
                            //InvokeOnObjectiveChanged(_rotation[oldindex], flag, 0, oldindex, flag.index);
                            InvokeOnFlagNeutralized(flag, 1, 2);
                        }
                    }
                }
            }
            SendUIParameters t1 = SendUIParameters.Nil;
            SendUIParameters t2 = SendUIParameters.Nil;
            SendUIParameters t1v = SendUIParameters.Nil;
            SendUIParameters t2v = SendUIParameters.Nil;
            if (flag.Team1TotalCappers > 0)
                t1 = InvasionUI.RefreshStaticUI(1, flag, false, AttackingTeam);
            if (flag.Team1TotalPlayers - flag.Team1TotalCappers > 0)
                t1v = InvasionUI.RefreshStaticUI(1, flag, true, AttackingTeam);
            if (flag.Team2TotalCappers > 0)
                t2 = InvasionUI.RefreshStaticUI(2, flag, false, AttackingTeam);
            if (flag.Team2TotalPlayers - flag.Team2TotalCappers > 0)
                t2v = InvasionUI.RefreshStaticUI(2, flag, true, AttackingTeam);
            if (flag.Team1TotalPlayers > 0)
                foreach (Player player in flag.PlayersOnFlagTeam1)
                    (player.movement.getVehicle() == null ? t1 : t1v).SendToPlayer(player.channel.owner);
            if (flag.Team2TotalPlayers > 0)
                foreach (Player player in flag.PlayersOnFlagTeam2)
                    (player.movement.getVehicle() == null ? t2 : t2v).SendToPlayer(player.channel.owner);
            if (NewOwner == 0)
            {
                foreach (SteamPlayer client in Provider.clients)
                {
                    ulong team = client.GetTeam();
                    client.SendChat("flag_neutralized", UCWarfare.GetColor("flag_neutralized"),
                        flag.Discovered(team) ? flag.Name : Translation.Translate("undiscovered_flag", client.playerID.steamID.m_SteamID),
                        flag.TeamSpecificHexColor);
                }
            }
            else
            {
                foreach (SteamPlayer client in Provider.clients)
                {
                    ulong team = client.GetTeam();
                    client.SendChat("team_capture", UCWarfare.GetColor("team_capture"), Teams.TeamManager.TranslateName(NewOwner, client.playerID.steamID.m_SteamID),
                        TeamManager.GetTeamHexColor(NewOwner), flag.Discovered(team) ? flag.Name : Translation.Translate("undiscovered_flag", client.playerID.steamID.m_SteamID),
                        flag.TeamSpecificHexColor);
                }
            }
        }
        protected override void FlagPointsChanged(float NewPoints, float OldPoints, Flag flag)
        {
            if (NewPoints == 0)
                flag.SetOwner(0);
            SendUIParameters t1 = SendUIParameters.Nil;
            SendUIParameters t2 = SendUIParameters.Nil;
            SendUIParameters t1v = SendUIParameters.Nil;
            SendUIParameters t2v = SendUIParameters.Nil;
            if (flag.Team1TotalCappers > 0)
                t1 = InvasionUI.RefreshStaticUI(1, flag, false, AttackingTeam);
            if (flag.Team1TotalPlayers - flag.Team1TotalCappers > 0)
                t1v = InvasionUI.RefreshStaticUI(1, flag, true, AttackingTeam);
            if (flag.Team2TotalCappers > 0)
                t2 = InvasionUI.RefreshStaticUI(2, flag, false, AttackingTeam);
            if (flag.Team2TotalPlayers - flag.Team2TotalCappers > 0)
                t2v = InvasionUI.RefreshStaticUI(2, flag, true, AttackingTeam);
            foreach (Player player in flag.PlayersOnFlag)
            {
                byte team = player.GetTeamByte();
                if (team == 1)
                    (player.movement.getVehicle() == null ? t1 : t1v).SendToPlayer(player.channel.owner);
                else if (team == 2)
                    (player.movement.getVehicle() == null ? t2 : t2v).SendToPlayer(player.channel.owner);
            }
        }
        public override void OnGroupChanged(UCPlayer player, ulong oldGroup, ulong newGroup, ulong oldteam, ulong newteam)
        {
            CTFUI.ClearFlagList(player);
            if (_onFlag.TryGetValue(player.Player.channel.owner.playerID.steamID.m_SteamID, out int id))
                InvasionUI.RefreshStaticUI(newteam, _rotation.FirstOrDefault(x => x.ID == id)
                    ?? _rotation[0], player.Player.movement.getVehicle() != null, AttackingTeam)
                    .SendToPlayer(player.Player.channel.owner);
            InvasionUI.SendFlagList(player);
            base.OnGroupChanged(player, oldGroup, newGroup, oldteam, newteam);
        }
        public override void OnPlayerJoined(UCPlayer player, bool wasAlreadyOnline = false)
        {
            base.OnPlayerJoined(player, wasAlreadyOnline);
            if (isScreenUp && _endScreen != null)
            {
                _endScreen.SendLeaderboard(player, TeamManager.GetTeamHexColor(player.GetTeam()));
            }
            else
            {
                InvasionUI.SendFlagList(player);
                if (State == EState.STAGING)
                    this.ShowStagingUI(player);
            }
        }
        public override void ShowStagingUI(UCPlayer player)
        {
            EffectManager.sendUIEffect(CTFUI.headerID, CTFUI.headerKey, player.connection, true);
            if (player.GetTeam() == AttackingTeam)
                EffectManager.sendUIEffectText(CTFUI.headerKey, player.connection, true, "Top", Translation.Translate("phases_briefing", player));
            else if (player.GetTeam() == DefendingTeam)
                EffectManager.sendUIEffectText(CTFUI.headerKey, player.connection, true, "Top", Translation.Translate("phases_preparation", player));
        }
        protected override void EndStagingPhase()
        {
            base.EndStagingPhase();
            if (_attackTeam == 1)
                DestoryBlockerOnT1();
            else
                DestoryBlockerOnT2();
        }
        public override void Dispose()
        {
            foreach (SteamPlayer player in Provider.clients)
            {
                CTFUI.ClearFlagList(player.transportConnection);
                SendUIParameters.Nil.SendToPlayer(player); // clear all capturing uis
                if (F.TryGetPlaytimeComponent(player.player, out Components.PlaytimeComponent c))
                    c.stats = null;
            }
            base.Dispose();
        }
    }

    public class InvasionLeaderboard : BaseCTFLeaderboard<BaseCTFStats, InvasionTracker>
    {
        protected override Guid GUID => Gamemode.Config.UI.CTFLeaderboardGUID;
    }
}

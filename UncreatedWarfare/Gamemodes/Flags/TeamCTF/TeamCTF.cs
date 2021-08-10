using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Players;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Tickets;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Flags.TeamCTF
{
    public delegate Task ObjectiveChangedDelegate(Flag OldFlagObj, Flag NewFlagObj, ulong Team, int OldObj, int NewObj);
    public delegate Task FlagCapturedHandler(Flag flag, ulong capturedTeam, ulong lostTeam);
    public delegate Task FlagNeutralizedHandler(Flag flag, ulong capturedTeam, ulong lostTeam);
    public class TeamCTF : FlagGamemode
    {
        // vars
        protected Config<TeamCTFData> _config;
        public TeamCTFData Config { get => _config.Data; }
        public int ObjectiveT1Index;
        public int ObjectiveT2Index;
        public List<ulong> InAMC = new List<ulong>();
        public Flag ObjectiveTeam1 { get => Rotation[ObjectiveT1Index]; }
        public Flag ObjectiveTeam2 { get => Rotation[ObjectiveT2Index]; }

        // leaderboard
        private EndScreenLeaderboard EndScreen;
        public bool isScreenUp = false;
        public WarStatsTracker GameStats;


        // events
        public event ObjectiveChangedDelegate OnObjectiveChanged;
        public event FlagCapturedHandler OnFlagCaptured;
        public event FlagNeutralizedHandler OnFlagNeutralized;
        public event VoidDelegate OnNewGameStarting;
        public TeamCTF() : base(nameof(TeamCTF), 1f)
        {
            _config = new Config<TeamCTFData>(Data.FlagStorage, "config.json");
            SetTiming(Config.PlayerCheckSpeedSeconds);
        }
        public override void OnEvaluate()
        {
            IEnumerator<SteamPlayer> players = Provider.clients.GetEnumerator();
            while (players.MoveNext())
            {
                ulong team = players.Current.GetTeam();
                UCPlayer player = UCPlayer.FromSteamPlayer(players.Current);
                if (!player.OnDutyOrAdmin() && !players.Current.player.life.isDead && ((team == 1 && TeamManager.Team2AMC.IsInside(players.Current.player.transform.position)) || 
                    (team == 2 && TeamManager.Team1AMC.IsInside(players.Current.player.transform.position))))
                {
                    if (!InAMC.Contains(players.Current.playerID.steamID.m_SteamID))
                    {
                        InAMC.Add(players.Current.playerID.steamID.m_SteamID);
                        int a = Mathf.RoundToInt(Config.NearOtherBaseKillTimer);
                        ToastMessage.QueueMessage(players.Current,
                            F.Translate("entered_enemy_territory", players.Current.playerID.steamID.m_SteamID, a.ToString(Data.Locale), a.S()),
                            ToastMessageSeverity.WARNING);
                        UCWarfare.I.StartCoroutine(KillPlayerInEnemyTerritory(players.Current));
                    }
                } else
                {
                    InAMC.Remove(players.Current.playerID.steamID.m_SteamID);
                }
            }
            players.Dispose();
        }
        public IEnumerator<WaitForSeconds> KillPlayerInEnemyTerritory(SteamPlayer player)
        {
            yield return new WaitForSeconds(Config.NearOtherBaseKillTimer);
            if (player != null && !player.player.life.isDead && InAMC.Contains(player.playerID.steamID.m_SteamID))
            {
                player.player.movement.forceRemoveFromVehicle();
                player.player.life.askDamage(byte.MaxValue, Vector3.zero, EDeathCause.ACID, ELimb.SKULL, Provider.server, out _, false, ERagdollEffect.NONE, false, true);
            }
        }
        public override void OnPlayerDeath(UCWarfare.DeathEventArgs args)
        {
            InAMC.Remove(args.dead.channel.owner.playerID.steamID.m_SteamID);
            EventFunctions.RemoveDamageMessageTicks(args.dead.channel.owner.playerID.steamID.m_SteamID);
        }
        public override void Init()
        {
            GameStats = UCWarfare.I.gameObject.AddComponent<WarStatsTracker>();
        }
        protected override bool TimeToCheck()
        {
            if (_counter > Config.FlagCounterMax)
            {
                _counter = 0;
                return true;
            }
            else
            {
                _counter++;
                return false;
            }
        }
        protected override bool TimeToTicket()
        {
            if (_counter2 > 1 / Config.PlayerCheckSpeedSeconds)
            {
                _counter2 = 0;
                return true;
            }
            else
            {
                _counter2++;
                return false;
            }
        }
        public override void EvaluateTickets()
        {
            if (State == EState.ACTIVE)
            {
                TicketManager.GetTeamBleed(TeamManager.Team1ID, out int Team1Bleed, out _);
                TicketManager.GetTeamBleed(TeamManager.Team2ID, out int Team2Bleed, out _);

                if (TicketCounter % 60 == 0)
                {
                    if (Team1Bleed == -1)
                        TicketManager.Team1Tickets--;
                    if (Team2Bleed == -1)
                        TicketManager.Team2Tickets--;
                }
                if (TicketCounter % 30 == 0)
                {
                    if (Team1Bleed == -2)
                        TicketManager.Team1Tickets--;
                    if (Team2Bleed == -2)
                        TicketManager.Team2Tickets--;
                }
                if (TicketCounter % 10 == 0)
                {
                    if (Team1Bleed == -3)
                        TicketManager.Team1Tickets--;
                    if (Team2Bleed == -3)
                        TicketManager.Team2Tickets--;
                }
                if (TicketCounter % Config.xpSecondInterval == 0)
                {
                    TicketManager.OnFlagTick();
                }

                if (Team1Bleed < 0)
                    TicketManager.UpdateUITeam1();
                if (Team2Bleed < 0)
                    TicketManager.UpdateUITeam2();

                TicketCounter++;
                if (TicketCounter > 60)
                    TicketCounter = 0;
            }
        }
        public override void DeclareWin(ulong winner)
        {
            F.Log(TeamManager.TranslateName(winner, 0) + " just won the game!", ConsoleColor.Cyan);

            foreach (SteamPlayer client in Provider.clients)
            {
                client.SendChat("team_win", TeamManager.TranslateName(winner, client.playerID.steamID.m_SteamID), TeamManager.GetTeamHexColor(winner));
                client.player.movement.forceRemoveFromVehicle();

                ToastMessage.QueueMessage(client.player, "", F.Translate("team_win", client, TeamManager.TranslateName(winner, client.playerID.steamID.m_SteamID), TeamManager.GetTeamHexColor(winner)), ToastMessageSeverity.BIG);
            }
            this.State = EState.FINISHED;
            UCWarfare.ReplaceBarricadesAndStructures();
            Commands.ClearCommand.WipeVehiclesAndRespawn();
            Commands.ClearCommand.ClearItems();
            TicketManager.OnRoundWin(winner);
            StartCoroutine(EndGame2(winner));
        }
        private IEnumerator<WaitForSeconds> EndGame2(ulong winner)
        {
            yield return new WaitForSeconds(Config.end_delay);
            InvokeOnTeamWin(winner);
            if (Config.ShowLeaderboard)
            {
                EndScreen = UCWarfare.I.gameObject.AddComponent<EndScreenLeaderboard>();
                EndScreen.winner = winner;
                EndScreen.warstats = GameStats;
                EndScreen.OnLeaderboardExpired += OnShouldStartNewGame;
                EndScreen.ShuttingDown = shutdownAfterGame;
                EndScreen.ShuttingDownMessage = shutdownMessage;
                EndScreen.ShuttingDownPlayer = shutdownPlayer;
                isScreenUp = true;
                EndScreen.EndGame(Config.ProgressChars);
            }
            else OnShouldStartNewGame();
        }
        private void OnShouldStartNewGame()
        {
            if (EndScreen != default)
                EndScreen.OnLeaderboardExpired -= OnShouldStartNewGame;
            UnityEngine.Object.Destroy(EndScreen);
            isScreenUp = false;
            StartNextGame();
        }
        public void ReloadConfig()
        {
            _config.Reload();
        }
        public override void StartNextGame(bool onLoad = false)
        {
            F.Log("Loading new game.", ConsoleColor.Cyan);
            base.StartNextGame(onLoad); // set game id
            LoadRotation();
            State = EState.ACTIVE;
            EffectManager.ClearEffectByID_AllPlayers(Config.CaptureUI);
            GameStats.Reset();
            InvokeOnNewGameStarting(onLoad);
        }
        public override void LoadRotation()
        {
            if (AllFlags == null) return;
            ResetFlags();
            OnFlag.Clear();
            if (Config.PathingMode == ObjectivePathing.EPathingMode.AUTODISTANCE)
            {
                Config.PathingData.Set();
                Rotation = ObjectivePathing.CreateAutoPath(AllFlags);
            } 
            else if (Config.PathingMode == ObjectivePathing.EPathingMode.LEVELS)
            {
                Rotation = ObjectivePathing.CreatePathUsingLevels(AllFlags, Config.MaxFlagsPerLevel);
            } 
            else if (Config.PathingMode == ObjectivePathing.EPathingMode.ADJACENCIES)
            {
                Rotation = ObjectivePathing.PathWithAdjacents(AllFlags, Config.team1adjacencies, Config.team2adjacencies);
            } else
            {
                F.LogWarning("Invalid pathing value, no flags will be loaded. Expect errors.");
            }
            ObjectiveT1Index = 0;
            ObjectiveT2Index = Rotation.Count - 1;
            if (Config.DiscoveryForesight < 1)
            {
                F.LogWarning("Discovery Foresight is set to 0 in Flag Settings. The players can not see their next flags.");
            }
            else
            {
                for (int i = 0; i < Config.DiscoveryForesight; i++)
                {
                    if (i >= Rotation.Count || i < 0) break;
                    Rotation[i].Discover(1);
                }
                for (int i = Rotation.Count - 1; i > Rotation.Count - 1 - Config.DiscoveryForesight; i--)
                {
                    if (i >= Rotation.Count || i < 0) break;
                    Rotation[i].Discover(2);
                }
            }
            foreach (Flag flag in Rotation)
            {
                InitFlag(flag); //subscribe to abstract events.
            }
            foreach (SteamPlayer client in Provider.clients)
            {
                CTFUI.ClearListUI(client.transportConnection, Config.FlagUICount);
                CTFUI.SendFlagListUI(client.transportConnection, client.playerID.steamID.m_SteamID, client.GetTeam(), Rotation, Config.FlagUICount, Config.AttackIcon, Config.DefendIcon);
            }
            PrintFlagRotation();
            EvaluatePoints();
        }
        public override void PrintFlagRotation()
        {
            F.Log("Team 1 objective: " + ObjectiveTeam1.Name + ", Team 2 objective: " + ObjectiveTeam2.Name, ConsoleColor.Green);
            base.PrintFlagRotation();
        }
        private void InvokeOnObjectiveChanged(Flag OldFlagObj, Flag NewFlagObj, ulong Team, int OldObj, int NewObj)
        {
            if (OnObjectiveChanged != null)
                OnObjectiveChanged.Invoke(OldFlagObj, NewFlagObj, Team, OldObj, NewObj);
            if (Team != 0)
            {
                if (GameStats != null)
                    GameStats.totalFlagOwnerChanges++;
                F.Log("Team 1 objective: " + ObjectiveTeam1.Name + ", Team 2 objective: " + ObjectiveTeam2.Name, ConsoleColor.Green);
                if (UCWarfare.Config.FlagSettings.DiscoveryForesight > 0)
                {
                    if (Team == 1)
                    {
                        for (int i = NewFlagObj.index; i < NewFlagObj.index + UCWarfare.Config.FlagSettings.DiscoveryForesight; i++)
                        {
                            if (i >= Rotation.Count || i < 0) break;
                            Rotation[i].Discover(1);
                        }
                    }
                    else if (Team == 2)
                    {
                        for (int i = NewFlagObj.index; i > NewFlagObj.index - UCWarfare.Config.FlagSettings.DiscoveryForesight; i--)
                        {
                            if (i >= Rotation.Count || i < 0) break;
                            Rotation[i].Discover(2);
                        }
                    }
                }
            }
        }
        private void InvokeOnFlagCaptured(Flag flag, ulong capturedTeam, ulong lostTeam)
        {
            if (OnFlagCaptured != null)
                OnFlagCaptured.Invoke(flag, capturedTeam, lostTeam);
            TicketManager.OnFlagCaptured(flag, capturedTeam, lostTeam);
        }
        private void InvokeOnFlagNeutralized(Flag flag, ulong capturedTeam, ulong lostTeam)
        {
            if (OnFlagNeutralized != null)
                OnFlagNeutralized.Invoke(flag, capturedTeam, lostTeam);
            TicketManager.OnFlagNeutralized(flag, capturedTeam, lostTeam);
        }
        private void InvokeOnNewGameStarting(bool onLoad)
        {
            if (OnNewGameStarting != null)
                OnNewGameStarting.Invoke();
            TicketManager.OnNewGameStarting();
            if (!onLoad)
            {
                VehicleSpawner.RespawnAllVehicles();
                //FOBManager.WipeAllFOBRelatedBarricades(); (ran already kinda)
            }
            FOBManager.UpdateUIAll();
            RallyManager.WipeAllRallies();
        }
        protected override void PlayerEnteredFlagRadius(Flag flag, Player player)
        {
            ulong team = player.GetTeam();
            //F.Log("Player " + player.channel.owner.playerID.playerName + " entered flag " + flag.Name, ConsoleColor.White);
            player.SendChat("entered_cap_radius", UCWarfare.GetColor(team == 1 ? "entered_cap_radius_team_1" : (team == 2 ? "entered_cap_radius_team_2" : "default")), flag.Name, flag.ColorString);
            SendUIParameters t1 = SendUIParameters.Nil;
            SendUIParameters t2 = SendUIParameters.Nil;
            SendUIParameters t1v = SendUIParameters.Nil;
            SendUIParameters t2v = SendUIParameters.Nil;
            if (flag.Team1TotalCappers > 0)
                t1 = CTFUI.RefreshStaticUI(1, flag, false);
            if (flag.Team1TotalPlayers - flag.Team1TotalCappers > 0)
                t1v = CTFUI.RefreshStaticUI(1, flag, true);
            if (flag.Team2TotalCappers > 0)
                t2 = CTFUI.RefreshStaticUI(2, flag, false);
            if (flag.Team2TotalPlayers - flag.Team2TotalCappers > 0)
                t2v = CTFUI.RefreshStaticUI(2, flag, true);
            foreach (Player capper in flag.PlayersOnFlag)
            {
                ulong t = capper.GetTeam();
                if (t == 1)
                {
                    if (capper.movement.getVehicle() == null)
                        t1.SendToPlayer(Config.PlayerIcon, Config.UseUI, Config.CaptureUI, Config.ShowPointsOnUI, Config.ProgressChars, capper.channel.owner, capper.channel.owner.transportConnection);
                    else
                        t1v.SendToPlayer(Config.PlayerIcon, Config.UseUI, Config.CaptureUI, Config.ShowPointsOnUI, Config.ProgressChars, capper.channel.owner, capper.channel.owner.transportConnection);
                }
                else if (t == 2)
                {
                    if (capper.movement.getVehicle() == null)
                        t2.SendToPlayer(Config.PlayerIcon, Config.UseUI, Config.CaptureUI, Config.ShowPointsOnUI, Config.ProgressChars, capper.channel.owner, capper.channel.owner.transportConnection);
                    else
                        t2v.SendToPlayer(Config.PlayerIcon, Config.UseUI, Config.CaptureUI, Config.ShowPointsOnUI, Config.ProgressChars, capper.channel.owner, capper.channel.owner.transportConnection);
                }
            }
        }
        protected override void PlayerLeftFlagRadius(Flag flag, Player player)
        {
            ITransportConnection Channel = player.channel.owner.transportConnection;
            ulong team = player.GetTeam();
            F.Log("Player " + player.channel.owner.playerID.playerName + " left flag " + flag.Name, ConsoleColor.White);
            player.SendChat("left_cap_radius", UCWarfare.GetColor(team == 1 ? "left_cap_radius_team_1" : (team == 2 ? "left_cap_radius_team_2" : "default")), flag.Name, flag.ColorString);
            if (UCWarfare.Config.FlagSettings.UseUI)
                EffectManager.askEffectClearByID(UCWarfare.Config.FlagSettings.UIID, Channel);
            SendUIParameters t1 = SendUIParameters.Nil;
            SendUIParameters t2 = SendUIParameters.Nil;
            SendUIParameters t1v = SendUIParameters.Nil;
            SendUIParameters t2v = SendUIParameters.Nil;
            if (flag.Team1TotalCappers > 0)
                t1 = CTFUI.RefreshStaticUI(1, flag, false);
            if (flag.Team1TotalPlayers - flag.Team1TotalCappers > 0)
                t1v = CTFUI.RefreshStaticUI(1, flag, true);
            if (flag.Team2TotalCappers > 0)
                t2 = CTFUI.RefreshStaticUI(2, flag, false);
            if (flag.Team2TotalPlayers - flag.Team2TotalCappers > 0)
                t2v = CTFUI.RefreshStaticUI(2, flag, true);
            foreach (Player capper in flag.PlayersOnFlag)
            {
                ulong t = capper.GetTeam();
                if (t == 1)
                {
                    if (capper.movement.getVehicle() == null)
                        t1.SendToPlayer(Config.PlayerIcon, Config.UseUI, Config.CaptureUI, Config.ShowPointsOnUI, Config.ProgressChars, capper.channel.owner, capper.channel.owner.transportConnection);
                    else
                        t1v.SendToPlayer(Config.PlayerIcon, Config.UseUI, Config.CaptureUI, Config.ShowPointsOnUI, Config.ProgressChars, capper.channel.owner, capper.channel.owner.transportConnection);
                }
                else if (t == 2)
                {
                    if (capper.movement.getVehicle() == null)
                        t2.SendToPlayer(Config.PlayerIcon, Config.UseUI, Config.CaptureUI, Config.ShowPointsOnUI, Config.ProgressChars, capper.channel.owner, capper.channel.owner.transportConnection);
                    else
                        t2v.SendToPlayer(Config.PlayerIcon, Config.UseUI, Config.CaptureUI, Config.ShowPointsOnUI, Config.ProgressChars, capper.channel.owner, capper.channel.owner.transportConnection);
                }
            }
        }
        protected override void FlagOwnerChanged(ulong OldOwner, ulong NewOwner, Flag flag)
        {
            if (NewOwner == 1)
            {
                if (ObjectiveT1Index >= Rotation.Count - 1) // if t1 just capped the last flag
                {
                    DeclareWin(NewOwner);
                    ObjectiveT1Index = Rotation.Count - 1;
                    return;
                }
                else
                {
                    ObjectiveT1Index = flag.index + 1;
                    InvokeOnObjectiveChanged(flag, Rotation[ObjectiveT1Index], NewOwner, flag.index, ObjectiveT1Index);
                    InvokeOnFlagCaptured(flag, NewOwner, OldOwner);
                    for (int i = 0; i < flag.PlayersOnFlagTeam1.Count; i++)
                    {
                        if (F.TryGetPlaytimeComponent(flag.PlayersOnFlagTeam1[i], out Components.PlaytimeComponent c) && c.stats != null)
                            c.stats.captures++;
                    }
                }
            }
            else if (NewOwner == 2)
            {
                if (ObjectiveT2Index < 1) // if t2 just capped the last flag
                {
                    DeclareWin(NewOwner);
                    ObjectiveT2Index = 0;
                    return;
                }
                else
                {

                    ObjectiveT2Index = flag.index - 1;
                    InvokeOnObjectiveChanged(flag, Rotation[ObjectiveT2Index], NewOwner, flag.index, ObjectiveT2Index);
                    InvokeOnFlagCaptured(flag, NewOwner, OldOwner);
                    for (int i = 0; i < flag.PlayersOnFlagTeam2.Count; i++)
                    {
                        if (F.TryGetPlaytimeComponent(flag.PlayersOnFlagTeam2[i], out Components.PlaytimeComponent c) && c.stats != null)
                            c.stats.captures++;
                    }
                }
            }
            if (OldOwner == 1)
            {
                int oldindex = ObjectiveT1Index;
                ObjectiveT1Index = flag.index;
                if (oldindex != flag.index)
                {
                    InvokeOnObjectiveChanged(Rotation[oldindex], flag, 0, oldindex, flag.index);
                    InvokeOnFlagNeutralized(flag, 2, 1);
                }
            }
            else if (OldOwner == 2)
            {
                int oldindex = ObjectiveT2Index;
                ObjectiveT2Index = flag.index;
                if (oldindex != flag.index)
                {
                    InvokeOnObjectiveChanged(Rotation[oldindex], flag, 0, oldindex, flag.index);
                    InvokeOnFlagNeutralized(flag, 1, 2);
                }
            }
            SendUIParameters t1 = SendUIParameters.Nil;
            SendUIParameters t2 = SendUIParameters.Nil;
            SendUIParameters t1v = SendUIParameters.Nil;
            SendUIParameters t2v = SendUIParameters.Nil;
            if (flag.Team1TotalCappers > 0)
                t1 = CTFUI.RefreshStaticUI(1, flag, false);
            if (flag.Team1TotalPlayers - flag.Team1TotalCappers > 0)
                t1v = CTFUI.RefreshStaticUI(1, flag, true);
            if (flag.Team2TotalCappers > 0)
                t2 = CTFUI.RefreshStaticUI(2, flag, false);
            if (flag.Team2TotalPlayers - flag.Team2TotalCappers > 0)
                t2v = CTFUI.RefreshStaticUI(2, flag, true);
            if (flag.Team1TotalPlayers > 0)
                foreach (Player player in flag.PlayersOnFlagTeam1)
                    (player.movement.getVehicle() == null ? t1 : t1v).SendToPlayer(Config.PlayerIcon, Config.UseUI, Config.CaptureUI, Config.ShowPointsOnUI, Config.ProgressChars, player.channel.owner, player.channel.owner.transportConnection);
            if (flag.Team2TotalPlayers > 0)
                foreach (Player player in flag.PlayersOnFlagTeam2)
                    (player.movement.getVehicle() == null ? t2 : t2v).SendToPlayer(Config.PlayerIcon, Config.UseUI, Config.CaptureUI, Config.ShowPointsOnUI, Config.ProgressChars, player.channel.owner, player.channel.owner.transportConnection);
            if (NewOwner == 0)
            {
                foreach (SteamPlayer client in Provider.clients)
                {
                    ulong team = client.GetTeam();
                    client.SendChat("flag_neutralized", UCWarfare.GetColor("flag_neutralized"),
                        flag.Discovered(team) ? flag.Name : F.Translate("undiscovered_flag", client.playerID.steamID.m_SteamID),
                        flag.TeamSpecificHexColor);
                    CTFUI.SendFlagListUI(client.transportConnection, client.playerID.steamID.m_SteamID, team, Rotation, Config.FlagUICount, Config.AttackIcon, Config.DefendIcon);
                }
            }
            else
            {
                foreach (SteamPlayer client in Provider.clients)
                {
                    ulong team = client.GetTeam();
                    client.SendChat("team_capture", UCWarfare.GetColor("team_capture"), TeamManager.TranslateName(NewOwner, client.playerID.steamID.m_SteamID),
                        TeamManager.GetTeamHexColor(NewOwner), flag.Discovered(team) ? flag.Name : F.Translate("undiscovered_flag", client.playerID.steamID.m_SteamID),
                        flag.TeamSpecificHexColor);
                    CTFUI.SendFlagListUI(client.transportConnection, client.playerID.steamID.m_SteamID, team, Rotation, Config.FlagUICount, Config.AttackIcon, Config.DefendIcon);
                }
            }
        }
        protected override void FlagPointsChanged(int NewPoints, int OldPoints, Flag flag)
        {
            if (NewPoints == 0)
                flag.SetOwner(0);
            SendUIParameters t1 = SendUIParameters.Nil;
            SendUIParameters t2 = SendUIParameters.Nil;
            SendUIParameters t1v = SendUIParameters.Nil;
            SendUIParameters t2v = SendUIParameters.Nil;
            if (flag.Team1TotalCappers > 0)
                t1 = CTFUI.RefreshStaticUI(1, flag, false);
            if (flag.Team1TotalPlayers - flag.Team1TotalCappers > 0)
                t1v = CTFUI.RefreshStaticUI(1, flag, true);
            if (flag.Team2TotalCappers > 0)
                t2 = CTFUI.RefreshStaticUI(2, flag, false);
            if (flag.Team2TotalPlayers - flag.Team2TotalCappers > 0)
                t2v = CTFUI.RefreshStaticUI(2, flag, true);
            foreach (Player player in flag.PlayersOnFlag)
            {
                byte team = player.GetTeamByte();
                if (team == 1) 
                    (player.movement.getVehicle() == null ? t1 : t1v).SendToPlayer(Config.PlayerIcon, Config.UseUI, Config.CaptureUI, Config.ShowPointsOnUI, Config.ProgressChars, player.channel.owner, player.channel.owner.transportConnection);
                else if (team == 2) 
                    (player.movement.getVehicle() == null ? t2 : t2v).SendToPlayer(Config.PlayerIcon, Config.UseUI, Config.CaptureUI, Config.ShowPointsOnUI, Config.ProgressChars, player.channel.owner, player.channel.owner.transportConnection);
            }
        }
        public override void OnGroupChanged(SteamPlayer player, ulong oldGroup, ulong newGroup, ulong oldteam, ulong newteam)
        {
            CTFUI.ClearListUI(player.transportConnection, Config.FlagUICount);
            if (OnFlag.ContainsKey(player.playerID.steamID.m_SteamID))
                CTFUI.RefreshStaticUI(newteam, Rotation.FirstOrDefault(x => x.ID == OnFlag[player.playerID.steamID.m_SteamID])
                    ?? Rotation[0], player.player.movement.getVehicle() != null).SendToPlayer(Config.PlayerIcon, Config.UseUI, Config.CaptureUI, Config.ShowPointsOnUI, Config.ProgressChars, player, player.transportConnection);
            CTFUI.SendFlagListUI(player.transportConnection, player.playerID.steamID.m_SteamID, newGroup, Rotation, Config.FlagUICount, Config.AttackIcon, Config.DefendIcon);
        }
        public override void OnPlayerJoined(SteamPlayer player)
        {
            GameStats.AddPlayer(player.player);
            if (isScreenUp && EndScreen != null && Config.ShowLeaderboard)
            {
                EndScreen.SendScreenToPlayer(player, Config.ProgressChars);
            } else
            {
                CTFUI.SendFlagListUI(player.transportConnection, player.playerID.steamID.m_SteamID, player.GetTeam(), Rotation, Config.FlagUICount, Config.AttackIcon, Config.DefendIcon);
            }
        }
        public override void OnPlayerLeft(ulong player)
        {
            foreach (Flag flag in Rotation)
                flag.RecalcCappers(true);
        }
        public override void Dispose()
        {
            foreach (SteamPlayer player in Provider.clients)
            {
                CTFUI.ClearListUI(player.transportConnection, Config.FlagUICount);
                SendUIParameters.Nil.SendToPlayer(Config.PlayerIcon, Config.UseUI, Config.CaptureUI, Config.ShowPointsOnUI, Config.ProgressChars, player, player.transportConnection); // clear all capturing uis
            }
            base.Dispose();
        }
    }


    public class TeamCTFData : ConfigData
    {
        public float PlayerCheckSpeedSeconds;
        public bool UseUI;
        public bool UseChat;
        [JsonConverter(typeof(StringEnumConverter))]
        public ObjectivePathing.EPathingMode PathingMode;
        public int MaxFlagsPerLevel;
        public ushort CaptureUI;
        public ushort FlagUIIdFirst;
        public int FlagUICount;
        public bool EnablePlayerCount;
        public bool ShowPointsOnUI;
        public int FlagCounterMax;
        public bool AllowPlayersToCaptureInVehicle;
        public bool HideUnknownFlags;
        public uint DiscoveryForesight;
        public int RequiredPlayerDifferenceToCapture;
        public string ProgressChars;
        public char PlayerIcon;
        public char AttackIcon;
        public char DefendIcon;
        public bool ShowLeaderboard;
        public AutoObjectiveData PathingData;
        public int end_delay;
        public float NearOtherBaseKillTimer;
        public int xpSecondInterval;
        // 0-360
        public float team1spawnangle;
        public float team2spawnangle;
        public float lobbyspawnangle;
        public Dictionary<int, float> team1adjacencies;
        public Dictionary<int, float> team2adjacencies;
        public TeamCTFData() => SetDefaults();
        public override void SetDefaults()
        {
            this.PlayerCheckSpeedSeconds = 0.25f;
            this.PathingMode = ObjectivePathing.EPathingMode.ADJACENCIES;
            this.MaxFlagsPerLevel = 2;
            this.UseUI = true;
            this.UseChat = false;
            this.CaptureUI = 36000;
            this.FlagUIIdFirst = 36010;
            this.FlagUICount = 10;
            this.EnablePlayerCount = true;
            this.ShowPointsOnUI = true;
            this.FlagCounterMax = 1;
            this.HideUnknownFlags = true;
            this.DiscoveryForesight = 2;
            this.AllowPlayersToCaptureInVehicle = false;
            this.RequiredPlayerDifferenceToCapture = 2;
            this.ProgressChars = "¶·¸¹º»:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            this.PlayerIcon = '³';
            this.AttackIcon = 'µ';
            this.DefendIcon = '´';
            this.ShowLeaderboard = true;
            this.PathingData = new AutoObjectiveData();
            this.end_delay = 15;
            this.NearOtherBaseKillTimer = 10f;
            this.team1spawnangle = 0f;
            this.team2spawnangle = 0f;
            this.lobbyspawnangle = 0f;
            this.team1adjacencies = new Dictionary<int, float>();
            this.team2adjacencies = new Dictionary<int, float>();
            this.xpSecondInterval = 10;
        }
        public class AutoObjectiveData
        {
            public float main_search_radius;
            public float main_stop_radius;
            public float absolute_max_distance_from_main;
            public float flag_search_radius;
            public float forward_bias;
            public float back_bias;
            public float left_bias;
            public float right_bias;
            public float distance_falloff;
            public float average_distance_buffer;
            public float radius_tuning_resolution;
            public int max_flags;
            public int min_flags;
            public int max_redos;
            public AutoObjectiveData()
            {
                main_search_radius = 1200f;
                main_stop_radius = 1600f;
                absolute_max_distance_from_main = 1900f;
                flag_search_radius = 1200f;
                forward_bias = 0.65f;
                back_bias = 0.05f;
                left_bias = 0.15f;
                right_bias = 0.15f;
                distance_falloff = 0.1f;
                average_distance_buffer = 2400f;
                radius_tuning_resolution = 40f;
                max_flags = 8;
                min_flags = 5;
                max_redos = 20;
            }
            [JsonConstructor]
            public AutoObjectiveData(
                float main_search_radius, 
                float main_stop_radius, 
                float absolute_max_distance_from_main, 
                float flag_search_radius, 
                float forward_bias, 
                float back_bias, 
                float left_bias, 
                float right_bias, 
                float distance_falloff, 
                float average_distance_buffer, 
                float radius_tuning_resolution, 
                int max_flags, 
                int min_flags, 
                int max_redos
                )
            {
                this.main_search_radius = main_search_radius;
                this.main_stop_radius = main_stop_radius;
                this.absolute_max_distance_from_main = absolute_max_distance_from_main;
                this.flag_search_radius = flag_search_radius;
                this.forward_bias = forward_bias;
                this.back_bias = back_bias;
                this.right_bias = right_bias;
                this.left_bias = left_bias;
                this.distance_falloff = distance_falloff;
                this.average_distance_buffer = average_distance_buffer;
                this.radius_tuning_resolution = radius_tuning_resolution;
                this.max_flags = max_flags;
                this.min_flags = min_flags;
                this.max_redos = max_redos;
            }
            public void Set()
            {
                ObjectivePathing.SetVariables(
                    main_search_radius, 
                    main_stop_radius, 
                    absolute_max_distance_from_main, 
                    flag_search_radius, 
                    forward_bias, 
                    back_bias, 
                    left_bias,
                    right_bias, 
                    distance_falloff, 
                    average_distance_buffer, 
                    radius_tuning_resolution, 
                    max_flags, 
                    min_flags, 
                    max_redos
                );
            }
        }
    }
}

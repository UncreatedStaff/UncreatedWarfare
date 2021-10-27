using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using UnityEngine;
using Uncreated.Warfare.Teams;
using Uncreated.Players;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Revives;
using Uncreated.Warfare.Tickets;
using Uncreated.Warfare.Stats;

namespace Uncreated.Warfare.Gamemodes.Flags.Invasion
{
    public class Invasion : TicketGamemode
    {
        const float MATCH_PRESENT_THRESHOLD = 0.65f;

        public override string DisplayName => "Invasion";
        private readonly Config<InvasionData> _config;
        public InvasionData Config { get => _config.Data; }

        public WarStatsTracker GameStats;
        private EndScreenLeaderboard EndScreen;
        public bool isScreenUp = false;

        public ulong AttackTeam;
        public ulong DefendTeam;

        public int ObjectiveT1Index;
        public int ObjectiveT2Index;
        public Flag ObjectiveTeam1
        {
            get
            {
                if (ObjectiveT1Index != -1)
                    return Rotation[ObjectiveT1Index];
                else
                    return null;
            }
        }
        public Flag ObjectiveTeam2
        {
            get
            {
                if (ObjectiveT2Index != -1)
                    return Rotation[ObjectiveT2Index];
                else
                    return null;
            }
        }

        public VehicleSpawner VehicleSpawner;
        public VehicleBay VehicleBay;
        public VehicleSigns VehicleSigns;
        public FOBManager FOBManager;
        public BuildManager BuildManager;
        public RequestSigns RequestSignManager;
        public KitManager KitManager;
        public ReviveManager ReviveManager;
        public SquadManager SquadManager;
        public Invasion(string Name, float EventLoopSpeed) : base(Name, EventLoopSpeed)
        {
            _config = new Config<InvasionData>(Data.FlagStorage, "invasion.json");
        }
        public override void Init()
        {
            base.Init();
            FOBManager = new FOBManager();
            BuildManager = new BuildManager();
            SquadManager = new SquadManager();
            KitManager = new KitManager();
            ReviveManager = new ReviveManager();
            GameStats = UCWarfare.I.gameObject.AddComponent<WarStatsTracker>();
        }
        public override void StartNextGame(bool onLoad = false)
        {
            F.Log("Loading new game.", ConsoleColor.Cyan);
            base.StartNextGame(onLoad); // set game id

            AttackTeam = (ulong)UnityEngine.Random.Range(1, 3);
            if (AttackTeam == 1)
                DefendTeam = 2;
            else if (AttackTeam == 2)
                DefendTeam = 1;

            LoadRotation();
            EffectManager.ClearEffectByID_AllPlayers(Config.CaptureUI);
            GameStats.Reset();

            InvokeOnNewGameStarting(onLoad);
        }
        private void InvokeOnNewGameStarting(bool onLoad)
        {
            TicketManager.OnNewGameStarting();
            if (!onLoad)
            {
                VehicleSpawner.RespawnAllVehicles();
            }
            FOBManager.UpdateUIAll();
            RallyManager.WipeAllRallies();
        }
        private void InvokeOnObjectiveChanged(Flag OldFlagObj, Flag NewFlagObj, ulong Team, int OldObj, int NewObj)
        {
            if (Team != 0)
            {
                if (GameStats != null)
                    GameStats.totalFlagOwnerChanges++;
                F.Log("Team 1 objective: " + ObjectiveTeam1?.Name ?? "null" + ", Team 2 objective: " + ObjectiveTeam2?.Name ?? "null", ConsoleColor.Green);
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
            TicketManager.OnFlagCaptured(flag, capturedTeam, lostTeam);
            Stats.StatsManager.ModifyTeam(capturedTeam, t => t.FlagsCaptured++, false);
            Stats.StatsManager.ModifyTeam(lostTeam, t => t.FlagsLost++, false);
            List<string> kits = new List<string>();
            if (capturedTeam == 1)
            {
                for (int p = 0; p < flag.PlayersOnFlagTeam1.Count; p++)
                {
                    Stats.StatsManager.ModifyStats(flag.PlayersOnFlagTeam1[p].channel.owner.playerID.steamID.m_SteamID, s => s.FlagsCaptured++, false);
                    if (Kits.KitManager.HasKit(flag.PlayersOnFlagTeam1[p], out Kit kit) && !kits.Contains(kit.Name))
                    {
                        Stats.StatsManager.ModifyKit(kit.Name, k => k.FlagsCaptured++, true);
                        kits.Add(kit.Name);
                    }
                }
                if (flag.IsObj(2))
                    for (int p = 0; p < flag.PlayersOnFlagTeam2.Count; p++)
                        Stats.StatsManager.ModifyStats(flag.PlayersOnFlagTeam2[p].channel.owner.playerID.steamID.m_SteamID, s => s.FlagsLost++, false);
            }
            else if (capturedTeam == 2)
            {
                if (flag.IsObj(1))
                    for (int p = 0; p < flag.PlayersOnFlagTeam1.Count; p++)
                        Stats.StatsManager.ModifyStats(flag.PlayersOnFlagTeam1[p].channel.owner.playerID.steamID.m_SteamID, s => s.FlagsLost++, false);
                for (int p = 0; p < flag.PlayersOnFlagTeam2.Count; p++)
                {
                    Stats.StatsManager.ModifyStats(flag.PlayersOnFlagTeam2[p].channel.owner.playerID.steamID.m_SteamID, s => s.FlagsCaptured++, false);
                    if (Kits.KitManager.HasKit(flag.PlayersOnFlagTeam2[p], out Kit kit) && !kits.Contains(kit.Name))
                    {
                        Stats.StatsManager.ModifyKit(kit.Name, k => k.FlagsCaptured++, true);
                        kits.Add(kit.Name);
                    }
                }
            }
        }
        private void InvokeOnFlagNeutralized(Flag flag, ulong capturedTeam, ulong lostTeam)
        {
            TicketManager.OnFlagNeutralized(flag, capturedTeam, lostTeam);
        }
        public override void DeclareWin(ulong winner)
        {
            F.Log(TeamManager.TranslateName(winner, 0) + " just won the game!", ConsoleColor.Cyan);

            foreach (SteamPlayer client in Provider.clients)
            {
                client.SendChat("team_win", TeamManager.TranslateName(winner, client.playerID.steamID.m_SteamID), TeamManager.GetTeamHexColor(winner));
                client.player.movement.forceRemoveFromVehicle();
                EffectManager.askEffectClearByID(UCWarfare.Config.GiveUpUI, client.transportConnection);
                ToastMessage.QueueMessage(client.player, "", F.Translate("team_win", client, TeamManager.TranslateName(winner, client.playerID.steamID.m_SteamID), TeamManager.GetTeamHexColor(winner)), ToastMessageSeverity.BIG);
            }
            Stats.StatsManager.ModifyTeam(winner, t => t.Wins++, false);
            Stats.StatsManager.ModifyTeam(Teams.TeamManager.Other(winner), t => t.Losses++, false);
            foreach (PlayerCurrentGameStats played in GameStats.playerstats.Values)
            {
                // Any player who was online for 70% of the match will be awarded a win or punished with a loss
                if ((float)played.onlineCount1 / GameStats.gamepercentagecounter >= MATCH_PRESENT_THRESHOLD)
                {
                    if (winner == 1)
                        Stats.StatsManager.ModifyStats(played.id, s => s.Wins++, false);
                    else
                        Stats.StatsManager.ModifyStats(played.id, s => s.Losses++, false);
                }
                else if ((float)played.onlineCount2 / GameStats.gamepercentagecounter >= MATCH_PRESENT_THRESHOLD)
                {
                    if (winner == 2)
                        Stats.StatsManager.ModifyStats(played.id, s => s.Wins++, false);
                    else
                        Stats.StatsManager.ModifyStats(played.id, s => s.Losses++, false);
                }
            }
            this.State = EState.FINISHED;
            ReplaceBarricadesAndStructures();
            Commands.ClearCommand.WipeVehiclesAndRespawn();
            Commands.ClearCommand.ClearItems();
            TicketManager.OnRoundWin(winner);
            StartCoroutine(EndGameCoroutine(winner));
        }
        private IEnumerator<WaitForSeconds> EndGameCoroutine(ulong winner)
        {
            yield return new WaitForSeconds(Config.end_delay);
            InvokeOnTeamWin(winner);
            if (Config.ShowLeaderboard)
            {
                EndScreen = UCWarfare.I.gameObject.AddComponent<EndScreenLeaderboard>();
                EndScreen.winner = winner;
                EndScreen.Gamemode = this;
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
            Destroy(EndScreen);
            isScreenUp = false;
            StartNextGame();
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
            }
            else
            {
                F.LogWarning("Invalid pathing value, no flags will be loaded. Expect errors.");
            }
            if (AttackTeam == 1)
            {
                ObjectiveT1Index = 0;
                ObjectiveT2Index = -1;
            }
            else if (AttackTeam == 2)
            {
                ObjectiveT2Index = Rotation.Count - 1;
                ObjectiveT1Index = -1;
            }
            if (Config.DiscoveryForesight < 1)
            {
                F.LogWarning("Discovery Foresight is set to 0 in Flag Settings. The players can not see their next flags.");
            }
            else
            {
                foreach (var flag in Rotation)
                {
                    flag.Discover(DefendTeam);
                }


                if (AttackTeam == 1)
                {
                    for (int i = 0; i < Config.DiscoveryForesight; i++)
                    {
                        if (i >= Rotation.Count || i < 0) break;
                        Rotation[i].Discover(1);
                    }
                }
                else if (AttackTeam == 2)
                {
                    for (int i = Rotation.Count - 1; i > Rotation.Count - 1 - Config.DiscoveryForesight; i--)
                    {
                        if (i >= Rotation.Count || i < 0) break;
                        Rotation[i].Discover(2);
                    }
                }
                
            }
            foreach (Flag flag in Rotation)
            {
                InitFlag(flag); //subscribe to abstract events.
            }
            foreach (SteamPlayer client in Provider.clients)
            {
                InvasionUI.ClearListUI(client.transportConnection, Config.FlagUICount);
                InvasionUI.SendFlagListUI(client.transportConnection, client.playerID.steamID.m_SteamID, client.GetTeam(), Rotation, Config.FlagUICount, Config.AttackIcon, Config.DefendIcon);
            }
            PrintFlagRotation();
            EvaluatePoints();
        }
        public override void InitFlag(Flag flag)
        {
            base.InitFlag(flag);

            flag.SetOwnerNoEventInvocation(DefendTeam);
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
                    if (AttackTeam == 1)
                    {
                        ObjectiveT1Index = flag.index + 1;
                    }
                    else
                    {
                        ObjectiveT1Index = -1;
                    }
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

                    if (AttackTeam == 2)
                    {
                        ObjectiveT2Index = flag.index - 1;
                    }
                    else
                    {
                        ObjectiveT2Index = -1;
                    }
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
                    client.SendChat("team_capture", UCWarfare.GetColor("team_capture"), Teams.TeamManager.TranslateName(NewOwner, client.playerID.steamID.m_SteamID),
                        Teams.TeamManager.GetTeamHexColor(NewOwner), flag.Discovered(team) ? flag.Name : F.Translate("undiscovered_flag", client.playerID.steamID.m_SteamID),
                        flag.TeamSpecificHexColor);
                    CTFUI.SendFlagListUI(client.transportConnection, client.playerID.steamID.m_SteamID, team, Rotation, Config.FlagUICount, Config.AttackIcon, Config.DefendIcon);
                }
            }
        }
        protected override void FlagPointsChanged(float NewPoints, float OldPoints, Flag flag) => throw new NotImplementedException();
        protected override void PlayerEnteredFlagRadius(Flag flag, Player player)
        {
            ulong team = player.GetTeam();
            if (UCWarfare.Config.Debug)
                F.Log("Player " + player.channel.owner.playerID.playerName + " entered flag " + flag.Name, ConsoleColor.White);
            player.SendChat("entered_cap_radius", UCWarfare.GetColor(team == 1 ? "entered_cap_radius_team_1" : (team == 2 ? "entered_cap_radius_team_2" : "default")), flag.Name, flag.ColorString);
            SendUIParameters t1 = SendUIParameters.Nil;
            SendUIParameters t2 = SendUIParameters.Nil;
            SendUIParameters t1v = SendUIParameters.Nil;
            SendUIParameters t2v = SendUIParameters.Nil;
            if (flag.Team1TotalCappers > 0)
                t1 = InvasionUI.RefreshStaticUI(1, flag, false);
            if (flag.Team1TotalPlayers - flag.Team1TotalCappers > 0)
                t1v = InvasionUI.RefreshStaticUI(1, flag, true);
            if (flag.Team2TotalCappers > 0)
                t2 = InvasionUI.RefreshStaticUI(2, flag, false);
            if (flag.Team2TotalPlayers - flag.Team2TotalCappers > 0)
                t2v = InvasionUI.RefreshStaticUI(2, flag, true);
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
            if (UCWarfare.Config.Debug)
                F.Log("Player " + player.channel.owner.playerID.playerName + " left flag " + flag.Name, ConsoleColor.White);
            player.SendChat("left_cap_radius", UCWarfare.GetColor(team == 1 ? "left_cap_radius_team_1" : (team == 2 ? "left_cap_radius_team_2" : "default")), flag.Name, flag.ColorString);
            if (UCWarfare.Config.FlagSettings.UseUI)
                EffectManager.askEffectClearByID(UCWarfare.Config.FlagSettings.UIID, Channel);
            SendUIParameters t1 = SendUIParameters.Nil;
            SendUIParameters t2 = SendUIParameters.Nil;
            SendUIParameters t1v = SendUIParameters.Nil;
            SendUIParameters t2v = SendUIParameters.Nil;
            if (flag.Team1TotalCappers > 0)
                t1 = InvasionUI.RefreshStaticUI(1, flag, false);
            if (flag.Team1TotalPlayers - flag.Team1TotalCappers > 0)
                t1v = InvasionUI.RefreshStaticUI(1, flag, true);
            if (flag.Team2TotalCappers > 0)
                t2 = InvasionUI.RefreshStaticUI(2, flag, false);
            if (flag.Team2TotalPlayers - flag.Team2TotalCappers > 0)
                t2v = InvasionUI.RefreshStaticUI(2, flag, true);
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

        public override void OnGroupChanged(SteamPlayer player, ulong oldGroup, ulong newGroup, ulong oldteam, ulong newteam)
        {
            CTFUI.ClearListUI(player.transportConnection, Config.FlagUICount);
            if (OnFlag.ContainsKey(player.playerID.steamID.m_SteamID))
                CTFUI.RefreshStaticUI(newteam, Rotation.FirstOrDefault(x => x.ID == OnFlag[player.playerID.steamID.m_SteamID])
                    ?? Rotation[0], player.player.movement.getVehicle() != null).SendToPlayer(Config.PlayerIcon, Config.UseUI, Config.CaptureUI, Config.ShowPointsOnUI, Config.ProgressChars, player, player.transportConnection);
            CTFUI.SendFlagListUI(player.transportConnection, player.playerID.steamID.m_SteamID, newGroup, Rotation, Config.FlagUICount, Config.AttackIcon, Config.DefendIcon);
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
        protected override void EvaluateTickets() => throw new NotImplementedException();

        public override void OnPlayerJoined(UCPlayer player)
        {
            if (KitManager.KitExists(player.KitName, out Kit kit))
            {
                if (kit.IsLimited(out int currentPlayers, out int allowedPlayers, player.GetTeam()) || (kit.IsLoadout && kit.IsClassLimited(out currentPlayers, out allowedPlayers, player.GetTeam())))
                {
                    if (!KitManager.TryGiveRiflemanKit(player))
                        KitManager.TryGiveUnarmedKit(player);
                }
            }
            ReviveManager.DownedPlayers.Remove(player.CSteamID.m_SteamID);

            ulong team = player.GetTeam();
            FPlayerName names = F.GetPlayerOriginalNames(player);
            if ((player.KitName == null || player.KitName == string.Empty) && team > 0 && team < 3)
            {
                if (KitManager.KitExists(team == 1 ? TeamManager.Team1UnarmedKit : TeamManager.Team2UnarmedKit, out Kit unarmed))
                    KitManager.GiveKit(player, unarmed);
                else if (KitManager.KitExists(TeamManager.DefaultKit, out unarmed)) KitManager.GiveKit(player, unarmed);
                else F.LogWarning("Unable to give " + names.PlayerName + " a kit.");
            }
            ReviveManager.OnPlayerConnected(player);
            if (!AllowCosmetics)
            {
                player.Player.clothing.ServerSetVisualToggleState(EVisualToggleType.COSMETIC, false);
                player.Player.clothing.ServerSetVisualToggleState(EVisualToggleType.MYTHIC, false);
                player.Player.clothing.ServerSetVisualToggleState(EVisualToggleType.SKIN, false);
            }
            if (UCWarfare.Config.ModifySkillLevels)
            {
                player.Player.skills.ServerSetSkillLevel((int)EPlayerSpeciality.OFFENSE, (int)EPlayerOffense.SHARPSHOOTER, 7);
                player.Player.skills.ServerSetSkillLevel((int)EPlayerSpeciality.OFFENSE, (int)EPlayerOffense.PARKOUR, 2);
                player.Player.skills.ServerSetSkillLevel((int)EPlayerSpeciality.OFFENSE, (int)EPlayerOffense.EXERCISE, 1);
                player.Player.skills.ServerSetSkillLevel((int)EPlayerSpeciality.OFFENSE, (int)EPlayerOffense.CARDIO, 5);
                player.Player.skills.ServerSetSkillLevel((int)EPlayerSpeciality.DEFENSE, (int)EPlayerDefense.VITALITY, 5);
            }
            GameStats.AddPlayer(player.Player);
            if (isScreenUp && EndScreen != null && Config.ShowLeaderboard)
            {
                EndScreen.SendScreenToPlayer(player.Player.channel.owner, Config.ProgressChars);
            }
            else
            {
                CTFUI.SendFlagListUI(player.Player.channel.owner.transportConnection, player.Player.channel.owner.playerID.steamID.m_SteamID, player.GetTeam(), Rotation, Config.FlagUICount, Config.AttackIcon, Config.DefendIcon);
            }
            StatsManager.RegisterPlayer(player.CSteamID.m_SteamID);
            StatsManager.ModifyStats(player.CSteamID.m_SteamID, s => s.LastOnline = DateTime.Now.Ticks);
        }
        public override void OnPlayerLeft(UCPlayer player)
        {
            foreach (Flag flag in Rotation)
                flag.RecalcCappers(true);
            StatsCoroutine.previousPositions.Remove(player.Player.channel.owner.playerID.steamID.m_SteamID);
            ReviveManager.OnPlayerDisconnected(player.Player.channel.owner);
            StatsManager.DeregisterPlayer(player.CSteamID.m_SteamID);
        }
        public override void Dispose()
        {
            foreach (SteamPlayer player in Provider.clients)
            {
                CTFUI.ClearListUI(player.transportConnection, Config.FlagUICount);
                SendUIParameters.Nil.SendToPlayer(Config.PlayerIcon, Config.UseUI, Config.CaptureUI, Config.ShowPointsOnUI, Config.ProgressChars, player, player.transportConnection); // clear all capturing uis
            }
            SquadManager?.Dispose();
            VehicleSpawner?.Dispose();
            ReviveManager?.Dispose();
            KitManager?.Dispose();
            base.Dispose();
        }
        public override void OnLevelLoaded()
        {
            VehicleBay = new VehicleBay();
            VehicleSpawner = new VehicleSpawner();
            VehicleSigns = new VehicleSigns();
            RequestSignManager = new RequestSigns();
            base.OnLevelLoaded();
            FOBManager.LoadFobs();
            RepairManager.LoadRepairStations();
            RallyManager.WipeAllRallies();
            VehicleSigns.InitAllSigns();
        }
        protected override void EventLoopAction()
        {
            base.EventLoopAction();
            FOBManager.OnGameTick(TicketCounter);
        }


        public override void Subscribe()
        {
            UseableConsumeable.onPerformedAid += EventHandlers.OnPostHealedPlayer;
            Patches.BarricadeDestroyedHandler += EventHandlers.OnBarricadeDestroyed;
            Patches.StructureDestroyedHandler += EventHandlers.OnStructureDestroyed;
            PlayerInput.onPluginKeyTick += EventHandlers.OnPluginKeyPressed;
            base.Subscribe();
        }
        public override void Unsubscribe()
        {
            UseableConsumeable.onPerformedAid -= EventHandlers.OnPostHealedPlayer;
            Patches.BarricadeDestroyedHandler -= EventHandlers.OnBarricadeDestroyed;
            Patches.StructureDestroyedHandler -= EventHandlers.OnStructureDestroyed;
            PlayerInput.onPluginKeyTick -= EventHandlers.OnPluginKeyPressed;
            base.Unsubscribe();
        }
    }

    public class InvasionData : ConfigData
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
        public TeamCTFData.AutoObjectiveData PathingData;
        public int end_delay;
        public float NearOtherBaseKillTimer;
        public int xpSecondInterval;
        // 0-360
        public float team1spawnangle;
        public float team2spawnangle;
        public float lobbyspawnangle;
        public Dictionary<int, float> team1adjacencies;
        public Dictionary<int, float> team2adjacencies;
        public InvasionData() => SetDefaults();
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
            this.PathingData = new TeamCTFData.AutoObjectiveData();
            this.end_delay = 15;
            this.NearOtherBaseKillTimer = 10f;
            this.team1spawnangle = 0f;
            this.team2spawnangle = 0f;
            this.lobbyspawnangle = 0f;
            this.team1adjacencies = new Dictionary<int, float>();
            this.team2adjacencies = new Dictionary<int, float>();
            this.xpSecondInterval = 10;
        }
    }
}

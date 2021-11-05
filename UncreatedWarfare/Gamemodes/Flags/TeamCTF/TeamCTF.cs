using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Uncreated.Players;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Revives;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Tickets;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Flags.TeamCTF
{
    public delegate Task ObjectiveChangedDelegate(Flag OldFlagObj, Flag NewFlagObj, ulong Team, int OldObj, int NewObj);
    public delegate Task FlagCapturedHandler(Flag flag, ulong capturedTeam, ulong lostTeam);
    public delegate Task FlagNeutralizedHandler(Flag flag, ulong capturedTeam, ulong lostTeam);
    public class TeamCTF : TicketGamemode, IFlagTeamObjectiveGamemode, IWarstatsGamemode, IVehicles, IFOBs, IKitRequests, IRevives, ISquads, IImplementsLeaderboard, IStructureSaving, IStagingPhase
    {
        const float MATCH_PRESENT_THRESHOLD = 0.65f;
        // vars
        protected Config<TeamCTFData> _config;
        public TeamCTFData Config => _config.Data;
        protected int _objectiveT1Index;
        protected int _objectiveT2Index;
        public int ObjectiveT1Index => _objectiveT1Index;
        public int ObjectiveT2Index => _objectiveT2Index;
        public Flag ObjectiveTeam1 => _rotation[_objectiveT1Index];
        public Flag ObjectiveTeam2 => _rotation[_objectiveT2Index];
        public override string DisplayName => "Advance and Secure";
        public override bool EnableAMC => true;
        public override bool ShowOFPUI => true;
        public override bool ShowXPUI => true;
        public override bool TransmitMicWhileNotActive => true;
        public override bool UseJoinUI => true;
        public override bool UseWhitelist => true;
        public override bool AllowCosmetics => UCWarfare.Config.AllowCosmetics;
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
        // leaderboard
        private EndScreenLeaderboard _endScreen;
        EndScreenLeaderboard IWarstatsGamemode.Leaderboard => _endScreen;
        ILeaderboard IImplementsLeaderboard.Leaderboard => _endScreen;
        protected Transform _blockerBarricadeT1 = null;
        protected Transform _blockerBarricadeT2 = null;
        private bool _isScreenUp = false;
        public bool isScreenUp => _isScreenUp;
        private WarStatsTracker _gameStats;
        public WarStatsTracker GameStats => _gameStats;

        protected int _stagingSeconds { get; set; }
        public int StagingSeconds { get => _stagingSeconds; }


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
        public override void Init()
        {
            base.Init();
            _FOBManager = new FOBManager();
            _squadManager = new SquadManager();
            _kitManager = new KitManager();
            _vehicleBay = new VehicleBay();
            _reviveManager = new ReviveManager();
            _gameStats = UCWarfare.I.gameObject.AddComponent<WarStatsTracker>();
        }
        public override void OnLevelLoaded()
        {
            _structureSaver = new StructureSaver();
            _vehicleSpawner = new VehicleSpawner();
            _vehicleSigns = new VehicleSigns();
            _requestSigns = new RequestSigns();
            FOBManager.LoadFobsFromMap();
            RepairManager.LoadRepairStations();
            VehicleSpawner.OnLevelLoaded();
            RallyManager.WipeAllRallies();
            VehicleSigns.InitAllSigns();
            base.OnLevelLoaded();
        }
        public override void OnEvaluate()
        {
            CheckPlayersAMC();
        }
        public override void OnPlayerDeath(UCWarfare.DeathEventArgs args)
        {
            InAMC.Remove(args.dead.channel.owner.playerID.steamID.m_SteamID);
            EventFunctions.RemoveDamageMessageTicks(args.dead.channel.owner.playerID.steamID.m_SteamID);
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
        protected override void EvaluateTickets()
        {
            if (_state == EState.ACTIVE)
            {
                TicketManager.GetTeamBleed(1, out int Team1Bleed, out _);
                TicketManager.GetTeamBleed(2, out int Team2Bleed, out _);

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
            }
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
            this._state = EState.FINISHED;
            TicketManager.OnRoundWin(winner);
            StartCoroutine(EndGameCoroutine(winner));
        }
        private IEnumerator<WaitForSeconds> EndGameCoroutine(ulong winner)
        {
            yield return new WaitForSeconds(Config.end_delay);

            ReplaceBarricadesAndStructures();
            Commands.ClearCommand.WipeVehiclesAndRespawn();
            Commands.ClearCommand.ClearItems();

            InvokeOnTeamWin(winner);
            if (Config.ShowLeaderboard)
            {
                _endScreen = UCWarfare.I.gameObject.AddComponent<EndScreenLeaderboard>();
                _endScreen.winner = winner;
                _endScreen.warstats = GameStats;
                _endScreen.OnLeaderboardExpired += OnShouldStartNewGame;
                _endScreen.ShuttingDown = shutdownAfterGame;
                _endScreen.ShuttingDownMessage = shutdownMessage;
                _endScreen.ShuttingDownPlayer = shutdownPlayer;
                _isScreenUp = true;
                _endScreen.EndGame(Config.ProgressChars);
            }
            else OnShouldStartNewGame();
        }
        private void OnShouldStartNewGame()
        {
            if (_endScreen != default)
                _endScreen.OnLeaderboardExpired -= OnShouldStartNewGame;
            Destroy(_endScreen);
            _isScreenUp = false;
            StartNextGame();
        }
        public void ReloadConfig()
        {
            _config.Reload();
        }
        public override void StartNextGame(bool onLoad = false)
        {
            base.StartNextGame(onLoad); // set game id
            if (_state == EState.DISCARD) return;

            _joinManager.OnNewGameStarting();

            LoadRotation();
            PlaceBlockerBarricades();
            EffectManager.ClearEffectByID_AllPlayers(Config.CaptureUI);
            GameStats.Reset();
            InvokeOnNewGameStarting(onLoad);
            StartStagingPhase(Config.StagingPhaseSeconds);
        }
        public override void LoadRotation()
        {
            if (_allFlags == null) return;
            ResetFlags();
            _onFlag.Clear();
            if (Config.PathingMode == ObjectivePathing.EPathingMode.AUTODISTANCE)
            {
                Config.PathingData.Set();
                _rotation = ObjectivePathing.CreateAutoPath(_allFlags);
            }
            else if (Config.PathingMode == ObjectivePathing.EPathingMode.LEVELS)
            {
                _rotation = ObjectivePathing.CreatePathUsingLevels(_allFlags, Config.MaxFlagsPerLevel);
            }
            else if (Config.PathingMode == ObjectivePathing.EPathingMode.ADJACENCIES)
            {
                _rotation = ObjectivePathing.PathWithAdjacents(_allFlags, Config.team1adjacencies, Config.team2adjacencies);
            }
            else
            {
                F.LogWarning("Invalid pathing value, no flags will be loaded. Expect errors.");
            }
            _objectiveT1Index = 0;
            _objectiveT2Index = _rotation.Count - 1;
            if (Config.DiscoveryForesight < 1)
            {
                F.LogWarning("Discovery Foresight is set to 0 in Flag Settings. The players can not see their next flags.");
            }
            else
            {
                for (int i = 0; i < Config.DiscoveryForesight; i++)
                {
                    if (i >= _rotation.Count || i < 0) break;
                    _rotation[i].Discover(1);
                }
                for (int i = _rotation.Count - 1; i > _rotation.Count - 1 - Config.DiscoveryForesight; i--)
                {
                    if (i >= _rotation.Count || i < 0) break;
                    _rotation[i].Discover(2);
                }
            }
            foreach (Flag flag in _rotation)
            {
                InitFlag(flag); //subscribe to abstract events.
            }
            foreach (SteamPlayer client in Provider.clients)
            {
                CTFUI.ClearListUI(client.transportConnection, Config.FlagUICount);
                CTFUI.SendFlagListUI(client.transportConnection, client.playerID.steamID.m_SteamID, client.GetTeam(), _rotation, Config.FlagUICount, Config.AttackIcon, Config.DefendIcon);
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
                            if (i >= _rotation.Count || i < 0) break;
                            _rotation[i].Discover(1);
                        }
                    }
                    else if (Team == 2)
                    {
                        for (int i = NewFlagObj.index; i > NewFlagObj.index - UCWarfare.Config.FlagSettings.DiscoveryForesight; i--)
                        {
                            if (i >= _rotation.Count || i < 0) break;
                            _rotation[i].Discover(2);
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
            if (UCWarfare.Config.Debug)
                F.Log("Player " + player.channel.owner.playerID.playerName + " entered flag " + flag.Name, ConsoleColor.White);
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
                if (_objectiveT1Index >= _rotation.Count - 1) // if t1 just capped the last flag
                {
                    DeclareWin(NewOwner);
                    _objectiveT1Index = _rotation.Count - 1;
                    return;
                }
                else
                {
                    _objectiveT1Index = flag.index + 1;
                    InvokeOnObjectiveChanged(flag, _rotation[_objectiveT1Index], NewOwner, flag.index, _objectiveT1Index);
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
                if (_objectiveT2Index < 1) // if t2 just capped the last flag
                {
                    DeclareWin(NewOwner);
                    _objectiveT2Index = 0;
                    return;
                }
                else
                {

                    _objectiveT2Index = flag.index - 1;
                    InvokeOnObjectiveChanged(flag, _rotation[_objectiveT2Index], NewOwner, flag.index, _objectiveT2Index);
                    InvokeOnFlagCaptured(flag, NewOwner, OldOwner);
                    for (int i = 0; i < flag.PlayersOnFlagTeam2.Count; i++)
                    {
                        if (F.TryGetPlaytimeComponent(flag.PlayersOnFlagTeam2[i], out Components.PlaytimeComponent c) && c.stats is IFlagStats fg)
                            fg.AddCapture();
                    }
                }
            }
            if (OldOwner == 1)
            {
                int oldindex = _objectiveT1Index;
                _objectiveT1Index = flag.index;
                if (oldindex != flag.index)
                {
                    InvokeOnObjectiveChanged(_rotation[oldindex], flag, 0, oldindex, flag.index);
                    InvokeOnFlagNeutralized(flag, 2, 1);
                }
            }
            else if (OldOwner == 2)
            {
                int oldindex = _objectiveT2Index;
                _objectiveT2Index = flag.index;
                if (oldindex != flag.index)
                {
                    InvokeOnObjectiveChanged(_rotation[oldindex], flag, 0, oldindex, flag.index);
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
                    CTFUI.SendFlagListUI(client.transportConnection, client.playerID.steamID.m_SteamID, team, _rotation, Config.FlagUICount, Config.AttackIcon, Config.DefendIcon);
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
                    CTFUI.SendFlagListUI(client.transportConnection, client.playerID.steamID.m_SteamID, team, _rotation, Config.FlagUICount, Config.AttackIcon, Config.DefendIcon);
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
            if (_onFlag.ContainsKey(player.playerID.steamID.m_SteamID))
                CTFUI.RefreshStaticUI(newteam, _rotation.FirstOrDefault(x => x.ID == _onFlag[player.playerID.steamID.m_SteamID])
                    ?? _rotation[0], player.player.movement.getVehicle() != null).SendToPlayer(Config.PlayerIcon, Config.UseUI, Config.CaptureUI, Config.ShowPointsOnUI, Config.ProgressChars, player, player.transportConnection);
            CTFUI.SendFlagListUI(player.transportConnection, player.playerID.steamID.m_SteamID, newGroup, _rotation, Config.FlagUICount, Config.AttackIcon, Config.DefendIcon);
            base.OnGroupChanged(player, oldGroup, newGroup, oldteam, newteam);
        }
        public override void OnPlayerJoined(UCPlayer player, bool wasAlreadyOnline = false)
        {
            if (KitManager.KitExists(player.KitName, out Kit kit))
            {
                if (kit.IsLimited(out int currentPlayers, out int allowedPlayers, player.GetTeam()) || (kit.IsLoadout && kit.IsClassLimited(out currentPlayers, out allowedPlayers, player.GetTeam())))
                {
                    if (!KitManager.TryGiveRiflemanKit(player))
                        KitManager.TryGiveUnarmedKit(player);
                }
            }
            _reviveManager.DownedPlayers.Remove(player.CSteamID.m_SteamID);
            ulong team = player.GetTeam();
            FPlayerName names = F.GetPlayerOriginalNames(player);
            if ((player.KitName == null || player.KitName == string.Empty) && team > 0 && team < 3)
            {
                if (KitManager.KitExists(team == 1 ? TeamManager.Team1UnarmedKit : TeamManager.Team2UnarmedKit, out Kit unarmed))
                    KitManager.GiveKit(player, unarmed);
                else if (KitManager.KitExists(TeamManager.DefaultKit, out unarmed)) KitManager.GiveKit(player, unarmed);
                else F.LogWarning("Unable to give " + names.PlayerName + " a kit.");
            }
            _reviveManager.OnPlayerConnected(player);
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
            if (isScreenUp && _endScreen != null && Config.ShowLeaderboard)
            {
                _endScreen.SendScreenToPlayer(player.Player.channel.owner, Config.ProgressChars);
            }
            else
            {
                CTFUI.SendFlagListUI(player.Player.channel.owner.transportConnection, player.Player.channel.owner.playerID.steamID.m_SteamID, 
                    player.GetTeam(), _rotation, Config.FlagUICount, Config.AttackIcon, Config.DefendIcon);
                if (State == EState.STAGING)
                    this.ShowStagingUI(player);
            }
            StatsManager.RegisterPlayer(player.CSteamID.m_SteamID);
            StatsManager.ModifyStats(player.CSteamID.m_SteamID, s => s.LastOnline = DateTime.Now.Ticks);
            base.OnPlayerJoined(player, wasAlreadyOnline);
        }
        public override void OnPlayerLeft(UCPlayer player)
        {
            foreach (Flag flag in _rotation)
                flag.RecalcCappers(true);
            StatsCoroutine.previousPositions.Remove(player.Player.channel.owner.playerID.steamID.m_SteamID);
            _reviveManager.OnPlayerDisconnected(player.Player.channel.owner);
            StatsManager.DeregisterPlayer(player.CSteamID.m_SteamID);
            base.OnPlayerLeft(player);
        }
        public override void Dispose()
        {
            DestroyBlockerBarricades();
            foreach (SteamPlayer player in Provider.clients)
            {
                CTFUI.ClearListUI(player.transportConnection, Config.FlagUICount);
                SendUIParameters.Nil.SendToPlayer(Config.PlayerIcon, Config.UseUI, Config.CaptureUI, Config.ShowPointsOnUI, Config.ProgressChars, player, player.transportConnection); // clear all capturing uis
                if (F.TryGetPlaytimeComponent(player.player, out Components.PlaytimeComponent c))
                    c.stats = null;
            }
            _squadManager?.Dispose();
            _vehicleSpawner?.Dispose();
            _reviveManager?.Dispose();
            _kitManager?.Dispose();
            EndStagingPhase();
            FOBManager.Reset();
            Destroy(_gameStats);
            base.Dispose();
        }
        protected override void EventLoopAction()
        {
            base.EventLoopAction();
            FOBManager.OnGameTick(TicketCounter);
        }

        private void DestroyBlockerBarricades()
        {
            bool backup = false;
            if (_blockerBarricadeT1 != null && Regions.tryGetCoordinate(_blockerBarricadeT1.position, out byte x, out byte y))
            {
                BarricadeDrop drop = BarricadeManager.regions[x, y].FindBarricadeByRootTransform(_blockerBarricadeT1);
                if (drop != null)
                {
                    BarricadeManager.destroyBarricade(drop, x, y, ushort.MaxValue);
                }
                else
                {
                    backup = true;
                }
                _blockerBarricadeT1 = null;
            }
            else backup = true;
            if (_blockerBarricadeT2 != null && Regions.tryGetCoordinate(_blockerBarricadeT2.position, out x, out y))
            {
                BarricadeDrop drop = BarricadeManager.regions[x, y].FindBarricadeByRootTransform(_blockerBarricadeT2);
                if (drop != null)
                {
                    BarricadeManager.destroyBarricade(drop, x, y, ushort.MaxValue);
                }
                else
                {
                    backup = true;
                }
                _blockerBarricadeT2 = null;
            }
            else backup = true;
            if (backup)
            {
                for (x = 0; x < Regions.WORLD_SIZE; x++)
                {
                    for (y = 0; y < Regions.WORLD_SIZE; y++)
                    {
                        for (int i = 0; i < BarricadeManager.regions[x, y].drops.Count; i++)
                        {
                            BarricadeDrop d = BarricadeManager.regions[x, y].drops[i];
                            if (d.asset.id == Config.T1BlockerID || d.asset.id == Config.T2BlockerID)
                            {
                                BarricadeManager.destroyBarricade(d, x, y, ushort.MaxValue);
                            }
                        }
                    }
                }
            }
        }
        readonly Vector3 SpawnRotation = new Vector3(270f, 0f, 180f);
        private void PlaceBlockerBarricades()
        {
            DestroyBlockerBarricades();
            _blockerBarricadeT1 = BarricadeManager.dropNonPlantedBarricade(new Barricade(Config.T1BlockerID),
                TeamManager.Team1Main.Center3DAbove, Quaternion.Euler(SpawnRotation), 0, 0);
            _blockerBarricadeT2 = BarricadeManager.dropNonPlantedBarricade(new Barricade(Config.T2BlockerID),
                TeamManager.Team2Main.Center3DAbove, Quaternion.Euler(SpawnRotation), 0, 0);
        }
        public void StartStagingPhase(int seconds)
        {
            _stagingSeconds = seconds;
            _state = EState.STAGING;

            StartCoroutine(StagingPhaseLoop());
        }
        public void SkipStagingPhase()
        {
            _stagingSeconds = 0;
        }
        public IEnumerator<WaitForSeconds> StagingPhaseLoop()
        {
            ShowStagingUIForAll();

            while (StagingSeconds > 0)
            {
                if (State != EState.STAGING)
                {
                    EndStagingPhase();
                    yield break;
                }

                UpdateStagingUIForAll();

                yield return new WaitForSeconds(1);
                _stagingSeconds -= 1;
            }
            EndStagingPhase();
        }
        public void ShowStagingUI(UCPlayer player)
        {
            EffectManager.sendUIEffect(Config.HeaderID, 29001, player.connection, true);
            EffectManager.sendUIEffectText(29001, player.connection, true, "Top", "BRIEFING PHASE");
        }
        public void ShowStagingUIForAll()
        {
            foreach (UCPlayer player in PlayerManager.OnlinePlayers)
                ShowStagingUI(player);
        }
        public void UpdateStagingUI(UCPlayer player, TimeSpan timeleft)
        {
            EffectManager.sendUIEffectText(29001, player.connection, true, "Bottom", $"{timeleft.Minutes}:{timeleft.Seconds:D2}");
        }
        public void UpdateStagingUIForAll()
        {
            TimeSpan timeLeft = TimeSpan.FromSeconds(StagingSeconds);
            foreach (UCPlayer player in PlayerManager.OnlinePlayers)
                UpdateStagingUI(player, timeLeft);
        }
        private void EndStagingPhase()
        {
            TicketManager.OnStagingPhaseEnded();
            DestroyBlockerBarricades();
            foreach (UCPlayer player in PlayerManager.OnlinePlayers)
                EffectManager.askEffectClearByID(Config.HeaderID, player.connection);

            _state = EState.ACTIVE;
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
        public int StagingPhaseSeconds;
        public ushort T1BlockerID;
        public ushort T2BlockerID;
        public ushort HeaderID;
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
            this.team1adjacencies = new Dictionary<int, float>();
            this.team2adjacencies = new Dictionary<int, float>();
            this.xpSecondInterval = 10;
            this.StagingPhaseSeconds = 90;
            this.T1BlockerID = 36058;
            this.T2BlockerID = 36059;
            this.HeaderID = 36066;
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

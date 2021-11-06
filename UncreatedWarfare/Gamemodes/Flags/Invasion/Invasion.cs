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
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Structures;

namespace Uncreated.Warfare.Gamemodes.Flags.Invasion
{
    public class Invasion : TicketGamemode, IFlagTeamObjectiveGamemode, IWarstatsGamemode, IVehicles, IFOBs, IKitRequests, IRevives, ISquads, IAttackDefence, IImplementsLeaderboard, IStructureSaving, IStagingPhase
    {
        const float MATCH_PRESENT_THRESHOLD = 0.65f;

        public override string DisplayName => "Invasion";
        private readonly Config<InvasionData> _config;
        public InvasionData Config { get => _config.Data; }

        protected WarStatsTracker _gameStats;
        public WarStatsTracker GameStats { get => _gameStats; }
        protected EndScreenLeaderboard _endScreen;
        EndScreenLeaderboard IWarstatsGamemode.Leaderboard { get => _endScreen; }
        ILeaderboard IImplementsLeaderboard.Leaderboard { get => _endScreen; }
        protected bool _isScreenUp = false;
        public bool isScreenUp { get => _isScreenUp; }
        protected ulong _attackTeam;
        public ulong AttackingTeam { get => _attackTeam; }
        protected ulong _defendTeam;
        public ulong DefendingTeam { get => _defendTeam; }
        protected Transform _blockerBarricade = null;
        protected int _objectiveT1Index;
        protected int _objectiveT2Index;
        public int ObjectiveT1Index { get => _objectiveT1Index; }
        public int ObjectiveT2Index { get => _objectiveT2Index; }
        public Flag ObjectiveTeam1 { get => _objectiveT1Index < 0 || _objectiveT1Index >= _rotation.Count ? null : _rotation[_objectiveT1Index]; }
        public Flag ObjectiveTeam2 { get => _objectiveT2Index < 0 || _objectiveT2Index >= _rotation.Count ? null : _rotation[_objectiveT2Index]; }

        public override bool EnableAMC => true;
        public override bool ShowOFPUI => true;
        public override bool ShowXPUI => true;
        public override bool TransmitMicWhileNotActive => true;
        public override bool UseJoinUI => true;
        public override bool UseWhitelist => true;
        public override bool AllowCosmetics => UCWarfare.Config.AllowCosmetics;

        protected VehicleSpawner _vehicleSpawner;
        public VehicleSpawner VehicleSpawner { get => _vehicleSpawner; }
        protected VehicleBay _vehicleBay;
        public VehicleBay VehicleBay { get => _vehicleBay; }
        protected VehicleSigns _vehicleSigns;
        public VehicleSigns VehicleSigns { get => _vehicleSigns; }
        protected FOBManager _FOBManager;
        public FOBManager FOBManager { get => _FOBManager; }
        protected RequestSigns _requestSigns;
        public RequestSigns RequestSigns { get => _requestSigns; }
        protected KitManager _kitManager;
        public KitManager KitManager { get => _kitManager; }
        protected ReviveManager _reviveManager;
        public ReviveManager ReviveManager { get => _reviveManager; }
        protected SquadManager _squadManager;
        public SquadManager SquadManager { get => _squadManager; }
        protected StructureSaver _structureSaver;
        public StructureSaver StructureSaver { get => _structureSaver; }
        protected int _stagingSeconds { get; set; }
        public int StagingSeconds { get => _stagingSeconds; }

        public Invasion() : base(nameof(Invasion), 0.25f)
        {
            _config = new Config<InvasionData>(Data.FlagStorage, "invasion.json");
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
            VehicleSpawner.OnLevelLoaded();
            RepairManager.LoadRepairStations();
            RallyManager.WipeAllRallies();
            VehicleSigns.InitAllSigns();
            base.OnLevelLoaded();
        }
        public override void StartNextGame(bool onLoad = false)
        {
            base.StartNextGame(onLoad); // set game id
            if (_state == EState.DISCARD) return;

            _joinManager.OnNewGameStarting();

            _attackTeam = (ulong)UnityEngine.Random.Range(1, 3);
            if (_attackTeam == 1)
                _defendTeam = 2;
            else if (_attackTeam == 2)
                _defendTeam = 1;
            PlaceBlockerOverAttackerMain();

            LoadRotation();

            EffectManager.ClearEffectByID_AllPlayers(Config.CaptureUI);
            GameStats.Reset();

            StartStagingPhase(Config.StagingPhaseSeconds);

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
            TicketManager.OnFlagCaptured(flag, capturedTeam, lostTeam);
            StatsManager.ModifyTeam(capturedTeam, t => t.FlagsCaptured++, false);
            StatsManager.ModifyTeam(lostTeam, t => t.FlagsLost++, false);
            List<string> kits = new List<string>();
            if (capturedTeam == 1)
            {
                for (int p = 0; p < flag.PlayersOnFlagTeam1.Count; p++)
                {
                    StatsManager.ModifyStats(flag.PlayersOnFlagTeam1[p].channel.owner.playerID.steamID.m_SteamID, s => s.FlagsCaptured++, false);
                    if (KitManager.HasKit(flag.PlayersOnFlagTeam1[p], out Kit kit) && !kits.Contains(kit.Name))
                    {
                        StatsManager.ModifyKit(kit.Name, k => k.FlagsCaptured++, true);
                        kits.Add(kit.Name);
                    }
                }
                if (flag.IsObj(2))
                    for (int p = 0; p < flag.PlayersOnFlagTeam2.Count; p++)
                        StatsManager.ModifyStats(flag.PlayersOnFlagTeam2[p].channel.owner.playerID.steamID.m_SteamID, s => s.FlagsLost++, false);
            }
            else if (capturedTeam == 2)
            {
                if (flag.IsObj(1))
                    for (int p = 0; p < flag.PlayersOnFlagTeam1.Count; p++)
                        StatsManager.ModifyStats(flag.PlayersOnFlagTeam1[p].channel.owner.playerID.steamID.m_SteamID, s => s.FlagsLost++, false);
                for (int p = 0; p < flag.PlayersOnFlagTeam2.Count; p++)
                {
                    StatsManager.ModifyStats(flag.PlayersOnFlagTeam2[p].channel.owner.playerID.steamID.m_SteamID, s => s.FlagsCaptured++, false);
                    if (KitManager.HasKit(flag.PlayersOnFlagTeam2[p], out Kit kit) && !kits.Contains(kit.Name))
                    {
                        StatsManager.ModifyKit(kit.Name, k => k.FlagsCaptured++, true);
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
            this._state = EState.FINISHED;
            
            TicketManager.OnRoundWin(winner);
            StartCoroutine(EndGameCoroutine(winner));
        }
        private IEnumerator<WaitForSeconds> EndGameCoroutine(ulong winner)
        {
            yield return new WaitForSeconds(Config.end_delay);

            InvokeOnTeamWin(winner);
            ReplaceBarricadesAndStructures();
            Commands.ClearCommand.WipeVehiclesAndRespawn();
            Commands.ClearCommand.ClearItems();

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
            if (_attackTeam == 1)
            {
                _objectiveT1Index = 0;
                _objectiveT2Index = -1;
            }
            else if (_attackTeam == 2)
            {
                _objectiveT2Index = _rotation.Count - 1;
                _objectiveT1Index = -1;
            }
            if (Config.DiscoveryForesight < 1)
            {
                F.LogWarning("Discovery Foresight is set to 0 in Flag Settings. The players can not see their next flags.");
            }
            else
            {
                for (int i = 0; i < _rotation.Count; i++)
                {
                    _rotation[i].Discover(_defendTeam);
                }
                if (_attackTeam == 1)
                {
                    for (int i = 0; i < Config.DiscoveryForesight; i++)
                    {
                        if (i >= _rotation.Count || i < 0) break;
                        _rotation[i].Discover(1);
                    }
                }
                else if (_attackTeam == 2)
                {
                    for (int i = _rotation.Count - 1; i > _rotation.Count - 1 - Config.DiscoveryForesight; i--)
                    {
                        if (i >= _rotation.Count || i < 0) break;
                        _rotation[i].Discover(2);
                    }
                }
            }
            foreach (Flag flag in _rotation)
            {
                InitFlag(flag); //subscribe to abstract events.
            }
            foreach (SteamPlayer client in Provider.clients)
            {
                InvasionUI.ClearListUI(client.transportConnection, Config.FlagUICount);
                InvasionUI.SendFlagListUI(client.transportConnection, client.playerID.steamID.m_SteamID, client.GetTeam(), _rotation, Config.FlagUICount, Config.AttackIcon, Config.DefendIcon, AttackingTeam, Config.LockedIcon);
            }
            F.Log("Reached end of load rotation :)");
            PrintFlagRotation();
            EvaluatePoints();
        }
        public override void InitFlag(Flag flag)
        {
            base.InitFlag(flag);
            flag.EvaluatePointsOverride = FlagCheck;
            flag.IsContestedOverride = ContestedCheck;
            flag.SetOwnerNoEventInvocation(_defendTeam);
            flag.SetPoints(_attackTeam == 2 ? Flag.MAX_POINTS : -Flag.MAX_POINTS, true);
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
                if (OldOwner == _defendTeam)
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
                    InvasionUI.SendFlagListUI(client.transportConnection, client.playerID.steamID.m_SteamID, team, _rotation, 
                        Config.FlagUICount, Config.AttackIcon, Config.DefendIcon, AttackingTeam, Config.LockedIcon);
                }
            }
            else
            {
                foreach (SteamPlayer client in Provider.clients)
                {
                    ulong team = client.GetTeam();
                    client.SendChat("team_capture", UCWarfare.GetColor("team_capture"), Teams.TeamManager.TranslateName(NewOwner, client.playerID.steamID.m_SteamID),
                        TeamManager.GetTeamHexColor(NewOwner), flag.Discovered(team) ? flag.Name : F.Translate("undiscovered_flag", client.playerID.steamID.m_SteamID),
                        flag.TeamSpecificHexColor);
                    InvasionUI.SendFlagListUI(client.transportConnection, client.playerID.steamID.m_SteamID, team, _rotation, Config.FlagUICount, 
                        Config.AttackIcon, Config.DefendIcon, AttackingTeam, Config.LockedIcon);
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
                    (player.movement.getVehicle() == null ? t1 : t1v).SendToPlayer(Config.PlayerIcon, Config.UseUI, Config.CaptureUI, Config.ShowPointsOnUI, Config.ProgressChars, player.channel.owner, player.channel.owner.transportConnection);
                else if (team == 2)
                    (player.movement.getVehicle() == null ? t2 : t2v).SendToPlayer(Config.PlayerIcon, Config.UseUI, Config.CaptureUI, Config.ShowPointsOnUI, Config.ProgressChars, player.channel.owner, player.channel.owner.transportConnection);
            }
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
        private void DestroyBlockerBarricade()
        {
            bool backup = false;
            if (_blockerBarricade != null && Regions.tryGetCoordinate(_blockerBarricade.position, out byte x, out byte y))
            {
                BarricadeDrop drop = BarricadeManager.regions[x, y].FindBarricadeByRootTransform(_blockerBarricade);
                if (drop != null)
                {
                    BarricadeManager.destroyBarricade(drop, x, y, ushort.MaxValue);
                }
                else backup = true;
                _blockerBarricade = null;
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
        private void PlaceBlockerOverAttackerMain()
        {
            DestroyBlockerBarricade();
            if (_attackTeam == 1)
            {
                _blockerBarricade = BarricadeManager.dropNonPlantedBarricade(new Barricade(Config.T1BlockerID), 
                    TeamManager.Team1Main.Center3DAbove, Quaternion.Euler(SpawnRotation), 0, 0);
            }
            else if (_attackTeam == 2)
            {
                _blockerBarricade = BarricadeManager.dropNonPlantedBarricade(new Barricade(Config.T2BlockerID), 
                    TeamManager.Team2Main.Center3DAbove, Quaternion.Euler(SpawnRotation), 0, 0);
            }
        }
        public override void OnGroupChanged(SteamPlayer player, ulong oldGroup, ulong newGroup, ulong oldteam, ulong newteam)
        {
            InvasionUI.ClearListUI(player.transportConnection, Config.FlagUICount);
            if (_onFlag.ContainsKey(player.playerID.steamID.m_SteamID))
                InvasionUI.RefreshStaticUI(newteam, _rotation.FirstOrDefault(x => x.ID == _onFlag[player.playerID.steamID.m_SteamID])
                    ?? _rotation[0], player.player.movement.getVehicle() != null, AttackingTeam)
                    .SendToPlayer(Config.PlayerIcon, Config.UseUI, Config.CaptureUI, Config.ShowPointsOnUI, 
                    Config.ProgressChars, player, player.transportConnection);
            InvasionUI.SendFlagListUI(player.transportConnection, player.playerID.steamID.m_SteamID, newGroup, _rotation, Config.FlagUICount, Config.AttackIcon, Config.DefendIcon, AttackingTeam, Config.LockedIcon);
            base.OnGroupChanged(player, oldGroup, newGroup, oldteam, newteam);
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

                if (Team1Bleed < 0)
                    TicketManager.UpdateUITeam1();
                if (Team2Bleed < 0)
                    TicketManager.UpdateUITeam2();
            }
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
                if (State == EState.STAGING)
                    this.ShowStagingUI(player);
                InvasionUI.SendFlagListUI(player.Player.channel.owner.transportConnection, player.Player.channel.owner.playerID.steamID.m_SteamID, 
                    player.GetTeam(), _rotation, Config.FlagUICount, Config.AttackIcon, Config.DefendIcon, AttackingTeam, Config.LockedIcon);
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
            DestroyBlockerBarricade();
            foreach (SteamPlayer player in Provider.clients)
            {
                InvasionUI.ClearListUI(player.transportConnection, Config.FlagUICount);
                SendUIParameters.Nil.SendToPlayer(Config.PlayerIcon, Config.UseUI, Config.CaptureUI, Config.ShowPointsOnUI, Config.ProgressChars, player, player.transportConnection); // clear all capturing uis
                if (F.TryGetPlaytimeComponent(player.player, out Components.PlaytimeComponent c))
                    c.stats = null;
                EffectManager.askEffectClearByID(Config.HeaderID, player.transportConnection);
            }
            if (UpdateCoroutine != null)
                StopCoroutine(UpdateCoroutine);
            _squadManager?.Dispose();
            _vehicleSpawner?.Dispose();
            _reviveManager?.Dispose();
            _kitManager?.Dispose();
            EndStagingPhase();
            FOBManager.Reset();
            Destroy(_gameStats);
            base.Dispose();
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
                    if (flag.Team1TotalCappers - UCWarfare.Config.FlagSettings.RequiredPlayerDifferenceToCapture >= flag.Team2TotalCappers)
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
                    if (flag.Team2TotalCappers - UCWarfare.Config.FlagSettings.RequiredPlayerDifferenceToCapture >= flag.Team1TotalCappers)
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
        protected override void EventLoopAction()
        {
            base.EventLoopAction();
            FOBManager.OnGameTick(TicketCounter);
        }
        public override void OnEvaluate()
        {
            CheckPlayersAMC();
        }
        public void StartStagingPhase(int seconds)
        {
            _stagingSeconds = seconds;
            _state = EState.STAGING;

            Flag firstFlag = null;
            if (DefendingTeam == 1)
                firstFlag = Rotation.Last();
            else if (DefendingTeam == 2)
                firstFlag = Rotation.First();

            FOBManager.RegisterNewSpecialFOB("VCP", firstFlag.ZoneData.Center3DAbove, DefendingTeam, "#5482ff", true);

            UpdateCoroutine = StartCoroutine(StagingPhaseLoop());
        }
        protected Coroutine UpdateCoroutine = null;
        public IEnumerator<WaitForSeconds> StagingPhaseLoop()
        {
            //ShowStagingUIForAll();

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
            if (player.GetTeam() == AttackingTeam)
                EffectManager.sendUIEffectText(29001, player.connection, true, "Top", "BRIEFING PHASE");
            else if (player.GetTeam() == DefendingTeam)
                EffectManager.sendUIEffectText(29001, player.connection, true, "Top", "PREPARATION PHASE");
        }
        public void ShowStagingUIForAll()
        {
            foreach (UCPlayer player in PlayerManager.OnlinePlayers)
                ShowStagingUI(player);
        }
        public void SkipStagingPhase()
        {
            _stagingSeconds = 0;
        }
        public void UpdateStagingUI(UCPlayer player, TimeSpan timeleft)
        {
            EffectManager.sendUIEffectText(29001, player.connection, true, "Bottom", $"{timeleft.Minutes}:{timeleft.Seconds.ToString("D2")}");
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

            foreach (UCPlayer player in PlayerManager.OnlinePlayers)
                EffectManager.askEffectClearByID(Config.HeaderID, player.connection);

            _state = EState.ACTIVE;
            DestroyBlockerBarricade();
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
        public char LockedIcon;
        public bool ShowLeaderboard;
        public TeamCTFData.AutoObjectiveData PathingData;
        public int end_delay;
        public float NearOtherBaseKillTimer;
        public int xpSecondInterval;
        public ushort T1BlockerID;
        public ushort T2BlockerID;
        public ushort HeaderID;
        public int TicketsFlagCaptured;
        public int AttackStartingTickets;
        public int StagingPhaseSeconds;
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
            this.LockedIcon = 'Ø';
            this.ShowLeaderboard = true;
            this.PathingData = new TeamCTFData.AutoObjectiveData();
            this.end_delay = 15;
            this.NearOtherBaseKillTimer = 10f;
            this.TicketsFlagCaptured = 150;
            this.AttackStartingTickets = 250;
            this.StagingPhaseSeconds = 150;
            this.T1BlockerID = 36058;
            this.T2BlockerID = 36059;
            this.HeaderID = 36066;
            this.team1adjacencies = new Dictionary<int, float>();
            this.team2adjacencies = new Dictionary<int, float>();
            this.xpSecondInterval = 10;
        }
    }
}

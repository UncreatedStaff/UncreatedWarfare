﻿using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using Uncreated.Players;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Revives;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Tickets;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Flags
{
    public delegate void ObjectiveChangedDelegate(Flag OldFlagObj, Flag NewFlagObj, ulong Team, int OldObj, int NewObj);
    public delegate void FlagCapturedHandler(Flag flag, ulong capturedTeam, ulong lostTeam);
    public delegate void FlagNeutralizedHandler(Flag flag, ulong capturedTeam, ulong lostTeam);
    public abstract class CTFBaseMode<Leaderboard, Stats, StatTracker> : TicketGamemode, IFlagTeamObjectiveGamemode, IVehicles, IFOBs, IKitRequests, IRevives, ISquads, IImplementsLeaderboard<Stats, StatTracker>, IStructureSaving, IStagingPhase, IGameStats
        where Leaderboard : BaseCTFLeaderboard<Stats, StatTracker>
        where Stats : BaseCTFStats
        where StatTracker : BaseCTFTracker<Stats>
    {
        // vars
        protected int _objectiveT1Index;
        protected int _objectiveT2Index;
        public int ObjectiveT1Index => _objectiveT1Index;
        public int ObjectiveT2Index => _objectiveT2Index;
        public Flag? ObjectiveTeam1 => _objectiveT1Index >= 0 && _objectiveT1Index < _rotation.Count ? _rotation[_objectiveT1Index] : null;
        public Flag? ObjectiveTeam2 => _objectiveT2Index >= 0 && _objectiveT2Index < _rotation.Count ? _rotation[_objectiveT2Index] : null;
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
        protected Leaderboard _endScreen;
        Leaderboard<Stats, StatTracker> IImplementsLeaderboard<Stats, StatTracker>.Leaderboard => _endScreen;
        protected Transform? _blockerBarricadeT1 = null;
        protected Transform? _blockerBarricadeT2 = null;
        private bool _isScreenUp = false;
        public bool isScreenUp => _isScreenUp;
        private StatTracker _gameStats;
        public StatTracker GameStats => _gameStats;

        object IGameStats.GameStats => _gameStats;

        public CTFBaseMode(string name, float timing) : base(name, timing)
        {

        }
        public override void Init()
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            base.Init();
            _FOBManager = new FOBManager();
            _squadManager = Data.Singletons.LoadSingleton<SquadManager>();
            _kitManager = Data.Singletons.LoadSingleton<KitManager>();
            Commands.ReloadCommand.ReloadKits();
            _vehicleBay = new VehicleBay();
            _reviveManager = new ReviveManager();
            _gameStats = UCWarfare.I.gameObject.AddComponent<StatTracker>();
        }
        public override void OnLevelLoaded()
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            _structureSaver = new StructureSaver();
            _vehicleSpawner = new VehicleSpawner();
            _vehicleSigns = new VehicleSigns();
            _requestSigns = new RequestSigns();
            RepairManager.LoadRepairStations();
            VehicleSpawner.OnLevelLoaded();
            RallyManager.WipeAllRallies();
            VehicleSigns.InitAllSigns();
            base.OnLevelLoaded();
        }
        protected override bool TimeToCheck()
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (_counter > Config.TeamCTF.FlagTickInterval)
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
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (_counter2 > 1 / Config.TeamCTF.EvaluateTime)
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
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (_state == EState.ACTIVE)
            {
                if (EveryMinute)
                {
                    int Team1Bleed = TicketManager.GetTeamBleed(1);
                    int Team2Bleed = TicketManager.GetTeamBleed(2);

                    if (Team1Bleed < 0)
                    {
                        TicketManager.Team1Tickets += Team1Bleed;
                        TicketManager.UpdateUITeam1(Team1Bleed);
                    }
                    if (Team2Bleed < 0)
                    {
                        TicketManager.Team2Tickets += Team2Bleed;
                        TicketManager.UpdateUITeam2(Team2Bleed);
                    }
                }
            }
            if (EveryXSeconds(5))
            {
                FOBManager.Tick();
            }
            base.EvaluateTickets();
        }
        public override void DeclareWin(ulong winner)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (this._state == EState.FINISHED) return;
            this._state = EState.FINISHED;
            L.Log(TeamManager.TranslateName(winner, 0) + " just won the game!", ConsoleColor.Cyan);

            string Team1Tickets = TicketManager.Team1Tickets.ToString() + " Tickets";
            if (TicketManager.Team1Tickets <= 0)
                Team1Tickets = Team1Tickets.Colorize("969696");

            string Team2Tickets = TicketManager.Team2Tickets.ToString() + " Tickets";
            if (TicketManager.Team2Tickets <= 0)
                Team2Tickets = Team2Tickets.Colorize("969696");

            ushort winToastUI = 0;
            if (Assets.find(Gamemode.Config.UI.WinToastGUID) is EffectAsset e)
            {
                winToastUI = e.id;
            }
            else
                L.LogWarning("WinToast UI not found. GUID: " + Gamemode.Config.UI.WinToastGUID);

            QuestManager.OnGameOver(winner);
            ActionLog.Add(EActionLogType.TEAM_WON, TeamManager.TranslateName(winner, 0));

            foreach (SteamPlayer client in Provider.clients)
            {
                client.SendChat("team_win", TeamManager.TranslateName(winner, client.playerID.steamID.m_SteamID), TeamManager.GetTeamHexColor(winner));
                client.player.movement.forceRemoveFromVehicle();
                EffectManager.askEffectClearByID(UCWarfare.Config.GiveUpUI, client.transportConnection);
                //ToastMessage.QueueMessage(client.player, new ToastMessage("", Translation.Translate("team_win", client, TeamManager.TranslateName(winner, client.playerID.steamID.m_SteamID), TeamManager.GetTeamHexColor(winner)), EToastMessageSeverity.BIG));

                EffectManager.sendUIEffect(winToastUI, 12345, client.transportConnection, true);
                EffectManager.sendUIEffectText(12345, client.transportConnection, true, "Header", Translation.Translate("team_win", client, TeamManager.TranslateName(winner, client.playerID.steamID.m_SteamID), "ffffff"));
                EffectManager.sendUIEffectText(12345, client.transportConnection, true, "Team1Tickets", Team1Tickets);
                EffectManager.sendUIEffectText(12345, client.transportConnection, true, "Team2Tickets", Team2Tickets);
            }
            StatsManager.ModifyTeam(winner, t => t.Wins++, false);
            StatsManager.ModifyTeam(TeamManager.Other(winner), t => t.Losses++, false);
            foreach (Stats played in GameStats.stats.Values)
            {
                // Any player who was online for 70% of the match will be awarded a win or punished with a loss
                if ((float)played.onlineCount1 / GameStats.coroutinect >= MATCH_PRESENT_THRESHOLD)
                {
                    if (winner == 1)
                        StatsManager.ModifyStats(played.Steam64, s => s.Wins++, false);
                    else
                        StatsManager.ModifyStats(played.Steam64, s => s.Losses++, false);
                }
                else if ((float)played.onlineCount2 / GameStats.coroutinect >= MATCH_PRESENT_THRESHOLD)
                {
                    if (winner == 2)
                        StatsManager.ModifyStats(played.Steam64, s => s.Wins++, false);
                    else
                        StatsManager.ModifyStats(played.Steam64, s => s.Losses++, false);
                }
            }
            TicketManager.OnRoundWin(winner);
            StartCoroutine(EndGameCoroutine(winner));
        }
        private IEnumerator<WaitForSeconds> EndGameCoroutine(ulong winner)
        {
            yield return new WaitForSeconds(Config.GeneralConfig.LeaderboardDelay);

#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            ReplaceBarricadesAndStructures();
            Commands.ClearCommand.WipeVehiclesAndRespawn();
            Commands.ClearCommand.ClearItems();

            InvokeOnTeamWin(winner);
            _endScreen = UCWarfare.I.gameObject.AddComponent<Leaderboard>();
            _endScreen.OnLeaderboardExpired = OnShouldStartNewGame;
            _endScreen.SetShutdownConfig(shutdownAfterGame, shutdownMessage);
            _isScreenUp = true;
            _endScreen.StartLeaderboard(winner, GameStats);
        }
        private void OnShouldStartNewGame()
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (_endScreen != null)
            {
                _endScreen.OnLeaderboardExpired = null;
                Destroy(_endScreen);
            }
            _isScreenUp = false;
            EndGame();
        }
        public override void StartNextGame(bool onLoad = false)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            LoadRotation();
            base.StartNextGame(onLoad);
            GameStats.Reset();
            CTFUI.ClearCaptureUI();
            InvokeOnNewGameStarting(onLoad);
        }
        protected void LoadFlagsIntoRotation()
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            ResetFlags();
            _onFlag.Clear();

            do
            {
                _rotation = ObjectivePathing.PathWithAdjacents(_allFlags, Config.MapConfig.Team1Adjacencies, Config.MapConfig.Team2Adjacencies);
            }
            while (_rotation.Count < 4 || _rotation.Count > Config.UI.FlagUICount);
        }
        public override void LoadRotation()
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (_allFlags == null || _allFlags.Count == 0) return;
            LoadFlagsIntoRotation();
            if (_rotation.Count < 1)
            {
                L.LogError("No flags were put into rotation!!");
            }
            _objectiveT1Index = 0;
            _objectiveT2Index = _rotation.Count - 1;
            if (Config.TeamCTF.DiscoveryForesight < 1)
            {
                L.LogWarning("Discovery Foresight is set to 0 in Flag Settings. The players can not see their next flags.");
            }
            else
            {
                for (int i = 0; i < Config.TeamCTF.DiscoveryForesight; i++)
                {
                    if (i >= _rotation.Count || i < 0) break;
                    _rotation[i].Discover(1);
                }
                for (int i = _rotation.Count - 1; i > _rotation.Count - 1 - Config.TeamCTF.DiscoveryForesight; i--)
                {
                    if (i >= _rotation.Count || i < 0) break;
                    _rotation[i].Discover(2);
                }
            }
            foreach (Flag flag in _rotation)
            {
                InitFlag(flag); //subscribe to abstract events.
            }
            foreach (UCPlayer player in PlayerManager.OnlinePlayers)
            {
                CTFUI.ClearFlagList(player);
                CTFUI.SendFlagList(player);
            }
            PrintFlagRotation();
            EvaluatePoints();
        }
        public override void PrintFlagRotation()
        {
            L.Log("Team 1 objective: " + (ObjectiveTeam1?.Name ?? "null") + ", Team 2 objective: " + (ObjectiveTeam2?.Name ?? "null"), ConsoleColor.Green);
            base.PrintFlagRotation();
        }
        protected virtual void InvokeOnObjectiveChanged(Flag OldFlagObj, Flag NewFlagObj, ulong Team, int OldObj, int NewObj)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (Team != 0)
            {
                if (GameStats != null)
                    GameStats.flagOwnerChanges++;
                L.Log("Team 1 objective: " + (ObjectiveTeam1?.Name ?? "null") + ", Team 2 objective: " + (ObjectiveTeam2?.Name ?? "null"), ConsoleColor.Green);
                if (Config.TeamCTF.DiscoveryForesight > 0)
                {
                    if (Team == 1)
                    {
                        for (int i = NewFlagObj.index; i < NewFlagObj.index + Config.TeamCTF.DiscoveryForesight; i++)
                        {
                            if (i >= _rotation.Count || i < 0) break;
                            _rotation[i].Discover(1);
                            if (this is Invasion.Invasion)
                                Invasion.InvasionUI.ReplicateFlagUpdate(_rotation[i]);
                            else
                                CTFUI.ReplicateFlagUpdate(_rotation[i]);
                        }
                    }
                    else if (Team == 2)
                    {
                        for (int i = NewFlagObj.index; i > NewFlagObj.index - Config.TeamCTF.DiscoveryForesight; i--)
                        {
                            if (i >= _rotation.Count || i < 0) break;
                            _rotation[i].Discover(2);
                            if (this is Invasion.Invasion)
                                Invasion.InvasionUI.ReplicateFlagUpdate(_rotation[i]);
                            else
                                CTFUI.ReplicateFlagUpdate(_rotation[i]);
                        }
                    }
                }
            }
        }
        protected virtual void InvokeOnFlagCaptured(Flag flag, ulong capturedTeam, ulong lostTeam)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            TicketManager.OnFlagCaptured(flag, capturedTeam, lostTeam);
            StatsManager.ModifyTeam(capturedTeam, t => t.FlagsCaptured++, false);
            StatsManager.ModifyTeam(lostTeam, t => t.FlagsLost++, false);
            VehicleSigns.OnFlagCaptured();
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
        protected virtual void InvokeOnFlagNeutralized(Flag flag, ulong capturedTeam, ulong lostTeam)
        {
            TicketManager.OnFlagNeutralized(flag, capturedTeam, lostTeam);
        }
        protected virtual void InvokeOnNewGameStarting(bool onLoad)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            TicketManager.OnNewGameStarting();
            if (!onLoad)
            {
                VehicleSpawner.RespawnAllVehicles();
                //FOBManager.WipeAllFOBRelatedBarricades(); (ran already kinda)
            }
            FOBManager.OnNewGameStarting();
            RallyManager.WipeAllRallies();
        }
        protected override void PlayerEnteredFlagRadius(Flag flag, Player player)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            ulong team = player.GetTeam();
            L.LogDebug("Player " + player.channel.owner.playerID.playerName + " entered flag " + flag.Name, ConsoleColor.White);
            player.SendChat("entered_cap_radius", UCWarfare.GetColor(team == 1 ? "entered_cap_radius_team_1" : (team == 2 ? "entered_cap_radius_team_2" : "default")), flag.Name, flag.ColorHex);
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
            for (int i = 0; i < flag.PlayersOnFlag.Count; i++)
            {
                Player capper = flag.PlayersOnFlag[i];
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
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            ITransportConnection channel = player.channel.owner.transportConnection;
            ulong team = player.GetTeam();
            L.LogDebug("Player " + player.channel.owner.playerID.playerName + " left flag " + flag.Name, ConsoleColor.White);
            player.SendChat("left_cap_radius", UCWarfare.GetColor(team == 1 ? "left_cap_radius_team_1" : (team == 2 ? "left_cap_radius_team_2" : "default")), flag.Name, flag.ColorHex);
            CTFUI.ClearCaptureUI(channel);
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
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (NewOwner == 1)
            {
                ActionLog.Add(EActionLogType.TEAM_CAPTURED_OBJECTIVE, TeamManager.TranslateName(1, 0));
                if (_objectiveT1Index >= _rotation.Count - 1) // if t1 just capped the last flag
                {
                    DeclareWin(1);
                    _objectiveT1Index = _rotation.Count - 1;
                    return;
                }
                else
                {
                    _objectiveT1Index = flag.index + 1;
                    InvokeOnObjectiveChanged(flag, _rotation[_objectiveT1Index], 1, flag.index, _objectiveT1Index);
                    InvokeOnFlagCaptured(flag, 1, OldOwner);
                    for (int i = 0; i < flag.PlayersOnFlagTeam1.Count; i++)
                    {
                        if (flag.PlayersOnFlagTeam1[i].TryGetPlaytimeComponent(out Components.PlaytimeComponent c) && c.stats is IFlagStats fg)
                            fg.AddCapture();
                    }
                }
            }
            else if (NewOwner == 2)
            {
                ActionLog.Add(EActionLogType.TEAM_CAPTURED_OBJECTIVE, TeamManager.TranslateName(2, 0));
                if (_objectiveT2Index < 1) // if t2 just capped the last flag
                {
                    DeclareWin(2);
                    _objectiveT2Index = 0;
                    return;
                }
                else
                {

                    _objectiveT2Index = flag.index - 1;
                    InvokeOnObjectiveChanged(flag, _rotation[_objectiveT2Index], 2, flag.index, _objectiveT2Index);
                    InvokeOnFlagCaptured(flag, 2, OldOwner);
                    for (int i = 0; i < flag.PlayersOnFlagTeam2.Count; i++)
                    {
                        if (flag.PlayersOnFlagTeam2[i].TryGetPlaytimeComponent(out Components.PlaytimeComponent c) && c.stats is IFlagStats fg)
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
                    (player.movement.getVehicle() == null ? t1 : t1v).SendToPlayer(player.channel.owner);
            if (flag.Team2TotalPlayers > 0)
                foreach (Player player in flag.PlayersOnFlagTeam2)
                    (player.movement.getVehicle() == null ? t2 : t2v).SendToPlayer(player.channel.owner);
            if (NewOwner == 0)
            {
                foreach (UCPlayer player in PlayerManager.OnlinePlayers)
                {
                    ulong team = player.GetTeam();
                    player.SendChat("flag_neutralized", UCWarfare.GetColor("flag_neutralized"), flag.Name, flag.TeamSpecificHexColor);
                }
            }
            else
            {
                foreach (UCPlayer player in PlayerManager.OnlinePlayers)
                {
                    ulong team = player.GetTeam();
                    player.SendChat("team_capture", UCWarfare.GetColor("team_capture"), TeamManager.TranslateName(NewOwner, player.Player),
                        TeamManager.GetTeamHexColor(NewOwner), flag.Discovered(team) ? flag.Name : Translation.Translate("undiscovered_flag", player),
                        flag.TeamSpecificHexColor);
                }
            }
        }
        protected override void FlagPointsChanged(float NewPoints, float OldPoints, Flag flag)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
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
                    (player.movement.getVehicle() == null ? t1 : t1v).SendToPlayer(player.channel.owner);
                else if (team == 2)
                    (player.movement.getVehicle() == null ? t2 : t2v).SendToPlayer(player.channel.owner);
            }
        }
        public override void OnGroupChanged(UCPlayer player, ulong oldGroup, ulong newGroup, ulong oldteam, ulong newteam)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (State == EState.STAGING)
            {
                if (newteam != 1 && newteam != 2)
                    ClearStagingUI(player);
                else
                    ShowStagingUI(player);
            }
            base.OnGroupChanged(player, oldGroup, newGroup, oldteam, newteam);
        }
        public override void OnPlayerJoined(UCPlayer player, bool wasAlreadyOnline, bool shouldRespawn)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
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
                else L.LogWarning("Unable to give " + names.PlayerName + " a kit.");
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
            GameStats.OnPlayerJoin(player.Player);
            if (isScreenUp && _endScreen != null)
            {
                _endScreen.SendLeaderboard(player, TeamManager.GetTeamHexColor(_endScreen.Winner));
            }
            else
            {
                CTFUI.SendFlagList(player);
                if (State == EState.STAGING)
                    this.ShowStagingUI(player);
            }
            StatsManager.RegisterPlayer(player.CSteamID.m_SteamID);
            StatsManager.ModifyStats(player.CSteamID.m_SteamID, s => s.LastOnline = DateTime.Now.Ticks);
            base.OnPlayerJoined(player, wasAlreadyOnline, shouldRespawn);
        }
        public override void OnPlayerLeft(UCPlayer player)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (_onFlag.TryGetValue(player.Steam64, out int id))
            {
                for (int i = 0; i < _rotation.Count; i++)
                {
                    if (_rotation[i].ID == id)
                    {
                        _rotation[i].RecalcCappers();
                        break;
                    }
                }
            }
            StatsCoroutine.previousPositions.Remove(player.Player.channel.owner.playerID.steamID.m_SteamID);
            _reviveManager.OnPlayerDisconnected(player.Player.channel.owner);
            StatsManager.DeregisterPlayer(player.CSteamID.m_SteamID);
            base.OnPlayerLeft(player);
        }
        protected override void EventLoopAction()
        {
            base.EventLoopAction();
        }
        public override void Dispose()
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            EffectManager.ClearEffectByID_AllPlayers(CTFUI.headerID);
            if (_stagingPhaseTimer != null)
                StopCoroutine(_stagingPhaseTimer);
            Data.Singletons.UnloadSingleton(ref _squadManager);
            _vehicleSpawner?.Dispose();
            _reviveManager?.Dispose();
            Data.Singletons.UnloadSingleton(ref _kitManager);
            _vehicleBay?.Dispose();
            FOBManager.Reset();
            Destroy(_gameStats);
            base.Dispose();
        }
    }
}

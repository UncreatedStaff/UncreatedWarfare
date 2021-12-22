using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Revives;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Tickets;
using Uncreated.Warfare.Vehicles;
using Uncreated.Warfare.Teams;
using UnityEngine;
using Uncreated.Players;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Gamemodes.Flags;

namespace Uncreated.Warfare.Gamemodes.Insurgency
{
    public class Insurgency : TeamGamemode, ITeams, IFOBs, IVehicles, IKitRequests, IRevives, ISquads, IImplementsLeaderboard<InsurgencyPlayerStats, InsurgencyTracker>, IStructureSaving, ITickets, IStagingPhase, IAttackDefense, IGameStats
    {
        public override string DisplayName => "Insurgency";
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

        private uint _counter;

        protected ulong _attackTeam;
        public ulong AttackingTeam { get => _attackTeam; }
        protected ulong _defendTeam;
        public ulong DefendingTeam { get => _defendTeam; }

        public int IntelligencePoints;

        public int CachesLeft { get; private set; }
        public int CachesDestroyed { get; private set; }
        public List<CacheData> Caches;
        public List<CacheData> ActiveCaches { get => Caches.Where(c => c.IsActive && !c.IsDestroyed).ToList(); }
        public int ActiveCachesCount { get => Caches.Count(c => c.IsActive && !c.IsDestroyed); }
        private List<Vector3> SeenCaches;

        public bool _isScreenUp;
        public bool isScreenUp { get => _isScreenUp; }

        protected InsurgencyTracker _gameStats;
        public InsurgencyTracker GameStats { get => _gameStats; }
        protected InsurgencyLeaderboard _endScreen;
        Leaderboard<InsurgencyPlayerStats, InsurgencyTracker> IImplementsLeaderboard<InsurgencyPlayerStats, InsurgencyTracker>.Leaderboard { get => _endScreen; }

        object IGameStats.GameStats => _gameStats;

        private TicketManager _ticketManager;
        public TicketManager TicketManager { get => _ticketManager; }

        public Insurgency() : base("Insurgency", 0.25F) { }
        public override void Init()
        {
            base.Init();
            _counter = 0;
            _FOBManager = new FOBManager();
            _squadManager = new SquadManager();
            _kitManager = new KitManager();
            _vehicleBay = new VehicleBay();
            _reviveManager = new ReviveManager();
            _ticketManager = new TicketManager();
        }

        public override void OnLevelLoaded()
        {
            _structureSaver = new StructureSaver();
            _vehicleSpawner = new VehicleSpawner();
            _vehicleSigns = new VehicleSigns();
            _requestSigns = new RequestSigns();
            _gameStats = UCWarfare.I.gameObject.AddComponent<InsurgencyTracker>();
            FOBManager.LoadFobsFromMap();
            RepairManager.LoadRepairStations();
            VehicleSpawner.OnLevelLoaded();
            RallyManager.WipeAllRallies();
            VehicleSigns.InitAllSigns();
            base.OnLevelLoaded();
        }
        public override void StartNextGame(bool onLoad = false)
        {
            base.StartNextGame(onLoad); // set game id
            _gameStats.Reset();

            _attackTeam = (ulong)UnityEngine.Random.Range(1, 3);
            if (_attackTeam == 1)
                _defendTeam = 2;
            else if (_attackTeam == 2)
                _defendTeam = 1;

            CachesDestroyed = 0;
            Caches = new List<CacheData>();
            SeenCaches = new List<Vector3>();

            TicketManager.OnNewGameStarting();
            if (!onLoad)
            {
                VehicleSpawner.RespawnAllVehicles();
            }
            FOBManager.OnNewGameStarting();
            RallyManager.WipeAllRallies();

            CachesLeft = UnityEngine.Random.Range(Config.Insurgency.MinStartingCaches, Config.Insurgency.MaxStartingCaches + 1);
            for (int i = 0; i < CachesLeft; i++)
                Caches.Add(new CacheData());

            SpawnNewCache();
            if (_attackTeam == 1)
                SpawnBlockerOnT1();
            else
                SpawnBlockerOnT2();
            StartStagingPhase(Config.Insurgency.StagingTime);
        }
        public override void DeclareWin(ulong winner)
        {
            L.Log(TeamManager.TranslateName(winner, 0) + " just won the game!", ConsoleColor.Cyan);

            foreach (SteamPlayer client in Provider.clients)
            {
                client.SendChat("team_win", TeamManager.TranslateName(winner, client.playerID.steamID.m_SteamID), TeamManager.GetTeamHexColor(winner));
                client.player.movement.forceRemoveFromVehicle();
                EffectManager.askEffectClearByID(UCWarfare.Config.GiveUpUI, client.transportConnection);
                ToastMessage.QueueMessage(client.player, "", Translation.Translate("team_win", client, TeamManager.TranslateName(winner, client.playerID.steamID.m_SteamID), TeamManager.GetTeamHexColor(winner)), EToastMessageSeverity.BIG);
            }
            StatsManager.ModifyTeam(winner, t => t.Wins++, false);
            StatsManager.ModifyTeam(TeamManager.Other(winner), t => t.Losses++, false);


            foreach (InsurgencyPlayerStats played in GameStats.stats.Values)
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


            this._state = EState.FINISHED;
            TicketManager.OnRoundWin(winner);
            StartCoroutine(EndGameCoroutine(winner));
        }
        private IEnumerator<WaitForSeconds> EndGameCoroutine(ulong winner)
        {
            yield return new WaitForSeconds(Config.GeneralConfig.LeaderboardDelay);
            InvokeOnTeamWin(winner);

            ReplaceBarricadesAndStructures();
            Commands.ClearCommand.WipeVehiclesAndRespawn();
            Commands.ClearCommand.ClearItems();

            _endScreen = UCWarfare.I.gameObject.AddComponent<InsurgencyLeaderboard>();
            _endScreen.OnLeaderboardExpired = OnShouldStartNewGame;
            _endScreen.SetShutdownConfig(shutdownAfterGame, shutdownMessage);
            _isScreenUp = true;
            _endScreen.StartLeaderboard(winner, _gameStats);
        }
        private void OnShouldStartNewGame()
        {
            if (_endScreen != null)
            {
                _endScreen.OnLeaderboardExpired = null;
                Destroy(_endScreen);
            }
            _isScreenUp = false;
            EndGame();
        }
        protected override void EventLoopAction()
        {
            FOBManager.OnGameTick(_counter);

            CheckPlayersAMC();
            _counter++;
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
                _endScreen.SendLeaderboard(player, TeamManager.GetTeamHexColor(_endScreen.Winner), this);
            }
            else
            {
                if (State == EState.STAGING)
                    this.ShowStagingUI(player);
                InsurgencyUI.SendCacheList(player);
                TicketManager.UpdateUI(player.connection, player.GetTeam(), 0, "");
            }
            StatsManager.RegisterPlayer(player.CSteamID.m_SteamID);
            StatsManager.ModifyStats(player.CSteamID.m_SteamID, s => s.LastOnline = DateTime.Now.Ticks);
            base.OnPlayerJoined(player, wasAlreadyOnline);
        }
        public override void OnGroupChanged(UCPlayer player, ulong oldGroup, ulong newGroup, ulong oldteam, ulong newteam)
        {
            if (State == EState.STAGING)
            {
                if (newteam != 1 && newteam != 2)
                    ClearStagingUI(player);
                else
                    ShowStagingUI(player);
            }
            InsurgencyUI.SendCacheList(player);
            TicketManager.UpdateUI(player.connection, player.GetTeam(), 0, "");
            base.OnGroupChanged(player, oldGroup, newGroup, oldteam, newteam);
        }
        public bool AddIntelligencePoints(int points)
        {
            List<CacheData> activeCaches = ActiveCaches;
            if (activeCaches.Count != 1 && activeCaches.Count != 2) return false;
            CacheData first = activeCaches[0];
            if (first == null) return false;
            if (activeCaches.Count == 1)
            {
                if (!first.IsDiscovered)
                {
                    IntelligencePoints += points;
                    if (IntelligencePoints >= Config.Insurgency.IntelPointsToDiscovery)
                    {
                        IntelligencePoints = 0;
                        OnCacheDiscovered(first.Cache);
                        return true;
                    }
                    return false;
                }
                if (first.IsDiscovered && CachesLeft != 1)
                {
                    IntelligencePoints += points;
                    if (IntelligencePoints >= Config.Insurgency.IntelPointsToSpawn)
                    {
                        IntelligencePoints = 0;
                        SpawnNewCache(true);
                        return true;
                    }
                    return false;
                }
                return false;
            }
            CacheData last = activeCaches[activeCaches.Count - 1];
            if (last == null) return false;
            if (!last.IsDiscovered)
            {
                IntelligencePoints += points;
                if (IntelligencePoints >= Config.Insurgency.IntelPointsToDiscovery)
                {
                    IntelligencePoints = 0;
                    OnCacheDiscovered(last.Cache);
                    return true;
                }
                return false;
            }
            return false;
        }
        public void OnCacheDiscovered(FOB cache)
        {
            cache.isDiscovered = true;

            foreach (UCPlayer player in PlayerManager.OnlinePlayers)
            {
                if (player.GetTeam() == AttackingTeam)
                    ToastMessage.QueueMessage(player, Translation.Translate("cache_discovered_attack", player, cache.ClosestLocation.ToUpper()), "", EToastMessageSeverity.BIG);
                else if (player.GetTeam() == DefendingTeam)
                    ToastMessage.QueueMessage(player, Translation.Translate("cache_discovered_defense", player), "", EToastMessageSeverity.BIG);
            }
            for (int i = 0; i < Caches.Count; i++)
            {
                CacheData d = Caches[i];
                if (d.Cache == cache)
                {
                    InsurgencyUI.ReplicateCacheUpdate(d);
                    break;
                }
            }
        }
        public void SpawnNewCache(bool message = false)
        {
            IEnumerable<SerializableTransform> viableSpawns = Config.MapConfig.CacheSpawns.Where(c1 => !SeenCaches.Contains(c1.Position) && SeenCaches.All(c => (c1.Position - c).sqrMagnitude > Math.Pow(300, 2)));

            if (viableSpawns.Count() == 0)
            {
                L.LogWarning("NO VIABLE CACHE SPAWNS");
                return;
            }
            SerializableTransform transform = viableSpawns.ElementAt(UnityEngine.Random.Range(0, viableSpawns.Count()));

            if (!(Assets.find(Config.Barricades.InsurgencyCacheGUID) is ItemBarricadeAsset barricadeAsset))
            {
                L.LogWarning("Invalid barricade GUID for Insurgency Cache!");
                return;
            }
            Barricade barricade = new Barricade(barricadeAsset);
            Quaternion rotation = transform.Rotation;
            rotation.eulerAngles = new Vector3(transform.Rotation.eulerAngles.x - 90, transform.Rotation.eulerAngles.y, transform.Rotation.eulerAngles.z + 180);
            Transform barricadeTransform = BarricadeManager.dropNonPlantedBarricade(barricade, transform.Position, rotation, 0, DefendingTeam);
            BarricadeDrop foundationDrop = BarricadeManager.FindBarricadeByRootTransform(barricadeTransform);
            if (foundationDrop == null)
            {
                L.LogWarning("Foundation drop is null.");
                return;
            }
            FOB cache = FOBManager.RegisterNewFOB(foundationDrop, UCWarfare.GetColorHex("insurgency_cache_color"), true);
            CacheData d = Caches[CachesDestroyed];
            CacheData d2 = Caches[CachesDestroyed + 1];
            if (!d.IsActive)
                d.Activate(cache);
            else
                d2.Activate(cache);


            SeenCaches.Add(transform.Position);

            if (message)
            {
                foreach (UCPlayer player in PlayerManager.OnlinePlayers)
                    if (player.GetTeam() == DefendingTeam)
                        ToastMessage.QueueMessage(player, Translation.Translate("cache_spawned_defense", player), "", EToastMessageSeverity.BIG);
            }

            InsurgencyUI.ReplicateCacheUpdate(d);
            InsurgencyUI.ReplicateCacheUpdate(d2);

            SpawnCacheItems(cache);
        }
        void SpawnCacheItems(FOB cache)
        {
            Guid ammoID;
            Guid buildID;
            if (DefendingTeam == 1)
            {
                ammoID = Config.Items.T1Ammo;
                buildID = Config.Items.T1Build;
            }
            else if (DefendingTeam == 2)
            {
                ammoID = Config.Items.T2Ammo;
                buildID = Config.Items.T2Build;
            }
            else return;
            if (!(Assets.find(ammoID) is ItemAsset ammo) || !(Assets.find(buildID) is ItemAsset build))
                return;
            if (cache.Structure.interactable is InteractableStorage storage)
            {
                while (storage.items.tryAddItem(new Item(ammo.id, true))) { }
            }
            Vector3 point = cache.Structure.model.TransformPoint(new Vector3(0, 2, 0));

            for (int i = 0; i < 15; i++)
                ItemManager.dropItem(new Item(build.id, true), point, false, true, false);

            foreach (KeyValuePair<ushort, int> entry in Config.Insurgency.CacheItems)
            {
                for (int i = 0; i < entry.Value; i++)
                    ItemManager.dropItem(new Item(entry.Key, true), point, false, true, true);
            }
        }
        private IEnumerator<WaitForSeconds> WaitToSpawnNewCache()
        {
            yield return new WaitForSeconds(60);
            SpawnNewCache(true);
        }
        public void OnCacheDestroyed(FOB cache, UCPlayer destroyer)
        {
            CachesDestroyed++;
            CachesLeft--;

            if (CachesLeft == 0)
            {
                DeclareWin(AttackingTeam);
            }
            else
            {
                foreach (UCPlayer player in PlayerManager.OnlinePlayers)
                {
                    if (player.GetTeam() == AttackingTeam)
                        ToastMessage.QueueMessage(player, Translation.Translate("cache_destroyed_attack", player), "", EToastMessageSeverity.BIG);
                    else if (player.GetTeam() == DefendingTeam)
                        ToastMessage.QueueMessage(player, Translation.Translate("cache_destroyed_defense", player), "", EToastMessageSeverity.BIG);
                }

                if (ActiveCachesCount == 0)
                {
                    StartCoroutine(WaitToSpawnNewCache());
                }
            }
            
            if (destroyer != null)
            {
                if (destroyer.GetTeam() == AttackingTeam)
                {
                    XP.XPManager.AddXP(destroyer.Player, Config.Insurgency.XPCacheDestroyed, Translation.Translate("xp_cache_killed", destroyer));
                    StatsManager.ModifyStats(destroyer.Steam64, x => x.FlagsCaptured++, false);
                    StatsManager.ModifyTeam(AttackingTeam, t => t.FlagsCaptured++, false);
                    if (_gameStats != null)
                    {
                        foreach (KeyValuePair<ulong, InsurgencyPlayerStats> stats in _gameStats.stats)
                        {
                            if (stats.Key == destroyer.Steam64)
                            {
                                stats.Value._cachesDestroyed++;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    XP.XPManager.AddXP(destroyer.Player, Config.Insurgency.XPCacheTeamkilled, Translation.Translate("xp_cache_teamkilled", destroyer));
                }
            }
            for (int i = 0; i < Caches.Count; i++)
            {
                if (Caches[i].Cache == cache)
                {
                    InsurgencyUI.ReplicateCacheUpdate(Caches[i]);
                    break;
                }
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
            _squadManager?.Dispose();
            _vehicleSpawner?.Dispose();
            _reviveManager?.Dispose();
            _kitManager?.Dispose();
            _vehicleBay?.Dispose();
            FOBManager.Reset();
            Destroy(_gameStats);
            base.Dispose();
        }

        public class CacheData
        {
            public int Number { get => Cache != null ? Cache.Number : 0;  }
            public bool IsActive { get => Cache != null;  }
            public bool IsDestroyed { get => Cache != null && Cache.Structure.GetServersideData().barricade.isDead; }
            public bool IsDiscovered { get => Cache != null && Cache.isDiscovered; }
            public FOB Cache { get; private set; }

            public CacheData()
            {
                Cache = null;
            }
            public void Activate(FOB cache)
            {
                Cache = cache;
            }
        }
    }
}

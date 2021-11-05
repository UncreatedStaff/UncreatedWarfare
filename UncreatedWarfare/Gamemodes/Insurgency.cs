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

namespace Uncreated.Warfare.Gamemodes
{
    internal class Insurgency : TeamGamemode, ITeams, IFOBs, IVehicles, IKitRequests, IRevives, ISquads, IImplementsLeaderboard, IStructureSaving, ITickets, IStagingPhase, IWarstatsGamemode
    {
        const float MATCH_PRESENT_THRESHOLD = 0.65f;

        private readonly Config<InsurgencyConfig> _config;
        public InsurgencyConfig Config { get => _config.Data; }

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
        protected Transform _blockerBarricade = null;

        private uint _counter;

        protected ulong _attackTeam;
        public ulong AttackingTeam { get => _attackTeam; }
        protected ulong _defendTeam;
        public ulong DefendingTeam { get => _defendTeam; }

        public int IntelligentsPoints;

        public int CachesLeft { get; private set; }
        public int CachesDestroyed { get; private set; }
        private List<CacheData> Caches;
        public List<CacheData> ActiveCaches { get => Caches.Where(c => c.IsActive && !c.IsDestroyed).ToList(); }
        private List<Vector3> SeenCaches;

        protected int _stagingSeconds;
        public int StagingSeconds { get => _stagingSeconds; }

        public bool _isScreenUp;
        public bool isScreenUp { get => _isScreenUp; }

        protected WarStatsTracker _gameStats;
        public WarStatsTracker GameStats { get => _gameStats; }
        protected EndScreenLeaderboard _endScreen;
        EndScreenLeaderboard IWarstatsGamemode.Leaderboard { get => _endScreen; }
        ILeaderboard IImplementsLeaderboard.Leaderboard { get => _endScreen; }

        private TicketManager _ticketManager;
        public TicketManager TicketManager { get => _ticketManager; }

        public Insurgency()
            : base("Insurgency", 0.25F)
        {
            _config = new Config<InsurgencyConfig>(Data.FlagStorage, "insurgency.json");
        }
        public override void Init()
        {
            base.Init();

            _counter = 0;
            InAMC = new List<ulong>();

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
            _gameStats = UCWarfare.I.gameObject.AddComponent<WarStatsTracker>();
            FOBManager.LoadFobsFromMap();
            RepairManager.LoadRepairStations();
            VehicleSpawner.OnLevelLoaded();
            RallyManager.WipeAllRallies();
            VehicleSigns.InitAllSigns();
            base.OnLevelLoaded();
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
        public override void StartNextGame(bool onLoad = false)
        {
            base.StartNextGame(onLoad); // set game id
            GameStats.Reset();

            _joinManager.OnNewGameStarting();

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

            CachesLeft = UnityEngine.Random.Range(Config.MinStartingCaches, Config.MaxStartingCaches + 1);
            for (int i = 0; i < CachesLeft; i++)
                Caches.Add(new CacheData());

            SpawnNewCache();

            StartStagingPhase(Config.StagingPhaseSeconds);
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
            StatsManager.ModifyTeam(winner, t => t.Wins++, false);
            StatsManager.ModifyTeam(TeamManager.Other(winner), t => t.Losses++, false);


            foreach (PlayerCurrentGameStats played in GameStats.playerstats.Values)
            {
                // Any player who was online for 70% of the match will be awarded a win or punished with a loss
                if ((float)played.onlineCount1 / GameStats.gamepercentagecounter >= MATCH_PRESENT_THRESHOLD)
                {
                    if (winner == 1)
                        StatsManager.ModifyStats(played.id, s => s.Wins++, false);
                    else
                        StatsManager.ModifyStats(played.id, s => s.Losses++, false);
                }
                else if ((float)played.onlineCount2 / GameStats.gamepercentagecounter >= MATCH_PRESENT_THRESHOLD)
                {
                    if (winner == 2)
                        StatsManager.ModifyStats(played.id, s => s.Wins++, false);
                    else
                        StatsManager.ModifyStats(played.id, s => s.Losses++, false);
                }
            }


            this._state = EState.FINISHED;
            TicketManager.OnRoundWin(winner);
            StartCoroutine(EndGameCoroutine(winner));
        }
        private IEnumerator<WaitForSeconds> EndGameCoroutine(ulong winner)
        {
            yield return new WaitForSeconds(10);
            InvokeOnTeamWin(winner);

            ReplaceBarricadesAndStructures();
            Commands.ClearCommand.WipeVehiclesAndRespawn();
            Commands.ClearCommand.ClearItems();

            _endScreen = UCWarfare.I.gameObject.AddComponent<EndScreenLeaderboard>();
            _endScreen.winner = winner;
            _endScreen.warstats = GameStats;
            _endScreen.OnLeaderboardExpired += OnShouldStartNewGame;
            _endScreen.ShuttingDown = shutdownAfterGame;
            _endScreen.ShuttingDownMessage = shutdownMessage;
            _endScreen.ShuttingDownPlayer = shutdownPlayer;
            _isScreenUp = true;
            _endScreen.EndGame("¶·¸¹º»:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");
            
        }
        private void OnShouldStartNewGame()
        {
            if (_endScreen != default)
                _endScreen.OnLeaderboardExpired -= OnShouldStartNewGame;
            Destroy(_endScreen);
            _isScreenUp = false;
            StartNextGame();
        }
        protected override void EventLoopAction()
        {
            CheckMainCampers();
            FOBManager.OnGameTick(_counter);

            if (_counter % 1 * 4 == 0) // 1 seconds
            {
                CheckPlayersAMC();
            }
            if (_counter % 10 * 4 == 0) // 10 seconds
            {

            }

            _counter++;
        }
        private void CheckMainCampers()
        {
            IEnumerator<SteamPlayer> players = Provider.clients.GetEnumerator();
            while (players.MoveNext())
            {
                ulong team = players.Current.GetTeam();
                UCPlayer player = UCPlayer.FromSteamPlayer(players.Current);
                try
                {
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
                    }
                    else
                    {
                        InAMC.Remove(players.Current.playerID.steamID.m_SteamID);
                    }
                }
                catch (Exception ex)
                {
                    F.LogError("Error checking for duty players on player " + players.Current.playerID.playerName);
                    if (UCWarfare.Config.Debug)
                        F.LogError(ex);
                }
            }
            players.Dispose();
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
            if (isScreenUp && _endScreen != null)
            {
                _endScreen.SendScreenToPlayer(player.Player.channel.owner, "¶·¸¹º»:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");
            }
            else
            {
                UpdateUI(player);
            }
            StatsManager.RegisterPlayer(player.CSteamID.m_SteamID);
            StatsManager.ModifyStats(player.CSteamID.m_SteamID, s => s.LastOnline = DateTime.Now.Ticks);
            base.OnPlayerJoined(player, wasAlreadyOnline);
        }
        public override void OnGroupChanged(SteamPlayer player, ulong oldGroup, ulong newGroup, ulong oldteam, ulong newteam)
        {
            UpdateUI(UCPlayer.FromSteamPlayer(player));
            base.OnGroupChanged(player, oldGroup, newGroup, oldteam, newteam);
        }
        public void AddIntelligencePoints(int points)
        {
            var activeCaches = ActiveCaches;
            if (activeCaches.Count == 1 && !activeCaches.First().IsDiscovered)
            {
                IntelligentsPoints += points;
                if (IntelligentsPoints >= Config.IntelPointsToDiscovery)
                {
                    IntelligentsPoints = 0;
                    OnCacheDiscovered(activeCaches.First().Cache);
                }
            }
            else if (activeCaches.Count == 1 && activeCaches.First().IsDiscovered && CachesLeft != 1)
            {
                IntelligentsPoints += points;
                if (IntelligentsPoints >= Config.IntelPointsToSpawn)
                {
                    IntelligentsPoints = 0;
                    SpawnNewCache(true);
                }
            }
            else if (activeCaches.Count == 2 && !activeCaches.Last().IsDiscovered)
            {
                IntelligentsPoints += points;
                if (IntelligentsPoints >= Config.IntelPointsToDiscovery)
                {
                    IntelligentsPoints = 0;
                    OnCacheDiscovered(activeCaches.Last().Cache);
                }
            }
        }
        public override void OnPlayerDeath(UCWarfare.DeathEventArgs args)
        {
            InAMC.Remove(args.dead.channel.owner.playerID.steamID.m_SteamID);
            EventFunctions.RemoveDamageMessageTicks(args.dead.channel.owner.playerID.steamID.m_SteamID);
        }
        public void OnCacheDiscovered(FOB cache)
        {
            cache.isDiscovered = true;

            foreach (UCPlayer player in PlayerManager.OnlinePlayers)
            {
                if (player.GetTeam() == AttackingTeam)
                    ToastMessage.QueueMessage(player, F.Translate("cache_discovered_attack", player, cache.ClosestLocation.ToUpper()), "", ToastMessageSeverity.BIG);
                else if (player.GetTeam() == DefendingTeam)
                    ToastMessage.QueueMessage(player, F.Translate("cache_discovered_defence", player), "", ToastMessageSeverity.BIG);
            }

            UpdateUIAll();
        }
        public void SpawnNewCache(bool message = false)
        {
            IEnumerable<SerializableTransform> viableSpawns = Config.CacheSpawns.Where(c1 => !SeenCaches.Contains(c1.Position) && SeenCaches.All(c => (c1.Position - c).sqrMagnitude > Math.Pow(300, 2)));

            if (viableSpawns.Count() == 0)
            {
                F.LogWarning("NO VIABLE CACHE SPAWNS");
                return;
            }
            SerializableTransform transform = viableSpawns.ElementAt(UnityEngine.Random.Range(0, viableSpawns.Count()));

            Barricade barricade = new Barricade(Config.CacheID);
            Quaternion rotation = transform.Rotation;
            rotation.eulerAngles = new Vector3(transform.Rotation.eulerAngles.x - 90, transform.Rotation.eulerAngles.y, transform.Rotation.eulerAngles.z + 180);
            Transform barricadeTransform = BarricadeManager.dropNonPlantedBarricade(barricade, transform.Position, rotation, 0, DefendingTeam);
            BarricadeDrop foundationDrop = BarricadeManager.FindBarricadeByRootTransform(barricadeTransform);
            if (foundationDrop == null)
            {
                F.LogWarning("Foundation drop is null.");
                return;
            }
            FOB cache = FOBManager.RegisterNewFOB(foundationDrop, "#c480d9", true);

            if (!Caches[CachesDestroyed].IsActive)
                Caches[CachesDestroyed].Activate(cache);
            else
                Caches[CachesDestroyed + 1].Activate(cache);


            SeenCaches.Add(transform.Position);

            if (message)
            {
                foreach (UCPlayer player in PlayerManager.OnlinePlayers)
                    if (player.GetTeam() == DefendingTeam)
                        ToastMessage.QueueMessage(player, F.Translate("cache_spawned_defence", player), "", ToastMessageSeverity.BIG);
            }

            UpdateUIAll();

            SpawnCacheItems(cache);
        }
        void SpawnCacheItems(FOB cache)
        {
            ushort ammoID = 0;
            ushort buildID = 0;
            if (DefendingTeam == 1)
            {
                ammoID = FOBManager.config.Data.Team1AmmoID;
                buildID = FOBManager.config.Data.Team1BuildID;
            }
            else if (DefendingTeam == 1)
            {
                ammoID = FOBManager.config.Data.Team2AmmoID;
                buildID = FOBManager.config.Data.Team2BuildID;
            }
            if (cache.Structure.interactable is InteractableStorage storage)
            {
                while (storage.items.tryAddItem(new Item(ammoID, true))) { }
            }
            Vector3 point = cache.Structure.model.TransformPoint(new Vector3(0, 2, 0));

            for (int i = 0; i < 15; i++)
                ItemManager.dropItem(new Item(buildID, true), point, false, true, false);

            foreach (KeyValuePair<ushort, int> entry in Config.CacheItems)
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
                foreach (var player in PlayerManager.OnlinePlayers)
                {
                    if (player.GetTeam() == AttackingTeam)
                        ToastMessage.QueueMessage(player, F.Translate("cache_destroyed_attack", player), "", ToastMessageSeverity.BIG);
                    else if (player.GetTeam() == DefendingTeam)
                        ToastMessage.QueueMessage(player, F.Translate("cache_destroyed_defence", player), "", ToastMessageSeverity.BIG);
                }

                if (ActiveCaches.Count == 0)
                {
                    StartCoroutine(WaitToSpawnNewCache());
                }
            }
            
            if (destroyer != null)
            {
                if (destroyer.GetTeam() == AttackingTeam)
                {
                    XP.XPManager.AddXP(destroyer.Player, Config.XPCacheDestroyed, F.Translate("xp_cache_killed", destroyer));
                    Stats.StatsManager.ModifyStats(destroyer.Steam64, x => x.FlagsCaptured++, false);
                    Stats.StatsManager.ModifyTeam(AttackingTeam, t => t.FlagsCaptured++, false);
                }
                else
                {
                    XP.XPManager.AddXP(destroyer.Player, Config.XPCacheTeamkilled, F.Translate("xp_cache_teamkilled", destroyer));
                }
            }
            UpdateUIAll();
        }
        public void UpdateUI(UCPlayer player)
        {
            if (State == EState.STAGING)
                this.ShowStagingUI(player);
            TicketManager.UpdateUI(player.connection, player.GetTeam(), 0, "");

            ClearUI(player);

            int FirstUI = UCWarfare.Config.FlagSettings.FlagUIIdFirst;
            for (int i = 0; i < Caches.Count; i++) unchecked
                {
                    CacheData cache = Caches[i];

                    string text;
                    if (!cache.IsActive)
                    {
                        if (player.GetTeam() == AttackingTeam)
                        {
                            text = $"<color=#696969>Undiscovered</color>";
                        }
                        else
                        {
                            text = $"<color=#696969>Unknown</color>";
                        }
                    }
                    else if (cache.IsDestroyed)
                    {
                        if (player.GetTeam() == AttackingTeam)
                        {
                            text = $"<color=#5a6e5c>Destroyed</color>";
                        }
                        else
                        {
                            text = $"<color=#6b5858>Lost</color>";
                        }
                    }
                    else
                    {
                        if (cache.IsDiscovered)
                        {
                            if (player.GetTeam() == AttackingTeam)
                            {
                                text = $"<color=#ffca61>{cache.Cache.Name}</color> <color=#c2c2c2>{cache.Cache.ClosestLocation}</color>";
                            }
                            else
                            {
                                text = $"<color=#c480d9>{cache.Cache.Name}</color> <color=#c2c2c2>{cache.Cache.ClosestLocation}</color>";
                            }
                        }
                        else
                        {
                            if (player.GetTeam() == AttackingTeam)
                            {
                                text = $"<color=#696969>Undiscovered</color>";

                            }
                            else
                            {
                                text = $"<color=#84d980>{cache.Cache.Name}</color> <color=#c2c2c2>{cache.Cache.ClosestLocation}</color>";
                            }
                        }
                    }

                    EffectManager.sendUIEffect((ushort)(FirstUI + i), (short)(1000 + i), player.connection, true,
                        text,
                        ""
                        );
                }
        }
        public void UpdateUIAll()
        {
            foreach (UCPlayer player in PlayerManager.OnlinePlayers)
                UpdateUI(player);
        }
        public void ClearUI(UCPlayer player)
        {
            int FirstUI = UCWarfare.Config.FlagSettings.FlagUIIdFirst;
            for (int i = 0; i < 10; i++) unchecked
                {
                    EffectManager.askEffectClearByID((ushort)(FirstUI + i), player.connection);
                }
        }
        public void ClearUIAll()
        {
            foreach (UCPlayer player in PlayerManager.OnlinePlayers)
                ClearUI(player);
        }

        public void ReloadConfig() => _config.Reload();
        public void SaveConfig() => _config.Save();

        public void StartStagingPhase(int seconds)
        {
            _stagingSeconds = seconds;
            _state = EState.STAGING;
            PlaceBlockerOverAttackerMain();
            StartCoroutine(StagingPhaseLoop());
        }
        public void SkipStagingPhase()
        {
            _stagingSeconds = 0;
        }
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
            DestroyBlockerBarricade();
            TicketManager.OnStagingPhaseEnded();

            foreach (UCPlayer player in PlayerManager.OnlinePlayers)
                EffectManager.askEffectClearByID(Config.HeaderID, player.connection);

            _state = EState.ACTIVE;
        }
        public override void Dispose()
        {
            _squadManager?.Dispose();
            _vehicleSpawner?.Dispose();
            _reviveManager?.Dispose();
            _kitManager?.Dispose();
            EndStagingPhase();
            FOBManager.Reset();
            Destroy(_gameStats);
            base.Dispose();
        }


        public class InsurgencyConfig : ConfigData
        {
            public int MinStartingCaches;
            public int MaxStartingCaches;
            public int StagingPhaseSeconds;
            public int AttackStartingTickets;
            public float NearOtherBaseKillTimer;
            public float CacheDiscoverRange;
            public int IntelPointsToDiscovery;
            public int IntelPointsToSpawn;
            public int XPCacheDestroyed;
            public int XPCacheTeamkilled;
            public int TicketsCache;
            public ushort HeaderID;
            public ushort T1BlockerID;
            public ushort T2BlockerID;
            public ushort CacheID;
            public ushort CacheStartingBuild;
            public Dictionary<ushort, int> CacheItems;
            public List<SerializableTransform> CacheSpawns;

            public override void SetDefaults()
            {
                MinStartingCaches = 4;
                MaxStartingCaches = 6;
                StagingPhaseSeconds = 150;
                AttackStartingTickets = 300;
                NearOtherBaseKillTimer = 7;
                CacheDiscoverRange = 75;
                IntelPointsToDiscovery = 30;
                IntelPointsToSpawn = 15;
                XPCacheDestroyed = 800;
                HeaderID = 36066;
                T1BlockerID = 36058;
                T2BlockerID = 36059;
                XPCacheTeamkilled = -8000;
                TicketsCache = 80;
                CacheID = 38404;
                CacheStartingBuild = 15;
                CacheItems = new Dictionary<ushort, int>();
                CacheSpawns = new List<SerializableTransform>();
            }
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

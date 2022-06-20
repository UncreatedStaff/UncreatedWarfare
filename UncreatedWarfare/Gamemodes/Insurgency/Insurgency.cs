using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Players;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Revives;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Tickets;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using Cache = Uncreated.Warfare.Components.Cache;

namespace Uncreated.Warfare.Gamemodes.Insurgency;

public class Insurgency : 
    TeamGamemode,
    ITeams,
    IFOBs,
    IVehicles,
    IKitRequests,
    IRevives,
    ISquads,
    IImplementsLeaderboard<InsurgencyPlayerStats, InsurgencyTracker>,
    IStructureSaving,
    ITickets,
    IStagingPhase,
    IAttackDefense,
    IGameStats
{
    protected VehicleSpawner _vehicleSpawner;
    protected VehicleBay _vehicleBay;
    protected VehicleSigns _vehicleSigns;
    protected FOBManager _FOBManager;
    protected RequestSigns _requestSigns;
    protected KitManager _kitManager;
    protected ReviveManager _reviveManager;
    protected SquadManager _squadManager;
    protected StructureSaver _structureSaver;
    protected InsurgencyTracker _gameStats;
    protected InsurgencyLeaderboard _endScreen;
    private TicketManager _ticketManager;
    protected ulong _attackTeam;
    protected ulong _defendTeam;
    public int IntelligencePoints;
    public List<CacheData> Caches;
    private List<Vector3> SeenCaches;
    public bool _isScreenUp;
    public override string DisplayName => "Insurgency";
    public override EGamemode GamemodeType => EGamemode.INVASION;
    public override bool EnableAMC => true;
    public override bool ShowOFPUI => true;
    public override bool ShowXPUI => true;
    public override bool TransmitMicWhileNotActive => true;
    public override bool UseJoinUI => true;
    public override bool UseWhitelist => true;
    public override bool AllowCosmetics => UCWarfare.Config.AllowCosmetics;
    public VehicleSpawner VehicleSpawner => _vehicleSpawner;
    public VehicleBay VehicleBay => _vehicleBay;
    public VehicleSigns VehicleSigns => _vehicleSigns;
    public FOBManager FOBManager => _FOBManager;
    public RequestSigns RequestSigns => _requestSigns;
    public KitManager KitManager => _kitManager;
    public ReviveManager ReviveManager => _reviveManager;
    public SquadManager SquadManager => _squadManager;
    public StructureSaver StructureSaver => _structureSaver;
    public ulong AttackingTeam => _attackTeam;
    public ulong DefendingTeam => _defendTeam;
    public int CachesLeft { get; private set; }
    public int CachesDestroyed { get; private set; }
    public List<CacheData> ActiveCaches => Caches.Where(c => c.IsActive && !c.IsDestroyed).ToList();
    public List<CacheData> DiscoveredCaches => Caches.Where(c => c.IsActive && !c.IsDestroyed && c.IsDestroyed).ToList();
    public int ActiveCachesCount => Caches.Count(c => c.IsActive && !c.IsDestroyed);
    public bool isScreenUp => _isScreenUp;
    public InsurgencyTracker WarstatsTracker => _gameStats;
    Leaderboard<InsurgencyPlayerStats, InsurgencyTracker> IImplementsLeaderboard<InsurgencyPlayerStats, InsurgencyTracker>.Leaderboard => _endScreen;
    object IGameStats.GameStats => _gameStats;
    public TicketManager TicketManager { get => _ticketManager; }
    public Insurgency() : base("Insurgency", 0.25F) { }
    protected override void PreInit()
    {
        AddSingletonRequirement(ref _squadManager);
        AddSingletonRequirement(ref _kitManager);
        AddSingletonRequirement(ref _vehicleSpawner);
        AddSingletonRequirement(ref _reviveManager);
        AddSingletonRequirement(ref _ticketManager);
        AddSingletonRequirement(ref _vehicleBay);
        AddSingletonRequirement(ref _FOBManager);
        AddSingletonRequirement(ref _structureSaver);
        AddSingletonRequirement(ref _vehicleSigns);
        AddSingletonRequirement(ref _requestSigns);
        base.PreInit();
    }
    protected override void PostInit()
    {
        Commands.ReloadCommand.ReloadKits();
    }
    protected override void OnReady()
    {
        _gameStats = UCWarfare.I.gameObject.AddComponent<InsurgencyTracker>();
        RepairManager.LoadRepairStations();
        RallyManager.WipeAllRallies();
        VehicleSigns.InitAllSigns();
        base.OnReady();
    }
    protected override void PostDispose()
    {
        Destroy(_gameStats);
        base.PostDispose();
    }
    protected override void PreGameStarting(bool isOnLoad)
    {
        _gameStats.Reset();

        _attackTeam = (ulong)UnityEngine.Random.Range(1, 3);
        if (_attackTeam == 1)
            _defendTeam = 2;
        else if (_attackTeam == 2)
            _defendTeam = 1;

        CachesDestroyed = 0;
        Caches = new List<CacheData>();
        SeenCaches = new List<Vector3>();

        CachesLeft = UnityEngine.Random.Range(Config.Insurgency.MinStartingCaches, Config.Insurgency.MaxStartingCaches + 1);
        for (int i = 0; i < CachesLeft; i++)
            Caches.Add(new CacheData());
        base.PreGameStarting(isOnLoad);
    }
    protected override void PostGameStarting(bool isOnLoad)
    {
        base.PostGameStarting(isOnLoad);
        RallyManager.WipeAllRallies();

        SpawnNewCache();
        if (_attackTeam == 1)
            SpawnBlockerOnT1();
        else
            SpawnBlockerOnT2();
        StartStagingPhase(Config.Insurgency.StagingTime);
    }
    public override void DeclareWin(ulong winner)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (this._state == EState.FINISHED) return;
        this._state = EState.FINISHED;
        L.Log(TeamManager.TranslateName(winner, 0) + " just won the game!", ConsoleColor.Cyan);

        string Team1Tickets = "";
        string Team2Tickets = "";
        if (AttackingTeam == 1)
        {
            Team1Tickets = TicketManager.Team1Tickets.ToString() + " Tickets";
            if (TicketManager.Team1Tickets <= 0)
                Team1Tickets = Team1Tickets.Colorize("969696");

            Team2Tickets = CachesLeft.ToString() + " Caches left";
            if (CachesLeft <= 0)
                Team2Tickets = Team2Tickets.Colorize("969696");
        }
        else
        {
            Team2Tickets = TicketManager.Team2Tickets.ToString() + " Tickets";
            if (TicketManager.Team2Tickets <= 0)
                Team2Tickets = Team2Tickets.Colorize("969696");

            Team1Tickets = CachesLeft.ToString() + " Caches left";
            if (CachesLeft <= 0)
                Team1Tickets = Team1Tickets.Colorize("969696");
        }

        ushort winToastUI = 0;
        if (Assets.find(Config.UI.WinToastGUID) is EffectAsset e)
        {
            winToastUI = e.id;
        }
        else
            L.LogWarning("WinToast UI not found. GUID: " + Config.UI.WinToastGUID);

        QuestManager.OnGameOver(winner);
        ActionLog.Add(EActionLogType.TEAM_WON, TeamManager.TranslateName(winner, 0));

        foreach (SteamPlayer client in Provider.clients)
        {
            client.SendChat("team_win", TeamManager.TranslateName(winner, client.playerID.steamID.m_SteamID), TeamManager.GetTeamHexColor(winner));
            client.player.movement.forceRemoveFromVehicle();
            EffectManager.askEffectClearByID(Config.UI.InjuredUI, client.transportConnection);

            EffectManager.sendUIEffect(winToastUI, 12345, client.transportConnection, true);
            EffectManager.sendUIEffectText(12345, client.transportConnection, true, "Header", Translation.Translate("team_win", client, TeamManager.TranslateName(winner, client.playerID.steamID.m_SteamID), "ffffff"));
            EffectManager.sendUIEffectText(12345, client.transportConnection, true, "Team1Tickets", Team1Tickets);
            EffectManager.sendUIEffectText(12345, client.transportConnection, true, "Team2Tickets", Team2Tickets);
        }
        StatsManager.ModifyTeam(winner, t => t.Wins++, false);
        StatsManager.ModifyTeam(TeamManager.Other(winner), t => t.Losses++, false);


        foreach (InsurgencyPlayerStats played in _gameStats.stats.Values)
        {
            // Any player who was online for 70% of the match will be awarded a win or punished with a loss
            if ((float)played.onlineCount1 / _gameStats.coroutinect >= MATCH_PRESENT_THRESHOLD)
            {
                if (winner == 1)
                    StatsManager.ModifyStats(played.Steam64, s => s.Wins++, false);
                else
                    StatsManager.ModifyStats(played.Steam64, s => s.Losses++, false);
            }
            else if ((float)played.onlineCount2 / _gameStats.coroutinect >= MATCH_PRESENT_THRESHOLD)
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
    private IEnumerator<WaitForSeconds> TryDiscoverFirstCache()
    {
        yield return new WaitForSeconds(Config.Insurgency.FirstCacheSpawnTime);

#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (ActiveCaches.Count > 0 && !ActiveCaches.First().IsDiscovered)
        {
            IntelligencePoints = 0;
            OnCacheDiscovered(ActiveCaches.First().Cache);
        }
    }
    private IEnumerator<WaitForSeconds> EndGameCoroutine(ulong winner)
    {
        yield return new WaitForSeconds(Config.GeneralConfig.LeaderboardDelay);
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        InvokeOnTeamWin(winner);

        ReplaceBarricadesAndStructures();
        Commands.ClearCommand.WipeVehicles();
        Commands.ClearCommand.ClearItems();

        _endScreen = UCWarfare.I.gameObject.AddComponent<InsurgencyLeaderboard>();
        _endScreen.OnLeaderboardExpired = OnShouldStartNewGame;
        _endScreen.SetShutdownConfig(shutdownAfterGame, shutdownMessage);
        _isScreenUp = true;
        _endScreen.StartLeaderboard(winner, _gameStats);
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
    protected override void EventLoopAction()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        CheckPlayersAMC();
        TeamManager.EvaluateBases();
    }
    public override void PlayerInit(UCPlayer player, bool wasAlreadyOnline)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        base.PlayerInit(player, wasAlreadyOnline);
        if (KitManager.KitExists(player.KitName, out Kit kit))
        {
            if (kit.IsLimited(out int currentPlayers, out int allowedPlayers, player.GetTeam()) || (kit.IsLoadout && kit.IsClassLimited(out currentPlayers, out allowedPlayers, player.GetTeam())))
            {
                if (!KitManager.TryGiveRiflemanKit(player))
                    KitManager.TryGiveUnarmedKit(player);
            }
        }
        ulong team = player.GetTeam();
        FPlayerName names = F.GetPlayerOriginalNames(player);
        if ((player.KitName == null || player.KitName == string.Empty) && team > 0 && team < 3)
        {
            if (KitManager.KitExists(team == 1 ? TeamManager.Team1UnarmedKit : TeamManager.Team2UnarmedKit, out Kit unarmed))
                KitManager.GiveKit(player, unarmed);
            else if (KitManager.KitExists(TeamManager.DefaultKit, out unarmed)) KitManager.GiveKit(player, unarmed);
            else L.LogWarning("Unable to give " + names.PlayerName + " a kit.");
        }
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
        _gameStats.OnPlayerJoin(player);
        if (isScreenUp && _endScreen != null)
        {
            _endScreen.OnPlayerJoined(player);
        }
        else if (!UseJoinUI)
        {
            if (State == EState.STAGING)
                this.ShowStagingUI(player);
            InsurgencyUI.SendCacheList(player);
            int bleed = TicketManager.GetTeamBleed(player.GetTeam());
            TicketManager.GetUIDisplayerInfo(player.GetTeam(), bleed, out ushort UIID, out string tickets, out string message);
            TicketManager.UpdateUI(player.Connection, UIID, tickets, message);
        }
        StatsManager.RegisterPlayer(player.CSteamID.m_SteamID);
        StatsManager.ModifyStats(player.CSteamID.m_SteamID, s => s.LastOnline = DateTime.Now.Ticks);
    }
    public override void OnGroupChanged(GroupChanged e)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (State == EState.STAGING)
        {
            if (e.NewTeam is < 1 or > 2)
                ClearStagingUI(e.Player);
            else
                ShowStagingUI(e.Player);
        }
        if (e.NewTeam is > 0 and < 3)
        {
            InsurgencyUI.SendCacheList(e.Player);
        }
        else
        {
            CTFUI.ClearFlagList(e.Player);
            CTFUI.ClearCaptureUI(e.Player);
        }
        base.OnGroupChanged(e);
    }
    public bool AddIntelligencePoints(int points)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
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
    public void OnCacheDiscovered(Cache cache)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        cache.IsDiscovered = true;

        foreach (UCPlayer player in PlayerManager.OnlinePlayers)
        {
            if (player.GetTeam() == AttackingTeam)
                ToastMessage.QueueMessage(player, new ToastMessage(Translation.Translate("cache_discovered_attack", player, cache.ClosestLocation.ToUpper()), "", EToastMessageSeverity.BIG));
            else if (player.GetTeam() == DefendingTeam)
                ToastMessage.QueueMessage(player, new ToastMessage(Translation.Translate("cache_discovered_defense", player), "", EToastMessageSeverity.BIG));
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

        cache.SpawnAttackIcon();

        if (AttackingTeam == 1)
            TicketManager.UpdateUITeam1();
        else if (AttackingTeam == 2)
            TicketManager.UpdateUITeam2();
        VehicleSigns.OnFlagCaptured();
    }
    public void SpawnNewCache(bool message = false)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
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
        rotation.eulerAngles = new Vector3(transform.Rotation.eulerAngles.x, transform.Rotation.eulerAngles.y, transform.Rotation.eulerAngles.z);
        Transform barricadeTransform = BarricadeManager.dropNonPlantedBarricade(barricade, transform.Position, rotation, 0, DefendingTeam);
        BarricadeDrop foundationDrop = BarricadeManager.FindBarricadeByRootTransform(barricadeTransform);
        if (foundationDrop == null)
        {
            L.LogWarning("Foundation drop is null.");
            return;
        }
        Cache cache = FOBManager.RegisterNewCache(foundationDrop);
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
                    ToastMessage.QueueMessage(player, new ToastMessage(Translation.Translate("cache_spawned_defense", player), "", EToastMessageSeverity.BIG));
        }

        InsurgencyUI.ReplicateCacheUpdate(d);
        InsurgencyUI.ReplicateCacheUpdate(d2);

        SpawnCacheItems(cache);
    }
    void SpawnCacheItems(Cache cache)
    {
        //try
        //{
        //    Guid ammoID;
        //    Guid buildID;
        //    if (DefendingTeam == 1)
        //    {
        //        ammoID = Config.Items.T1Ammo;
        //        buildID = Config.Items.T1Build;
        //    }
        //    else if (DefendingTeam == 2)
        //    {
        //        ammoID = Config.Items.T2Ammo;
        //        buildID = Config.Items.T2Build;
        //    }
        //    else return;
        //    if (!(Assets.find(ammoID) is ItemAsset ammo) || !(Assets.find(buildID) is ItemAsset build))
        //        return;
        //    Vector3 point = cache.Structure.model.TransformPoint(new Vector3(0, 2, 0));

        //    for (int i = 0; i < 15; i++)
        //        ItemManager.dropItem(new Item(build.id, true), point, false, true, false);

        //    foreach (KeyValuePair<ushort, int> entry in Config.Insurgency.CacheItems)
        //    {
        //        for (int i = 0; i < entry.Value; i++)
        //            ItemManager.dropItem(new Item(entry.Key, true), point, false, true, true);
        //    }
        //}
        //catch(Exception ex)
        //{
        //    L.LogError(ex.ToString());
        //}
    }
    private IEnumerator<WaitForSeconds> WaitToSpawnNewCache()
    {
        yield return new WaitForSeconds(60);
        SpawnNewCache(true);
    }
    public void OnCacheDestroyed(Cache cache, UCPlayer? destroyer)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        CachesDestroyed++;
        CachesLeft--;

        QuestManager.OnObjectiveCaptured(Provider.clients
            .Where(x => x.GetTeam() == _attackTeam && (x.player.transform.position - cache.Position).sqrMagnitude < 10000f)
            .Select(x => x.playerID.steamID.m_SteamID).ToArray());

        ActionLog.Add(EActionLogType.TEAM_CAPTURED_OBJECTIVE, TeamManager.TranslateName(AttackingTeam, 0) + " DESTROYED CACHE");

        if (CachesLeft == 0)
        {
            DeclareWin(AttackingTeam);
        }
        else
        {
            foreach (UCPlayer player in PlayerManager.OnlinePlayers)
            {
                if (player.GetTeam() == AttackingTeam)
                    ToastMessage.QueueMessage(player, new ToastMessage(Translation.Translate("cache_destroyed_attack", player), "", EToastMessageSeverity.BIG));
                else if (player.GetTeam() == DefendingTeam)
                    ToastMessage.QueueMessage(player, new ToastMessage(Translation.Translate("cache_destroyed_defense", player), "", EToastMessageSeverity.BIG));
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
                Points.AwardXP(destroyer.Player, Config.Insurgency.XPCacheDestroyed, Translation.Translate("xp_cache_killed", destroyer));
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
                Points.AwardXP(destroyer.Player, Config.Insurgency.XPCacheTeamkilled, Translation.Translate("xp_cache_teamkilled", destroyer));
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
        TicketManager.UpdateUITeam1();
        TicketManager.UpdateUITeam2();
        VehicleSigns.OnFlagCaptured();
    }
    public override void ShowStagingUI(UCPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        CTFUI.StagingUI.SendToPlayer(player.Connection);
        if (player.GetTeam() == AttackingTeam)
            CTFUI.StagingUI.Top.SetText(player.Connection, Translation.Translate("phases_briefing", player));
        else if (player.GetTeam() == DefendingTeam)
            CTFUI.StagingUI.Top.SetText(player.Connection, Translation.Translate("phases_preparation", player));
    }
    protected override void EndStagingPhase()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        base.EndStagingPhase();
        if (_attackTeam == 1)
            DestoryBlockerOnT1();
        else
            DestoryBlockerOnT2();
        StartCoroutine(TryDiscoverFirstCache());
    }

    public class CacheData
    {
        public int Number { get => Cache != null ? Cache.Number : 0;  }
        public bool IsActive { get => Cache != null;  }
        public bool IsDestroyed { get => Cache != null && Cache.Structure.GetServersideData().barricade.isDead; }
        public bool IsDiscovered { get => Cache != null && Cache.IsDiscovered; }
        public Cache Cache { get; private set; }

        public CacheData()
        {
            Cache = null!;
        }
        public void Activate(Cache cache)
        {
            Cache = cache;
        }
    }
}

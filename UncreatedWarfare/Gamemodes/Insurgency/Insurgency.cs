using SDG.NetTransport;
using SDG.Provider;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Uncreated.Players;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Locations;
using Uncreated.Warfare.Maps;
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
    public List<SerializableTransform> CacheSpawns;
    private List<Vector3> SeenCaches;
    public bool _isScreenUp;
    public override string DisplayName => "Insurgency";
    public override EGamemode GamemodeType => EGamemode.INVASION;
    public override bool EnableAMC => true;
    public override bool ShowOFPUI => true;
    public override bool ShowXPUI => true;
    public override bool TransmitMicWhileNotActive => true;
    public override bool UseTeamSelector => true;
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
    public bool IsScreenUp => _isScreenUp;
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
        _ticketManager.Provider = new InsurgencyTicketProvider();
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
        string file = Path.Combine(Data.Paths.MapStorage, "insurgency_caches.json");
        if (File.Exists(file))
        {
            try
            {
                using (FileStream str = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                    CacheSpawns = JsonSerializer.Deserialize<List<SerializableTransform>>(str, JsonEx.serializerSettings)!;
            }
            catch (Exception ex)
            {
                L.LogError("Error deserializing cache spawns for Insurgency at \"" + file + "\".");
                L.LogError(ex);
            }
            if (CacheSpawns == null)
            {
                for (int i = 0; i < DefaultCacheSpawns.Length; ++i)
                {
                    if (Provider.map.Equals(DefaultCacheSpawns[i].Key))
                    {
                        CacheSpawns = new List<SerializableTransform>(DefaultCacheSpawns[i].Value);
                        break;
                    }
                }
                if (CacheSpawns == null)
                {
                    CacheSpawns = new List<SerializableTransform>(0);
                    L.LogWarning("No default cache spawns found!");
                }
                else
                {
                    L.LogWarning("Falling back to default cache spawns for " + Provider.map + ".");
                }
            }
            else
            {
                L.Log("Found " + CacheSpawns.Count + " caches in cache list.", ConsoleColor.Magenta);
            }
        }
        else
        {
            for (int i = 0; i < DefaultCacheSpawns.Length; ++i)
            {
                if (Provider.map.Equals(DefaultCacheSpawns[i].Key))
                {
                    CacheSpawns = new List<SerializableTransform>(DefaultCacheSpawns[i].Value);
                    break;
                }
            }
            if (CacheSpawns == null)
            {
                CacheSpawns = new List<SerializableTransform>(0);
                using (FileStream str = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    byte[] bytes = System.Text.Encoding.UTF8.GetBytes("[]");
                    str.Write(bytes, 0, bytes.Length);
                }
                L.LogWarning("No default cache spawns found!");
            }
            else
            {
                try
                {
                    using (FileStream str = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.Read))
                        JsonSerializer.Serialize(str, CacheSpawns, JsonEx.serializerSettings);
                }
                catch (Exception ex)
                {
                    L.LogError("Error serializing cache spawns for Insurgency at \"" + file + "\".");
                    L.LogError(ex);
                }
            }
        }
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
        SendWinUI(winner);
        base.DeclareWin(winner);

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
    public override void OnPlayerDeath(PlayerDied e)
    {
        if (e.Killer is not null && !e.WasTeamkill && e.DeadTeam == _defendTeam)
        {
            AddIntelligencePoints(1);
            if (e.Killer!.Player.TryGetPlayerData(out UCPlayerData c) && c.stats is InsurgencyPlayerStats s)
                s._intelligencePointsCollected++;
            WarstatsTracker.intelligenceGathered++;
        }
        base.OnPlayerDeath(e);
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
        if (!AllowCosmetics)
            player.SetCosmeticStates(false);

        if (UCWarfare.Config.ModifySkillLevels)
            Skillset.SetDefaultSkills(player);

        ulong team = player.GetTeam();
        StatsManager.RegisterPlayer(player.CSteamID.m_SteamID);
        StatsManager.ModifyStats(player.CSteamID.m_SteamID, s => s.LastOnline = DateTime.Now.Ticks);
    }
    public override void OnJoinTeam(UCPlayer player, ulong newTeam)
    {
        OnPlayerJoinedTeam(player);
        base.OnJoinTeam(player, newTeam);
    }
    private void OnPlayerJoinedTeam(UCPlayer player)
    {
        ulong team = player.GetTeam();
        FPlayerName names = F.GetPlayerOriginalNames(player);
        if ((player.KitName == null || player.KitName == string.Empty) && team > 0 && team < 3)
        {
            if (KitManager.KitExists(team == 1 ? TeamManager.Team1UnarmedKit : TeamManager.Team2UnarmedKit, out Kit unarmed))
                KitManager.GiveKit(player, unarmed);
            else if (KitManager.KitExists(TeamManager.DefaultKit, out unarmed)) KitManager.GiveKit(player, unarmed);
            else L.LogWarning("Unable to give " + names.PlayerName + " a kit.");
        }
        _gameStats.OnPlayerJoin(player);
        if (IsScreenUp && _endScreen != null)
        {
            _endScreen.OnPlayerJoined(player);
        }
        else
        {
            InsurgencyUI.SendCacheList(player);
            TicketManager.SendUI(player);
        }
    }
    public override void OnGroupChanged(GroupChanged e)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
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
                ToastMessage.QueueMessage(player, new ToastMessage(Localization.Translate("cache_discovered_attack", player, cache.ClosestLocation.ToUpper()), "", EToastMessageSeverity.BIG));
            else if (player.GetTeam() == DefendingTeam)
                ToastMessage.QueueMessage(player, new ToastMessage(Localization.Translate("cache_discovered_defense", player), "", EToastMessageSeverity.BIG));
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

        TicketManager.UpdateUI(AttackingTeam);
        VehicleSigns.OnFlagCaptured();
    }
    public void SpawnNewCache(bool message = false)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        IEnumerable<SerializableTransform> viableSpawns = CacheSpawns.Where(c1 => !SeenCaches.Contains(c1.Position) && SeenCaches.All(c => (c1.Position - c).sqrMagnitude > Math.Pow(300, 2)));

        if (viableSpawns.Count() == 0)
        {
            L.LogWarning("NO VIABLE CACHE SPAWNS");
            return;
        }
        SerializableTransform transform = viableSpawns.ElementAt(UnityEngine.Random.Range(0, viableSpawns.Count()));

        JsonAssetReference<ItemBarricadeAsset> r = Config.Barricades.InsurgencyCacheGUID.Value;
        if (!r.Exists)
        {
            L.LogWarning("Invalid barricade GUID for Insurgency Cache!");
            return;
        }
        Barricade barricade = new Barricade(r.Asset);
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
                    ToastMessage.QueueMessage(player, new ToastMessage(Localization.Translate("cache_spawned_defense", player), "", EToastMessageSeverity.BIG));
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

        ActionLogger.Add(EActionLogType.TEAM_CAPTURED_OBJECTIVE, TeamManager.TranslateName(AttackingTeam, 0) + " DESTROYED CACHE");

        if (CachesLeft == 0)
        {
            DeclareWin(AttackingTeam);
        }
        else
        {
            foreach (UCPlayer player in PlayerManager.OnlinePlayers)
            {
                if (player.GetTeam() == AttackingTeam)
                    ToastMessage.QueueMessage(player, new ToastMessage(Localization.Translate("cache_destroyed_attack", player), "", EToastMessageSeverity.BIG));
                else if (player.GetTeam() == DefendingTeam)
                    ToastMessage.QueueMessage(player, new ToastMessage(Localization.Translate("cache_destroyed_defense", player), "", EToastMessageSeverity.BIG));
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
                Points.AwardXP(destroyer.Player, Config.Insurgency.XPCacheDestroyed, Localization.Translate("xp_cache_killed", destroyer));
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
                Points.AwardXP(destroyer.Player, Config.Insurgency.XPCacheTeamkilled, Localization.Translate("xp_cache_teamkilled", destroyer));
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
        TicketManager.UpdateUI(1);
        TicketManager.UpdateUI(2);
        VehicleSigns.OnFlagCaptured();
    }
    public override void ShowStagingUI(UCPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        CTFUI.StagingUI.SendToPlayer(player.Connection);
        if (player.GetTeam() == AttackingTeam)
            CTFUI.StagingUI.Top.SetText(player.Connection, Localization.Translate("phases_briefing", player));
        else if (player.GetTeam() == DefendingTeam)
            CTFUI.StagingUI.Top.SetText(player.Connection, Localization.Translate("phases_preparation", player));
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

    internal void AddCacheSpawn(SerializableTransform transform)
    {
        CacheSpawns.Add(transform);
        string file = Path.Combine(Data.Paths.MapStorage, "insurgency_caches.json");
        try
        {
            using (FileStream str = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.Read))
                JsonSerializer.Serialize(str, CacheSpawns, JsonEx.serializerSettings);
        }
        catch (Exception ex)
        {
            L.LogError("Error serializing cache spawns for Insurgency at \"" + file + "\".");
            L.LogError(ex);
        }
    }
    protected void SendWinUI(ulong winner)
    {
        WinToastUI.SendToAllPlayers();
        string img1 = TeamManager.Team1Faction.FlagImageURL;
        string img2 = TeamManager.Team2Faction.FlagImageURL;
        string tick1;
        string tick2;
        if (AttackingTeam == 1)
        {
            tick1 = TicketManager.Team1Tickets.ToString(Data.Locale);
            tick2 = CachesLeft.ToString(Data.Locale);
        }
        else
        {
            tick1 = CachesLeft.ToString(Data.Locale);
            tick2 = TicketManager.Team2Tickets.ToString(Data.Locale);
        }
        foreach (LanguageSet set in Localization.EnumerateLanguageSets())
        {
            string t1tickets;
            string t2tickets;
            if (AttackingTeam == 1)
            {
                t1tickets = Localization.Translate("win_ui_value_tickets", set.Language, tick1);
                if (TicketManager.Team1Tickets <= 0)
                    t1tickets = t1tickets.Colorize("969696");
                t2tickets = Localization.Translate("win_ui_value_caches", set.Language, tick2);
                if (CachesLeft <= 0)
                    t2tickets = t2tickets.Colorize("969696");
            }
            else
            {
                t1tickets = Localization.Translate("win_ui_value_caches", set.Language, tick1);
                if (CachesLeft <= 0)
                    t1tickets = t1tickets.Colorize("969696");
                t2tickets = Localization.Translate("win_ui_value_tickets", set.Language, tick2);
                if (TicketManager.Team2Tickets <= 0)
                    t2tickets = t2tickets.Colorize("969696");
            }
            string header = Localization.Translate("win_ui_header_winner", set.Language, TeamManager.TranslateName(winner, set.Language, true));
            while (set.MoveNext())
            {
                if (!set.Next.IsOnline || set.Next.HasUIHidden) continue;
                ITransportConnection c = set.Next.Connection;
                WinToastUI.Team1Flag.SetImage(c, img1);
                WinToastUI.Team2Flag.SetImage(c, img2);
                WinToastUI.Team1Tickets.SetText(c, t1tickets);
                WinToastUI.Team2Tickets.SetText(c, t2tickets);
                WinToastUI.Header.SetText(c, header);
            }
        }
    }

    public class CacheData : IObjective, IDeployable, IFOB
    {
        public int Number { get => Cache != null ? Cache.Number : 0;  }
        public bool IsActive { get => Cache != null;  }
        public bool IsDestroyed { get => Cache != null && Cache.Structure.GetServersideData().barricade.isDead; }
        public bool IsDiscovered { get => Cache != null && Cache.IsDiscovered; }
        public Cache Cache { get; private set; }
        public string Name => ((IObjective)Cache).Name;
        public Vector3 Position => ((IObjective)Cache).Position;
        public float Yaw => ((IDeployable)Cache).Yaw;
        public string ClosestLocation => ((IFOB)Cache).ClosestLocation;
        public GridLocation GridLocation => ((IFOB)Cache).GridLocation;
        public CacheData()
        {
            Cache = null!;
        }
        public void Activate(Cache cache)
        {
            Cache = cache;
        }
        public string Translate(string language, string? format, UCPlayer? target, ref TranslationFlags flags) => ((ITranslationArgument)Cache).Translate(language, format, target, ref flags);
        public bool CheckDeployable(UCPlayer player, CommandInteraction? ctx) => ((IDeployable)Cache).CheckDeployable(player, ctx);
        public bool CheckDeployableTick(UCPlayer player, bool chat) => ((IDeployable)Cache).CheckDeployableTick(player, chat);
        public void OnDeploy(UCPlayer player, bool chat) => ((IDeployable)Cache).OnDeploy(player, chat);
    }
    #region DEFAULT CACHE SPAWNS
    private static readonly KeyValuePair<string, SerializableTransform[]>[] DefaultCacheSpawns =
        new KeyValuePair<string, SerializableTransform[]>[]
        {
            new KeyValuePair<string, SerializableTransform[]>(MapScheduler.Nuijamaa, new SerializableTransform[91]
            {
                new SerializableTransform(211.300583f, 37.7143173f, 61.399395f, 0f, 179.149933f, 0f),
                new SerializableTransform(-11.5022888f, 70.63667f, -261.72052f, 0f, 88.94999f, 0f),
                new SerializableTransform(8.11329651f, 70.63667f, -249.7733f, 0f, 272.250061f, 0f),
                new SerializableTransform(5.92330933f, 65.88658f, -260.0689f, 0f, 178.500061f, 0f),
                new SerializableTransform(-9.233465f, 65.88658f, -251.471329f, 0f, 359.1f, 0f),
                new SerializableTransform(420.090576f, 71.5975f, -142.901291f, 0f, 0.8499718f, 0f),
                new SerializableTransform(465.664459f, 67.7518539f, -119.160088f, 0f, 265.3f, 0f),
                new SerializableTransform(382.011169f, 57.22876f, -240.3982f, 0f, 88.54994f, 0f),
                new SerializableTransform(613.8219f, 55.2565155f, -254.794357f, 0f, 112.850037f, 0f),
                new SerializableTransform(670.832153f, 55.73659f, -169.284378f, 0f, 180.950058f, 0f),
                new SerializableTransform(583.959534f, 55.238884f, -172.474655f, 0f, 272.750031f, 0f),
                new SerializableTransform(533.4562f, 55.5497131f, -173.006577f, 0f, 88.4001f, 0f),
                new SerializableTransform(206.92511f, 57.23698f, -294.8498f, 0f, 89.10002f, 0f),
                new SerializableTransform(189.179108f, 57.30183f, -250.772156f, 0f, 179.549911f, 0f),
                new SerializableTransform(185.191574f, 57.30183f, -265.599152f, 0f, 85.94989f, 0f),
                new SerializableTransform(176.814941f, 57.30183f, -271.1832f, 0f, 86.84988f, 0f),
                new SerializableTransform(-410.5612f, 40.891037f, -668.213135f, 0f, 34.0499573f, 0f),
                new SerializableTransform(-422.312866f, 40.891037f, -657.6499f, 0f, 34.94998f, 0f),
                new SerializableTransform(-412.7648f, 40.891037f, -669.8023f, 0f, 218.09993f, 0f),
                new SerializableTransform(-412.309082f, 45.6410446f, -670.1775f, 0f, 218.400146f, 0f),
                new SerializableTransform(-418.3301f, 45.6410446f, -666.79895f, 0f, 308.100159f, 0f),
                new SerializableTransform(-502.8874f, 40.9491577f, -93.64789f, 0f, 42.4999733f, 0f),
                new SerializableTransform(-502.523865f, 40.9491577f, -79.45072f, 0f, 222.350037f, 0f),
                new SerializableTransform(-484.608856f, 40.9402428f, -137.176758f, 0f, 347.600128f, 0f),
                new SerializableTransform(-482.23584f, 40.91754f, -149.0282f, 0f, 346.250122f, 0f),
                new SerializableTransform(-481.528778f, 40.9402466f, -154.310181f, 0f, 171.350159f, 0f),
                new SerializableTransform(-507.527344f, 41.4843445f, -266.21286f, 0f, 345.8f, 0f),
                new SerializableTransform(-506.384949f, 46.2349434f, -252.794846f, 0f, 255.350067f, 0f),
                new SerializableTransform(-513.4673f, 46.2349434f, -252.60997f, 0f, 254.750061f, 0f),
                new SerializableTransform(-520.7801f, 46.23494f, -254.459274f, 0f, 254.750061f, 0f),
                new SerializableTransform(-518.1406f, 41.0946922f, -274.926331f, 0f, 86.750145f, 0f),
                new SerializableTransform(-513.371948f, 41.4843445f, -253.286926f, 0f, 259.700134f, 0f),
                new SerializableTransform(-526.634033f, 40.98004f, 430.530273f, 0f, 345.450043f, 0f),
                new SerializableTransform(-542.970764f, 40.98004f, 405.267059f, 0f, 33.15008f, 0f),
                new SerializableTransform(-522.314758f, 42.6923676f, 351.44104f, 0f, 346.650146f, 0f),
                new SerializableTransform(-536.995056f, 42.6923676f, 346.5384f, 0f, 166.350113f, 0f),
                new SerializableTransform(-531.787354f, 42.6923676f, 348.352631f, 0f, 77.7001953f, 0f),
                new SerializableTransform(372.0398f, 41.7456474f, 184.827316f, 0f, 214.600189f, 0f),
                new SerializableTransform(314.761627f, 41.718914f, 99.99346f, 0f, 34.45018f, 0f),
                new SerializableTransform(186.7212f, 35.2399635f, -498.943634f, 0f, 217.90007f, 0f),
                new SerializableTransform(184.842773f, 35.2399635f, -490.844727f, 0f, 308.500061f, 0f),
                new SerializableTransform(186.420181f, 35.2399635f, -492.4004f, 0f, 129.100037f, 0f),
                new SerializableTransform(206.586655f, 35.1705627f, -504.037781f, 0f, 178.90007f, 0f),
                new SerializableTransform(466.251251f, 45.2722359f, -401.8267f, 0f, 274.950043f, 0f),
                new SerializableTransform(-139.82843f, 46.3012733f, 210.834854f, 0f, 134.650116f, 0f),
                new SerializableTransform(-187.632187f, 46.3038139f, 195.410187f, 0f, 134.249878f, 0f),
                new SerializableTransform(-225.656189f, 46.30116f, 229.42128f, 0f, 314.549866f, 0f),
                new SerializableTransform(-224.612335f, 46.30116f, 237.214615f, 0f, 134.099884f, 0f),
                new SerializableTransform(-205.013519f, 46.3040428f, 267.498474f, 0f, 315.925079f, 0f),
                new SerializableTransform(-246.430023f, 46.3011551f, 259.2519f, 0f, 137.249741f, 0f),
                new SerializableTransform(588.2777f, 35.1819649f, 279.46228f, 0f, 269.149963f, 0f),
                new SerializableTransform(586.707458f, 39.9319725f, 270.513641f, 0f, 0.0500241555f, 0f),
                new SerializableTransform(586.898743f, 39.9319725f, 287.373474f, 0f, 181.250122f, 0f),
                new SerializableTransform(688.4736f, 35.1764946f, 312.650238f, 0f, 175.100159f, 0f),
                new SerializableTransform(700.934143f, 39.9264946f, 302.0669f, 0f, 270.6502f, 0f),
                new SerializableTransform(684.0695f, 39.9264946f, 311.978455f, 0f, 91.40021f, 0f),
                new SerializableTransform(684.063f, 39.9264946f, 302.3407f, 0f, 90.6502f, 0f),
                new SerializableTransform(803.445f, 50.04394f, 515.1205f, 0f, 308.150146f, 0f),
                new SerializableTransform(802.002136f, 54.7939453f, 529.871033f, 0f, 217.400146f, 0f),
                new SerializableTransform(802.9041f, 54.75959f, 514.750061f, 0f, 310.550079f, 0f),
                new SerializableTransform(790.8298f, 54.7939453f, 516.738342f, 0f, 41.000164f, 0f),
                new SerializableTransform(-602.1001f, 40.8446732f, 247.16597f, 0f, 98.50005f, 0f),
                new SerializableTransform(-648.6162f, 40.90011f, 243.529846f, 0f, 1.74994516f, 0f),
                new SerializableTransform(-660.66394f, 40.90011f, 262.497375f, 0f, 264.699921f, 0f),
                new SerializableTransform(-665.3761f, 40.8595352f, 296.391418f, 0f, 182.94986f, 0f),
                new SerializableTransform(-655.8673f, 40.8521042f, 296.3904f, 0f, 183.099945f, 0f),
                new SerializableTransform(-97.37492f, 46.617733f, 646.7229f, 0f, 90.05f, 0f),
                new SerializableTransform(-62.9612846f, 46.617733f, 627.3119f, 0f, 93.7998047f, 0f),
                new SerializableTransform(-58.07756f, 46.617733f, 624.209961f, 0f, 40.0997734f, 0f),
                new SerializableTransform(60.5422859f, 40.907093f, 430.864746f, 0f, 106.799957f, 0f),
                new SerializableTransform(8.730333f, 40.88394f, 447.091339f, 0f, 105.824913f, 0f),
                new SerializableTransform(-90.71227f, 46.617733f, 649.608765f, 0f, 145.999908f, 0f),
                new SerializableTransform(48.3672638f, 40.88661f, 475.216156f, 0f, 283.725037f, 0f),
                new SerializableTransform(-78.24946f, 34.3677139f, 640.677063f, 0f, 125.749916f, 0f),
                new SerializableTransform(-70.5189362f, 34.3677139f, 627.6236f, 0f, 280.699951f, 0f),
                new SerializableTransform(99.10344f, 40.9936028f, 464.4927f, 0f, 105.375137f, 0f),
                new SerializableTransform(-46.1998672f, 34.3677139f, 618.179565f, 0f, 127.549957f, 0f),
                new SerializableTransform(-30.4742985f, 34.3677139f, 617.7121f, 0f, 257.899933f, 0f),
                new SerializableTransform(60.8913155f, 40.90628f, 430.435852f, 0f, 105.300117f, 0f),
                new SerializableTransform(0.122714f, 40.88394f, 442.549164f, 0f, 105.750305f, 0f),
                new SerializableTransform(-733.278564f, 47.2598648f, 462.001343f, 0f, 267.899963f, 0f),
                new SerializableTransform(-745.6896f, 47.82388f, 455.580444f, 0f, 85.49992f, 0f),
                new SerializableTransform(-733.2538f, 47.1943932f, 453.418182f, 0f, 275.249878f, 0f),
                new SerializableTransform(-219.196991f, 48.64708f, -806.9547f, 0f, 281.150055f, 0f),
                new SerializableTransform(-212.715f, 48.64708f, -814.1917f, 0f, 195.500015f, 0f),
                new SerializableTransform(-216.277176f, 48.64708f, -823.0809f, 0f, 284.599976f, 0f),
                new SerializableTransform(-210.211f, 48.64708f, -808.9972f, 0f, 106.849968f, 0f),
                new SerializableTransform(291.617859f, 38.38968f, -573.3151f, 0f, 271.3f, 0f),
                new SerializableTransform(189.369614f, 45.0764236f, -447.277283f, 0f, 31.12429f, 0f),
                new SerializableTransform(265.075653f, 42.2353f, 389.17807f, 0f, 177.650055f, 0f),
                new SerializableTransform(269.669922f, 42.2353f, 380.341553f, 0f, 268.8501f, 0f)
            }),
            new KeyValuePair<string, SerializableTransform[]>(MapScheduler.GulfOfAqaba, new SerializableTransform[62]
            {
                new SerializableTransform(-712.8696f, 36.70459f, -210.1968f, 270f, 2f, 0f),
                new SerializableTransform(-694.0713f, 36.70459f, -210.6987f, 270f, 92f, 0f),
                new SerializableTransform(-577.7798f, 37.50977f, -212.6816f, 270f, 0f, 0f),
                new SerializableTransform(-579.4556f, 37.50977f, -223.1621f, 270f, 182f, 0f),
                new SerializableTransform(-536.8232f, 53.57178f, 71.69824f, 270f, 90f, 0f),
                new SerializableTransform(-600.1055f, 53.72705f, 424.458f, 270f, 238f, 0f),
                new SerializableTransform(-432.7109f, 49.19336f, -28.54883f, 270f, 90f, 0f),
                new SerializableTransform(-458.9492f, 49.19336f, -18.25195f, 270f, 90f, 0f),
                new SerializableTransform(-468.1382f, 49.19336f, -86.24951f, 270f, 0f, 0f),
                new SerializableTransform(-438.6714f, 49.19385f, -90.03613f, 270f, 92f, 0f),
                new SerializableTransform(-438.4868f, 49.19385f, -96.64746f, 270f, 92f, 0f),
                new SerializableTransform(-438.3354f, 49.19336f, 12.32617f, 270f, 268f, 0f),
                new SerializableTransform(-425.6035f, 58.65918f, 159.1484f, 270f, 2f, 0f),
                new SerializableTransform(-422.8228f, 53.6748f, 149.5879f, 270f, 92f, 0f),
                new SerializableTransform(-426.624f, 53.65918f, 158.8276f, 270f, 2f, 0f),
                new SerializableTransform(-463.3091f, 58.68213f, 280.5845f, 270f, 0f, 0f),
                new SerializableTransform(-467.7466f, 58.65918f, 288.5288f, 270f, 2f, 0f),
                new SerializableTransform(-470.3179f, 63.65918f, 279.7285f, 270f, 270f, 0f),
                new SerializableTransform(-467.7896f, 63.65918f, 288.4824f, 270f, 2f, 0f),
                new SerializableTransform(-344.417f, 49.10938f, -163.8447f, 270f, 2f, 0f),
                new SerializableTransform(-287.5771f, 49.11035f, -170.5625f, 270f, 272f, 0f),
                new SerializableTransform(-284.124f, 49.13281f, -193.8491f, 270f, 94f, 0f),
                new SerializableTransform(-293.0879f, 49.58398f, -24.7417f, 270f, 178f, 0f),
                new SerializableTransform(-294.0464f, 49.58398f, -14.65088f, 270f, 0f, 0f),
                new SerializableTransform(-159.563f, 54.23438f, -197.3672f, 270f, 0f, 0f),
                new SerializableTransform(-245.2979f, 49.5874f, -82.8999f, 270f, 180f, 0f),
                new SerializableTransform(-242.9263f, 49.5874f, -73.04297f, 270f, 0f, 0f),
                new SerializableTransform(-132.9321f, 49.10059f, -25.6626f, 270f, 178f, 0f),
                new SerializableTransform(-204.5278f, 49.10205f, 61.25342f, 270f, 272f, 0f),
                new SerializableTransform(-215.6221f, 49.10645f, 95.44922f, 270f, 272f, 0f),
                new SerializableTransform(-248.7036f, 50.19238f, 225.0908f, 270f, 2f, 0f),
                new SerializableTransform(-159.4409f, 53.98779f, 591.8545f, 270f, 2f, 0f),
                new SerializableTransform(-161.9844f, 53.98779f, 575.2256f, 270f, 178f, 0f),
                new SerializableTransform(-231.7827f, 54.64355f, 570.041f, 270f, 180f, 0f),
                new SerializableTransform(-121.1885f, 30.45361f, -362.8164f, 270f, 178f, 0f),
                new SerializableTransform(-94.32129f, 49.10156f, -51.04053f, 270f, 7.999999f, 0f),
                new SerializableTransform(-56.03125f, 59.17578f, -38.66162f, 270f, 182f, 0f),
                new SerializableTransform(-92.50928f, 49.10596f, -12.7583f, 270f, 4f, 0f),
                new SerializableTransform(-81.52148f, 49.10742f, 57.07568f, 270f, 0f, 0f),
                new SerializableTransform(-100.9458f, 49.10693f, 95.8667f, 270f, 272f, 0f),
                new SerializableTransform(-69.03564f, 49.10742f, 90.67871f, 270f, 180f, 0f),
                new SerializableTransform(-20.13477f, 108.2378f, 727.436f, 270f, 270f, 0f),
                new SerializableTransform(62.29053f, 61.89844f, 281.3765f, 270f, 100f, 0f),
                new SerializableTransform(61.77686f, 61.87891f, 256.5625f, 270f, 102f, 0f),
                new SerializableTransform(197.7275f, 79.16895f, 840.4292f, 270f, 42f, 0f),
                new SerializableTransform(161.835f, 79.0918f, 840.166f, 270f, 272f, 0f),
                new SerializableTransform(185.6895f, 79.0918f, 862.3535f, 270f, 2f, 0f),
                new SerializableTransform(288.7466f, 53.40723f, 499.1182f, 270f, 74f, 0f),
                new SerializableTransform(325.5752f, 51.36133f, 385.2715f, 270f, 150f, 0f),
                new SerializableTransform(369.0918f, 50.69238f, 423.3989f, 270f, 356f, 0f),
                new SerializableTransform(488.3462f, 54.00684f, 19.62207f, 270f, 88f, 0f),
                new SerializableTransform(614.6968f, 58.18555f, 609.1064f, 270f, 84f, 0f),
                new SerializableTransform(576.9849f, 58.23877f, 594.8682f, 270f, 352f, 0f),
                new SerializableTransform(553.2617f, 58.23877f, 598.7881f, 270f, 170f, 0f),
                new SerializableTransform(760.7163f, 70.10547f, -88.41357f, 270f, 274f, 0f),
                new SerializableTransform(760.3477f, 75.10547f, -88.74219f, 270f, 278f, 0f),
                new SerializableTransform(760.5259f, 80.10547f, -88.76563f, 270f, 276f, 0f),
                new SerializableTransform(769.166f, 70.10547f, -87.80225f, 270f, 288f, 0f),
                new SerializableTransform(768.7002f, 75.10547f, -87.62695f, 270f, 308f, 0f),
                new SerializableTransform(769.042f, 80.10938f, -84.94727f, 270f, 268f, 0f),
                new SerializableTransform(820.5498f, 70.18408f, 278.3696f, 270f, 2f, 0f),
                new SerializableTransform(810.2891f, 70.18408f, 277.5303f, 270f, 92f, 0f)
            })
        };
    #endregion
}

public sealed class InsurgencyTicketProvider : BaseTicketProvider
{
    public override void GetDisplayInfo(ulong team, out string message, out string tickets, out string bleed)
    {
        int b = GetTeamBleed(team);
        bleed = b == 0 ? string.Empty : b.ToString(Data.Locale);
        if (Data.Is(out Insurgency ins))
        {
            if (ins.DefendingTeam == team)
            {
                tickets = ins.CachesLeft + " left";
                message = "DEFEND THE WEAPONS CACHES";
                return;
            }
            else if (ins.AttackingTeam == team)
            {
                tickets = string.Empty;
                if (ins.DiscoveredCaches.Count == 0)
                    message = "FIND THE WEAPONS CACHES\n(kill enemies for intel)";
                else
                    message = "DESTROY THE WEAPONS CACHES";
                return;
            }
        }
        tickets = message = string.Empty;
    }
    public override int GetTeamBleed(ulong team)
    {
        return 0;
    }
    public override void OnGameStarting(bool isOnLoaded)
    {
        if (!Data.Is(out Insurgency ins)) return;
        int attack = Gamemode.Config.Insurgency.AttackStartingTickets;
        int defence = ins.CachesLeft;

        if (ins.AttackingTeam == 1)
        {
            Manager.Team1Tickets = attack;
        }
        else if (ins.AttackingTeam == 2)
        {
            Manager.Team2Tickets = attack;
        }
    }
    public override void OnTicketsChanged(ulong team, int oldValue, int newValue, ref bool updateUI)
    {
        if (Data.Is(out Insurgency ins) && ins.DefendingTeam == team)
            throw new InvalidOperationException("Tried to change tickets of defending team during Insurgency.");
        if (oldValue > 0 && newValue <= 0)
            Data.Gamemode.DeclareWin(TeamManager.Other(team));
    }
    public override void Tick()
    {
        if (!Data.Gamemode.EveryXSeconds(20f) || !Data.Is(out Insurgency ins)) return;
        for (int i = 0; i < ins.ActiveCaches.Count; i++)
        {
            Insurgency.CacheData cache = ins.ActiveCaches[i];
            if (cache.IsActive && !cache.IsDestroyed)
            {
                for (int j = 0; j < cache.Cache.NearbyDefenders.Count; j++)
                    Points.AwardXP(cache.Cache.NearbyDefenders[j], Points.XPConfig.FlagDefendXP, Localization.Translate("xp_flag_defend", cache.Cache.NearbyDefenders[j]));
            }
        }
    }
}

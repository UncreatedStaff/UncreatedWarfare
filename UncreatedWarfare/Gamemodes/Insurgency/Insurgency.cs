#if DEBUG
using System;
#endif
using SDG.NetTransport;
using SDG.Unturned;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Warfare.Actions;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Events.Vehicles;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Revives;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Stats;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Tickets;
using Uncreated.Warfare.Traits;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using Cache = Uncreated.Warfare.Components.Cache;
using Random = UnityEngine.Random;
using XPReward = Uncreated.Warfare.Levels.XPReward;

namespace Uncreated.Warfare.Gamemodes.Insurgency;

public class Insurgency :
    TicketGamemode<InsurgencyTicketProvider>,
    IFOBs,
    IVehicles,
    IKitRequests,
    IRevives,
    ISquads,
    IImplementsLeaderboard<InsurgencyPlayerStats, InsurgencyTracker>,
    IStagingPhase,
    IAttackDefense,
    IGameStats,
    ITraits
{
    private VehicleSpawner _vehicleSpawner;
    private VehicleBay _vehicleBay;
    private FOBManager _fobManager;
    private KitManager _kitManager;
    private ReviveManager _reviveManager;
    private SquadManager _squadManager;
    private StructureSaver _structureSaver;
    private InsurgencyTracker _gameStats;
    private InsurgencyLeaderboard? _endScreen;
    private TraitManager _traitManager;
    private ActionManager _actionManager;
    private ulong _attackTeam;
    private ulong _defendTeam;
    public int IntelligencePoints;
    public List<CacheData> Caches;
    private HashSet<CacheLocation> _seenCaches;
    private bool _isScreenUp;
    public override string DisplayName => "Insurgency";
    public override GamemodeType GamemodeType => GamemodeType.Invasion;
    public override bool EnableAMC => true;
    public override bool ShowOFPUI => true;
    public override bool ShowXPUI => true;
    public override bool TransmitMicWhileNotActive => true;
    public override bool UseTeamSelector => true;
    public override bool UseWhitelist => true;
    public override bool AllowCosmetics => UCWarfare.Config.AllowCosmetics;
    public VehicleSpawner VehicleSpawner => _vehicleSpawner;
    public VehicleBay VehicleBay => _vehicleBay;
    public FOBManager FOBManager => _fobManager;
    public KitManager KitManager => _kitManager;
    public ReviveManager ReviveManager => _reviveManager;
    public SquadManager SquadManager => _squadManager;
    public StructureSaver StructureSaver => _structureSaver;
    public TraitManager TraitManager => _traitManager;
    public ActionManager ActionManager => _actionManager;
    public ulong AttackingTeam => _attackTeam;
    public ulong DefendingTeam => _defendTeam;
    public int CachesLeft { get; private set; }
    public int CachesDestroyed { get; private set; }
    public List<CacheData> ActiveCaches => Caches.Where(c => c.IsActive && !c.IsDestroyed).ToList();
    public List<CacheData> DiscoveredCaches => Caches.Where(c => c.IsActive && !c.IsDestroyed && c.IsDestroyed).ToList();
    public int ActiveCachesCount => Caches.Count(c => c.IsActive && !c.IsDestroyed);
    public bool IsScreenUp => _isScreenUp;
    public ILeaderboard<InsurgencyPlayerStats, InsurgencyTracker>? Leaderboard => _endScreen;
    ILeaderboard? IImplementsLeaderboard.Leaderboard => _endScreen;
    InsurgencyTracker IImplementsLeaderboard<InsurgencyPlayerStats, InsurgencyTracker>.WarstatsTracker { get => _gameStats; set => _gameStats = value; }
    IStatTracker IGameStats.GameStats => _gameStats;
    public CacheLocations Locations { get; } = new CacheLocations();
    public Insurgency() : base("Insurgency", 0.25F) { }
    protected override Task PreInit(CancellationToken token)
    {
        token.CombineIfNeeded(UnloadToken);
        AddSingletonRequirement(ref _squadManager);
        AddSingletonRequirement(ref _kitManager);
        AddSingletonRequirement(ref _vehicleSpawner);
        AddSingletonRequirement(ref _reviveManager);
        AddSingletonRequirement(ref _vehicleBay);
        AddSingletonRequirement(ref _fobManager);
        AddSingletonRequirement(ref _structureSaver);
        AddSingletonRequirement(ref _traitManager);
        if (UCWarfare.Config.EnableActionMenu)
            AddSingletonRequirement(ref _actionManager);
        return base.PreInit(token);
    }
    protected override Task PostInit(CancellationToken token)
    {
        token.CombineIfNeeded(UnloadToken);
        Locations.Reload();

        return base.PostInit(token);
    }
    protected override Task PreGameStarting(bool isOnLoad, CancellationToken token)
    {
        token.CombineIfNeeded(UnloadToken);
        _gameStats.Reset();

        _attackTeam = (ulong)Random.Range(1, 3);
        if (_attackTeam == 1)
            _defendTeam = 2;
        else if (_attackTeam == 2)
            _defendTeam = 1;

        CachesDestroyed = 0;
        Caches = new List<CacheData>();
        _seenCaches = new HashSet<CacheLocation>();

        CachesLeft = Random.Range(Config.InsurgencyMinStartingCaches, Config.InsurgencyMaxStartingCaches + 1);
        for (int i = 0; i < CachesLeft; i++)
            Caches.Add(new CacheData());
        return base.PreGameStarting(isOnLoad, token);
    }
    protected override Task PostGameStarting(bool isOnLoad, CancellationToken token)
    {
        token.CombineIfNeeded(UnloadToken);
        RallyManager.WipeAllRallies();

        TrySpawnNewCache();
        if (_attackTeam == 1)
            SpawnBlockerOnT1();
        else
            SpawnBlockerOnT2();
        StartStagingPhase(Config.InsurgencyStagingTime);
        return base.PostGameStarting(isOnLoad, token);
    }
    public override Task DeclareWin(ulong winner, CancellationToken token)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        StartCoroutine(EndGameCoroutine(winner));
        return base.DeclareWin(winner, token);
    }
    private IEnumerator<WaitForSeconds> TryDiscoverFirstCache()
    {
        yield return new WaitForSeconds(Config.InsurgencyFirstCacheSpawnTime);

#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        List<CacheData> activeCaches = ActiveCaches;
        if (activeCaches.Count > 0 && !activeCaches[0].IsDiscovered)
        {
            IntelligencePoints = 0;
            OnCacheDiscovered(activeCaches[0].Cache, null);
        }
    }
    private IEnumerator<WaitForSeconds> EndGameCoroutine(ulong winner)
    {
        yield return new WaitForSeconds(Config.GeneralLeaderboardDelay);
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ReplaceBarricadesAndStructures();
        Commands.ClearCommand.WipeVehicles();
        Commands.ClearCommand.ClearItems();

        _endScreen = UCWarfare.I.gameObject.AddComponent<InsurgencyLeaderboard>();
        _endScreen.OnLeaderboardExpired += OnShouldStartNewGame;
        _endScreen.SetShutdownConfig(ShouldShutdownAfterGame, ShutdownMessage);
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
            _endScreen.OnLeaderboardExpired -= OnShouldStartNewGame;
            Destroy(_endScreen);
        }
        _isScreenUp = false;
        UCWarfare.RunTask(EndGame, UCWarfare.UnloadCancel, ctx: "Starting next gamemode.");
    }
    protected override void EventLoopAction()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (EveryXSeconds(Config.AASFlagTickSeconds))
            CheckMainCampZones();
        base.EventLoopAction();
    }
    public override void OnPlayerDeath(PlayerDied e)
    {
        if (e.Killer is not null && !e.WasTeamkill && e.DeadTeam == _defendTeam)
        {
            AddIntelligencePoints(1, e.Killer);
            if (e.Killer!.Player.TryGetPlayerData(out UCPlayerData c) && c.Stats is InsurgencyPlayerStats s)
                s._intelligencePointsCollected++;
            ((IImplementsLeaderboard<InsurgencyPlayerStats, InsurgencyTracker>)this).WarstatsTracker.intelligenceGathered++;
        }
        base.OnPlayerDeath(e);
    }
    protected override void InitUI(UCPlayer player)
    {
        InsurgencyUI.SendCacheList(player);
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
    public bool AddIntelligencePoints(int points, UCPlayer? instigator)
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
                if (IntelligencePoints >= Config.InsurgencyIntelPointsToDiscovery)
                {
                    IntelligencePoints = 0;
                    OnCacheDiscovered(first.Cache, instigator);
                    return true;
                }
                return false;
            }
            if (first.IsDiscovered && CachesLeft != 1)
            {
                IntelligencePoints += points;
                if (IntelligencePoints >= Config.InsurgencyIntelPointsToSpawn)
                {
                    IntelligencePoints = 0;
                    TrySpawnNewCache(true);
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
            if (IntelligencePoints >= Config.InsurgencyIntelPointsToDiscovery)
            {
                IntelligencePoints = 0;
                OnCacheDiscovered(last.Cache, instigator);
                return true;
            }
            return false;
        }
        return false;
    }
    public void OnCacheDiscovered(Cache cache, UCPlayer? instigator)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        cache.IsDiscovered = true;

        if (instigator != null && instigator.Player.TryGetPlayerData(out UCPlayerData data) && data.Stats is InsurgencyPlayerStats ips)
            ++ips._cachesDiscovered;

        foreach (UCPlayer player in PlayerManager.OnlinePlayers)
        {
            if (player.GetTeam() == AttackingTeam)
                ToastMessage.QueueMessage(player, new ToastMessage(Localization.Translate(T.CacheDiscoveredAttack, player, cache.ClosestLocation), ToastMessageSeverity.Big));
            else if (player.GetTeam() == DefendingTeam)
                ToastMessage.QueueMessage(player, new ToastMessage(Localization.Translate(T.CacheDiscoveredDefense, player), ToastMessageSeverity.Big));
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

        FOBManager.UpdateFOBListForTeam(DefendingTeam, cache);

        cache.SpawnAttackIcon();

        for (int i = 0; i < Singletons.Count; ++i)
        {
            if (Singletons[i] is ICacheDiscoveredListener f)
                f.OnCacheDiscovered(cache);
        }
        TicketManager.UpdateUI(AttackingTeam);
    }
    public void TrySpawnNewCache(bool message = false)
    {

#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        const float sqrRadius = 300 * 300;
        CacheLocation[] viableSpawns = Locations.Locations
            .Where(c1 => c1 != null && !c1.IsDisabled && !_seenCaches.Contains(c1) &&
                _seenCaches
                    .All(c => (c1.Position - c.Position).sqrMagnitude > sqrRadius
                    )
                )
            .ToArray();

        if (viableSpawns.Length == 0)
        {
            L.LogWarning("[INSURGENCY] No viable cache spawns.");
            return;
        }
        CacheLocation location = viableSpawns[Random.Range(0, viableSpawns.Length)];

        if (!Config.BarricadeInsurgencyCache.ValidReference(out ItemBarricadeAsset asset))
        {
            L.LogWarning("[INSURGENCY] Invalid barricade GUID for Insurgency Cache.");
            return;
        }
        Barricade barricade = new Barricade(asset);
        Transform barricadeTransform = BarricadeManager.dropNonPlantedBarricade(barricade, location.Position, location.GetBarricadeAngle(), 0, TeamManager.GetGroupID(DefendingTeam));
        BarricadeDrop foundationDrop = BarricadeManager.FindBarricadeByRootTransform(barricadeTransform);
        if (foundationDrop == null)
        {
            L.LogWarning("[INSURGENCY] Unable to spawn cache barricade.");
            return;
        }

        Cache cache = FOBManager.RegisterNewCache(foundationDrop, DefendingTeam, location);
        CacheData currentCache = Caches[CachesDestroyed];


        if (!currentCache.IsActive)
        {
            currentCache.Activate(cache);
            InsurgencyUI.ReplicateCacheUpdate(currentCache);
        }
        else if (CachesDestroyed < Caches.Count - 1)
        {
            CacheData nextCache = Caches[CachesDestroyed + 1]; // todo: ERROR HERE
            nextCache.Activate(cache);
            InsurgencyUI.ReplicateCacheUpdate(nextCache);
        }

        _seenCaches.Add(location);

        if (message)
        {
            foreach (LanguageSet set in LanguageSet.OnTeam(DefendingTeam))
            {
                ToastMessage msg = new ToastMessage(T.CacheSpawnedDefense.Translate(set.Language), ToastMessageSeverity.Big);
                while (set.MoveNext())
                    ToastMessage.QueueMessage(set.Next, msg);
            }
        }

        SpawnCacheItems(cache);
    }
    private void SpawnCacheItems(Cache cache)
    {
        _ = cache;
#if false
        try
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
            Vector3 point = cache.Structure.model.TransformPoint(new Vector3(0, 2, 0));

            for (int i = 0; i < 15; i++)
                ItemManager.dropItem(new Item(build.id, true), point, false, true, false);

            foreach (KeyValuePair<ushort, int> entry in Config.Insurgency.CacheItems)
            {
                for (int i = 0; i < entry.Value; i++)
                    ItemManager.dropItem(new Item(entry.Key, true), point, false, true, true);
            }
        }
        catch(Exception ex)
        {
            L.LogError(ex.ToString());
        }
#endif
    }
    private IEnumerator<WaitForSeconds> WaitToSpawnNewCache()
    {
        L.Log("CACHE: New cache spawning in 60 seconds");
        yield return new WaitForSeconds(60);
        TrySpawnNewCache(true);
    }
    public void OnCacheDestroyed(Cache cache, UCPlayer? destroyer)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (State != State.Active)
            return;
        CachesDestroyed++;
        CachesLeft--;

        QuestManager.OnObjectiveCaptured(Provider.clients
            .Where(x => x.GetTeam() == _attackTeam && (x.player.transform.position - cache.Position).sqrMagnitude < 10000f)
            .Select(x => x.playerID.steamID.m_SteamID).ToArray());

        ActionLog.Add(ActionLogType.TeamCapturedObjective, TeamManager.TranslateName(AttackingTeam, 0) + " DESTROYED CACHE");

        if (CachesLeft == 0)
        {
            UCWarfare.RunTask(DeclareWin, AttackingTeam, UCWarfare.UnloadCancel, ctx: "Caches destroyed, attackers (team " + AttackingTeam + ") win.");
        }
        else
        {
            foreach (UCPlayer player in PlayerManager.OnlinePlayers)
            {
                if (player.GetTeam() == AttackingTeam)
                    ToastMessage.QueueMessage(player, new ToastMessage(T.CacheDestroyedAttack.Translate(player), string.Empty, ToastMessageSeverity.Big));
                else if (player.GetTeam() == DefendingTeam)
                    ToastMessage.QueueMessage(player, new ToastMessage(T.CacheDestroyedDefense.Translate(player), string.Empty, ToastMessageSeverity.Big));
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
                Points.AwardXP(destroyer, XPReward.CacheDestroyed);
                StatsManager.ModifyStats(destroyer.Steam64, x => x.FlagsCaptured++, false);
                StatsManager.ModifyTeam(AttackingTeam, t => t.FlagsCaptured++, false);
                if (_gameStats != null)
                {
                    for (int i = 0; i < _gameStats.stats.Count; ++i)
                    {
                        if (_gameStats.stats[i].Steam64 == destroyer.Steam64)
                        {
                            _gameStats.stats[i]._cachesDestroyed++;
                            break;
                        }
                    }
                }
            }
            else if (destroyer.GetTeam() == DefendingTeam)
            {
                Points.AwardXP(destroyer, XPReward.FriendlyCacheDestroyed);
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
        if (AttackingTeam == 1)
            TicketManager.Team1Tickets += Config.InsurgencyTicketsCache;
        else if (AttackingTeam == 2)
            TicketManager.Team2Tickets += Config.InsurgencyTicketsCache;

        TicketManager.UpdateUI(1);
        TicketManager.UpdateUI(2);

        for (int i = 0; i < Singletons.Count; ++i)
        {
            if (Singletons[i] is ICacheDestroyedListener f)
                f.OnCacheDestroyed(cache);
        }
    }
    public override void ShowStagingUI(UCPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        CTFUI.StagingUI.SendToPlayer(player.Connection);
        if (player.GetTeam() == AttackingTeam)
            CTFUI.StagingUI.Top.SetText(player.Connection, T.PhaseBriefing.Translate(player));
        else if (player.GetTeam() == DefendingTeam)
            CTFUI.StagingUI.Top.SetText(player.Connection, T.PhasePreparation.Translate(player));
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
    internal override bool CanRefillAmmoAt(ItemBarricadeAsset barricade)
    {
        return base.CanRefillAmmoAt(barricade) || Config.BarricadeInsurgencyCache.MatchGuid(barricade.GUID);
    }
    protected override void SendWinUI(ulong winner)
    {
        WinToastUI.SendToAllPlayers();
        string img1 = TeamManager.Team1Faction.FlagImageURL;
        string img2 = TeamManager.Team2Faction.FlagImageURL;
        foreach (LanguageSet set in LanguageSet.All())
        {
            string t1Tickets;
            string t2Tickets;
            if (AttackingTeam == 1)
            {
                t1Tickets = T.WinUIValueTickets.Translate(set.Language, TicketManager.Team1Tickets);
                if (TicketManager.Team1Tickets <= 0)
                    t1Tickets = t1Tickets.Colorize("969696");
                t2Tickets = T.WinUIValueCaches.Translate(set.Language, CachesLeft);
                if (CachesLeft <= 0)
                    t2Tickets = t2Tickets.Colorize("969696");
            }
            else
            {
                t1Tickets = T.WinUIValueCaches.Translate(set.Language, CachesLeft);
                if (CachesLeft <= 0)
                    t1Tickets = t1Tickets.Colorize("969696");
                t2Tickets = T.WinUIValueTickets.Translate(set.Language, TicketManager.Team2Tickets);
                if (TicketManager.Team2Tickets <= 0)
                    t2Tickets = t2Tickets.Colorize("969696");
            }
            string header = T.WinUIHeaderWinner.Translate(set.Language, TeamManager.GetFactionSafe(winner)!);
            while (set.MoveNext())
            {
                if (!set.Next.IsOnline || set.Next.HasUIHidden) continue;
                ITransportConnection c = set.Next.Connection;
                WinToastUI.Team1Flag.SetImage(c, img1);
                WinToastUI.Team2Flag.SetImage(c, img2);
                WinToastUI.Team1Tickets.SetText(c, t1Tickets);
                WinToastUI.Team2Tickets.SetText(c, t2Tickets);
                WinToastUI.Header.SetText(c, header);
            }
        }
    }

    public class CacheData
    {
        public int Number => Cache != null ? Cache.Number : 0;
        public bool IsActive => Cache != null;
        public bool IsDestroyed => Cache != null && Cache.IsDestroyed;
        public bool IsDiscovered => Cache != null && Cache.IsDiscovered;
        public Cache Cache { get; private set; }
        public CacheLocation Location { get; internal set; }
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

public sealed class InsurgencyTicketProvider : BaseTicketProvider
{
    public override void GetDisplayInfo(ulong team, out string message, out string tickets, out string bleed)
    {
        int b = GetTeamBleed(team);
        bleed = b == 0 ? string.Empty : b.ToString(Data.LocalLocale);
        // todo translations
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
                tickets = "";
                if (ins.AttackingTeam == 1)
                    tickets = TicketManager.Singleton.Team1Tickets.ToString();
                else if (ins.AttackingTeam == 2)
                    tickets = TicketManager.Singleton.Team2Tickets.ToString();

                message = ins.DiscoveredCaches.Count == 0 ? "FIND THE WEAPONS CACHES\n(kill enemies for intel)" : "DESTROY THE WEAPONS CACHES";
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
        int attack = Gamemode.Config.InsurgencyAttackStartingTickets;

        switch (ins.AttackingTeam)
        {
            case 1:
                Manager.Team1Tickets = attack;
                break;
            case 2:
                Manager.Team2Tickets = attack;
                break;
        }
    }

    public override void OnPlayerDeath(PlayerDied e)
    {
        if (!Data.Is(out Insurgency ins)) return;
        if (e.DeadTeam != ins.AttackingTeam)
            return;
        if (e.DeadTeam == 1ul)
            --Manager.Team1Tickets;
        else if (e.DeadTeam == 2ul)
            --Manager.Team2Tickets;
    }

    public override void OnTicketsChanged(ulong team, int oldValue, int newValue, ref bool updateUI)
    {
        if (Data.Is(out Insurgency ins) && ins.DefendingTeam == team)
        {
            L.LogWarning("Tried to change tickets of defending team during Insurgency.");
            return;
        }
        if (oldValue > 0 && newValue <= 0)
            UCWarfare.RunTask(Data.Gamemode.DeclareWin, TeamManager.Other(team), default, ctx: "Lose game, attacker's tickets reached 0.");
    }
    public override void Tick()
    {
        if (Data.Gamemode == null || !Data.Gamemode.EveryXSeconds(20f) || !Data.Is(out Insurgency ins)) return;
        List<Insurgency.CacheData> caches = ins.ActiveCaches;
        for (int i = 0; i < caches.Count; i++)
        {
            Insurgency.CacheData cache = caches[i];
            if (cache.IsActive && !cache.IsDestroyed)
            {
                for (int j = 0; j < cache.Cache.NearbyDefenders.Count; j++)
                    Points.AwardXP(cache.Cache.NearbyDefenders[j], XPReward.DefendingFlag);
            }
        }
    }

    protected override void OnVehicleDestroyed(VehicleDestroyed e)
    {
        if (!Data.Is(out IAttackDefense ins)) return;
        if (e.VehicleData is not null && e.Team == ins.AttackingTeam)
        {
            if (e.Team == 1)
                TicketManager.Singleton.Team1Tickets -= e.VehicleData.TicketCost;
            else if (e.Team == 2)
                TicketManager.Singleton.Team2Tickets -= e.VehicleData.TicketCost;
        }
    }
}

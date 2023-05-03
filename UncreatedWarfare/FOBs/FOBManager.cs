using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using Uncreated.Framework;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Barricades;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.FOBs.UI;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Insurgency;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Maps;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using Cache = Uncreated.Warfare.Components.Cache;
using Object = UnityEngine.Object;
using XPReward = Uncreated.Warfare.Levels.XPReward;

namespace Uncreated.Warfare.FOBs;
[SingletonDependency(typeof(Whitelister))]
public class FOBManager : BaseSingleton, ILevelStartListener, IGameStartListener, IPlayerDisconnectListener, IGameTickListener, IJoinedTeamListener, IUIListener
{
    private static readonly List<InteractableVehicle> WorkingNearbyVehicles = new List<InteractableVehicle>(16);

    private const float InsideFOBRangeSqr = 2f * 2f;
    internal static bool IgnorePlacingBarricade;
    internal static bool IgnorePlacingStructure;

    private List<IFOBItem> _floatingItems;
    private List<IFOB> _fobs;
    private static FOBManager _singleton;
    private static readonly FOBConfig ConfigFile = new FOBConfig();
    public static readonly FOBListUI ListUI = new FOBListUI();
    public static readonly NearbyResourceUI ResourceUI = new NearbyResourceUI();

    public static FOBConfigData Config => ConfigFile.Data;
    public static bool Loaded => _singleton.IsLoaded();
    public IReadOnlyList<IFOBItem> FloatingItems { get; private set; }
    public IReadOnlyList<IFOB> FOBs { get; private set; }

    public override void Load()
    {
        EventDispatcher.GroupChanged += OnGroupChanged;
        EventDispatcher.BarricadePlaced += OnBarricadePlaced;
        EventDispatcher.BarricadeDestroyed += OnBarricadeDestroyed;
        EventDispatcher.BarricadePlaceRequested += OnRequestedBarricadePlace;
        _floatingItems = new List<IFOBItem>(64);
        _fobs = new List<IFOB>(24);
        FloatingItems = _floatingItems.AsReadOnly();
        FOBs = _fobs.AsReadOnly();
        for (int i = 0; i < Config.Buildables.Count; ++i)
        {
            BuildableData data = Config.Buildables[i];
            if (data.DontAutoWhitelist) continue;
            if (data.Foundation.ValidReference(out Guid guid))
                Whitelister.AddItem(guid);
            if (data.Emplacement != null)
            {
                if (data.Emplacement.Ammo.ValidReference(out guid))
                    Whitelister.AddItem(guid);
            }
        }
        for (int i = 1; i <= 2; ++i)
        {
            FactionInfo f = TeamManager.GetFaction((ulong)i);
            if (f.Ammo.ValidReference(out Guid guid))
                Whitelister.AddItem(guid);
            if (f.Build.ValidReference(out guid))
                Whitelister.AddItem(guid);
            if (f.FOBRadio.ValidReference(out guid))
                Whitelister.AddItem(guid);
            if (f.RallyPoint.ValidReference(out guid))
                Whitelister.AddItem(guid);
        }
        _singleton = this;
    }
    public override void Unload()
    {
        EventDispatcher.BarricadePlaceRequested -= OnRequestedBarricadePlace;
        EventDispatcher.BarricadeDestroyed -= OnBarricadeDestroyed;
        EventDispatcher.BarricadePlaced -= OnBarricadePlaced;
        EventDispatcher.GroupChanged -= OnGroupChanged;
        for (int i = _fobs.Count - 1; i >= 0; --i)
        {
            try
            {
                if (_fobs[i] is MonoBehaviour { isActiveAndEnabled: true } b)
                    Object.Destroy(b);
                else if (_fobs[i] is IDisposable d)
                    d.Dispose();
            }
            catch (Exception ex)
            {
                L.LogError($"[FOBS] [{_fobs[i].Name}] Error removing.");
                L.LogError(ex);
            }
        }

        _fobs.Clear();
        _singleton = null!;
    }
    public void LoadRepairStations()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        foreach (BarricadeDrop barricade in UCBarricadeManager.NonPlantedBarricades)
        {
            BuildableData? data = FindBuildable(barricade.asset);
            if (data is { Type: BuildableType.RepairStation } && !barricade.model.TryGetComponent(out RepairStationComponent _))
                barricade.model.gameObject.AddComponent<RepairStationComponent>();
        }
    }
    public static byte[] GetRadioState(ulong team) => team switch
    {
        1 => Config.T1RadioState,
        2 => Config.T2RadioState,
        _ => Array.Empty<byte>()
    };
    public static void TriggerBuildEffect(Vector3 pos)
    {
        if (Gamemode.Config.EffectDig.ValidReference(out EffectAsset effect))
            F.TriggerEffectReliable(effect, EffectManager.MEDIUM, pos);
    }
    public static float GetBuildIncrementMultiplier(UCPlayer player)
    {
        float amount = player.KitClass == Class.CombatEngineer ? 2f : 1f;

        return amount * player.ShovelSpeedMultiplier;
    }
    public static BuildableData? FindBuildable(Asset asset, bool ammo = false)
    {
        IList<BuildableData>? data = Config?.Buildables;
        if (data == null)
            return null;
        Guid guid = asset.GUID;
        if (asset is ItemAsset)
        {
            for (int i = 0; i < data.Count; ++i)
            {
                BuildableData d = data[i];
                if (d.Foundation.MatchGuid(guid) ||
                    d.FullBuildable.MatchGuid(guid) ||
                    d.Emplacement != null && (
                        d.Emplacement.BaseBarricade.MatchGuid(guid) ||
                        ammo && d.Emplacement.Ammo.MatchGuid(guid))
                    )
                    return d;
            }
        }
        else if (asset is VehicleAsset)
        {
            for (int i = 0; i < data.Count; ++i)
            {
                BuildableData d = data[i];
                if (d.Emplacement != null && d.Emplacement.EmplacementVehicle.MatchGuid(guid))
                    return d;
            }
        }
        
        return null;
    }
    internal static Type GetComponentType(BuildableData buildable)
    {
        return buildable.Type switch
        {
            BuildableType.Radio => typeof(RadioComponent),
            BuildableType.RepairStation => typeof(RepairStationComponent),
            BuildableType.Bunker => typeof(BunkerComponent),
            _ => typeof(ShovelableComponent)
        };
    }
    internal static void EnsureDisposed(IFOBItem item)
    {
        if (item.FOB == null)
            _singleton?._floatingItems.RemoveAll(x => x.Equals(item));
        else
            item.FOB.EnsureDisposed(item);
    }
    internal static void EnsureDisposed(IFOB fob)
    {
        _singleton?._fobs.Remove(fob);
    }
    public static InteractableVehicle SpawnEmplacement(VehicleAsset asset, Vector3 position, Quaternion rotation, ulong owner, ulong group)
    {
        Vector3 oldRot = rotation.eulerAngles;
        Quaternion rot = Quaternion.Euler(new Vector3(oldRot.x + 90, oldRot.y, oldRot.z));
        byte[][] turrets = new byte[asset.turrets.Length][];
        for (int i = 0; i < turrets.Length; ++i)
        {
            if (Assets.find(EAssetType.ITEM, asset.turrets[i].itemID) is ItemAsset item)
                turrets[i] = item.getState(true);
            else
            {
                byte[] turret = new byte[18];
                turret[13] = 100;
                turret[14] = 100;
                turret[15] = 100;
                turret[16] = 100;
                turret[17] = 100;
                turrets[i] = turret;
            }
        }

        InteractableVehicle vehicle = VehicleManager.SpawnVehicleV3(asset, 0, 0, 0f,
            new Vector3(position.x, position.y + 1, position.z), rot, false, false, false, false,
            asset.fuel, asset.health, VehicleSpawner.MaxBatteryCharge, new CSteamID(owner), new CSteamID(group), true, turrets, byte.MaxValue);

        return vehicle;
    }

    private string GetOpenStandardFOBName(ulong team)
    {
        int maxId = 0;
        int lowestGap = int.MaxValue;
        int last = -1;
        foreach (FOB fob in FOBs.OfType<FOB>().Where(f => f.Team == team).OrderBy(x => x.Number))
        {
            int c = fob.Number;
            if (last != -1)
            {
                if (last + 1 != c && lowestGap > last + 1)
                    lowestGap = last + 1;
            }

            last = c;

            if (maxId < c)
                maxId = c;
        }

        return "FOB" + (lowestGap == int.MaxValue ? maxId + 1 : lowestGap);
    }
    public void DestroyAllFOBs(ulong instigator = 0ul)
    {
        for (int i = _fobs.Count - 1; i >= 0; --i)
            DeleteFOB(_fobs[i], instigator);
    }
    public IFOB? FindFob(IBuildable buildable)
    {
        for (int i = 0; i < _fobs.Count; ++i)
        {
            if (_fobs[i].ContainsBuildable(buildable))
                return _fobs[i];
        }

        return null;
    }
    public IFOB? FindFob(InteractableVehicle vehicle)
    {
        for (int i = 0; i < _fobs.Count; ++i)
        {
            if (_fobs[i].ContainsVehicle(vehicle))
                return _fobs[i];
        }

        return null;
    }
    public T? FindNearestFOB<T>(Vector3 pos, ulong team) where T : IRadiusFOB
    {
        return _fobs
            .OfType<T>()
            .Where(x => x.Team == team)
            .OrderBy(x => (pos - x.Position).sqrMagnitude)
            .FirstOrDefault(x => (pos - x.Position).sqrMagnitude < Mathf.Pow(x.Radius, 2f));
    }
    internal bool ValidateFloatingPlacement(BuildableData buildable, UCPlayer player, Vector3 point, IFOBItem? ignoreFoundation)
    {
        if (buildable.Type is BuildableType.RepairStation or BuildableType.AmmoCrate or BuildableType.Bunker
            or BuildableType.Radio)
        {
            player.SendChat(T.BuildNoRadio, buildable.Type == BuildableType.Bunker ? Config.FOBBuildPickupRadiusNoBunker : Config.FOBBuildPickupRadius);
            return false;
        }

        Kit? kit = player.ActiveKit?.Item;
        if (kit == null || !kit.ContainsItem(buildable.Foundation.Value.Guid, player.GetTeam()) || _floatingItems == null)
            return false;

        int limit = kit.CountItems(buildable.Foundation.Value.Guid);
        for (int i = 0; i < _floatingItems.Count; ++i)
        {
            IFOBItem item = _floatingItems[i];
            if (ignoreFoundation is not null && item.Equals(ignoreFoundation))
                continue;
            BuildableData? b = item.Buildable;
            if (b != null && b.Foundation == buildable.Foundation)
            {
                --limit;
                if (limit <= 0)
                {
                    player.SendChat(T.RegionalBuildLimitReached, buildable.Limit, buildable);
                    return false;
                }
            }
        }

        return true;
    }
    internal IFOBItem? UpgradeFloatingItem(IFOBItem item, Transform newObj)
    {
        if (item is not MonoBehaviour itemMb)
        {
            for (int i = 0; i < _floatingItems.Count; ++i)
            {
                if (_floatingItems[i].Equals(item))
                {
                    if (_floatingItems[i] is IDisposable d)
                        d.Dispose();
                    _floatingItems[i] = (IFOBItem)Activator.CreateInstance(item.GetType());
                    return _floatingItems[i];
                }
            }

            return null;
        }

        for (int i = 0; i < _floatingItems.Count; ++i)
        {
            if (_floatingItems[i] is MonoBehaviour mb && mb == itemMb)
            {
                _floatingItems[i] = (IFOBItem)newObj.gameObject.AddComponent(item.GetType());
                Object.Destroy(itemMb);
                return _floatingItems[i];
            }
        }

        return null;
    }

    void IJoinedTeamListener.OnJoinTeam(UCPlayer player, ulong team) => SendFOBList(player);
    void ILevelStartListener.OnLevelReady()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        foreach (BuildableData b in Config.Buildables)
        {
            if (b.Foundation.ValidReference(out Guid guid) && !Whitelister.IsWhitelisted(guid, out _))
                Whitelister.AddItem(guid);

            if (b.Emplacement != null)
            {
                if (!Whitelister.IsWhitelisted(b.Emplacement.Ammo, out _))
                    Whitelister.AddItem(b.Emplacement.Ammo);
            }
        }

        LoadRepairStations();
    }
    void IGameStartListener.OnGameStarting(bool isOnLoad)
    {
        SendFOBListToTeam(1);
        SendFOBListToTeam(2);
    }
    void IPlayerDisconnectListener.OnPlayerDisconnecting(UCPlayer player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        for (int i = 0; i < FOBs.Count; i++)
        {
            IFOB f = FOBs[i];
            if (f is IPlayerDisconnectListener dl)
                dl.OnPlayerDisconnecting(player);
        }
    }
    public FOB? CreateStandardFob(BarricadeDrop radio)
    {
        ulong team = radio.GetServersideData().group.GetTeam();
        if (team is not 1ul and not 2ul)
            return null;
        GameObject fobObj = new GameObject("FOB_" + radio.instanceID);
        fobObj.transform.SetPositionAndRotation(radio.model.position, radio.model.rotation);
        FOB fob = fobObj.AddComponent<FOB>();
        fob.Radio = radio.model.gameObject.AddComponent<RadioComponent>();
        fob.Name = GetOpenStandardFOBName(team);

        _fobs.Add(fob);
        return fob;
    }
    private void OnRequestedBarricadePlace(PlaceBarricadeRequested e)
    {
        if (IgnorePlacingBarricade) return;
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif

        ulong team = e.GroupOwner.GetTeam();
        if (team is not 1ul and not 2ul || e.OriginalPlacer == null)
        {
            if (e.OriginalPlacer == null || !e.OriginalPlacer.OnDuty())
                e.Break();
            return;
        }

        bool isRadio = false;
        // radio, check team of radio
        if (Gamemode.Config.FOBRadios.Value.Any(r => r.MatchGuid(e.Barricade.asset.GUID)))
        {
            FactionInfo? info = TeamManager.GetFactionSafe(e.GroupOwner.GetTeam());
            if (!(e.OriginalPlacer != null && e.OriginalPlacer.OnDuty()) && (info == null || !info.FOBRadio.MatchGuid(e.Barricade.asset.GUID)))
            {
                if (e.OriginalPlacer != null)
                    e.OriginalPlacer.SendChat(T.ProhibitedPlacement, e.Asset);
                
                e.Break();
                return;
            }
            isRadio = true;
        }

        BuildableData? buildable = FindBuildable(e.Asset, false);
        if (buildable == null && !isRadio)
            return;

        FOB? fob = isRadio ? null : FindNearestFOB<FOB>(e.Position, team);
        // floating buildable
        if (fob == null && !isRadio) 
        {
            if (e.OriginalPlacer == null || !ValidateFloatingPlacement(buildable!, e.OriginalPlacer, e.Position, null))
                e.Break();

            return;
        }
        if (isRadio || buildable!.Type == BuildableType.Bunker)
        {
            if (Config.RestrictFOBPlacement)
            {
                // underwater
                if (SDG.Framework.Water.WaterUtility.isPointUnderwater(e.Position))
                {
                    e.OriginalPlacer?.SendChat(T.BuildFOBUnderwater);
                    e.Break();
                    return;
                }

                // on a tower, windmill, etc
                if (e.Position.y > F.GetTerrainHeightAt2DPoint(e.Position.x, e.Position.z) + Config.FOBMaxHeightAboveTerrain)
                {
                    e.OriginalPlacer?.SendChat(T.BuildFOBTooHigh, Config.FOBMaxHeightAboveTerrain);
                    e.Break();
                    return;
                }
            }

            // near main
            if (Data.Gamemode is ITeams && TeamManager.IsInAnyMainOrAMCOrLobby(e.Position))
            {
                e.OriginalPlacer?.SendChat(T.BuildFOBTooCloseToMain);
                e.Break();
                return;
            }

            float fobRad = Config.FOBBuildPickupRadiusNoBunker;
            fobRad *= fobRad;
            if (!isRadio)
            {
                // not in inner fob radius
                if (fob == null || (fob.transform.position - e.Position).sqrMagnitude > fobRad)
                {
                    // no radio nearby, bunker must be within 30m of radio
                    e.OriginalPlacer?.SendChat(T.BuildNoRadio, 30);
                    e.Break();
                    return;
                }
            }
            else
            {
                // check for logis
                VehicleBay? bay = VehicleBay.GetSingletonQuick();
                bool found = false;
                if (bay != null)
                {
                    try
                    {
                        VehicleManager.getVehiclesInRadius(e.Position, fobRad, WorkingNearbyVehicles);
                        for (int i = 0; i < WorkingNearbyVehicles.Count; ++i)
                        {
                            InteractableVehicle veh = WorkingNearbyVehicles[i];
                            if (veh.lockedGroup.m_SteamID != e.GroupOwner)
                                continue;
                            VehicleData? data = bay.GetDataSync(veh.asset.GUID);
                            if (data != null && VehicleData.IsLogistics(data.Type))
                            {
                                found = true;
                                break;
                            }
                        }
                    }
                    finally
                    {
                        WorkingNearbyVehicles.Clear();
                    }
                }
                if (!found)
                {
                    e.OriginalPlacer?.SendChat(T.BuildNoLogisticsVehicle);
                    e.Break();
                    return;
                }

                // check nearby fobs and fob count
                int c = 0;
                float rad = Config.FOBBuildPickupRadius * 2;
                float sqrRad = rad * rad;
                for (int i = 0; i < _fobs.Count; ++i)
                {
                    IFOB f = _fobs[i];
                    if (f.Team == team)
                    {
                        ++c;
                        if (c >= Config.FobLimit)
                        {
                            e.OriginalPlacer?.SendChat(T.BuildMaxFOBsHit);
                            e.Break();
                            return;
                        }
                    }
                    if (f is FOB fob2 && (fob2.transform.position - e.Position).sqrMagnitude < rad)
                    {
                        e.OriginalPlacer?.SendChat(T.BuildFOBTooClose, fob2, (fob2.transform.position - e.Position).magnitude, sqrRad);
                        e.Break();
                        return;
                    }
                }

                return;
            }
        }
        if (e.OriginalPlacer == null || !fob!.ValidatePlacement(buildable!, e.OriginalPlacer, null))
            e.Break();
    }
    private void OnBarricadePlaced(BarricadePlaced e)
    {
        if (IgnorePlacingBarricade) return;
        L.LogDebug("[FOBS] Placed barricade: " + (e.Owner?.ToString() ?? "null") + ".");
        Guid guid = e.ServersideData.barricade.asset.GUID;
        bool isRadio = Gamemode.Config.FOBRadios.Value.Any(r => r.MatchGuid(guid));
        ulong team = e.Barricade.GetServersideData().group.GetTeam();
        if (team is not 1 and not 2)
            return;
        FOB? fob;
        BarricadeDrop drop = e.Barricade;
        if (isRadio)
        {
            fob = CreateStandardFob(drop);
            if (fob == null)
                return;
            if (e.Owner != null)
            {
                if (Gamemode.Config.BarricadeFOBBunkerBase.ValidReference(out ItemBarricadeAsset fobBase))
                    ItemManager.dropItem(new Item(fobBase.id, true), e.Owner.Position, true, true, true);
                if (Gamemode.Config.BarricadeAmmoCrateBase.ValidReference(out ItemBarricadeAsset ammoBase))
                    ItemManager.dropItem(new Item(ammoBase.id, true), e.Owner.Position, true, true, true);
                QuestManager.OnFOBBuilt(e.Owner, fob);
                Tips.TryGiveTip(e.Owner, 3, T.TipPlaceBunker);
            }
            SendFOBListToTeam(fob.Team);
            return;
        }

        BuildableData? buildable = Config.Buildables.Find(b => b.Foundation.MatchGuid(guid));
        if (buildable == null)
            return;

        fob = FindNearestFOB<FOB>(e.Barricade.model.position, team);
        IFOBItem item = (IFOBItem)e.Barricade.model.gameObject.AddComponent(GetComponentType(buildable));
        if (fob == null)
        {
            _floatingItems.Add(item);
            L.LogDebug($"[FOBS] [FLOATING] Registered item: {buildable}.");
            return;
        }

        fob.RegisterItem(item);
    }
    void IGameTickListener.Tick()
    {
        for (int i = 0; i < _fobs.Count; ++i)
        {
            if (_fobs[i] is IGameTickListener ticker)
                ticker.Tick();
        }
    }
    private void OnBarricadeDestroyed(BarricadeDestroyed e)
    {
        L.LogDebug("[FOBS] barricade destroyed: " + e.Barricade.asset.itemName + ".");
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (e.Transform.TryGetComponent(out RadioComponent radio) && radio.FOB != null)
        {
            ulong team = e.ServersideData.group.GetTeam();
            FactionInfo? info = TeamManager.GetFactionSafe(team);
            if (info is not null)
            {
                if (info.FOBRadio.MatchGuid(e.ServersideData.barricade.asset.GUID))
                {
                    if (radio.IsSalvaged)
                        radio.FOB.Destroy();
                    else
                        radio.FOB.UpdateRadioState(RadioComponent.RadioState.Bleeding);
                }
                else if (Gamemode.Config.BarricadeFOBRadioDamaged.MatchGuid(e.ServersideData.barricade.asset.GUID))
                {
                    if (radio.State == RadioComponent.RadioState.Bleeding)
                        radio.FOB.Destroy();
                }
                else return;

                SendFOBListToTeam(team);
            }
        }
        else if (e.Transform.TryGetComponent(out Cache.CacheComponent c))
        {
            DeleteFOB(c.Cache);
        }
    }

    public static bool IsPointInFOB(Vector3 point, out IFOB fob)
    {
        _singleton.AssertLoaded();
        for (int i = 0; i < _singleton._fobs.Count; ++i)
        {
            if ((_singleton._fobs[i].Position - point).sqrMagnitude <= InsideFOBRangeSqr)
            {
                fob = _singleton._fobs[i];
                return true;
            }
        }

        fob = null!;
        return false;
    }
    public static bool IsOnFOB(UCPlayer player, out IFOB fob)
    {
        ThreadUtil.assertIsGameThread();
        _singleton.AssertLoaded();

        for (int i = 0; i < _singleton._fobs.Count; ++i)
        {
            if (_singleton._fobs[i] is IRadiusFOB fob2 && fob2.IsPlayerOn(player))
            {
                fob = fob2;
                return true;
            }
        }

        fob = null!;
        return false;
    }
    public static SpecialFOB RegisterNewSpecialFOB(string name, Vector3 point, ulong team, string color, bool disappearAroundEnemies)
    {
        ThreadUtil.assertIsGameThread();
        _singleton.AssertLoaded();
        SpecialFOB f = new SpecialFOB(name, point, team, color, disappearAroundEnemies);
        _singleton._fobs.Insert(0, f);

        SendFOBListToTeam(team);
        return f;
    }
    public static Cache RegisterNewCache(BarricadeDrop drop)
    {
        ThreadUtil.assertIsGameThread();
        _singleton.AssertLoaded();
        if (Data.Is(out Insurgency insurgency))
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            Cache cache = new Cache(drop);
            int number;
            List<Insurgency.CacheData> caches = insurgency.ActiveCaches;
            if (caches.Count == 0)
                number = insurgency.CachesDestroyed + 1;
            else
                number = caches.Last().Number + 1;

            cache.Number = number;
            cache.Name = "CACHE" + number;

            _singleton._fobs.Add(cache);

            SendFOBListToTeam(cache.Team);

            if (Gamemode.Config.EffectMarkerCacheDefend.ValidReference(out Guid effectGuid))
                IconManager.AttachIcon(effectGuid, drop.model, insurgency.DefendingTeam, 3.25f);

            return cache;
        }
        
        return null!;
    }
    public void DeleteFOB(IFOB fob, ulong instigator = 0)
    {
        ThreadUtil.assertIsGameThread();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Deployment.CancelDeploymentsTo(fob);
        ulong team = fob.Team;

        UCPlayer? killer = UCPlayer.FromID(instigator);
        fob.Instigator = killer;

        _fobs.RemoveAll(x => x.Equals(fob));
        
        if (fob is FOB f2)
            f2.Destroy();
        if (fob is IDisposable d)
            d.Dispose();

        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        if (fob is Object b && b != null)
            Object.Destroy(b);

        ulong killerteam = 0;
        if (killer != null)
            killerteam = killer.GetTeam();

        if (Data.Gamemode is { State: State.Active } && fob is not ISalvageInfo { IsSalvaged: true })
        {
            // doesnt count destroying fobs after game ends
            if (killer != null && killerteam != 0 && killerteam != team && Data.Is(out IGameStats w) && w.GameStats is IFobsTracker ft)
            {
                if (team == 1)
                    ft.FOBsDestroyedT2++;
                else if (team == 2)
                    ft.FOBsDestroyedT1++;
            }
            if (killer != null)
            {
                if (killer.Player.TryGetPlayerData(out UCPlayerData c) && c.Stats is IFOBStats f)
                    f.AddFOBDestroyed();
                if (Data.Is(out ITickets tickets))
                {
                    if (team == 1) tickets.TicketManager.Team1Tickets += Config.TicketsFOBRadioLost;
                    else if (team == 2) tickets.TicketManager.Team2Tickets += Config.TicketsFOBRadioLost;
                }

                if (killer.GetTeam() == team)
                {
                    // todo find out why random barricade teamkills are still happening, if they are at all
                    Points.AwardXP(killer, XPReward.FriendlyRadioDestroyed);
                }
                else
                {
                    Points.AwardXP(killer, XPReward.RadioDestroyed);

                    Points.TryAwardDriverAssist(killer.Player, XPReward.RadioDestroyed, quota: 5);

                    Stats.StatsManager.ModifyStats(killer.Steam64, x => x.FobsDestroyed++, false);
                    Stats.StatsManager.ModifyTeam(team, t => t.FobsDestroyed++, false);
                }
            }
        }

        SendFOBListToTeam(team);
    }
    
    public static bool TryFindFOB(string name, ulong team, out IDeployable fob)
    {
        if (_singleton is { IsUnloading: false })
            _singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        List<IFOB> fobs = _singleton._fobs;
        if (int.TryParse(name, NumberStyles.Number, Data.LocalLocale, out int fobNumber))
        {
            for (int i = 0; i < fobs.Count; ++i)
            {
                if (fobs[i] is FOB fob2 && fob2.Number == fobNumber && fob2.Team == team)
                {
                    fob = fobs[i];
                    return true;
                }
            }
        }
        for (int i = 0; i < fobs.Count; ++i)
        {
            IFOB fob2 = fobs[i];
            if (fob2.Team == team && fob2.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
            {
                fob = fob2;
                return true;
            }
        }
        if (name.Equals("cache", StringComparison.InvariantCultureIgnoreCase))
        {
            fob = fobs.OfType<Cache>().FirstOrDefault(x => x.Team == team)!;
            return fob != null;
        }

        fob = null!;
        return false;
    }
    public static void UpdateFOBListForTeam(ulong team, IFOB? fob = null, bool resourcesOnly = false)
    {
        ThreadUtil.assertIsGameThread();
        if (_singleton is { IsUnloading: false })
            _singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        List<IFOB> list = _singleton._fobs;

        if (fob != null)
        {
            bool teams = Data.Is<ITeams>();
            if (teams && team == 0) return;

            int max = ListUI.FOBNames.Length;
            int index = -1;
            bool found = false;
            for (int i = 0; i < list.Count && index < max; ++i)
            {
                if (!teams || list[i].Team != team) continue;
                ++index;
                if (list[i].Equals(fob))
                {
                    found = true;
                    break;
                }
            }

            if (found && index < max)
            {
                IFOB f = list[index];
                foreach (LanguageSet set in LanguageSet.OnTeam(team))
                {
                    string? resx = (f as IResourceFOB)?.UIResourceString;
                    string? txt = resourcesOnly ? null : T.FOBUI.Translate(set.Language, f, f.GridLocation, f.ClosestLocation, null, set.Team);
                    while (set.MoveNext())
                    {
                        if (set.Next.HasUIHidden)
                            continue;
                        if (txt is not null)
                            ListUI.FOBNames[index].SetText(set.Next.Connection, txt);
                        if (resx is not null)
                            ListUI.FOBResources[index].SetText(set.Next.Connection, resx);
                    }
                }
                return;
            }
        }
        SendFOBListToTeam(team);
    }
    public static void SendFOBListToTeam(ulong team)
    {
        ThreadUtil.assertIsGameThread();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        bool teams = Data.Is<ITeams>();
        if (teams && team == 0) return;

        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
        {
            UCPlayer pl = PlayerManager.OnlinePlayers[i];
            if (!teams || pl.GetTeam() == team)
                SendFOBList(PlayerManager.OnlinePlayers[i]);
        }
    }

    public static void HideFOBList(UCPlayer player)
    {
        ThreadUtil.assertIsGameThread();
        ListUI.ClearFromPlayer(player.Player.channel.owner.transportConnection);
        player.HasFOBUI = false;
    }
    public static void SendFOBList(UCPlayer player)
    {
        if (player.HasUIHidden || Data.Gamemode is not { State: State.Active or State.Staging })
        {
            if (player.HasFOBUI)
                HideFOBList(player);
            return;
        }
        ulong team = player.GetTeam();
        if (team is not 1 and not 2)
        {
            HideFOBList(player);
            return;
        }

        if (_singleton is { IsUnloading: false })
            _singleton.AssertLoaded();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ITransportConnection connection = player.Connection;

        if (!player.HasFOBUI)
        {
            ListUI.SendToPlayer(connection);
            player.HasFOBUI = true;
        }

        List<IFOB> fobs = _singleton._fobs;
        int max = ListUI.FOBParents.Length;
        int i2 = 0;
        for (int i = 0; i < fobs.Count && i2 < max; ++i)
        {
            if (fobs[i].Team != team)
                continue;
            IFOB fob = fobs[i];
            ListUI.FOBParents[i2].SetVisibility(connection, true);
            ListUI.FOBNames[i2].SetText(connection, T.FOBUI.Translate(player, fob, fob.GridLocation, fob.ClosestLocation));
            ListUI.FOBResources[i2].SetText(connection, fob is IResourceFOB r ? r.UIResourceString : string.Empty);
            i2++;
        }
        for (; i2 < max; i2++)
        {
            ListUI.FOBParents[i2].SetVisibility(connection, false);
        }
    }
    private static void OnGroupChanged(GroupChanged e)
    {
        if (e.NewGroup.GetTeam() is > 0 and < 3)
            SendFOBList(e.Player);
    }
    void IUIListener.HideUI(UCPlayer player)
    {
        HideFOBList(player);
        ResourceUI.ClearFromPlayer(player.Connection);
    }
    void IUIListener.ShowUI(UCPlayer player)
    {
        SendFOBList(player);
        for (int i = 0; i < _fobs.Count; ++i)
        {
            if (_fobs[i] is FOB fob && fob.FriendliesNearby.Contains(player))
            {
                ResourceUI.SendToPlayer(player.Connection);
                ResourceUI.SetValues(player.Connection, fob.BuildSupply, fob.AmmoSupply);
                break;
            }
        }
    }
    void IUIListener.UpdateUI(UCPlayer player)
    {
        SendFOBList(player);
        for (int i = 0; i < _fobs.Count; ++i)
        {
            if (_fobs[i] is FOB fob && fob.FriendliesNearby.Contains(player))
            {
                ResourceUI.SetValues(player.Connection, fob.BuildSupply, fob.AmmoSupply);
                break;
            }
        }
    }
}

public class FOBConfigData : JSONConfigData
{
    public float FOBMaxHeightAboveTerrain;
    public bool RestrictFOBPlacement;
    public ushort FOBID;
    public ushort FOBRequiredBuild;
    public int FOBBuildPickupRadius;
    public int FOBBuildPickupRadiusNoBunker;
    public byte FobLimit;
    public int TicketsFOBRadioLost;
    public float BaseFOBRepairHits;

    public float AmmoCommandCooldown;
    public ushort AmmoCrateRequiredBuild;
    public ushort RepairStationRequiredBuild;

    public List<BuildableData> Buildables;
    public int AmmoBagMaxUses;

    public float DeployMainDelay;
    public float DeployFOBDelay;

    public bool EnableCombatLogger;
    public uint CombatCooldown;

    public bool EnableDeployCooldown;
    public uint DeployCooldown;
    public bool DeployCancelOnMove;
    public bool DeployCancelOnDamage;

    public bool ShouldRespawnAtMain;
    public bool ShouldWipeAllFOBsOnRoundedEnded;
    public bool ShouldSendPlayersBackToMainOnRoundEnded;
    public bool ShouldKillMaincampers;

    public ushort FirstFOBUiId;
    public ushort BuildResourceUI;

    [JsonConverter(typeof(Base64Converter))]
    public byte[] T1RadioState;
    [JsonConverter(typeof(Base64Converter))]
    public byte[] T2RadioState;

    public override void SetDefaults()
    {
        FOBMaxHeightAboveTerrain = 25f;
        RestrictFOBPlacement = true;
        FOBRequiredBuild = 15;
        FOBBuildPickupRadius = 80;
        FOBBuildPickupRadiusNoBunker = 30;
        FobLimit = 10;
        TicketsFOBRadioLost = -40;
        // amount of hits it takes to full repair a radio. 30 dmg x 20 = 600 total hp
        BaseFOBRepairHits = 20;

        AmmoCrateRequiredBuild = 2;
        AmmoCommandCooldown = 120f;

        RepairStationRequiredBuild = 6;

        T1RadioState = Convert.FromBase64String("8yh8NQEAEAEAAAAAAAAAACIAAACmlQFkAAQAAKyVAWQAAAIArpUBZAAABQDGlQFkAAMFAMmVAWQABgUAyZUBZAAACACmZQFkAAMIAMCVAWQABggAwJUBZAAHAgDWlQFkAAoCANaVAWQABwMA1pUBZAAKAwDWlQFkAAcEANaVAWQACgQA1pUBZAAJBQDYlQFkAAkHANiVAWQACQkA2JUBZAAACwDOlQFkAAAMAM6VAWQAAA0AzpUBZAADCwDOlQFkAAcAAKyVAWQACgAA1pUBZAAKAQDWlQFkAAMMAM6VAWQAAw0AzpUBZAAGDQDQlQFkAAQCANqVAWQACQsA0JUBZAAJDADQlQFkAAkNANCVAWQABgsAzpUBZAAGDADOlQFkAA==");
        //T2RadioState = "8yh8NQEAEAEAAAAAAAAAACIAAACmlQFkAAQAAKyVAWQAAAIArpUBZAADCADAlQFkAAYIAMCVAWQABwIA1pUBZAAKAgDWlQFkAAcDANaVAWQACgMA1pUBZAAHBADWlQFkAAoEANaVAWQACQUA2JUBZAAJBwDYlQFkAAkJANiVAWQAAAsAzpUBZAAADADOlQFkAAANAM6VAWQAAwsAzpUBZAAHAACslQFkAAoAANaVAWQACgEA1pUBZAADDADOlQFkAAMNAM6VAWQABg0A0JUBZAAEAgDalQFkAAkLANCVAWQACQwA0JUBZAAJDQDQlQFkAAYLAM6VAWQABgwAzpUBZAAABQDDlQFkAAMFAMqVAWQABgUAypUBZAAACAC6ZQFkAA=="; // Russia/MEC
        T2RadioState = Convert.FromBase64String("8yh8NQEAEAEAAAAAAAAAACIAAACmlQFkAAQAAKyVAWQAAAIArpUBZAADCADAlQFkAAYIAMCVAWQABwIA1pUBZAAKAgDWlQFkAAcDANaVAWQACgMA1pUBZAAHBADWlQFkAAoEANaVAWQACQUA2JUBZAAJBwDYlQFkAAkJANiVAWQAAAsAzpUBZAAADADOlQFkAAANAM6VAWQAAwsAzpUBZAAHAACslQFkAAoAANaVAWQACgEA1pUBZAADDADOlQFkAAMNAM6VAWQABg0A0JUBZAAEAgDalQFkAAkLANCVAWQACQwA0JUBZAAJDQDQlQFkAAYLAM6VAWQABgwAzpUBZAAACAC6ZQFkAAAFANVlAWQAAwUAvmUBZAAGBQC+ZQFkAA==");

        AmmoBagMaxUses = 3;

        Buildables = new List<BuildableData>()
        {
            new BuildableData
            {
                FullBuildable = new JsonAssetReference<ItemAsset>("61c349f10000498fa2b92c029d38e523"),
                Foundation = new JsonAssetReference<ItemAsset>("1bb17277dd8148df9f4c53d1a19b2503"),
                Type = BuildableType.Bunker,
                RequiredHits = 30,
                RequiredBuild = 15,
                Team = 0,
                Limit = 1,
                Emplacement = null
            },
            new BuildableData
            {
                FullBuildable = new JsonAssetReference<ItemAsset>("6fe208519d7c45b0be38273118eea7fd"),
                Foundation = new JsonAssetReference<ItemAsset>("eccfe06e53d041d5b83c614ffa62ee59"),
                Type = BuildableType.AmmoCrate,
                RequiredHits = 10,
                RequiredBuild = 1,
                Team = 0,
                Limit = 6,
                Emplacement = null
            },
            new BuildableData
            {
                FullBuildable = new JsonAssetReference<ItemAsset>("c0d11e0666694ddea667377b4c0580be"),
                Foundation = new JsonAssetReference<ItemAsset>("26a6b91cd1944730a0f28e5f299cebf9"),
                Type = BuildableType.RepairStation,
                RequiredHits = 25,
                RequiredBuild = 15,
                Team = 0,
                Limit = 1,
                Emplacement = null
            },
            new BuildableData
            {
                // sandbag line
                FullBuildable = new JsonAssetReference<ItemAsset>("ab702192eab4456ebb9f6d7cc74d4ba2"),
                Foundation = new JsonAssetReference<ItemAsset>("15f674dcaf3f44e19a124c8bf7e19ca2"),
                Type = BuildableType.Fortification,
                RequiredHits = 10,
                RequiredBuild = 1,
                Team = 0,
                Limit = 8,
                Emplacement = null
            },
            new BuildableData
            {
                // sandbag pillbox
                FullBuildable = new JsonAssetReference<ItemAsset>("f3bd9ee2fa334faabc8fd9d5a3b84424"),
                Foundation = new JsonAssetReference<ItemAsset>("a9294335d8e84b76b1cbcb7d70f66aaa"),
                Type = BuildableType.Fortification,
                RequiredHits = 10,
                RequiredBuild = 1,
                Team = 0,
                Limit = 6,
                Emplacement = null
            },
            new BuildableData
            {
                // sandbag crescent
                FullBuildable = new JsonAssetReference<ItemAsset>("eefee76f077349e58359f5fd03cf311d"),
                Foundation = new JsonAssetReference<ItemAsset>("920f8b30ae314406ab032a0c2efa753d"),
                Type = BuildableType.Fortification,
                RequiredHits = 10,
                RequiredBuild = 1,
                Team = 0,
                Limit = 4,
                Emplacement = null
            },
            new BuildableData
            {
                // sandbag foxhole
                FullBuildable = new JsonAssetReference<ItemAsset>("a71e3e3d6bb54a36b7bd8bf5f25160aa"),
                Foundation = new JsonAssetReference<ItemAsset>("12ea830dd9ab4f949893bbbbc5e9a5f6"),
                Type = BuildableType.Fortification,
                RequiredHits = 12,
                RequiredBuild = 2,
                Team = 0,
                Limit = 3,
                Emplacement = null
            },
            new BuildableData
            {
                // razorwire
                FullBuildable = new JsonAssetReference<ItemAsset>("bc24bd85ff714ff7bb2f8b2dd5056395"),
                Foundation = new JsonAssetReference<ItemAsset>("a2a8a01a58454816a6c9a047df0558ad"),
                Type = BuildableType.Fortification,
                RequiredHits = 10,
                RequiredBuild = 1,
                Team = 0,
                Limit = 16,
                Emplacement = null
            },
            new BuildableData
            {
                // hesco wall
                FullBuildable = new JsonAssetReference<ItemAsset>("e1af3a3af31e4996bc5d6ffd9a0773ec"),
                Foundation = new JsonAssetReference<ItemAsset>("baf23a8b514441ee8db891a3ddf32ef4"),
                Type = BuildableType.Fortification,
                RequiredHits = 25,
                RequiredBuild = 1,
                Team = 0,
                Limit = 4,
                Emplacement = null
            },
            new BuildableData
            {
                // hesco tower
                FullBuildable = new JsonAssetReference<ItemAsset>("857c85161f254964a921700a69e215a9"),
                Foundation = new JsonAssetReference<ItemAsset>("827d0ca8bfff43a39f750f191e16ea71"),
                Type = BuildableType.Fortification,
                RequiredHits = 20,
                RequiredBuild = 1,
                Team = 0,
                Limit = 4,
                Emplacement = null
            },
            new BuildableData
            {
                // M2A1
                FullBuildable = new JsonAssetReference<ItemAsset>(),
                Foundation = new JsonAssetReference<ItemAsset>("80396c361d3040d7beb3921964ec2997"),
                Type = BuildableType.Emplacement,
                RequiredHits = 16,
                RequiredBuild = 6,
                Team = 1,
                Limit = 2,
                Emplacement = new EmplacementData
                {
                    EmplacementVehicle = new JsonAssetReference<VehicleAsset>("aa3c6af4911243b5b5c9dc95ca1263bf"),
                    BaseBarricade =  new JsonAssetReference<ItemAsset>(),
                    Ammo = new JsonAssetReference<ItemAsset>("523c49ce4df44d46ba37be0dd6b4504b"),
                    AmmoCount = 2
                }
            },
            new BuildableData
            {
                // Kord
                FullBuildable = new JsonAssetReference<ItemAsset>(),
                Foundation = new JsonAssetReference<ItemAsset>("e44ba62f763c432e882ddc7eabaa9c77"),
                Type = BuildableType.Emplacement,
                RequiredHits = 16,
                RequiredBuild = 6,
                Team = 2,
                Limit = 2,
                Emplacement = new EmplacementData
                {
                    EmplacementVehicle = new JsonAssetReference<VehicleAsset>("86cfe1eb8be144aeae7659c9c74ff11a"),
                    BaseBarricade = new JsonAssetReference<ItemAsset>(),
                    Ammo = new JsonAssetReference<ItemAsset>("6e9bc2083a1246b49b1656c2ec6f535a"),
                    AmmoCount = 2,
                }
            },
            new BuildableData
            {
                // QJC-88
                FullBuildable = new JsonAssetReference<ItemAsset>(),
                Foundation = new JsonAssetReference<ItemAsset>("beaa260d7f844724bd26993569d9e42a"),
                Type = BuildableType.Emplacement,
                RequiredHits = 16,
                RequiredBuild = 6,
                Team = 2,
                Limit = 2,
                Emplacement = new EmplacementData
                {
                    EmplacementVehicle = new JsonAssetReference<VehicleAsset>("9525ef43b9674343bb9561b5db078c1b"),
                    BaseBarricade = new JsonAssetReference<ItemAsset>(),
                    Ammo = new JsonAssetReference<ItemAsset>("6e9bc2083a1246b49b1656c2ec6f535a"),
                    AmmoCount = 2,
                }
            },
            new BuildableData
            {
                // TOW
                FullBuildable = new JsonAssetReference<ItemAsset>(),
                Foundation = new JsonAssetReference<ItemAsset>("a68ae466fb804829a0eb0d4556071801"),
                Type = BuildableType.Emplacement,
                RequiredHits = 25,
                RequiredBuild = 14,
                Team = 1,
                Limit = 1,
                Emplacement = new EmplacementData
                {
                    EmplacementVehicle = new JsonAssetReference<VehicleAsset>("9d305050a6a142349376d6c49fb38362"),
                    BaseBarricade = new JsonAssetReference<ItemAsset>(),
                    Ammo = new JsonAssetReference<ItemAsset>("3128a69d06ac4bbbbfddc992aa7185a6"),
                    AmmoCount = 1
                }
            },
            new BuildableData
            {
                // Kornet
                FullBuildable = new JsonAssetReference<ItemAsset>(),
                Foundation = new JsonAssetReference<ItemAsset>("37811b1847744c958fcb30a0b759874b"),
                Type = BuildableType.Emplacement,
                RequiredHits = 25,
                RequiredBuild = 14,
                Team = 2,
                Limit = 1,
                Emplacement = new EmplacementData
                {
                    EmplacementVehicle = new JsonAssetReference<VehicleAsset>("677b1084-dffa-4633-84d2-9167a3fae25b"),
                    BaseBarricade = new JsonAssetReference<ItemAsset>(),
                    Ammo = new JsonAssetReference<ItemAsset>("d7774b017c404adbb0a0fe8e902b9689"),
                    AmmoCount = 1
                }
            },
            new BuildableData
            {
                // HJ-8
                FullBuildable = new JsonAssetReference<ItemAsset>(),
                Foundation = new JsonAssetReference<ItemAsset>("f76572f13c214a138415f20bbc1a31c3"),
                Type = BuildableType.Emplacement,
                RequiredHits = 25,
                RequiredBuild = 14,
                Team = 2,
                Limit = 1,
                Emplacement = new EmplacementData
                {
                    EmplacementVehicle = new JsonAssetReference<VehicleAsset>("b63e9c2999c34ff3894138592dd9cb2e"),
                    BaseBarricade = new JsonAssetReference<ItemAsset>(),
                    Ammo = new JsonAssetReference<ItemAsset>("9ba5db5cbd2c4b51bc236122a4c6b205"),
                    AmmoCount = 1
                }
            },
            new BuildableData
            {
                // Stinger
                FullBuildable = new JsonAssetReference<ItemAsset>(),
                Foundation = new JsonAssetReference<ItemAsset>("3c2dd7febc854b7f8859852b8c736c8e"),
                Type = BuildableType.Emplacement,
                RequiredHits = 25,
                RequiredBuild = 14,
                Team = 2,
                Limit = 1,
                Emplacement = new EmplacementData
                {
                    EmplacementVehicle = new JsonAssetReference<VehicleAsset>("1883345cbdad40aa81e49c84e6c872ef"),
                    BaseBarricade = new JsonAssetReference<ItemAsset>(),
                    Ammo = new JsonAssetReference<ItemAsset>("3c0a94af5af24901a9e3207f3e9ed0ba"),
                    AmmoCount = 1
                }
            },
            new BuildableData
            {
                // Igla
                FullBuildable = new JsonAssetReference<ItemAsset>(),
                Foundation = new JsonAssetReference<ItemAsset>("b50cb548734946ffa5f88d6691a2c7ce"),
                Type = BuildableType.Emplacement,
                RequiredHits = 25,
                RequiredBuild = 14,
                Limit = 1,
                Emplacement = new EmplacementData
                {
                    EmplacementVehicle = new JsonAssetReference<VehicleAsset>("8add59a2e2b94f93ab0d6b727d310097"),
                    BaseBarricade = new JsonAssetReference<ItemAsset>(),
                    Ammo = new JsonAssetReference<ItemAsset>("a54d571983c2432a9624eec39d602997"),
                    AmmoCount = 1
                }
            },
            new BuildableData
            {
                // Mortar
                FullBuildable = new JsonAssetReference<ItemAsset>(),
                Foundation = new JsonAssetReference<ItemAsset>("6ff4826eaeb14c7cac1cf25a55d24bd3"),
                Type = BuildableType.Emplacement,
                RequiredHits = 22,
                RequiredBuild = 10,
                Team = 0,
                Limit = 2,
                Emplacement = new EmplacementData
                {
                    EmplacementVehicle = new JsonAssetReference<VehicleAsset>("94bf8feb05bc4680ac26464bc175460c"),
                    BaseBarricade = new JsonAssetReference<ItemAsset>("c3eb4dd3fd1d463993ec69c4c3de50d7"), // Mortar
                    Ammo = new JsonAssetReference<ItemAsset>("66f4c76a119e4d6ca9d0b1a866c4d901"),
                    AmmoCount = 3,
                    ShouldWarnFriendlies = true,
                    ShouldWarnEnemies = true
                }
            },
        };

        DeployMainDelay = 3;
        DeployFOBDelay = 5;

        DeployCancelOnMove = true;
        DeployCancelOnDamage = true;

        ShouldRespawnAtMain = true;
        ShouldSendPlayersBackToMainOnRoundEnded = true;
        ShouldWipeAllFOBsOnRoundedEnded = true;
        ShouldKillMaincampers = true;
    }
}

public class BuildableData : ITranslationArgument
{
    [JsonPropertyName("foundationID")]
    public RotatableConfig<JsonAssetReference<ItemAsset>> Foundation;
    [JsonPropertyName("structureID")]
    public RotatableConfig<JsonAssetReference<ItemAsset>>? FullBuildable;
    [JsonPropertyName("type")]
    public BuildableType Type;
    [JsonPropertyName("requiredHits")]
    public RotatableConfig<int> RequiredHits;
    [JsonPropertyName("requiredBuild")]
    public RotatableConfig<int> RequiredBuild;
    [JsonPropertyName("team")]
    public int Team;
    [JsonPropertyName("limit")]
    public int Limit;
    [JsonPropertyName("disabled")]
    public RotatableConfig<bool> Disabled;
    [JsonPropertyName("emplacementData")]
    public EmplacementData? Emplacement;
    [JsonPropertyName("dontAutoWhitelist")]
    public bool DontAutoWhitelist;

    public string Translate(string language, string? format, UCPlayer? target, CultureInfo? culture, ref TranslationFlags flags)
    {
        if (Emplacement is not null && Emplacement.EmplacementVehicle.ValidReference(out VehicleAsset vasset))
        {
            string plural = Translation.Pluralize(language, culture, vasset.vehicleName, flags);
            if (format is not null && format.Equals(T.FormatRarityColor))
                return Localization.Colorize(ItemTool.getRarityColorUI(vasset.rarity).Hex(), plural, flags);

            return plural;
        }

        if (Foundation.ValidReference(out ItemAsset iasset) || FullBuildable.ValidReference(out iasset))
        {
            string plural = Translation.Pluralize(language, culture, GetItemName(iasset.itemName), flags);
            if (format is not null && format.Equals(T.FormatRarityColor))
                return Localization.Colorize(ItemTool.getRarityColorUI(iasset.rarity).Hex(), plural, flags);
            else
                return plural;
        }

        if (Emplacement is not null)
        {
            if (Emplacement.BaseBarricade.ValidReference(out iasset))
            {
                string plural = Translation.Pluralize(language, culture, GetItemName(iasset.itemName), flags);
                if (format is not null && format.Equals(T.FormatRarityColor))
                    return Localization.Colorize(ItemTool.getRarityColorUI(iasset.rarity).Hex(), plural, flags);
                else
                    return plural;
            }
            if (Emplacement.Ammo.ValidReference(out iasset))
            {
                string plural = Translation.Pluralize(language, culture, GetItemName(iasset.itemName), flags);
                if (format is not null && format.Equals(T.FormatRarityColor))
                    return Localization.Colorize(ItemTool.getRarityColorUI(iasset.rarity).Hex(), plural, flags);
                else
                    return plural;
            }
        }

        string GetItemName(string itemName)
        {
            int ind = itemName.IndexOf(" Built", StringComparison.OrdinalIgnoreCase);
            if (ind != -1)
                itemName = itemName.Substring(0, ind);
            return itemName;
        }

        return Localization.TranslateEnum(Type, language);
    }

    public override string ToString()
    {
        TranslationFlags flags = TranslationFlags.NoRichText;
        return Translate(L.Default, null, null, Data.LocalLocale, ref flags);
    }
}

[JsonSerializable(typeof(EmplacementData))]
public class EmplacementData
{
    [JsonPropertyName("vehicleID")]
    public JsonAssetReference<VehicleAsset> EmplacementVehicle;
    [JsonPropertyName("baseID")]
    public JsonAssetReference<ItemAsset> BaseBarricade;
    [JsonPropertyName("ammoID")]
    public JsonAssetReference<ItemAsset> Ammo;
    [JsonPropertyName("ammoAmount")]
    public int AmmoCount;
    [JsonPropertyName("warnFriendlyProjectiles")]
    public bool ShouldWarnFriendlies;
    [JsonPropertyName("warnEnemyProjectiles")]
    public bool ShouldWarnEnemies;
}

[Translatable("Buildable Type")]
public enum BuildableType
{
    Bunker,
    AmmoCrate,
    RepairStation,
    Fortification,
    Emplacement,
    Radio
}
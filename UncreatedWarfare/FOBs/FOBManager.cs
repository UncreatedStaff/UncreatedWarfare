using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Uncreated.Warfare.Buildables;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Configuration;
using Uncreated.Warfare.Configuration.JsonConverters;
using Uncreated.Warfare.Events;
using Uncreated.Warfare.Events.Barricades;
using Uncreated.Warfare.Events.Players;
using Uncreated.Warfare.Events.Vehicles;
using Uncreated.Warfare.FOBs.UI;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Layouts.Insurgency;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Models.Kits;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Players.UI;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Util;
using Uncreated.Warfare.Vehicles;
using Cache = Uncreated.Warfare.Components.Cache;
using XPReward = Uncreated.Warfare.Levels.XPReward;

namespace Uncreated.Warfare.FOBs;

[SingletonDependency(typeof(Whitelister))]
public class FOBManager : BaseSingleton, ILevelStartListener, IGameStartListener, IPlayerDisconnectListener, IGameTickListener, IJoinedTeamListener, IUIListener, IDisposable
{
    private readonly IConfigurationRoot _config;
    private readonly IDisposable? _configListener;

    private static readonly List<InteractableVehicle> WorkingNearbyVehicles = new List<InteractableVehicle>(16);

    private const float InsideFOBRangeSqr = 2f * 2f;
    internal static bool IgnorePlacingBarricade;
    internal static bool IgnorePlacingStructure;

    private List<IFOBItem> _floatingItems;
    private List<IFOB> _fobs;
    private static FOBManager _singleton;
    public static readonly FOBListUI ListUI = new FOBListUI();
    public static readonly NearbyResourceUI ResourceUI = new NearbyResourceUI();
    private readonly IFOB?[] _team1List = new IFOB?[ListUI.FOBs.Length * 2];
    private readonly IFOB?[] _team2List = new IFOB?[ListUI.FOBs.Length * 2];

    public static FOBConfigData Config { get; private set; }
    public static bool Loaded => _singleton.IsLoaded();
    public IReadOnlyList<IFOBItem> FloatingItems { get; private set; }
    public IReadOnlyList<IFOB> FOBs { get; private set; }
    public IReadOnlyList<IFOB?> Team1ListEntries { get; private set; }
    public IReadOnlyList<IFOB?> Team2ListEntries { get; private set; }

    public FOBManager(WarfareModule warfare)
    {
        ConfigurationBuilder configBuilder = new ConfigurationBuilder();
        ConfigurationHelper.AddSourceWithMapOverride(configBuilder, Path.Join(warfare.HomeDirectory, "FOBs.yml"));

        _config = configBuilder.Build();
        _configListener = _config.GetReloadToken().RegisterChangeCallback(_ => ReloadConfig(), null);
        ReloadConfig();
    }
    public void ReloadConfig()
    {
        Config = _config.ParseConfigData<FOBConfigData>();
    }
    public override void Load()
    {
        ReloadConfig();
        EventDispatcher.PlayerDied += OnPlayerDied;
        EventDispatcher.GroupChanged += OnGroupChanged;
        EventDispatcher.BarricadePlaced += OnBarricadePlaced;
        EventDispatcher.VehicleDestroyed += OnVehicleDestroyed;
        EventDispatcher.BarricadeDestroyed += OnBarricadeDestroyed;
        EventDispatcher.BarricadePlaceRequested += OnRequestedBarricadePlace;
        _floatingItems = new List<IFOBItem>(64);
        _fobs = new List<IFOB>(24);
        FloatingItems = _floatingItems.AsReadOnly();
        FOBs = _fobs.AsReadOnly();
        Team1ListEntries = new ReadOnlyCollection<IFOB?>(_team1List);
        Team2ListEntries = new ReadOnlyCollection<IFOB?>(_team2List);
        int ct = Math.Max(_team1List.Length, _team2List.Length);
        for (int i = 0; i < ct; ++i)
        {
            if (i < _team1List.Length)
                _team1List[i] = null;
            if (i < _team2List.Length)
                _team2List[i] = null;
        }
        ListUI.Update(this);
        for (int i = 0; i < Config.Buildables.Count; ++i)
        {
            BuildableData data = Config.Buildables[i];
            if (data.DontAutoWhitelist) continue;
            if (data.Foundation.TryGetGuid(out Guid guid))
                Whitelister.AddItem(guid);
            if (data.Emplacement != null)
            {
                if (data.Emplacement.Ammo.TryGetGuid(out guid))
                    Whitelister.AddItem(guid);
            }
        }
        for (int i = 1; i <= 2; ++i)
        {
            FactionInfo f = TeamManager.GetFaction((ulong)i);
            if (f.Ammo.TryGetGuid(out Guid guid))
                Whitelister.AddItem(guid);
            if (f.Build.TryGetGuid(out guid))
                Whitelister.AddItem(guid);
            if (f.FOBRadio.TryGetGuid(out guid))
                Whitelister.AddItem(guid);
            if (f.RallyPoint.TryGetGuid(out guid))
                Whitelister.AddItem(guid);
        }
        _singleton = this;
    }
    public override void Unload()
    {
        EventDispatcher.BarricadePlaceRequested -= OnRequestedBarricadePlace;
        EventDispatcher.BarricadeDestroyed -= OnBarricadeDestroyed;
        EventDispatcher.VehicleDestroyed -= OnVehicleDestroyed;
        EventDispatcher.BarricadePlaced -= OnBarricadePlaced;
        EventDispatcher.GroupChanged -= OnGroupChanged;
        EventDispatcher.PlayerDied -= OnPlayerDied;
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
    private void AddFOB(IFOB fob)
    {
        AddFOBToList(fob.Team, fob);
        for (int i = 0; i < _fobs.Count; ++i)
        {
            if (GetFOBPriority(_fobs[i]) > GetFOBPriority(fob))
            {
                _fobs.Insert(i, fob);
                return;
            }
        }

        _fobs.Add(fob);
    }
    public static void ShowResourceToast(LanguageSet set, int build = 0, int ammo = 0, string? message = null)
    {
        if (build != 0)
        {
            string number = (build > 0 ? T.FOBToastGainBuild : T.FOBToastLoseBuild).Translate(set.Language, Math.Abs(build), null, set.Team);
            ToastMessage msg = string.IsNullOrEmpty(message)
                ? new ToastMessage(ToastMessageStyle.Mini, number)
                : new ToastMessage(ToastMessageStyle.Mini, number + "\n" + message.Colorize("adadad"));
            while (set.MoveNext())
            {
                set.Next.Toasts.Queue(in msg);
            }
            set.Reset();
        }
        if (ammo != 0)
        {
            string number = (ammo > 0 ? T.FOBToastGainAmmo : T.FOBToastLoseAmmo).Translate(set.Language, Math.Abs(ammo), null, set.Team);
            ToastMessage msg = string.IsNullOrEmpty(message)
                ? new ToastMessage(ToastMessageStyle.Mini, number)
                : new ToastMessage(ToastMessageStyle.Mini, number + "\n" + message.Colorize("adadad"));
            while (set.MoveNext())
            {
                set.Next.Toasts.Queue(in msg);
            }
            set.Reset();
        }
    }
    private static int GetFOBPriority(IFOB fob)
    {
        if (fob == null) return -1;
        if (fob is FOB f)
            return f.Number;
        if (fob is Cache c)
            return c.Number - 15;
        return -100;
    }
    public void LoadRepairStations()
    {
        foreach (BarricadeInfo barricade in BarricadeUtility.EnumerateNonPlantedBarricades())
        {
            BuildableData? data = FindBuildable(barricade.Drop.asset);
            if (data is { Type: BuildableType.RepairStation } && !barricade.Drop.model.TryGetComponent(out RepairStationComponent _))
            {
                barricade.Drop.model.gameObject.AddComponent<RepairStationComponent>();
            }
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
        if (GamemodeOld.Config.EffectDig.TryGetAsset(out EffectAsset? effect))
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
        try
        {
            if (fob is IDisposable disposable)
                disposable.Dispose();
        }
        catch (Exception ex)
        {
            L.LogError($"[FOBS] Failed to dispose FOB {fob.Name}.");
            L.LogError(ex);
        }

        if (_singleton == null)
            return;

        _singleton._fobs.Remove(fob);
        _singleton.RemoveFOBFromList(fob.Team, fob);
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

    private string GetOpenStandardFOBName(ulong team, out int number)
    {
        int last = 0;
        foreach (FOB fob in FOBs.OfType<FOB>().Where(f => f.Team == team).OrderBy(x => x.Number))
        {
            if (fob.Number != last + 1)
                break;

            last = fob.Number;
        }

        number = last + 1;
        return "FOB" + number.ToString(Data.LocalLocale);
    }
    public void DestroyAllFOBs(ulong instigator = 0ul)
    {
        for (int i = _fobs.Count - 1; i >= 0; --i)
            DeleteFOB(_fobs[i], instigator);
    }
    public IFOB? FindFob(IBuildable buildable)
    {
        if (buildable?.Model == null)
            return null;
        Vector3 pos = buildable.Model.position;
        foreach (IFOB fob in FOBs.OrderBy(x => (pos - x.Position).sqrMagnitude))
        {
            if (fob.ContainsBuildable(buildable))
                return fob;
        }
        
        return null;
    }
    public IFOB? FindFob(InteractableVehicle vehicle)
    {
        if (vehicle == null)
            return null;
        Vector3 pos = vehicle.transform.position;
        foreach (IFOB fob in FOBs.OrderBy(x => (pos - x.Position).sqrMagnitude))
        {
            if (fob.ContainsVehicle(vehicle))
                return fob;
        }

        return null;
    }
    public IFOBItem? FindFobItem(IBuildable buildable)
    {
        if (buildable?.Model == null)
            return null;
        Vector3 pos = buildable.Model.position;
        foreach (IFOB fob in FOBs.OrderBy(x => (pos - x.Position).sqrMagnitude))
        {
            IFOBItem? item = fob.FindFOBItem(buildable);
            if (item != null)
                return item;
        }

        return null;
    }
    public IFOBItem? FindFobItem(InteractableVehicle vehicle)
    {
        if (vehicle == null)
            return null;
        Vector3 pos = vehicle.transform.position;
        foreach (IFOB fob in FOBs.OrderBy(x => (pos - x.Position).sqrMagnitude))
        {
            IFOBItem? item = fob.FindFOBItem(vehicle);
            if (item != null)
                return item;
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
    public T? FindNearestFOB<T>(Vector3 pos, ulong team, float radius) where T : IRadiusFOB
    {
        radius *= radius;
        return _fobs
            .OfType<T>()
            .Where(x => x.Team == team)
            .OrderBy(x => (pos - x.Position).sqrMagnitude)
            .FirstOrDefault(x => (pos - x.Position).sqrMagnitude < Mathf.Pow(radius, 2f));
    }
    internal bool ValidateFloatingPlacement(BuildableData buildable, UCPlayer player, Vector3 point, IFOBItem? ignoreFoundation)
    {
        if (player.OnDuty() && buildable.Type is BuildableType.RepairStation or BuildableType.AmmoCrate)
            return true;
        
        if (buildable.Type is BuildableType.RepairStation or BuildableType.AmmoCrate or BuildableType.Bunker or BuildableType.Radio)
        {
            player.SendChat(T.BuildNoRadio, buildable.Type == BuildableType.Bunker ? Config.FOBBuildPickupRadiusNoBunker : Config.FOBBuildPickupRadius);
            return false;
        }

        Kit? kit = player.CachedActiveKitInfo;
        if (kit == null || !kit.ContainsItem(buildable.Foundation, player.GetTeam()) || _floatingItems == null)
        {
            player.SendChat(T.BuildNoRadio, buildable.Type == BuildableType.Bunker ? Config.FOBBuildPickupRadiusNoBunker : Config.FOBBuildPickupRadius);
            return false;
        }

        ulong team = player.GetTeam();
        int limit = kit.CountItems(buildable.Foundation);
        int count = 0;
        for (int i = 0; i < _floatingItems.Count; ++i)
        {
            IFOBItem item = _floatingItems[i];
            if (ignoreFoundation is not null && item.Equals(ignoreFoundation))
                continue;
            BuildableData? b = item.Buildable;
            if (b != null && item.Team == team && item.Owner == player.Steam64 && b.Foundation.MatchAsset(buildable.Foundation) && (item is not ShovelableComponent sh || sh.ActiveVehicle == null || !sh.ActiveVehicle.isDead))
            {
                ++count;
                if (count < limit)
                    continue;

                player.SendChat(T.RegionalBuildLimitReached, limit, buildable);
                return false;
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
                    IFOBItem newItem = (IFOBItem)Activator.CreateInstance(item.GetType());
                    if (newItem.Icon.TryGetGuid(out Guid icon))
                        IconManager.AttachIcon(icon, newObj, newItem.Team, newItem.IconOffset);
                    _floatingItems[i] = newItem;
                    return newItem;
                }
            }

            return null;
        }

        for (int i = 0; i < _floatingItems.Count; ++i)
        {
            if (_floatingItems[i] is MonoBehaviour mb && mb == itemMb)
            {
                IFOBItem newItem = (IFOBItem)newObj.gameObject.AddComponent(item.GetType());
                _floatingItems[i] = newItem;
                if (newItem.Icon.TryGetGuid(out Guid icon))
                    IconManager.AttachIcon(icon, newObj, newItem.Team, newItem.IconOffset);
                if (itemMb is IDisposable d)
                    d.Dispose();
                Object.Destroy(itemMb);
                return newItem;
            }
        }

        return null;
    }

    void IJoinedTeamListener.OnJoinTeam(UCPlayer player, ulong team) => ListUI.UpdateFor(this, player);
    void ILevelStartListener.OnLevelReady()
    {
        foreach (BuildableData b in Config.Buildables)
        {
            if (b.Foundation.TryGetGuid(out Guid guid) && !Whitelister.IsWhitelisted(guid, out _))
            {
                Whitelister.AddItem(guid);
            }

            if (b.Emplacement == null)
                continue;

            if (!Whitelister.IsWhitelisted(b.Emplacement.Ammo, out _))
            {
                Whitelister.AddItem(b.Emplacement.Ammo);
            }
        }

        LoadRepairStations();
    }
    void IGameStartListener.OnGameStarting(bool isOnLoad)
    {
        ListUI.Update(this);
    }
    void IPlayerDisconnectListener.OnPlayerDisconnecting(UCPlayer player)
    {
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
        fob.Name = GetOpenStandardFOBName(team, out int number);
        fob.Number = number;
        fob.RegisterItem(radio.model.gameObject.AddComponent<RadioComponent>());
        fob.Team = team;

        AddFOB(fob);
        return fob;
    }
    private void OnPlayerDied(PlayerDied e)
    {
        if (e.WasSuicide || e.WasTeamkill || e.ActiveVehicle == null || !e.PrimaryAssetIsVehicle || Assets.find(e.PrimaryAsset) is not { } asset)
            return;

        BuildableData? data = FindBuildable(asset);
        if (data == null)
            return;

        IFOBItem? item = FindFobItem(e.ActiveVehicle);

        if (item?.FOB?.Record == null)
            return;

        Task.Run(async () =>
        {
            try
            {
                await item.FOB.Record.Update(record =>
                {
                    ++record.EmplacementPlayerKills;
                });
            }
            catch (Exception ex)
            {
                L.LogError($"[FOBS] [{item.FOB.Name}] Failed to update FOB record tracker after getting an emplacement kill.");
                L.LogError(ex);
            }
        });

        Task.Run(async () =>
        {
            try
            {
                await item.FOB.Record.Update(item, record =>
                {
                    ++record.PlayerKills;
                });
            }
            catch (Exception ex)
            {
                L.LogError($"[FOBS] [{item.FOB.Name}] Failed to update FOB record item tracker after getting an emplacement kill.");
                L.LogError(ex);
            }
        });
    }
    private void OnVehicleDestroyed(VehicleDestroyed e)
    {
        if (e.ActiveVehicle == null || e.ActiveVehicle == e.Vehicle)
            return;

        BuildableData? data = FindBuildable(e.ActiveVehicle.asset);
        if (data == null)
            return;

        IFOBItem? item = FindFobItem(e.ActiveVehicle);

        if (item?.FOB?.Record == null)
            return;

        Task.Run(async () =>
        {
            try
            {
                await item.FOB.Record.Update(record =>
                {
                    ++record.EmplacementVehicleKills;
                });
            }
            catch (Exception ex)
            {
                L.LogError($"[FOBS] [{item.FOB.Name}] Failed to update FOB record tracker after getting an emplacement vehicle kill.");
                L.LogError(ex);
            }
        });

        Task.Run(async () =>
        {
            try
            {
                await item.FOB.Record.Update(item, record =>
                {
                    ++record.VehicleKills;
                });
            }
            catch (Exception ex)
            {
                L.LogError($"[FOBS] [{item.FOB.Name}] Failed to update FOB record item tracker after getting an emplacement vehicle kill.");
                L.LogError(ex);
            }
        });
    }
    private void OnRequestedBarricadePlace(PlaceBarricadeRequested e)
    {
        if (IgnorePlacingBarricade) return;

        ulong team = e.GroupOwner.GetTeam();
        if (team is not 1ul and not 2ul || e.OriginalPlacer == null)
        {
            if (e.OriginalPlacer == null || !e.OriginalPlacer.OnDuty())
                e.Break();
            return;
        }

        bool isRadio = false;
        // radio, check team of radio
        if (GamemodeOld.Config.FOBRadios.Any(r => r.MatchGuid(e.Barricade.asset.GUID)))
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
                int uiSpace = GetUIRemainingSpace(team);
                if (uiSpace < 1)
                {
                    e.OriginalPlacer?.SendChat(T.BuildMaxFOBsHit);
                    e.Break();
                    return;
                }
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
                    if (f is FOB fob2 && (fob2.transform.position - e.Position).sqrMagnitude < sqrRad)
                    {
                        e.OriginalPlacer?.SendChat(T.BuildFOBTooClose, fob2, (fob2.transform.position - e.Position).magnitude, rad);
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
        bool isRadio = GamemodeOld.Config.FOBRadios.Any(r => r.MatchGuid(guid));
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

            if (e.Owner == null)
                return;

            if (GamemodeOld.Config.BarricadeFOBBunkerBase.TryGetAsset(out ItemBarricadeAsset? fobBase))
                ItemManager.dropItem(new Item(fobBase.id, true), e.Owner.Position, true, true, true);
            if (GamemodeOld.Config.BarricadeAmmoCrateBase.TryGetAsset(out ItemBarricadeAsset? ammoBase))
                ItemManager.dropItem(new Item(ammoBase.id, true), e.Owner.Position, true, true, true);
            QuestManager.OnFOBBuilt(e.Owner, fob);
            Tips.TryGiveTip(e.Owner, 3, T.TipPlaceBunker);
            return;
        }

        BuildableData? buildable = Config.Buildables.Find(b => b.Foundation.MatchGuid(guid));
        if (buildable == null || buildable.Type == BuildableType.Radio)
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
        if (Data.Gamemode.State is not State.Active and not State.Staging) return;
        for (int i = 0; i < _fobs.Count; ++i)
        {
            if (_fobs[i] is IGameTickListener ticker)
                ticker.Tick();
        }
    }
    private void OnBarricadeDestroyed(BarricadeDestroyed e)
    {
        L.LogDebug("[FOBS] barricade destroyed: " + e.Barricade.asset.itemName + ".");
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
                else if (GamemodeOld.Config.BarricadeFOBRadioDamaged.MatchGuid(e.ServersideData.barricade.asset.GUID))
                {
                    if (radio.State == RadioComponent.RadioState.Bleeding)
                        radio.FOB.Destroy();
                }
                else return;

                ListUI.Update(this, team);
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
            if ((_singleton._fobs[i].SpawnPosition - point).sqrMagnitude <= InsideFOBRangeSqr)
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
    public static bool IsOnFOB<TFOB>(UCPlayer player, out TFOB fob) where TFOB : class, IRadiusFOB
    {
        ThreadUtil.assertIsGameThread();
        _singleton.AssertLoaded();

        for (int i = 0; i < _singleton._fobs.Count; ++i)
        {
            if (_singleton._fobs[i] is TFOB fob2 && fob2.IsPlayerOn(player))
            {
                fob = fob2;
                return true;
            }
        }

        fob = null!;
        return false;
    }
    public SpecialFOB RegisterNewSpecialFOB(string name, Vector3 point, ulong team, string color, bool disappearAroundEnemies)
    {
        ThreadUtil.assertIsGameThread();
        _singleton.AssertLoaded();
        SpecialFOB fob = new SpecialFOB(name, point, team, color, disappearAroundEnemies);
        _singleton.AddFOB(fob);

        return fob;
    }
    public static Cache RegisterNewCache(IBuildable drop, ulong team, CacheLocation location)
    {
        ThreadUtil.assertIsGameThread();
        _singleton.AssertLoaded();
        
        int number;
        if (Data.Is(out Insurgency? insurgency))
        {
            List<Insurgency.CacheData> caches = insurgency.ActiveCaches;
            if (caches.Count == 0)
                number = insurgency.CachesDestroyed + 1;
            else
                number = caches.Last().Number + 1;
        }
        else number = 0;
        Cache cache = new Cache(drop, team, location, number != 0 ? "CACHE" + number.ToString(CultureInfo.InvariantCulture) : "CACHE", number);


        _singleton.AddFOB(cache);

        if (Data.Is(out IAttackDefense? atk) && GamemodeOld.Config.EffectMarkerCacheDefend.TryGetGuid(out Guid effectGuid))
            IconManager.AttachIcon(effectGuid, drop.Model, atk.DefendingTeam, 3.25f);

        return cache;

    }
    public void DeleteFOB(IFOB fob, ulong instigator = 0)
    {
        ThreadUtil.assertIsGameThread();
        Deployment.CancelDeploymentsTo(fob);
        ulong team = fob.Team;

        UCPlayer? killer = UCPlayer.FromID(instigator);
        fob.Instigator = killer;
        
        if (fob is FOB f2)
            f2.Destroy();
        else
        {
            RemoveFOBFromList(team, fob);
            _fobs.RemoveAll(x => x.Equals(fob));
        }
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
            if (killer != null && killerteam != 0 && killerteam != team && Data.Is(out IGameStats? w) && w.GameStats is IFobsTracker ft)
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
                if (Data.Is(out ITickets? tickets))
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
                }
            }
        }
    }
    
    public static bool TryFindFOB(string name, ulong team, out IDeployable fob)
    {
        if (_singleton is { IsUnloading: false })
            _singleton.AssertLoaded();
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
    private void OnGroupChanged(GroupChanged e)
    {
        ListUI.UpdateFor(this, e.Player);
    }
    internal int UpdateFOBInList(ulong team, IFOB fob, bool resourcesOnly = false)
    {
        IFOB?[] fobs = team switch
        {
            1ul => _team1List,
            2ul => _team2List,
            _ => throw new ArgumentOutOfRangeException(nameof(team))
        };
        int index = -1;
        for (int i = 0; i < fobs.Length; ++i)
        {
            IFOB? fob2 = fobs[i];
            if (fob2 != fob)
            {
                if (fob2 == null)
                    break;
                continue;
            }

            index = i;
            break;
        }

        if (index == -1)
            return -1;

        L.LogDebug($"[FOBS] [{fob.Name.ToUpper()}] Updating fob list.");
        ListUI.UpdateFor(LanguageSet.OnTeam(team), fobs, index, resourcesOnly);
        return index;
    }
    // sorted by priority
    internal int AddFOBToList(ulong team, IFOB fob)
    {
        IFOB?[] fobs = team switch
        {
            1ul => _team1List,
            2ul => _team2List,
            _ => throw new ArgumentOutOfRangeException(nameof(team))
        };
        int index = -1;
        int priority = GetFOBPriority(fob);
        for (int i = 0; i < fobs.Length; ++i)
        {
            IFOB? fob2 = fobs[i];
            if (fob2 == null)
            {
                index = i;
                break;
            }
            int priority2 = GetFOBPriority(fob2);
            if (priority2 <= priority)
                continue;
            index = i;
            break;
        }

        return AddFOBToList(team, fob, index);
    }
    internal int AddFOBToList(ulong team, IFOB fob, int index)
    {
        IFOB?[]? fobs = team switch
        {
            1ul => _team1List,
            2ul => _team2List,
            _ => null
        };

        if (fobs == null)
            return -1;

        if (index >= fobs.Length)
            index = fobs.Length - 1;
        if (index < 0)
        {
            for (int i = 0; i < fobs.Length; ++i)
            {
                if (fobs[i] == null)
                {
                    index = i;
                    break;
                }
            }
        }

        if (index < 0)
            return -1;

        for (int i = fobs.Length - 1; i > index; --i)
        {
            fobs[i] = fobs[i - 1];
        }

        fobs[index] = fob;
        ListUI.UpdatePast(this, index, team);
        L.LogDebug($"[FOBS] [{fob.Name.ToUpper()}] Adding to fob list.");
        return index;
    }
    internal bool RemoveFOBFromList(ulong team, IFOB fob)
    {
        IFOB?[]? fobs = team switch
        {
            1ul => _team1List,
            2ul => _team2List,
            _ => null
        };
        if (fobs == null)
            return false;

        int index = -1;

        for (int i = 0; i < fobs.Length; ++i)
        {
            if (fobs[i] == fob)
            {
                index = i;
                break;
            }
        }

        if (index == -1)
            return false;
        
        for (int i = index; i < fobs.Length - 1; ++i)
        {
            fobs[i] = fobs[i + 1];
            if (fobs[i] == null)
                break;
        }

        L.LogDebug($"[FOBS] [{fob.Name.ToUpper()}] Removed from fob list.");
        ListUI.UpdatePast(this, index, team);
        return true;
    }
    internal int GetUIRemainingSpace(ulong team)
    {
        IFOB?[] fobs = team switch
        {
            1ul => _team1List,
            2ul => _team2List,
            _ => throw new ArgumentOutOfRangeException(nameof(team))
        };
        int ct = Math.Min(fobs.Length, ListUI.FOBs.Length);
        for (int i = 0; i < ct; ++i)
        {
            if (fobs[i] == null)
                return ct - i;
        }

        return 0;
    }
    void IUIListener.HideUI(UCPlayer player)
    {
        ListUI.Hide(player);
        ResourceUI.ClearFromPlayer(player.Connection);
    }
    void IUIListener.ShowUI(UCPlayer player)
    {
        ListUI.UpdateFor(this, player);
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
        ListUI.UpdateFor(this, player);
        for (int i = 0; i < _fobs.Count; ++i)
        {
            if (_fobs[i] is FOB fob && fob.FriendliesNearby.Contains(player))
            {
                ResourceUI.SetValues(player.Connection, fob.BuildSupply, fob.AmmoSupply);
                break;
            }
        }
    }

    public void Dispose()
    {
        _configListener?.Dispose();
        if (_config is IDisposable disp)
        {
            disp.Dispose();
        }
    }
}

public class FOBConfigData : JSONConfigData
{
    public float FOBMaxHeightAboveTerrain { get; set; }
    public bool RestrictFOBPlacement { get; set; }
    public ushort FOBID { get; set; }
    public ushort FOBRequiredBuild { get; set; }
    public int FOBBuildPickupRadius { get; set; }
    public int FOBBuildPickupRadiusNoBunker { get; set; }
    public byte FobLimit { get; set; }
    public int TicketsFOBRadioLost { get; set; }
    public float BaseFOBRepairHits { get; set; }
    public float SalvageRefundPercentage { get; set; }
    public float RepairBuildDiscountPercentage { get; set; }

    public float AmmoCommandCooldown { get; set; }
    public ushort AmmoCrateRequiredBuild { get; set; }
    public ushort RepairStationRequiredBuild { get; set; }

    public List<BuildableData> Buildables { get; set; }
    public int AmmoBagMaxUses { get; set; }

    public float DeployMainDelay { get; set; }
    public float DeployFOBDelay { get; set; }

    public bool EnableCombatLogger { get; set; }
    public uint CombatCooldown { get; set; }

    public bool EnableDeployCooldown { get; set; }
    public uint DeployCooldown { get; set; }
    public bool DeployCancelOnMove { get; set; }
    public bool DeployCancelOnDamage { get; set; }

    public bool ShouldRespawnAtMain { get; set; }
    public bool ShouldWipeAllFOBsOnRoundedEnded { get; set; }
    public bool ShouldSendPlayersBackToMainOnRoundEnded { get; set; }
    public bool ShouldKillMaincampers { get; set; }

    public ushort FirstFOBUiId { get; set; }
    public ushort BuildResourceUI { get; set; }

    [JsonConverter(typeof(ByteArrayJsonConverter))]
    public byte[] T1RadioState { get; set; }

    [JsonConverter(typeof(ByteArrayJsonConverter))]
    public byte[] T2RadioState { get; set; }

    public override void SetDefaults()
    {
        FOBMaxHeightAboveTerrain = 25f;
        RestrictFOBPlacement = true;
        FOBRequiredBuild = 15;
        FOBBuildPickupRadius = 80;
        FOBBuildPickupRadiusNoBunker = 30;
        FobLimit = 10;
        TicketsFOBRadioLost = 20;
        // amount of hits it takes to full repair a radio. 30 dmg x 20 = 600 total hp
        BaseFOBRepairHits = 20;
        SalvageRefundPercentage = 75f;
        RepairBuildDiscountPercentage = 20f; // 20% off

        AmmoCrateRequiredBuild = 2;
        AmmoCommandCooldown = 120f;

        RepairStationRequiredBuild = 6;

        T1RadioState = Convert.FromBase64String("8yh8NQEAEAEAAAAAAAAAACIAAACmlQFkAAQAAKyVAWQAAAIArpUBZAAABQDGlQFkAAMFAMmVAWQABgUAyZUBZAAACACmZQFkAAMIAMCVAWQABggAwJUBZAAHAgDWlQFkAAoCANaVAWQABwMA1pUBZAAKAwDWlQFkAAcEANaVAWQACgQA1pUBZAAJBQDYlQFkAAkHANiVAWQACQkA2JUBZAAACwDOlQFkAAAMAM6VAWQAAA0AzpUBZAADCwDOlQFkAAcAAKyVAWQACgAA1pUBZAAKAQDWlQFkAAMMAM6VAWQAAw0AzpUBZAAGDQDQlQFkAAQCANqVAWQACQsA0JUBZAAJDADQlQFkAAkNANCVAWQABgsAzpUBZAAGDADOlQFkAA==");
        //T2RadioState = "8yh8NQEAEAEAAAAAAAAAACIAAACmlQFkAAQAAKyVAWQAAAIArpUBZAADCADAlQFkAAYIAMCVAWQABwIA1pUBZAAKAgDWlQFkAAcDANaVAWQACgMA1pUBZAAHBADWlQFkAAoEANaVAWQACQUA2JUBZAAJBwDYlQFkAAkJANiVAWQAAAsAzpUBZAAADADOlQFkAAANAM6VAWQAAwsAzpUBZAAHAACslQFkAAoAANaVAWQACgEA1pUBZAADDADOlQFkAAMNAM6VAWQABg0A0JUBZAAEAgDalQFkAAkLANCVAWQACQwA0JUBZAAJDQDQlQFkAAYLAM6VAWQABgwAzpUBZAAABQDDlQFkAAMFAMqVAWQABgUAypUBZAAACAC6ZQFkAA=="; // Russia/MEC
        T2RadioState = Convert.FromBase64String("8yh8NQEAEAEAAAAAAAAAACIAAACmlQFkAAQAAKyVAWQAAAIArpUBZAADCADAlQFkAAYIAMCVAWQABwIA1pUBZAAKAgDWlQFkAAcDANaVAWQACgMA1pUBZAAHBADWlQFkAAoEANaVAWQACQUA2JUBZAAJBwDYlQFkAAkJANiVAWQAAAsAzpUBZAAADADOlQFkAAANAM6VAWQAAwsAzpUBZAAHAACslQFkAAoAANaVAWQACgEA1pUBZAADDADOlQFkAAMNAM6VAWQABg0A0JUBZAAEAgDalQFkAAkLANCVAWQACQwA0JUBZAAJDQDQlQFkAAYLAM6VAWQABgwAzpUBZAAACAC6ZQFkAAAFANVlAWQAAwUAvmUBZAAGBQC+ZQFkAA==");

        AmmoBagMaxUses = 3;

        Buildables =
        [
            new BuildableData
            {
                FullBuildable = AssetLink.Create<ItemAsset>("61c349f10000498fa2b92c029d38e523"),
                Foundation = AssetLink.Create<ItemAsset>("1bb17277dd8148df9f4c53d1a19b2503"),
                Type = BuildableType.Bunker,
                RequiredHits = 30,
                RequiredBuild = 15,
                Team = 0,
                Limit = 1,
                Emplacement = null
            },
            new BuildableData
            {
                FullBuildable = AssetLink.Create<ItemAsset>("6fe208519d7c45b0be38273118eea7fd"),
                Foundation = AssetLink.Create<ItemAsset>("eccfe06e53d041d5b83c614ffa62ee59"),
                Type = BuildableType.AmmoCrate,
                RequiredHits = 10,
                RequiredBuild = 1,
                Team = 0,
                Limit = 6,
                Emplacement = null
            },
            new BuildableData
            {
                FullBuildable = AssetLink.Create<ItemAsset>("c0d11e0666694ddea667377b4c0580be"),
                Foundation = AssetLink.Create<ItemAsset>("26a6b91cd1944730a0f28e5f299cebf9"),
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
                FullBuildable = AssetLink.Create<ItemAsset>("ab702192eab4456ebb9f6d7cc74d4ba2"),
                Foundation = AssetLink.Create<ItemAsset>("15f674dcaf3f44e19a124c8bf7e19ca2"),
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
                FullBuildable = AssetLink.Create<ItemAsset>("f3bd9ee2fa334faabc8fd9d5a3b84424"),
                Foundation = AssetLink.Create<ItemAsset>("a9294335d8e84b76b1cbcb7d70f66aaa"),
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
                FullBuildable = AssetLink.Create<ItemAsset>("eefee76f077349e58359f5fd03cf311d"),
                Foundation = AssetLink.Create<ItemAsset>("920f8b30ae314406ab032a0c2efa753d"),
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
                FullBuildable = AssetLink.Create<ItemAsset>("a71e3e3d6bb54a36b7bd8bf5f25160aa"),
                Foundation = AssetLink.Create<ItemAsset>("12ea830dd9ab4f949893bbbbc5e9a5f6"),
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
                FullBuildable = AssetLink.Create<ItemAsset>("bc24bd85ff714ff7bb2f8b2dd5056395"),
                Foundation = AssetLink.Create<ItemAsset>("a2a8a01a58454816a6c9a047df0558ad"),
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
                FullBuildable = AssetLink.Create<ItemAsset>("e1af3a3af31e4996bc5d6ffd9a0773ec"),
                Foundation = AssetLink.Create<ItemAsset>("baf23a8b514441ee8db891a3ddf32ef4"),
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
                FullBuildable = AssetLink.Create<ItemAsset>("857c85161f254964a921700a69e215a9"),
                Foundation = AssetLink.Create<ItemAsset>("827d0ca8bfff43a39f750f191e16ea71"),
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
                FullBuildable = AssetLink.Create<ItemAsset>(Guid.Empty),
                Foundation = AssetLink.Create<ItemAsset>("80396c361d3040d7beb3921964ec2997"),
                Type = BuildableType.Emplacement,
                RequiredHits = 16,
                RequiredBuild = 6,
                Team = 1,
                Limit = 2,
                Emplacement = new EmplacementData
                {
                    EmplacementVehicle = AssetLink.Create<VehicleAsset>("aa3c6af4911243b5b5c9dc95ca1263bf"),
                    BaseBarricade =  AssetLink.Create<ItemAsset>(Guid.Empty),
                    Ammo = AssetLink.Create<ItemAsset>("523c49ce4df44d46ba37be0dd6b4504b"),
                    AmmoCount = 2
                }
            },
            new BuildableData
            {
                // Kord
                FullBuildable = AssetLink.Create<ItemAsset>(Guid.Empty),
                Foundation = AssetLink.Create<ItemAsset>("e44ba62f763c432e882ddc7eabaa9c77"),
                Type = BuildableType.Emplacement,
                RequiredHits = 16,
                RequiredBuild = 6,
                Team = 2,
                Limit = 2,
                Emplacement = new EmplacementData
                {
                    EmplacementVehicle = AssetLink.Create<VehicleAsset>("86cfe1eb8be144aeae7659c9c74ff11a"),
                    BaseBarricade = AssetLink.Create<ItemAsset>(Guid.Empty),
                    Ammo = AssetLink.Create<ItemAsset>("6e9bc2083a1246b49b1656c2ec6f535a"),
                    AmmoCount = 2,
                }
            },
            new BuildableData
            {
                // QJC-88
                FullBuildable = AssetLink.Create<ItemAsset>(Guid.Empty),
                Foundation = AssetLink.Create<ItemAsset>("beaa260d7f844724bd26993569d9e42a"),
                Type = BuildableType.Emplacement,
                RequiredHits = 16,
                RequiredBuild = 6,
                Team = 2,
                Limit = 2,
                Emplacement = new EmplacementData
                {
                    EmplacementVehicle = AssetLink.Create<VehicleAsset>("9525ef43b9674343bb9561b5db078c1b"),
                    BaseBarricade = AssetLink.Create<ItemAsset>(Guid.Empty),
                    Ammo = AssetLink.Create<ItemAsset>("6e9bc2083a1246b49b1656c2ec6f535a"),
                    AmmoCount = 2,
                }
            },
            new BuildableData
            {
                // TOW
                FullBuildable = AssetLink.Create<ItemAsset>(Guid.Empty),
                Foundation = AssetLink.Create<ItemAsset>("a68ae466fb804829a0eb0d4556071801"),
                Type = BuildableType.Emplacement,
                RequiredHits = 25,
                RequiredBuild = 14,
                Team = 1,
                Limit = 1,
                Emplacement = new EmplacementData
                {
                    EmplacementVehicle = AssetLink.Create<VehicleAsset>("9d305050a6a142349376d6c49fb38362"),
                    BaseBarricade = AssetLink.Create<ItemAsset>(Guid.Empty),
                    Ammo = AssetLink.Create<ItemAsset>("3128a69d06ac4bbbbfddc992aa7185a6"),
                    AmmoCount = 1
                }
            },
            new BuildableData
            {
                // Kornet
                FullBuildable = AssetLink.Create<ItemAsset>(Guid.Empty),
                Foundation = AssetLink.Create<ItemAsset>("37811b1847744c958fcb30a0b759874b"),
                Type = BuildableType.Emplacement,
                RequiredHits = 25,
                RequiredBuild = 14,
                Team = 2,
                Limit = 1,
                Emplacement = new EmplacementData
                {
                    EmplacementVehicle = AssetLink.Create<VehicleAsset>("677b1084-dffa-4633-84d2-9167a3fae25b"),
                    BaseBarricade = AssetLink.Create<ItemAsset>(Guid.Empty),
                    Ammo = AssetLink.Create<ItemAsset>("d7774b017c404adbb0a0fe8e902b9689"),
                    AmmoCount = 1
                }
            },
            new BuildableData
            {
                // HJ-8
                FullBuildable = AssetLink.Create<ItemAsset>(Guid.Empty),
                Foundation = AssetLink.Create<ItemAsset>("f76572f13c214a138415f20bbc1a31c3"),
                Type = BuildableType.Emplacement,
                RequiredHits = 25,
                RequiredBuild = 14,
                Team = 2,
                Limit = 1,
                Emplacement = new EmplacementData
                {
                    EmplacementVehicle = AssetLink.Create<VehicleAsset>("b63e9c2999c34ff3894138592dd9cb2e"),
                    BaseBarricade = AssetLink.Create<ItemAsset>(Guid.Empty),
                    Ammo = AssetLink.Create<ItemAsset>("9ba5db5cbd2c4b51bc236122a4c6b205"),
                    AmmoCount = 1
                }
            },
            new BuildableData
            {
                // Stinger
                FullBuildable = AssetLink.Create<ItemAsset>(Guid.Empty),
                Foundation = AssetLink.Create<ItemAsset>("3c2dd7febc854b7f8859852b8c736c8e"),
                Type = BuildableType.Emplacement,
                RequiredHits = 25,
                RequiredBuild = 14,
                Team = 2,
                Limit = 1,
                Emplacement = new EmplacementData
                {
                    EmplacementVehicle = AssetLink.Create<VehicleAsset>("1883345cbdad40aa81e49c84e6c872ef"),
                    BaseBarricade = AssetLink.Create<ItemAsset>(Guid.Empty),
                    Ammo = AssetLink.Create<ItemAsset>("3c0a94af5af24901a9e3207f3e9ed0ba"),
                    AmmoCount = 1
                }
            },
            new BuildableData
            {
                // Igla
                FullBuildable = AssetLink.Create<ItemAsset>(Guid.Empty),
                Foundation = AssetLink.Create<ItemAsset>("b50cb548734946ffa5f88d6691a2c7ce"),
                Type = BuildableType.Emplacement,
                RequiredHits = 25,
                RequiredBuild = 14,
                Limit = 1,
                Emplacement = new EmplacementData
                {
                    EmplacementVehicle = AssetLink.Create<VehicleAsset>("8add59a2e2b94f93ab0d6b727d310097"),
                    BaseBarricade = AssetLink.Create<ItemAsset>(Guid.Empty),
                    Ammo = AssetLink.Create<ItemAsset>("a54d571983c2432a9624eec39d602997"),
                    AmmoCount = 1
                }
            },
            new BuildableData
            {
                // Mortar
                FullBuildable = AssetLink.Create<ItemAsset>(Guid.Empty),
                Foundation = AssetLink.Create<ItemAsset>("6ff4826eaeb14c7cac1cf25a55d24bd3"),
                Type = BuildableType.Emplacement,
                RequiredHits = 22,
                RequiredBuild = 10,
                Team = 0,
                Limit = 2,
                Emplacement = new EmplacementData
                {
                    EmplacementVehicle = AssetLink.Create<VehicleAsset>("94bf8feb05bc4680ac26464bc175460c"),
                    BaseBarricade = AssetLink.Create<ItemAsset>("c3eb4dd3fd1d463993ec69c4c3de50d7"), // Mortar
                    Ammo = AssetLink.Create<ItemAsset>("66f4c76a119e4d6ca9d0b1a866c4d901"),
                    AmmoCount = 3,
                    ShouldWarnFriendlies = true,
                    ShouldWarnEnemies = true
                }
            }
        ];

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
    public IAssetLink<ItemAsset> Foundation { get; set; }

    [JsonPropertyName("structureID")]
    public IAssetLink<ItemAsset>? FullBuildable { get; set; }

    [JsonPropertyName("type")]
    public BuildableType Type { get; set; }

    [JsonPropertyName("requiredHits")]
    public int RequiredHits { get; set; }

    [JsonPropertyName("requiredBuild")]
    public int RequiredBuild { get; set; }

    [JsonPropertyName("team")]
    public int Team { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("disabled")]
    public bool Disabled { get; set; }

    [JsonPropertyName("emplacementData")]
    public EmplacementData? Emplacement { get; set; }

    [JsonPropertyName("dontAutoWhitelist")]
    public bool DontAutoWhitelist { get; set; }

    public string Translate(LanguageInfo language, string? format, UCPlayer? target, CultureInfo? culture,
        ref TranslationFlags flags)
    {
        if (Emplacement is not null && Emplacement.EmplacementVehicle.TryGetAsset(out VehicleAsset? vasset))
        {
            string name = vasset.vehicleName;
            if (format is not null && format.Equals(T.FormatRarityColor))
                return Localization.Colorize(ColorUtility.ToHtmlStringRGB(ItemTool.getRarityColorUI(vasset.rarity)), name, flags);

            return name;
        }

        if (Foundation.TryGetAsset(out ItemAsset? iasset) || FullBuildable.TryGetAsset(out iasset))
        {
            string name = GetItemName(iasset.itemName);
            if (format is not null && format.Equals(T.FormatRarityColor))
                return Localization.Colorize(ColorUtility.ToHtmlStringRGB(ItemTool.getRarityColorUI(iasset.rarity)), name, flags);
            else
                return name;
        }

        if (Emplacement is not null)
        {
            if (Emplacement.BaseBarricade.TryGetAsset(out iasset))
            {
                string name = GetItemName(iasset.itemName);
                if (format is not null && format.Equals(T.FormatRarityColor))
                    return Localization.Colorize(ColorUtility.ToHtmlStringRGB(ItemTool.getRarityColorUI(iasset.rarity)), name, flags);
                else
                    return name;
            }
            if (Emplacement.Ammo.TryGetAsset(out iasset))
            {
                string name = GetItemName(iasset.itemName);
                if (format is not null && format.Equals(T.FormatRarityColor))
                    return Localization.Colorize(ColorUtility.ToHtmlStringRGB(ItemTool.getRarityColorUI(iasset.rarity)), name, flags);
                else
                    return name;
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
        return Translate(Localization.GetDefaultLanguage(), null, null, Data.LocalLocale, ref flags);
    }
}

public class EmplacementData
{
    [JsonPropertyName("vehicleID")]
    public IAssetLink<VehicleAsset> EmplacementVehicle { get; set; }

    [JsonPropertyName("baseID")]
    public IAssetLink<ItemAsset> BaseBarricade { get; set; }

    [JsonPropertyName("ammoID")]
    public IAssetLink<ItemAsset> Ammo { get; set; }

    [JsonPropertyName("ammoAmount")]
    public int AmmoCount { get; set; }

    [JsonPropertyName("warnFriendlyProjectiles")]
    public bool ShouldWarnFriendlies { get; set; }

    [JsonPropertyName("warnEnemyProjectiles")]
    public bool ShouldWarnEnemies { get; set; }
}

[Translatable("Buildable Type", Description = "Buildable types for FOB buildings.")]
public enum BuildableType
{
    [Translatable(Languages.ChineseSimplified, "地堡")]
    Bunker,
    [Translatable(Languages.ChineseSimplified, "弹药箱")]
    AmmoCrate,
    [Translatable(Languages.ChineseSimplified, "维修站")]
    RepairStation,
    [Translatable(Languages.ChineseSimplified, "防御")]
    [Translatable("Fortification", Description = "Barricade or Structure buildables that do not fall into another category.")]
    Fortification,
    [Translatable(Languages.ChineseSimplified, "架设")]
    [Translatable("Emplacement", Description = "Vehicle buildables.")]
    Emplacement,
    [Translatable(Languages.ChineseSimplified, "电台")]
    Radio
}
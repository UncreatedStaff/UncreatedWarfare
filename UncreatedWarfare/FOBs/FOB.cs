using JetBrains.Annotations;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Globalization;
using Uncreated.Framework;
using Uncreated.SQL;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Levels;
using Uncreated.Warfare.Locations;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Singletons;
using Uncreated.Warfare.Structures;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using XPReward = Uncreated.Warfare.Levels.XPReward;

namespace Uncreated.Warfare.FOBs;
public sealed class FOB : MonoBehaviour, IRadiusFOB, IResourceFOB, IGameTickListener, IPlayerDisconnectListener
{
    private const int ResourcesRewardLimit = 60;

    private static readonly List<RegionCoordinate> RegionBuffer = new List<RegionCoordinate>(4);
    private static readonly List<KeyValuePair<ItemData, float>> SupplyBuffer = new List<KeyValuePair<ItemData, float>>(32);

    private readonly List<IFOBItem> _items = new List<IFOBItem>();
    private readonly List<UCPlayer> _friendlies = new List<UCPlayer>(16);
    private BunkerComponent? _bunker;
    private ItemBarricadeAsset _originalRadio = null!;
    private ushort _buildItemId;
    private ushort _ammoItemId;
    private byte[] _originalState;
    private bool _destroyed;
    private bool _isBeingDestroyed;

    public IReadOnlyList<IFOBItem> Items { get; }
    public IReadOnlyList<UCPlayer> FriendliesNearby { get; }
    public string Name { get; internal set; }
    public int Number { get; internal set; }
    public string ClosestLocation { get; private set; }
    UCPlayer? IFOB.Instigator { get; set; }
    public GridLocation GridLocation { get; private set; }
    public RadioComponent Radio { get; internal set; }
    public BunkerComponent? Bunker
    {
        get => _bunker;
        internal set
        {
            if (ReferenceEquals(_bunker, value))
                return;
            _bunker = value;
            Radius = value == null
                ? FOBManager.Config.FOBBuildPickupRadiusNoBunker
                : FOBManager.Config.FOBBuildPickupRadius;
            FOBManager.UpdateFOBListForTeam(Team, this);
            L.LogDebug($"[FOBS] [{Name}] Radius Updated: {Radius}m.");
        }
    }

    Vector3 IDeployable.Position => Bunker == null ? Vector3.zero : Bunker.SpawnPosition;
    float IDeployable.Yaw => Bunker == null ? 0f : Bunker.SpawnYaw;
    public ulong Team => Radio.Team;
    public float Radius { get; private set; }

    public bool Bleeding => Radio.State == RadioComponent.RadioState.Bleeding;
    public float ProxyScore { get; private set; }
    public bool IsProxied => ProxyScore >= 1f;
    public ulong Owner => Radio.Owner;
    public int BuildSupply { get; private set; }
    public int AmmoSupply { get; private set; }
    public bool BeingDestroyed => _isBeingDestroyed;
    public string UIResourceString => Bleeding ? string.Empty : BuildSupply.ToString(Data.LocalLocale).Colorize("d4c49d") + " " + AmmoSupply.ToString(Data.LocalLocale).Colorize("b56e6e");

    /// <summary>
    /// Checks for limitations for non-floating objects. Don't use this for radios.
    /// </summary>
    public bool ValidatePlacement(BuildableData buildable, UCPlayer player, IFOBItem? ignoreFoundation)
    {
        if (buildable.Type == BuildableType.Bunker && Bunker != null)
        {
            player.SendChat(T.BuildTickStructureExists, buildable);
            return false;
        }

        int limit = buildable.Limit;
        for (int i = 0; i < Items.Count; ++i)
        {
            IFOBItem item = Items[i];
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

    [UsedImplicitly]
    private FOB()
    {
        Items = _items.AsReadOnly();
        FriendliesNearby = _friendlies.AsReadOnly();
    }

    [UsedImplicitly]
    private void Awake()
    {
        ClosestLocation = F.GetClosestLocationName(transform.position, true, true);
        GridLocation = new GridLocation(transform.position);
        Radius = FOBManager.Config.FOBBuildPickupRadiusNoBunker;
    }
    private void Start()
    {
        if (Radio == null)
        {
            L.LogWarning($"[FOBS] [{Name}] FOB created without setting radio.");
            Destroy(this);
            return;
        }
        if (Radio.State != RadioComponent.RadioState.Alive)
        {
            L.LogWarning($"[FOBS] [{Name}] FOB created with damaged or destroyed radio.");
            Destroy(this);
            return;
        }

        if (Bunker != null)
            Radius = FOBManager.Config.FOBBuildPickupRadius;

        _originalRadio = Radio.Barricade.asset;

        Radio.FOB = this;
        _originalState = Radio.Barricade.GetServersideData().barricade.state;

        ItemAsset? build = TeamManager.GetRedirectInfo(RedirectType.BuildSupply, null, TeamManager.GetFactionSafe(Team), out _, out _);
        ItemAsset? ammo = TeamManager.GetRedirectInfo(RedirectType.AmmoSupply, null, TeamManager.GetFactionSafe(Team), out _, out _);
        if (build != null)
            _buildItemId = build.id;
        if (ammo != null)
            _ammoItemId = ammo.id;

        if (Bunker != null)
            Bunker.FOB = this;

        if (Radio.Icon.ValidReference(out Guid radioEffect))
            IconManager.AttachIcon(radioEffect, transform, Team, 3.5f);

        L.LogDebug($"[FOBS] [{Name}] Initialized FOB: {Radio.Barricade.asset.itemName} (Radio State: {Radio.State})");

        OffloadNearbyLogisticsVehicle();
    }
    [UsedImplicitly]
    private void OnDestroy()
    {
        if (!_destroyed)
        {
            _destroyed = false;
            Destroy();
        }
    }
    public void ModifyBuild(int delta)
    {
        BuildSupply += delta;
        UpdateResourceUI(true, false);
    }
    public void ModifyAmmo(int delta)
    {
        AmmoSupply += delta;
        UpdateResourceUI(false, true);
    }
    public void ModifyResources(int buildDelta, int ammoDelta)
    {
        BuildSupply += buildDelta;
        AmmoSupply += ammoDelta;
        UpdateResourceUI(true, true);
    }
    public void Restock()
    {
        if (Team is not 1 and not 2)
            return;
        byte[] state = Team == 1 ? FOBManager.Config.T1RadioState : FOBManager.Config.T2RadioState;
        Radio.Barricade.GetServersideData().barricade.state = state;
        Radio.Barricade.ReceiveUpdateState(state);
        Vector3 pos = transform.position;
        if (Gamemode.Config.EffectUnloadBuild.ValidReference(out EffectAsset asset))
            F.TriggerEffectReliable(asset, 40, pos);
        // refresh inventory
        for (int i = 0; i < _friendlies.Count; ++i)
        {
            if (_friendlies[i].Player.inventory.storage == Radio.Barricade.interactable)
                _friendlies[i].Player.inventory.sendStorage();
        }
    }
    public string GetUIColor()
    {
        string key;
        if (Bleeding)
            key = "bleeding_fob_color";
        else if (Bunker == null)
            key = "no_bunker_fob_color";
        else if (IsProxied)
            key = "enemy_nearby_fob_color";
        else
            key = "default_fob_color";
        return UCWarfare.GetColorHex(key);
    }
    public float GetProxyScore(UCPlayer enemy)
    {
        ThreadUtil.assertIsGameThread();

        if (Bunker == null)
            return 0;

        float distanceFromBunker = (enemy.Position - Bunker.SpawnPosition).magnitude;

        if (distanceFromBunker > 80)
            return 0;

        return 12.5f / distanceFromBunker;
    }
    public void RegisterItem(IFOBItem item)
    {
        item.FOB = this;
        _items.Add(item);
        L.LogDebug($"[FOBS] [{Name}] Registered item: {item.Buildable}.");
    }
    public void UpdateRadioState(RadioComponent.RadioState state)
    {
        ThreadUtil.assertIsGameThread();

        switch (state)
        {
            case RadioComponent.RadioState.Destroyed:
                Destroy();
                break;
            case RadioComponent.RadioState.Alive:
                if (Radio.State == RadioComponent.RadioState.Alive)
                    return;
                Barricade b = new Barricade(_originalRadio, _originalRadio.health, _originalState ??= FOBManager.GetRadioState(Team));
                Transform t;
                FOBManager.IgnorePlacingBarricade = true;
                try
                {
                    t = BarricadeManager.dropNonPlantedBarricade(b, transform.position,
                        transform.rotation, Radio.Owner, TeamManager.GetGroupID(Radio.Team));
                }
                finally
                {
                    FOBManager.IgnorePlacingBarricade = false;
                }
                
                if (t == null)
                {
                    L.LogWarning($"[FOBS] [{Name}] Failed to place barricade {_originalRadio.itemName}.");
                    return;
                }
                Destroy(Radio);
                Radio = t.gameObject.AddComponent<RadioComponent>();
                Radio.FOB = this;
                break;
            case RadioComponent.RadioState.Bleeding:
                if (Radio.State == RadioComponent.RadioState.Bleeding)
                    return;
                if (!Gamemode.Config.BarricadeFOBRadioDamaged.ValidReference(out ItemBarricadeAsset damagedFobRadio))
                {
                    L.LogWarning($"[FOBS] [{Name}] Can't replace with damaged fob, asset not found from config.");
                    return;
                }

                ulong oldKiller = 0ul;
                float oldKillerTime = 0f;
                BarricadeComponent component;
                if (Radio.State == RadioComponent.RadioState.Alive)
                {
                    _originalState = Util.CloneBytes(Radio.Barricade.GetServersideData().barricade.state);
                    if (Radio.Barricade.model.TryGetComponent(out component))
                    {
                        oldKiller = component.LastDamager;
                        oldKillerTime = component.LastDamagerTime;
                    }
                }

                b = new Barricade(damagedFobRadio, damagedFobRadio.health, Array.Empty<byte>());
                FOBManager.IgnorePlacingBarricade = true;
                try
                {
                    t = BarricadeManager.dropNonPlantedBarricade(b, transform.position,
                        transform.rotation, Radio.Owner, TeamManager.GetGroupID(Radio.Team));
                }
                finally
                {
                    FOBManager.IgnorePlacingBarricade = false;
                }

                if (t == null)
                {
                    L.LogWarning($"[FOBS] [{Name}] Failed to place barricade {damagedFobRadio.itemName}.");
                    return;
                }

                if (t.TryGetComponent(out component))
                {
                    component.LastDamager = oldKiller;
                    component.LastDamagerTime = oldKillerTime;
                }
                Destroy(Radio);
                Radio = t.gameObject.AddComponent<RadioComponent>();
                Radio.FOB = this;
                break;
            default:
                L.LogWarning($"[FOBS] [{Name}] Unknown radio state: {state}.");
                return;
        }
        L.LogDebug($"[FOBS] [{Name}] Updated radio state: {state}.");
    }
    public void Destroy()
    {
        ThreadUtil.assertIsGameThread();
        _isBeingDestroyed = true;
        try
        {
            for (int i = _items.Count - 1; i >= 0; --i)
            {
                try
                {
                    if (_items[i] is MonoBehaviour mb)
                        Destroy(mb);
                    if (_items[i] is IDisposable d)
                        d.Dispose();
                }
                catch (Exception ex)
                {
                    L.LogError($"[FOBS] [{Name}] Error destroying FOB item: {_items[i]}.");
                    L.LogError(ex);
                }
                finally
                {
                    _items.RemoveAt(i);
                }
            }

            if (Bunker != null)
            {
                try
                {
                    Destroy(Bunker);
                }
                catch (Exception ex)
                {
                    L.LogError($"[FOBS] [{Name}] Error destroying FOB Bunker.");
                    L.LogError(ex);
                }
                finally
                {
                    Bunker = null;
                }
            }

            if (Radio != null)
            {
                try
                {
                    Destroy(Radio);
                }
                catch (Exception ex)
                {
                    L.LogError($"[FOBS] [{Name}] Error destroying FOB Radio.");
                    L.LogError(ex);
                }
                finally
                {
                    Radio = null!;
                }
            }

            if (!_destroyed)
                Destroy(gameObject);
            _destroyed = true;
            L.LogDebug($"[FOBS] [{Name}] Destroyed.");
            FOBManager.EnsureDisposed(this);
        }
        catch (Exception ex)
        {
            L.LogError($"[FOBS] [{Name}] Error destroying.");
            L.LogError(ex);
        }
        finally
        {
            _isBeingDestroyed = false;
        }
    }
    public void OffloadNearbyLogisticsVehicle()
    {
        ThreadUtil.assertIsGameThread();
        
        InteractableVehicle? nearestLogi = UCVehicleManager.GetNearestLogi(transform.position, FOBManager.Config.FOBBuildPickupRadiusNoBunker, Team);
        if (nearestLogi != null)
        {
            ulong delivererId = Owner;
            if (nearestLogi.transform.TryGetComponent(out VehicleComponent component))
            {
                component.Quota += 5;
                delivererId = component.LastDriver;
            }

            if (nearestLogi.isDriven)
                return;
            
            int buildRemoved = 0;
            int ammoRemoved = 0;

            foreach (ItemJar item in nearestLogi.trunkItems.EnumerateInOrder())
            {
                bool shouldRemove = false;
                if (item.item.id == _buildItemId && buildRemoved < 16)
                {
                    shouldRemove = true;
                    buildRemoved++;
                }
                if (item.item.id == _ammoItemId && ammoRemoved < 12)
                {
                    shouldRemove = true;
                    ammoRemoved++;
                }
                if (shouldRemove)
                {
                    ItemManager.dropItem(item.item, nearestLogi.transform.position, false, true, true);
                    nearestLogi.trunkItems.removeItem(nearestLogi.trunkItems.getIndex(item.x, item.y));
                }
            }

            UCPlayer? deliverer = UCPlayer.FromID(delivererId) ?? (delivererId != Owner ? UCPlayer.FromID(Owner) : null);
            if (deliverer != null)
            {
                int groupsUnloaded = (buildRemoved + ammoRemoved) / 6;
                if (groupsUnloaded > 0 && Points.PointsConfig.XPData.TryGetValue(XPReward.UnloadSupplies, out PointsConfig.XPRewardData data) &&
                    data.Amount != 0)
                {
                    int xp = data.Amount;
                    if (deliverer.KitClass == Class.Pilot)
                        xp *= 2;
                    Points.AwardXP(deliverer, XPReward.UnloadSupplies, groupsUnloaded * xp);
                }
            }
        }
    }
    private void TryConsumeResources()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Vector3 pos = transform.position;
        Regions.getRegionsInRadius(pos, Radius, RegionBuffer);
        try
        {
            float sqrRad = Radius;
            sqrRad *= sqrRad;
            for (int r = 0; r < RegionBuffer.Count; r++)
            {
                RegionCoordinate rc = RegionBuffer[r];
                ItemRegion region = ItemManager.regions[rc.x, rc.y];
                for (int j = 0; j < region.items.Count; j++)
                {
                    ItemData item = region.items[j];
                    if ((item.item.id == _ammoItemId || item.item.id == _buildItemId))
                    {
                        float d = (item.point - pos).sqrMagnitude;
                        if (d > sqrRad)
                            continue;
                        bool f = false;
                        for (int i = 0; i < SupplyBuffer.Count; ++i)
                        {
                            if (SupplyBuffer[i].Value < sqrRad)
                            {
                                f = true;
                                SupplyBuffer.Insert(i, new KeyValuePair<ItemData, float>(item, d));
                                break;
                            }
                        }

                        if (!f)
                            SupplyBuffer.Add(new KeyValuePair<ItemData, float>(item, d));
                    }
                }
            }


            int counter = 0;
            int buildCount = 0, ammoCount = 0;
            for (int i = 0; i < SupplyBuffer.Count; ++i)
            {
                ItemData item = SupplyBuffer[i].Key;
                if (!EventFunctions.DroppedItemsOwners.TryGetValue(item.instanceID, out ulong playerID))
                    continue;

                UCPlayer? player = UCPlayer.FromID(playerID);
                if (player != null)
                {
                    ++player.SuppliesUnloaded;
                    if (player.SuppliesUnloaded >= 6 &&
                        Points.PointsConfig.XPData.TryGetValue(XPReward.UnloadSupplies, out PointsConfig.XPRewardData data) && data.Amount != 0)
                    {
                        int xp = data.Amount;

                        if (player.KitClass == Class.Pilot)
                            xp *= 2;

                        QuestManager.OnSuppliesConsumed(this, playerID, player.SuppliesUnloaded);

                        InteractableVehicle? vehicle = player.Player.movement.getVehicle();
                        if (vehicle != null && vehicle.transform.TryGetComponent(out VehicleComponent component))
                            component.Quota += 1f / 3f;

                        if (BuildSupply + AmmoSupply <= ResourcesRewardLimit)
                            Points.AwardXP(player, XPReward.UnloadSupplies, xp);

                        player.SuppliesUnloaded = 0;
                    }
                }

                if (item.item.id == _buildItemId)
                    ++buildCount;
                else if (item.item.id == _ammoItemId)
                    ++ammoCount;
                counter++;
                if (counter >= 3)
                    break;
            }

            bool update = false;
            Vector3 pt = pos;
            if (buildCount > 0)
            {
                BuildSupply += buildCount;
                int index = 0;
                while (buildCount > 0 && index < SupplyBuffer.Count)
                {
                    ItemData d = SupplyBuffer[index].Key;
                    if (index == 0)
                        pt = d.point;
                    if (d.item.id == _buildItemId)
                        d.Destroy();
                    ++index;
                    --buildCount;
                }
                if (Gamemode.Config.EffectUnloadBuild.ValidReference(out EffectAsset effect))
                    F.TriggerEffectReliable(effect, EffectManager.MEDIUM, pt);
                UpdateResourceUI(true, false, false);
                update = true;
            }
            if (ammoCount > 0)
            {
                AmmoSupply += ammoCount;
                int index = 0;
                while (ammoCount > 0 && index < SupplyBuffer.Count)
                {
                    ItemData d = SupplyBuffer[index].Key;
                    if (index == 0)
                        pt = d.point;
                    if (d.item.id == _ammoItemId)
                        d.Destroy();
                    ++index;
                    --ammoCount;
                }
                if (Gamemode.Config.EffectUnloadAmmo.ValidReference(out EffectAsset effect))
                    F.TriggerEffectReliable(effect, EffectManager.MEDIUM, pt);
                UpdateResourceUI(false, true, false);
                update = true;
            }
            if (update)
                FOBManager.UpdateFOBListForTeam(Team, this, true);
        }
        finally
        {
            RegionBuffer.Clear();
            SupplyBuffer.Clear();
        }
    }
    public void UpdateResourceUI(bool build, bool ammo, bool foblist = true)
    {
        ThreadUtil.assertIsGameThread();
        if (!build && !ammo)
            return;
        if (foblist)
            FOBManager.UpdateFOBListForTeam(Team, this, true);
        for (int i = 0; i < FriendliesNearby.Count; ++i)
        {
            UCPlayer player = FriendliesNearby[i];
            if (build)
                FOBManager.ResourceUI.BuildLabel.SetText(player.Connection, BuildSupply.ToString(player.Culture));
            if (ammo)
                FOBManager.ResourceUI.AmmoLabel.SetText(player.Connection, AmmoSupply.ToString(player.Culture));
        }
    }
    public void ShowResourceUI(UCPlayer player)
    {
        FOBManager.ResourceUI.SendToPlayer(player.Connection);
        FOBManager.ResourceUI.BuildLabel.SetText(player.Connection, BuildSupply.ToString(player.Culture));
        FOBManager.ResourceUI.AmmoLabel.SetText(player.Connection, AmmoSupply.ToString(player.Culture));
    }
    public void HideResourceUI(UCPlayer player)
    {
        FOBManager.ResourceUI.ClearFromPlayer(player.Connection);
    }
    private void OnPlayerEnteredRadius(UCPlayer player)
    {
        L.LogDebug($"[FOBS] [{Name}] Player entered FOB: {player}.");
        ShowResourceUI(player);

        InteractableVehicle? vehicle = player.Player.movement.getVehicle();
        if (vehicle == null)
            return;
        if (vehicle.TryGetComponent(out VehicleComponent comp) && comp.Data?.Item != null && VehicleData.IsLogistics(comp.Data.Item.Type))
            Tips.TryGiveTip(player, 120, T.TipUnloadSupplies);
    }
    private void OnPlayerLeftRadius(UCPlayer player)
    {
        L.LogDebug($"[FOBS] [{Name}] Player left FOB: {player}.");
        HideResourceUI(player);
    }
    internal void EnsureDisposed(IFOBItem item)
    {
        _items.RemoveAll(x => x.Equals(item));
        if (item.Equals(Bunker))
            Bunker = null;
        if (item.Equals(Radio))
        {
            Radio = null!;
            if (item is RadioComponent r && r.State == RadioComponent.RadioState.Bleeding)
                Destroy();
        }
    }
    public bool ContainsBuildable(IBuildable buildable)
    {
        if (Radio != null && Radio.Barricade.instanceID == buildable.InstanceId &&
            buildable.Type == StructType.Barricade)
            return true;
        if (Bunker != null && (Bunker.ActiveStructure.BuildableEquals(buildable) || Bunker.Base.BuildableEquals(buildable)))
            return true;
        for (int i = 0; i < _items.Count; ++i)
        {
            if (_items[i] is ShovelableComponent sh &&
                (sh.ActiveStructure.BuildableEquals(buildable) || sh.Base.BuildableEquals(buildable)))
                return true;
        }

        return false;
    }
    public bool ContainsVehicle(InteractableVehicle vehicle)
    {
        if (Bunker != null && Bunker.ActiveVehicle != null && Bunker.ActiveVehicle.instanceID == vehicle.instanceID)
            return true;
        for (int i = 0; i < _items.Count; ++i)
        {
            if (_items[i] is ShovelableComponent sh && sh.ActiveVehicle != null && sh.ActiveVehicle.instanceID == vehicle.instanceID)
                return true;
        }

        return false;
    }
    internal IFOBItem? UpgradeItem(IFOBItem item, Transform newObj)
    {
        if (item is not MonoBehaviour itemMb)
        {
            for (int i = 0; i < _items.Count; ++i)
            {
                if (_items[i].Equals(item))
                {
                    if (_items[i] is IDisposable d)
                        d.Dispose();
                    _items[i] = (IFOBItem)Activator.CreateInstance(item.GetType());
                    _items[i].FOB = this;
                    return _items[i];
                }
            }

            return null;
        }
        if (itemMb is BunkerComponent b && b == Bunker)
        {
            Bunker = newObj.gameObject.AddComponent<BunkerComponent>();
            Bunker.FOB = this;
            Destroy(b);
            return Bunker;
        }

        for (int i = 0; i < _items.Count; ++i)
        {
            if (_items[i] is MonoBehaviour mb && mb == itemMb)
            {
                _items[i] = (IFOBItem)newObj.gameObject.AddComponent(item.GetType());
                _items[i].FOB = this;
                Destroy(itemMb);
                return _items[i];
            }
        }

        return null;
    }
    float IDeployable.GetDelay() => FOBManager.Config.DeployFOBDelay;
    bool IDeployable.CheckDeployable(UCPlayer player, CommandInteraction? ctx)
    {
        if (Bunker == null || Bunker.ActiveStructure?.Model == null)
        {
            if (ctx is not null)
                throw ctx.Reply(T.DeployNoBunker, this);
            return false;
        }
        if (Bleeding)
        {
            if (ctx is not null)
                throw ctx.Reply(T.DeployRadioDamaged, this);
            return false;
        }
        if (IsProxied)
        {
            if (ctx is not null)
                throw ctx.Reply(T.DeployEnemiesNearby, this);
            return false;
        }

        return false;
    }
    bool IDeployable.CheckDeployableTick(UCPlayer player, bool chat)
    {
        if (Bunker == null || Bunker.ActiveStructure?.Model == null)
        {
            if (chat)
                player.SendChat(T.DeployNoBunker, this);
            return false;
        }
        if (Bleeding)
        {
            if (chat)
                player.SendChat(T.DeployRadioDamaged, this);
            return false;
        }
        if (IsProxied)
        {
            if (chat)
                player.SendChat(T.DeployEnemiesNearbyTick, this);
            return false;
        }

        return false;
    }
    void IDeployable.OnDeploy(UCPlayer player, bool chat)
    {
        ActionLog.Add(ActionLogType.DeployToLocation, "FOB BUNKER " + Name + " TEAM " + TeamManager.TranslateName(Team, 0), player);
        if (chat)
            player.SendChat(T.DeploySuccess, this);

        Points.TryAwardFOBCreatorXP(this, XPReward.BunkerDeployment);

        if (Bunker != null)
            QuestManager.OnPlayerSpawnedAtBunker(Bunker, player);
        L.LogDebug($"[FOBS] [{Name}] {player} deployed to bunker.");
    }
    void IGameTickListener.Tick()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Vector3 pos = transform.position;
        float proxyScore = 0;
        float sqrRad = Radius;
        sqrRad *= sqrRad;
        for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
        {
            UCPlayer player = PlayerManager.OnlinePlayers[i];
            if (player.GetTeam() == Team)
            {
                if ((player.Position - pos).sqrMagnitude < sqrRad)
                {
                    if (!_friendlies.Contains(player))
                    {
                        _friendlies.Add(player);
                        OnPlayerEnteredRadius(player);
                    }
                }
                else
                {
                    if (_friendlies.RemoveFast(player))
                        OnPlayerLeftRadius(player);
                }
            }
            else
            {
                if (_friendlies.RemoveFast(player))
                    OnPlayerLeftRadius(player);

                proxyScore += GetProxyScore(player);
            }
        }

        float oldProxyScore = ProxyScore;
        ProxyScore = proxyScore;

        if (oldProxyScore < 1 && proxyScore >= 1 || oldProxyScore >= 1 && proxyScore < 1)
            FOBManager.UpdateFOBListForTeam(Team, this);

        if (Data.Gamemode.EverySecond)
        {
            if (!Bleeding)
                TryConsumeResources();

            if (Data.Gamemode.EveryXSeconds(2f))
            {
                if (Bleeding)
                {
                    const ushort loss = 10;
                    L.LogDebug($"[FOBS] [{Name}] Bleeding -{loss}.");
                    BarricadeManager.damage(transform, loss, 1, false, default, EDamageOrigin.Useable_Melee);
                }

                if (Data.Gamemode.EveryMinute)
                {
                    if (!Bleeding)
                        Restock();
                }
            }
        }

        if (Radio != null && Radio is IGameTickListener ticker3)
            ticker3.Tick();
        if (Bunker != null && Bunker is IGameTickListener ticker2)
            ticker2.Tick();
        for (int i = 0; i < _items.Count; ++i)
        {
            if (_items[i] is IGameTickListener ticker)
                ticker.Tick();
        }
    }
    void IPlayerDisconnectListener.OnPlayerDisconnecting(UCPlayer player)
    {
        if (_friendlies.Remove(player))
            OnPlayerLeftRadius(player);
    }
    public bool IsPlayerOn(UCPlayer player) => _friendlies.Contains(player);

    [FormatDisplay(typeof(IDeployable), "Colored Name")]
    public const string FormatNameColored = "cn";
    [FormatDisplay(typeof(IDeployable), "Closest Location")]
    public const string FormatLocationName = "l";
    [FormatDisplay(typeof(IDeployable), "Grid Location")]
    public const string FormatGridLocation = "g";
    [FormatDisplay(typeof(IDeployable), "Name")]
    public const string FormatName = "n";
    string ITranslationArgument.Translate(string language, string? format, UCPlayer? target, CultureInfo? culture, ref TranslationFlags flags)
    {
        if (format is not null)
        {
            if (format.Equals(FormatNameColored, StringComparison.Ordinal))
                return Localization.Colorize(GetUIColor(), Name, flags);
            if (format.Equals(FormatLocationName, StringComparison.Ordinal))
                return ClosestLocation;
            if (format.Equals(FormatGridLocation, StringComparison.Ordinal))
                return GridLocation.ToString();
        }
        return Name;
    }

    public void Dump(UCPlayer? target)
    {
        EffectAsset? marker = null;
        L.Log($"[FOBS] [{Name}] === Fob Dump ===");
        using IDisposable indent = L.IndentLog(1);
        L.Log($"Team: {Team}, Owner: {new OfflinePlayer(Owner)}");
        if (Radio == null)
            L.LogWarning("Radio State: Null");
        else
        {
            L.Log($"Radio State: {Radio.State}.");
            TrySpawnMarker(Radio.Position);
        }
        if (Bunker == null)
            L.Log("Bunker State: Null");
        else
        {
            L.Log($"Bunker State: {Bunker.State}.");
            TrySpawnMarker(Bunker.Position);
        }
        L.Log($"{_items.Count} Item{_items.Count.S()}:");
        using (L.IndentLog(1))
        {
            for (int i = 0; i < _items.Count; ++i)
            {
                IFOBItem item = _items[i];
                switch (item)
                {
                    case ShovelableComponent shovelable:
                        L.Log($"{i}. Shovelable {shovelable.Buildable}: {shovelable.Progress} / {shovelable.Total}.");
                        break;
                    default:
                        L.Log($"{i}. {item.GetType().Name}: {item}.");
                        break;
                }

                TrySpawnMarker(item.Position);
            }
        }
        L.Log($"{_friendlies.Count} Friendly Player{_friendlies.Count.S()}:");
        using (L.IndentLog(1))
        {
            for (int i = 0; i < _friendlies.Count; ++i)
            {
                L.Log($"{i}. {_friendlies[i]}");
            }
        }

        L.Log($"Proxied: {IsProxied}, Score: {ProxyScore}.");
        L.Log($"Grid Location: {GridLocation}, Closest Location: {ClosestLocation}.");
        L.Log($"Supplies: Build = {BuildSupply}, Ammo = {AmmoSupply}.");

        void TrySpawnMarker(Vector3 pos)
        {
            if (target != null)
            {
                marker ??= Assets.find<EffectAsset>(new Guid("2c17fbd0f0ce49aeb3bc4637b68809a2"));
                if (marker != null)
                    F.TriggerEffectReliable(marker, target.Connection, pos);
            }
        }
    }
}

public interface IFOB : IDeployable
{
    ulong Team { get; }
    string Name { get; }
    string ClosestLocation { get; }
    UCPlayer? Instigator { get; set; }
    GridLocation GridLocation { get; }
    bool ContainsBuildable(IBuildable buildable);
    bool ContainsVehicle(InteractableVehicle vehicle);
    void Dump(UCPlayer? target);
}
public interface IResourceFOB : IFOB
{
    string UIResourceString { get; }
}
public interface IRadiusFOB : IFOB
{
    float Radius { get; }
    bool IsPlayerOn(UCPlayer player);
}
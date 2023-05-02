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
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using XPReward = Uncreated.Warfare.Levels.XPReward;

namespace Uncreated.Warfare.FOBs;
public sealed class FOB : MonoBehaviour, IFOB, IGameTickListener, IPlayerDisconnectListener
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

    public IReadOnlyList<IFOBItem> Items { get; }
    public IReadOnlyList<UCPlayer> FriendliesNearby { get; }
    public string Name { get; internal set; }
    public int Number { get; internal set; }
    public string ClosestLocation { get; private set; }
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
    public Vector3 Position => Bunker == null ? Vector3.zero : Bunker.SpawnPosition;
    public float Yaw => Bunker == null ? 0f : Bunker.SpawnYaw;
    public ulong Team => Radio.Team;
    public float Radius { get; private set; }
    public bool Bleeding => Radio.State == RadioComponent.RadioState.Bleeding;
    public float ProxyScore { get; private set; }
    public bool IsProxied => ProxyScore >= 1f;
    public ulong Owner => Radio.Owner;
    public int BuildSupply { get; private set; }
    public int AmmoSupply { get; private set; }

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
        if (Gamemode.Config.EffectUnloadAmmo.ValidReference(out EffectAsset asset))
            F.TriggerEffectReliable(asset, 40, pos);
        if (Gamemode.Config.EffectUnloadBuild.ValidReference(out asset))
            F.TriggerEffectReliable(asset, 40, pos);
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

        for (int i = _items.Count - 1; i >= 0; --i)
        {
            try
            {
                if (_items[i] is MonoBehaviour mb)
                    Destroy(mb);
                else if (_items[i] is IDisposable d)
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
                Bunker.ActiveStructure.Destroy();
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
                if (BarricadeManager.tryGetRegion(Radio.Barricade.model, out byte x, out byte y, out ushort plant, out _))
                    BarricadeManager.destroyBarricade(Radio.Barricade, x, y, plant);
                else Destroy(Radio);
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
    }
    public void OffloadNearbyLogisticsVehicle()
    {
        ThreadUtil.assertIsGameThread();
        
        InteractableVehicle? nearestLogi = UCVehicleManager.GetNearestLogi(Position, 30, Team);
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

            byte c = nearestLogi.trunkItems.getItemCount();

            int buildRemoved = 0;
            int ammoRemoved = 0;

            for (int i = c - 1; i >= 0; i--)
            {
                ItemJar item = nearestLogi.trunkItems.items[i];
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
                    ItemManager.dropItem(new Item(item.item.id, true), nearestLogi.transform.position, false, true, true);
                    nearestLogi.trunkItems.removeItem(nearestLogi.trunkItems.getIndex(item.x, item.y));
                    i--;
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
        RegionBuffer.Clear();
        SupplyBuffer.Clear();
        Vector3 pos = transform.position;
        Regions.getRegionsInRadius(pos, Radius, RegionBuffer);

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
            FOBManager.UpdateResourceUIString(this);
    }
    public void UpdateResourceUI(bool build, bool ammo, bool foblist = true)
    {
        ThreadUtil.assertIsGameThread();
        if (!build && !ammo)
            return;
        if (foblist)
            FOBManager.UpdateResourceUIString(this);
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
    internal bool UpgradeItem(IFOBItem item, Transform newObj)
    {
        if (item is not MonoBehaviour itemMb)
            return false;
        if (itemMb is BunkerComponent b && b == Bunker)
        {
            Bunker = newObj.gameObject.AddComponent<BunkerComponent>();
            Destroy(b);
            return true;
        }

        for (int i = 0; i < _items.Count; ++i)
        {
            if (_items[i] is MonoBehaviour mb && mb == itemMb)
            {
                _items[i] = (IFOBItem)newObj.gameObject.AddComponent(item.GetType());
                Destroy(itemMb);
                return true;
            }
        }

        return false;
    }
    float IDeployable.GetDelay() => FOBManager.Config.DeployFOBDelay;
    bool IDeployable.CheckDeployable(UCPlayer player, CommandInteraction? ctx)
    {
        if (Bunker == null || Bunker.ActiveStructure.Model == null)
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
        if (Bunker == null || Bunker.ActiveStructure.Model == null)
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

                    BarricadeManager.damage(transform, loss, 1, false, default, EDamageOrigin.Useable_Melee);
                }

                if (Data.Gamemode.EveryMinute)
                {
                    if (!Bleeding)
                        Restock();
                }
            }
        }
    }
    void IPlayerDisconnectListener.OnPlayerDisconnecting(UCPlayer player)
    {
        if (_friendlies.Remove(player))
            OnPlayerLeftRadius(player);
    }

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
}

public interface IFOB : IDeployable
{
    string Name { get; }
    string ClosestLocation { get; }
    GridLocation GridLocation { get; }
}
public interface IResourceFOB : IFOB
{
    string UIResourceString { get; }
}
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Framework;
using Uncreated.Warfare.Commands.CommandSystem;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Locations;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Quests;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;

namespace Uncreated.Warfare.Components;

public class FOBComponent : MonoBehaviour
{
    public FOB Parent { get; private set; }

    public void Initialize(FOB parent)
    {
        this.Parent = parent;
        Data.Gamemode.OnGameTick += OnTick;
        Restock();
    }
    public void Restock()
    {
        if (Parent.Team is not 1 and not 2)
            return;
        byte[] state = Convert.FromBase64String(Parent.Team == 1 ? FOBManager.Config.T1RadioState : FOBManager.Config.T2RadioState);
        Parent.Radio.GetServersideData().barricade.state = state;
        Parent.Radio.ReceiveUpdateState(state);
    }
    private void OnTick()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Vector3 pos = Parent.Position;
        foreach (UCPlayer player in PlayerManager.OnlinePlayers)
        {
            if (player.GetTeam() == Parent.Team)
            {
                if ((player.Position - pos).sqrMagnitude < Parent.SqrRadius)
                {
                    if (!Parent.FriendliesOnFOB.Contains(player))
                    {
                        Parent.FriendliesOnFOB.Add(player);
                        Parent.OnPlayerEnteredFOB(player);
                    }
                }
                else
                {
                    if (Parent.FriendliesOnFOB.Remove(player))
                    {
                        Parent.OnPlayerLeftFOB(player);
                    }
                }
            }
            else
            {
                if (Parent.FriendliesOnFOB.Remove(player))
                    Parent.OnPlayerLeftFOB(player);
                if (Parent.Bunker != null)
                {
                    // keeps people from being able to block FOBs from the floor above
                    if (Mathf.Abs(player.Position.y - pos.y) < 4 && Util.SqrDistance2D(player.Position, Parent.Bunker.model.position) < 49)
                    {
                        if (!Parent.NearbyEnemies.Contains(player))
                        {
                            Parent.NearbyEnemies.Add(player);
                            Parent.OnEnemyEnteredFOB(player);
                        }
                    }
                    else
                    {
                        if (Parent.NearbyEnemies.Remove(player))
                        {
                            Parent.OnEnemyLeftFOB(player);
                        }
                    }
                }
                else if (Parent.NearbyEnemies.Count > 0)
                {
                    for (int i = 0; i < Parent.NearbyEnemies.Count; ++i)
                        Parent.OnEnemyLeftFOB(Parent.NearbyEnemies[i]);
                    Parent.NearbyEnemies.Clear();
                }
            }
        }

        if (Data.Gamemode.EveryXSeconds(1f))
        {
            if (!Parent.IsBleeding)
                Parent.ConsumeResources();

            if (Data.Gamemode.EveryXSeconds(2f))
            {
                if (Parent.IsBleeding)
                {
                    const ushort loss = 10;
                    
                    BarricadeManager.damage(transform, loss, 1, false, default, EDamageOrigin.Useable_Melee);
                }

                if (Data.Gamemode.EveryMinute)
                {
                    if (!Parent.IsBleeding)
                    {
                        Restock();
                    }
                }
            }
        }
    }

    public void Destroy()
    {
        Data.Gamemode.OnGameTick -= OnTick;
        this.Parent = null!;
        Destroy(this);
    }
}
public class FOB : IFOB, IDeployable
{
    public BarricadeDrop Radio;
    private FOBComponent _component;
    public int Number;
    private readonly string _cl;
    private readonly GridLocation _gc;
    public string Name { get; set; }
    public GridLocation GridLocation => _gc;
    public string ClosestLocation => _cl;
    public ulong Team => Radio.GetServersideData().group.GetTeam();
    public ulong Owner => Radio.GetServersideData().owner;
    public BarricadeDrop? Bunker { get; private set; }
    public Vector3 Position => Radio.model.position;
    public float Yaw => Bunker == null || Bunker.model == null ? 0 : (Bunker.model.rotation.eulerAngles.y + 90f);
    public float Radius { get; private set; }
    public float SqrRadius
    {
        get
        {
            float rad = Radius;
            return rad * rad;
        }
    }
    public int Build { get; private set; }
    public int Ammo { get; private set; }
    public bool IsBleeding { get; private set; }
    public bool IsSpawnable { get => !IsBleeding && Radio != null && Bunker != null && !Radio.GetServersideData().barricade.isDead && !Bunker.GetServersideData().barricade.isDead; }
    public string UIColor
    {
        get
        {
            if (IsBleeding)
                return UCWarfare.GetColorHex("bleeding_fob_color");
            if (Bunker == null)
                return UCWarfare.GetColorHex("no_bunker_fob_color");
            if (NearbyEnemies.Count != 0)
                return UCWarfare.GetColorHex("enemy_nearby_fob_color");
            return UCWarfare.GetColorHex("default_fob_color");
        }
    }
    public string UIResourceString
    {
        get
        {
            return IsBleeding
                ? string.Empty
                : Build.ToString(Data.LocalLocale).Colorize("d4c49d") + " " + Ammo.ToString(Data.LocalLocale).Colorize("b56e6e");
        }
    }
    public BarricadeDrop? RepairStation
    {
        get => Gamemode.Config.BarricadeRepairStation.ValidReference(out Guid guid)
            ? UCBarricadeManager.GetNearbyBarricades(guid, Radius, Position, Team, false).FirstOrDefault()
            : null;
    }
    public IEnumerable<BarricadeDrop> AmmoCrates
    {
        get => Gamemode.Config.BarricadeAmmoCrate.ValidReference(out Guid guid)
            ? UCBarricadeManager.GetNearbyBarricades(guid, Radius, Position, Team, true)
            : Array.Empty<BarricadeDrop>();
    }
    public IEnumerable<InteractableVehicle> Emplacements => UCVehicleManager.GetNearbyVehicles(FOBManager.Config.Buildables.Where(bl => bl.Type == EBuildableType.EMPLACEMENT).Cast<Guid>(), Radius, Position);
    public List<UCPlayer> FriendliesOnFOB { get; }
    public List<UCPlayer> NearbyEnemies { get; }
    public ulong Killer { get; private set; }
    public ulong Placer { get; }
    public ulong Creator { get; }

    private readonly Guid _builtRadioGUID;
    private byte[] _builtState;

    private readonly Guid _buildID;
    private readonly Guid _ammoID;

    private readonly ushort _shortBuildID;
    private readonly ushort _shortAmmoID;

    public FOB(BarricadeDrop radio)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        Radio = radio;

        if (Radio.interactable is InteractableStorage storage)
            storage.despawnWhenDestroyed = true;

        FriendliesOnFOB = new List<UCPlayer>();
        NearbyEnemies = new List<UCPlayer>();

        Ammo = 0;
        Build = 0;

        _gc = new GridLocation(Position);
        _cl = F.GetClosestLocationName(Position);

        if (Data.Is(out IFlagRotation fg))
        {
            Flag flag = fg.LoadedFlags.Find(f => f.Name.Equals(ClosestLocation, StringComparison.OrdinalIgnoreCase));
            if (flag != null)
            {
                if (!string.IsNullOrEmpty(flag.ShortName))
                    _cl = flag.ShortName;
            }
        }

        IsBleeding = false;
        IsWipedByAuthority = false;
        IsDestroyed = false;

        Killer = 0;

        Placer = radio.GetServersideData().owner;

        _builtRadioGUID = radio.asset.GUID;

        TeamManager.GetFaction(Team).Build.ValidReference(out ItemAsset? build);
        TeamManager.GetFaction(Team).Ammo.ValidReference(out ItemAsset? ammo);

        if (build is not null)
        {
            _buildID = build.GUID;
            _shortBuildID = build.id;
        }
        if (ammo is not null)
        {
            _ammoID = ammo.GUID;
            _shortAmmoID = ammo.id;
        }

        InteractableVehicle? nearestLogi = UCVehicleManager.GetNearestLogi(Position, 30, Team);
        if (nearestLogi != null)
        {
            if (nearestLogi.transform.TryGetComponent(out VehicleComponent component))
            {
                component.Quota += 5;
                Creator = component.LastDriver;
            }

            if (!nearestLogi.isDriven)
            {
                int supplyCount = Mathf.Clamp(nearestLogi.trunkItems.getItemCount(), 0, 26);

                UCPlayer? creator = UCPlayer.FromID(Creator);
                int groupsUnloaded = 0;
                if (creator != null)
                {
                    creator.SuppliesUnloaded += supplyCount;
                    while (creator.SuppliesUnloaded > 0)
                    {
                        creator.SuppliesUnloaded -= 6;
                        if (creator.SuppliesUnloaded < 0)
                            creator.SuppliesUnloaded = 0;
                        else
                            groupsUnloaded++;
                    }

                    if (groupsUnloaded > 0)
                    {
                        int xp = Points.XPConfig.UnloadSuppliesXP;

                        if (creator.KitClass == Class.Pilot)
                        {
                            xp *= 2;
                        }

                        Points.AwardXP(creator, groupsUnloaded * xp, T.XPToastSuppliesUnloaded);
                    }
                }

                int buildRemoved = 0;
                int ammoRemoved = 0;

                for (int i = 0; i < nearestLogi.trunkItems.getItemCount(); i++)
                {
                    ItemJar item = nearestLogi.trunkItems.items[i];
                    bool shouldRemove = false;
                    if (item.item.id == _shortBuildID && buildRemoved < 16)
                    {
                        shouldRemove = true;
                        buildRemoved++;
                    }
                    if (item.item.id == _shortAmmoID && ammoRemoved < 12)
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
            }
        }
        if (Gamemode.Config.BarricadeFOBBunker.ValidReference(out Guid fob))
            UpdateBunker(UCBarricadeManager.GetNearbyBarricades(fob, 30, Position, Team, false).FirstOrDefault());

        _component = Radio.model.gameObject.AddComponent<FOBComponent>();
        _component.Initialize(this);
    }
    public void UpdateBunker(BarricadeDrop? bunker)
    {
        L.LogDebug("Bunker updated: " + bunker?.GetType());

        Bunker = bunker;

        Radius = Bunker == null ? 30 : FOBManager.Config.FOBBuildPickupRadius;

        FOBManager.UpdateFOBListForTeam(Team, this);
    }
    public void ConsumeResources()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        List<ItemData> nearbyBuild = UCBarricadeManager.GetNearbyItems(_buildID, Radius, Position);
        List<ItemData> nearbyAmmo = UCBarricadeManager.GetNearbyItems(_ammoID, Radius, Position);

        List<ItemData> items = new List<ItemData>(nearbyBuild);
        items.AddRange(nearbyAmmo);

        int itemsCount = items.Count;
        int counter = 0;
        foreach (ItemData item in items)
        {
            if (item.item.id == _shortBuildID || item.item.id == _shortAmmoID)
            {
                if (EventFunctions.droppeditemsInverse.TryGetValue(item.instanceID, out ulong playerID))
                {
                    UCPlayer? player = UCPlayer.FromID(playerID);
                    if (player != null)
                    {
                        player.SuppliesUnloaded++;
                        if (player.SuppliesUnloaded >= 6)
                        {
                            int xp = Points.XPConfig.UnloadSuppliesXP;

                            if (player.KitClass == Class.Pilot)
                            {
                                xp *= 2;
                            }

                            QuestManager.OnSuppliesConsumed(this, playerID, player.SuppliesUnloaded);

                            InteractableVehicle? vehicle = player.Player.movement.getVehicle();
                            if (vehicle is not null && vehicle.transform.TryGetComponent(out VehicleComponent component))
                            {
                                component.Quota += 0.33F;
                            }

                            Points.AwardXP(player, xp, T.XPToastSuppliesUnloaded);

                            player.SuppliesUnloaded = 0;
                        }
                    }
                }
            }
            counter++;
            if (counter >= Math.Min(itemsCount, 3))
            {
                break;
            }
        }

        int buildCount = nearbyBuild.Count;
        int ammoCount = nearbyAmmo.Count;

        if (buildCount > 0)
        {
            Build += Math.Min(buildCount, 3);
            UCBarricadeManager.RemoveNearbyItemsByID(_buildID, 3, Position, Radius);
            if (Gamemode.Config.EffectUnloadBuild.ValidReference(out EffectAsset effect))
                F.TriggerEffectReliable(effect, EffectManager.MEDIUM, nearbyBuild[0].point);
            foreach (UCPlayer player in FriendliesOnFOB)
                UpdateBuildUI(player);
            return;
        }
        if (ammoCount > 0)
        {
            Ammo += Math.Min(ammoCount, 3);
            UCBarricadeManager.RemoveNearbyItemsByID(_ammoID, 3, Position, Radius);
            if (Gamemode.Config.EffectUnloadAmmo.ValidReference(out EffectAsset effect))
                F.TriggerEffectReliable(effect, EffectManager.MEDIUM, nearbyBuild[0].point);
            foreach (UCPlayer player in FriendliesOnFOB)
                UpdateAmmoUI(player);
        }
    }
    public void ReduceAmmo(int amount)
    {
        Ammo -= amount;
        foreach (UCPlayer player in FriendliesOnFOB)
            UpdateAmmoUI(player);
    }
    public void ReduceBuild(int amount)
    {
        Build -= amount;
        foreach (UCPlayer player in FriendliesOnFOB)
            UpdateBuildUI(player);
    }
    public void AddBuild(int amount)
    {
        Build += amount;
        foreach (UCPlayer player in FriendliesOnFOB)
            UpdateBuildUI(player);
    }
    internal void OnPlayerEnteredFOB(UCPlayer player)
    {
        L.LogDebug("Player entered FOB: " + player);
        ShowResourceUI(player);

        InteractableVehicle? vehicle = player.Player.movement.getVehicle();
        if (vehicle == null)
            return;
        if (vehicle.TryGetComponent(out VehicleComponent comp) && comp.Data?.Item != null && VehicleData.IsLogistics(comp.Data.Item.Type))
            Tips.TryGiveTip(player, 120, T.TipUnloadSupplies);
    }
    internal void OnPlayerLeftFOB(UCPlayer player)
    {
        L.LogDebug("Player left FOB: " + player);
        HideResourceUI(player);
    }
    internal void OnEnemyEnteredFOB(UCPlayer player)
    {
        L.LogDebug("Enemy entered FOB: " + player);
        FOBManager.UpdateFOBListForTeam(this.Team, this);
    }
    internal void OnEnemyLeftFOB(UCPlayer player)
    {
        L.LogDebug("Enemy left FOB: " + player);
        FOBManager.UpdateFOBListForTeam(this.Team, this);
    }
    public void ShowResourceUI(UCPlayer player)
    {
        FOBManager.ResourceUI.SendToPlayer(player.Connection);
        UpdateBuildUI(player);
        UpdateAmmoUI(player);
    }
    public void HideResourceUI(UCPlayer player)
    {
        FOBManager.ResourceUI.ClearFromPlayer(player.Connection);
    }
    public void UpdateBuildUI(UCPlayer player)
    {
        FOBManager.ResourceUI.BuildLabel.SetText(player.Connection, Build.ToString());
        FOBManager.UpdateResourceUIString(this);
    }
    public void UpdateAmmoUI(UCPlayer player)
    {
        FOBManager.ResourceUI.AmmoLabel.SetText(player.Connection, Ammo.ToString());
        FOBManager.UpdateResourceUIString(this);
    }
    private void SwapRadioBarricade(BarricadeDrop newDrop)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!(Radio == null || Radio.GetServersideData().barricade.isDead))
        {
            if (Regions.tryGetCoordinate(Radio.model.position, out byte x, out byte y))
            {
                BarricadeManager.destroyBarricade(Radio, x, y, ushort.MaxValue);
            }
        }

        _component.Destroy();

        Radio = newDrop;
        _component = newDrop.model.gameObject.AddComponent<FOBComponent>();
        _component.Initialize(this);

    }
    public void StartBleed()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!Gamemode.Config.BarricadeFOBRadioDamaged.ValidReference(out ItemBarricadeAsset asset))
        {
            L.LogError("Damaged FOB Radio GUID does not match a barricade. (Change \"" +
                       nameof(GamemodeConfigData.BarricadeFOBRadioDamaged) + "\" in gamemode config).");
            return;
        }
        _builtState = Radio.GetServersideData().barricade.state;

        if (Radio.model.TryGetComponent(out BarricadeComponent component))
        {
            Killer = component.LastDamager;
        }

        BarricadeData data = Radio.GetServersideData();
        Barricade barricade = new Barricade(asset);
        Transform transform = BarricadeManager.dropNonPlantedBarricade(barricade, data.point, Quaternion.Euler(data.angle_x * 2, data.angle_y * 2, data.angle_z * 2), data.owner, data.group);
        BarricadeDrop newRadio = BarricadeManager.FindBarricadeByRootTransform(transform);

        IsBleeding = true;

        SwapRadioBarricade(newRadio);

        FOBManager.UpdateFOBListForTeam(Team, this);
    }
    public void Reactivate()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        BarricadeData data = Radio.GetServersideData();
        Barricade barricade = new Barricade(Assets.find<ItemBarricadeAsset>(_builtRadioGUID));
        Transform transform = BarricadeManager.dropNonPlantedBarricade(barricade, data.point, Quaternion.Euler(data.angle_x * 2, data.angle_y * 2, data.angle_z * 2), data.owner, data.group);
        BarricadeDrop newRadio = BarricadeManager.FindBarricadeByRootTransform(transform);

        IsBleeding = false;

        SwapRadioBarricade(newRadio);

        if (Radio.interactable is InteractableStorage storage)
            storage.despawnWhenDestroyed = true;

        Radio.GetServersideData().barricade.state = _builtState;
        Radio.ReceiveUpdateState(_builtState);

        FOBManager.SendFOBListToTeam(Team);
    }

    public void Repair(UCPlayer builder)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        float amount = 30;

        if (builder.KitClass == Class.CombatEngineer)
            amount *= 2;
        if (Gamemode.Config.EffectDig.ValidReference(out EffectAsset effect))
            F.TriggerEffectReliable(effect, EffectManager.MEDIUM, builder.Position);

        BarricadeManager.repair(Radio.model, amount, 1, builder.CSteamID);

        if (IsBleeding && Radio.GetServersideData().barricade.health >= Radio.asset.health)
        {
            Reactivate();
        }
    }
    public bool IsWipedByAuthority;
    public bool IsDestroyed { get; private set; }
    public void Destroy()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (IsDestroyed)
            return;

        foreach (UCPlayer player in FriendliesOnFOB)
            OnPlayerLeftFOB(player);
        foreach (UCPlayer player in NearbyEnemies)
            OnEnemyLeftFOB(player);

        FriendliesOnFOB.Clear();
        NearbyEnemies.Clear();

        _component.Destroy();

        if (!(Bunker == null || Bunker.GetServersideData().barricade.isDead))
        {
            if (Regions.tryGetCoordinate(Bunker.model.position, out byte x, out byte y))
                BarricadeManager.destroyBarricade(Bunker, x, y, ushort.MaxValue);
        }

        BarricadeDrop? rp = RepairStation; // loops each access
        if (rp != null)
        {
            if (Regions.tryGetCoordinate(rp.model.position, out byte x, out byte y))
                BarricadeManager.destroyBarricade(rp, x, y, ushort.MaxValue);
        }
        foreach (BarricadeDrop ammoCrate in AmmoCrates)
        {
            if (Regions.tryGetCoordinate(ammoCrate.model.position, out byte x, out byte y))
                BarricadeManager.destroyBarricade(ammoCrate, x, y, ushort.MaxValue);
        }

        IsDestroyed = true;

        FOBManager.DeleteFOB(this);
    }
    public static List<FOB> GetFoBs(ulong team)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        List<BarricadeDrop> barricades = UCBarricadeManager.GetBarricadesWhere(b =>
            b.model.TryGetComponent<FOBComponent>(out _)
        );

        List<FOB> fobs = new List<FOB>();

        foreach (BarricadeDrop barricade in barricades)
        {
            if (team != 0 && barricade.GetServersideData().group.GetTeam() == team)
                if (barricade.model.TryGetComponent(out FOBComponent comp))
                    fobs.Add(comp.Parent);
        }

        return fobs;
    }
    public static List<FOB> GetNearbyFoBs(Vector3 point, ulong team = 0, EfobRadius radius = EfobRadius.FULL)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        float radius2 = GetRadius(radius);
        List<BarricadeDrop> barricades = UCBarricadeManager.GetBarricadesWhere(b =>
            {
                BarricadeData data = b.GetServersideData();

                if (!b.model.TryGetComponent(out FOBComponent f)) return false;

                if (team != 0 && data.group != team) return false;
                if (radius == EfobRadius.FULL_WITH_BUNKER_CHECK)
                {
                    if ((data.point - point).sqrMagnitude <= 30 * 30)
                        return true;
                    else
                        return f.Parent.Bunker != null && (data.point - point).sqrMagnitude <= radius2;
                }
                else if (radius2 > 0)
                    return (data.point - point).sqrMagnitude <= radius2;

                return false;
            }
        );

        List<FOB> fobs = new List<FOB>();

        foreach (BarricadeDrop barricade in barricades)
        {
            fobs.Add(barricade.model.GetComponent<FOBComponent>().Parent);
        }

        return fobs;
    }
    public static FOB? GetNearestFOB(Vector3 point, EfobRadius radius = EfobRadius.FULL, ulong team = 0)
    {
        return GetNearbyFoBs(point, team, radius).FirstOrDefault();
    }
    public static bool IsOnFOB(UCPlayer player, out FOB fob)
    {
        fob = GetNearbyFoBs(player.Position, player.GetTeam()).FirstOrDefault(f => f.FriendliesOnFOB.Contains(player))!;
        return fob != null;
    }
    /// <returns>Numeric radius corresponding to the value of <paramref name="radius"/>.
    /// <para><see cref="EfobRadius.ENEMY_BUNKER_CLAIM"/> will return the radius with a bunker,
    /// additional checks should be done if this is the case.</para></returns>
    public static float GetRadius(EfobRadius radius) => radius switch
    {
        EfobRadius.SHORT => 30 * 30,
        EfobRadius.FULL_WITH_BUNKER_CHECK or EfobRadius.FULL =>
            FOBManager.Config.FOBBuildPickupRadius * FOBManager.Config.FOBBuildPickupRadius,
        EfobRadius.FOB_PLACEMENT => Mathf.Pow(FOBManager.Config.FOBBuildPickupRadius * 2, 2),
        EfobRadius.ENEMY_BUNKER_CLAIM => 5 * 5,
        _ => 0
    };

    [FormatDisplay(typeof(IDeployable), "Colored Name")]
    public const string COLORED_NAME_FORMAT = "cn";
    [FormatDisplay(typeof(IDeployable), "Closest Location")]
    public const string CLOSEST_LOCATION_FORMAT = "l";
    [FormatDisplay(typeof(IDeployable), "Grid Location")]
    public const string GRID_LOCATION_FORMAT = "g";
    [FormatDisplay(typeof(IDeployable), "Name")]
    public const string NAME_FORMAT = "n";
    string ITranslationArgument.Translate(string language, string? format, UCPlayer? target, ref TranslationFlags flags)
    {
        if (format is not null)
        {
            if (format.Equals(COLORED_NAME_FORMAT, StringComparison.Ordinal))
                return Localization.Colorize(UIColor, Name, flags);
            else if (format.Equals(CLOSEST_LOCATION_FORMAT, StringComparison.Ordinal))
                return ClosestLocation;
            else if (format.Equals(GRID_LOCATION_FORMAT, StringComparison.Ordinal))
                return GridLocation.ToString();
        }
        return Name;
    }
    bool IDeployable.CheckDeployable(UCPlayer player, CommandInteraction? ctx)
    {
        if (NearbyEnemies.Count != 0)
        {
            if (ctx is not null)
                throw ctx.Reply(T.DeployEnemiesNearby, this);
            return false;
        }
        if (IsBleeding || !IsSpawnable)
        {
            if (ctx is not null)
                throw ctx.Reply(T.DeployNotSpawnable, this);
            return false;
        }
        if (Bunker == null)
        {
            if (ctx is not null)
                throw ctx.Reply(T.DeployNoBunker, this);
            return false;
        }

        return true;
    }
    bool IDeployable.CheckDeployableTick(UCPlayer player, bool chat)
    {
        if (NearbyEnemies.Count != 0)
        {
            if (chat)
                player.SendChat(T.DeployEnemiesNearbyTick, this);
            return false;
        }
        if (IsBleeding || !IsSpawnable)
        {
            if (chat)
                player.SendChat(T.DeployNotSpawnableTick, this);
            return false;
        }
        if (Bunker == null)
        {
            if (chat)
                player.SendChat(T.DeployNoBunker, this);
            return false;
        }

        return true;
    }
    void IDeployable.OnDeploy(UCPlayer player, bool chat)
    {
        ActionLog.Add(ActionLogType.DeployToLocation, "FOB BUNKER " + Name + " TEAM " + TeamManager.TranslateName(Team, 0), player);
        if (chat)
            player.SendChat(T.DeploySuccess, this);
    }
}

public interface IFOB : ITranslationArgument
{
    Vector3 Position { get; }
    string Name { get; }
    string ClosestLocation { get; }
    GridLocation GridLocation { get; }
}

public enum EfobRadius : byte
{
    SHORT,
    FULL,
    FULL_WITH_BUNKER_CHECK,
    FOB_PLACEMENT,
    ENEMY_BUNKER_CLAIM
}

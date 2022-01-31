using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;

namespace Uncreated.Warfare.Components
{
    public class FOBComponent : MonoBehaviour
    {
        public FOB parent { get; private set; }
        private Coroutine loop;
        

        public void Initialize(FOB parent)
        {
            this.parent = parent;

            loop = StartCoroutine(Tick());
        }

        private IEnumerator<WaitForSeconds> Tick()
        {
            float count = 0;
            float tickFrequency = 0.25F;

            while (true)
            {
                foreach (UCPlayer player in PlayerManager.OnlinePlayers)
                {
                    if (player.GetTeam() == parent.Team)
                    {
                        if ((player.Position - parent.Position).sqrMagnitude < parent.SqrRadius)
                        {
                            if (!parent.FriendliesOnFOB.Contains(player))
                            {
                                parent.FriendliesOnFOB.Add(player);
                                parent.OnPlayerEnteredFOB(player);
                            }
                        }
                        else
                        {
                            if (parent.FriendliesOnFOB.Remove(player))
                            {
                                parent.OnPlayerLeftFOB(player);
                            }
                        }
                    }
                    else if (parent.Bunker != null)
                    {
                        if (Mathf.Abs(player.Position.y - parent.Position.y) < 4 && F.SqrDistance2D(player.Position, parent.Bunker.model.position) < Math.Pow(7, 2))
                        {
                            if (!parent.NearbyEnemies.Contains(player))
                            {
                                parent.NearbyEnemies.Add(player);
                                parent.OnEnemyEnteredFOB(player);
                            }
                        }
                        else
                        {
                            if (parent.NearbyEnemies.Remove(player))
                            {
                                parent.OnEnemyLeftFOB(player);
                            }
                        }
                    }
                }

                if (count % (1 / tickFrequency) == 0) // every 1 second
                {
                    if (!parent.IsBleeding)
                        parent.ConsumeResources();
                }
                if (count % (2 / tickFrequency) == 0)  // every 2 seconds
                {
                    if (parent.IsBleeding)
                    {
                        ushort loss = 10;

                        Barricade barricade = parent.Radio.GetServersideData().barricade;

                        BarricadeManager.damage(transform, loss, 1, false, default, EDamageOrigin.Useable_Melee);
                    }
                   
                }

                count ++;
                if (count >= (2 / tickFrequency))
                    count = 0;
                yield return new WaitForSeconds(tickFrequency);
            }
        }
        public void Destroy()
        {
            StopCoroutine(loop);
            Destroy(this);
        }
    }
    public class FOB
    {
        public BarricadeDrop Radio;
        private FOBComponent component;
        public int Number;
        public string Name;
        public string GridCoordinates { get; private set; }
        public string ClosestLocation { get; private set; }
        public ulong Team { get => Radio.GetServersideData().group; }
        public ulong Owner { get => Radio.GetServersideData().owner; }
        public BarricadeDrop Bunker { get; private set; }
        public Vector3 Position { get => Radio.model.position; }
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
                else if (Bunker == null)
                    return UCWarfare.GetColorHex("no_bunker_fob_color");
                else if (NearbyEnemies.Count != 0)
                    return UCWarfare.GetColorHex("enemy_nearby_fob_color");
                else
                    return UCWarfare.GetColorHex("default_fob_color");
            }
        }
        public string UIResourceString
        {
            get
            {
                if (IsBleeding)
                    return string.Empty;
                else
                    return Build.ToString(Data.Locale).Colorize("d4c49d") + " " + Ammo.ToString(Data.Locale).Colorize("b56e6e");
            }
        }
        public BarricadeDrop RepairStation { get => UCBarricadeManager.GetNearbyBarricades(Gamemode.Config.Barricades.RepairStationGUID, Radius, Position, Team, false).FirstOrDefault(); }
        public IEnumerable<BarricadeDrop> AmmoCrates { get => UCBarricadeManager.GetNearbyBarricades(Gamemode.Config.Barricades.AmmoCrateGUID, Radius, Position, Team, true); }
        public int AmmoCrateCount => UCBarricadeManager.CountNearbyBarricades(Gamemode.Config.Barricades.AmmoCrateGUID, Radius, Position, Team);
        public IEnumerable<BarricadeDrop> Fortifications
        {
            get
            {
                return UCBarricadeManager.GetBarricadesWhere(b =>
                    FOBManager.config.data.Buildables.Exists(bl => bl.structureID == b.asset.GUID && bl.type == EBuildableType.FORTIFICATION) &&
                    (Position - b.model.position).sqrMagnitude < SqrRadius &&
                    b.GetServersideData().group == Team
                    );
            }
        }
        public int FortificationsCount
        {
            get
            {
                return UCBarricadeManager.CountBarricadesWhere(b =>
                    FOBManager.config.data.Buildables.Exists(bl => bl.structureID == b.asset.GUID && bl.type == EBuildableType.FORTIFICATION) &&
                    (Position - b.model.position).sqrMagnitude < SqrRadius &&
                    b.GetServersideData().group == Team
                    );
            }
        }
        public IEnumerable<InteractableVehicle> Emplacements => UCVehicleManager.GetNearbyVehicles(FOBManager.config.data.Buildables.Where(bl => bl.type == EBuildableType.EMPLACEMENT).Cast<Guid>(), Radius, Position);

        public List<UCPlayer> FriendliesOnFOB { get; private set; }
        public List<UCPlayer> NearbyEnemies { get; private set; }
        public ulong Killer { get; private set; }
        public ulong Placer { get; private set; }
        public ulong Creator { get; private set; }

        private readonly Guid builtRadioGUID;
        private byte[] builtState;

        private readonly Guid BuildID;
        private readonly Guid AmmoID;

        private readonly ushort shortBuildID;
        private readonly ushort shortAmmoID;

        public FOB(BarricadeDrop radio)
        {
            Radio = radio;

            if (Radio.interactable is InteractableStorage storage)
                storage.despawnWhenDestroyed = true;

            FriendliesOnFOB = new List<UCPlayer>();
            NearbyEnemies = new List<UCPlayer>();

            Ammo = 0;
            Build = 0;

            GridCoordinates = F.ToGridPosition(Position);
            ClosestLocation = F.GetClosestLocation(Position);

            if (Data.Is(out IFlagRotation fg))
            {
                Flag flag = fg.LoadedFlags.Find(f => f.Name.Equals(ClosestLocation, StringComparison.OrdinalIgnoreCase));
                if (flag != null)
                {
                    if (!string.IsNullOrEmpty(flag.ShortName))
                        ClosestLocation = flag.ShortName;
                }
            }

            IsBleeding = false;
            IsWipedByAuthority = false;
            IsDestroyed = false;

            Killer = 0;

            Placer = radio.GetServersideData().owner;

            InteractableVehicle nearestLogi = UCVehicleManager.GetNearbyVehicles(FOBManager.config.data.LogiTruckIDs.AsEnumerable(), 30, Position).FirstOrDefault(l => l.lockedGroup.m_SteamID == Team);
            if (nearestLogi != null)
            {
                if (nearestLogi.transform.TryGetComponent(out VehicleComponent component))
                    component.Quota += 3;
                Creator = nearestLogi.lockedOwner.m_SteamID;
            }

            builtRadioGUID = radio.asset.GUID;

            if (Team == 1)
            {
                BuildID = Gamemode.Config.Items.T1Build;
                AmmoID = Gamemode.Config.Items.T1Ammo;
            }
            else if (Team == 2)
            {
                BuildID = Gamemode.Config.Items.T2Build;
                AmmoID = Gamemode.Config.Items.T2Ammo;
            }
            else return;

            if (Assets.find(BuildID) is ItemAsset build)
                shortBuildID = build.id;
            if (Assets.find(AmmoID) is ItemAsset ammo)
                shortAmmoID = ammo.id;

            UpdateBunker(UCBarricadeManager.GetNearbyBarricades(Gamemode.Config.Barricades.FOBGUID, 30, Position, Team, false).FirstOrDefault());

            component = Radio.model.gameObject.AddComponent<FOBComponent>();
            component.Initialize(this);

            FOBManager.SendFOBListToTeam(Team);
        }
        public void UpdateBunker(BarricadeDrop bunker)
        {
            L.LogDebug("Bunker updated: " + bunker?.GetType());

            Bunker = bunker;

            if (Bunker == null)
            {
                Radius = 30;
            }
            else
            {
                Radius = FOBManager.config.data.FOBBuildPickupRadius;
            }
        }
        public void ConsumeResources()
        {
            List<SDG.Unturned.ItemData> NearbyBuild = UCBarricadeManager.GetNearbyItems(BuildID, Radius, Position);
            List<SDG.Unturned.ItemData> NearbyAmmo = UCBarricadeManager.GetNearbyItems(AmmoID, Radius, Position);

            IEnumerable<SDG.Unturned.ItemData> items = NearbyBuild.Concat(NearbyAmmo);

            int itemsCount = items.Count();
            int counter = 0;
            foreach (SDG.Unturned.ItemData item in items)
            {
                if (item.item.id == shortBuildID || item.item.id == shortAmmoID)
                {
                    if (EventFunctions.droppeditemsInverse.TryGetValue(item.instanceID, out ulong playerID))
                    {
                        UCPlayer player = UCPlayer.FromID(playerID);
                        if (player != null)
                        {
                            player.SuppliesUnloaded++;
                            if (player.SuppliesUnloaded >= 6)
                            {
                                int tw = Points.TWConfig.UnloadSuppliesPoints;
                                int xp = Points.XPConfig.UnloadSuppliesXP;

                                if (player.KitClass == EClass.PILOT)
                                {
                                    xp *= 2;
                                    tw *= 2;
                                }

                                InteractableVehicle vehicle = player.Player.movement.getVehicle();
                                if (vehicle is not null && vehicle.transform.TryGetComponent(out VehicleComponent component))
                                {
                                    component.Quota += 0.33F;
                                }

                                Points.AwardXP(player, xp, Translation.Translate("xp_supplies_unloaded", player));
                                Points.AwardTW(player, tw);

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

            int buildCount = NearbyBuild.Count;
            int ammoCount = NearbyAmmo.Count;

            if (buildCount > 0)
            {
                Build += Math.Min(buildCount, 3);
                UCBarricadeManager.RemoveNearbyItemsByID(BuildID, 3, Position, Radius);
                EffectManager.sendEffect(25997, EffectManager.MEDIUM, NearbyBuild[0].point);
                foreach (UCPlayer player in FriendliesOnFOB)
                    UpdateBuildUI(player);
                return;
            }
            if (ammoCount > 0)
            {
                Ammo += Math.Min(ammoCount, 3);
                UCBarricadeManager.RemoveNearbyItemsByID(AmmoID, 3, Position, Radius);
                EffectManager.sendEffect(25998, EffectManager.MEDIUM, NearbyAmmo[0].point);
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
            ShowResourceUI(player);
        }
        internal void OnPlayerLeftFOB(UCPlayer player)
        {
            HideResourceUI(player);
        }
        internal void OnEnemyEnteredFOB(UCPlayer player)
        {
            FOBManager.UpdateFOBListForTeam(this.Team, this);
        }
        internal void OnEnemyLeftFOB(UCPlayer player)
        {
            FOBManager.UpdateFOBListForTeam(this.Team, this);
        }
        public void ShowResourceUI(UCPlayer player)
        {
            EffectManager.sendUIEffect(FOBManager.nearbyResourceId, (short)FOBManager.nearbyResourceId, player.connection, true);
            UpdateBuildUI(player);
            UpdateAmmoUI(player);
        }
        public void HideResourceUI(UCPlayer player)
        {
            EffectManager.askEffectClearByID(FOBManager.nearbyResourceId, player.connection);
        }
        public void UpdateBuildUI(UCPlayer player)
        {
            EffectManager.sendUIEffectText((short)FOBManager.nearbyResourceId, player.connection, true,
                    "Build",
                    Build.ToString()
                    );

            FOBManager.UpdateResourceUIString(this);
        }
        public void UpdateAmmoUI(UCPlayer player)
        {
            EffectManager.sendUIEffectText((short)FOBManager.nearbyResourceId, player.connection, true,
                    "Ammo",
                    Ammo.ToString()
                    );

            FOBManager.UpdateResourceUIString(this);
        }
        private void SwapRadioBarricade(BarricadeDrop newDrop)
        {
            if (!(Radio == null || Radio.GetServersideData().barricade.isDead))
            {
                if (Regions.tryGetCoordinate(Radio.model.position, out byte x, out byte y))
                {
                    BarricadeManager.destroyBarricade(Radio, x, y, ushort.MaxValue);
                }
            }

            component.Destroy();

            Radio = newDrop;
            component = newDrop.model.gameObject.AddComponent<FOBComponent>();
            component.Initialize(this);

        }
        public void StartBleed()
        {
            builtState = Radio.GetServersideData().barricade.state;

            if (Radio.model.TryGetComponent(out BarricadeComponent component))
            {
                Killer = component.LastDamager;
            }

            SDG.Unturned.BarricadeData data = Radio.GetServersideData();
            Barricade barricade = new Barricade(Assets.find<ItemBarricadeAsset>(Gamemode.Config.Barricades.FOBRadioDamagedGUID));
            Transform transform = BarricadeManager.dropNonPlantedBarricade(barricade, data.point, Quaternion.Euler(data.angle_x * 2, data.angle_y * 2, data.angle_z * 2), data.owner, data.group);
            BarricadeDrop newRadio = BarricadeManager.FindBarricadeByRootTransform(transform);

            IsBleeding = true;

            SwapRadioBarricade(newRadio);

            FOBManager.SendFOBListToTeam(Team);
        }
        public void Reactivate()
        {
            SDG.Unturned.BarricadeData data = Radio.GetServersideData();
            Barricade barricade = new Barricade(Assets.find<ItemBarricadeAsset>(builtRadioGUID));
            Transform transform = BarricadeManager.dropNonPlantedBarricade(barricade, data.point, Quaternion.Euler(data.angle_x * 2, data.angle_y * 2, data.angle_z * 2), data.owner, data.group);
            BarricadeDrop newRadio = BarricadeManager.FindBarricadeByRootTransform(transform);

            IsBleeding = false;

            SwapRadioBarricade(newRadio);

            if (Radio.interactable is InteractableStorage storage)
                storage.despawnWhenDestroyed = true;

            Radio.GetServersideData().barricade.state = builtState;
            Radio.ReceiveUpdateState(builtState);

            FOBManager.SendFOBListToTeam(Team);
        }

        public void Repair(UCPlayer builder)
        {
            float amount = 30;

            if (builder.KitClass == EClass.COMBAT_ENGINEER)
                amount *= 2;

            EffectManager.sendEffect(38405, EffectManager.MEDIUM, builder.Position);

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
            if (IsDestroyed || Radio.GetServersideData().barricade.isDead)
                return;

            foreach (UCPlayer player in FriendliesOnFOB)
                OnPlayerLeftFOB(player);
            foreach (UCPlayer player in NearbyEnemies)
                OnEnemyLeftFOB(player);

            FriendliesOnFOB.Clear();
            NearbyEnemies.Clear();

            component.Destroy();

            if(!(Bunker == null || Bunker.GetServersideData().barricade.isDead))
            {
                if (Regions.tryGetCoordinate(Bunker.model.position, out byte x, out byte y))
                    BarricadeManager.destroyBarricade(Bunker, x, y, ushort.MaxValue);
            }
            if (RepairStation != null)
            {
                if (Regions.tryGetCoordinate(RepairStation.model.position, out byte x, out byte y))
                    BarricadeManager.destroyBarricade(RepairStation, x, y, ushort.MaxValue);
            }
            foreach (BarricadeDrop ammoCrate in AmmoCrates)
            {
                if (Regions.tryGetCoordinate(ammoCrate.model.position, out byte x, out byte y))
                    BarricadeManager.destroyBarricade(ammoCrate, x, y, ushort.MaxValue);
            }

            IsDestroyed = true;

            FOBManager.DeleteFOB(this);
        }
        public static List<FOB> GetFOBs(ulong team)
        {
            List<BarricadeDrop> barricades = UCBarricadeManager.GetBarricadesWhere(b =>
                b.model.TryGetComponent<FOBComponent>(out _)
            );

            List<FOB> fobs = new List<FOB>();

            foreach (BarricadeDrop barricade in barricades)
            {
                if (team != 0 && barricade.GetServersideData().group.GetTeam() == team)
                    if (barricade.model.TryGetComponent(out FOBComponent comp))
                        fobs.Add(comp.parent);
            }

            return fobs;
        }
        public static List<FOB> GetNearbyFOBs(Vector3 point, ulong team = 0, EFOBRadius radius = EFOBRadius.FULL)
        {
            float radius2 = GetRadius(radius);
            List<BarricadeDrop> barricades = UCBarricadeManager.GetBarricadesWhere(radius2, point, b =>
                {
                    if (!b.model.TryGetComponent(out FOBComponent f)) return false;
                    if (radius == EFOBRadius.FULL_WITH_BUNKER_CHECK)
                    {
                        if ((b.model.position - point).sqrMagnitude <= 30 * 30)
                            return true;
                        else
                            return f.parent.Bunker != null && (b.model.position - point).sqrMagnitude <= radius2;
                    }
                    else if (radius2 > 0) 
                        return (b.model.position - point).sqrMagnitude <= radius2;

                    return false;
                }
            );

            List<FOB> fobs = new List<FOB>();

            foreach (BarricadeDrop barricade in barricades)
            {
                if (team == 0 || barricade.GetServersideData().group.GetTeam() == team)
                    fobs.Add(barricade.model.GetComponent<FOBComponent>().parent);
            }

            return fobs;
        }
        public static FOB GetNearestFOB(Vector3 point, EFOBRadius radius = EFOBRadius.FULL, ulong team = 0)
        {
            return GetNearbyFOBs(point, team, radius).FirstOrDefault();
        }
        public static bool IsOnFOB(UCPlayer player, out FOB fob)
        {
            fob = GetNearbyFOBs(player.Position, player.GetTeam()).Where(f => f.FriendliesOnFOB.Contains(player)).FirstOrDefault();
            return fob != null;
        }
        /// <returns>Numeric radius corresponding to the value of <paramref name="radius"/>.
        /// <para><see cref="EFOBRadius.ENEMY_BUNKER_CLAIM"/> will return the radius with a bunker,
        /// additional checks should be done if this is the case.</para></returns>
        public static float GetRadius(EFOBRadius radius) => radius switch
        {
            EFOBRadius.SHORT => 30 * 30,
            EFOBRadius.FULL_WITH_BUNKER_CHECK or EFOBRadius.FULL =>
                FOBManager.config.data.FOBBuildPickupRadius * FOBManager.config.data.FOBBuildPickupRadius,
            EFOBRadius.FOB_PLACEMENT => Mathf.Pow(FOBManager.config.data.FOBBuildPickupRadius * 2, 2),
            EFOBRadius.ENEMY_BUNKER_CLAIM => 5 * 5,
            _ => 0
        };
    }
    public enum EFOBRadius
    {
        SHORT,
        FULL,
        FULL_WITH_BUNKER_CHECK,
        FOB_PLACEMENT,
        ENEMY_BUNKER_CLAIM
    }
}

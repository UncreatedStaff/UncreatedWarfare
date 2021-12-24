using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

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
                        if ((player.Position - parent.Position).sqrMagnitude < Math.Pow(parent.Radius, 2))
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
                        if ((player.Position - parent.Bunker.model.position).sqrMagnitude < Math.Pow(10, 2))
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

                if (count % (1.5 / tickFrequency) == 0) // every 1 second
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

                        if (loss >= barricade.health)
                            parent.Destroy();
                    }
                   
                }

                count ++;
                if (count >= (3 / tickFrequency))
                    count = 0;
                yield return new WaitForSeconds(tickFrequency);
            }
        }
        public void Destroy()
        {
            StopCoroutine(loop);
            Destroy(gameObject);
        }
    }
    public class FOB
    {
        public BarricadeDrop Radio { get; private set; }
        private FOBComponent component;
        public EFOBStatus Status;
        public int Number;
        public string Name;
        public string ClosestLocation;
        public ulong Team { get => Radio.GetServersideData().group; }
        public ulong Owner { get => Radio.GetServersideData().owner; }
        public BarricadeDrop Bunker { get; private set; }
        public Vector3 Position { get => Radio.model.position; }
        public float Radius { get; private set; }
        public int Build { get; private set; }
        public int Ammo { get; private set; }
        public bool IsBleeding { get; private set; }
        public bool IsSpawnable { get => Radio != null || Bunker != null || !Radio.GetServersideData().barricade.isDead || !Bunker.GetServersideData().barricade.isDead; }

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
        public BarricadeDrop RepairStation { get => UCBarricadeManager.GetNearbyBarricades(Gamemode.Config.Barricades.RepairStationGUID, Radius, Position, Team, false).FirstOrDefault(); }
        public IEnumerable<BarricadeDrop> AmmoCrates { get => UCBarricadeManager.GetNearbyBarricades(Gamemode.Config.Barricades.AmmoCrateGUID, Radius, Position, Team, true); }
        public int AmmoCrateCount => UCBarricadeManager.CountNearbyBarricades(Gamemode.Config.Barricades.AmmoCrateGUID, Radius, Position, Team);
        public IEnumerable<BarricadeDrop> Fortifications
        {
            get
            {
                return UCBarricadeManager.GetBarricadesWhere(b =>
                    FOBManager.config.Data.Buildables.Exists(bl => bl.structureID == b.asset.GUID && bl.type == EbuildableType.FORTIFICATION) &&
                    (Position - b.model.position).sqrMagnitude < Math.Pow(Radius, 2) &&
                    b.GetServersideData().group == Team
                    );
            }
        }
        public int FortificationsCount
        {
            get
            {
                return UCBarricadeManager.CountBarricadesWhere(b =>
                    FOBManager.config.Data.Buildables.Exists(bl => bl.structureID == b.asset.GUID && bl.type == EbuildableType.FORTIFICATION) &&
                    (Position - b.model.position).sqrMagnitude < Math.Pow(Radius, 2) &&
                    b.GetServersideData().group == Team
                    );
            }
        }
        public IEnumerable<InteractableVehicle> Emplacements => UCVehicleManager.GetNearbyVehicles(FOBManager.config.Data.Buildables.Where(bl => bl.type == EbuildableType.EMPLACEMENT).Cast<Guid>(), Radius, Position);

        public List<UCPlayer> FriendliesOnFOB { get; private set; }
        public List<UCPlayer> NearbyEnemies { get; private set; }
        public ulong killer { get; private set; }

        public FOB(BarricadeDrop radio)
        {
            Radio = radio;
            FriendliesOnFOB = new List<UCPlayer>();
            NearbyEnemies = new List<UCPlayer>();

            Ammo = 0;
            Build = 0;

            ClosestLocation = F.GetClosestLocation(Position) ?? Provider.map;
            Status = EFOBStatus.RADIO;
            IsBleeding = false;

            killer = 0;

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
                Status &= ~EFOBStatus.HAB;
            }
            else
            {
                Radius = FOBManager.config.Data.FOBBuildPickupRadius;
                Status |= EFOBStatus.HAB;
            }
        }
        public void ConsumeResources()
        {
            Guid BuildID;
            Guid AmmoID;
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

            List<SDG.Unturned.ItemData> NearbyBuild = UCBarricadeManager.GetNearbyItems(BuildID, Radius, Position);
            List<SDG.Unturned.ItemData> NearbyAmmo = UCBarricadeManager.GetNearbyItems(AmmoID, Radius, Position);

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
                return;
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

        }
        internal void OnEnemyLeftFOB(UCPlayer player)
        {

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
        }
        public void UpdateAmmoUI(UCPlayer player)
        {
            EffectManager.sendUIEffectText((short)FOBManager.nearbyResourceId, player.connection, true,
                    "Ammo",
                    Ammo.ToString()
                    );
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
            if (Radio.model.TryGetComponent(out BarricadeComponent component))
            {
                killer = component.LastDamager;
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
            Barricade barricade = new Barricade(Assets.find<ItemBarricadeAsset>(Gamemode.Config.Barricades.FOBRadioGUID));
            Transform transform = BarricadeManager.dropNonPlantedBarricade(barricade, data.point, Quaternion.Euler(data.angle_x * 2, data.angle_y * 2, data.angle_z * 2), data.owner, data.group);
            BarricadeDrop newRadio = BarricadeManager.FindBarricadeByRootTransform(transform);

            IsBleeding = false;

            SwapRadioBarricade(newRadio);

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

        public void Destroy()
        {
            foreach (UCPlayer player in FriendliesOnFOB)
                OnPlayerLeftFOB(player);
            foreach (UCPlayer player in NearbyEnemies)
                OnEnemyLeftFOB(player);

            FriendliesOnFOB.Clear();
            NearbyEnemies.Clear();

            component.Destroy();

            if (!(Radio == null || Radio.GetServersideData().barricade.isDead))
            {
                if (Regions.tryGetCoordinate(Radio.model.position, out byte x, out byte y))
                    BarricadeManager.destroyBarricade(Radio, x, y, ushort.MaxValue);
            }
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
            float range = 0;

            if (radius == EFOBRadius.FULL)
                range = FOBManager.config.Data.FOBBuildPickupRadius;
            else if (radius == EFOBRadius.SHORT)
                range = 30;
            else if (radius == EFOBRadius.FOB_PLACEMENT)
                range = FOBManager.config.Data.FOBBuildPickupRadius * 2;


            List<BarricadeDrop> barricades = UCBarricadeManager.GetBarricadesWhere(b =>
                (b.model.position - point).sqrMagnitude <= Math.Pow(range, 2) &&
                b.model.TryGetComponent<FOBComponent>(out _)
            );

            List<FOB> fobs = new List<FOB>();

            foreach (BarricadeDrop barricade in barricades)
            {
                if (team == 0 || (team != 0 && barricade.GetServersideData().group == team))
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


    }
    public enum EFOBRadius
    {
        SHORT,
        FULL,
        FOB_PLACEMENT
    }
}

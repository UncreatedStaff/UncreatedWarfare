using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
namespace Uncreated.Warfare.Components
{
    public class VehicleComponent : MonoBehaviour
    {
        public Guid item;
        public InteractableVehicle Vehicle;
        public ulong Team { get => Vehicle.lockedGroup.m_SteamID; }
        public VehicleData Data;
        public bool isInVehiclebay { get; private set; }
        public EDamageOrigin lastDamageOrigin;
        public ulong lastDamager;
        public Dictionary<ulong, Vector3> TransportTable { get; private set; }
        public Dictionary<ulong, double> UsageTable { get; private set; }
        private Dictionary<ulong, DateTime> TimeEnteredTable;
        private Dictionary<ulong, DateTime> TimeRewardedTable;
        public Dictionary<ulong, KeyValuePair<ushort, DateTime>> DamageTable;
        private float _quota;
        private float _requiredQuota;
        public float Quota { get => _quota; set => _quota = value; }
        public float RequiredQuota { get => _requiredQuota; set => _requiredQuota = value; }

        private bool IsResupplied;

        private Coroutine quotaLoop;
        private Coroutine autoSupplyLoop;
        public Coroutine forceSupplyLoop { get; private set; }
        public void Initialize(InteractableVehicle vehicle)
        {
            Vehicle = vehicle;

            lastDamager = 0;
            TransportTable = new Dictionary<ulong, Vector3>();
            UsageTable = new Dictionary<ulong, double>();
            TimeEnteredTable = new Dictionary<ulong, DateTime>();
            TimeRewardedTable = new Dictionary<ulong, DateTime>();
            DamageTable = new Dictionary<ulong, KeyValuePair<ushort, DateTime>>();
            IsResupplied = true;

            _quota = 0;
            _requiredQuota = -1;

            if (VehicleBay.VehicleExists(vehicle.asset.GUID, out var data))
            {
                Data = data;
                isInVehiclebay = true;
            }
        }
        public void OnPlayerEnteredVehicle(Player nelsonplayer, InteractableVehicle vehicle)
        {
            UCPlayer player = UCPlayer.FromPlayer(nelsonplayer);
            if (player == null)
                return;
            
            byte toSeat = 0;
            for (byte i = 0; i < vehicle.passengers.Length; i++)
            {
                if (vehicle.passengers[i].player == null)
                    toSeat = i;
            }

            if (VehicleBay.VehicleExists(vehicle.asset.GUID, out var data))
            {
                bool isCrewSeat = data.CrewSeats.Contains(toSeat);

                if (!isCrewSeat)
                {
                    if (!TransportTable.ContainsKey(player.Steam64))
                        TransportTable.Add(player.Steam64, player.Position);
                    else
                        TransportTable[player.Steam64] = player.Position;
                }

                if (!TimeEnteredTable.ContainsKey(player.Steam64))
                    TimeEnteredTable.Add(player.Steam64, DateTime.Now);
                else
                    TimeEnteredTable[player.Steam64] = DateTime.Now;

                if (quotaLoop is null)
                {
                    _requiredQuota = data.TicketCost;
                    quotaLoop = StartCoroutine(QuotaLoop());
                }
            }
        }

        public void OnPlayerExitedVehicle(Player nelsonplayer, InteractableVehicle vehicle)
        {
            UCPlayer player = UCPlayer.FromPlayer(nelsonplayer);
            if (player == null)
                return;

            if (isInVehiclebay)
                EvaluateUsage(nelsonplayer.channel.owner);

            if (vehicle.passengers[0].player == null)
                return;

            if (vehicle.passengers[0].player.player.channel.owner.playerID.steamID == player.CSteamID)
                return;

            if (TransportTable.TryGetValue(player.Steam64, out Vector3 original))
            {
                float distance = (player.Position - original).magnitude;
                if (distance >= 200 && !(player.KitClass == EClass.CREWMAN || player.KitClass == EClass.PILOT))
                {
                    bool isOnCooldown = false;
                    if (TimeRewardedTable.TryGetValue(player.Steam64, out DateTime time) && (DateTime.Now - time).TotalSeconds < 60)
                        isOnCooldown = true;

                    if (!isOnCooldown)
                    {
                        int amount = (int)(Math.Floor(distance / 100) * 2) + 5;

                        Points.AwardXP(vehicle.passengers[0].player.player, amount, Translation.Translate("xp_transporting_players", vehicle.passengers[0].player.player));

                        _quota += 0.5F;

                        if (!TimeRewardedTable.ContainsKey(player.Steam64))
                            TimeRewardedTable.Add(player.Steam64, DateTime.Now);
                        else
                            TimeRewardedTable[player.Steam64] = DateTime.Now;
                    }
                }
                TransportTable.Remove(player.Steam64);
            }
        }
        public void OnPlayerSwapSeatRequested(Player nelsonplayer, InteractableVehicle vehicle, byte toSeatIndex)
        {
            UCPlayer player = UCPlayer.FromPlayer(nelsonplayer);
            if (player == null)
                return;

            if (isInVehiclebay)
            {
                EvaluateUsage(nelsonplayer.channel.owner);

                if (!Data.CrewSeats.Contains(toSeatIndex))
                {
                    if (!TransportTable.ContainsKey(player.Steam64))
                        TransportTable.Add(player.Steam64, player.Position);
                }
                else
                    TransportTable.Remove(player.Steam64);
            }
        }

        public void EvaluateUsage(SteamPlayer player)
        {
            byte currentSeat = player.player.movement.getSeat();
            bool isCrewSeat = Data.CrewSeats.Contains(currentSeat);

            if (currentSeat == 0 || isCrewSeat)
            {
                ulong Steam64 = player.playerID.steamID.m_SteamID;

                if (TimeEnteredTable.TryGetValue(Steam64, out DateTime start))
                {
                    double time = (DateTime.Now - start).TotalSeconds;
                    if (!UsageTable.ContainsKey(Steam64))
                        UsageTable.Add(Steam64, time);
                    else
                        UsageTable[Steam64] += time;
                }
            }
        }
        public void StartForceLoadSupplies(UCPlayer caller, ESupplyType type, int amount)
        {
            forceSupplyLoop = StartCoroutine(ForceSupplyLoop(caller, type, amount));
        }
        public void TryStartAutoLoadSupplies()
        {
            if (IsResupplied || autoSupplyLoop != null || Data.Metadata is null || Data.Metadata.TrunkItems is null || Vehicle.trunkItems is null)
                return;

            autoSupplyLoop = StartCoroutine(AutoSupplyLoop());

        }
        private IEnumerator<WaitForSeconds> ForceSupplyLoop(UCPlayer caller, ESupplyType type, int amount)
        {
            Guid buildGUID = Guid.Empty;
            Guid ammoGUID = Guid.Empty;

            ItemAsset supplyAsset;

            if (Team == 1)
            {
                buildGUID = Gamemode.Config.Items.T1Build;
                ammoGUID = Gamemode.Config.Items.T1Ammo;
            }
            else if (Team == 2)
            {
                buildGUID = Gamemode.Config.Items.T2Build;
                ammoGUID = Gamemode.Config.Items.T2Ammo;
            }

            if (type == ESupplyType.BUILD)
            {
                supplyAsset = Assets.find(buildGUID) as ItemAsset;
                //caller.Message("load_st_build", amount.ToString());
            }
            else if (type == ESupplyType.AMMO)
            {
                supplyAsset = Assets.find(ammoGUID) as ItemAsset;
                //caller.Message("load_st_ammo", amount.ToString());
            }
            else
            {
                caller.Message("load_e_itemassetnotfound");
                yield break;
            }

            int existingCount = 0;
            int addedBackCount = 0;
            int addedNewCount = 0;
            int loaderBreak = 0;

            var oldTrunkItems = new List<ItemJar>();
            for (int i = Vehicle.trunkItems.items.Count - 1; i >= 0; i--)
            {
                if (Vehicle.trunkItems.items[i].item.id == supplyAsset.id)
                    existingCount++;

                oldTrunkItems.Add(Vehicle.trunkItems.items[i]);
                Vehicle.trunkItems.removeItem(Vehicle.trunkItems.getIndex(Vehicle.trunkItems.items[i].x, Vehicle.trunkItems.items[i].y));
                
            }

            bool shouldAddMoreItems = true;
            foreach (var item in oldTrunkItems)
            {
                if (item.item.id == supplyAsset.id)
                {
                    var newItem = new Item(item.item.id, true) { metadata = item.item.metadata };
                    if (Vehicle.trunkItems.tryAddItem(newItem))
                    {
                        addedBackCount++;
                        loaderBreak++;
                        if (loaderBreak >= 3)
                        {
                            loaderBreak = 0;
                            if (supplyAsset.GUID == buildGUID)
                                EffectManager.sendEffect(25997, EffectManager.MEDIUM, Vehicle.transform.position);
                            else
                                EffectManager.sendEffect(25998, EffectManager.MEDIUM, Vehicle.transform.position);
                            yield return new WaitForSeconds(1);

                            while (!(Vehicle.speed >= -1 && Vehicle.speed <= 1))
                                yield return new WaitForSeconds(1);
                        }
                        if (addedBackCount >= amount)
                        {
                            shouldAddMoreItems = false;
                            break;
                        }
                    }
                }
            }

            if (shouldAddMoreItems)
            {
                for (int i = 0; i < amount - existingCount; i++)
                {
                    if (Vehicle.trunkItems.tryAddItem(new Item(supplyAsset.id, true)))
                    {
                        addedNewCount++;
                        loaderBreak++;
                        if (loaderBreak >= 3)
                        {
                            loaderBreak = 0;
                            if (supplyAsset.GUID == buildGUID)
                                EffectManager.sendEffect(25997, EffectManager.MEDIUM, Vehicle.transform.position);
                            else
                                EffectManager.sendEffect(25998, EffectManager.MEDIUM, Vehicle.transform.position);
                            yield return new WaitForSeconds(1);

                            while (!(Vehicle.speed >= -1 && Vehicle.speed <= 1))
                                yield return new WaitForSeconds(1);
                        }
                    }
                }
            }
            foreach (var item in oldTrunkItems)
            {
                if (item.item.id != supplyAsset.id)
                {
                    Vehicle.trunkItems.tryAddItem(item.item);
                }
            }

            if (type == ESupplyType.BUILD)
                caller.Message("load_s_build", (addedBackCount + addedNewCount).ToString());
            else if (type == ESupplyType.AMMO)
                caller.Message("load_s_ammo", (addedBackCount + addedNewCount).ToString());

            forceSupplyLoop = null;
        }
        private IEnumerator<WaitForSeconds> AutoSupplyLoop()
        {
            Guid buildGUID = Guid.Empty;
            Guid ammoGUID = Guid.Empty;

            if (Team == 1)
            {
                buildGUID = Gamemode.Config.Items.T1Build;
                ammoGUID = Gamemode.Config.Items.T1Ammo;
            }
            else if (Team == 2)
            {
                buildGUID = Gamemode.Config.Items.T2Build;
                ammoGUID = Gamemode.Config.Items.T2Ammo;
            }

            ItemAsset build = Assets.find(buildGUID) as ItemAsset;
            ItemAsset ammo = Assets.find(ammoGUID) as ItemAsset;

            int loaderCount = 0;

            var trunk = Data.Metadata.TrunkItems;
            for (int i = 0; i < trunk.Count; i++)
            {
                ItemAsset asset = null;
                if (trunk[i].id == buildGUID) asset = build;
                else if (trunk[i].id == ammoGUID) asset = ammo;
                else asset = Assets.find(trunk[i].id) as ItemAsset;

                if (asset != null && Vehicle.trunkItems.checkSpaceEmpty(trunk[i].x, trunk[i].y, asset.size_x, asset.size_y, trunk[i].rotation))
                {
                    var item = new Item(asset.id, true) { state = Convert.FromBase64String(trunk[i].metadata) };
                    Vehicle.trunkItems.addItem(trunk[i].x, trunk[i].y, trunk[i].rotation, item);
                    loaderCount++;

                    if (loaderCount >= 3)
                    {
                        loaderCount = 0;
                        if (asset.GUID == buildGUID)
                            EffectManager.sendEffect(25997, EffectManager.MEDIUM, Vehicle.transform.position);
                        else
                            EffectManager.sendEffect(25998, EffectManager.MEDIUM, Vehicle.transform.position);

                        yield return new WaitForSeconds(1);
                        while (!(Vehicle.speed >= -1 && Vehicle.speed <= 1))
                            yield return new WaitForSeconds(1);
                    }
                }
            }
            IsResupplied = true;
            autoSupplyLoop = null;
        }
        private IEnumerator<WaitForSeconds> QuotaLoop()
        {
            int tick = 0;

            while (true)
            {
                yield return new WaitForSeconds(3);

                if (F.IsInMain(Vehicle.transform.position))
                {
                    //var ammoCrate = UCBarricadeManager.GetNearbyBarricades(Gamemode.Config.Barricades.AmmoCrateGUID, 30, Vehicle.transform.position, true).FirstOrDefault();
                    if (Vehicle.speed >= -1 && Vehicle.speed <= 1)
                    {
                        TryStartAutoLoadSupplies();
                    }
                }
                else if (IsResupplied)
                {
                    IsResupplied = false;
                }

                tick++;
                if (tick >= 20)
                {
                    _quota += 0.5F;
                    tick = 0;
                }
            }
        }
    }
    public enum ESupplyType
    {
        BUILD,
        AMMO
    }
}

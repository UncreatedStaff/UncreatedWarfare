using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
namespace Uncreated.Warfare.Components
{
    public class VehicleComponent : MonoBehaviour
    {
        public Guid item;
        public InteractableVehicle Vehicle;
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

        private Coroutine quotaLoop;

        public void Initialize(InteractableVehicle vehicle)
        {
            Vehicle = vehicle;

            lastDamager = 0;
            TransportTable = new Dictionary<ulong, Vector3>();
            UsageTable = new Dictionary<ulong, double>();
            TimeEnteredTable = new Dictionary<ulong, DateTime>();
            TimeRewardedTable = new Dictionary<ulong, DateTime>();
            DamageTable = new Dictionary<ulong, KeyValuePair<ushort, DateTime>>();

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
        private IEnumerator<WaitForSeconds> QuotaLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(60);

                _quota += 0.5F;
            }
        }
    }
}

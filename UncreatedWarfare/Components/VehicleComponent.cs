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
        public CSteamID owner;
        public Guid item;
        public bool isVehicle = false;
        public EDamageOrigin lastDamageOrigin;
        public ulong lastDamager = 0;
        private Dictionary<ulong, Vector3> TransportTable = new Dictionary<ulong, Vector3>();

        public void OnPlayerEnteredVehicle(Player nelsonplayer, InteractableVehicle vehicle)
        {
            UCPlayer player = UCPlayer.FromPlayer(nelsonplayer);
            if (player == null)
                return;
            
            byte toSeat = 0;
            for (byte i = 0; i < vehicle.passengers.Length; i++)
            {
                if (vehicle.passengers[i].player == null)
                {
                    toSeat = i;
                }
            }
            if (VehicleBay.VehicleExists(vehicle.asset.GUID, out var data) &&
                !data.CrewSeats.Contains(toSeat))
            {
                if (!TransportTable.ContainsKey(player.Steam64))
                    TransportTable.Add(player.Steam64, player.Position);
                else
                    TransportTable[player.Steam64] = player.Position;
            }
        }
        public void OnPlayerExitedVehicle(Player nelsonplayer, InteractableVehicle vehicle)
        {
            UCPlayer player = UCPlayer.FromPlayer(nelsonplayer);
            if (player == null)
                return;

            if (vehicle.passengers[0].player == null)
                return;

            if (vehicle.passengers[0].player.player.channel.owner.playerID.steamID == player.CSteamID)
                return;

            if (TransportTable.TryGetValue(player.Steam64, out Vector3 original))
            {
                float distance = (player.Position - original).magnitude;
                if (distance >= 200 && !(player.KitClass == EClass.CREWMAN || player.KitClass == EClass.PILOT))
                {
                    int amount = (int)(Math.Floor(distance / 100) * 5) + 15;

                    Points.AwardXP(vehicle.passengers[0].player.player, amount, Translation.Translate("xp_transporting_players", vehicle.passengers[0].player.player));
                }
                TransportTable.Remove(player.Steam64);
            }
        }
        public void OnPlayerSwapSeatRequested(Player nelsonplayer, InteractableVehicle vehicle, byte toSeatIndex)
        {
            UCPlayer player = UCPlayer.FromPlayer(nelsonplayer);
            if (player == null)
                return;

            if (VehicleBay.VehicleExists(vehicle.asset.GUID, out var data) &&
                !data.CrewSeats.Contains(toSeatIndex))
            {
                if (!TransportTable.ContainsKey(player.Steam64))
                    TransportTable.Add(player.Steam64, player.Position);
                else
                    TransportTable[player.Steam64] = player.Position;
            }
            else
                TransportTable.Remove(player.Steam64);
        }
    }
}

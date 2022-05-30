using HarmonyLib;
using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare
{
    public static partial class Patches
    {
        [HarmonyPatch]
        public class VehiclePatches
        {
            // SDG.Unturned.PlayerAnimator
            /// <summary>
            /// Postfix of <see cref="InteractableVehicle.tryAddPlayer(out byte, Player)"/> to control which seats the player enters.
            /// </summary>
            [HarmonyPatch(typeof(InteractableVehicle), nameof(InteractableVehicle.tryAddPlayer))]
            [HarmonyPostfix]
            static void TryAddPlayerPostfix(ref byte seat, Player player, InteractableVehicle __instance, ref bool __result)
            {
#if DEBUG
                using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
                if (__result)
                {
                    if (VehicleBay.VehicleExists(__instance.asset.GUID, out VehicleData vehicleData))
                    {
                        UCPlayer? enterer = UCPlayer.FromPlayer(player);

                        if (enterer != null)
                        {
                            if (vehicleData.Type == EVehicleType.EMPLACEMENT)
                            {
                                if (!VehicleBay.TryGetFirstNonDriverSeat(__instance, out seat))
                                {
                                    __result = false;
                                }
                            }
                            else if (vehicleData.Type == EVehicleType.JET)
                            {
                                if (VehicleBay.CountCrewmen(__instance, vehicleData) >= 2)
                                {
                                    __result = false;
                                }
                            }
                            else
                            {
                                UCPlayer? owner = UCPlayer.FromCSteamID(__instance.lockedOwner);

                                if (vehicleData.RequiredClass != EClass.NONE) // vehicle requires crewman or pilot
                                {
                                    if (enterer.KitClass == vehicleData.RequiredClass) // for crewman trying to enter a crewed vehicle
                                    {
                                        if (seat == 0)
                                        {
                                            bool canEnterDriverSeat = owner is null || enterer == owner || VehicleBay.IsOwnerInVehicle(__instance, owner) || (owner is not null && owner.Squad != null && owner.Squad.Members.Contains(enterer) || (owner!.Position - __instance.transform.position).sqrMagnitude > Math.Pow(200, 2));

                                            if (!canEnterDriverSeat)
                                            {
                                                if (!VehicleBay.TryGetFirstNonDriverSeat(__instance, out seat))
                                                {
                                                    if (owner!.Squad == null)
                                                        enterer.Message("vehicle_wait_for_owner", owner.CharacterName);
                                                    else
                                                        enterer.Message("vehicle_wait_for_owner_or_squad", owner.CharacterName, owner.Squad.Name);

                                                    __result = false;
                                                }
                                            }
                                        }
                                    }
                                    else // for non crewman trying to enter a crewed vehicle
                                    {
                                        if (!VehicleBay.TryGetFirstNonCrewSeat(__instance, vehicleData, out seat))
                                        {

                                            enterer.Message("vehicle_no_passenger_seats");
                                            __result = false;
                                        }
                                    }
                                }
                                else
                                {
                                    if (seat == 0)
                                    {
                                        bool canEnterDriverSeat = owner is null || enterer == owner || (owner.Squad != null && owner.Squad.Members.Contains(enterer)) || (owner.Position - __instance.transform.position).sqrMagnitude > Math.Pow(200, 2) || (vehicleData.Type == EVehicleType.LOGISTICS && FOB.GetNearestFOB(__instance.transform.position, EFOBRadius.FULL_WITH_BUNKER_CHECK, __instance.lockedGroup.m_SteamID) != null);

                                        if (!canEnterDriverSeat)
                                        {
                                            if (!VehicleBay.TryGetFirstNonDriverSeat(__instance, out seat))
                                            {
                                                if (owner!.Squad == null)
                                                    enterer.Message("vehicle_wait_for_owner", owner.CharacterName);
                                                else
                                                    enterer.Message("vehicle_wait_for_owner_or_squad", owner.CharacterName, owner.Squad.Name);

                                                __result = false;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (seat == 0 && __instance.transform.TryGetComponent(out VehicleComponent c))
                    {   
                        c.LastDriver = player.channel.owner.playerID.steamID.m_SteamID;
                        c.LastDriverTime = Time.realtimeSinceStartup;
                    }
                }
            }
        }
    }
}

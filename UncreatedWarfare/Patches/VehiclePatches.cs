using HarmonyLib;
using SDG.Unturned;
using System;
using JetBrains.Annotations;
using Uncreated.Warfare.Components;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Vehicles;
using UnityEngine;

namespace Uncreated.Warfare.Harmony;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Local

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
        [UsedImplicitly]
        static void TryAddPlayerPostfix(ref byte seat, Player player, InteractableVehicle __instance, ref bool __result)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (__result)
            {
                VehicleData? data = VehicleBay.GetSingletonQuick()?.GetDataSync(__instance.asset.GUID);
                if (data != null)
                {
                    UCPlayer? enterer = UCPlayer.FromPlayer(player);

                    if (enterer != null)
                    {
                        if (VehicleData.IsEmplacement(data.Type))
                        {
                            if (!VehicleSpawner.TryGetFirstNonDriverSeat(__instance, out seat))
                            {
                                __result = false;
                                return;
                            }
                        }
                        else if (data.Type == VehicleType.Jet)
                        {
                            if (VehicleSpawner.CountCrewmen(__instance, data) >= 2)
                            {
                                __result = false;
                                return;
                            }
                        }

                        UCPlayer? owner = UCPlayer.FromCSteamID(__instance.lockedOwner);

                        if (data.RequiredClass != Class.None) // vehicle requires crewman or pilot
                        {
                            if (enterer.KitClass == data.RequiredClass) // for crewman trying to enter a crewed vehicle
                            {
                                if (seat == 0)
                                {
                                    bool canEnterDriverSeat = owner is null || enterer == owner || VehicleSpawner.IsOwnerInVehicle(__instance, owner) || (owner is not null && owner.Squad != null && owner.Squad.Members.Contains(enterer) || (owner!.Position - __instance.transform.position).sqrMagnitude > Math.Pow(200, 2));

                                    if (!canEnterDriverSeat)
                                    {
                                        if (!VehicleSpawner.TryGetFirstNonDriverSeat(__instance, out seat))
                                        {
                                            if (owner!.Squad == null)
                                                enterer.SendChat(T.VehicleWaitForOwner, owner);
                                            else
                                                enterer.SendChat(T.VehicleWaitForOwnerOrSquad, owner, owner.Squad);

                                            __result = false;
                                        }
                                    }
                                }
                            }
                            else // for non crewman trying to enter a crewed vehicle
                            {
                                if (!VehicleSpawner.TryGetFirstNonCrewSeat(__instance, data, out seat))
                                {
                                    enterer.SendChat(T.VehicleNoPassengerSeats);
                                    __result = false;
                                }
                            }
                        }
                        else
                        {
                            if (seat == 0)
                            {
                                bool canEnterDriverSeat =
                                    owner is null ||
                                    enterer == owner ||
                                    (owner.Squad != null && owner.Squad.Members.Contains(enterer)) ||
                                    (owner.Position - __instance.transform.position).sqrMagnitude > Math.Pow(200, 2) ||
                                    (data.Type == VehicleType.LogisticsGround && FOB.GetNearestFOB(__instance.transform.position, FobRadius.FullBunkerDependant, __instance.lockedGroup.m_SteamID) != null);

                                if (!canEnterDriverSeat)
                                {
                                    if (!VehicleSpawner.TryGetFirstNonDriverSeat(__instance, out seat))
                                    {
                                        if (owner!.Squad == null)
                                            enterer.SendChat(T.VehicleWaitForOwner, owner);
                                        else
                                            enterer.SendChat(T.VehicleWaitForOwnerOrSquad, owner, owner.Squad);

                                        __result = false;
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

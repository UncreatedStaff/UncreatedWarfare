using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags.Invasion;
using Uncreated.Warfare.Gamemodes.Insurgency;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Point;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using UnityEngine;
using VehicleSpawn = Uncreated.Warfare.Vehicles.VehicleSpawn;

namespace Uncreated.Warfare.Commands
{
    public class BuyCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "buy";
        public string Help => "Buy a kit permanently by looking at a sign or a vehicle by looking at the vehicle, then do /buy.";
        public string Syntax => "/buy";
        public List<string> Aliases => new List<string>(1) { "buy" };
        public List<string> Permissions => new List<string>(1) { "uc.buy" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            // dont allow requesting between game end and leaderboard
            if (Data.Gamemode.State != EState.ACTIVE && Data.Gamemode.State != EState.STAGING)
            {
                return;
            }
            UnturnedPlayer player = (UnturnedPlayer)caller;
            if (!Data.Is(out IKitRequests ctf))
            {
                player.Message("command_e_gamemode");
                return;
            }
            UCPlayer? ucplayer = UCPlayer.FromIRocketPlayer(caller);
            if (player == null || ucplayer == null) return;
            if (ucplayer.Position == Vector3.zero) return;
            ulong team = ucplayer.GetTeam();
            InteractableVehicle? vehicle = UCBarricadeManager.GetVehicleFromLook(player.Player.look);
            InteractableSign? signlook = UCBarricadeManager.GetInteractableFromLook<InteractableSign>(player.Player.look);
            if (signlook != null)
            {
                if (!RequestSigns.SignExists(signlook, out RequestSign requestsign))
                {
                    //if (!VehicleSigns.SignExists(signlook, out VehicleSign vbsign))
                    //{
                    //    ucplayer.Message("request_kit_e_kitnoexist");
                    //    return;
                    //}
                    //if (vbsign.bay != default && vbsign.bay.HasLinkedVehicle(out InteractableVehicle veh))
                    //{
                    //    if (Data.Is<IVehicles>(out _))
                    //    {
                    //        if (veh != default)
                    //            BuyVehicle(ucplayer, veh, team);
                    //    }
                    //    else
                    //    {
                    //        ucplayer.SendChat("command_e_gamemode");
                    //    }
                    //}
                    //return;
                    ucplayer.Message("request_kit_e_kitnoexist");
                    return;
                }
                if (Data.Gamemode is not IKitRequests)
                {
                    ucplayer.SendChat("command_e_gamemode");
                    return;
                }
                if (requestsign.kit_name.StartsWith("loadout_"))
                {
                    ucplayer.Message("request_kit_e_notbuyablecredits");
                    return;
                }
                if (!KitManager.KitExists(requestsign.kit_name, out Kit kit))
                {
                    ucplayer.Message("request_kit_e_kitnoexist");
                    return;
                }
                else if (ucplayer.Rank.Level < kit.UnlockLevel)
                {
                    ucplayer.Message("request_kit_e_wronglevel", RankData.GetRankName(kit.UnlockLevel));
                    return;
                }
                if (kit.IsPremium)
                {
                    ucplayer.Message("request_kit_e_notbuyablecredits");
                    return;
                }
                else if (kit.CreditCost == 0 || ucplayer.AccessibleKits.Contains(kit.Name))
                {
                    ucplayer.Message("request_kit_e_alreadyhaskit");
                    return;
                }
                else if (ucplayer.CachedCredits < kit.CreditCost)
                {
                    ucplayer.Message("request_kit_e_notenoughcredits", (kit.CreditCost - ucplayer.CachedCredits).ToString());
                    return;
                }

                Task.Run(
                    async () => 
                    {
                        await Data.DatabaseManager.AddAccessibleKit(ucplayer.Steam64, kit.Name);

                        await UCWarfare.ToUpdate();
                        ucplayer.AccessibleKits.Add(kit.Name);

                        RequestSigns.InvokeLangUpdateForSignsOfKit(ucplayer.SteamPlayer, kit.Name);
                        EffectManager.sendEffect(81, 7f, (requestsign.barricadetransform?.position).GetValueOrDefault());
                        ucplayer.Message("request_kit_boughtcredits", kit.CreditCost.ToString());
                        Points.AwardCredits(ucplayer, -kit.CreditCost, isPurchase: true);
                        ActionLog.Add(EActionLogType.BUY_KIT, "BOUGHT KIT " + kit.Name + " FOR " + kit.CreditCost + " CREDITS", ucplayer);
                        L.Log(F.GetPlayerOriginalNames(ucplayer).PlayerName + " (" + ucplayer.Steam64 + ") bought " + kit.Name);
                    } );

            }
            else
            {
                ucplayer.Message("request_not_looking");
            }
        }
        //private void BuyVehicle(UCPlayer ucplayer, InteractableVehicle vehicle, ulong team)
        //{
        //    if (!VehicleBay.VehicleExists(vehicle.asset.GUID, out VehicleData data))
        //    {
        //        ucplayer.Message("request_vehicle_e_notbuyable");
        //        return;
        //    }
        //    else if (data.Team != team)
        //    {
        //        ucplayer.Message("request_vehicle_e_notinteam");
        //        return;
        //    }
        //    else if (data.AllowedUsers.Contains(ucplayer.Steam64))
        //    {
        //        ucplayer.Message("request_vehicle_e_alreadyowned");
        //        return;
        //    }
        //    else if (data.CreditCost < ucplayer.CachedCredits)
        //    {
        //        ucplayer.Message("request_vehicle_e_notenoughcredits");
        //        return;
        //    }

        //    data.AllowedUsers.Add(ucplayer.Steam64);
        //    VehicleBay.Save();

        //    ucplayer.Message("request_vehicle_boughtcredits");
        //}
    }
}

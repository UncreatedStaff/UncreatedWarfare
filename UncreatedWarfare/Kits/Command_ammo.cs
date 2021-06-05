using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.FOBs;
using Uncreated.Warfare.Teams;
using UnityEngine;
using static UnityEngine.Physics;

namespace Uncreated.Warfare.Kits
{
    class Command_refill : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;

        public string Name => "ammo";

        public string Help => "refills your current loadout or vehicle";

        public string Syntax => "/ammo";

        public List<string> Aliases => new List<string>();

        public List<string> Permissions => new List<string>() { "uc.ammo" };
        //private BuildManager BuildManager => UCWarfare.I.BuildManager;

        public void Execute(IRocketPlayer caller, string[] arguments)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;

            var barricadeData = BuildManager.GetBarricadeFromLook(player);
            var storage = BuildManager.GetInteractableFromLook<InteractableStorage>(player.Player.look);

            if (barricadeData != null)
            {
                if (barricadeData.barricade.id != Data.FOBManager.config.AmmoCrateID)
                {
                    player.Message("ammo_error_nocrate");
                    return;
                }
                if (!TeamManager.IsTeam1(player) && !TeamManager.IsTeam2(player))
                {
                    player.Message("Join a team first and get a kit.");
                    return;
                }
                if ((TeamManager.IsTeam1(player) && !storage.items.items.Exists(j => j.item.id == Data.FOBManager.config.Team1AmmoID)) || 
                    (TeamManager.IsTeam2(player) && !storage.items.items.Exists(j => j.item.id == Data.FOBManager.config.Team2AmmoID)))
                {
                    player.Message("This ammo box has no ammo. If you have logistics truck, go and fetch some more crates from main.");
                    return;
                }
                if (!KitManager.HasKit(player.CSteamID, out var kit))
                {
                    player.Message("ammo_error_nokit");
                    return;
                }

                KitManager.ResupplyKit(player, kit);

                player.Message("ammo_success");

                if (TeamManager.IsTeam1(player))
                    RemoveSingleItemFromStorage(storage, Data.FOBManager.config.Team1AmmoID);
                else if (TeamManager.IsTeam2(player))
                    RemoveSingleItemFromStorage(storage, Data.FOBManager.config.Team2AmmoID);
            }
            else
            {
                InteractableVehicle vehicle = GetVehicleFromLook(player);

                if (vehicle != null)
                {

                }
            }
        }
    }
}

using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UncreatedWarfare.Teams;
using UnityEngine;
using static UncreatedWarfare.FOBs.FOBConfig;
using Logger = Rocket.Core.Logging.Logger;

namespace UncreatedWarfare.FOBs
{
    class Command_build : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;

        public string Name => "build";

        public string Help => "Builds a FOB on an existing FOB base";

        public string Syntax => "/build";

        public List<string> Aliases => new List<string>();

        public List<string> Permissions
        {
            get
            {
                List<string> perms = new List<string>();
                perms.Add("build");
                return perms;
            }
        }

        private TeamManager teams => UCWarfare.I.TeamManager;
        private FOBManager FOBManager => UCWarfare.I.FOBManager;
        private BuildManager BuildManager => UCWarfare.I.BuildManager;

        public void Execute(IRocketPlayer caller, string[] arguments)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;

            if (!teams.hasTeam(player))
            {
                player.Message("build_error_noteam");
                return;
            }

            BarricadeData foundation = BuildManager.GetBarricadeFromLook(player);

            if (foundation == null || !teams.IsFriendly(player, foundation.group))
            {
                player.Message("build_error_notfriendly");
                return;
            }

            if (foundation.barricade.id == FOBManager.config.FOBBaseID)
            {
                BuildManager.TryBuildFOB(foundation, player);
            }
            else if (foundation.barricade.id == FOBManager.config.AmmoCrateBaseID)
            {
                BuildManager.TryBuildAmmoCrate(foundation, player);
            }
            else if (foundation.barricade.id == FOBManager.config.RepairStationBaseID)
            {
                BuildManager.TryBuildRepairStation(foundation, player);
            }
            else
            {
                Emplacement emplacement = FOBManager.config.Emplacements.Find(e => e.baseID == foundation.barricade.id);

                if (emplacement != null)
                {
                    BuildManager.TryBuildEmplacement(foundation, player, emplacement);
                    return;
                }

                Fortification fortification = FOBManager.config.Fortifications.Find(f => f.base_id == foundation.barricade.id);

                if (fortification != null)
                {
                    BuildManager.TryBuildFortification(foundation, player, fortification);
                    return;
                }

                player.Message("build_error_notbuildable");
            }

        }


    }
}

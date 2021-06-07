﻿using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Teams;
using UnityEngine;
using Uncreated.Warfare.FOBs;
using static Uncreated.Warfare.FOBs.FOBConfig;

namespace Uncreated.Warfare.FOBs
{
    class Command_build : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;

        public string Name => "build";

        public string Help => "Builds a FOB on an existing FOB base";

        public string Syntax => "/build";

        public List<string> Aliases => new List<string>();

        public List<string> Permissions => new List<string>() { "uc.build" };

        public void Execute(IRocketPlayer caller, string[] arguments)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;

            if (!TeamManager.HasTeam(player))
            {
                player.Message("build_error_noteam");
                return;
            }

            BarricadeData foundation = UCBarricadeManager.GetBarricadeDataFromLook(player);

            if (foundation == null || !TeamManager.IsFriendly(player, foundation.group))
            {
                player.Message("build_error_notfriendly");
                return;
            }

            if (foundation.barricade.id == FOBManager.config.FOBBaseID)
            {
                Data.BuildManager.TryBuildFOB(foundation, player);
            }
            else if (foundation.barricade.id == FOBManager.config.AmmoCrateBaseID)
            {
                Data.BuildManager.TryBuildAmmoCrate(foundation, player);
            }
            else if (foundation.barricade.id == FOBManager.config.RepairStationBaseID)
            {
                Data.BuildManager.TryBuildRepairStation(foundation, player);
            }
            else
            {
                Emplacement emplacement = FOBManager.config.Emplacements.Find(e => e.baseID == foundation.barricade.id);

                if (emplacement != null)
                {
                    Data.BuildManager.TryBuildEmplacement(foundation, player, emplacement);
                    return;
                }

                Fortification fortification = FOBManager.config.Fortifications.Find(f => f.base_id == foundation.barricade.id);

                if (fortification != null)
                {
                    Data.BuildManager.TryBuildFortification(foundation, player, fortification);
                    return;
                }

                player.Message("build_error_notbuildable");
            }

        }


    }
}

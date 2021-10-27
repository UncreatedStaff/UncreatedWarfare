using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System.Collections.Generic;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Teams;

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
            if (!Data.TryMode(out TeamCTF ctf))
            {
                player.SendChat("command_e_gamemode");
                return;
            }
            if (!TeamManager.HasTeam(player))
            {
                player.SendChat("build_error_noteam");
                return;
            }
            ulong team = player.GetTeam();
            SDG.Unturned.BarricadeData foundation = UCBarricadeManager.GetBarricadeDataFromLook(player.Player.look);
            if (foundation == null)
            {
                player.SendChat("build_error_noteam");
                return;
            }
            if (!TeamManager.IsFriendly(player, foundation.group))
            {
                player.SendChat("build_error_notfriendly");
                return;
            }

            if (foundation.barricade.id == FOBManager.config.Data.FOBBaseID)
            {
                if ((team == 1 ? FOBManager.Team1FOBs : FOBManager.Team2FOBs).Count > 9)
                {
                    player.SendChat("build_error_too_many_fobs");
                    return;
                }
                BuildManager.TryBuildFOB(foundation, player);
            }
            else if (foundation.barricade.id == FOBManager.config.Data.AmmoCrateBaseID)
            {
                BuildManager.TryBuildAmmoCrate(foundation, player);
            }
            else if (foundation.barricade.id == FOBManager.config.Data.RepairStationBaseID)
            {
                BuildManager.TryBuildRepairStation(foundation, player);
            }
            else
            {
                Emplacement emplacement = FOBManager.config.Data.Emplacements.Find(e => e.baseID == foundation.barricade.id);

                if (emplacement != default)
                {
                    BuildManager.TryBuildEmplacement(foundation, player, emplacement);
                    return;
                }

                Fortification fortification = FOBManager.config.Data.Fortifications.Find(f => f.base_id == foundation.barricade.id);

                if (fortification != default)
                {
                    BuildManager.TryBuildFortification(foundation, player, fortification);
                    return;
                }
                player.SendChat("build_error_notbuildable");
            }
        }
    }
}

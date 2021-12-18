using Rocket.API;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System.Collections.Generic;
using Uncreated.Warfare.Gamemodes;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Interfaces;
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
            player.SendChat("Hit the foundation with your Entrenching Tool to build it.");
        }
    }
}

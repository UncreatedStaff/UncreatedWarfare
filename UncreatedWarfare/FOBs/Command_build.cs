using Rocket.API;
using Rocket.Unturned.Player;
using System.Collections.Generic;

namespace Uncreated.Warfare.FOBs
{
    class BuildCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "build";
        public string Help => "Builds a FOB on an existing FOB base";
        public string Syntax => "/build";
        private readonly List<string> _aliases = new List<string>(0);
        public List<string> Aliases => _aliases;
        private readonly List<string> _permissions = new List<string>(1) { "uc.build" };
		public List<string> Permissions => _permissions;
        public void Execute(IRocketPlayer caller, string[] arguments)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;
            player.SendChat("Hit the foundation with your Entrenching Tool to build it.");
        }
    }
}

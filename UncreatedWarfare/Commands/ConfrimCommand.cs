using Rocket.API;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncreated.Warfare.Commands
{
    public class ConfirmCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "confirm";
        public string Help => "Confirm a current action.";
        public string Syntax => "/confirm";
        private readonly List<string> _aliases = new List<string>(0);
        public List<string> Aliases => _aliases;
        private readonly List<string> _permissions = new List<string>(1) { "uc.confirm" };
		public List<string> Permissions => _permissions;
        public void Execute(IRocketPlayer caller, string[] command) { }
    }
}

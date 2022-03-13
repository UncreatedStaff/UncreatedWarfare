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
        public List<string> Aliases => new List<string>(0);
        public List<string> Permissions => new List<string>(1) { "uc.confirm" };
        public void Execute(IRocketPlayer caller, string[] command) { }
    }
}

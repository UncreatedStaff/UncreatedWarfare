using Rocket.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Networking;
using Uncreated.Networking.Encoding;

namespace Uncreated.Warfare.Commands
{
    public class ReportCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "report";
        public string Help => "Use to report a player for specific actions. Use /report reasons for examples.";
        public string Syntax => "/report <\"reasons\" | player> <reason> <custom message...>";
        public List<string> Aliases => new List<string>(0);
        public List<string> Permissions => new List<string>(1) { "uc.report" };
        public void Execute(IRocketPlayer caller, string[] command)
        {

        }
    }
}

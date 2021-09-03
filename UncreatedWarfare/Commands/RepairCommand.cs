using Rocket.API;
using System.Collections.Generic;

namespace Uncreated.Warfare.Commands
{
    public class Command_repair : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "repair";
        public string Help => "repairs vehicles";
        public string Syntax => "/repair";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string>() { "uc.repair" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
            UCPlayer player = UCPlayer.FromIRocketPlayer(caller);

            player.Message("Repair Stations now auto-repair nearby vehicles.");
        }
    }
}

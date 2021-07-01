using Rocket.API;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Kits;

namespace Uncreated.Warfare.Commands
{
    public class Command_kits : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "kits";
        public string Help => "shows you a list of kits";
        public string Syntax => "/kits";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string>() { "kits" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
            UCPlayer player = UCPlayer.FromIRocketPlayer(caller);

            var kits = KitManager.GetAccessibleKits(player.Steam64).ToList();

            if (kits.Count > 0) // create kit
            {
                string list = "";

                for (int i = 0; i < kits.Count; i++)
                {
                    list += kits[i].Team == player.GetTeam() ? $"<color=#c2fff5>{kits[i].Name}</color>" : $"<color=#97adaa>{kits[i].Name}</color>";
                    if (i < kits.Count - 1)
                        list += ", ";
                }

                player.Message("kits_heading");
                player.Message(list);
            }
            else
            {
                player.Message("kits_nokits");
            }
        }
    }
}

using Rocket.API;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public List<string> Permissions => new List<string>() { "uc.kits" };
        public void Execute(IRocketPlayer caller, string[] command)
        {
            UCPlayer player = UCPlayer.FromIRocketPlayer(caller);

            List<Kit> kits = KitManager.GetAccessibleKits(player.Steam64).ToList();

            if (kits.Count > 0)
            {
                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < kits.Count; i++)
                {
                    if (i != 0) sb.Append(", ");
                    sb.Append(kits[i].Team == player.GetTeam() ? $"<color=#c2fff5>{kits[i].Name}</color>" : $"<color=#97adaa>{kits[i].Name}</color>");
                }

                player.SendChat("kits_heading");
                player.SendChat(sb.ToString());
            }
            else
            {
                player.SendChat("kits_nokits");
            }
        }
    }
}

using Rocket.API;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;

namespace Uncreated.Warfare.Commands
{
    public class KitsCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "kits";
        public string Help => "shows you a list of kits";
        public string Syntax => "/kits";
        private readonly List<string> _aliases = new List<string>(0);
        public List<string> Aliases => _aliases;
        private readonly List<string> _permissions = new List<string>() { "uc.kits" };
		public List<string> Permissions => _permissions;
        public void Execute(IRocketPlayer caller, string[] command)
        {
            UCPlayer? player = UCPlayer.FromIRocketPlayer(caller);
            if (player == null) return;

            if (!Data.Is(out IKitRequests ctf))
            {
                player.SendChat("command_e_gamemode");
                return;
            }
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

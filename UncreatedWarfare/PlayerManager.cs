//using Rocket.Unturned;
//using Rocket.Unturned.Player;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Uncreated.Warfare.Kits;
//using Uncreated.Warfare.Teams;

//namespace Uncreated.Warfare.Players
//{
//    public class PlayerManager : IDisposable
//    {
//        public List<UCPlayer> OnlinePlayers;

//        public LogoutSaver logoutSaver;

//        public PlayerManager()
//        {
//            OnlinePlayers = new List<UCPlayer>();

//            U.Events.OnPlayerConnected += OnPlayerConnected;
//            U.Events.OnPlayerDisconnected += OnPlayerDisconnected;
//        }

//        private void OnPlayerConnected(UnturnedPlayer rocketplayer)
//        {
//            UCPlayer player;

//            if (logoutSaver.HasSave(rocketplayer.CSteamID, out var save))
//            {
//                player = new UCPlayer(rocketplayer, save.Team, save.Branch, save.KitClass, save.KitName);
//            }
//            else
//            {
//                player = new UCPlayer(rocketplayer, ETeam.NEUTRAL, Branch.EBranch.DEFAULT, Kit.EClass.NONE, "");
//            }

//            OnlinePlayers.Add(player);
//        }
//        private void OnPlayerDisconnected(UnturnedPlayer player)
//        {
//            OnlinePlayers.RemoveAll(p => p.steamID == player.CSteamID);
//        }

//        public void Dispose()
//        {
//            U.Events.OnPlayerConnected -= OnPlayerConnected;
//            U.Events.OnPlayerDisconnected -= OnPlayerDisconnected;
//        }
//    }
//}

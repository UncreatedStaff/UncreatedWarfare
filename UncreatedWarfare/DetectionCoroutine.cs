using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UncreatedWarfare.Flags;
using SDG.NetTransport;
using Flag = UncreatedWarfare.Flags.Flag;

namespace UncreatedWarfare
{
    partial class UCWarfare
    {
        internal List<IEnumerator<WaitForSeconds>> Coroutines;
        public void StartAllCoroutines()
        {
            foreach(IEnumerator<WaitForSeconds> coroutine in Coroutines)
            {
                StartCoroutine(coroutine);
            }
        }
        internal IEnumerator<WaitForSeconds> CheckPlayers()
        {
            //DateTime start = DateTime.Now;
            List<SteamPlayer> OnlinePlayers = Provider.clients;
            bool ttc = FlagManager.TimeToCheck;
            if (ttc)
            {
                FlagManager.EvaluatePoints(OnlinePlayers);
            }
            foreach(Flag flag in FlagManager.FlagRotation)
            {
                List<Player> LeftPlayers = flag.GetUpdatedPlayers(OnlinePlayers, out List<Player> NewPlayers);
                foreach(Player player in LeftPlayers)
                    FlagManager.RemovePlayerFromFlag(player, flag);
                foreach (Player player in NewPlayers)
                    FlagManager.AddPlayerOnFlag(player, flag);
            }
            /*
            if(ttc)
                CommandWindow.Log((DateTime.Now - start).TotalMilliseconds.ToString() + "ms");
            */
            yield return new WaitForSeconds(Config.FlagSettings.PlayerCheckSpeedSeconds);
            StartCoroutine(CheckPlayers());
        }
    }
}

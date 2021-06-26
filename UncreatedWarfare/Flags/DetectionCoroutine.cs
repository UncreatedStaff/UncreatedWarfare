using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Uncreated.Warfare.Flags;
using SDG.NetTransport;
using Flag = Uncreated.Warfare.Flags.Flag;

namespace Uncreated.Warfare
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
            try
            {
                DateTime start = DateTime.Now;
                List<SteamPlayer> OnlinePlayers = Provider.clients;
                bool ttc = Data.FlagManager.TimeToCheck;
                foreach (Flag flag in Data.FlagManager.FlagRotation)
                {
                    if (flag == null)
                    {
                        F.LogError("FLAG IS NULL");
                        continue;
                    }
                    List<Player> LeftPlayers = flag.GetUpdatedPlayers(OnlinePlayers, out List<Player> NewPlayers);
                    foreach (Player player in LeftPlayers)
                        Data.FlagManager.RemovePlayerFromFlag(player, flag);
                    foreach (Player player in NewPlayers)
                        Data.FlagManager.AddPlayerOnFlag(player, flag);
                }
                if (ttc)
                {
                    Data.FlagManager.EvaluatePoints();
                }
                
                if(ttc && CoroutineTiming)
                    F.Log((DateTime.Now - start).TotalMilliseconds.ToString(Data.Locale) + "ms");
                
            } catch (Exception ex)
            {
                F.LogError("ERROR IN DetectionCoroutine.cs: CheckPlayers():\n" + ex.ToString());
            }
            yield return new WaitForSeconds(Config.FlagSettings.PlayerCheckSpeedSeconds);
            StartCoroutine(CheckPlayers());
        }
    }
}

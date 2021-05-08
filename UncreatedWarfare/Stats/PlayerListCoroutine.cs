using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace UncreatedWarfare
{
    partial class UCWarfare
    {
        public IEnumerator<WaitForSeconds> SimulateStats()
        {
            yield return new WaitForSeconds(Config.PlayerStatsSettings.StatUpdateFrequency);
            List<SteamPlayer> online = Provider.clients;
            foreach(SteamPlayer player in online)
            {

            }
        }
    }
}

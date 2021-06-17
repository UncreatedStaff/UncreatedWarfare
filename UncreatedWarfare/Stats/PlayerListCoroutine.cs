using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Warfare.Stats;
using UnityEngine;

namespace Uncreated.Warfare
{
    partial class UCWarfare
    { 
        public IEnumerator<WaitForSeconds> SimulateStats()
        {
            yield return new WaitForSeconds(Config.PlayerStatsSettings.StatUpdateFrequency);
            List<SteamPlayer> online = Provider.clients;
            List<PlayerStatsCoroutineData> data = new List<PlayerStatsCoroutineData>();
            foreach(SteamPlayer player in online)
                data.Add(new PlayerStatsCoroutineData(player.playerID.steamID.m_SteamID, player.player.transform.position.x, player.player.transform.position.z));
        }
    }
}

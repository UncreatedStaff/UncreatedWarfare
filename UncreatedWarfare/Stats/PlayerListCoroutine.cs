using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UncreatedWarfare.Stats;
using UnityEngine;

namespace UncreatedWarfare
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
            WebInterface?.SendCoroutinePlayerData(data);
        }
        private void ReceivedResponeFromListenServer(object sender, HeardResponseEventArgs e)
        {
            string Received = e.data;
            if(Received.Length >= 4) 
            {
                string prefix = Received.Substring(0, 3);
                string data = Received.Substring(4, Received.Length - 4);
                // 76561198267927009,Too Good,220938256127229952
                if (prefix == "BAN")
                {
                    string[] args = data.Split(',');
                    if(args.Length != 3)
                    {
                        e.response = EResponseFromAsyncSocketEvent.NO_ARGS_BAN.ToString();
                        return;
                    } else
                    {
                        if(!ulong.TryParse(args[0], out ulong Steam64))
                        {
                            e.response = EResponseFromAsyncSocketEvent.NO_STEAM_ID_BAN.ToString();
                        } else
                        {
                            if (!ulong.TryParse(args[0], out ulong DiscordID))
                            {
                                e.response = EResponseFromAsyncSocketEvent.NO_DISCORD_ID_BAN.ToString();
                            }
                            else
                            {
                                
                            }
                        }
                    }
                }
            }
        }
    }
}

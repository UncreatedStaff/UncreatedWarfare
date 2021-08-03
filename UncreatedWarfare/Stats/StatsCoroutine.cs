using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Players;
using Uncreated.Warfare.Components;
using UnityEngine;

namespace Uncreated.Warfare.Stats
{
    internal static class StatsCoroutine
    {
        private static float lastAfkCheck = 0f;
        internal static Dictionary<ulong, Vector3> previousPositions = new Dictionary<ulong, Vector3>();
        public static IEnumerator<WaitForSeconds> StatsRoutine()
        {
            while (true)
            {
                try
                {
                    IEnumerator<SteamPlayer> players = Provider.clients.GetEnumerator();
                    while (players.MoveNext())
                    {
                        SteamPlayer player = players.Current;
                        if (F.TryGetPlaytimeComponent(player.player, out PlaytimeComponent c))
                        {
                            UncreatedPlayer stats = c.UCPlayerStats;
                            if (stats != null)
                            {
                                stats.warfare_stats.Update(player, false);
                                stats.UpdateSession(WarfareStats.WarfareName, false);
                                stats.SaveAsync();
                            }
                        }
                    }
                    players.Dispose();
                    if (Time.realtimeSinceStartup - lastAfkCheck < UCWarfare.Config.AfkCheckInterval)
                    {
                        lastAfkCheck = Time.realtimeSinceStartup;
                        for (int i = 0; i < Provider.clients.Count; i++)
                        {
                            Vector3 position = Provider.clients[i].player.transform.position;
                            if (previousPositions.TryGetValue(Provider.clients[i].playerID.steamID.m_SteamID, out Vector3 oldpos))
                            {
                                if (oldpos == position) // player hasnt moved
                                {
                                    Provider.kick(Provider.clients[i].playerID.steamID, "Auto-kick for being AFK.");
                                }
                                previousPositions[Provider.clients[i].playerID.steamID.m_SteamID] = position;
                            } else
                            {
                                previousPositions.Add(Provider.clients[i].playerID.steamID.m_SteamID, position);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    F.LogError("Error in Stats Coroutine:");
                    F.LogError(ex);
                }
                yield return new WaitForSeconds(UCWarfare.Config.StatsInterval);
            }
        }
    }
}

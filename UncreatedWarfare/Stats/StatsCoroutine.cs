using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Drawing;
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
        private static int counter;
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
                    bool check = counter % Mathf.RoundToInt(UCWarfare.Config.AfkCheckInterval / UCWarfare.Config.StatsInterval) == 0;
                    List<SteamPlayer> tokick = null;
                    if (check)
                    {
                        counter = 0;
                        tokick = new List<SteamPlayer>();
                    }
                    for (int i = 0; i < Provider.clients.Count; i++)
                    {
                        Vector3 position = Provider.clients[i].player.transform.position;
                        if (previousPositions.TryGetValue(Provider.clients[i].playerID.steamID.m_SteamID, out Vector3 oldpos))
                        {
                            if (oldpos == position && check) // player hasnt moved
                            {
                                if (check)
                                {
                                    FPlayerName names = F.GetPlayerOriginalNames(Provider.clients[i]);
                                    F.Log(F.Translate("kick_kicked_console_operator", 0, out _, names.PlayerName,
                                        Provider.clients[i].playerID.steamID.m_SteamID.ToString(Data.Locale), "AFK Auto-Kick"), ConsoleColor.Cyan);
                                    tokick.Add(Provider.clients[i]);
                                    previousPositions.Remove(Provider.clients[i].playerID.steamID.m_SteamID);
                                } else if (counter + 1 % Mathf.RoundToInt(UCWarfare.Config.AfkCheckInterval / UCWarfare.Config.StatsInterval) == 0) // one cycle left
                                {
                                    Provider.clients[i].SendChat("afk_warning", F.GetTimeFromSeconds((uint)Mathf.RoundToInt(UCWarfare.Config.AfkCheckInterval), Provider.clients[i].playerID.steamID.m_SteamID));
                                }
                            }
                            else
                            {
                                previousPositions[Provider.clients[i].playerID.steamID.m_SteamID] = position;
                            }
                        } else
                        {
                            previousPositions.Add(Provider.clients[i].playerID.steamID.m_SteamID, position);
                        }
                    }
                    if (check)
                        for (int i = 0; i < tokick.Count; i++)
                            Provider.kick(tokick[i].playerID.steamID, "Auto-kick for being AFK.");
                    counter++;
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

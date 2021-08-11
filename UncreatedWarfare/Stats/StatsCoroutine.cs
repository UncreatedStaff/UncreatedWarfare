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
        internal static Dictionary<ulong, Afk> previousPositions = new Dictionary<ulong, Afk>();
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
                                stats.Save();
                            }
                        }
                    }
                    players.Dispose();
                    int n = Afk.Clamp(counter);
                    if (n == 0)
                        counter = 0;
                    foreach (SteamPlayer player in Provider.clients.ToList())
                    {
                        Vector3 position = player.player.transform.position;
                        if (previousPositions.TryGetValue(player.playerID.steamID.m_SteamID, out Afk afk))
                        {
                            if (afk.lastLocation == position)
                            {
                                if (afk.time == n)
                                {
                                    FPlayerName names = F.GetPlayerOriginalNames(player);
                                    F.Log(F.Translate("kick_kicked_console_operator", 0, out _, names.PlayerName,
                                        player.playerID.steamID.m_SteamID.ToString(Data.Locale), "AFK Auto-Kick"), ConsoleColor.Cyan);
                                    Provider.kick(player.playerID.steamID, "Auto-kick for being AFK.");
                                    previousPositions.Remove(player.playerID.steamID.m_SteamID);
                                }
                                else if (afk.time == Afk.Clamp(n + 1)) // one cycle left
                                {
                                    player.SendChat("afk_warning", F.GetTimeFromSeconds((uint)Mathf.RoundToInt(UCWarfare.Config.StatsInterval), player.playerID.steamID.m_SteamID));
                                }
                            } else
                            {
                                afk.lastLocation = position;
                                afk.time = n;
                            }
                        } else
                        {
                            previousPositions.Add(player.playerID.steamID.m_SteamID, new Afk() { lastLocation = position, player = player.playerID.steamID.m_SteamID, time = n });
                        }
                    }
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
    class Afk
    {
        public int time;
        public ulong player;
        public Vector3 lastLocation;
        public static int Clamp(int input) => input % Mathf.RoundToInt(UCWarfare.Config.AfkCheckInterval / UCWarfare.Config.StatsInterval);
    }
}

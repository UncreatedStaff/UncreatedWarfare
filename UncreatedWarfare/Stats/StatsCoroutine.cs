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
        public static IEnumerator<WaitForSeconds> StatsRoutine()
        {
            while (true)
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
                yield return new WaitForSeconds(UCWarfare.Config.StatsInterval);
            }
        }
    }
}

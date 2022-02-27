using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Players;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Point;
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
                // TODO: optimize
#if DEBUG
                IDisposable profiler = ProfilingUtils.StartTracking();
#endif
                try
                {
                    int n = Afk.Clamp(counter);
                    if (n == 0)
                        counter = 0;
                    /* PLAYTIME COUNTER */
                    for (int i = Provider.clients.Count - 1; i >= 0; i--)
                    {
                        SteamPlayer pl = Provider.clients[i];
                        byte team = pl.GetTeamByte();
                        if (KitManager.HasKit(pl, out Kit kit))
                        {
                            StatsManager.ModifyStats(pl.playerID.steamID.m_SteamID, s =>
                            {
                                s.PlaytimeMinutes += (uint)UCWarfare.Config.StatsInterval;
                                WarfareStats.KitData kitData = s.Kits.Find(k => k.KitID == kit.Name && k.Team == team);
                                if (kitData == default)
                                {
                                    kitData = new WarfareStats.KitData() { KitID = kit.Name, Team = team, PlaytimeMinutes = (uint)UCWarfare.Config.StatsInterval };
                                    s.Kits.Add(kitData);
                                }
                                else
                                {
                                    kitData.PlaytimeMinutes += (uint)UCWarfare.Config.StatsInterval;
                                }
                            }, true);
                        }
                        else
                            StatsManager.ModifyStats(pl.playerID.steamID.m_SteamID, s => s.PlaytimeMinutes += (uint)UCWarfare.Config.StatsInterval);
                        /* ON DUTY AWARDER */
                        UCPlayer? player = UCPlayer.FromSteamPlayer(pl);
                        if (player != null && Points.XPConfig.OnDutyXP > 0 && player.OnDuty())
                        {
                            Points.AwardXP(player.Player, Points.XPConfig.OnDutyXP, Translation.Translate("xp_on_duty", player));
                        }


                        Vector3 position = pl.player.transform.position;
                        if (previousPositions.TryGetValue(pl.playerID.steamID.m_SteamID, out Afk afk))
                        {
                            if (afk.lastLocation == position)
                            {
                                if (afk.time == n)
                                {
                                    FPlayerName names = F.GetPlayerOriginalNames(pl);
                                    L.Log(Translation.Translate("kick_kicked_console_operator", 0, out _, names.PlayerName,
                                        pl.playerID.steamID.m_SteamID.ToString(Data.Locale), "AFK Auto-Kick"), ConsoleColor.Cyan);
                                    Provider.kick(pl.playerID.steamID, "Auto-kick for being AFK.");
                                    previousPositions.Remove(pl.playerID.steamID.m_SteamID);
                                }
                                else if (afk.time == Afk.Clamp(n + 1)) // one cycle left
                                {
                                    pl.SendChat("afk_warning", ((uint)UCWarfare.Config.StatsInterval).GetTimeFromMinutes(pl.playerID.steamID.m_SteamID));
                                }
                            }
                            else
                            {
                                afk.lastLocation = position;
                                afk.time = n;
                                previousPositions[pl.playerID.steamID.m_SteamID] = afk;
                            }
                        }
                        else
                        {
                            previousPositions.Add(pl.playerID.steamID.m_SteamID, new Afk() { lastLocation = position, player = pl.playerID.steamID.m_SteamID, time = n });
                        }
                    }
                    counter++;

                    /* CALCULATE AVERAGE PLAYERS AND SAVE */
                    StatsManager.ModifyTeam(1, t => t.AveragePlayers = (t.AveragePlayers * t.AveragePlayersCounter +
                    Provider.clients.Count(sp => sp.player.quests.groupID.m_SteamID == Teams.TeamManager.Team1ID)) / ++t.AveragePlayersCounter, false);
                    StatsManager.ModifyTeam(2, t => t.AveragePlayers = (t.AveragePlayers * t.AveragePlayersCounter +
                    Provider.clients.Count(sp => sp.player.quests.groupID.m_SteamID == Teams.TeamManager.Team2ID)) / ++t.AveragePlayersCounter, false);
                    StatsManager.SaveTeams();
                    /* TICK STAT BACKUP */
                    StatsManager.BackupTick();
                }
                catch (Exception ex)
                {
                    L.LogError("Error in Stats Coroutine:");
                    L.LogError(ex);
                }
#if DEBUG
                profiler.Dispose();
#endif
                // stats interval is in minutes here
                yield return new WaitForSeconds(UCWarfare.Config.StatsInterval * 60f);
            }
        }
    }
    /// <summary>Used to store data about where and how long a player has been afk.</summary>
    struct Afk
    {
        public int time;
        public ulong player;
        public Vector3 lastLocation;
        public static int Clamp(int input) => input % Mathf.RoundToInt(UCWarfare.Config.AfkCheckInterval / (UCWarfare.Config.StatsInterval * 60f));
    }
}

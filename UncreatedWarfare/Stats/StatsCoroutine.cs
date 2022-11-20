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
                        UCPlayer? ucplayer = UCPlayer.FromSteamPlayer(Provider.clients[i]);
                        if (ucplayer == null) continue;
                        byte team = ucplayer.Player.channel.owner.GetTeamByte();
                        if (KitManager.HasKit(ucplayer, out Kit kit))
                        {
                            StatsManager.ModifyStats(ucplayer.Steam64, s =>
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
                            StatsManager.ModifyStats(ucplayer.Steam64, s => s.PlaytimeMinutes += (uint)UCWarfare.Config.StatsInterval);
                        /* ON DUTY AWARDER */
                        bool isOnDuty = ucplayer.OnDuty();
                        if (Points.XPConfig.OnDutyXP > 0 && isOnDuty)
                            Points.AwardXP(ucplayer, Points.XPConfig.OnDutyXP, T.XPToastOnDuty);


                        Vector3 position = ucplayer.Position;
                        if (previousPositions.TryGetValue(ucplayer.Steam64, out Afk afk))
                        {
                            if (ucplayer.OffDuty() && afk.lastLocation == position)
                            {
                                if (afk.time == n)
                                {
                                    PlayerNames names = ucplayer.Name;
                                    L.Log($"{names.PlayerName} ({ucplayer.Steam64}) was auto-kicked for being AFK.", ConsoleColor.Cyan);
                                    Provider.kick(ucplayer.CSteamID, "Auto-kick for being AFK.");
                                    previousPositions.Remove(ucplayer.Steam64);
                                }
                                else if (afk.time == Afk.Clamp(n + 1)) // one cycle left
                                {
                                    ucplayer.SendChat(T.InactivityWarning, UCWarfare.Config.StatsInterval.GetTimeFromMinutes(ucplayer.Steam64));
                                }
                            }
                            else
                            {
                                afk.lastLocation = position;
                                afk.time = n;
                                previousPositions[ucplayer.Steam64] = afk;
                            }
                        }
                        else
                        {
                            previousPositions.Add(ucplayer.Steam64, new Afk() { lastLocation = position, player = ucplayer.Steam64, time = n });
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

                    long mem = GC.GetTotalMemory(false);
                    L.LogDebug("Memory usage: " + mem);

                    if (mem >= 1000000000 /* ~1GB */)
                    {
                        UCWarfare.ShutdownNow("Memory error, shutdown to prevent corrupted state.", 0);
                    }
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

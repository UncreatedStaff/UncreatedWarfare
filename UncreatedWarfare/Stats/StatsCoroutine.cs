using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Players;
using Uncreated.Warfare.Kits;
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
                    /* PLAYTIME COUNTER */
                    IEnumerator<SteamPlayer> players = Provider.clients.GetEnumerator();
                    while (players.MoveNext())
                    {
                        byte team = players.Current.GetTeamByte();
                        if (KitManager.HasKit(players.Current, out Kit kit))
                        {
                            StatsManager.ModifyStats(players.Current.playerID.steamID.m_SteamID, s =>
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
                            StatsManager.ModifyStats(players.Current.playerID.steamID.m_SteamID, s => s.PlaytimeMinutes += (uint)UCWarfare.Config.StatsInterval);
                        /* ON DUTY AWARDER */
                        UCPlayer player = UCPlayer.FromSteamPlayer(players.Current);
                        if (XP.XPManager.config.Data.OnDutyXP > 0 && player.OnDuty())
                        {
                            XP.XPManager.AddXP(player.Player, XP.XPManager.config.Data.OnDutyXP, F.Translate("xp_on_duty", player));
                        }
                    }
                    players.Dispose();


                    /* AFK KICKER */
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
                                    player.SendChat("afk_warning", F.GetTimeFromMinutes((uint)UCWarfare.Config.StatsInterval, player.playerID.steamID.m_SteamID));
                                }
                            }
                            else
                            {
                                afk.lastLocation = position;
                                afk.time = n;
                            }
                        }
                        else
                        {
                            previousPositions.Add(player.playerID.steamID.m_SteamID, new Afk() { lastLocation = position, player = player.playerID.steamID.m_SteamID, time = n });
                        }
                    }
                    counter++;

                    /* CALCULATE AVERAGE PLAYERS AND SAVE */
                    StatsManager.ModifyTeam(1, t => t.AveragePlayers = (t.AveragePlayers * t.AveragePlayersCounter +
                    Provider.clients.Count(sp => sp.player.quests.groupID.m_SteamID == Teams.TeamManager.Team1ID)) / ++t.AveragePlayersCounter, false);
                    StatsManager.ModifyTeam(2, t => t.AveragePlayers = (t.AveragePlayers * t.AveragePlayersCounter +
                    Provider.clients.Count(sp => sp.player.quests.groupID.m_SteamID == Teams.TeamManager.Team2ID)) / ++t.AveragePlayersCounter, false);
                    StatsManager.SaveTeams();
                }
                catch (Exception ex)
                {
                    F.LogError("Error in Stats Coroutine:");
                    F.LogError(ex);
                }

                // stats interval is in minutes here
                yield return new WaitForSeconds(UCWarfare.Config.StatsInterval * 60f);
            }
        }
    }
    /// <summary>Used to store data about where and how long a player has been afk.</summary>
    class Afk
    {
        public int time;
        public ulong player;
        public Vector3 lastLocation;
        public static int Clamp(int input) => input % Mathf.RoundToInt(UCWarfare.Config.AfkCheckInterval / (UCWarfare.Config.StatsInterval * 60f));
    }
}

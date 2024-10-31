using System.Collections.Generic;

namespace Uncreated.Warfare.Stats;

// todo cleanup
internal static class StatsCoroutine
{
    private static int _counter;
    private static readonly Dictionary<ulong, Afk> PreviousPositions = new Dictionary<ulong, Afk>();
    internal static void RemovePlayer(ulong player) => PreviousPositions.Remove(player);
#if false
    public static IEnumerator<WaitForSeconds> StatsRoutine()
    {
        while (true)
        {
            try
            {
                int n = Afk.Clamp(_counter);
                if (n == 0)
                    _counter = 0;
                /* PLAYTIME COUNTER */
                for (int i = Provider.clients.Count - 1; i >= 0; i--)
                {
                    UCPlayer? ucplayer = UCPlayer.FromSteamPlayer(Provider.clients[i]);
                    if (ucplayer == null) continue;

                    /* ON DUTY AWARDER */
                    if (ucplayer.OnDuty())
                        Points.AwardXP(ucplayer, XPReward.OnDuty);


                    Vector3 position = ucplayer.Position;
                    if (PreviousPositions.TryGetValue(ucplayer.Steam64, out Afk afk))
                    {
                        if (ucplayer.OffDuty() && afk.LastLocation == position)
                        {
                            if (afk.Time == n)
                            {
                                PlayerNames names = ucplayer.Name;
                                L.Log($"{names.PlayerName} ({ucplayer.Steam64}) was auto-kicked for being AFK.", ConsoleColor.Cyan);
                                Provider.kick(ucplayer.CSteamID, "Auto-kick for being AFK.");
                                PreviousPositions.Remove(ucplayer.Steam64);
                            }
                            else if (afk.Time == Afk.Clamp(n + 1)) // one cycle left
                            {
                                ucplayer.SendChat(T.InactivityWarning, Localization.GetTimeFromMinutes(UCWarfare.Config.StatsInterval, ucplayer.Locale.LanguageInfo, ucplayer.Locale.CultureInfo));
                            }
                        }
                        else
                        {
                            afk.LastLocation = position;
                            afk.Time = n;
                            PreviousPositions[ucplayer.Steam64] = afk;
                        }
                    }
                    else
                    {
                        PreviousPositions.Add(ucplayer.Steam64, new Afk() { LastLocation = position, Player = ucplayer.Steam64, Time = n });
                    }
                }
                _counter++;

                /* CALCULATE AVERAGE PLAYERS AND SAVE */
                // StatsManager.ModifyTeam(1, t => t.AveragePlayers = (t.AveragePlayers * t.AveragePlayersCounter +
                //                                                     Provider.clients.Count(sp => sp.player.quests.groupID.m_SteamID == Teams.TeamManager.Team1ID)) / ++t.AveragePlayersCounter, false);
                // StatsManager.ModifyTeam(2, t => t.AveragePlayers = (t.AveragePlayers * t.AveragePlayersCounter +
                //                                                     Provider.clients.Count(sp => sp.player.quests.groupID.m_SteamID == Teams.TeamManager.Team2ID)) / ++t.AveragePlayersCounter, false);

#if DEBUG
                long mem = GC.GetTotalMemory(false);
                L.LogDebug("Memory usage: " + mem);

                if (mem >= 10000000000 /* ~10GB */)
                {
                    UCWarfare.ShutdownNow("Memory error, shutdown to prevent corrupted state.", 0);
                }
#endif
            }
            catch (Exception ex)
            {
                L.LogError("Error in Stats Coroutine:");
                L.LogError(ex);
            }
            // stats interval is in minutes here
            yield return new WaitForSeconds(UCWarfare.Config.StatsInterval * 60f);
        }
    }
#endif
    /// <summary>Used to store data about where and how long a player has been afk.</summary>
    private struct Afk
    {
        public int Time;
        public ulong Player;
        public Vector3 LastLocation;
        public static int Clamp(int input) => 0;// todo input % Mathf.RoundToInt(UCWarfare.Config.AfkCheckInterval / (UCWarfare.Config.StatsInterval * 60f));
    }
}
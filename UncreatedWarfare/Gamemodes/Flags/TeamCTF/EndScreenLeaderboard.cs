using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Flags.TeamCTF
{
    public class EndScreenLeaderboard : MonoBehaviour
    {
        public const float SecondsEndGameLength = 30f;
        public const short UiIdentifier = 10000;
        readonly string[] headers = new string[] { "TopSquadHeader", "MostKillsHeader", "TimeOnPointHeader", "TimeDrivingHeader" };
        readonly string[] headerPrefixes = new string[] { "TS", "MK", "TP", "XP" };
        public event Networking.EmptyTaskDelegate OnLeaderboardExpired;
        private float secondsLeft;
        public bool ShuttingDown = false;
        public string ShuttingDownMessage = string.Empty;
        public ulong ShuttingDownPlayer = 0;
        const float updateTimeFrequency = 1f;
        private readonly Dictionary<ulong, EPluginWidgetFlags> oldFlags = new Dictionary<ulong, EPluginWidgetFlags>();
        public WarStatsTracker warstats;
        public ulong winner;
        public CancellationTokenSource CancelToken = new CancellationTokenSource();
        public async Task EndGame(string progresschars)
        {
            SendEndScreen(winner, progresschars);
            secondsLeft = SecondsEndGameLength;
            _ = StartUpdatingTimer(CancelToken.Token, progresschars).ConfigureAwait(false);
            await Task.Yield();
        }
        private async Task StartUpdatingTimer(CancellationToken token, string progresschars)
        {
            while (secondsLeft > 0 && !token.IsCancellationRequested)
            {
                secondsLeft -= updateTimeFrequency;
                await Task.Delay(Mathf.RoundToInt(updateTimeFrequency * 1000));
                UpdateLeaderboard(secondsLeft, progresschars);
            }
            SynchronizationContext rtn = await ThreadTool.SwitchToGameThread();
            EffectManager.ClearEffectByID_AllPlayers(UCWarfare.Config.EndScreenUI);
            foreach (SteamPlayer player in Provider.clients)
            {
                if (oldFlags.ContainsKey(player.playerID.steamID.m_SteamID))
                {
                    player.player.setAllPluginWidgetFlags(oldFlags[player.playerID.steamID.m_SteamID]);
                }
                player.player.movement.sendPluginSpeedMultiplier(1f);
                player.player.movement.sendPluginJumpMultiplier(1f);
            }
            if (ShuttingDown)
            {
                await Networking.Client.SendShuttingDown(ShuttingDownPlayer, ShuttingDownMessage);
                rtn = await ThreadTool.SwitchToGameThread();
                Provider.shutdown(0, ShuttingDownMessage);
                await rtn;
            }
            else if (OnLeaderboardExpired != null)
            {
                await OnLeaderboardExpired.Invoke();
            }
            await rtn;
        }
        public void SendScreenToPlayer(SteamPlayer player, string progresschars) => SendScreenToPlayer(winner, player, TeamManager.GetTeamHexColor(winner), progresschars);
        public void SendScreenToPlayer(ulong winner, SteamPlayer player, string teamcolor, string progresschars)
        {
            oldFlags.Add(player.playerID.steamID.m_SteamID, player.player.pluginWidgetFlags);
            player.player.setAllPluginWidgetFlags(EPluginWidgetFlags.None);
            player.player.movement.sendPluginSpeedMultiplier(0f);
            player.player.life.serverModifyHealth(100);
            player.player.life.serverModifyFood(100);
            player.player.life.serverModifyWater(100);
            player.player.life.serverModifyVirus(100);
            player.player.life.serverModifyStamina(100);
            player.player.movement.sendPluginJumpMultiplier(0f);
            player.player.teleportToLocation(F.GetBaseSpawn(player.player.channel.owner), winner == 1 ? 0f : (winner == 2 ? 85f : 0));
            // error is here
            KeyValuePair<ulong, PlayerCurrentGameStats> statsvalue = warstats.playerstats.FirstOrDefault(x => x.Key == player.playerID.steamID.m_SteamID);
            PlayerCurrentGameStats stats;
            if (statsvalue.Equals(default(KeyValuePair<ulong, PlayerCurrentGameStats>)))
                stats = new PlayerCurrentGameStats(player.player);
            else stats = statsvalue.Value;
            string originalName;
            if (Data.OriginalNames.ContainsKey(player.playerID.steamID.m_SteamID))
                originalName = Data.OriginalNames[player.playerID.steamID.m_SteamID].PlayerName;
            else originalName = player.playerID.playerName;
            ITransportConnection channel = player.transportConnection;
            EffectManager.sendUIEffect(UCWarfare.Config.EndScreenUI, UiIdentifier, channel, true);
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "Title1", F.Translate("game_over", player));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "TitleWinner", F.Translate("winner", player, TeamManager.TranslateName(winner, player), teamcolor));
            if (ShuttingDown)
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "NextGameStartsIn", F.Translate("next_game_start_label_shutting_down", player, ShuttingDownMessage));
            else
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, "NextGameStartsIn", F.Translate("next_game_start_label", player));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "NextGameSeconds", F.Translate("next_game_starting_format", player, TimeSpan.FromSeconds(SecondsEndGameLength)));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "NextGameCircleForeground", progresschars[CTFUI.FromMax(0, Mathf.RoundToInt(SecondsEndGameLength), progresschars)].ToString());
            List<KeyValuePair<Player, string>> topsquadplayers = warstats.GetTopSquad(out string squadname, out ulong squadteam);
            List<KeyValuePair<Player, int>> topkills = warstats.GetTop5MostKills();
            List<KeyValuePair<Player, TimeSpan>> toptimeonpoint = warstats.GetTop5OnPointTime();
            List<KeyValuePair<Player, int>> topxpgain = warstats.GetTop5XP();
            for (int h = 2; h <= 4; h++)
                if (headers.Length > h - 1)
                    EffectManager.sendUIEffectText(UiIdentifier, channel, true, headers[h - 1], F.Translate("lb_header_" + h.ToString(Data.Locale), player));
            if (squadteam != 0)
            {
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, headers[0], F.Translate("lb_header_1", player, squadname, TeamManager.GetTeamHexColor(squadteam))); // squad header
            } else
            {
                EffectManager.sendUIEffectText(UiIdentifier, channel, true, headers[0], F.Translate("lb_header_1_no_squad", player)); // squad header
            }
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "PlayerGameStatsHeader", F.Translate("player_name_header", player, originalName, F.GetTeamColorHex(player)));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "WarHeader", F.Translate("war_name_header", player,
                TeamManager.TranslateName(1, player.playerID.steamID.m_SteamID), TeamManager.Team1ColorHex,
                TeamManager.TranslateName(2, player.playerID.steamID.m_SteamID), TeamManager.Team2ColorHex));
            for (int i = 0; i < 6; i++)
            {
                if (i >= topsquadplayers.Count)
                {
                    EffectManager.sendUIEffectText(UiIdentifier, channel, true, headerPrefixes[0] + (i + 1).ToString(Data.Locale) + 'N', WarStatsTracker.NO_PLAYER_NAME_PLACEHOLDER);
                    EffectManager.sendUIEffectText(UiIdentifier, channel, true, headerPrefixes[1] + (i + 1).ToString(Data.Locale) + 'V', WarStatsTracker.NO_PLAYER_VALUE_PLACEHOLDER);
                }
                else if (topsquadplayers[i].Key == null)
                {
                    EffectManager.sendUIEffectText(UiIdentifier, channel, true, headerPrefixes[0] + (i + 1).ToString(Data.Locale) + 'N', WarStatsTracker.NO_PLAYER_NAME_PLACEHOLDER);
                    EffectManager.sendUIEffectText(UiIdentifier, channel, true, headerPrefixes[0] + (i + 1).ToString(Data.Locale) + 'V', WarStatsTracker.NO_PLAYER_VALUE_PLACEHOLDER);
                }
                else
                {
                    string color = topsquadplayers[i].Key.GetTeamColorHex();
                    EffectManager.sendUIEffectText(UiIdentifier, channel, true, headerPrefixes[0] + (i + 1).ToString(Data.Locale) + 'N', F.Translate("lb_player_name", player,
                        F.GetPlayerOriginalNames(topsquadplayers[i].Key).CharacterName, color));
                    EffectManager.sendUIEffectText(UiIdentifier, channel, true, headerPrefixes[0] + (i + 1).ToString(Data.Locale) + 'V', F.Translate("lb_float_player_value", player,
                        topsquadplayers[i].Value, color));
                }
            }
            for (int i = 0; i < 6; i++)
            {
                if (i >= topkills.Count)
                {
                    EffectManager.sendUIEffectText(UiIdentifier, channel, true, headerPrefixes[1] + (i + 1).ToString(Data.Locale) + 'N', WarStatsTracker.NO_PLAYER_NAME_PLACEHOLDER);
                    EffectManager.sendUIEffectText(UiIdentifier, channel, true, headerPrefixes[1] + (i + 1).ToString(Data.Locale) + 'V', WarStatsTracker.NO_PLAYER_VALUE_PLACEHOLDER);
                }
                else if (topkills[i].Key == null)
                {
                    EffectManager.sendUIEffectText(UiIdentifier, channel, true, headerPrefixes[1] + (i + 1).ToString(Data.Locale) + 'N', WarStatsTracker.NO_PLAYER_NAME_PLACEHOLDER);
                    EffectManager.sendUIEffectText(UiIdentifier, channel, true, headerPrefixes[1] + (i + 1).ToString(Data.Locale) + 'V', WarStatsTracker.NO_PLAYER_VALUE_PLACEHOLDER);
                }
                else
                {
                    string color = F.GetTeamColorHex(topkills[i].Key);
                    EffectManager.sendUIEffectText(UiIdentifier, channel, true, headerPrefixes[1] + (i + 1).ToString(Data.Locale) + 'N', F.Translate("lb_player_name", player,
                        F.GetPlayerOriginalNames(topkills[i].Key).CharacterName, color));
                    EffectManager.sendUIEffectText(UiIdentifier, channel, true, headerPrefixes[1] + (i + 1).ToString(Data.Locale) + 'V', F.Translate("lb_player_value", player,
                        topkills[i].Value, color));
                }
            }
            for (int i = 0; i < 6; i++)
            {
                if (i >= toptimeonpoint.Count)
                {
                    EffectManager.sendUIEffectText(UiIdentifier, channel, true, headerPrefixes[2] + (i + 1).ToString(Data.Locale) + 'N', WarStatsTracker.NO_PLAYER_NAME_PLACEHOLDER);
                    EffectManager.sendUIEffectText(UiIdentifier, channel, true, headerPrefixes[2] + (i + 1).ToString(Data.Locale) + 'V', WarStatsTracker.NO_PLAYER_VALUE_PLACEHOLDER);
                }
                else if (toptimeonpoint[i].Key == null)
                {
                    EffectManager.sendUIEffectText(UiIdentifier, channel, true, headerPrefixes[2] + (i + 1).ToString(Data.Locale) + 'N', WarStatsTracker.NO_PLAYER_NAME_PLACEHOLDER);
                    EffectManager.sendUIEffectText(UiIdentifier, channel, true, headerPrefixes[2] + (i + 1).ToString(Data.Locale) + 'V', WarStatsTracker.NO_PLAYER_VALUE_PLACEHOLDER);
                }
                else
                {
                    string color = F.GetTeamColorHex(toptimeonpoint[i].Key);
                    EffectManager.sendUIEffectText(UiIdentifier, channel, true, headerPrefixes[2] + (i + 1).ToString(Data.Locale) + 'N', F.Translate("lb_player_name", player,
                        F.GetPlayerOriginalNames(toptimeonpoint[i].Key).CharacterName, color));
                    EffectManager.sendUIEffectText(UiIdentifier, channel, true, headerPrefixes[2] + (i + 1).ToString(Data.Locale) + 'V', F.Translate("lb_time_player_value", player,
                        toptimeonpoint[i].Value, color));
                }
            }
            for (int i = 0; i < 6; i++)
            {
                if (i >= topxpgain.Count)
                {
                    EffectManager.sendUIEffectText(UiIdentifier, channel, true, headerPrefixes[3] + (i + 1).ToString(Data.Locale) + 'N', WarStatsTracker.NO_PLAYER_NAME_PLACEHOLDER);
                    EffectManager.sendUIEffectText(UiIdentifier, channel, true, headerPrefixes[3] + (i + 1).ToString(Data.Locale) + 'V', WarStatsTracker.NO_PLAYER_VALUE_PLACEHOLDER);
                }
                else if (topxpgain[i].Key == null)
                {
                    EffectManager.sendUIEffectText(UiIdentifier, channel, true, headerPrefixes[3] + (i + 1).ToString(Data.Locale) + 'N', WarStatsTracker.NO_PLAYER_NAME_PLACEHOLDER);
                    EffectManager.sendUIEffectText(UiIdentifier, channel, true, headerPrefixes[3] + (i + 1).ToString(Data.Locale) + 'V', WarStatsTracker.NO_PLAYER_VALUE_PLACEHOLDER);
                }
                else
                {
                    string color = F.GetTeamColorHex(topxpgain[i].Key);
                    EffectManager.sendUIEffectText(UiIdentifier, channel, true, headerPrefixes[3] + (i + 1).ToString(Data.Locale) + 'N', F.Translate("lb_player_name", player,
                        F.GetPlayerOriginalNames(topxpgain[i].Key).CharacterName, color));
                    EffectManager.sendUIEffectText(UiIdentifier, channel, true, headerPrefixes[3] + (i + 1).ToString(Data.Locale) + 'V', F.Translate("lb_player_value", player,
                        topxpgain[i].Value, color));
                }
            }
            /*
             *  STATS
             */
            // titles
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblKills", F.Translate("lblKills", player));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblDeaths", F.Translate("lblDeaths", player));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblKDR", F.Translate("lblKDR", player));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblKillsOnPoint", F.Translate("lblKillsOnPoint", player));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblTimeDeployed", F.Translate("lblTimeDeployed", player));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblXpGained", F.Translate("lblXpGained", player));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblTimeOnPoint", F.Translate("lblTimeOnPoint", player));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblCaptures", F.Translate("lblCaptures", player));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblTimeInVehicle", F.Translate("lblTimeInVehicle", player));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblTeamkills", F.Translate("lblTeamkills", player));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblEnemyFOBsDestroyed", F.Translate("lblFOBsDestroyed", player));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblOfficerPointsGainedValue", F.Translate("lblOfficerPointsGained", player));

            string defaultColor = UCWarfare.GetColorHex("default");
            // values
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "KillsValue", F.Translate("stats_player_value", player, stats.kills, defaultColor));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "DeathsValue", F.Translate("stats_player_value", player, stats.deaths, defaultColor));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "KDRValue", F.Translate("stats_player_float_value", player, stats.KDR, defaultColor));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "KillsOnPointValue", F.Translate("stats_player_value", player, stats.killsonpoint, defaultColor));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "TimeDeployedValue", F.Translate("stats_player_time_value", player, stats.TimeDeployed, defaultColor));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "XPGainedValue", F.Translate("stats_player_value", player, stats.xpgained, defaultColor));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "TimeOnPointValue", F.Translate("stats_player_time_value", player, stats.TimeOnPoint, defaultColor));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "CapturesValue", F.Translate("stats_player_value", player, stats.captures, defaultColor));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "TimeInVehicleValue", F.Translate("stats_player_time_value", player, stats.TimeDriving, defaultColor));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "TeamkillsValue", F.Translate("stats_player_value", player, stats.teamkills, defaultColor));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "EnemyFOBsDestroyedValue", F.Translate("stats_player_value", player, stats.fobsdestroyed, defaultColor));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "OfficerPointsGainedValue", F.Translate("stats_player_value", player, stats.officerpointsgained, defaultColor));

            /*
             *  WAR
             */
            // titles
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblDuration", F.Translate("lblDuration", player));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblCasualtiesT1", F.Translate("lblDeathsT1", player));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblCasualtiesT2", F.Translate("lblDeathsT2", player));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblOwnerChangedCount", F.Translate("lblOwnerChangeCount", player));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblAveragePlayerCountT1", F.Translate("lblAveragePlayerCountT1", player));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblAveragePlayerCountT2", F.Translate("lblAveragePlayerCountT2", player));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblFOBsPlacedT1", F.Translate("lblFOBsPlacedT1", player));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblFOBsPlacedT2", F.Translate("lblFOBsPlacedT2", player));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblFOBsDestroyedT1", F.Translate("lblFOBsDestroyedT1", player));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblFOBsDestroyedT2", F.Translate("lblFOBsDestroyedT2", player));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblTeamkillingCasualties", F.Translate("lblTeamkillingCasualties", player));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "lblTopRankingOfficer", F.Translate("lblTopRankingOfficer", player));

            // values
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "DurationValue", F.Translate("stats_war_time_value", player, warstats.Duration, defaultColor));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "CasualtiesValueT1", F.Translate("stats_war_value", player, warstats.casualtiesT1, defaultColor));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "CasualtiesValueT2", F.Translate("stats_war_value", player, warstats.casualtiesT2, defaultColor));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "FlagCapturesValue", F.Translate("stats_war_value", player, warstats.totalFlagOwnerChanges, defaultColor));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "AveragePlayerCountsT1Value", F.Translate("stats_war_float_value", player, warstats.averageArmySizeT1, defaultColor));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "AveragePlayerCountsT2Value", F.Translate("stats_war_float_value", player, warstats.averageArmySizeT2, defaultColor));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "FOBsPlacedT1Value", F.Translate("stats_war_value", player, warstats.fobsPlacedT1, defaultColor));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "FOBsPlacedT2Value", F.Translate("stats_war_value", player, warstats.fobsPlacedT2, defaultColor));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "FOBsDestroyedT1Value", F.Translate("stats_war_value", player, warstats.fobsDestroyedT1, defaultColor));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "FOBsDestroyedT2Value", F.Translate("stats_war_value", player, warstats.fobsDestroyedT2, defaultColor));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "TeamkillingCasualtiesValue", F.Translate("stats_war_value", player, warstats.teamkills, defaultColor));
            EffectManager.sendUIEffectText(UiIdentifier, channel, true, "TopRankingOfficerValue", " TODO "); // do this after eating grilled cheese
        }
        public void SendEndScreen(ulong winner, string progresschars)
        {
            string teamcolor = TeamManager.GetTeamHexColor(winner);
            for (int players = 0; players < Provider.clients.Count; players++)
            {
                SendScreenToPlayer(winner, Provider.clients[players], teamcolor, progresschars);
            }
        }
        public void UpdateLeaderboard(float newTime, string progresschars)
        {
            foreach(SteamPlayer player in Provider.clients)
            {
                EffectManager.sendUIEffectText(UiIdentifier, player.transportConnection, true, "NextGameSeconds", F.Translate("next_game_starting_format", player, TimeSpan.FromSeconds(newTime)));
                int time = Mathf.RoundToInt(SecondsEndGameLength);
                EffectManager.sendUIEffectText(UiIdentifier, player.transportConnection, true, "NextGameCircleForeground",
                    progresschars[CTFUI.FromMax(Mathf.RoundToInt(time - newTime), time, progresschars)].ToString());
            }
        }
    }
    public class PlayerCurrentGameStats
    {
        public Player player;
        public ulong id;
        public int kills;
        public int deaths;
        public float KDR { get => deaths == 0 ? kills : kills / deaths; }
        public int killsonpoint;
        public int xpgained;
        public int officerpointsgained;
        public TimeSpan TimeDeployed { get => TimeSpan.FromSeconds(timeDeployedCounter); }
        private float timeDeployedCounter;
        public TimeSpan TimeOnPoint { get => TimeSpan.FromSeconds(timeOnPointCounter); }
        private float timeOnPointCounter;
        public int captures;
        public int teamkills;
        public int fobsdestroyed;
        public TimeSpan TimeDriving { get => TimeSpan.FromSeconds(timeDrivingCounter); }
        private float timeDrivingCounter;
        public PlayerCurrentGameStats(Player player)
        {
            this.player = player;
            this.id = player.channel.owner.playerID.steamID.m_SteamID;
            Reset();
        }
        public void Reset()
        {
            this.kills = 0;
            this.deaths = 0;
            this.killsonpoint = 0;
            this.timeDeployedCounter = 0;
            this.timeOnPointCounter = 0;
            this.captures = 0;
            this.teamkills = 0;
            this.fobsdestroyed = 0;
            this.timeDrivingCounter = 0;
            this.xpgained = 0;
            this.officerpointsgained = 0;
        }
        public void AddKill()
        {
            kills++;
            if (player != default && Data.Gamemode is FlagGamemode fg && fg.Rotation.Exists(x => x.ZoneData.IsInside(player.transform.position))) killsonpoint++;
        }
        public void AddDeath() => deaths++;
        public void AddTeamkill() => teamkills++;
        public void AddXP(int amount) => xpgained += amount;
        public void AddOfficerPoints(int amount) => officerpointsgained += amount;
        public void AddToTimeDeployed(float amount) => timeDeployedCounter += amount;
        public void AddToTimeOnPoint(float amount) => timeOnPointCounter += amount;
        public void AddToTimeDriving(float amount) => timeDrivingCounter += amount;
    }
    public class WarStatsTracker : MonoBehaviour
    {
        public TimeSpan Duration { get => TimeSpan.FromSeconds(durationCounter); }
        public Dictionary<ulong, PlayerCurrentGameStats> playerstats;
        private float durationCounter = 0;
        public int casualtiesT1;
        public int casualtiesT2;
        public int totalFlagOwnerChanges;
        public float averageArmySizeT1;
        public float averageArmySizeT2;
        private int averageArmySizeT1counter = 0;
        private int averageArmySizeT2counter = 0;
        public int fobsPlacedT1;
        public int fobsPlacedT2;
        public int fobsDestroyedT1;
        public int fobsDestroyedT2;
        public int teamkills;
        public void Update()
        {
            durationCounter += Time.deltaTime;
        }
        public void AddPlayer(Player player)
        {
            if(!playerstats.ContainsKey(player.channel.owner.playerID.steamID.m_SteamID))
                playerstats.Add(player.channel.owner.playerID.steamID.m_SteamID, new PlayerCurrentGameStats(player));
        }
        public void Start() => Reset();
        public void Reset()
        {
            if (playerstats == null)
                playerstats = new Dictionary<ulong, PlayerCurrentGameStats>();
            else
                playerstats.Clear();
            durationCounter = 0;
            casualtiesT1 = 0;
            casualtiesT2 = 0;
            totalFlagOwnerChanges = 0;
            averageArmySizeT1 = 0;
            averageArmySizeT2 = 0;
            averageArmySizeT1counter = 0;
            averageArmySizeT2counter = 0;
            fobsPlacedT1 = 0;
            fobsPlacedT2 = 0;
            fobsDestroyedT1 = 0;
            fobsDestroyedT2 = 0;
            teamkills = 0;
            StartCoroutine(CompileAverages());
        }
        public void StopCounting()
        {
            StopAllCoroutines();
        }
        private void CompileArmyAverageT1(int newcount)
        {
            float oldArmySize = averageArmySizeT1 * averageArmySizeT1counter;
            averageArmySizeT1counter++;
            averageArmySizeT1 = (oldArmySize + newcount) / averageArmySizeT1counter;
        }
        private void CompileArmyAverageT2(int newcount)
        {
            float oldArmySize = averageArmySizeT2 * averageArmySizeT2counter;
            averageArmySizeT2counter++;
            averageArmySizeT2 = (oldArmySize + newcount) / averageArmySizeT2counter;
        }
        private IEnumerator<WaitForSeconds> CompileAverages()
        {
            // checks for how many players are outside of main
            DateTime dt = DateTime.Now;
            CompileArmyAverageT1(Provider.clients.Count(x => x.GetTeam() == 1 && !TeamManager.Team1Main.IsInside(x.player.transform.position)));
            CompileArmyAverageT2(Provider.clients.Count(x => x.GetTeam() == 2 && !TeamManager.Team2Main.IsInside(x.player.transform.position)));
            yield return new WaitForSeconds(10f);
            StartCoroutine(CompileAverages());
        }
        public bool TryGetPlayer(ulong id, out PlayerCurrentGameStats stats)
        {
            if (!(playerstats == null) && playerstats.ContainsKey(id))
            {
                stats = playerstats[id];
                return true;
            }
            else
            {
                stats = null;
                return false;
            }
        }
        public const string NO_PLAYER_NAME_PLACEHOLDER = "---";
        public const string NO_PLAYER_VALUE_PLACEHOLDER = "--";
        public List<KeyValuePair<Player, int>> GetTop5MostKills()
        {
            List<PlayerCurrentGameStats> stats = playerstats.Values.ToList();
            stats.Sort((PlayerCurrentGameStats a, PlayerCurrentGameStats b) => 
            {
                if (a.player == default) // if the player left midgame the player will be default, this checks if they rejoined, if not they are sent to the bottom.
                {
                    SteamPlayer player = PlayerTool.getSteamPlayer(a.id);
                    if (player == default || player.player == default) return -1;
                    else a.player = player.player;
                }
                return a.kills.CompareTo(b.kills);
            });
            List<KeyValuePair<Player, int>> rtnList = new List<KeyValuePair<Player, int>>();
            for(int i = 0; i < Math.Min(stats.Count, 6); i++)
                rtnList.Add(new KeyValuePair<Player, int>(stats[i].player, stats[i].kills));
            return rtnList;
        }
        public List<KeyValuePair<Player, string>> GetTopSquad(out string squadname, out ulong squadteam)
        {
            List<Squad> squads = SquadManager.Squads.ToList();
            if (squads.Count == 0)
            {
                squadname = NO_PLAYER_NAME_PLACEHOLDER;
                squadteam = 0;
                return new List<KeyValuePair<Player, string>>();
            }
            squads.Sort((a, b) =>
            {
                int totalxpgaina = 0;
                for (int i = 0; i < a.Members.Count; i++)
                {
                    if (a.Members[i].Player.TryGetPlaytimeComponent(out Components.PlaytimeComponent c))
                        totalxpgaina += c.stats.xpgained;
                }
                int totalxpgainb = 0;
                for (int i = 0; i < b.Members.Count; i++)
                {
                    if (b.Members[i].Player.TryGetPlaytimeComponent(out Components.PlaytimeComponent c))
                        totalxpgainb += c.stats.xpgained;
                }
                if (totalxpgaina == totalxpgainb)
                {
                    int totalopgaina = 0;
                    for (int i = 0; i < a.Members.Count; i++)
                    {
                        if (a.Members[i].Player.TryGetPlaytimeComponent(out Components.PlaytimeComponent c))
                            totalopgaina += c.stats.officerpointsgained;
                    }
                    int totalopgainb = 0;
                    for (int i = 0; i < b.Members.Count; i++)
                    {
                        if (b.Members[i].Player.TryGetPlaytimeComponent(out Components.PlaytimeComponent c))
                            totalopgainb += c.stats.officerpointsgained;
                    }
                    if (totalxpgaina == totalxpgainb)
                    {
                        int totalkillsa = 0;
                        for (int i = 0; i < a.Members.Count; i++)
                        {
                            if (a.Members[i].Player.TryGetPlaytimeComponent(out Components.PlaytimeComponent c))
                                totalkillsa += c.stats.kills;
                        }
                        int totalkillsb = 0;
                        for (int i = 0; i < b.Members.Count; i++)
                        {
                            if (b.Members[i].Player.TryGetPlaytimeComponent(out Components.PlaytimeComponent c))
                                totalkillsb += c.stats.kills;
                        }
                        return totalkillsa.CompareTo(totalkillsb);
                    }
                    return totalopgaina.CompareTo(totalopgainb);
                }
                return totalxpgaina.CompareTo(totalxpgainb);
            });
            Squad topsquad = squads[0];
            squadname = topsquad.Name;
            squadteam = topsquad.Team;
            List<UCPlayer> players = topsquad.Members.ToList();
            players.Sort((a, b) =>
            {
                if (topsquad.Leader.Steam64 == a.Steam64) return 1;
                else
                {
                    int axp = 0, bxp = 0;
                    if (a.Player.TryGetPlaytimeComponent(out Components.PlaytimeComponent ca))
                        axp = ca.stats.xpgained;
                    if (b.Player.TryGetPlaytimeComponent(out Components.PlaytimeComponent cb))
                        bxp = cb.stats.xpgained;
                    return axp.CompareTo(bxp);
                }
            });
            List<KeyValuePair<Player, string>> rtn = new List<KeyValuePair<Player, string>>(players.Count > 6 ? 6 : players.Count);
            for (int i = 0; i < (players.Count > 6 ? 6 : players.Count); i++)
            {
                rtn.Add(new KeyValuePair<Player, string>(players[i].Player, players[i].Icon));
            }
            return rtn;
        }
        public List<KeyValuePair<Player, TimeSpan>> GetTop5OnPointTime()
        {
            List<PlayerCurrentGameStats> stats = playerstats.Values.ToList();
            stats.Sort((PlayerCurrentGameStats a, PlayerCurrentGameStats b) =>
            {
                if (a.player == default)
                {
                    SteamPlayer player = PlayerTool.getSteamPlayer(a.id);
                    if (player == default || player.player == default) return -1;
                    else a.player = player.player;
                }
                return a.KDR.CompareTo(b.KDR);
            });
            List<KeyValuePair<Player, TimeSpan>> rtnList = new List<KeyValuePair<Player, TimeSpan>>();
            for (int i = 0; i < Math.Min(stats.Count, 6); i++)
                rtnList.Add(new KeyValuePair<Player, TimeSpan>(stats[i].player, stats[i].TimeOnPoint));
            return rtnList;
        }
        public List<KeyValuePair<Player, int>> GetTop5XP()
        {
            List<PlayerCurrentGameStats> stats = playerstats.Values.ToList();
            stats.Sort((PlayerCurrentGameStats a, PlayerCurrentGameStats b) =>
            {
                if (a.player == default)
                {
                    SteamPlayer player = PlayerTool.getSteamPlayer(a.id);
                    if (player == default || player.player == default) return -1;
                    else a.player = player.player;
                }
                return a.xpgained.CompareTo(b.xpgained);
            });
            List<KeyValuePair<Player, int>> rtnList = new List<KeyValuePair<Player, int>>();
            for (int i = 0; i < Math.Min(stats.Count, 6); i++)
                rtnList.Add(new KeyValuePair<Player, int>(stats[i].player, stats[i].xpgained));
            return rtnList;
        }
    }
}

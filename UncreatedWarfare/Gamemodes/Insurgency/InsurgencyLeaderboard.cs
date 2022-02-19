using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Players;
using Uncreated.Warfare.Gamemodes.Flags;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Insurgency
{
    public class InsurgencyLeaderboard : Leaderboard<InsurgencyPlayerStats, InsurgencyTracker>
    {
        protected override Guid GUID => Gamemode.Config.UI.CTFLeaderboardGUID;
        private List<InsurgencyPlayerStats> statsT1;
        private List<InsurgencyPlayerStats> statsT2;
        private bool longestShotTaken = false;
        private ulong longestShotTakerTeam = 0;
        private float longestShotDistance = 0;
        private FPlayerName longestShotTaker;
        internal EffectAsset asset;
        private string longestShotWeapon = string.Empty;

        public override void Calculate()
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            tracker.GetTopStats(14, out statsT1, out statsT2);

            longestShotTaken = tracker.LongestShot.Player != 0;
            if (longestShotTaken)
            {
                SteamPlayer longestshottaker = PlayerTool.getSteamPlayer(tracker.LongestShot.Player);
                if (longestshottaker == null)
                {
                    longestShotTaker = Data.DatabaseManager.GetUsernames(tracker.LongestShot.Player);
                    longestShotTakerTeam = tracker.LongestShot.Team;
                }
                else
                {
                    longestShotTaker = F.GetPlayerOriginalNames(longestshottaker);
                    longestShotTakerTeam = tracker.LongestShot.Team;
                }
                longestShotDistance = tracker.LongestShot.Distance;
                Asset a = Assets.find(tracker.LongestShot.Gun);
                if (a is ItemAsset asset)
                    longestShotWeapon = asset.itemName;
                else if (a is VehicleAsset vasset)
                    longestShotWeapon = vasset.vehicleName;
                else longestShotWeapon = string.Empty;
            }
            else
            {
                longestShotDistance = 0;
                longestShotTaker = FPlayerName.Nil;
                longestShotTakerTeam = 0;
                longestShotWeapon = string.Empty;
            }
        }
        public override void SendLeaderboard()
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            if (!Data.Is(out Insurgency gm)) return;
            if (Assets.find(GUID) is not EffectAsset asset) return;
            this.asset = asset;
            string teamcolor = TeamManager.GetTeamHexColor(_winner);
            states = new bool[2][] { new bool[Math.Min(14, statsT1.Count - 1)], new bool[Math.Min(14, statsT2.Count - 1)] };
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                SendLeaderboard(PlayerManager.OnlinePlayers[i], teamcolor, gm);
            }
        }
        public virtual void SendLeaderboard(UCPlayer player, string teamcolor, Insurgency gm)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            try
            {
                ulong team = player.GetTeam();
                ITransportConnection channel = player.Player.channel.owner.transportConnection;
                player.Player.movement.sendPluginSpeedMultiplier(0f);
                player.Player.life.sendRevive();
                player.Player.movement.sendPluginJumpMultiplier(0f);

                gm.ReviveManager.RevivePlayer(player.Player);

                string language = Translation.DecideLanguage(player.Steam64, Data.Localization);
                if (!player.Player.life.isDead)
                    player.Player.teleportToLocationUnsafe(team.GetBaseSpawnFromTeam(), team.GetBaseAngle());
                else
                    player.Player.life.ReceiveRespawnRequest(false);

                // resupply the kit.
                if (string.IsNullOrEmpty(player.KitName))
                {
                    if (KitManager.KitExists(player.KitName, out Kit kit))
                        KitManager.ResupplyKit(player, kit);
                }
                player.Player.setAllPluginWidgetFlags(EPluginWidgetFlags.None);
                CTFUI.ClearFlagList(channel);
                tracker.stats.TryGetValue(player.Steam64, out InsurgencyPlayerStats stats);
                if (stats == null) stats = new InsurgencyPlayerStats(player.Player);
                FPlayerName originalNames = F.GetPlayerOriginalNames(player);
                EffectManager.sendUIEffect(this.asset.id, LeaderboardEx.leaderboardKey, channel, true);
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "TitleWinner", Translation.Translate("winner", language, TeamManager.TranslateName(_winner, player.Player), teamcolor));
                if (shuttingDown)
                    EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "NextGameStartsIn", Translation.Translate("next_game_start_label_shutting_down", language, shuttingDownMessage));
                else
                    EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "NextGameStartsIn", Translation.Translate("next_game_start_label", language));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "NextGameSeconds", Translation.ObjectTranslate("next_game_starting_format", language, TimeSpan.FromSeconds(Gamemode.Config.GeneralConfig.LeaderboardTime)));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "NextGameCircleForeground", Gamemode.Config.UI.ProgressChars[0].ToString());

                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "PlayerGameStatsHeader", Translation.ObjectTranslate("player_name_header", language,
                    originalNames.CharacterName, TeamManager.GetTeamHexColor(player.GetTeam()), (float)(stats.onlineCount1 + stats.onlineCount2) / tracker.coroutinect * 100f));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "WarHeader", Translation.Translate("war_name_header", language,
                    TeamManager.TranslateName(1, player.Steam64), TeamManager.Team1ColorHex,
                    TeamManager.TranslateName(2, player.Steam64), TeamManager.Team2ColorHex));
                /*
                 * LEADERBOARD
                 */
                for (int i = 0; i < Math.Min(15, statsT1.Count); i++)
                {
                    string n = i == 0 ? TeamManager.TranslateShortName(1, player.Steam64, true).ToUpper() : statsT1[i].Player.channel.owner.playerID.nickName;
                    string k = statsT1[i].kills.ToString(Data.Locale);
                    string d = statsT1[i].deaths.ToString(Data.Locale);
                    string x = statsT1[i].XPGained.ToString(Data.Locale);
                    string f = statsT1[i].OFPGained.ToString(Data.Locale);
                    string c = statsT1[i].KDR.ToString("N2", Data.Locale);
                    string t = statsT1[i].DamageDone.ToString(Data.Locale);

                    if (statsT1[i].Player != null && player.Steam64 == statsT1[i].Steam64)
                    {
                        n = n.Colorize("dbffdc");
                        k = k.Colorize("dbffdc");
                        d = d.Colorize("dbffdc");
                        x = x.Colorize("dbffdc");
                        f = f.Colorize("dbffdc");
                        c = c.Colorize("dbffdc");
                        t = t.Colorize("dbffdc");
                    }

                    EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "1N" + i, n);
                    EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "1K" + i, k);
                    EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "1D" + i, d);
                    EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "1X" + i, x);
                    EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "1F" + i, f);
                    EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "1C" + i, c);
                    EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "1T" + i, t);
                }
                for (int i = 0; i < Math.Min(15, statsT2.Count); i++)
                {
                    string n = i == 0 ? TeamManager.TranslateShortName(2, player.Steam64, true).ToUpper() : statsT2[i].Player.channel.owner.playerID.nickName;
                    string k = statsT2[i].kills.ToString(Data.Locale);
                    string d = statsT2[i].deaths.ToString(Data.Locale);
                    string x = statsT2[i].XPGained.ToString(Data.Locale);
                    string f = statsT2[i].OFPGained.ToString(Data.Locale);
                    string c = statsT2[i].KDR.ToString("N2", Data.Locale);
                    string t = statsT2[i].DamageDone.ToString(Data.Locale);

                    if (statsT2[i].Player != null && player.Steam64 == statsT2[i].Steam64)
                    {
                        n = n.Colorize("dbffdc");
                        k = k.Colorize("dbffdc");
                        d = d.Colorize("dbffdc");
                        x = x.Colorize("dbffdc");
                        f = f.Colorize("dbffdc");
                        c = c.Colorize("dbffdc");
                        t = t.Colorize("dbffdc");
                    }
                    EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "2N" + i, n);
                    EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "2K" + i, k);
                    EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "2D" + i, d);
                    EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "2X" + i, x);
                    EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "2F" + i, f);
                    EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "2C" + i, c);
                    EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "2T" + i, t);
                }

                //UCPlayer topOfficer = PlayerManager.OnlinePlayers.OrderByDescending(x => x.cachedOfp).FirstOrDefault();
                //if (topOfficer.cachedOfp == 0) topOfficer = default;
                /*
                 *  STATS
                 */
                // titles
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblKills", Translation.Translate("lblKills", language));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblDeaths", Translation.Translate("lblDeaths", language));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblDamageDone", Translation.Translate("lblDamageDone", language));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblObjectiveKills", Translation.Translate("lblObjectiveKills", language));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblTimeDeployed", Translation.Translate("lblTimeDeployed", language));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblXpGained", Translation.Translate("lblXpGained", language));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblIntelligenceGathered", Translation.Translate("lblIntelligenceGathered", language));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblCachesDiscovered", Translation.Translate("lblCachesDiscovered", language));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblCachesDestroyed", Translation.Translate("lblCachesDestroyed", language));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblTeamkills", Translation.Translate("lblTeamkills", language));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblEnemyFOBsDestroyed", Translation.Translate("lblFOBsDestroyed", language));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblOfficerPointsGained", Translation.Translate("lblOfficerPointsGained", language));

                string defaultColor = UCWarfare.GetColorHex("default");
                // values
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "KillsValue", Translation.ObjectTranslate("stats_player_value", language, stats.kills, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "DeathsValue", Translation.ObjectTranslate("stats_player_value", language, stats.deaths, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "DamageDoneValue", Translation.ObjectTranslate("stats_player_value", language, stats.DamageDone, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "ObjectiveKillsValue", Translation.ObjectTranslate("stats_player_value", language, team == gm.AttackingTeam ? stats.KillsAttack : stats.KillsDefense, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "TimeDeployedValue", Translation.ObjectTranslate("stats_player_time_value", language, TimeSpan.FromSeconds(stats.timedeployed), defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "XPGainedValue", Translation.ObjectTranslate("stats_player_value", language, stats.XPGained, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "IntelligenceGatheredValue", Translation.ObjectTranslate("stats_player_time_value", language, TimeSpan.FromSeconds(stats.timeonpoint), defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "CachesDiscoveredValue", Translation.ObjectTranslate("stats_player_value", language, stats._cachesDiscovered, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "CachesDestroyedValue", Translation.ObjectTranslate("stats_player_value", language, stats._cachesDestroyed, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "TeamkillsValue", Translation.ObjectTranslate("stats_player_value", language, stats.teamkills, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "EnemyFOBsDestroyedValue", Translation.ObjectTranslate("stats_player_value", language, stats.FOBsDestroyed, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "OfficerPointsGainedValue", Translation.ObjectTranslate("stats_player_value", language, stats.OFPGained, defaultColor));

                /*
                 *  WAR
                 */
                // titles
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblDuration", Translation.Translate("lblDuration", language));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblCasualtiesT1", Translation.Translate("lblDeathsT1", language));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblCasualtiesT2", Translation.Translate("lblDeathsT2", language));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblIntelligenceGathered", Translation.Translate("lblOwnerChangeCount", language));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblAveragePlayerCountT1", Translation.Translate("lblAveragePlayerCountT1", language));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblAveragePlayerCountT2", Translation.Translate("lblAveragePlayerCountT2", language));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblFOBsPlacedT1", Translation.Translate("lblFOBsPlacedT1", language));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblFOBsPlacedT2", Translation.Translate("lblFOBsPlacedT2", language));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblFOBsDestroyedT1", Translation.Translate("lblFOBsDestroyedT1", language));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblFOBsDestroyedT2", Translation.Translate("lblFOBsDestroyedT2", language));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblTeamkillingCasualties", Translation.Translate("lblTeamkillingCasualties", language));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblLongestShot", Translation.Translate("lblTopRankingOfficer", language));

                // values
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "DurationValue", Translation.ObjectTranslate("stats_war_time_value", language, tracker.Duration, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "CasualtiesValueT1", Translation.ObjectTranslate("stats_war_value", language, tracker.casualtiesT1, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "CasualtiesValueT2", Translation.ObjectTranslate("stats_war_value", language, tracker.casualtiesT2, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "IntelligenceGathered", Translation.ObjectTranslate("stats_war_value", language, tracker.intelligenceGathered, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "AveragePlayerCountsT1Value", Translation.ObjectTranslate("stats_war_float_value", language, tracker.AverageTeam1Size, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "AveragePlayerCountsT2Value", Translation.ObjectTranslate("stats_war_float_value", language, tracker.AverageTeam2Size, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "FOBsPlacedT1Value", Translation.ObjectTranslate("stats_war_value", language, tracker.fobsPlacedT1, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "FOBsPlacedT2Value", Translation.ObjectTranslate("stats_war_value", language, tracker.fobsPlacedT2, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "FOBsDestroyedT1Value", Translation.ObjectTranslate("stats_war_value", language, tracker.fobsDestroyedT1, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "FOBsDestroyedT2Value", Translation.ObjectTranslate("stats_war_value", language, tracker.fobsDestroyedT2, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "TeamkillingCasualtiesValue", Translation.ObjectTranslate("stats_war_value", language, tracker.teamkillsT1 + tracker.teamkillsT2, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "LongestShotValue", longestShotTaken ?
                    Translation.Translate("longest_shot_format", language, longestShotDistance.ToString("N1"), longestShotWeapon,
                    F.ColorizeName(longestShotTaker.CharacterName, longestShotTakerTeam)) : NO_PLAYER_NAME_PLACEHOLDER);
            }
            catch (Exception ex)
            {
                L.LogError($"Error sending end screen to {F.GetPlayerOriginalNames(player).PlayerName} ( {player.Steam64} ).");
                L.LogError(ex);
            }
        }
        bool[][] states;
        protected override void Update()
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            float rt = Time.realtimeSinceStartup;
            for (int i = 1; i < Math.Min(15, statsT1.Count); i++)
            {
                UCPlayer pl = statsT1[i].Player == null ? null : UCPlayer.FromPlayer(statsT1[i].Player);
                if (states[0][i - 1])
                {
                    if (pl == null || rt - pl.LastSpoken > 1f)
                    {
                        UpdateStateT1(false, i);
                    }
                }
                else if (pl != null && rt - pl.LastSpoken <= 1f)
                {
                    UpdateStateT1(true, i);
                }
            }
            for (int i = 1; i < Math.Min(15, statsT2.Count); i++)
            {
                UCPlayer pl = statsT2[i].Player == null ? null : UCPlayer.FromPlayer(statsT2[i].Player);
                if (states[1][i - 1])
                {
                    if (pl == null || rt - pl.LastSpoken > 1f)
                    {
                        UpdateStateT2(false, i);
                    }
                }
                else if (pl != null && rt - pl.LastSpoken <= 1f)
                {
                    UpdateStateT2(true, i);
                }
            }
        }
        private void UpdateStateT1(bool newval, int index)
        {
            states[0][index - 1] = newval;
            for (int i = 0; i < Provider.clients.Count; i++)
                EffectManager.sendUIEffectVisibility(LeaderboardEx.leaderboardKey, Provider.clients[i].transportConnection, false, "1VC" + index.ToString(), newval);
        }
        private void UpdateStateT2(bool newval, int index)
        {
            states[1][index - 1] = newval;
            for (int i = 0; i < Provider.clients.Count; i++)
                EffectManager.sendUIEffectVisibility(LeaderboardEx.leaderboardKey, Provider.clients[i].transportConnection, false, "2VC" + index.ToString(), newval);
        }
    }
    public class InsurgencyTracker : TeamStatTracker<InsurgencyPlayerStats>, ILongestShotTracker, IFobsTracker
    {
        public int fobsPlacedT1;
        public int fobsPlacedT2;
        public int fobsDestroyedT1;
        public int fobsDestroyedT2;
        public int FOBsPlacedT1 { get => fobsPlacedT1; set => fobsPlacedT1 = value; }
        public int FOBsPlacedT2 { get => fobsPlacedT2; set => fobsPlacedT2 = value; }
        public int FOBsDestroyedT1 { get => fobsDestroyedT1; set => fobsDestroyedT1 = value; }
        public int FOBsDestroyedT2 { get => fobsDestroyedT2; set => fobsDestroyedT2 = value; }
        public int intelligenceGathered;
        private LongestShot _longestShot = LongestShot.Nil;
        public LongestShot LongestShot { get => _longestShot; set => _longestShot = value; }
        public override void Reset()
        {
            base.Reset();
            fobsPlacedT1 = 0;
            fobsPlacedT2 = 0;
            fobsDestroyedT1 = 0;
            fobsDestroyedT2 = 0;
            intelligenceGathered = 0;
            _longestShot = LongestShot.Nil;
        }
        public virtual void GetTopStats(int count, out List<InsurgencyPlayerStats> statsT1, out List<InsurgencyPlayerStats> statsT2)
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            List<InsurgencyPlayerStats> stats = this.stats.Values.ToList();

            stats.RemoveAll(p =>
            {
                if (p == null) return true;
                if (p.Player == null)
                {
                    SteamPlayer player = PlayerTool.getSteamPlayer(p.Steam64);
                    if (player == default || player.player == default) return true;
                    else p.Player = player.player;
                    return false;
                }
                else return false;
            });
            InsurgencyPlayerStats totalT1 = new InsurgencyPlayerStats(0UL);
            InsurgencyPlayerStats totalT2 = new InsurgencyPlayerStats(0UL);
            IEnumerator<InsurgencyPlayerStats> enumerator = stats.GetEnumerator();
            while (enumerator.MoveNext())
            {
                InsurgencyPlayerStats stat = enumerator.Current;

                if (stat.Steam64.GetTeamFromPlayerSteam64ID() == 1)
                {
                    totalT1.kills += stat.kills;
                    totalT1.deaths += stat.deaths;
                    totalT1.AddXP(stat.XPGained);
                    totalT1.AddOfficerPoints(stat.OFPGained);
                    totalT1.AddDamage(stat.DamageDone);
                }
                else if (stat.Steam64.GetTeamFromPlayerSteam64ID() == 2)
                {
                    totalT2.kills += stat.kills;
                    totalT2.deaths += stat.deaths;
                    totalT2.AddXP(stat.XPGained);
                    totalT2.AddOfficerPoints(stat.OFPGained);
                    totalT2.AddDamage(stat.DamageDone);
                }
            }
            enumerator.Dispose();

            stats.Sort((InsurgencyPlayerStats a, InsurgencyPlayerStats b) => b.XPGained.CompareTo(a.XPGained));

            statsT1 = stats.Where(p => p.Player.GetTeam() == 1).ToList();
            statsT2 = stats.Where(p => p.Player.GetTeam() == 2).ToList();
            statsT1.Take(count);
            statsT2.Take(count);
            statsT1.Insert(0, totalT1);
            statsT2.Insert(0, totalT2);
        }
    }

    public class InsurgencyPlayerStats : TeamPlayerStats, IExperienceStats, IFOBStats, IRevivesStats
    {
        public InsurgencyPlayerStats(Player player) : base(player) { }
        public InsurgencyPlayerStats(ulong player) : base(player) { }

        protected int _xp;
        protected int _ofp;
        protected int _fobsDestroyed;
        protected int _fobsPlaced;
        protected int _revives;
        internal int _killsAttack;
        internal int _killsDefense;
        internal int _cachesDestroyed;
        internal int _cachesDiscovered;
        internal int _intelligencePointsCollected;
        public int XPGained => _xp;
        public int OFPGained => _ofp;
        public int FOBsDestroyed => _fobsDestroyed;
        public int FOBsPlaced => _fobsPlaced;
        public int Revives => _revives;
        public int KillsAttack => _killsAttack;
        public int KillsDefense => _killsDefense;
        public void AddFOBDestroyed() => _fobsDestroyed++;
        public void AddFOBPlaced() => _fobsPlaced++;
        public void AddOfficerPoints(int amount) => _ofp += amount;
        public void AddXP(int amount) => _xp += amount;
        public void AddRevive() => _revives++;
        public override void Reset()
        {
            base.Reset();
            _xp = 0;
            _ofp = 0;
            _fobsDestroyed = 0;
            _fobsPlaced = 0;
            _revives = 0;
            _killsAttack = 0;
            _killsDefense = 0;
        }
    }
}

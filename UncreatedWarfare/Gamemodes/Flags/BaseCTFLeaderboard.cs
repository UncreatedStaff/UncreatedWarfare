﻿using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Uncreated.Players;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Networking;
using Uncreated.Warfare.Squads;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Flags
{
    public class BaseCTFLeaderboard<Stats, StatTracker> : Leaderboard<Stats, StatTracker> where Stats : BaseCTFStats where StatTracker : BaseCTFTracker<Stats>
    {
        protected override Guid GUID => Gamemode.Config.UI.CTFLeaderboardGUID;
        private List<Stats> statsT1;
        private List<Stats> statsT2;
        private bool longestShotTaken = false;
        private FPlayerName longestShotTaker = FPlayerName.Nil;
        private ulong longestShotTakerTeam = 0;
        private float longestShotDistance = 0;
        private string longestShotWeapon = string.Empty;
        public override void Calculate()
        {
            // topsquadplayers = warstats.GetTopSquad(out squadname, out squadteam, winner);
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
                if (Assets.find(EAssetType.ITEM, tracker.LongestShot.Gun) is ItemAsset asset)
                    longestShotWeapon = asset.itemName;
                else if (Assets.find(EAssetType.VEHICLE, tracker.LongestShot.Gun) is VehicleAsset vasset)
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
            string teamcolor = TeamManager.GetTeamHexColor(_winner);
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                SendLeaderboard(PlayerManager.OnlinePlayers[i], teamcolor);
            }
        }
        public virtual void SendLeaderboard(UCPlayer player, string teamcolor)
        {
            try
            {
                ulong team = player.GetTeam();
                ITransportConnection channel = player.Player.channel.owner.transportConnection;
                player.Player.movement.sendPluginSpeedMultiplier(0f);
                player.Player.life.serverModifyHealth(100);
                player.Player.life.serverModifyFood(100);
                player.Player.life.serverModifyWater(100);
                player.Player.life.serverModifyVirus(100);
                player.Player.life.serverModifyStamina(100);
                player.Player.movement.sendPluginJumpMultiplier(0f);

                if (Data.Is(out IRevives r))
                    r.ReviveManager.RevivePlayer(player.Player);

                if (!player.Player.life.isDead)
                    player.Player.teleportToLocationUnsafe(F.GetBaseSpawnFromTeam(team), F.GetBaseAngle(team));
                else
                    player.Player.life.ReceiveRespawnRequest(false);

                // resupply the kit.
                if (Data.Is<IKitRequests>(out _) && string.IsNullOrEmpty(player.KitName))
                {
                    if (KitManager.KitExists(player.KitName, out Kit kit))
                        KitManager.ResupplyKit(player, kit);
                }
                player.Player.setAllPluginWidgetFlags(EPluginWidgetFlags.None);
                if (Data.Is<IFlagRotation>(out _))
                    CTFUI.ClearFlagList(channel);
                tracker.stats.TryGetValue(player.Steam64, out Stats stats);
                if (stats == null) stats = BasePlayerStats.New<Stats>(player.Player);
                FPlayerName originalNames = F.GetPlayerOriginalNames(player);
                EffectManager.sendUIEffect(UCWarfare.Config.EndScreenUI, LeaderboardEx.leaderboardKey, channel, true);
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "TitleWinner", F.Translate("winner", player, TeamManager.TranslateName(_winner, player.Player), teamcolor));
                if (shuttingDown)
                    EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "NextGameStartsIn", F.Translate("next_game_start_label_shutting_down", player, shuttingDownMessage));
                else
                    EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "NextGameStartsIn", F.Translate("next_game_start_label", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "NextGameSeconds", F.ObjectTranslate("next_game_starting_format", player.Steam64, TimeSpan.FromSeconds(Gamemode.Config.GeneralConfig.LeaderboardTime)));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "NextGameCircleForeground", Gamemode.Config.UI.ProgressChars[0].ToString());

                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "PlayerGameStatsHeader", F.ObjectTranslate("player_name_header", player.Steam64,
                    originalNames.CharacterName, TeamManager.GetTeamHexColor(player.GetTeam()), (float)(stats.onlineCount1 + stats.onlineCount2) / tracker.coroutinect * 100f));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "WarHeader", F.Translate("war_name_header", player,
                    TeamManager.TranslateName(1, player.Steam64), TeamManager.Team1ColorHex,
                    TeamManager.TranslateName(2, player.Steam64), TeamManager.Team2ColorHex));

                /*
                 * LEADERBOARD
                 */
                for (int i = 0; i < Math.Min(15, statsT1.Count); i++)
                {
                    string n = i == 0 ? TeamManager.TranslateName(1, player.Steam64, true).ToUpper() : statsT1[i].Player.channel.owner.playerID.nickName;
                    string k = statsT1[i].kills.ToString(Data.Locale);
                    string d = statsT1[i].deaths.ToString(Data.Locale);
                    string x = statsT1[i].XPGained.ToString(Data.Locale);
                    string f = statsT1[i].OFPGained.ToString(Data.Locale);
                    string c = statsT1[i].Captures.ToString(Data.Locale);
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
                    string n = i == 0 ? TeamManager.TranslateName(2, player.Steam64, true).ToUpper() : statsT2[i].Player.channel.owner.playerID.nickName;
                    string k = statsT2[i].kills.ToString(Data.Locale);
                    string d = statsT2[i].deaths.ToString(Data.Locale);
                    string x = statsT2[i].XPGained.ToString(Data.Locale);
                    string f = statsT2[i].OFPGained.ToString(Data.Locale);
                    string c = statsT2[i].Captures.ToString(Data.Locale);
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
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblKills", F.Translate("lblKills", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblDeaths", F.Translate("lblDeaths", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblKDR", F.Translate("lblKDR", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblKillsOnPoint", F.Translate("lblKillsOnPoint", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblTimeDeployed", F.Translate("lblTimeDeployed", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblXpGained", F.Translate("lblXpGained", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblTimeOnPoint", F.Translate("lblTimeOnPoint", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblCaptures", F.Translate("lblCaptures", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblTimeInVehicle", F.Translate("lblTimeInVehicle", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblTeamkills", F.Translate("lblTeamkills", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblEnemyFOBsDestroyed", F.Translate("lblFOBsDestroyed", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblOfficerPointsGainedValue", F.Translate("lblOfficerPointsGained", player));

                string defaultColor = UCWarfare.GetColorHex("default");
                // values
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "KillsValue", F.ObjectTranslate("stats_player_value", player.Steam64, stats.kills, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "DeathsValue", F.ObjectTranslate("stats_player_value", player.Steam64, stats.deaths, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "KDRValue", F.ObjectTranslate("stats_player_float_value", player.Steam64, stats.KDR, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "KillsOnPointValue", F.ObjectTranslate("stats_player_value", player.Steam64, stats.KillsOnPoint, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "TimeDeployedValue", F.ObjectTranslate("stats_player_time_value", player.Steam64, TimeSpan.FromSeconds(stats.timedeployed), defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "XPGainedValue", F.ObjectTranslate("stats_player_value", player.Steam64, stats.XPGained, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "TimeOnPointValue", F.ObjectTranslate("stats_player_time_value", player.Steam64, TimeSpan.FromSeconds(stats.timeonpoint), defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "CapturesValue", F.ObjectTranslate("stats_player_value", player.Steam64, stats.Captures, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "TimeInVehicleValue", F.ObjectTranslate("stats_player_value", player.Steam64, stats.DamageDone, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "TeamkillsValue", F.ObjectTranslate("stats_player_value", player.Steam64, stats.teamkills, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "EnemyFOBsDestroyedValue", F.ObjectTranslate("stats_player_value", player.Steam64, stats.FOBsDestroyed, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "OfficerPointsGainedValue", F.ObjectTranslate("stats_player_value", player.Steam64, stats.OFPGained, defaultColor));

                /*
                 *  WAR
                 */
                // titles
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblDuration", F.Translate("lblDuration", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblCasualtiesT1", F.Translate("lblDeathsT1", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblCasualtiesT2", F.Translate("lblDeathsT2", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblOwnerChangedCount", F.Translate("lblOwnerChangeCount", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblAveragePlayerCountT1", F.Translate("lblAveragePlayerCountT1", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblAveragePlayerCountT2", F.Translate("lblAveragePlayerCountT2", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblFOBsPlacedT1", F.Translate("lblFOBsPlacedT1", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblFOBsPlacedT2", F.Translate("lblFOBsPlacedT2", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblFOBsDestroyedT1", F.Translate("lblFOBsDestroyedT1", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblFOBsDestroyedT2", F.Translate("lblFOBsDestroyedT2", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblTeamkillingCasualties", F.Translate("lblTeamkillingCasualties", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblTopRankingOfficer", F.Translate("lblTopRankingOfficer", player));

                // values
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "DurationValue", F.ObjectTranslate("stats_war_time_value", player.Steam64, tracker.Duration, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "CasualtiesValueT1", F.ObjectTranslate("stats_war_value", player.Steam64, tracker.casualtiesT1, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "CasualtiesValueT2", F.ObjectTranslate("stats_war_value", player.Steam64, tracker.casualtiesT2, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "FlagCapturesValue", F.ObjectTranslate("stats_war_value", player.Steam64, tracker.flagOwnerChanges, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "AveragePlayerCountsT1Value", F.ObjectTranslate("stats_war_float_value", player.Steam64, tracker.AverageTeam1Size, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "AveragePlayerCountsT2Value", F.ObjectTranslate("stats_war_float_value", player.Steam64, tracker.AverageTeam2Size, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "FOBsPlacedT1Value", F.ObjectTranslate("stats_war_value", player.Steam64, tracker.fobsPlacedT1, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "FOBsPlacedT2Value", F.ObjectTranslate("stats_war_value", player.Steam64, tracker.fobsPlacedT2, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "FOBsDestroyedT1Value", F.ObjectTranslate("stats_war_value", player.Steam64, tracker.fobsDestroyedT1, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "FOBsDestroyedT2Value", F.ObjectTranslate("stats_war_value", player.Steam64, tracker.fobsDestroyedT2, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "TeamkillingCasualtiesValue", F.ObjectTranslate("stats_war_value", player.Steam64, tracker.teamkillsT1 + tracker.teamkillsT2, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "TopRankingOfficerValue", longestShotTaken ?
                    F.Translate("longest_shot_format", player.Steam64, longestShotDistance.ToString("N1"), longestShotWeapon,
                    F.ColorizeName(longestShotTaker.CharacterName, longestShotTakerTeam)) : NO_PLAYER_NAME_PLACEHOLDER);
            }
            catch (Exception ex)
            {
                F.LogError($"Error sending end screen to {F.GetPlayerOriginalNames(player).PlayerName} ( {player.Steam64} ).");
                F.LogError(ex);
            }
        }
    }
    public struct LongestShot
    {
        public static readonly LongestShot Nil = new LongestShot() { Distance = 0, Gun = 0, Player = 0, Team = 0 };
        public ulong Player;
        public float Distance;
        public ushort Gun;
        public ulong Team;
    }

    public class BaseCTFStats : TeamPlayerStats, IExperienceStats, IFlagStats, IFOBStats, IRevivesStats
    {
        public BaseCTFStats(Player player) : base(player) { }
        public BaseCTFStats(ulong player) : base(player) { }

        protected int _xp;
        protected int _ofp;
        protected int _caps;
        protected int _fobsDestroyed;
        protected int _fobsPlaced;
        protected int _revives;
        protected int _killsOnPoint;
        public int XPGained => _xp;
        public int OFPGained => _ofp;
        public int Captures => _caps;
        public int FOBsDestroyed => _fobsDestroyed;
        public int FOBsPlaced => _fobsPlaced;
        public int Revives => _revives;
        public int KillsOnPoint => _killsOnPoint;
        public void AddCapture() => _caps++;
        public void AddCaptures(int amount) => _caps += amount;
        public void AddFOBDestroyed() => _fobsDestroyed++;
        public void AddFOBPlaced() => _fobsPlaced++;
        public void AddOfficerPoints(int amount) => _ofp += amount;
        public void AddXP(int amount) => _xp += amount;
        public void AddRevive() => _revives++;
        public void AddKillOnPoint() => _killsOnPoint++;
        public override void Reset()
        {
            base.Reset();
            _xp = 0;
            _ofp = 0;
            _caps = 0;
            _fobsDestroyed = 0;
            _fobsPlaced = 0;
            _revives = 0;
        }
    }

    public abstract class BaseCTFTracker<T> : TeamStatTracker<T> where T : BaseCTFStats
    {
        public int fobsPlacedT1;
        public int fobsPlacedT2;
        public int fobsDestroyedT1;
        public int fobsDestroyedT2;
        public int flagOwnerChanges;
        public LongestShot LongestShot = LongestShot.Nil;

        public override void Reset()
        {
            base.Reset();
            fobsPlacedT1 = 0;
            fobsPlacedT2 = 0;
            fobsDestroyedT1 = 0;
            fobsDestroyedT2 = 0;
            flagOwnerChanges = 0;
            LongestShot = LongestShot.Nil;
        }
        public virtual void GetTopStats(int count, out List<T> statsT1, out List<T> statsT2)
        {
            List<T> stats = this.stats.Values.ToList();

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

            T totalT1 = BasePlayerStats.New<T>(0UL);
            T totalT2 = BasePlayerStats.New<T>(0UL);
            IEnumerator<T> enumerator = stats.GetEnumerator();
            while (enumerator.MoveNext())
            {
                T stat = enumerator.Current;

                if (stat.Steam64.GetTeamFromPlayerSteam64ID() == 1)
                {
                    totalT1.kills += stat.kills;
                    totalT1.deaths += stat.deaths;
                    totalT1.AddXP(stat.XPGained);
                    totalT1.AddOfficerPoints(stat.OFPGained);
                    totalT1.AddCaptures(stat.Captures);
                    totalT1.AddDamage(stat.DamageDone);
                }
                else if (stat.Steam64.GetTeamFromPlayerSteam64ID() == 2)
                {
                    totalT2.kills += stat.kills;
                    totalT2.deaths += stat.deaths;
                    totalT2.AddXP(stat.XPGained);
                    totalT2.AddOfficerPoints(stat.OFPGained);
                    totalT2.AddCaptures(stat.Captures);
                    totalT2.AddDamage(stat.DamageDone);
                }
            }
            enumerator.Dispose();

            stats.Sort((T a, T b) => b.XPGained.CompareTo(a.XPGained));

            statsT1 = stats.Where(p => p.Player.GetTeam() == 1).ToList();
            statsT2 = stats.Where(p => p.Player.GetTeam() == 2).ToList();
            statsT1.Take(count);
            statsT2.Take(count);
            statsT1.Insert(0, totalT1);
            statsT2.Insert(0, totalT2);
        }
    }

    public class TeamCTFTracker : BaseCTFTracker<BaseCTFStats>
    {

    }
    public class InvasionTracker : BaseCTFTracker<BaseCTFStats>
    {

    }
}
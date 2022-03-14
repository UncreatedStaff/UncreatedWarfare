using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using Uncreated.Players;
using Uncreated.Warfare.Gamemodes.Flags.TeamCTF;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare.Gamemodes.Flags
{
    public class BaseCTFLeaderboard<Stats, StatTracker> : Leaderboard<Stats, StatTracker> where Stats : BaseCTFStats where StatTracker : BaseCTFTracker<Stats>
    {
        protected override Guid GUID => Gamemode.Config.UI.CTFLeaderboardGUID;
        private List<Stats>? statsT1;
        private List<Stats>? statsT2;
        private bool longestShotTaken = false;
        private FPlayerName longestShotTaker = FPlayerName.Nil;
        private ulong longestShotTakerTeam = 0;
        private float longestShotDistance = 0;
        internal EffectAsset? asset;
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
                if (Assets.find(tracker.LongestShot.Gun) is ItemAsset asset)
                    longestShotWeapon = asset.itemName;
                else if (Assets.find(tracker.LongestShot.Gun) is VehicleAsset vasset)
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
            string teamcolor = TeamManager.GetTeamHexColor(_winner);
            if (Assets.find(GUID) is not EffectAsset asset) return;
            this.asset = asset;
            states = new bool[2][] { new bool[Math.Min(14, statsT1!.Count - 1)], new bool[Math.Min(14, statsT2!.Count - 1)] };
            for (int i = 0; i < PlayerManager.OnlinePlayers.Count; i++)
            {
                SendLeaderboard(PlayerManager.OnlinePlayers[i], teamcolor);
            }
        }
        public virtual void SendLeaderboard(UCPlayer player, string teamcolor)
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

                if (Data.Is(out IRevives r))
                    r.ReviveManager.RevivePlayer(player.Player);

                if (!player.Player.life.isDead)
                    player.Player.teleportToLocationUnsafe(team.GetBaseSpawnFromTeam(), team.GetBaseAngle());
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
                
                EffectManager.sendUIEffect(this.asset!.id, LeaderboardEx.leaderboardKey, channel, true);
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "TitleWinner", Translation.Translate("winner", player, TeamManager.TranslateName(_winner, player.Player), teamcolor));
                if (shuttingDown && shuttingDownMessage != null)
                    EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "NextGameStartsIn", Translation.Translate("next_game_start_label_shutting_down", player, shuttingDownMessage));
                else
                    EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "NextGameStartsIn", Translation.Translate("next_game_start_label", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "NextGameSeconds", Translation.ObjectTranslate("next_game_starting_format", player.Steam64, TimeSpan.FromSeconds(Gamemode.Config.GeneralConfig.LeaderboardTime)));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "NextGameCircleForeground", Gamemode.Config.UI.ProgressChars[0].ToString());
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "PlayerGameStatsHeader", Translation.ObjectTranslate("player_name_header", player.Steam64,
                    originalNames.CharacterName, TeamManager.GetTeamHexColor(player.GetTeam()), (float)(stats.onlineCount1 + stats.onlineCount2) / tracker.coroutinect * 100f));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "WarHeader", Translation.Translate("war_name_header", player,
                    TeamManager.TranslateName(1, player.Steam64), TeamManager.Team1ColorHex,
                    TeamManager.TranslateName(2, player.Steam64), TeamManager.Team2ColorHex));

                /*
                 * LEADERBOARD
                 */
                for (int i = 0; i < Math.Min(15, statsT1!.Count); i++)
                {
                    string n = i == 0 ? TeamManager.TranslateShortName(1, player.Steam64, true).ToUpper() : statsT1[i].Player.channel.owner.playerID.nickName;
                    string k = statsT1[i].kills.ToString(Data.Locale);
                    string d = statsT1[i].deaths.ToString(Data.Locale);
                    string x = statsT1[i].XPGained.ToString(Data.Locale);
                    string f = statsT1[i].Credits.ToString(Data.Locale);
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
                    if (i != 0)
                        EffectManager.sendUIEffectVisibility(LeaderboardEx.leaderboardKey, channel, true, "1VC" + i, false);
                    
                }
                for (int i = 0; i < Math.Min(15, statsT2!.Count); i++)
                {
                    string n = i == 0 ? TeamManager.TranslateShortName(2, player.Steam64, true).ToUpper() : statsT2[i].Player.channel.owner.playerID.nickName;
                    string k = statsT2[i].kills.ToString(Data.Locale);
                    string d = statsT2[i].deaths.ToString(Data.Locale);
                    string x = statsT2[i].XPGained.ToString(Data.Locale);
                    string f = statsT2[i].Credits.ToString(Data.Locale);
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
                    if (i != 0)
                        EffectManager.sendUIEffectVisibility(LeaderboardEx.leaderboardKey, channel, true, "2VC" + i, false);
                }

                //UCPlayer topOfficer = PlayerManager.OnlinePlayers.OrderByDescending(x => x.cachedOfp).FirstOrDefault();
                //if (topOfficer.cachedOfp == 0) topOfficer = default;
                /*
                 *  STATS
                 */
                // titles
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblKills", Translation.Translate("lblKills", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblDeaths", Translation.Translate("lblDeaths", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblKDR", Translation.Translate("lblKDR", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblKillsOnPoint", Translation.Translate("lblKillsOnPoint", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblTimeDeployed", Translation.Translate("lblTimeDeployed", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblXpGained", Translation.Translate("lblXpGained", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblTimeOnPoint", Translation.Translate("lblTimeOnPoint", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblCaptures", Translation.Translate("lblCaptures", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblTimeInVehicle", Translation.Translate("lblTimeInVehicle", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblTeamkills", Translation.Translate("lblTeamkills", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblEnemyFOBsDestroyed", Translation.Translate("lblFOBsDestroyed", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblOfficerPointsGainedValue", Translation.Translate("lblOfficerPointsGained", player));

                string defaultColor = UCWarfare.GetColorHex("default");
                // values
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "KillsValue", Translation.ObjectTranslate("stats_player_value", player.Steam64, stats.kills, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "DeathsValue", Translation.ObjectTranslate("stats_player_value", player.Steam64, stats.deaths, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "KDRValue", Translation.ObjectTranslate("stats_player_float_value", player.Steam64, stats.KDR, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "KillsOnPointValue", Translation.ObjectTranslate("stats_player_value", player.Steam64, stats.KillsOnPoint, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "TimeDeployedValue", Translation.ObjectTranslate("stats_player_time_value", player.Steam64, TimeSpan.FromSeconds(stats.timedeployed), defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "XPGainedValue", Translation.ObjectTranslate("stats_player_value", player.Steam64, stats.XPGained, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "TimeOnPointValue", Translation.ObjectTranslate("stats_player_time_value", player.Steam64, TimeSpan.FromSeconds(stats.timeonpoint), defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "CapturesValue", Translation.ObjectTranslate("stats_player_value", player.Steam64, stats.Captures, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "TimeInVehicleValue", Translation.ObjectTranslate("stats_player_value", player.Steam64, stats.DamageDone, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "TeamkillsValue", Translation.ObjectTranslate("stats_player_value", player.Steam64, stats.teamkills, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "EnemyFOBsDestroyedValue", Translation.ObjectTranslate("stats_player_value", player.Steam64, stats.FOBsDestroyed, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "OfficerPointsGainedValue", Translation.ObjectTranslate("stats_player_value", player.Steam64, stats.Credits, defaultColor));

                /*
                 *  WAR
                 */
                // titles
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblDuration", Translation.Translate("lblDuration", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblCasualtiesT1", Translation.Translate("lblDeathsT1", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblCasualtiesT2", Translation.Translate("lblDeathsT2", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblOwnerChangedCount", Translation.Translate("lblOwnerChangeCount", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblAveragePlayerCountT1", Translation.Translate("lblAveragePlayerCountT1", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblAveragePlayerCountT2", Translation.Translate("lblAveragePlayerCountT2", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblFOBsPlacedT1", Translation.Translate("lblFOBsPlacedT1", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblFOBsPlacedT2", Translation.Translate("lblFOBsPlacedT2", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblFOBsDestroyedT1", Translation.Translate("lblFOBsDestroyedT1", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblFOBsDestroyedT2", Translation.Translate("lblFOBsDestroyedT2", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblTeamkillingCasualties", Translation.Translate("lblTeamkillingCasualties", player));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "lblTopRankingOfficer", Translation.Translate("lblTopRankingOfficer", player));

                // values
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "DurationValue", Translation.ObjectTranslate("stats_war_time_value", player.Steam64, tracker.Duration, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "CasualtiesValueT1", Translation.ObjectTranslate("stats_war_value", player.Steam64, tracker.casualtiesT1, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "CasualtiesValueT2", Translation.ObjectTranslate("stats_war_value", player.Steam64, tracker.casualtiesT2, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "FlagCapturesValue", Translation.ObjectTranslate("stats_war_value", player.Steam64, tracker.flagOwnerChanges, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "AveragePlayerCountsT1Value", Translation.ObjectTranslate("stats_war_float_value", player.Steam64, tracker.AverageTeam1Size, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "AveragePlayerCountsT2Value", Translation.ObjectTranslate("stats_war_float_value", player.Steam64, tracker.AverageTeam2Size, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "FOBsPlacedT1Value", Translation.ObjectTranslate("stats_war_value", player.Steam64, tracker.fobsPlacedT1, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "FOBsPlacedT2Value", Translation.ObjectTranslate("stats_war_value", player.Steam64, tracker.fobsPlacedT2, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "FOBsDestroyedT1Value", Translation.ObjectTranslate("stats_war_value", player.Steam64, tracker.fobsDestroyedT1, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "FOBsDestroyedT2Value", Translation.ObjectTranslate("stats_war_value", player.Steam64, tracker.fobsDestroyedT2, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "TeamkillingCasualtiesValue", Translation.ObjectTranslate("stats_war_value", player.Steam64, tracker.teamkillsT1 + tracker.teamkillsT2, defaultColor));
                EffectManager.sendUIEffectText(LeaderboardEx.leaderboardKey, channel, true, "TopRankingOfficerValue", longestShotTaken ?
                    Translation.Translate("longest_shot_format", player.Steam64, longestShotDistance.ToString("N1"), longestShotWeapon,
                    F.ColorizeName(longestShotTaker.CharacterName, longestShotTakerTeam)) : NO_PLAYER_NAME_PLACEHOLDER);
            }
            catch (Exception ex)
            {
                L.LogError($"Error sending end screen to {F.GetPlayerOriginalNames(player).PlayerName} ( {player.Steam64} ).");
                L.LogError(ex);
            }
        }
        bool[][]? states;
        protected override void Update()
        {
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
            float rt = Time.realtimeSinceStartup;
            for (int i = 1; i < Math.Min(15, statsT1!.Count); i++)
            {
                UCPlayer? pl = statsT1[i].Player == null ? null : UCPlayer.FromPlayer(statsT1[i].Player);
                if (states![0][i - 1])
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
            for (int i = 1; i < Math.Min(15, statsT2!.Count); i++)
            {
                UCPlayer? pl = statsT2[i].Player == null ? null : UCPlayer.FromPlayer(statsT2[i].Player);
                if (states![1][i - 1])
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
            states![0][index - 1] = newval;
            for (int i = 0; i < Provider.clients.Count; i++)
                EffectManager.sendUIEffectVisibility(LeaderboardEx.leaderboardKey, Provider.clients[i].transportConnection, false, "1VC" + index.ToString(), newval);
        }
        private void UpdateStateT2(bool newval, int index)
        {
            states![1][index - 1] = newval;
            for (int i = 0; i < Provider.clients.Count; i++)
                EffectManager.sendUIEffectVisibility(LeaderboardEx.leaderboardKey, Provider.clients[i].transportConnection, false, "2VC" + index.ToString(), newval);
        }
    }
    public struct LongestShot
    {
        public static readonly LongestShot Nil = new LongestShot() { Distance = 0, Gun = Guid.Empty, Player = 0, Team = 0 };
        public ulong Player;
        public float Distance;
        public Guid Gun;
        public ulong Team;
    }

    public class BaseCTFStats : TeamPlayerStats, IExperienceStats, IFlagStats, IFOBStats, IRevivesStats
    {
        public BaseCTFStats(Player player) : base(player) { }
        public BaseCTFStats(ulong player) : base(player) { }

        protected int _xp;
        protected int _credits;
        protected int _caps;
        protected int _fobsDestroyed;
        protected int _fobsPlaced;
        protected int _revives;
        protected int _killsOnPoint;
        public int XPGained => _xp;
        public int Credits => _credits;
        public int Captures => _caps;
        public int FOBsDestroyed => _fobsDestroyed;
        public int FOBsPlaced => _fobsPlaced;
        public int Revives => _revives;
        public int KillsOnPoint => _killsOnPoint;
        public void AddCapture() => _caps++;
        public void AddCaptures(int amount) => _caps += amount;
        public void AddFOBDestroyed() => _fobsDestroyed++;
        public void AddFOBPlaced() => _fobsPlaced++;
        public void AddCredits(int amount) => _credits += amount;
        public void AddXP(int amount) => _xp += amount;
        public void AddRevive() => _revives++;
        public void AddKillOnPoint() => _killsOnPoint++;
        public override void Reset()
        {
            base.Reset();
            _xp = 0;
            _credits = 0;
            _caps = 0;
            _fobsDestroyed = 0;
            _fobsPlaced = 0;
            _revives = 0;
            _killsOnPoint = 0;
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
#if DEBUG
            using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
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
                    totalT1.AddCredits(stat.Credits);
                    totalT1.AddCaptures(stat.Captures);
                    totalT1.AddDamage(stat.DamageDone);
                }
                else if (stat.Steam64.GetTeamFromPlayerSteam64ID() == 2)
                {
                    totalT2.kills += stat.kills;
                    totalT2.deaths += stat.deaths;
                    totalT2.AddXP(stat.XPGained);
                    totalT2.AddCredits(stat.Credits);
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

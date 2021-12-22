using Newtonsoft.Json;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uncreated.Networking.Encoding;
using Uncreated.Players;
using Uncreated.Warfare.Gamemodes.Interfaces;
using Uncreated.Warfare.Officers;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.XP
{
    public static class XPManager
    {
        public static Config<XPData> config = new Config<XPData>(Data.XPStorage, "config.json");
        public static void OnPlayerJoined(UCPlayer player)
        {
            L.Log(player.CharacterName);
            if (player.IsTeam1() || player.IsTeam2())
            {
                int amt = GetXP(player.Player, true);
                UpdateUI(player.Player, amt, out _);
            }
        }
        public static void OnGroupChanged(SteamPlayer player, ulong oldGroup, ulong newGroup)
        {
            int xp = GetXP(player.player, true);
            UpdateUI(player.player, xp, out _);
        }
        public static int GetXP(Player player, bool important)
        {
            UCPlayer ucplayer = UCPlayer.FromPlayer(player);
            if (ucplayer == default || important || ucplayer.CachedXp == -1)
            {
                int newxp = Data.DatabaseManager.GetXP(player.channel.owner.playerID.steamID.m_SteamID);
                if (ucplayer != null)
                    ucplayer.CachedXp = newxp;
                return newxp;
            }
            else return ucplayer.CachedXp;
        }
        public static int GetXP(ulong player, bool important)
        {
            UCPlayer ucplayer = UCPlayer.FromID(player);
            if (ucplayer == default || important || ucplayer.CachedXp == -1)
            {
                int newxp = Data.DatabaseManager.GetXP(player);
                if (ucplayer != default)
                    ucplayer.CachedXp = newxp;
                return newxp;
            }
            else return ucplayer.CachedXp;
        }
        public static void AddXP(Player player, int amount, string message = "", bool ignoreToastQueue = false)
        {
            if (!Data.TrackStats) return;
            UCPlayer ucplayer = UCPlayer.FromPlayer(player);

            Rank oldRank = null;
            if (ucplayer != null)
            {
                oldRank = ucplayer.XPRank();
            }

            int newBalance = Data.DatabaseManager.AddXP(player.channel.owner.playerID.steamID.m_SteamID, (int)(amount * config.Data.XPMultiplier));

            if (ucplayer != null)
            {
                ucplayer.CachedXp = newBalance;
            }

            if (message != "" && amount != 0 && !(Data.Gamemode is IEndScreen lb && lb.isScreenUp))
                ToastMessage.QueueMessage(player, Translation.Translate(amount >= 0 ? "gain_xp" : "loss_xp", player, Math.Abs(amount).ToString(Data.Locale)), message, EToastMessageSeverity.MINIXP);

            UpdateUI(player, newBalance, out Rank rank);

            if (rank.level > oldRank?.level)
            {
                ToastMessage.QueueMessage(player, Translation.Translate("promoted_xp", player), rank.TranslateName(player.channel.owner.playerID.steamID.m_SteamID).ToUpper(), EToastMessageSeverity.BIG);
                Chat.BroadcastToAllExcept(new List<CSteamID>() { ucplayer.CSteamID }, "xp_announce_promoted", F.GetPlayerOriginalNames(ucplayer).CharacterName, rank.TranslateName(ucplayer.Steam64));
            }
            else if (rank.level < oldRank?.level)
            {
                ToastMessage.QueueMessage(player, Translation.Translate("demoted_xp", player), rank.TranslateName(player.channel.owner.playerID.steamID.m_SteamID).ToUpper(), EToastMessageSeverity.BIG);
                Chat.BroadcastToAllExcept(new List<CSteamID>() { ucplayer.CSteamID }, "xp_announce_demoted", F.GetPlayerOriginalNames(ucplayer).CharacterName, rank.TranslateName(ucplayer.Steam64));
            }

            for (int i = 0; i < VehicleSigns.ActiveObjects.Count; i++)
                VehicleSigns.ActiveObjects[i].InvokeUpdate(player.channel.owner);
            // update the color of the ranks on all the vehicle signs in case the player unlocked a new rank.
            for (int i = 0; i < Kits.RequestSigns.ActiveObjects.Count; i++)
                Kits.RequestSigns.ActiveObjects[i].InvokeUpdate(player.channel.owner);
            // update the color of the ranks on all the request signs in case the player unlocked a new rank.
            if (player.TryGetPlaytimeComponent(out Components.PlaytimeComponent c) && c.stats is IExperienceStats ex)
            {
                ex.AddXP(amount);
            }
        }
        public static void UpdateUI(Player nelsonplayer, int balance, out Rank rank)
        {
            UCPlayer player = UCPlayer.FromPlayer(nelsonplayer);

            rank = GetRank(balance, out int currentXP, out Rank nextRank);
            short key = unchecked((short)config.Data.RankUI);
            if (player.OfficerRank != null)
            {
                EffectManager.sendUIEffect(config.Data.RankUI, key, player.Player.channel.owner.transportConnection, true);
                EffectManager.sendUIEffectText(key, player.Player.channel.owner.transportConnection, true,
                    "Rank", player.OfficerRank.TranslateName(nelsonplayer.channel.owner.playerID.steamID.m_SteamID)
                );
                EffectManager.sendUIEffectText(key, player.Player.channel.owner.transportConnection, true,
                    "Level", rank.level == 0 ? "" : Translation.Translate("ui_ofp_level", player, player.OfficerRank.level.ToString(Data.Locale))
                );
                EffectManager.sendUIEffectText(key, player.Player.channel.owner.transportConnection, true,
                    "XP", nextRank != null ? currentXP + "/" + rank.XP : currentXP.ToString(Data.Locale)
                );
                EffectManager.sendUIEffectText(key, player.Player.channel.owner.transportConnection, true,
                    "Next", nextRank != null ? Translation.Translate("ui_ofp_equivalent", player, nextRank.level.ToString(Data.Locale)) : ""
                );
                EffectManager.sendUIEffectText(key, player.Player.channel.owner.transportConnection, true,
                    "Progress", GetProgress(currentXP, rank.XP)
                );
            }
            else
            {
                EffectManager.sendUIEffect(config.Data.RankUI, unchecked((short)config.Data.RankUI), player.Player.channel.owner.transportConnection, true);
                EffectManager.sendUIEffectText(key, player.Player.channel.owner.transportConnection, true,
                    "Rank", rank.TranslateName(nelsonplayer.channel.owner.playerID.steamID.m_SteamID)
                );
                EffectManager.sendUIEffectText(key, player.Player.channel.owner.transportConnection, true,
                    "Level", rank.level == 0 ? "" : Translation.Translate("ui_xp_level", player, rank.level.ToString(Data.Locale))
                );
                EffectManager.sendUIEffectText(key, player.Player.channel.owner.transportConnection, true,
                    "XP", nextRank != null ? currentXP + "/" + rank.XP : currentXP.ToString()
                );
                EffectManager.sendUIEffectText(key, player.Player.channel.owner.transportConnection, true,
                    "Next", nextRank != null ?
                    Translation.Translate("ui_xp_next_level", player, nextRank.TranslateName(nelsonplayer.channel.owner.playerID.steamID.m_SteamID), nextRank.level.ToString(Data.Locale)) : string.Empty
                );
                EffectManager.sendUIEffectText(key, player.Player.channel.owner.transportConnection, true,
                    "Progress", GetProgress(currentXP, rank.XP)
                );
            }
        }
        public static string GetProgress(int currentPoints, int totalPoints, int barLength = 50)
        {
            float ratio = currentPoints / (float)totalPoints;

            int progress = UnityEngine.Mathf.RoundToInt(ratio * barLength);

            StringBuilder bars = new StringBuilder();
            for (int i = 0; i < progress; i++)
            {
                bars.Append(OfficerManager.config.Data.FullBlock);
            }
            return bars.ToString();
        }
        public static Rank GetRankFromLevel(int level, bool clamp = true)
        {
            if (level < 0) return clamp ? (config.Data.Ranks.Length > 0 ? config.Data.Ranks[0] : null) : null;
            if (config.Data.Ranks.Length > level) return config.Data.Ranks[level];
            return clamp ? (config.Data.Ranks.Length > 0 ? config.Data.Ranks[config.Data.Ranks.Length - 1] : null) : null;
        }
        public static Rank GetRank(int xpBalance, out int currentXP, out Rank nextRank)
        {
            int requiredXP = 0;
            nextRank = null;
            for (int i = 0; i < config.Data.Ranks.Length; i++)
            {
                requiredXP += config.Data.Ranks[i].XP;
                if (xpBalance < requiredXP)
                {
                    if (i + 1 < config.Data.Ranks.Length)
                        nextRank = config.Data.Ranks[i + 1];

                    currentXP = unchecked(config.Data.Ranks[i].XP - (requiredXP - xpBalance));
                    return config.Data.Ranks[i];
                }
            }
            currentXP = unchecked(xpBalance - requiredXP);
            return config.Data.Ranks.Last();
        }
    }
    public static class RankEx
    {
        public static string TranslateName(this Rank rank, ulong player)
        {
            if (player == 0)
            {
                if (rank.name_translations.TryGetValue(JSONMethods.DefaultLanguage, out string newname))
                {
                    return newname;
                }
                else if (rank.name_translations.Count > 0)
                {
                    return rank.name_translations.ElementAt(0).Value;
                }
                else return rank.name;
            }
            else
            {
                if (Data.Languages.TryGetValue(player, out string lang))
                {
                    if (!rank.name_translations.TryGetValue(lang, out string newname))
                    {
                        if (rank.name_translations.TryGetValue(JSONMethods.DefaultLanguage, out newname))
                        {
                            return newname;
                        }
                        else if (rank.name_translations.Count > 0)
                        {
                            return rank.name_translations.ElementAt(0).Value;
                        }
                        else return rank.name;
                    }
                    else
                    {
                        return newname;
                    }
                }
                else
                {
                    if (rank.name_translations.TryGetValue(JSONMethods.DefaultLanguage, out string newname))
                    {
                        return newname;
                    }
                    else if (rank.name_translations.Count > 0)
                    {
                        return rank.name_translations.ElementAt(0).Value;
                    }
                    else return rank.name;
                }
            }
        }
        public static string TranslateAbbreviation(this Rank rank, ulong player)
        {
            if (player == 0)
            {
                if (rank.abbreviation_translations.TryGetValue(JSONMethods.DefaultLanguage, out string newname))
                {
                    return newname;
                }
                else if (rank.abbreviation_translations.Count > 0)
                {
                    return rank.abbreviation_translations.ElementAt(0).Value;
                }
                else return rank.abbreviation;
            }
            else
            {
                if (Data.Languages.TryGetValue(player, out string lang))
                {
                    if (!rank.abbreviation_translations.TryGetValue(lang, out string newname))
                    {
                        if (rank.abbreviation_translations.TryGetValue(JSONMethods.DefaultLanguage, out newname))
                        {
                            return newname;
                        }
                        else if (rank.abbreviation_translations.Count > 0)
                        {
                            return rank.abbreviation_translations.ElementAt(0).Value;
                        }
                        else return rank.abbreviation;
                    }
                    else
                    {
                        return newname;
                    }
                }
                else
                {
                    if (rank.abbreviation_translations.TryGetValue(JSONMethods.DefaultLanguage, out string newname))
                    {
                        return newname;
                    }
                    else if (rank.abbreviation_translations.Count > 0)
                    {
                        return rank.abbreviation_translations.ElementAt(0).Value;
                    }
                    else return rank.abbreviation;
                }
            }
        }
    }

    public class XPData : ConfigData
    {
        public int EnemyKilledXP;
        public int FriendlyKilledXP;
        public int FriendlyRevivedXP;
        public int FOBKilledXP;
        public int FOBTeamkilledXP;
        public int FOBDeployedXP;
        public int FlagCapturedXP;
        public int FlagAttackXP;
        public int FlagDefendXP;
        public int FlagNeutralizedXP;
        public int TransportPlayerXP;
        public float TimeBetweenXpAndOfpAwardForTransport;
        public int ShovelXP;
        public int BuiltFOBXP;
        public int BuiltAmmoCrateXP;
        public int BuiltRepairStationXP;
        public int BuiltEmplacementXP;
        public int BuiltBarricadeXP;
        public int OnDutyXP;
        public int RessupplyFriendlyXP;
        public Dictionary<EVehicleType, int> VehicleDestroyedXP;

        public float XPMultiplier;

        public ushort RankUI;

        public Rank[] Ranks;

        public override void SetDefaults()
        {
            EnemyKilledXP = 10;
            FriendlyKilledXP = -50;
            FriendlyRevivedXP = 25;
            FOBKilledXP = 100;
            FOBTeamkilledXP = -1000;
            FOBDeployedXP = 10;
            FlagCapturedXP = 120;
            FlagAttackXP = 5;
            FlagDefendXP = 5;
            FlagNeutralizedXP = 40;
            TransportPlayerXP = 10;
            TimeBetweenXpAndOfpAwardForTransport = 10f;
            ShovelXP = 3;
            BuiltFOBXP = 50;
            BuiltAmmoCrateXP = 10;
            BuiltRepairStationXP = 25;
            BuiltEmplacementXP = 15;
            BuiltBarricadeXP = 5;
            OnDutyXP = 5;
            RessupplyFriendlyXP = 25;

            VehicleDestroyedXP = new Dictionary<EVehicleType, int>()
            {
                {EVehicleType.HUMVEE, 40},
                {EVehicleType.TRANSPORT, 30},
                {EVehicleType.LOGISTICS, 50},
                {EVehicleType.SCOUT_CAR, 60},
                {EVehicleType.APC, 80},
                {EVehicleType.IFV, 100},
                {EVehicleType.MBT, 200},
                {EVehicleType.HELI_TRANSPORT, 60},
                {EVehicleType.EMPLACEMENT, 30},
            };

            XPMultiplier = 1;

            RankUI = 36031;

            Ranks = new Rank[]
            {
                new Rank(0, "Recruit", "Rec.", 1500),
                new Rank(1, "Private", "Pvt.", 3800),
                new Rank(2, "Private 1st Class", "Pfc.", 5300),
                new Rank(3, "Corporal", "Cpl.", 7200),
                new Rank(4, "Specialist", "Spec.", 12500),
                new Rank(5, "Sergeant", "Sgt.", 15000),
                new Rank(6, "Staff Sergeant", "Ssg.", 20000),
                new Rank(7, "Sergeant 1st Class", "Sfc.", 24000),
                new Rank(8, "Sergeant Major", "S.M.", 28000),
                new Rank(9, "Warrant Officer", "W.O.", 32000),
                new Rank(10, "Chief Warrant Officer", "C.W.O.", 40000)
            };
        }
        public XPData()
        { }
    }
}

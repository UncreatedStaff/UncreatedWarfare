using Newtonsoft.Json;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Players;
using Uncreated.Warfare.Officers;
using Uncreated.Warfare.Teams;
using Uncreated.Warfare.Vehicles;
using Flag = Uncreated.Warfare.Gamemodes.Flags.Flag;

namespace Uncreated.Warfare.XP
{
    public class XPManager
    {
        public static Config<XPData> config;

        public XPManager()
        {
            config = new Config<XPData>(Data.XPStorage, "config.json");
        }

        public static async Task OnPlayerJoined(UCPlayer player)
        {
            F.Log(player.CharacterName);
            if (player.IsTeam1() || player.IsTeam2())
            {
                int amt = await GetXP(player.Player, player.GetTeam(), true);
                UpdateUI(player.Player, amt);
            }
        }
        public static async Task OnPlayerLeft(UCPlayer player)
        {
            await Task.Yield(); // just to remove the warning, feel free to remove, its basically an empty line.
        }
        public static async Task OnGroupChanged(SteamPlayer player, ulong oldGroup, ulong newGroup)
        {
            int xp = await GetXP(player.player, newGroup, true);
            UpdateUI(player.player, xp);
        }
        public static async Task<int> GetXP(Player player, ulong team, bool important)
        {
            if (team < 1 || team > 2) return 0;
            UCPlayer ucplayer = UCPlayer.FromPlayer(player);
            if (ucplayer == default || important || ucplayer.cachedXp == -1)
            {
                int newxp = await Data.DatabaseManager.GetXP(player.channel.owner.playerID.steamID.m_SteamID, team);
                if (ucplayer != null)
                    ucplayer.cachedXp = newxp;
                return newxp;
            } else return ucplayer.cachedXp;
        }
        public static async Task<int> GetXP(ulong player, ulong team, bool important)
        {
            if (team < 1 || team > 2) return 0;
            UCPlayer ucplayer = UCPlayer.FromID(player);
            if (ucplayer == default || important || ucplayer.cachedXp == -1)
            {
                int newxp = await Data.DatabaseManager.GetXP(player, team);
                if (ucplayer != default)
                    ucplayer.cachedXp = newxp;
                return newxp;
            }
            else return ucplayer.cachedXp;
        }
        public static async Task AddXP(Player player, ulong team, int amount, string message = "")
        {
            if (team < 1 || team > 2) return;
            UCPlayer ucplayer = UCPlayer.FromPlayer(player);
            int newBalance = await Data.DatabaseManager.AddXP(player.channel.owner.playerID.steamID.m_SteamID, team, (int)(amount * config.Data.XPMultiplier));
            if (ucplayer != null)
                ucplayer.cachedXp = newBalance;
            UpdateUI(player, newBalance);

            if (message != "" && amount != 0)
                ToastMessage.QueueMessage(player, F.Translate(amount >= 0 ? "gain_xp" : "loss_xp", player, Math.Abs(amount).ToString(Data.Locale)), message, ToastMessageSeverity.MINIXP);

            for (int i = 0; i < VehicleSigns.ActiveObjects.Count; i++)
                await VehicleSigns.ActiveObjects[i].InvokeUpdate(player.channel.owner);
                // update the color of the ranks on all the vehicle signs in case the player unlocked a new rank.
            for (int i = 0; i < Kits.RequestSigns.ActiveObjects.Count; i++)
                await Kits.RequestSigns.ActiveObjects[i].InvokeUpdate(player.channel.owner); 
                // update the color of the ranks on all the request signs in case the player unlocked a new rank.
            if (player.TryGetPlaytimeComponent(out Components.PlaytimeComponent c))
            {
                c.stats.AddXP(amount);
                c.UCPlayerStats.warfare_stats.AddXP(amount);
            }
        }
        public static void UpdateUI(Player nelsonplayer, int balance)
        {
            UCPlayer player = UCPlayer.FromPlayer(nelsonplayer);

            Rank rank = GetRank(balance, out int currentXP, out Rank nextRank);
            short key = unchecked((short)config.Data.RankUI);
            if (player.OfficerRank != null)
            {
                EffectManager.sendUIEffect(config.Data.RankUI, key, player.Player.channel.owner.transportConnection, true);
                EffectManager.sendUIEffectText(key, player.Player.channel.owner.transportConnection, true,
                    "Rank", player.OfficerRank.TranslateName(nelsonplayer.channel.owner.playerID.steamID.m_SteamID)
                );
                EffectManager.sendUIEffectText(key, player.Player.channel.owner.transportConnection, true,
                    "Level", "O" + player.OfficerRank.level
                );
                EffectManager.sendUIEffectText(key, player.Player.channel.owner.transportConnection, true,
                    "XP", nextRank != null ? currentXP + "/" + rank.XP : currentXP.ToString()
                );
                EffectManager.sendUIEffectText(key, player.Player.channel.owner.transportConnection, true,
                    "Next", nextRank != null ? "E" + nextRank.level + " equivalent" : ""
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
                    "Level", F.Translate("ui_xp_level", player, rank.level.ToString(Data.Locale))
                );
                EffectManager.sendUIEffectText(key, player.Player.channel.owner.transportConnection, true,
                    "XP", nextRank != null ? currentXP + "/" + rank.XP : currentXP.ToString()
                );
                EffectManager.sendUIEffectText(key, player.Player.channel.owner.transportConnection, true,
                    "Next", nextRank != null ? 
                    F.Translate("ui_xp_next_level", player, nextRank.TranslateName(nelsonplayer.channel.owner.playerID.steamID.m_SteamID), nextRank.level.ToString(Data.Locale)) : string.Empty
                );
                EffectManager.sendUIEffectText(key, player.Player.channel.owner.transportConnection, true,
                    "Progress", GetProgress(currentXP, rank.XP)
                );
            }
        }
        private static string GetProgress(int currentPoints, int totalPoints, int barLength = 50)
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
        public static Rank GetRankFromLevel(int level)
        {
            if (level <= 0) return null;
            if (config.Data.Ranks.Count > level - 1) return config.Data.Ranks[level - 1];
            return null;
        }
        public static Rank GetRank(int xpBalance, out int currentXP, out Rank nextRank)
        {
            int requiredXP = 0;
            nextRank = null;
            for (int i = 0; i < config.Data.Ranks.Count; i++)
            {
                requiredXP += config.Data.Ranks[i].XP;
                if (xpBalance < requiredXP)
                {
                    if (i + 1 < config.Data.Ranks.Count)
                        nextRank = config.Data.Ranks[i + 1];

                    currentXP = unchecked(config.Data.Ranks[i].XP - (requiredXP - xpBalance));
                    return config.Data.Ranks[i];
                }
            }
            currentXP = unchecked(xpBalance - requiredXP);
            return config.Data.Ranks.Last();
        }
    }
    public class Rank
    {
        [JsonSettable]
        public readonly int level;
        public readonly string name;
        public readonly Dictionary<string, string> name_translations;
        public readonly Dictionary<string, string> abbreviation_translations;
        [JsonSettable]
        public readonly string abbreviation;
        public readonly int XP;
        [JsonConstructor]
        public Rank(int level, string name, Dictionary<string, string> name_translations, string abbreviation, Dictionary<string, string> abbreviation_translations, int xp)
        {
            this.level = level;
            this.name = name;
            this.name_translations = name_translations;
            this.abbreviation = abbreviation;
            this.abbreviation_translations = abbreviation_translations;
            this.XP = xp;
        }
        public Rank(int level, string name, string abbreviation, int xp)
        {
            this.level = level;
            this.name = name;
            this.name_translations = new Dictionary<string, string>
            {
                { JSONMethods.DefaultLanguage, name }
            };
            this.abbreviation_translations = new Dictionary<string, string>
            {
                { JSONMethods.DefaultLanguage, abbreviation }
            };
            this.abbreviation = abbreviation;
            this.XP = xp;
        }
        public string TranslateName(ulong player)
        {
            if (player == 0)
            {
                if (name_translations.TryGetValue(JSONMethods.DefaultLanguage, out string newname))
                {
                    return newname;
                }
                else if (name_translations.Count > 0)
                {
                    return name_translations.ElementAt(0).Value;
                }
                else return name;
            }
            else
            {
                if (Data.Languages.TryGetValue(player, out string lang))
                {
                    if (!name_translations.TryGetValue(lang, out string newname))
                    {
                        if (name_translations.TryGetValue(JSONMethods.DefaultLanguage, out newname))
                        {
                            return newname;
                        }
                        else if (name_translations.Count > 0)
                        {
                            return name_translations.ElementAt(0).Value;
                        }
                        else return name;
                    } else
                    {
                        return newname;
                    }
                }
                else
                {
                    if (name_translations.TryGetValue(JSONMethods.DefaultLanguage, out string newname))
                    {
                        return newname;
                    }
                    else if (name_translations.Count > 0)
                    {
                        return name_translations.ElementAt(0).Value;
                    }
                    else return name;
                }
            }
        }
        public string TranslateAbbreviation(ulong player)
        {
            if (player == 0)
            {
                if (abbreviation_translations.TryGetValue(JSONMethods.DefaultLanguage, out string newname))
                {
                    return newname;
                }
                else if (abbreviation_translations.Count > 0)
                {
                    return abbreviation_translations.ElementAt(0).Value;
                }
                else return abbreviation;
            }
            else
            {
                if (Data.Languages.TryGetValue(player, out string lang))
                {
                    if (!abbreviation_translations.TryGetValue(lang, out string newname))
                    {
                        if (abbreviation_translations.TryGetValue(JSONMethods.DefaultLanguage, out newname))
                        {
                            return newname;
                        }
                        else if (abbreviation_translations.Count > 0)
                        {
                            return abbreviation_translations.ElementAt(0).Value;
                        }
                        else return abbreviation;
                    }
                    else
                    {
                        return newname;
                    }
                }
                else
                {
                    if (abbreviation_translations.TryGetValue(JSONMethods.DefaultLanguage, out string newname))
                    {
                        return newname;
                    }
                    else if (abbreviation_translations.Count > 0)
                    {
                        return abbreviation_translations.ElementAt(0).Value;
                    }
                    else return abbreviation;
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
        public int FOBDeployedXP;
        public int FlagCapturedXP;
        public int FlagAttackXP;
        public int FlagDefendXP;
        public int FlagNeutralizedXP;
        public int TransportPlayerXP;
        public float TimeBetweenXpAndOfpAwardForTransport;
        public int BuiltFOBXP;
        public int BuiltAmmoCrateXP;
        public int BuiltRepairStationXP;
        public int BuiltEmplacementXP;
        public int BuiltBarricadeXP;
        public Dictionary<EVehicleType, int> VehicleDestroyedXP;



        public float XPMultiplier;

        public ushort RankUI;

        public List<Rank> Ranks;

        public override void SetDefaults()
        {
            EnemyKilledXP = 10;
            FriendlyKilledXP = -50;
            FriendlyRevivedXP = 10;
            FOBKilledXP = 100;
            FOBDeployedXP = 10;
            FlagCapturedXP = 200;
            FlagAttackXP = 5;
            FlagDefendXP = 5;
            FlagNeutralizedXP = 50;
            TransportPlayerXP = 2;
            TimeBetweenXpAndOfpAwardForTransport = 10f;
            BuiltFOBXP = 50;
            BuiltAmmoCrateXP = 10;
            BuiltRepairStationXP = 25;
            BuiltEmplacementXP = 15;
            BuiltBarricadeXP = 5;

            VehicleDestroyedXP = new Dictionary<EVehicleType, int>()
            {
                {EVehicleType.HUMVEE, 50},
                {EVehicleType.TRANSPORT, 50},
                {EVehicleType.LOGISTICS, 80},
                {EVehicleType.SCOUT_CAR, 120},
                {EVehicleType.APC, 300},
                {EVehicleType.IFV, 400},
                {EVehicleType.MBT, 700},
                {EVehicleType.HELI_TRANSPORT, 200},
                {EVehicleType.EMPLACEMENT, 30},
            };

            XPMultiplier = 1;

            RankUI = 36031;

            Ranks = new List<Rank>()
            {
                new Rank(1, "Private", "Pvt.", 1000),
                new Rank(2, "Private 1st Class", "Pfc.", 2000),
                new Rank(3, "Corporal", "Cpl.", 2500),
                new Rank(4, "Specialist", "Spec.", 3000),
                new Rank(5, "Sergeant", "Sgt.", 5000),
                new Rank(6, "Staff Sergeant", "Ssg.", 7000),
                new Rank(7, "Sergeant 1st Class", "Sfc.", 9000),
                new Rank(8, "Sergeant Major", "S.M.", 12000),
                new Rank(9, "Warrant Officer", "W.O.", 16000),
                new Rank(10, "Chief Warrant Officer", "C.W.O.", 25000)
            };
        }
        public XPData() => SetDefaults();
    }
}

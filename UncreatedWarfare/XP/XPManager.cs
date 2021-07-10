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
                await AddXP(player.Player, player.GetTeam(), 0);
            }
        }
        public static async Task OnPlayerLeft(UCPlayer player)
        {
            await Task.Yield(); // just to remove the warning, feel free to remove, its basically an empty line.
        }
        public static async Task OnGroupChanged(SteamPlayer player, ulong oldGroup, ulong newGroup)
        {
            int xp = await GetXP(player.player, newGroup, true);
            SynchronizationContext rtn = await ThreadTool.SwitchToGameThread();
            UpdateUI(player.player, xp);
            await rtn;
        }
        public static async Task<int> GetXP(Player player, ulong team, bool important)
        {
            if (team == 0 || team > 2) return 0;
            UCPlayer ucplayer = UCPlayer.FromPlayer(player);
            if (important)
            {
                int newxp = await Data.DatabaseManager.GetXP(player.channel.owner.playerID.steamID.m_SteamID, team);
                if (ucplayer != null)
                    ucplayer.cachedXp = newxp;
                return newxp;
            } else
            {
                if (ucplayer == null)
                    return await GetXP(player, team, true);
                else return ucplayer.cachedXp;
            }
        }
        public static async Task<int> GetXP(ulong player, ulong team, bool important)
        {
            if (team == 0 || team > 2) return 0;
            UCPlayer ucplayer = UCPlayer.FromID(player);
            if (important)
            {
                int newxp = await Data.DatabaseManager.GetXP(player, team);
                if (ucplayer != null)
                    ucplayer.cachedXp = newxp;
                return newxp;
            }
            else
            {
                if (ucplayer == null)
                    return await GetXP(player, team, true);
                else return ucplayer.cachedXp;
            }
        }
        public static async Task AddXP(Player player, ulong team, int amount, string message = "")
        {
            int newBalance = await Data.DatabaseManager.AddXP(player.channel.owner.playerID.steamID.m_SteamID, team, (int)(amount * config.data.XPMultiplier));
            UCPlayer ucplayer = UCPlayer.FromPlayer(player);
            if (ucplayer != null)
                ucplayer.cachedXp = newBalance;
            SynchronizationContext rtn = await ThreadTool.SwitchToGameThread();
            UpdateUI(player, newBalance);

            if (message != "" && amount != 0)
                ToastMessage.QueueMessage(player, amount >= 0 ? ("+" + amount + " XP") : amount + " XP", message, ToastMessageSeverity.MINIXP);

            for (int i = 0; i < VehicleSigns.ActiveObjects.Count; i++)
                await VehicleSigns.ActiveObjects[i].InvokeUpdate(); // update the color of the ranks on all the signs in case the player unlocked a new rank.
            await rtn;
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
            if (player.OfficerRank != null)
            {
                EffectManager.sendUIEffect(config.data.RankUI, unchecked((short)config.data.RankUI), player.Player.channel.owner.transportConnection, true);
                EffectManager.sendUIEffectText(unchecked((short)config.data.RankUI), player.Player.channel.owner.transportConnection, true,
                    "Rank", player.OfficerRank.TranslateName(nelsonplayer.channel.owner.playerID.steamID.m_SteamID)
                );
                EffectManager.sendUIEffectText(unchecked((short)config.data.RankUI), player.Player.channel.owner.transportConnection, true,
                    "Level", "O" + player.OfficerRank.level
                );
                EffectManager.sendUIEffectText(unchecked((short)config.data.RankUI), player.Player.channel.owner.transportConnection, true,
                    "XP", nextRank != null ? currentXP + "/" + rank.XP : currentXP.ToString()
                );
                EffectManager.sendUIEffectText(unchecked((short)config.data.RankUI), player.Player.channel.owner.transportConnection, true,
                    "Next", nextRank != null ? "E" + nextRank.level + " equivalent" : ""
                );
                EffectManager.sendUIEffectText(unchecked((short)config.data.RankUI), player.Player.channel.owner.transportConnection, true,
                    "Progress", GetProgress(currentXP, rank.XP)
                );
            }
            else
            {
                EffectManager.sendUIEffect(config.data.RankUI, unchecked((short)config.data.RankUI), player.Player.channel.owner.transportConnection, true);
                EffectManager.sendUIEffectText(unchecked((short)config.data.RankUI), player.Player.channel.owner.transportConnection, true,
                    "Rank", rank.TranslateName(nelsonplayer.channel.owner.playerID.steamID.m_SteamID)
                );
                EffectManager.sendUIEffectText(unchecked((short)config.data.RankUI), player.Player.channel.owner.transportConnection, true,
                    "Level", "E" + rank.level
                );
                EffectManager.sendUIEffectText(unchecked((short)config.data.RankUI), player.Player.channel.owner.transportConnection, true,
                    "XP", nextRank != null ? currentXP + "/" + rank.XP : currentXP.ToString()
                );
                EffectManager.sendUIEffectText(unchecked((short)config.data.RankUI), player.Player.channel.owner.transportConnection, true,
                    "Next", nextRank != null ? nextRank.TranslateName(nelsonplayer.channel.owner.playerID.steamID.m_SteamID) + "  E" + nextRank.level : ""
                );
                EffectManager.sendUIEffectText(unchecked((short)config.data.RankUI), player.Player.channel.owner.transportConnection, true,
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
                bars.Append('█');
            }
            return bars.ToString();
        }
        public static Rank GetRankFromLevel(int level)
        {
            if (level == 0) return null;
            if (config.data.Ranks.Count > level - 1) return config.data.Ranks[unchecked(level - 1)];
            return null;
        }
        public static Rank GetRank(int xpBalance, out int currentXP, out Rank nextRank)
        {
            int requiredXP = 0;
            nextRank = null;
            for (int i = 0; i < config.data.Ranks.Count; i++)
            {
                requiredXP += config.data.Ranks[i].XP;
                if (xpBalance < requiredXP)
                {
                    if (i + 1 < config.data.Ranks.Count)
                        nextRank = config.data.Ranks[i + 1];

                    currentXP = unchecked(config.data.Ranks[i].XP - (requiredXP - xpBalance));
                    return config.data.Ranks[i];
                }
            }
            currentXP = unchecked(xpBalance - requiredXP);
            return config.data.Ranks.Last();
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
                if (Data.Languages.ContainsKey(player))
                {
                    string lang = Data.Languages[player];
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
                if (Data.Languages.ContainsKey(player))
                {
                    string lang = Data.Languages[player];
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
        public int FlagCapturedXP;
        public int FlagCapIncreasedXP;
        public int FlagNeutralizedXP;
        public int TransportPlayerXP;
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
            FlagCapturedXP = 200;
            FlagCapIncreasedXP = 1;
            FlagNeutralizedXP = 50;
            TransportPlayerXP = 1;
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

            RankUI = 32365;

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
        public XPData() { }
    }
}

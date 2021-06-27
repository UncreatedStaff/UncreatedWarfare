using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Warfare.Officers;
using Uncreated.Warfare.Teams;
using Flag = Uncreated.Warfare.Flags.Flag;

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
            uint xp = await GetXP(player.player, newGroup);
            SynchronizationContext rtn = await ThreadTool.SwitchToGameThread();
            UpdateUI(player.player, xp);
            await rtn;
        }
        public static async Task OnEnemyKilled(UCWarfare.KillEventArgs parameters)
        {
            await AddXP(parameters.killer, parameters.killer.GetTeam(), config.data.EnemyKilledXP);
        }
        public static async Task OnFriendlyKilled(UCWarfare.KillEventArgs parameters)
        {
            await AddXP(parameters.killer, parameters.killer.GetTeam(), config.data.FriendlyKilledXP);
        }
        public static async Task OnFlagCaptured(Flag flag, ulong capturedTeam, ulong lostTeam)
        {
            foreach (Player player in flag.PlayersOnFlagTeam1.Where(p => TeamManager.IsFriendly(p, capturedTeam)))
            {
                await AddXP(player, capturedTeam, config.data.FlagCapturedXP);
            }
        }
        public static async Task OnFlagNeutralized(Flag flag, ulong capturedTeam, ulong lostTeam)
        {
            foreach (Player player in flag.PlayersOnFlagTeam1.Where(p => TeamManager.IsFriendly(p, capturedTeam)))
            {
                await AddXP(player, capturedTeam, config.data.FlagNeutralizedXP);
            }
        }

        public static async Task<uint> GetXP(Player player, ulong team) => await Data.DatabaseManager.GetXP(player.channel.owner.playerID.steamID.m_SteamID, team);
        public static async Task AddXP(Player player, ulong team, int amount)
        {
            uint newBalance = await Data.DatabaseManager.AddXP(player.channel.owner.playerID.steamID.m_SteamID, team, (int)(amount * config.data.XPMultiplier));
            SynchronizationContext rtn = await ThreadTool.SwitchToGameThread();
            UpdateUI(player, newBalance);
            await rtn;
        }
        public static void UpdateUI(Player nelsonplayer, uint balance)
        {
            UCPlayer player = UCPlayer.FromPlayer(nelsonplayer);

            var rank = GetRank(balance, out var currentXP, out var nextRank);
            if (player.OfficerRank != null)
            {
                EffectManager.sendUIEffect(config.data.RankUI, (short)config.data.RankUI, player.Player.channel.owner.transportConnection, true,
                    player.OfficerRank.name,
                    nextRank != null ? currentXP + "/" + rank.level : currentXP.ToString(),
                    "",
                    GetProgress(currentXP, rank.XP)
               );
            }
            else
            {
                EffectManager.sendUIEffect(config.data.RankUI, (short)config.data.RankUI, player.Player.channel.owner.transportConnection, true,
                    rank.name,
                    nextRank != null ? currentXP + "/" + rank.XP : currentXP.ToString(),
                    nextRank != null ? nextRank.name : "",
                    GetProgress(currentXP, rank.XP)
               );
            }
        }
        private static string GetProgress(uint currentPoints, uint totalPoints, uint barLength = 50)
        {
            float ratio = currentPoints / (float)totalPoints;

            int progress = (int)Math.Round(ratio * barLength);

            string bars = "";
            for (int i = 0; i < progress; i++)
            {
                bars += "█";
            }
            return bars;
        }
        public static Rank GetRank(uint xpBalance, out uint currentXP, out Rank nextRank)
        {
            uint requiredXP = 0;

            CommandWindow.Log("balance: " + xpBalance);

            Rank rank = config.data.Ranks.Last();
            nextRank = null;
            for (int i = 0; i < config.data.Ranks.Count; i++)
            {
                requiredXP += config.data.Ranks[i].XP;

                CommandWindow.Log("required XP: " + requiredXP);

                if (xpBalance < requiredXP)
                {
                    if (i + 1 < config.data.Ranks.Count)
                        nextRank = config.data.Ranks[i + 1];

                    currentXP = unchecked((uint)(config.data.Ranks[i].XP - (requiredXP - xpBalance)));
                    return config.data.Ranks[i];
                }
            }
            currentXP = unchecked((uint)(xpBalance - requiredXP));
            return rank;
        }
    }
    public class Rank
    {
        public readonly uint level;
        public readonly string name;
        public readonly string abbreviation;
        public readonly uint XP;
        public Rank(uint level, string name, string abbreviation, uint xp)
        {
            this.level = level;
            this.name = name;
            this.abbreviation = abbreviation;
            XP = xp;
        }
    }

    public class XPData : ConfigData
    {
        public int EnemyKilledXP;
        public int FriendlyKilledXP;
        public int FOBKilledXP;
        public int FlagCapturedXP;
        public int FlagCapIncreasedXP;
        public int FlagNeutralizedXP;
        public float XPMultiplier;

        public ushort RankUI;

        public List<Rank> Ranks;

        public override void SetDefaults()
        {
            EnemyKilledXP = 10;
            FriendlyKilledXP = -50;
            FOBKilledXP = 50;
            FlagCapturedXP = 100;
            FlagCapIncreasedXP = 1;
            FlagNeutralizedXP = 30;
            XPMultiplier = 1;

            RankUI = 32365;

            Ranks = new List<Rank>();
            Ranks.Add(new Rank(1, "Private", "Pvt.", 1000));
            Ranks.Add(new Rank(2, "Private 1st Class", "Pfc.", 2000));
            Ranks.Add(new Rank(3, "Corporal", "Cpl.", 2500));
            Ranks.Add(new Rank(4, "Specialist", "Spec.", 3000));
            Ranks.Add(new Rank(5, "Sergeant", "Sgt.", 5000));
            Ranks.Add(new Rank(6, "Staff Sergeant", "Ssg.", 7000));
            Ranks.Add(new Rank(7, "Sergeant 1st Class", "Sfc.", 7000));
            Ranks.Add(new Rank(8, "Sergeant Major", "S.M.", 10000));
            Ranks.Add(new Rank(9, "Warrant Officer", "W.O.", 13000));
            Ranks.Add(new Rank(10, "Chief Warrant Officer", "C.W.O.", 0));
        }
        public XPData() { }
    }
}

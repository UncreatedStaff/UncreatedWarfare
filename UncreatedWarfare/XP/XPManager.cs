using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flag = Uncreated.Warfare.Flags.Flag;

namespace Uncreated.Warfare.XP
{
    public class XPManager
    {
        public static Config<XPData> config;

        public XPManager()
        {
            config = new Config<XPData>(Data.TeamStorage, "config.json");
        }

        public static void OnEnemyKilled(UCWarfare.KillEventArgs parameters)
        {
            AddXP(parameters.killer.channel.owner.playerID.steamID, parameters.killer.GetTeam(), config.data.EnemyKilledXP);
        }
        public static void OnFriendlyKilled(UCWarfare.KillEventArgs parameters)
        {
            AddXP(parameters.killer.channel.owner.playerID.steamID, parameters.killer.GetTeam(), config.data.FriendlyKilledXP);
        }
        public static void OnFlagCaptured(Flag flag, ulong capturedTeam, ulong lostTeam)
        {
            
        }
        public static void OnFlagNeutralized(Flag flag, ulong capturedTeam, ulong lostTeam)
        {

        }

        public static uint GetXP(CSteamID playerID, ulong team) => Data.DatabaseManager.GetXPSync(playerID.m_SteamID, (byte)team);
        public static void AddXP(CSteamID playerID, ulong team, int amount) => Data.DatabaseManager.AddXP(playerID.m_SteamID, (byte)team, amount);
    }

    public class XPData : ConfigData
    {
        public int EnemyKilledXP;
        public int FriendlyKilledXP;
        public int FOBKilledXP;
        public int FlagCapturedXP;  
        public int FlagCapIncreasedXP;

        public ushort RankUI;

        public override void SetDefaults()
        {
            EnemyKilledXP = 100;
            FriendlyKilledXP = -100;
            FOBKilledXP = 500;
            FlagCapturedXP = 800;
            FlagCapIncreasedXP = 1;

            RankUI = 32365;
    }

        public XPData() { }
    }
}

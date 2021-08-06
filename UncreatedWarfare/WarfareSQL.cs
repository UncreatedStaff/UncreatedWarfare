﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.SQL;
using Uncreated.Players;
using SDG.Unturned;
using Steamworks;
using Rocket.Core.Steam;
using Uncreated.Warfare.Stats;
using Google.Protobuf.WellKnownTypes;
using System.Reflection;
using static SDG.Unturned.SleekBlueprint;
using System.Drawing;

namespace Uncreated.Warfare
{
    public class WarfareSQL : MySqlDatabase
    {
        const string DEFAULT_GATEWAY_BEGINNING = "192.168.1.";
        const string LOCAL_IP = "127.0.0.1";
        const string TIME_FORMAT_SQL = "{0:yyyy-MM-dd HH:mm:ss}";
        public WarfareSQL(MySqlData data) : base(data) 
        {
            DebugLogging |= UCWarfare.Config.Debug;
        }
        public async Task<FPlayerName> GetUsernames(ulong Steam64)
        {
            FPlayerName? name = null;
            await Query(
                $"SELECT `PlayerName`, `CharacterName`, `NickName` " +
                $"FROM `usernames` " +
                $"WHERE `Steam64` = @0 LIMIT 1;",
                new object[] { Steam64 },
                (R) =>
                {
                    name = new FPlayerName() { Steam64 = Steam64, PlayerName = R.GetString(0), CharacterName = R.GetString(1), NickName = R.GetString(2) };
                });
            if (name.HasValue)
                return name.Value;
            string tname = Steam64.ToString(Data.Locale);
            return new FPlayerName() { Steam64 = Steam64, PlayerName = tname, CharacterName = tname, NickName = tname };
        }
        public async Task CheckUpdateUsernames(FPlayerName player)
        {
            FPlayerName oldNames = await GetUsernames(player.Steam64);
            bool updatePlayerName = false;
            bool updateCharacterName = false;
            bool updateNickName = false;
            if (oldNames.Steam64.ToString(Data.Locale) == oldNames.PlayerName)
            {
                updatePlayerName = true;
                updateCharacterName = true;
                updateNickName = true;
            }
            else
            {
                if (player.PlayerName != oldNames.PlayerName) updatePlayerName = true;
                if (player.CharacterName != oldNames.CharacterName) updateCharacterName = true;
                if (player.NickName != oldNames.NickName) updateNickName = true;
            }
            if (!updatePlayerName && !updateCharacterName && !updateNickName) return;
            object[] parameters = new object[] { player.Steam64, player.PlayerName, player.CharacterName, player.NickName };
            List<string> valueNames = new List<string>();
            if (updatePlayerName)
            {
                valueNames.Add("PlayerName");
            }
            if (updateCharacterName)
            {
                valueNames.Add("CharacterName");
            }
            if (updateNickName)
            {
                valueNames.Add("NickName");
            }
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < valueNames.Count; i++)
            {
                if (i != 0) sb.Append(", ");
                sb.Append('`').Append(valueNames[i]).Append("` = VALUES(`").Append(valueNames[i]).Append("`)");
            }
            string updates = sb.ToString();
            await NonQuery(
                $"INSERT INTO `usernames` " +
                $"(`Steam64`, `PlayerName`, `CharacterName`, `NickName`) VALUES(@0, @1, @2, @3) " +
                $"ON DUPLICATE KEY UPDATE " +
                updates + 
                $";", 
                parameters);
        }
        public async Task<int> GetXP(ulong Steam64, ulong Team)
        {
            int xp = 0;
            await Query(
                "SELECT `XP` " +
                "FROM `levels` " +
                "WHERE `Steam64` = @0 " +
                "AND `Team` = @1 LIMIT 1;", 
                new object[] { Steam64, Team },
                (R) =>
                {
                    xp = R.GetInt32(0);
                });
            return xp;
        }
        public async Task<int> GetOfficerPoints(ulong Steam64, ulong Team)
        {
            int officer_points = 0;
            await Query(
                $"SELECT `OfficerPoints` " +
                $"FROM `levels` " +
                $"WHERE `Steam64` = @0 " +
                $"AND `Team` = @1 LIMIT 1;",
                new object[] { Steam64, Team },
                (R) =>
                {
                    officer_points = R.GetInt32(0);
                });
            return officer_points;
        }
        public async Task<uint> GetKills(ulong Steam64, ulong Team)
        {
            uint kills = 0;
            MySqlTableLang table = GetTable("");
            await Query(
                "SELECT `Kills` " +
                "FROM `playerstats` " +
                "WHERE `Steam64` = @0 " +
                "AND `Team` = @1 LIMIT 1;",
                new object[] { Steam64, Team },
                (R) =>
                {
                    kills = R.GetUInt32(0);
                });
            return kills;
        }
        public async Task<uint> GetDeaths(ulong Steam64, ulong Team)
        {
            uint deaths = 0;
            await Query(
                $"SELECT `Deaths` " +
                $"FROM `playerstats` " +
                $"WHERE `Steam64` = @0 " +
                $"AND `Team` = @1 LIMIT 1;",
                new object[] { Steam64, Team },
                (R) =>
                {
                    deaths = R.GetUInt32(0);
                });
            return deaths;
        }
        public async Task<uint> GetTeamkills(ulong Steam64, ulong Team)
        {
            uint teamkills = 0;
            await Query(
                "SELECT `Teamkills` " +
                "FROM `playerstats` " +
                "WHERE `Steam64` = @0 " +
                "AND `Team` = @1 LIMIT 1;",
                new object[] { Steam64, Team },
                (R) =>
                {
                    teamkills = R.GetUInt32(0);
                });
            return teamkills;
        }
        /// <returns>New XP Value</returns>
        public async Task<int> AddXP(ulong Steam64, ulong Team, int amount)
        {
            int oldBalance = await GetXP(Steam64, Team);
            if (amount == 0) return oldBalance;
            if (amount > 0)
            {
                await NonQuery(
                    "INSERT INTO `levels` " +
                    "(`Steam64`, `Team`, `XP`, `OfficerPoints`) " +
                    "VALUES(@0, @1, @2, '0') " +
                    "ON DUPLICATE KEY UPDATE " +
                    "`XP` = `XP` + VALUES(`XP`);", 
                    new object[] { Steam64, Team, amount });
                return unchecked(oldBalance + amount);
            } else
            {
                int absamount = Math.Abs(amount);
                if (absamount >= oldBalance)
                {
                    await NonQuery(
                        "INSERT INTO `levels` " +
                        "(`Steam64`, `Team`, `XP`, `OfficerPoints`) " +
                        "VALUES(@0, @1, '0', '0') " +
                        "ON DUPLICATE KEY UPDATE " +
                        "`XP` = 0;", // clamp to 0
                        new object[] { Steam64, Team });
                    return 0;
                } else
                {
                    await NonQuery(
                        "UPDATE `levels` SET " +
                        "`XP` = `XP` - @2 " +
                        "WHERE `Steam64` = @0 AND `Team` = @1;",
                        new object[] { Steam64, Team, absamount });
                    return unchecked(oldBalance - absamount);
                }
            }
        }
        /// <returns>New Officer Points Value</returns>
        public async Task<int> AddOfficerPoints(ulong Steam64, ulong Team, int amount)
        {
            int oldBalance = await GetOfficerPoints(Steam64, Team);

            if (amount == 0) return oldBalance;
            if (amount > 0)
            {
                await NonQuery(
                    "INSERT INTO `levels` " +
                    "(`Steam64`, `Team`, `XP`, `OfficerPoints`) " +
                    "VALUES(@0, @1, '0', @2) " +
                    "ON DUPLICATE KEY UPDATE " +
                    "`OfficerPoints` = `OfficerPoints` + VALUES(`OfficerPoints`);",
                    new object[] { Steam64, Team, amount });
                return oldBalance + amount;
            }
            else
            {
                if (amount >= oldBalance)
                {
                    await NonQuery(
                        "INSERT INTO `levels` " +
                        "(`Steam64`, `Team`, `XP`, `OfficerPoints`) " +
                        "VALUES(@0, @1, '0', '0') " +
                        "ON DUPLICATE KEY UPDATE " +
                        "`XP` = 0;", // clamp to 0
                        new object[] { Steam64, Team });
                    return 0;
                }
                else
                {
                    await NonQuery(
                        "UPDATE `levels` SET " +
                        "`OfficerPoints` = `OfficerPoints` - @2 " +
                        "WHERE `Steam64` = @0 AND `Team` = @1;",
                        new object[] { Steam64, Team, Math.Abs(amount) });
                    return oldBalance - amount;
                }
            }
        }
        public async Task AddKill(ulong Steam64, ulong Team, int amount = 1)
        {
            if (amount == 0) return;
            if (amount > 0)
            {
                await NonQuery(
                    $"INSERT INTO `playerstats` " +
                    $"(`Steam64`, `Team`, `Kills`, `Deaths`, `Teamkills`) " +
                    $"VALUES(@0, @1, @2, '0', '0') " +
                    $"ON DUPLICATE KEY UPDATE " +
                    $"`Kills` = `Kills` + VALUES(`Kills`);",
                    new object[] { Steam64, Team, amount });
            }
            else
            {
                uint oldkills = await GetKills(Steam64, Team);
                if (amount >= oldkills)
                {
                    await NonQuery(
                        $"INSERT INTO `playerstats` " +
                        $"(`Steam64`, `Team`, `Kills`, `Deaths`, `Teamkills`) " +
                        $"VALUES(@0, @1, '0', '0', '0') " +
                        $"ON DUPLICATE KEY UPDATE " +
                        $"`Kills` = 0;", // clamp to 0
                        new object[] { Steam64, Team });
                }
                else
                {
                    await NonQuery(
                        $"UPDATE `playerstats` SET " +
                        $"`Kills` = `Kills` - @2 " +
                        $"WHERE `Steam64` = @0 AND `Team` = @1;",
                        new object[] { Steam64, Team, Math.Abs(amount) });
                }
            }
        }
        public async Task AddDeath(ulong Steam64, ulong Team, int amount = 1)
        {
            MySqlTableLang table = GetTable("playerstats");
            string s64 = table.GetColumnName("Steam64");
            string team = table.GetColumnName("Team");
            string kills = table.GetColumnName("Kills");
            string deaths = table.GetColumnName("Deaths");
            string teamkills = table.GetColumnName("Teamkills");
            if (amount == 0) return;
            if (amount > 0)
            {
                await NonQuery(
                    $"INSERT INTO `{table.TableName}` " +
                    $"(`{s64}`, `{team}`, `{kills}`, `{deaths}`, `{teamkills}`) " +
                    $"VALUES(@0, @1, '0', @2, '0') " +
                    $"ON DUPLICATE KEY UPDATE " +
                    $"`{deaths}` = `{deaths}` + VALUES(`{deaths}`);",
                    new object[] { Steam64, Team, amount });
            }
            else
            {
                uint oldDeaths = await GetDeaths(Steam64, Team);
                if (amount >= oldDeaths)
                {
                    await NonQuery(
                        $"INSERT INTO `{table.TableName}` " +
                        $"(`{s64}`, `{team}`, `{kills}`, `{deaths}`, `{teamkills}`) " +
                        $"VALUES(@0, @1, '0', '0', '0') " +
                        $"ON DUPLICATE KEY UPDATE " +
                        $"`{deaths}` = 0;", // clamp to 0
                        new object[] { Steam64, Team });
                }
                else
                {
                    await NonQuery(
                        $"UPDATE `{table.TableName}` SET " +
                        $"`{deaths}` = `{deaths}` - @2 " +
                        $"WHERE `{s64}` = @0 AND `{team}` = @1;",
                        new object[] { Steam64, Team, Math.Abs(amount) });
                }
            }
        }
        public async Task AddTeamkill(ulong Steam64, ulong Team, int amount = 1)
        {
            if (amount == 0) return;
            if (amount > 0)
            {
                await NonQuery(
                    $"INSERT INTO `playerstats` " +
                    $"(`Steam64`, `Team`, `Kills`, `Deaths`, `Teamkills`) " +
                    $"VALUES(@0, @1, '0', '0', @2) " +
                    $"ON DUPLICATE KEY UPDATE " +
                    $"`Teamkills` = `Teamkills` + VALUES(`Teamkills`);",
                    new object[] { Steam64, Team, amount });
            }
            else
            {
                uint oldTeamkills = await GetTeamkills(Steam64, Team);
                if (amount >= oldTeamkills)
                {
                    await NonQuery(
                        $"INSERT INTO `playerstats` " +
                        $"(`Steam64`, `Team`, `Kills`, `Deaths`, `Teamkills`) " +
                        $"VALUES(@0, @1, '0', '0', '0') " +
                        $"ON DUPLICATE KEY UPDATE " +
                        $"`Teamkills` = 0;", // clamp to 0
                        new object[] { Steam64, Team });
                }
                else
                {
                    await NonQuery(
                        $"UPDATE `playerstats` SET " +
                        $"`Teamkills` = `Teamkills` - @2 " +
                        $"WHERE `Steam64` = @0 AND `Team` = @1;",
                        new object[] { Steam64, Team, Math.Abs(amount) });
                }
            }
        }
        public async Task AddBan(ulong Banned, ulong Banner, uint Duration, string Reason)
            => await NonQuery(
                "INSERT INTO `bans` " +
                "(`Banned`, `Banner`, `Duration`, `Reason`, `Timestamp`) " +
                "VALUES(@0, @1, @2, @3, @4);",
                new object[] { Banned, Banner, Duration, Reason, DateTime.Now.ToString(TIME_FORMAT_SQL) });
        public async Task AddBan(ulong Banned, ulong Banner, uint Duration, string Reason, DateTime time)
            => await NonQuery(
                "INSERT INTO `bans` " +
                "(`Banned`, `Banner`, `Duration`, `Reason`, `Timestamp`) " +
                "VALUES(@0, @1, @2, @3, @4);",
                new object[] { Banned, Banner, Duration, Reason, time.ToString(TIME_FORMAT_SQL) });
        public async Task AddKick(ulong Kicked, ulong Kicker, string Reason)
            => await NonQuery(
                "INSERT INTO `kicks` " +
                "(`Kicked`, `Kicker`, `Reason`, `Timestamp`) " +
                "VALUES(@0, @1, @2, @3);",
                new object[] { Kicked, Kicker, Reason, DateTime.Now.ToString(TIME_FORMAT_SQL) });
        public async Task AddWarning(ulong Warned, ulong Warner, string Reason)
            => await NonQuery(
                "INSERT INTO `warnings` " +
                "(`Warned`, `Warner`, `Reason`, `Timestamp`) " +
                "VALUES(@0, @1, @2, @3);",
                new object[] { Warned, Warner, Reason, DateTime.Now.ToString(TIME_FORMAT_SQL) });
        public async Task AddBattleyeKick(ulong Kicked, string Reason)
            => await NonQuery(
                "INSERT INTO `battleye_kicks` " +
                "(`Kicked`, `Reason`, `Timestamp`) " +
                "VALUES(@0, @1, @2);",
                new object[] { Kicked, Reason, DateTime.Now.ToString(TIME_FORMAT_SQL) });
        public async Task AddTeamkill(ulong Teamkiller, ulong Teamkilled, string Cause, string ItemName = "", ushort Item = 0, float Distance = 0f)
            => await NonQuery(
                "INSERT INTO `teamkills` " +
                "(`Teamkiller`, `Teamkilled`, `Cause`, `Item`, `ItemID`, `Distance`, `Timestamp`) " +
                "VALUES(@0, @1, @2, @3, @4, @5, @6);",
                new object[] { Teamkiller, Teamkilled, Cause, ItemName, Item, Distance, DateTime.Now.ToString(TIME_FORMAT_SQL) });
        public MySqlTableLang GetTable(string key)
        {
            if (Data.TableData.TryGetValue(key, out MySqlTableLang lang))
                return lang;
            else return new MySqlTableLang(key, new Dictionary<string, string>());
        }
        public async Task<bool> HasPlayerJoined(ulong Steam64)
        {
            MySqlTableLang table = GetTable("logindata");
            string s64 = table.GetColumnName("Steam64");
            int amt = await Scalar(
                $"SELECT COUNT(*) " +
                $"FROM `{table}` " +
                $"WHERE `{s64}` = @0;",
                new object[1] { Steam64 },
                o => Convert.ToInt32(o));
            return amt > 0;
        }
        public async Task RegisterLogin(Player player)
        {
            MySqlTableLang table = GetTable("logindata");
            string s64 = table.GetColumnName("Steam64");
            string ip = table.GetColumnName("IP");
            string lastentry = table.GetColumnName("LastLoggedIn");
            string ipaddress;
            if (player.channel.owner.getIPv4Address(out uint ipnum))
            {
                ipaddress = Parser.getIPFromUInt32(ipnum);
                if (ipaddress == LOCAL_IP || ipaddress.StartsWith(DEFAULT_GATEWAY_BEGINNING))
                    if (SteamGameServer.GetPublicIP().TryGetIPv4Address(out ipnum))
                        ipaddress = Parser.getIPFromUInt32(ipnum);
            }
            else if (SteamGameServer.GetPublicIP().TryGetIPv4Address(out ipnum))
                ipaddress = Parser.getIPFromUInt32(ipnum);
            else ipaddress = LOCAL_IP;
            await NonQuery(
                $"INSERT INTO `{table.TableName}` " +
                $"(`{s64}`, `{ip}`, `{lastentry}`) " +
                $"VALUES(@0, @1, @2) " +
                $"ON DUPLICATE KEY UPDATE " +
                $"`{ip}` = VALUES(`{ip}`), `{lastentry}` = VALUES(`{lastentry}`);",
                new object[3] { player.channel.owner.playerID.steamID.m_SteamID, ipaddress, string.Format(TIME_FORMAT_SQL, DateTime.Now) });
        }
        public override void Log(string message, ConsoleColor color = ConsoleColor.Gray)
            => F.Log(message, color);
        public override void LogWarning(string message, ConsoleColor color = ConsoleColor.Yellow)
            => F.LogWarning(message, color);
        public override void LogError(string message, ConsoleColor color = ConsoleColor.Red)
            => F.LogError(message, color);
        public override void LogError(Exception ex, ConsoleColor color = ConsoleColor.Red)
            => F.LogError(ex, color);
    }
}

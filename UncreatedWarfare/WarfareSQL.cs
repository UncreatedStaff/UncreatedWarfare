using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.SQL;
using Uncreated.Players;

namespace Uncreated.Warfare
{
    public class WarfareSqlTest : MySqlDatabase
    {
        public WarfareSqlTest(MySqlData data) : base(data) 
        {
            DebugLogging |= UCWarfare.Config.Debug;
        }
        public async Task<FPlayerName> GetUsernames(ulong Steam64)
        {
            FPlayerName? name = null;
            MySqlTableLang table = GetTable("usernames");
            string pn = table.GetColumnName("PlayerName");
            string cn = table.GetColumnName("CharacterName");
            string nn = table.GetColumnName("NickName");
            await Query(
                $"SELECT `{pn}`, `{cn}`, `{nn}` " +
                $"FROM `{table.TableName}` " +
                $"WHERE `{table.GetColumnName("Steam64")}` = @0 LIMIT 1;",
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
        public async Task<uint> GetXP(ulong Steam64, ulong Team)
        {
            uint xp = 0;
            MySqlTableLang table = GetTable("levels");
            await Query(
                $"SELECT `{table.GetColumnName("XP")}` " +
                $"FROM `{table.TableName}` " +
                $"WHERE `{table.GetColumnName("Steam64")}` = @0 " +
                $"AND `{table.GetColumnName("Team")}` = @1 LIMIT 1;", 
                new object[] { Steam64, Team },
                (R) =>
                {
                    xp = R.GetUInt32(0);
                });
            return xp;
        }
        public async Task<uint> GetOfficerPoints(ulong Steam64, ulong Team)
        {
            uint officer_points = 0;
            MySqlTableLang table = GetTable("levels");
            await Query(
                $"SELECT `{table.GetColumnName("OfficerPoints")}` " +
                $"FROM `{table.TableName}` " +
                $"WHERE `{table.GetColumnName("Steam64")}` = @0 " +
                $"AND `{table.GetColumnName("Team")}` = @1 LIMIT 1;",
                new object[] { Steam64, Team },
                (R) =>
                {
                    officer_points = R.GetUInt32(0);
                });
            return officer_points;
        }
        public async Task<uint> GetKills(ulong Steam64, ulong Team)
        {
            uint kills = 0;
            MySqlTableLang table = GetTable("playerstats");
            await Query(
                $"SELECT `{table.GetColumnName("Kills")}` " +
                $"FROM `{table.TableName}` " +
                $"WHERE `{table.GetColumnName("Steam64")}` = @0 " +
                $"AND `{table.GetColumnName("Team")}` = @1 LIMIT 1;",
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
            MySqlTableLang table = GetTable("playerstats");
            await Query(
                $"SELECT `{table.GetColumnName("Deaths")}` " +
                $"FROM `{table.TableName}` " +
                $"WHERE `{table.GetColumnName("Steam64")}` = @0 " +
                $"AND `{table.GetColumnName("Team")}` = @1 LIMIT 1;",
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
            MySqlTableLang table = GetTable("playerstats");
            await Query(
                $"SELECT `{table.GetColumnName("Teamkills")}` " +
                $"FROM `{table.TableName}` " +
                $"WHERE `{table.GetColumnName("Steam64")}` = @0 " +
                $"AND `{table.GetColumnName("Team")}` = @1 LIMIT 1;",
                new object[] { Steam64, Team },
                (R) =>
                {
                    teamkills = R.GetUInt32(0);
                });
            return teamkills;
        }
        /// <returns>New XP Value</returns>
        public async Task<uint> AddXP(ulong Steam64, ulong Team, int amount)
        {
            MySqlTableLang table = GetTable("levels");
            string s64 = table.GetColumnName("Steam64");
            string team = table.GetColumnName("Team");
            string xp = table.GetColumnName("XP");
            string op = table.GetColumnName("OfficerPoints");
            uint oldBalance = await GetXP(Steam64, Team);
            if (amount == 0) return oldBalance;
            if (amount > 0)
            {
                await NonQuery(
                    $"INSERT INTO `{table.TableName}` " +
                    $"(`{s64}`, `{team}`, `{xp}`, `{op}`) " +
                    $"VALUES(@0, @1, @2, '0') " +
                    $"ON DUPLICATE KEY UPDATE " +
                    $"`{xp}` = `{xp}` + VALUES(`{xp}`);", 
                    new object[] { Steam64, Team, amount });
                return unchecked((uint)(oldBalance + amount));
            } else
            {
                if (amount >= oldBalance)
                {
                    await NonQuery(
                        $"INSERT INTO `{table.TableName}` " +
                        $"(`{s64}`, `{team}`, `{xp}`, `{op}`) " +
                        $"VALUES(@0, @1, '0', '0') " +
                        $"ON DUPLICATE KEY UPDATE " +
                        $"`{xp}` = 0;", // clamp to 0
                        new object[] { Steam64, Team });
                    return 0;
                } else
                {
                    await NonQuery(
                        $"UPDATE `{table.TableName}` SET " +
                        $"`{xp}` = `{xp}` - @2 " +
                        $"WHERE `{s64}` = @0 AND `{team}` = @1;",
                        new object[] { Steam64, Team, Math.Abs(amount) });
                    return unchecked((uint)(oldBalance - amount));
                }
            }
        }
        /// <returns>New Officer Points Value</returns>
        public async Task<uint> AddOfficerPoints(ulong Steam64, ulong Team, int amount)
        {
            MySqlTableLang table = GetTable("levels");
            string s64 = table.GetColumnName("Steam64");
            string team = table.GetColumnName("Team");
            string xp = table.GetColumnName("XP");
            string op = table.GetColumnName("OfficerPoints");
            uint oldBalance = await GetXP(Steam64, Team);
            if (amount == 0) return oldBalance;
            if (amount > 0)
            {
                await NonQuery(
                    $"INSERT INTO `{table.TableName}` " +
                    $"(`{s64}`, `{team}`, `{xp}`, `{op}`) " +
                    $"VALUES(@0, @1, '0', @2) " +
                    $"ON DUPLICATE KEY UPDATE " +
                    $"`{op}` = `{op}` + VALUES(`{op}`);",
                    new object[] { Steam64, Team, amount });
                return unchecked((uint)(oldBalance + amount));
            }
            else
            {
                if (amount >= oldBalance)
                {
                    await NonQuery(
                        $"INSERT INTO `{table.TableName}` " +
                        $"(`{s64}`, `{team}`, `{xp}`, `{op}`) " +
                        $"VALUES(@0, @1, '0', '0') " +
                        $"ON DUPLICATE KEY UPDATE " +
                        $"`{xp}` = 0;", // clamp to 0
                        new object[] { Steam64, Team });
                    return 0;
                }
                else
                {
                    await NonQuery(
                        $"UPDATE `{table.TableName}` SET " +
                        $"`{op}` = `{op}` - @2 " +
                        $"WHERE `{s64}` = @0 AND `{team}` = @1;",
                        new object[] { Steam64, Team, Math.Abs(amount) });
                    return unchecked((uint)(oldBalance - amount));
                }
            }
        }
        public async Task AddKill(ulong Steam64, ulong Team, int amount = 0)
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
                    $"VALUES(@0, @1, @2, '0', '0') " +
                    $"ON DUPLICATE KEY UPDATE " +
                    $"`{kills}` = `{kills}` + VALUES(`{kills}`);",
                    new object[] { Steam64, Team, amount });
            }
            else
            {
                uint oldkills = await GetKills(Steam64, Team);
                if (amount >= oldkills)
                {
                    await NonQuery(
                        $"INSERT INTO `{table.TableName}` " +
                        $"(`{s64}`, `{team}`, `{kills}`, `{deaths}`, `{teamkills}`) " +
                        $"VALUES(@0, @1, '0', '0', '0') " +
                        $"ON DUPLICATE KEY UPDATE " +
                        $"`{kills}` = 0;", // clamp to 0
                        new object[] { Steam64, Team });
                }
                else
                {
                    await NonQuery(
                        $"UPDATE `{table.TableName}` SET " +
                        $"`{kills}` = `{kills}` - @2 " +
                        $"WHERE `{s64}` = @0 AND `{team}` = @1;",
                        new object[] { Steam64, Team, Math.Abs(amount) });
                }
            }
        }
        public async Task AddDeath(ulong Steam64, ulong Team, int amount = 0)
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
        public async Task AddTeamkill(ulong Steam64, ulong Team, int amount = 0)
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
                    $"VALUES(@0, @1, '0', '0', @2) " +
                    $"ON DUPLICATE KEY UPDATE " +
                    $"`{teamkills}` = `{teamkills}` + VALUES(`{teamkills}`);",
                    new object[] { Steam64, Team, amount });
            }
            else
            {
                uint oldTeamkills = await GetTeamkills(Steam64, Team);
                if (amount >= oldTeamkills)
                {
                    await NonQuery(
                        $"INSERT INTO `{table.TableName}` " +
                        $"(`{s64}`, `{team}`, `{kills}`, `{deaths}`, `{teamkills}`) " +
                        $"VALUES(@0, @1, '0', '0', '0') " +
                        $"ON DUPLICATE KEY UPDATE " +
                        $"`{teamkills}` = 0;", // clamp to 0
                        new object[] { Steam64, Team });
                }
                else
                {
                    await NonQuery(
                        $"UPDATE `{table.TableName}` SET " +
                        $"`{teamkills}` = `{teamkills}` - @2 " +
                        $"WHERE `{s64}` = @0 AND `{team}` = @1;",
                        new object[] { Steam64, Team, Math.Abs(amount) });
                }
            }
        }
        public MySqlTableLang GetTable(string key)
        {
            if (Data.TableData.TryGetValue(key, out MySqlTableLang lang))
                return lang;
            else return new MySqlTableLang(key, new Dictionary<string, string>());
        }
    }
}

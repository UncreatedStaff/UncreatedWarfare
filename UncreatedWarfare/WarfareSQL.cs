using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Players;
using Uncreated.SQL;

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
        public FPlayerName GetUsernames(ulong Steam64)
        {
            FPlayerName? name = null;
            Query(
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
        public async Task<FPlayerName> GetUsernamesAsync(ulong Steam64)
        {
            FPlayerName? name = null;
            await QueryAsync(
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
        public bool GetDiscordID(ulong Steam64, out ulong DiscordID)
        {
            ulong tid = 0;
            bool found = false;
            Query("SELECT `DiscordID` FROM `discordnames` WHERE `Steam64` = @0 LIMIT 1;", new object[1] { Steam64 }, R =>
            {
                tid = R.GetUInt64(0);
                found = true;
            });
            DiscordID = tid;
            return found;
        }
        public bool PlayerExistsInDatabase(ulong Steam64, out FPlayerName usernames)
        {
            FPlayerName? name = null;
            Query(
                $"SELECT `PlayerName`, `CharacterName`, `NickName` " +
                $"FROM `usernames` " +
                $"WHERE `Steam64` = @0 LIMIT 1;",
                new object[] { Steam64 },
                (R) =>
                {
                    name = new FPlayerName() { Steam64 = Steam64, PlayerName = R.GetString(0), CharacterName = R.GetString(1), NickName = R.GetString(2) };
                });
            if (name.HasValue)
            {
                usernames = name.Value;
                return true;
            }
            usernames = FPlayerName.Nil;
            return false;
        }
        public void CheckUpdateUsernames(FPlayerName player)
        {
            FPlayerName oldNames = GetUsernames(player.Steam64);
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
            NonQuery(
                $"INSERT INTO `usernames` " +
                $"(`Steam64`, `PlayerName`, `CharacterName`, `NickName`) VALUES(@0, @1, @2, @3) " +
                $"ON DUPLICATE KEY UPDATE " +
                updates +
                $";",
                parameters);
        }
        public void TryInitializeXP(ulong Steam64)
        {
            NonQuery(
                    "INSERT INTO `xp` " +
                    "(`Steam64`, `Branch`, `XP`) " +
                    "VALUES" +
                    "(@0, @1, 0), " +
                    "(@0, @2, 0), " +
                    "(@0, @3, 0) " +
                    "ON DUPLICATE KEY UPDATE " +
                    "`xp` = `xp`;",
                    new object[] { Steam64, (int)EBranch.INFANTRY, (int)EBranch.ARMOR, (int)EBranch.AIRFORCE });
        }
        public int GetXP(ulong Steam64, EBranch branch)
        {
            int xp = 0;
            Query(
                "SELECT `XP` " +
                "FROM `xp` " +
                "WHERE `Steam64` = @0 AND `Branch` = @1 " +
                "LIMIT 1;",
                new object[] { Steam64, branch },
                (R) =>
                {
                    xp = R.GetInt32(0);
                });
            return xp;
        }
        public Dictionary<EBranch, int> GetAllXP(ulong Steam64)
        {
            Dictionary<EBranch, int> levels = new Dictionary<EBranch, int>(6);
            Query(
                "SELECT `Branch`, `XP` " +
                "FROM `xp` " +
                "WHERE `Steam64` = @0;",
                new object[] { Steam64 },
                (R) =>
                {
                    levels.Add((EBranch)R.GetInt32(0), R.GetInt32(1));
                });
            if (levels.Count == 0)
                levels.Add(EBranch.DEFAULT, 0);
            return levels;
        }
        public int GetTeamwork(ulong Steam64)
        {
            int teamwork = 0;
            Query(
                $"SELECT `Points` " +
                $"FROM `teamwork` " +
                $"WHERE `Steam64` = @0 " +
                $"LIMIT 1;",
                new object[] { Steam64 },
                (R) =>
                {
                    teamwork = R.GetInt32(0);
                });
            return teamwork;
        }
        public uint GetKills(ulong Steam64, ulong Team)
        {
            uint kills = 0;
            Query(
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
        public uint GetDeaths(ulong Steam64, ulong Team)
        {
            uint deaths = 0;
            Query(
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
        public uint GetTeamkills(ulong Steam64, ulong Team)
        {
            uint teamkills = 0;
            Query(
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
        public int AddTeamwork(ulong Steam64, int amount)
        {
            int oldBalance = GetTeamwork(Steam64);

            if (amount == 0) return oldBalance;
            if (amount > 0)
            {
                NonQuery(
                    "INSERT INTO `teamwork` " +
                    "(`Steam64`, `Points`) " +
                    "VALUES(@0, @1) " +
                    "ON DUPLICATE KEY UPDATE " +
                    "`Points` = `Points` + @1;",
                    new object[] { Steam64, amount });
                return oldBalance + amount;
            }
            else
            {
                if (amount >= oldBalance)
                {
                    NonQuery(
                        "INSERT INTO `teamwork` " +
                        "(`Steam64`, `Points`) " +
                        "VALUES(@0, 0) " +
                        "ON DUPLICATE KEY UPDATE " +
                        "`Points` = 0;", // clamp to 0
                        new object[] { Steam64});
                    return 0;
                }
                else
                {
                    NonQuery(
                        "UPDATE `teamwork` SET " +
                        "`Points` = `Points` - @1 " +
                        "WHERE `Steam64` = @0;",
                        new object[] { Steam64, Math.Abs(amount) });
                    return oldBalance - amount;
                }
            }
        }
        private static readonly Uncreated.Networking.Encoding.ByteWriter bw = new Uncreated.Networking.Encoding.ByteWriter(0, false, 27);
        public void AddReport(Report report)
        {
            bw.BaseCapacity = report.Size;
            bw.Flush();
            Report.WriteReport(bw, report);
            byte[] blob = bw.ByteBuffer;
            NonQuery("INSERT INTO `reports` (`Reporter`, `Violator`, `ReportType`, `Data`, `Timestamp`, `Message`) VALUES (@0, @1, @2, @3, @4, @5);", new object[]
            {
                report.Reporter,
                report.Violator,
                report.Type,
                blob,
                string.Format(TIME_FORMAT_SQL, report.Time),
                report.Message
            });
        }
        /// <returns>New XP Value</returns>
        public int AddXP(ulong Steam64, EBranch branch, int amount)
        {
            int oldBalance = GetXP(Steam64, branch);

            if (amount == 0) return oldBalance;
            if (amount > 0)
            {
                NonQuery(
                    "INSERT INTO `xp` " +
                    "(`Steam64`, `Branch`, `XP`) " +
                    "VALUES(@0, @1, @2) " +
                    "ON DUPLICATE KEY UPDATE " +
                    "`xp` = `xp` + @2;",
                    new object[] { Steam64, (int)branch, amount });
                return oldBalance + amount;
            }
            else
            {
                if (amount >= oldBalance)
                {
                    NonQuery(
                        "INSERT INTO `xp` " +
                        "(`Steam64`, `Branch`, `XP`) " +
                        "VALUES(@0, @1, 0) " +
                        "ON DUPLICATE KEY UPDATE " +
                        "`XP` = 0;", // clamp to 0
                        new object[] { Steam64, (int)branch });
                    return 0;
                }
                else
                {
                    NonQuery(
                        "UPDATE `xp` SET " +
                        "`XP` = 0 " +
                        "WHERE `Steam64` = @0 AND `Branch` = @1;",
                        new object[] { Steam64, (int)branch, -amount });
                    return 0;
                }
            }
        }
        public void AddKill(ulong Steam64, ulong Team, int amount = 1)
        {
            if (!Data.TrackStats) return;
            if (amount == 0) return;
            if (amount > 0)
            {
                NonQuery(
                    $"INSERT INTO `playerstats` " +
                    $"(`Steam64`, `Team`, `Kills`, `Deaths`, `Teamkills`) " +
                    $"VALUES(@0, @1, @2, '0', '0') " +
                    $"ON DUPLICATE KEY UPDATE " +
                    $"`Kills` = `Kills` + VALUES(`Kills`);",
                    new object[] { Steam64, Team, amount });
            }
            else
            {
                uint oldkills = GetKills(Steam64, Team);
                if (amount >= oldkills)
                {
                    NonQuery(
                        $"INSERT INTO `playerstats` " +
                        $"(`Steam64`, `Team`, `Kills`, `Deaths`, `Teamkills`) " +
                        $"VALUES(@0, @1, '0', '0', '0') " +
                        $"ON DUPLICATE KEY UPDATE " +
                        $"`Kills` = 0;", // clamp to 0
                        new object[] { Steam64, Team });
                }
                else
                {
                    NonQuery(
                        $"UPDATE `playerstats` SET " +
                        $"`Kills` = `Kills` - @2 " +
                        $"WHERE `Steam64` = @0 AND `Team` = @1;",
                        new object[] { Steam64, Team, Math.Abs(amount) });
                }
            }
        }
        public void AddDeath(ulong Steam64, ulong Team, int amount = 1)
        {
            if (!Data.TrackStats) return;
            if (amount == 0) return;
            if (amount > 0)
            {
                NonQuery(
                    $"INSERT INTO `playerstats` " +
                    $"(`Steam64`, `Team`, `Kills`, `Deaths`, `Teamkills`) " +
                    $"VALUES(@0, @1, '0', @2, '0') " +
                    $"ON DUPLICATE KEY UPDATE " +
                    $"`Deaths` = `Deaths` + VALUES(`Deaths`);",
                    new object[] { Steam64, Team, amount });
            }
            else
            {
                uint oldDeaths = GetDeaths(Steam64, Team);
                if (amount >= oldDeaths)
                {
                    NonQuery(
                        $"INSERT INTO `playerstats` " +
                        $"(`Steam64`, `Team`, `Kills`, `Deaths`, `Teamkills`) " +
                        $"VALUES(@0, @1, '0', '0', '0') " +
                        $"ON DUPLICATE KEY UPDATE " +
                        $"`Deaths` = 0;", // clamp to 0
                        new object[] { Steam64, Team });
                }
                else
                {
                    NonQuery(
                        $"UPDATE `playerstats` SET " +
                        $"`Deaths` = `Deaths` - @2 " +
                        $"WHERE `Steam64` = @0 AND `Team` = @1;",
                        new object[] { Steam64, Team, Math.Abs(amount) });
                }
            }
        }
        public void AddTeamkill(ulong Steam64, ulong Team, int amount = 1)
        {
            if (!Data.TrackStats) return;
            if (amount == 0) return;
            if (amount > 0)
            {
                NonQuery(
                    $"INSERT INTO `playerstats` " +
                    $"(`Steam64`, `Team`, `Kills`, `Deaths`, `Teamkills`) " +
                    $"VALUES(@0, @1, '0', '0', @2) " +
                    $"ON DUPLICATE KEY UPDATE " +
                    $"`Teamkills` = `Teamkills` + VALUES(`Teamkills`);",
                    new object[] { Steam64, Team, amount });
            }
            else
            {
                uint oldTeamkills = GetTeamkills(Steam64, Team);
                if (amount >= oldTeamkills)
                {
                    NonQuery(
                        $"INSERT INTO `playerstats` " +
                        $"(`Steam64`, `Team`, `Kills`, `Deaths`, `Teamkills`) " +
                        $"VALUES(@0, @1, '0', '0', '0') " +
                        $"ON DUPLICATE KEY UPDATE " +
                        $"`Teamkills` = 0;", // clamp to 0
                        new object[] { Steam64, Team });
                }
                else
                {
                    NonQuery(
                        $"UPDATE `playerstats` SET " +
                        $"`Teamkills` = `Teamkills` - @2 " +
                        $"WHERE `Steam64` = @0 AND `Team` = @1;",
                        new object[] { Steam64, Team, Math.Abs(amount) });
                }
            }
        }
        public void AddUnban(ulong Pardoned, ulong Pardoner)
            => NonQuery(
                "INSERT INTO `unbans` " +
                "(`Pardoned`, `Pardoner`, `Timestamp`) " +
                "VALUES(@0, @1, @2);",
                new object[] { Pardoned, Pardoner, string.Format(TIME_FORMAT_SQL, DateTime.Now) });
        public void AddBan(ulong Banned, ulong Banner, uint Duration, string Reason)
            => NonQuery(
                "INSERT INTO `bans` " +
                "(`Banned`, `Banner`, `Duration`, `Reason`, `Timestamp`) " +
                "VALUES(@0, @1, @2, @3, @4);",
                new object[] { Banned, Banner, Duration, Reason, string.Format(TIME_FORMAT_SQL, DateTime.Now) });
        public void AddBan(ulong Banned, ulong Banner, uint Duration, string Reason, DateTime time)
            => NonQuery(
                "INSERT INTO `bans` " +
                "(`Banned`, `Banner`, `Duration`, `Reason`, `Timestamp`) " +
                "VALUES(@0, @1, @2, @3, @4);",
                new object[] { Banned, Banner, Duration, Reason, string.Format(TIME_FORMAT_SQL, time) });
        public void AddKick(ulong Kicked, ulong Kicker, string Reason)
            => NonQuery(
                "INSERT INTO `kicks` " +
                "(`Kicked`, `Kicker`, `Reason`, `Timestamp`) " +
                "VALUES(@0, @1, @2, @3);",
                new object[] { Kicked, Kicker, Reason, string.Format(TIME_FORMAT_SQL, DateTime.Now) });
        public void AddWarning(ulong Warned, ulong Warner, string Reason)
            => NonQuery(
                "INSERT INTO `warnings` " +
                "(`Warned`, `Warner`, `Reason`, `Timestamp`) " +
                "VALUES(@0, @1, @2, @3);",
                new object[] { Warned, Warner, Reason, string.Format(TIME_FORMAT_SQL, DateTime.Now) });
        public void AddBattleyeKick(ulong Kicked, string Reason)
            => NonQuery(
                "INSERT INTO `battleye_kicks` " +
                "(`Kicked`, `Reason`, `Timestamp`) " +
                "VALUES(@0, @1, @2);",
                new object[] { Kicked, Reason, string.Format(TIME_FORMAT_SQL, DateTime.Now) });
        public void AddTeamkill(ulong Teamkiller, ulong Teamkilled, string Cause, string ItemName = "", ushort Item = 0, float Distance = 0f)
            => NonQuery(
                "INSERT INTO `teamkills` " +
                "(`Teamkiller`, `Teamkilled`, `Cause`, `Item`, `ItemID`, `Distance`, `Timestamp`) " +
                "VALUES(@0, @1, @2, @3, @4, @5, @6);",
                new object[] { Teamkiller, Teamkilled, Cause, ItemName, Item, Distance, string.Format(TIME_FORMAT_SQL, DateTime.Now) });
        public bool HasPlayerJoined(ulong Steam64)
        {
            int amt = Scalar(
                $"SELECT COUNT(*) " +
                $"FROM `logindata` " +
                $"WHERE `Steam64` = @0;",
                new object[1] { Steam64 },
                o => Convert.ToInt32(o));
            return amt > 0;
        }
        public void RegisterLogin(Player player)
        {
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
            NonQuery(
                $"INSERT INTO `logindata` " +
                $"(`Steam64`, `IP`, `LastLoggedIn`) " +
                $"VALUES(@0, @1, @2) " +
                $"ON DUPLICATE KEY UPDATE " +
                $"`IP` = VALUES(`IP`), `LastLoggedIn` = VALUES(`LastLoggedIn`);",
                new object[3] { player.channel.owner.playerID.steamID.m_SteamID, ipaddress, string.Format(TIME_FORMAT_SQL, DateTime.Now) });
        }
        /// <returns> 0 if not banned, else duration (-1 meaning forever) </returns>
        public int IPBanCheck(ulong id, uint packedIP, byte[] hwid)
        { // returns true if banned
            ulong bannedid = 0;
            string oldids = string.Empty;
            int durationref = 0;
            string oldhwids = string.Empty;
            DateTime bantime = DateTime.Now;
            Query(
                $"SELECT * " +
                $"FROM `ipbans` " +
                $"WHERE `PackedIP` = @0 OR `HWID` = @1 " +
                $"LIMIT 1;",
                new object[] { packedIP, Convert.ToBase64String(hwid) }, R =>
                {
                    bannedid = R.GetUInt64("Instigator");
                    oldids = R.GetString("OtherIDs");
                    durationref = R.GetInt32("DurationMinutes");
                    bantime = R.GetDateTime("InitialBanDate");
                    oldhwids = R.GetString("OtherHWIDs");
                });
            if (bannedid == 0) return 0;
            if (durationref != -1 && bantime.AddMinutes(durationref) <= DateTime.Now) return 0;
            string idstr = id.ToString(Data.Locale);
            string[] ids = oldids.Split(',');
            if (ids.Contains(idstr) || ids.Length > 9) return durationref;
            string[] newids = new string[ids.Length + 1];
            for (int i = 0; i < ids.Length; i++)
                newids[i] = ids[i];
            newids[ids.Length] = idstr;
            string newidsstr = string.Join(",", newids);
            string newhwidsstr;
            if (hwid != null && hwid.Count(b => b == 0) != hwid.Length)
            {
                string hwidstr = Convert.ToBase64String(hwid);
                string[] hwids = oldhwids.Split(',');
                if (hwids.Contains(hwidstr) || hwids.Length > 9) return durationref;
                string[] newhwids = new string[hwids.Length + 1];
                for (int i = 0; i < hwids.Length; i++)
                    newhwids[i] = hwids[i];
                newhwids[hwids.Length] = idstr;
                newhwidsstr = string.Join(",", newids);
            } else
            {
                newhwidsstr = string.Empty;
            }
            
            NonQuery(
                $"UPDATE `ipbans` " +
                $"SET `OtherIDs` = @1, `OtherHWIDs` = @2 " +
                $"WHERE `PackedIP` = @0;",
                new object[] { packedIP, newidsstr, newhwidsstr }
                );
            return durationref;
        }
        /// <returns><see langword="true"/> if player was already banned and the duration was updated, else <see langword="false"/></returns>
        public bool AddIPBan(ulong id, uint packedIP, string unpackedIP, byte[] hwid, int duration = -1, string reason = "")
        {
            if (IPBanCheck(id, packedIP, hwid) != 0)
            {
                NonQuery(
                    $"UPDATE `ipbans` " +
                    $"SET `DurationMinutes` = @1 " +
                    $"WHERE `PackedIP` = @0;",
                    new object[] { packedIP, duration }
                    );
                return false;
            }
            NonQuery(
                $"INSERT INTO `ipbans` " +
                $"(`PackedIP`, `Instigator`, `OtherIDs`, `IPAddress`, `InitialBanDate`, `DurationMinutes`, `Reason`, `HWID`) " +
                $"VALUES(@0, @1, @2, @3, @4, @5, @6);",
                new object[] { packedIP, id, id.ToString(Data.Locale), unpackedIP, DateTime.Now, duration, reason, hwid }
                );
            return true;
        }
        public string GetIP(ulong id)
        {
            string ip = null;
            Query(
                $"SELECT `IP` " +
                $"FROM `logindata` " +
                $"WHERE `Steam64` = @0 " +
                $"LIMIT 1;",
                new object[] { id },
                R => ip = R.GetString("IP")
                );
            if (ip == null) return "255.255.255.255";
            else return ip;
        }
        public override void Log(string message, ConsoleColor color = ConsoleColor.Gray)
            => L.Log(message, color);
        public override void LogWarning(string message, ConsoleColor color = ConsoleColor.Yellow)
            => L.LogWarning(message, color);
        public override void LogError(string message, ConsoleColor color = ConsoleColor.Red)
            => L.LogError(message, color);
        public override void LogError(Exception ex, ConsoleColor color = ConsoleColor.Red)
            => L.LogError(ex, color);
    }
}

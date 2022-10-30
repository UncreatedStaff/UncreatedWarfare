using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Uncreated.Encoding;
using Uncreated.Framework;
using Uncreated.Players;
using Uncreated.SQL;

namespace Uncreated.Warfare;

public class WarfareSQL : MySqlDatabase
{
    const string DEFAULT_GATEWAY_BEGINNING = "192.168.1.";
    const string LOCAL_IP = "127.0.0.1";
    public const string TIME_FORMAT_SQL = "{0:" + TIME_FORMAT_SQL_I + "}";
    public const string TIME_FORMAT_SQL_I = "yyyy-MM-dd HH:mm:ss";
    public WarfareSQL(MySqlData data) : base(data)
    {
        DebugLogging |= UCWarfare.Config.Debug;
    }
    public FPlayerName GetUsernames(ulong Steam64)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        FPlayerName? name = null;
        Query(
            $"SELECT `PlayerName`, `CharacterName`, `NickName` " +
            $"FROM `usernames` " +
            $"WHERE `Steam64` = @0 LIMIT 1;",
            new object[] { Steam64 },
            (R) =>
            {
                name = new FPlayerName() { Steam64 = Steam64, PlayerName = R.GetString(0), CharacterName = R.GetString(1), NickName = R.GetString(2), WasFound = true };
            });
        if (name.HasValue)
            return name.Value;
        string tname = Steam64.ToString(Data.Locale);
        return new FPlayerName() { Steam64 = Steam64, PlayerName = tname, CharacterName = tname, NickName = tname, WasFound = false };
    }
    public async Task<FPlayerName> GetUsernamesAsync(ulong Steam64)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        FPlayerName? name = null;
        await QueryAsync(
            $"SELECT `PlayerName`, `CharacterName`, `NickName` " +
            $"FROM `usernames` " +
            $"WHERE `Steam64` = @0 LIMIT 1;",
            new object[] { Steam64 },
            (R) =>
            {
                name = new FPlayerName() { Steam64 = Steam64, PlayerName = R.GetString(0), CharacterName = R.GetString(1), NickName = R.GetString(2), WasFound = true };
            });
        if (name.HasValue)
            return name.Value;
        string tname = Steam64.ToString(Data.Locale);
        return new FPlayerName() { Steam64 = Steam64, PlayerName = tname, CharacterName = tname, NickName = tname, WasFound = false };
    }
    public bool GetDiscordID(ulong Steam64, out ulong DiscordID)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
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
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
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
    public async Task CheckUpdateUsernames(FPlayerName player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        FPlayerName oldNames = await GetUsernamesAsync(player.Steam64);
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
        await NonQueryAsync(
            $"INSERT INTO `usernames` " +
            $"(`Steam64`, `PlayerName`, `CharacterName`, `NickName`) VALUES(@0, @1, @2, @3) " +
            $"ON DUPLICATE KEY UPDATE " +
            updates +
            $";",
            parameters);
    }
    public int GetTeamwork(ulong Steam64)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
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
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
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
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
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
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
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
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
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
            if (amount + oldBalance < 0)
            {
                NonQuery(
                    "INSERT INTO `teamwork` " +
                    "(`Steam64`, `Points`) " +
                    "VALUES(@0, 0) " +
                    "ON DUPLICATE KEY UPDATE " +
                    "`Points` = 0;", // clamp to 0
                    new object[] { Steam64 });
                return 0;
            }
            else
            {
                NonQuery(
                    "UPDATE `teamwork` SET " +
                    "`Points` = @1 " +
                    "WHERE `Steam64` = @0;",
                    new object[] { Steam64, amount + oldBalance });
                return amount + oldBalance;
            }
        }
    }
    private static readonly ByteWriter bw = new ByteWriter(false, 27);
    public void AddReport(Report report)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        byte[] blob;
        lock (bw)
        {
            bw.BaseCapacity = report.Size;
            bw.Flush();
            Report.WriteReport(bw, report);
            blob = bw.ToArray();
        }
        NonQuery("INSERT INTO `reports` (`Reporter`, `Violator`, `ReportType`, `Data`, `Timestamp`, `Message`) VALUES (@0, @1, @2, @3, @4, @5);", new object[]
        {
            report.Reporter,
            report.Violator,
            report.Type,
            blob,
            string.Format(TIME_FORMAT_SQL, report.Time),
            report.Message ?? string.Empty
        });
    }
    public async Task<int> GetXP(ulong player, ulong team)
    {
        int xp = 0;
        await QueryAsync("SELECT `Experience` FROM `s2_levels` WHERE `Steam64` = @0 AND `Team` = @1 LIMIT 1;",
            new object[2] { player, team },
            R =>
            {
                xp = R.GetInt32(0);
            });
        return xp;
    }
    public async Task<int> GetCredits(ulong player, ulong team)
    {
        int xp = Point.Points.CreditsConfig.StartingCredits;
        await QueryAsync("SELECT `Credits` FROM `s2_levels` WHERE `Steam64` = @0 AND `Team` = @1 LIMIT 1;",
            new object[2] { player, team },
            R =>
            {
                xp = R.GetInt32(0);
            });
        return xp;
    }
    public async Task<int> AddXP(ulong player, ulong team, int amount)
    {
        int old = await GetXP(player, team);
        int total = amount + old;
        if (total >= 0)
        {
            await NonQueryAsync(
                "INSERT INTO `s2_levels` (`Steam64`, `Team`, `Experience`) VALUES (@0, @1, @2) ON DUPLICATE KEY UPDATE `Experience` = @2;",
                new object[3] { player, team, total }).ConfigureAwait(false);
            return total;
        }
        else if (amount != 0)
        {
            await NonQueryAsync(
                "INSERT INTO `s2_levels` (`Steam64`, `Team`, `Experience`) VALUES (@0, @1, 0) ON DUPLICATE KEY UPDATE `Experience` = 0;",
                new object[2] { player, team }).ConfigureAwait(false);
            return 0;
        }
        else return old;
    }
    public async Task<int> AddCredits(ulong player, ulong team, int amount)
    {
        int old = await GetCredits(player, team);
        int ttl = amount + old;
        if (ttl >= 0)
        {
            await NonQueryAsync(
                "INSERT INTO `s2_levels` (`Steam64`, `Team`, `Credits`) VALUES (@0, @1, @2) ON DUPLICATE KEY UPDATE `Credits` = @2;",
                new object[3] { player, team, ttl }).ConfigureAwait(false);
            return ttl;
        }
        else if (amount != 0)
        {
            await NonQueryAsync(
                "INSERT INTO `s2_levels` (`Steam64`, `Team`, `Credits`) VALUES (@0, @1, 0) ON DUPLICATE KEY UPDATE `Credits` = 0;",
                new object[2] { player, team }).ConfigureAwait(false);
            return 0;
        }
        else return old;
    }
    public Task AddKill(ulong Steam64, ulong Team, int amount = 1)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!Data.TrackStats) return Task.CompletedTask;
        if (amount == 0) return Task.CompletedTask;
        if (amount > 0)
        {
            return NonQueryAsync(
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
                return NonQueryAsync(
                    $"INSERT INTO `playerstats` " +
                    $"(`Steam64`, `Team`, `Kills`, `Deaths`, `Teamkills`) " +
                    $"VALUES(@0, @1, '0', '0', '0') " +
                    $"ON DUPLICATE KEY UPDATE " +
                    $"`Kills` = 0;", // clamp to 0
                    new object[] { Steam64, Team });
            }
            else
            {
                return NonQueryAsync(
                    $"UPDATE `playerstats` SET " +
                    $"`Kills` = `Kills` - @2 " +
                    $"WHERE `Steam64` = @0 AND `Team` = @1;",
                    new object[] { Steam64, Team, Math.Abs(amount) });
            }
        }
    }
    public Task AddDeath(ulong Steam64, ulong Team, int amount = 1)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!Data.TrackStats) return Task.CompletedTask;
        if (amount == 0) return Task.CompletedTask;
        if (amount > 0)
        {
            return NonQueryAsync(
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
                return NonQueryAsync(
                    $"INSERT INTO `playerstats` " +
                    $"(`Steam64`, `Team`, `Kills`, `Deaths`, `Teamkills`) " +
                    $"VALUES(@0, @1, '0', '0', '0') " +
                    $"ON DUPLICATE KEY UPDATE " +
                    $"`Deaths` = 0;", // clamp to 0
                    new object[] { Steam64, Team });
            }
            else
            {
                return NonQueryAsync(
                    $"UPDATE `playerstats` SET " +
                    $"`Deaths` = `Deaths` - @2 " +
                    $"WHERE `Steam64` = @0 AND `Team` = @1;",
                    new object[] { Steam64, Team, Math.Abs(amount) });
            }
        }
    }
    public Task AddTeamkill(ulong steam64, ulong team, int amount = 1)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!Data.TrackStats) return Task.CompletedTask;
        if (amount == 0) return Task.CompletedTask;
        if (amount > 0)
        {
            return NonQueryAsync(
                $"INSERT INTO `playerstats` " +
                $"(`Steam64`, `Team`, `Kills`, `Deaths`, `Teamkills`) " +
                $"VALUES(@0, @1, '0', '0', @2) " +
                $"ON DUPLICATE KEY UPDATE " +
                $"`Teamkills` = `Teamkills` + VALUES(`Teamkills`);",
                new object[] { steam64, team, amount });
        }
        else
        {
            uint oldTeamkills = GetTeamkills(steam64, team);
            if (amount >= oldTeamkills)
            {
                return NonQueryAsync(
                    $"INSERT INTO `playerstats` " +
                    $"(`Steam64`, `Team`, `Kills`, `Deaths`, `Teamkills`) " +
                    $"VALUES(@0, @1, '0', '0', '0') " +
                    $"ON DUPLICATE KEY UPDATE " +
                    $"`Teamkills` = 0;", // clamp to 0
                    new object[] { steam64, team });
            }
            else
            {
                return NonQueryAsync(
                    $"UPDATE `playerstats` SET " +
                    $"`Teamkills` = `Teamkills` - @2 " +
                    $"WHERE `Steam64` = @0 AND `Team` = @1;",
                    new object[] { steam64, team, Math.Abs(amount) });
            }
        }
    }
    public Task AddUnban(ulong target, ulong admin)
        => NonQueryAsync(
            "INSERT INTO `unbans` " +
            "(`Pardoned`, `Pardoner`, `Timestamp`) " +
            "VALUES(@0, @1, @2);",
            new object[] { target, admin, string.Format(TIME_FORMAT_SQL, DateTime.Now) });
    public Task AddBan(ulong target, ulong admin, int duration, string reason)
        => NonQueryAsync(
            "INSERT INTO `bans` " +
            "(`Banned`, `Banner`, `Duration`, `Reason`, `Timestamp`) " +
            "VALUES(@0, @1, @2, @3, @4);",
            new object[] { target, admin, duration, reason, string.Format(TIME_FORMAT_SQL, DateTime.Now) });
    public Task AddBan(ulong target, ulong admin, int duration, string reason, DateTime time)
        => NonQueryAsync(
            "INSERT INTO `bans` " +
            "(`Banned`, `Banner`, `Duration`, `Reason`, `Timestamp`) " +
            "VALUES(@0, @1, @2, @3, @4);",
            new object[] { target, admin, duration, reason, string.Format(TIME_FORMAT_SQL, time) });
    public Task AddKick(ulong target, ulong admin, string reason)
        => NonQueryAsync(
            "INSERT INTO `kicks` " +
            "(`Kicked`, `Kicker`, `Reason`, `Timestamp`) " +
            "VALUES(@0, @1, @2, @3);",
            new object[] { target, admin, reason, string.Format(TIME_FORMAT_SQL, DateTime.Now) });
    public Task AddWarning(ulong target, ulong admin, string reason)
        => NonQueryAsync(
            "INSERT INTO `warnings` " +
            "(`Warned`, `Warner`, `Reason`, `Timestamp`) " +
            "VALUES(@0, @1, @2, @3);",
            new object[] { target, admin, reason, string.Format(TIME_FORMAT_SQL, DateTime.Now) });
    public Task AddBattleyeKick(ulong target, string reason)
        => NonQueryAsync(
            "INSERT INTO `battleye_kicks` " +
            "(`Kicked`, `Reason`, `Timestamp`) " +
            "VALUES(@0, @1, @2);",
            new object[] { target, reason, string.Format(TIME_FORMAT_SQL, DateTime.Now) });
    public Task AddTeamkill(ulong target, ulong teamkilled, string deathCause, string itemName = "", ushort itemId = 0, float distance = 0f)
        => NonQueryAsync(
            "INSERT INTO `teamkills` " +
            "(`Teamkiller`, `Teamkilled`, `Cause`, `Item`, `ItemID`, `Distance`, `Timestamp`) " +
            "VALUES(@0, @1, @2, @3, @4, @5, @6);",
            new object[] { target, teamkilled, deathCause, itemName, itemId, distance, string.Format(TIME_FORMAT_SQL, DateTime.Now) });
    public async Task<bool> HasPlayerJoined(ulong steam64)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        bool found = false;
        await QueryAsync(
            $"SELECT COUNT(`Steam64`) " +
            $"FROM `logindata` " +
            $"WHERE `Steam64` = @0;",
            new object[1] { steam64 }, R => found = true);
        return found;
    }
    public Task RegisterLogin(Player player)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
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
        return NonQueryAsync(
            $"INSERT INTO `logindata` " +
            $"(`Steam64`, `IP`, `LastLoggedIn`) " +
            $"VALUES(@0, @1, @2) " +
            $"ON DUPLICATE KEY UPDATE " +
            $"`IP` = VALUES(`IP`), `LastLoggedIn` = VALUES(`LastLoggedIn`);",
            new object[3] { player.channel.owner.playerID.steamID.m_SteamID, ipaddress, string.Format(TIME_FORMAT_SQL, DateTime.Now) });
    }
    public string GetIP(ulong id)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        string? ip = null;
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
    public uint GetPackedIP(ulong id)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        uint ip = 0;
        Query(
            $"SELECT `IP`, `PackedIP` " +
            $"FROM `logindata` " +
            $"WHERE `Steam64` = @0 " +
            $"LIMIT 1;",
            new object[] { id },
            R =>
            {
                ip = R.GetUInt32(1);
                if (ip == 0)
                    ip = Parser.getUInt32FromIP(R.GetString(0));
            }
            );
        return ip;
    }
    protected override void Log(string message, ConsoleColor color = ConsoleColor.Gray)
        => L.Log(message, color);
    protected override void LogWarning(string message, ConsoleColor color = ConsoleColor.Yellow)
        => L.LogWarning(message, color);
    protected override void LogError(string message, ConsoleColor color = ConsoleColor.Red)
        => L.LogError(message, color);
    protected override void LogError(Exception ex, ConsoleColor color = ConsoleColor.Red)
        => L.LogError(ex, color);
}

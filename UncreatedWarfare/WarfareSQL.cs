using SDG.Unturned;
using Steamworks;
using System;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Encoding;
using Uncreated.Framework;
using Uncreated.Players;
using Uncreated.SQL;
using Uncreated.Warfare.Levels;

namespace Uncreated.Warfare;

public class WarfareSQL : MySqlDatabase
{
    /* TABLES */
    private const string PLAYER_STATS_TABLE = "playerstats";
    private const string LEVELS_TABLE = "s2_levels";
    private const string USERNAMES_TABLE = "usernames";
    private const string DISCORD_IDS_TABLE = "discordnames";
    private const string REPORTS_TABLE = "reports";
    private const string LOGIN_DATA_TABLE = "logindata";


    const string DEFAULT_GATEWAY_BEGINNING = "192.168.1.";
    const string LOCAL_IP = "127.0.0.1";
    public const string TIME_FORMAT_SQL = "{0:" + TIME_FORMAT_SQL_I + "}";
    public const string TIME_FORMAT_SQL_I = "yyyy-MM-dd HH:mm:ss";
    private static readonly ByteWriter ReportWriter = new ByteWriter(false, 27);
    public WarfareSQL(MySqlData data) : base(data)
    {
        DebugLogging |= UCWarfare.Config.Debug;
    }
    public PlayerNames GetUsernames(ulong s64)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        PlayerNames name = default;
        Query(
            "SELECT `PlayerName`, `CharacterName`, `NickName` " +
            "FROM `" + USERNAMES_TABLE + "` " +
            "WHERE `Steam64` = @0 LIMIT 1;",
            new object[] { s64 },
            reader =>
            {
                name = new PlayerNames() { Steam64 = s64, PlayerName = reader.GetString(0), CharacterName = reader.GetString(1), NickName = reader.GetString(2), WasFound = true };
            });
        if (name.WasFound)
            return name;
        string tname = s64.ToString(Data.AdminLocale);
        return new PlayerNames { Steam64 = s64, PlayerName = tname, CharacterName = tname, NickName = tname, WasFound = false };
    }
    public async Task<PlayerNames> GetUsernamesAsync(ulong s64, CancellationToken token = default)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        token.ThrowIfCancellationRequested();
        PlayerNames name = default;
        await QueryAsync(
            "SELECT `PlayerName`, `CharacterName`, `NickName` " +
            "FROM `" + USERNAMES_TABLE + "` " +
            "WHERE `Steam64` = @0 LIMIT 1;",
            new object[] { s64 },
            reader =>
            {
                name = new PlayerNames { Steam64 = s64, PlayerName = reader.GetString(0), CharacterName = reader.GetString(1), NickName = reader.GetString(2), WasFound = true };
            }, token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        if (name.WasFound)
            return name;
        string tname = s64.ToString(Data.AdminLocale);
        return new PlayerNames { Steam64 = s64, PlayerName = tname, CharacterName = tname, NickName = tname, WasFound = false };
    }
    [Obsolete]
    public bool GetDiscordID(ulong s64, out ulong discordId)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        ulong tid = 0;
        Query("SELECT `DiscordID` FROM `" + DISCORD_IDS_TABLE + "` WHERE `Steam64` = @0 LIMIT 1;", new object[] { s64 },
            reader =>
            {
                tid = reader.GetUInt64(0);
            });
        discordId = tid;
        return tid != 0;
    }
    public async Task<ulong> GetDiscordID(ulong s64, CancellationToken token = default)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        token.ThrowIfCancellationRequested();
        ulong tid = 0;
        await QueryAsync("SELECT `DiscordID` FROM `" + DISCORD_IDS_TABLE + "` WHERE `Steam64`=@0 LIMIT 1;", new object[] { s64 },
            reader => tid = reader.GetUInt64(0), token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        return tid;
    }
    public Task UpdateUsernames(PlayerNames player, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        return NonQueryAsync(
            "INSERT INTO `" + USERNAMES_TABLE + "` " +
            "(`Steam64`,`PlayerName`,`CharacterName`,`NickName`) VALUES(@0, @1, @2, @3) " +
            "ON DUPLICATE KEY UPDATE " +
            "`PlayerName`=VALUES(`PlayerName`),`CharacterName`=VALUES(`CharacterName`),`NickName`=VALUES(`NickName`);",
            new object[] { player.Steam64, player.PlayerName, player.CharacterName, player.NickName }, token);
    }
    public async Task AddReport(Report report, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        byte[] blob;
        lock (ReportWriter)
        {
            ReportWriter.BaseCapacity = report.Size;
            ReportWriter.Flush();
            Report.WriteReport(ReportWriter, report);
            blob = ReportWriter.ToArray();
        }
        await NonQueryAsync("INSERT INTO `" + REPORTS_TABLE + "` (`Reporter`, `Violator`, `ReportType`, `Data`, `Timestamp`, `Message`) VALUES (@0, @1, @2, @3, @4, @5);", new object[]
        {
            report.Reporter,
            report.Violator,
            report.Type,
            blob,
            string.Format(TIME_FORMAT_SQL, report.Time),
            report.Message ?? string.Empty
        }, token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
    }
    public async Task<(int, int)> GetCreditsAndXP(ulong player, ulong team, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        int credits = 0;
        int xp = 0;
        await QueryAsync("SELECT `Credits`, `Experience` FROM `" + LEVELS_TABLE + "` WHERE `Steam64` = @0 AND `Team` = @1 LIMIT 1;",
            new object[] { player, team },
            reader =>
            {
                credits = reader.GetInt32(0);
                xp = reader.GetInt32(1);
            }, token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        return (credits, xp);
    }
    public async Task<int> GetXP(ulong player, ulong team, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        int xp = 0;
        await QueryAsync("SELECT `Experience` FROM `" + LEVELS_TABLE + "` WHERE `Steam64` = @0 AND `Team` = @1 LIMIT 1;",
            new object[] { player, team },
            reader =>
            {
                xp = reader.GetInt32(0);
            }, token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        return xp;
    }
    public async Task<int> GetCredits(ulong player, ulong team, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        int xp = Points.CreditsConfig.StartingCredits;
        await QueryAsync("SELECT `Credits` FROM `" + LEVELS_TABLE + "` WHERE `Steam64` = @0 AND `Team` = @1 LIMIT 1;",
            new object[] { player, team },
            reader =>
            {
                xp = reader.GetInt32(0);
            }, token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        return xp;
    }
    public async Task<int> AddXP(ulong player, ulong team, int amount, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (amount > 0)
        {
            await QueryAsync("INSERT INTO `" + LEVELS_TABLE +
                             "` (`Steam64`,`Team`,`Experience`) VALUES (@0,@1,@2) ON DUPLICATE KEY UPDATE `Experience`=`Experience`+@2;" +
                             "SELECT `Experience` FROM `" + LEVELS_TABLE + "` WHERE `Steam64`=@0 AND `Team`=@1 LIMIT 1;",
                new object[] { player, team, amount },
                reader =>
                {
                    amount = reader.GetInt32(0);
                }, token).ConfigureAwait(false);
            return amount;
        }
        int old = await GetXP(player, team, token).ConfigureAwait(false);
        if (amount == 0)
            return old;
        int total = amount + old;
        if (total >= 0)
        {
            await NonQueryAsync(
                "INSERT INTO `" + LEVELS_TABLE + "` (`Steam64`, `Team`, `Experience`) VALUES (@0, @1, @2) ON DUPLICATE KEY UPDATE `Experience` = `Experience` + @2;",
                new object[] { player, team, amount }, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            return total;
        }
        await NonQueryAsync(
            "INSERT INTO `" + LEVELS_TABLE + "` (`Steam64`, `Team`, `Experience`) VALUES (@0, @1, 0) ON DUPLICATE KEY UPDATE `Experience` = 0;",
            new object[] { player, team }, token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        return 0;
    }
    public async Task<int> AddCredits(ulong player, ulong team, int amount, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (amount > 0)
        {
            await QueryAsync("INSERT INTO `" + LEVELS_TABLE +
                             "` (`Steam64`,`Team`,`Credits`) VALUES (@0,@1,@2) ON DUPLICATE KEY UPDATE `Credits`=`Credits`+@2;" +
                             "SELECT `Credits` FROM `" + LEVELS_TABLE + "` WHERE `Steam64`=@0 AND `Team`=@1 LIMIT 1;",
                new object[] { player, team, amount },
                reader =>
                {
                    amount = reader.GetInt32(0);
                }, token).ConfigureAwait(false);
            return amount;
        }
        int old = await GetCredits(player, team, token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        if (amount == 0)
            return old;
        int ttl = amount + old;
        if (ttl >= 0)
        {
            await NonQueryAsync(
                "INSERT INTO `" + LEVELS_TABLE + "` (`Steam64`, `Team`, `Credits`) VALUES (@0, @1, @2) ON DUPLICATE KEY UPDATE `Credits` = `Credits` + @2;",
                new object[] { player, team, amount }, token).ConfigureAwait(false);
            return ttl;
        }
        await NonQueryAsync(
            "INSERT INTO `" + LEVELS_TABLE + "` (`Steam64`, `Team`, `Credits`) VALUES (@0, @1, 0) ON DUPLICATE KEY UPDATE `Credits` = 0;",
            new object[] { player, team }, token).ConfigureAwait(false);
        return 0;
    }
    public async Task<(int, int)> AddCreditsAndXP(ulong player, ulong team, int credits, int xp, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (credits > 0 && xp > 0)
        {
            await QueryAsync("INSERT INTO `" + LEVELS_TABLE +
                             "` (`Steam64`,`Team`,`Credits`,`Experience`) VALUES (@0,@1,@2,@3) ON DUPLICATE KEY UPDATE `Credits`=`Credits`+@2, `Experience`=`Experience`+@3;" +
                             "SELECT `Credits`,`Experience` FROM `" + LEVELS_TABLE + "` WHERE `Steam64`=@0 AND `Team`=@1 LIMIT 1;",
                new object[] { player, team, credits, xp },
                reader =>
                {
                    credits = reader.GetInt32(0);
                    xp = reader.GetInt32(1);
                }, token).ConfigureAwait(false);
            return (credits, xp);
        }
        if (credits == 0 && xp == 0)
            return (credits, xp);
        (int oldCredits, int oldXP) = await GetCreditsAndXP(player, team, token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        int ttlc = credits + oldCredits;
        int ttlx = xp + oldXP;
        if (ttlc >= 0 && ttlx >= 0)
        {
            await NonQueryAsync(
                "INSERT INTO `" + LEVELS_TABLE + "` (`Steam64`, `Team`, `Credits`, `Experience`) VALUES (@0, @1, @2, @3) ON DUPLICATE KEY UPDATE `Credits` = `Credits` + @2, `Experience` = `Experience` + @3;",
                new object[] { player, team, credits, xp }, token).ConfigureAwait(false);
            return (ttlc, ttlx);
        }

        if (ttlc >= 0)
        {
            await NonQueryAsync(
                "INSERT INTO `" + LEVELS_TABLE + "` (`Steam64`, `Team`, `Credits`, `Experience`) VALUES (@0, @1, @2, 0) ON DUPLICATE KEY UPDATE `Credits` = `Credits` + @2, `Experience` = 0;",
                new object[] { player, team, credits }, token).ConfigureAwait(false);
            return (ttlc, 0);
        }
        if (ttlx >= 0)
        {
            await NonQueryAsync(
                "INSERT INTO `" + LEVELS_TABLE + "` (`Steam64`, `Team`, `Credits`, `Experience`) VALUES (@0, @1, 0, @2) ON DUPLICATE KEY UPDATE `Credits` = 0, `Experience` = `Experience` + @3;",
                new object[] { player, team, xp }, token).ConfigureAwait(false);
            return (0, ttlx);
        }
        await NonQueryAsync(
            "INSERT INTO `" + LEVELS_TABLE + "` (`Steam64`, `Team`, `Credits`, `Experience`) VALUES (@0, @1, 0, 0) ON DUPLICATE KEY UPDATE `Credits` = 0, `Experience` = 0;",
            new object[] { player, team }, token).ConfigureAwait(false);
        return (0, 0);
    }

    public async Task<uint> GetKills(ulong player, ulong team, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        uint amt = 0;
        await QueryAsync("SELECT `Kills` FROM `" + PLAYER_STATS_TABLE + "` WHERE `Steam64`=@0 AND `Team`=@1` LIMIT 1;",
            new object[] { player, team }, reader => amt = reader.GetUInt32(0), token).ConfigureAwait(false);
        return amt;
    }
    public async Task<uint> GetDeaths(ulong player, ulong team, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        uint amt = 0;
        await QueryAsync("SELECT `Deaths` FROM `" + PLAYER_STATS_TABLE + "` WHERE `Steam64`=@0 AND `Team`=@1` LIMIT 1;",
            new object[] { player, team }, reader => amt = reader.GetUInt32(0), token).ConfigureAwait(false);
        return amt;
    }
    public async Task<uint> GetTeamkills(ulong player, ulong team, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        uint amt = 0;
        await QueryAsync("SELECT `Teamkills` FROM `" + PLAYER_STATS_TABLE + "` WHERE `Steam64`=@0 AND `Team`=@1` LIMIT 1;",
            new object[] { player, team }, reader => amt = reader.GetUInt32(0), token).ConfigureAwait(false);
        return amt;
    }
    public async Task AddKill(ulong player, ulong team, int amount = 1, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!Data.TrackStats || amount == 0) return;
        if (amount > 0)
        {
            await NonQueryAsync(
                "INSERT INTO `" + PLAYER_STATS_TABLE + "` " +
                "(`Steam64`, `Team`, `Kills`, `Deaths`, `Teamkills`) " +
                "VALUES(@0, @1, @2, '0', '0') " +
                "ON DUPLICATE KEY UPDATE " +
                "`Kills` = `Kills` + VALUES(`Kills`);",
                new object[] { player, team, amount }, token).ConfigureAwait(false);
            return;
        }
        uint oldkills = await GetKills(player, team, token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        if (amount >= oldkills)
        {
            await NonQueryAsync(
                "INSERT INTO `" + PLAYER_STATS_TABLE + "` " +
                "(`Steam64`, `Team`, `Kills`, `Deaths`, `Teamkills`) " +
                "VALUES(@0, @1, '0', '0', '0') " +
                "ON DUPLICATE KEY UPDATE " +
                "`Kills` = 0;", // clamp to 0
                new object[] { player, team }, token).ConfigureAwait(false);
            return;
        }
        await NonQueryAsync(
            "UPDATE `" + PLAYER_STATS_TABLE + "` SET " +
            "`Kills` = `Kills` - @2 " +
            "WHERE `Steam64` = @0 AND `Team` = @1;",
            new object[] { player, team, Math.Abs(amount) }, token).ConfigureAwait(false);
    }
    public async Task AddDeath(ulong player, ulong team, int amount = 1, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!Data.TrackStats || amount == 0) return;
        if (amount > 0)
        {
            await NonQueryAsync(
                "INSERT INTO `" + PLAYER_STATS_TABLE + "` " +
                "(`Steam64`, `Team`, `Kills`, `Deaths`, `Teamkills`) " +
                "VALUES(@0, @1, '0', @2, '0') " +
                "ON DUPLICATE KEY UPDATE " +
                "`Deaths` = `Deaths` + VALUES(`Deaths`);",
                new object[] { player, team, amount }, token).ConfigureAwait(false);
            return;
        }
        uint oldDeaths = await GetDeaths(player, team, token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        if (amount >= oldDeaths)
        {
            await NonQueryAsync(
                "INSERT INTO `" + PLAYER_STATS_TABLE + "` " +
                "(`Steam64`, `Team`, `Kills`, `Deaths`, `Teamkills`) " +
                "VALUES(@0, @1, '0', '0', '0') " +
                "ON DUPLICATE KEY UPDATE " +
                "`Deaths` = 0;", // clamp to 0
                new object[] { player, team }, token).ConfigureAwait(false);
            return;
        }
        await NonQueryAsync(
            "UPDATE `" + PLAYER_STATS_TABLE + "` SET " +
            "`Deaths` = `Deaths` - @2 " +
            "WHERE `Steam64` = @0 AND `Team` = @1;",
            new object[] { player, team, Math.Abs(amount) }, token).ConfigureAwait(false);
    }
    public async Task AddTeamkill(ulong steam64, ulong team, int amount = 1, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (!Data.TrackStats || amount == 0) return;
        if (amount > 0)
        {
            await NonQueryAsync(
                "INSERT INTO `" + PLAYER_STATS_TABLE + "` " +
                "(`Steam64`, `Team`, `Kills`, `Deaths`, `Teamkills`) " +
                "VALUES(@0, @1, '0', '0', @2) " +
                "ON DUPLICATE KEY UPDATE " +
                "`Teamkills` = `Teamkills` + VALUES(`Teamkills`);",
                new object[] { steam64, team, amount }, token).ConfigureAwait(false);
            return;
        }
        uint oldTeamkills = await GetTeamkills(steam64, team, token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        if (amount >= oldTeamkills)
        {
            await NonQueryAsync(
                "INSERT INTO `" + PLAYER_STATS_TABLE + "` " +
                "(`Steam64`, `Team`, `Kills`, `Deaths`, `Teamkills`) " +
                "VALUES(@0, @1, '0', '0', '0') " +
                "ON DUPLICATE KEY UPDATE " +
                "`Teamkills` = 0;", // clamp to 0
                new object[] { steam64, team }, token).ConfigureAwait(false);
            return;
        }
        await NonQueryAsync(
            "UPDATE `" + PLAYER_STATS_TABLE + "` SET " +
            "`Teamkills` = `Teamkills` - @2 " +
            "WHERE `Steam64` = @0 AND `Team` = @1;",
            new object[] { steam64, team, Math.Abs(amount) }, token).ConfigureAwait(false);
    }
    public Task AddUnban(ulong target, ulong admin, CancellationToken token = default)
        => AddUnban(target, admin, DateTimeOffset.UtcNow, token);
    public Task AddBan(ulong target, ulong admin, int duration, string reason, CancellationToken token = default)
        => AddBan(target, admin, duration, reason, DateTimeOffset.UtcNow, token);
    public Task AddKick(ulong target, ulong admin, string reason, CancellationToken token = default)
        => AddKick(target, admin, reason, DateTimeOffset.UtcNow, token);
    public Task AddWarning(ulong target, ulong admin, string reason, CancellationToken token = default)
        => AddWarning(target, admin, reason, DateTimeOffset.UtcNow, token);
    public Task AddBattleyeKick(ulong target, string reason, CancellationToken token = default)
        => AddBattleyeKick(target, reason, DateTimeOffset.UtcNow, token);
    public Task AddTeamkill(ulong target, ulong teamkilled, string deathCause, string itemName = "", ushort itemId = 0, float distance = 0f, CancellationToken token = default)
        => AddTeamkill(target, teamkilled, deathCause, DateTimeOffset.UtcNow, itemName, itemId, distance, token);
    public Task AddUnban(ulong target, ulong admin, DateTimeOffset offset, CancellationToken token = default)
        => NonQueryAsync(
            "INSERT INTO `unbans` " +
            "(`Pardoned`, `Pardoner`, `Timestamp`) " +
            "VALUES(@0, @1, @2);",
            new object[] { target, admin, string.Format(TIME_FORMAT_SQL, offset.UtcDateTime) }, token);
    public Task AddBan(ulong target, ulong admin, int duration, string reason, DateTimeOffset offset, CancellationToken token = default)
        => NonQueryAsync(
            "INSERT INTO `bans` " +
            "(`Banned`, `Banner`, `Duration`, `Reason`, `Timestamp`) " +
            "VALUES(@0, @1, @2, @3, @4);",
            new object[] { target, admin, duration, reason, string.Format(TIME_FORMAT_SQL, offset.UtcDateTime) }, token);
    public Task AddKick(ulong target, ulong admin, string reason, DateTimeOffset offset, CancellationToken token = default)
        => NonQueryAsync(
            "INSERT INTO `kicks` " +
            "(`Kicked`, `Kicker`, `Reason`, `Timestamp`) " +
            "VALUES(@0, @1, @2, @3);",
            new object[] { target, admin, reason, string.Format(TIME_FORMAT_SQL, offset.UtcDateTime) }, token);
    public Task AddWarning(ulong target, ulong admin, string reason, DateTimeOffset offset, CancellationToken token = default)
        => NonQueryAsync(
            "INSERT INTO `warnings` " +
            "(`Warned`, `Warner`, `Reason`, `Timestamp`) " +
            "VALUES(@0, @1, @2, @3);",
            new object[] { target, admin, reason, string.Format(TIME_FORMAT_SQL, offset.UtcDateTime) }, token);
    public Task AddBattleyeKick(ulong target, string reason, DateTimeOffset offset, CancellationToken token = default)
        => NonQueryAsync(
            "INSERT INTO `battleye_kicks` " +
            "(`Kicked`, `Reason`, `Timestamp`) " +
            "VALUES(@0, @1, @2);",
            new object[] { target, reason, string.Format(TIME_FORMAT_SQL, offset.UtcDateTime) }, token);
    public Task AddTeamkill(ulong target, ulong teamkilled, string deathCause, DateTimeOffset offset, string itemName = "", ushort itemId = 0, float distance = 0f, CancellationToken token = default)
        => NonQueryAsync(
            "INSERT INTO `teamkills` " +
            "(`Teamkiller`, `Teamkilled`, `Cause`, `Item`, `ItemID`, `Distance`, `Timestamp`) " +
            "VALUES(@0, @1, @2, @3, @4, @5, @6);",
            new object[] { target, teamkilled, deathCause, itemName, itemId, distance, string.Format(TIME_FORMAT_SQL, offset.UtcDateTime) }, token);
    public Task RegisterLogin(Player player, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
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
            "INSERT INTO `" + LOGIN_DATA_TABLE + "` " +
            "(`Steam64`, `IP`, `LastLoggedIn`) " +
            "VALUES(@0, @1, @2) " +
            "ON DUPLICATE KEY UPDATE " +
            "`IP` = VALUES(`IP`), `LastLoggedIn` = VALUES(`LastLoggedIn`);",
            new object[] { player.channel.owner.playerID.steamID.m_SteamID, ipaddress, string.Format(TIME_FORMAT_SQL, DateTime.Now) }, token);
    }
    public string? TryGetIP(ulong id)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        string? ip = null;
        Query(
            "SELECT `IP` " +
            "FROM `logindata` " +
            "WHERE `Steam64` = @0 " +
            "LIMIT 1;",
            new object[] { id },
            reader => ip = reader.GetString(0)
            );
        return ip;
    }
    public uint TryGetPackedIP(ulong id)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        uint ip = 0;
        Query(
            "SELECT `IP`, `PackedIP` " +
            "FROM `logindata` " +
            "WHERE `Steam64` = @0 " +
            "LIMIT 1;",
            new object[] { id },
            reader =>
            {
                ip = reader.GetUInt32(1);
                if (ip == 0)
                    ip = Parser.getUInt32FromIP(reader.GetString(0));
            }
            );
        return ip;
    }
    public async Task<string?> TryGetIPAsync(ulong id, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        string? ip = null;
        await QueryAsync(
            "SELECT `IP` " +
            "FROM `logindata` " +
            "WHERE `Steam64` = @0 " +
            "LIMIT 1;",
            new object[] { id },
            reader => ip = reader.GetString(0), token
            ).ConfigureAwait(false);
        return ip ?? null;
    }
    public async Task<uint> TryGetPackedIPAsync(ulong id, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        uint ip = 0;
        await QueryAsync(
            "SELECT `IP`, `PackedIP` " +
            "FROM `logindata` " +
            "WHERE `Steam64` = @0 " +
            "LIMIT 1;",
            new object[] { id },
            reader =>
            {
                ip = reader.GetUInt32(1);
                if (ip == 0)
                    ip = Parser.getUInt32FromIP(reader.GetString(0));
            }, token
            ).ConfigureAwait(false);
        return ip;
    }
    protected override void Log(string message, ConsoleColor color = ConsoleColor.Gray)
        => L.Log(message, color);
    protected override void LogWarning(string message, ConsoleColor color = ConsoleColor.Yellow)
        => L.LogWarning(message, color, "MySQL");
    protected override void LogError(string message, ConsoleColor color = ConsoleColor.Red)
        => L.LogError(message, color, "MySQL");
    protected override void LogError(Exception ex, ConsoleColor color = ConsoleColor.Red)
        => L.LogError(ex, method: "MySQL");
}

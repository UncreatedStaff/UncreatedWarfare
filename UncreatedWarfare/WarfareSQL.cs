using SDG.Unturned;
using Steamworks;
using System;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Encoding;
using Uncreated.Framework;
using Uncreated.Networking;
using Uncreated.Players;
using Uncreated.SQL;
using Uncreated.Warfare.Levels;

namespace Uncreated.Warfare;

public interface IWarfareSql : IMySqlDatabase
{
    Task<PlayerNames> GetUsernamesAsync(ulong s64, CancellationToken token = default);
    Task<ulong> GetDiscordID(ulong s64, CancellationToken token = default);
    Task<ulong> GetSteam64(ulong discordId, CancellationToken token = default);
    Task<(int, int)> GetCreditsAndXP(ulong player, ulong team, CancellationToken token = default);
    Task<int> GetXP(ulong player, ulong team, CancellationToken token = default);
    Task<int> GetCredits(ulong player, ulong team, CancellationToken token = default);
    Task<uint> GetKills(ulong player, ulong team, CancellationToken token = default);
    Task<uint> GetDeaths(ulong player, ulong team, CancellationToken token = default);
    Task<uint> GetTeamkills(ulong player, ulong team, CancellationToken token = default);
}

public class WarfareSQL : MySqlDatabase, IWarfareSql
{
    /* TABLES */

    const string LOCAL_IP = "127.0.0.1";
    public const string TIME_FORMAT_SQL = "{0:" + TIME_FORMAT_SQL_I + "}";
    public const string TIME_FORMAT_SQL_I = "yyyy-MM-dd HH:mm:ss";
    private static readonly ByteWriter ReportWriter = new ByteWriter(false, 27);
    public WarfareSQL(MySqlData data) : base(data)
    {
        DebugLogging |= UCWarfare.Config.Debug;
    }

    public static readonly string TableLevels = UCWarfare.Season switch
    {
        1 => "levels",
        _ => ("s" + Math.Abs(UCWarfare.Season).ToString(Data.AdminLocale) + "_levels")
    };

    public static readonly string TableStats = UCWarfare.Season switch
    {
        // yes this is correct
        1 => "playerstats_s2",
        2 => "playerstats",
        _ => ("s" + Math.Abs(UCWarfare.Season).ToString(Data.AdminLocale) + "_playerstats")
    };

    // todo use these constants in the below methods

    public const string TableUsernames = "usernames";
    public const string TableDiscordIds = "discordnames";
    public const string TableReports = "reports";
    public const string TableBans = "bans";
    public const string TableKicks = "kicks";
    public const string TableWarnings = "warnings";
    public const string TableUnbans = "unbans";
    public const string TableBattlEyeKicks = "battleye_kicks";
    public const string TableMutes = "muted";
    public const string TableTeamkills = "teamkills";
    public const string TableLoginData = "logindata";
    public const string TableIPAddresses = "ip_addresses";
    public const string TableHWIDs = "hwids";
    public const string TableIPWhitelists = "ip_whitelists";

    public const string ColumnIPWhitelistsSteam64 = "Steam64";
    public const string ColumnIPWhitelistsIPRange = "IPRange";
    public const string ColumnIPWhitelistsAdmin = "Admin";

    public const string ColumnUsernamesSteam64 = "Steam64";
    public const string ColumnUsernamesPlayerName = "PlayerName";
    public const string ColumnUsernamesCharacterName = "CharacterName";
    public const string ColumnUsernamesNickName = "NickName";

    public const string ColumnDiscordIdsSteam64 = "Steam64";
    public const string ColumnDiscordIdsDiscordId = "DiscordID";

    public const string ColumnReportsPrimaryKey = "ReportID";
    public const string ColumnReportsReporter = "Reporter";
    public const string ColumnReportsViolator = "Violator";
    public const string ColumnReportsReportType = "ReportType";
    public const string ColumnReportsData = "Data";
    public const string ColumnReportsTimestamp = "Timestamp";
    public const string ColumnReportsMessage = "Message";

    public const string ColumnLevelsSteam64 = "Steam64";
    public const string ColumnLevelsTeam = "Team";
    public const string ColumnLevelsExperience = "Experience";
    public const string ColumnLevelsCredits = "Credits";

    public const string ColumnStatsSteam64 = "Steam64";
    public const string ColumnStatsTeam = "Team";
    public const string ColumnStatsKills = "Kills";
    public const string ColumnStatsDeaths = "Deaths";
    public const string ColumnStatsTeamkills = "Teamkills";

    public const string ColumnBansPrimaryKey = "BanID";
    public const string ColumnBansViolator = "Banned";
    public const string ColumnBansAdmin = "Banner";
    public const string ColumnBansDuration = "Duration";
    public const string ColumnBansReason = "Reason";
    public const string ColumnBansTimestamp = "Timestamp";

    public const string ColumnKicksPrimaryKey = "KickID";
    public const string ColumnKicksViolator = "Kicked";
    public const string ColumnKicksAdmin = "Kicker";
    public const string ColumnKicksReason = "Reason";
    public const string ColumnKicksTimestamp = "Timestamp";

    public const string ColumnWarningsPrimaryKey = "WarnID";
    public const string ColumnWarningsViolator = "Warned";
    public const string ColumnWarningsAdmin = "Warner";
    public const string ColumnWarningsReason = "Reason";
    public const string ColumnWarningsTimestamp = "Timestamp";

    public const string ColumnUnbansPrimaryKey = "UnbanID";
    public const string ColumnUnbansViolator = "Pardoned";
    public const string ColumnUnbansAdmin = "Pardoner";
    public const string ColumnUnbansTimestamp = "Timestamp";

    public const string ColumnBattlEyeKicksPrimaryKey = "BattleyeID";
    public const string ColumnBattlEyeKicksViolator = "Kicked";
    public const string ColumnBattlEyeKicksReason = "Reason";
    public const string ColumnBattlEyeKicksTimestamp = "Timestamp";

    public const string ColumnMutesPrimaryKey = "ID";
    public const string ColumnMutesViolator = "Steam64";
    public const string ColumnMutesAdmin = "Admin";
    public const string ColumnMutesReason = "Reason";
    public const string ColumnMutesDuration = "Duration";
    public const string ColumnMutesTimestamp = "Timestamp";
    public const string ColumnMutesType = "Type";
    public const string ColumnMutesDeactivated = "Deactivated";
    public const string ColumnMutesDeactivatedTimestamp = "DeactivateTimestamp";

    public const string ColumnTeamkillsPrimaryKey = "TeamkillID";
    public const string ColumnTeamkillsViolator = "Teamkiller";
    public const string ColumnTeamkillsVictim = "Teamkilled";
    public const string ColumnTeamkillsDeathCause = "Cause";
    public const string ColumnTeamkillsItemName = "Item";
    public const string ColumnTeamkillsItemId = "ItemID";
    public const string ColumnTeamkillsDistance = "Distance";
    public const string ColumnTeamkillsTimestamp = "Timestamp";

    public const string ColumnLoginDataSteam64 = "Steam64";
    public const string ColumnLoginDataUnpackedIP = "IP";
    public const string ColumnLoginDataPackedIP = "PackedIP";
    public const string ColumnLoginDataLastLoggedIn = "LastLoggedIn";
    public const string ColumnLoginDataFirstLoggedIn = "FirstLoggedIn";

    public const string ColumnIPAddressesPrimaryKey = "Id";
    public const string ColumnIPAddressesSteam64 = "Steam64";
    public const string ColumnIPAddressesPackedIP = "Packed";
    public const string ColumnIPAddressesUnpackedIP = "Unpacked";
    public const string ColumnIPAddressesLoginCount = "LoginCount";
    public const string ColumnIPAddressesLastLogin = "LastLogin";
    public const string ColumnIPAddressesFirstLogin = "FirstLogin";

    public const string ColumnHWIDsPrimaryKey = "Id";
    public const string ColumnHWIDsIndex = "Index";
    public const string ColumnHWIDsSteam64 = "Steam64";
    public const string ColumnHWIDsHWID = "HWID";
    public const string ColumnHWIDsLoginCount = "LoginCount";
    public const string ColumnHWIDsLastLogin = "LastLogin";
    public const string ColumnHWIDsFirstLogin = "FirstLogin";

    internal static readonly Schema[] WarfareSchemas =
    {
        new Schema(TableUsernames, new Schema.Column[]
        {
            new Schema.Column(ColumnUsernamesSteam64, SqlTypes.STEAM_64)
            {
                PrimaryKey = true,
                AutoIncrement = true
            },
            new Schema.Column(ColumnUsernamesPlayerName, SqlTypes.String(50)),
            new Schema.Column(ColumnUsernamesCharacterName, SqlTypes.String(50)),
            new Schema.Column(ColumnUsernamesNickName, SqlTypes.String(50))
        }, true, typeof(PlayerNames)),
        new Schema(TableDiscordIds, new Schema.Column[]
        {
            new Schema.Column(ColumnDiscordIdsSteam64, SqlTypes.STEAM_64)
            {
                PrimaryKey = true,
                AutoIncrement = true
            },
            new Schema.Column(ColumnDiscordIdsDiscordId, SqlTypes.ULONG)
        }, true, typeof(ulong)),
        new Schema(TableReports, new Schema.Column[]
        {
            new Schema.Column(ColumnReportsPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true
            },
            new Schema.Column(ColumnReportsReporter, SqlTypes.STEAM_64),
            new Schema.Column(ColumnReportsViolator, SqlTypes.STEAM_64),
            new Schema.Column(ColumnReportsReportType, SqlTypes.BYTE),
            new Schema.Column(ColumnReportsData, "blob"),
            new Schema.Column(ColumnReportsTimestamp, SqlTypes.DATETIME),
            new Schema.Column(ColumnReportsMessage, SqlTypes.STRING_255),
        }, true, typeof(Report)),
        // new Schema(TableLevels, new Schema.Column[]
        // {
        //     new Schema.Column(ColumnLevelsSteam64, SqlTypes.STEAM_64),
        //     new Schema.Column(ColumnLevelsTeam, SqlTypes.ULONG),
        //     new Schema.Column(ColumnLevelsExperience, SqlTypes.UINT)
        //     {
        //         Default = "'0'"
        //     },
        //     new Schema.Column(ColumnLevelsCredits, SqlTypes.UINT)
        //     {
        //         Default = "'" + Points.PointsConfig.StartingCredits.ToString(Data.AdminLocale) + "'"
        //     }
        // }, true, null),
        // new Schema(TableStats, new Schema.Column[]
        // {
        //     new Schema.Column(ColumnStatsSteam64, SqlTypes.STEAM_64),
        //     new Schema.Column(ColumnStatsTeam, SqlTypes.BYTE),
        //     new Schema.Column(ColumnStatsKills, SqlTypes.UINT)
        //     {
        //         Nullable = true,
        //         Default = "'0'"
        //     },
        //     new Schema.Column(ColumnStatsDeaths, SqlTypes.UINT)
        //     {
        //         Nullable = true,
        //         Default = "'0'"
        //     },
        //     new Schema.Column(ColumnStatsTeamkills, SqlTypes.UINT)
        //     {
        //         Nullable = true,
        //         Default = "'0'"
        //     }
        // }, true, null),
        new Schema(TableBans, new Schema.Column[]
        {
            new Schema.Column(ColumnBansPrimaryKey, SqlTypes.UINT)
            {
                PrimaryKey = true,
                AutoIncrement = true
            },
            new Schema.Column(ColumnBansViolator, SqlTypes.STEAM_64)
            {
                Nullable = true
            },
            new Schema.Column(ColumnBansAdmin, SqlTypes.STEAM_64)
            {
                Nullable = true
            },
            new Schema.Column(ColumnBansDuration, SqlTypes.INT)
            {
                Nullable = true
            },
            new Schema.Column(ColumnBansReason, SqlTypes.String(512))
            {
                Nullable = true
            },
            new Schema.Column(ColumnBansTimestamp, SqlTypes.DATETIME)
            {
                Nullable = true
            }
        }, true, null),
        new Schema(TableKicks, new Schema.Column[]
        {
            new Schema.Column(ColumnKicksPrimaryKey, SqlTypes.UINT)
            {
                PrimaryKey = true,
                AutoIncrement = true
            },
            new Schema.Column(ColumnKicksViolator, SqlTypes.STEAM_64)
            {
                Nullable = true
            },
            new Schema.Column(ColumnKicksAdmin, SqlTypes.STEAM_64)
            {
                Nullable = true
            },
            new Schema.Column(ColumnKicksReason, SqlTypes.String(512))
            {
                Nullable = true
            },
            new Schema.Column(ColumnKicksTimestamp, SqlTypes.DATETIME)
            {
                Nullable = true
            }
        }, true, null),
        new Schema(TableWarnings, new Schema.Column[]
        {
            new Schema.Column(ColumnWarningsPrimaryKey, SqlTypes.UINT)
            {
                PrimaryKey = true,
                AutoIncrement = true
            },
            new Schema.Column(ColumnWarningsViolator, SqlTypes.STEAM_64)
            {
                Nullable = true
            },
            new Schema.Column(ColumnWarningsAdmin, SqlTypes.STEAM_64)
            {
                Nullable = true
            },
            new Schema.Column(ColumnWarningsReason, SqlTypes.String(512))
            {
                Nullable = true
            },
            new Schema.Column(ColumnWarningsTimestamp, SqlTypes.DATETIME)
            {
                Nullable = true
            }
        }, true, null),
        new Schema(TableUnbans, new Schema.Column[]
        {
            new Schema.Column(ColumnUnbansPrimaryKey, SqlTypes.UINT)
            {
                PrimaryKey = true,
                AutoIncrement = true
            },
            new Schema.Column(ColumnUnbansViolator, SqlTypes.STEAM_64)
            {
                Nullable = true
            },
            new Schema.Column(ColumnUnbansAdmin, SqlTypes.STEAM_64)
            {
                Nullable = true
            },
            new Schema.Column(ColumnWarningsTimestamp, SqlTypes.DATETIME)
            {
                Nullable = true
            }
        }, true, null),
        new Schema(TableBattlEyeKicks, new Schema.Column[]
        {
            new Schema.Column(ColumnBattlEyeKicksPrimaryKey, SqlTypes.UINT)
            {
                PrimaryKey = true,
                AutoIncrement = true
            },
            new Schema.Column(ColumnBattlEyeKicksViolator, SqlTypes.STEAM_64)
            {
                Nullable = true
            },
            new Schema.Column(ColumnBattlEyeKicksReason, SqlTypes.String(512))
            {
                Nullable = true
            },
            new Schema.Column(ColumnBattlEyeKicksTimestamp, SqlTypes.DATETIME)
            {
                Nullable = true
            }
        }, true, null),
        new Schema(TableMutes, new Schema.Column[]
        {
            new Schema.Column(ColumnMutesPrimaryKey, SqlTypes.UINT)
            {
                PrimaryKey = true,
                AutoIncrement = true
            },
            new Schema.Column(ColumnMutesViolator, SqlTypes.STEAM_64)
            {
                Nullable = true,
                Default = "'0'"
            },
            new Schema.Column(ColumnMutesAdmin, SqlTypes.STEAM_64)
            {
                Nullable = true,
                Default = "'0'"
            },
            new Schema.Column(ColumnMutesReason, SqlTypes.STRING_255)
            {
                Nullable = true,
                Default = "''"
            },
            new Schema.Column(ColumnMutesDuration, SqlTypes.INT)
            {
                Nullable = true,
                Default = "'0'"
            },
            new Schema.Column(ColumnMutesTimestamp, SqlTypes.DATETIME)
            {
                Nullable = true
            },
            new Schema.Column(ColumnMutesType, SqlTypes.BYTE)
            {
                Nullable = true,
                Default = "'0'"
            },
            new Schema.Column(ColumnMutesDeactivated, SqlTypes.BOOLEAN)
            {
                Nullable = true,
                Default = "b'0'"
            },
            new Schema.Column(ColumnMutesDeactivatedTimestamp, SqlTypes.DATETIME)
            {
                Nullable = true
            }
        }, true, null),
        new Schema(TableTeamkills, new Schema.Column[]
        {
            new Schema.Column(ColumnTeamkillsPrimaryKey, SqlTypes.UINT)
            {
                PrimaryKey = true,
                AutoIncrement = true
            },
            new Schema.Column(ColumnTeamkillsViolator, SqlTypes.STEAM_64)
            {
                Nullable = true
            },
            new Schema.Column(ColumnTeamkillsVictim, SqlTypes.STEAM_64)
            {
                Nullable = true
            },
            new Schema.Column(ColumnTeamkillsDeathCause, SqlTypes.String(30))
            {
                Nullable = true
            },
            new Schema.Column(ColumnTeamkillsItemName, SqlTypes.String(50))
            {
                Nullable = true
            },
            new Schema.Column(ColumnTeamkillsItemId, SqlTypes.SHORT_ID)
            {
                Nullable = true
            },
            new Schema.Column(ColumnTeamkillsDistance, SqlTypes.FLOAT)
            {
                Nullable = true
            },
            new Schema.Column(ColumnTeamkillsTimestamp, SqlTypes.DATETIME)
            {
                Nullable = true
            }
        }, true, null),
        new Schema(TableLoginData, new Schema.Column[]
        {
            new Schema.Column(ColumnLoginDataSteam64, SqlTypes.STEAM_64)
            {
                PrimaryKey = true,
                AutoIncrement = true
            },
            new Schema.Column(ColumnLoginDataUnpackedIP, SqlTypes.String(50))
            {
                Default = "'" + LOCAL_IP + "'",
            },
            new Schema.Column(ColumnLoginDataPackedIP, SqlTypes.UINT)
            {
                Default = "'0'"
            },
            new Schema.Column(ColumnLoginDataLastLoggedIn, SqlTypes.DATETIME)
            {
                Nullable = true
            },
            new Schema.Column(ColumnLoginDataFirstLoggedIn, SqlTypes.DATETIME)
            {
                Nullable = true
            }
        }, true, null),
        new Schema(TableIPAddresses, new Schema.Column[]
        {
            new Schema.Column(ColumnIPAddressesPrimaryKey, SqlTypes.UINT)
            {
                PrimaryKey = true,
                AutoIncrement = true
            },
            new Schema.Column(ColumnIPAddressesSteam64, SqlTypes.STEAM_64),
            new Schema.Column(ColumnIPAddressesPackedIP, SqlTypes.UINT),
            new Schema.Column(ColumnIPAddressesUnpackedIP, "char(15)"),
            new Schema.Column(ColumnIPAddressesLoginCount, SqlTypes.UINT)
            {
                Default = "'1'"
            },
            new Schema.Column(ColumnIPAddressesLastLogin, SqlTypes.DATETIME)
            {
                Default = "CURRENT_TIMESTAMP"
            },
            new Schema.Column(ColumnIPAddressesFirstLogin, SqlTypes.DATETIME)
            {
                Nullable = true
            }
        }, true, null),
        new Schema(TableHWIDs, new Schema.Column[]
        {
            new Schema.Column(ColumnHWIDsPrimaryKey, SqlTypes.UINT)
            {
                PrimaryKey = true,
                AutoIncrement = true
            },
            new Schema.Column(ColumnHWIDsIndex, SqlTypes.UINT),
            new Schema.Column(ColumnHWIDsSteam64, SqlTypes.STEAM_64),
            new Schema.Column(ColumnHWIDsHWID, "binary(20)"),
            new Schema.Column(ColumnHWIDsLoginCount, SqlTypes.UINT)
            {
                Default = "'1'"
            },
            new Schema.Column(ColumnHWIDsLastLogin, SqlTypes.DATETIME)
            {
                Default = "CURRENT_TIMESTAMP"
            },
            new Schema.Column(ColumnHWIDsFirstLogin, SqlTypes.DATETIME)
            {
                Nullable = true
            },
        }, true, null),
        new Schema(TableIPWhitelists, new Schema.Column[]
        {
            new Schema.Column(ColumnIPWhitelistsSteam64, SqlTypes.STEAM_64),
            new Schema.Column(ColumnIPWhitelistsAdmin, SqlTypes.STEAM_64),
            new Schema.Column(ColumnIPWhitelistsIPRange, SqlTypes.String(18))
        }, true, typeof(IPv4Range))
    };
    public async Task<PlayerNames> GetUsernamesAsync(ulong s64, CancellationToken token = default)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        token.ThrowIfCancellationRequested();
        PlayerNames name = default;
        await QueryAsync(
            "SELECT `PlayerName`, `CharacterName`, `NickName` " +
            "FROM `" + TableUsernames + "` " +
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
        Query("SELECT `DiscordID` FROM `" + TableDiscordIds + "` WHERE `Steam64` = @0 LIMIT 1;", new object[] { s64 },
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
        await QueryAsync("SELECT `DiscordID` FROM `" + TableDiscordIds + "` WHERE `Steam64`=@0 LIMIT 1;", new object[] { s64 },
            reader => tid = reader.GetUInt64(0), token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        return tid;
    }
    public async Task<ulong> GetSteam64(ulong discordId, CancellationToken token = default)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        token.ThrowIfCancellationRequested();
        ulong tid = 0;
        await QueryAsync("SELECT `Steam64` FROM `" + TableDiscordIds + "` WHERE `DiscordID`=@0 LIMIT 1;", new object[] { discordId },
            reader => tid = reader.GetUInt64(0), token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        return tid;
    }
    public Task UpdateUsernames(PlayerNames player, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        return NonQueryAsync(
            "INSERT INTO `" + TableUsernames + "` " +
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
        await NonQueryAsync("INSERT INTO `" + TableReports + "` (`Reporter`, `Violator`, `ReportType`, `Data`, `Timestamp`, `Message`) VALUES (@0, @1, @2, @3, @4, @5);", new object[]
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
    public async Task TryInsertInitialRow(ulong player, CancellationToken token = default)
    {
        bool t1 = false, t2 = false;
        await QueryAsync("SELECT `Team` FROM `" + TableLevels + "` WHERE `Steam64` = @0;",
            new object[] { player },
            reader =>
            {
                ulong team = reader.GetUInt64(0);
                if (team == 1)
                    t1 = true;
                else if (team == 2)
                    t2 = true;
            }, token).ConfigureAwait(false);
        if (t1 && t2)
            return;
        string q = "INSERT INTO `" + TableLevels + "` (`Steam64`, `Team`, `Credits`, `Experience`) VALUES ";
        if (!t1)
        {
            q += "(@0, 1, @1, 0)";
            if (!t2)
                q += ",";
        }

        if (!t2)
            q += "(@0, 2, @1, 0)";
        q += ";";
        await NonQueryAsync(q, new object[] { player, Points.PointsConfig.StartingCredits }, token).ConfigureAwait(false);
        L.Log("Initialized new rows for " + player + ".");
    }
    public async Task<(int, int)> GetCreditsAndXP(ulong player, ulong team, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        int credits = 0;
        int xp = 0;
        await QueryAsync("SELECT `Credits`, `Experience` FROM `" + TableLevels + "` WHERE `Steam64` = @0 AND `Team` = @1 LIMIT 1;",
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
        await QueryAsync("SELECT `Experience` FROM `" + TableLevels + "` WHERE `Steam64` = @0 AND `Team` = @1 LIMIT 1;",
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
        int xp = Points.PointsConfig.StartingCredits;
        await QueryAsync("SELECT `Credits` FROM `" + TableLevels + "` WHERE `Steam64` = @0 AND `Team` = @1 LIMIT 1;",
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
            await QueryAsync("INSERT INTO `" + TableLevels +
                             "` (`Steam64`,`Team`,`Experience`) VALUES (@0,@1,@2) ON DUPLICATE KEY UPDATE `Experience`=`Experience`+@2;" +
                             "SELECT `Experience` FROM `" + TableLevels + "` WHERE `Steam64`=@0 AND `Team`=@1 LIMIT 1;",
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
                "INSERT INTO `" + TableLevels + "` (`Steam64`, `Team`, `Experience`) VALUES (@0, @1, 0) ON DUPLICATE KEY UPDATE `Experience` = `Experience` + @2;",
                new object[] { player, team, amount }, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            return total;
        }
        await NonQueryAsync(
            "INSERT INTO `" + TableLevels + "` (`Steam64`, `Team`, `Experience`) VALUES (@0, @1, 0) ON DUPLICATE KEY UPDATE `Experience` = 0;",
            new object[] { player, team }, token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        return 0;
    }
    public async Task<int> AddCredits(ulong player, ulong team, int amount, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (amount > 0)
        {
            await QueryAsync("INSERT INTO `" + TableLevels +
                             "` (`Steam64`,`Team`,`Credits`) VALUES (@0,@1,@2) ON DUPLICATE KEY UPDATE `Credits`=`Credits`+@2;" +
                             "SELECT `Credits` FROM `" + TableLevels + "` WHERE `Steam64`=@0 AND `Team`=@1 LIMIT 1;",
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
                "INSERT INTO `" + TableLevels + "` (`Steam64`, `Team`, `Credits`) VALUES (@0, @1, 0) ON DUPLICATE KEY UPDATE `Credits` = `Credits` + @2;",
                new object[] { player, team, amount }, token).ConfigureAwait(false);
            return ttl;
        }
        await NonQueryAsync(
            "INSERT INTO `" + TableLevels + "` (`Steam64`, `Team`, `Credits`) VALUES (@0, @1, 0) ON DUPLICATE KEY UPDATE `Credits` = 0;",
            new object[] { player, team }, token).ConfigureAwait(false);
        return 0;
    }
    public async Task<(int, int)> AddCreditsAndXP(ulong player, ulong team, int credits, int xp, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (credits > 0 && xp > 0)
        {
            await QueryAsync("INSERT INTO `" + TableLevels +
                             "` (`Steam64`,`Team`,`Credits`,`Experience`) VALUES (@0,@1,@2,@3) ON DUPLICATE KEY UPDATE `Credits`=`Credits`+@2, `Experience`=`Experience`+@3;" +
                             "SELECT `Credits`,`Experience` FROM `" + TableLevels + "` WHERE `Steam64`=@0 AND `Team`=@1 LIMIT 1;",
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
                "INSERT INTO `" + TableLevels + "` (`Steam64`, `Team`, `Credits`, `Experience`) VALUES (@0, @1, " +
                (credits < 0 ? "0" : "@2" ) + ", " + (xp < 0 ? "0" : "@3" ) + ") " +
                "ON DUPLICATE KEY UPDATE `Credits` = `Credits` + @2, `Experience` = `Experience` + @3;",
                new object[] { player, team, credits, xp }, token).ConfigureAwait(false);
            return (ttlc, ttlx);
        }

        if (ttlc >= 0)
        {
            await NonQueryAsync(
                "INSERT INTO `" + TableLevels + "` (`Steam64`, `Team`, `Credits`, `Experience`) VALUES (@0, @1, " + (credits < 0 ? "0" : "@2") + ", 0) ON DUPLICATE KEY UPDATE `Credits` = `Credits` + @2, `Experience` = 0;",
                new object[] { player, team, credits }, token).ConfigureAwait(false);
            return (ttlc, 0);
        }
        if (ttlx >= 0)
        {
            await NonQueryAsync(
                "INSERT INTO `" + TableLevels + "` (`Steam64`, `Team`, `Credits`, `Experience`) VALUES (@0, @1, 0, " + (xp < 0 ? "0" : "@2") + ") ON DUPLICATE KEY UPDATE `Credits` = 0, `Experience` = `Experience` + @2;",
                new object[] { player, team, xp }, token).ConfigureAwait(false);
            return (0, ttlx);
        }
        await NonQueryAsync(
            "INSERT INTO `" + TableLevels + "` (`Steam64`, `Team`, `Credits`, `Experience`) VALUES (@0, @1, 0, 0) ON DUPLICATE KEY UPDATE `Credits` = 0, `Experience` = 0;",
            new object[] { player, team }, token).ConfigureAwait(false);
        return (0, 0);
    }
    public async Task<uint> GetKills(ulong player, ulong team, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        uint amt = 0;
        await QueryAsync("SELECT `Kills` FROM `" + TableStats + "` WHERE `Steam64`=@0 AND `Team`=@1` LIMIT 1;",
            new object[] { player, team }, reader => amt = reader.GetUInt32(0), token).ConfigureAwait(false);
        return amt;
    }
    public async Task<uint> GetDeaths(ulong player, ulong team, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        uint amt = 0;
        await QueryAsync("SELECT `Deaths` FROM `" + TableStats + "` WHERE `Steam64`=@0 AND `Team`=@1` LIMIT 1;",
            new object[] { player, team }, reader => amt = reader.GetUInt32(0), token).ConfigureAwait(false);
        return amt;
    }
    public async Task<uint> GetTeamkills(ulong player, ulong team, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        uint amt = 0;
        await QueryAsync("SELECT `Teamkills` FROM `" + TableStats + "` WHERE `Steam64`=@0 AND `Team`=@1` LIMIT 1;",
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
                "INSERT INTO `" + TableStats + "` " +
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
                "INSERT INTO `" + TableStats + "` " +
                "(`Steam64`, `Team`, `Kills`, `Deaths`, `Teamkills`) " +
                "VALUES(@0, @1, '0', '0', '0') " +
                "ON DUPLICATE KEY UPDATE " +
                "`Kills` = 0;", // clamp to 0
                new object[] { player, team }, token).ConfigureAwait(false);
            return;
        }
        await NonQueryAsync(
            "UPDATE `" + TableStats + "` SET " +
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
                "INSERT INTO `" + TableStats + "` " +
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
                "INSERT INTO `" + TableStats + "` " +
                "(`Steam64`, `Team`, `Kills`, `Deaths`, `Teamkills`) " +
                "VALUES(@0, @1, '0', '0', '0') " +
                "ON DUPLICATE KEY UPDATE " +
                "`Deaths` = 0;", // clamp to 0
                new object[] { player, team }, token).ConfigureAwait(false);
            return;
        }
        await NonQueryAsync(
            "UPDATE `" + TableStats + "` SET " +
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
                "INSERT INTO `" + TableStats + "` " +
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
                "INSERT INTO `" + TableStats + "` " +
                "(`Steam64`, `Team`, `Kills`, `Deaths`, `Teamkills`) " +
                "VALUES(@0, @1, '0', '0', '0') " +
                "ON DUPLICATE KEY UPDATE " +
                "`Teamkills` = 0;", // clamp to 0
                new object[] { steam64, team }, token).ConfigureAwait(false);
            return;
        }
        await NonQueryAsync(
            "UPDATE `" + TableStats + "` SET " +
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
            if (ipaddress == LOCAL_IP)
                if (SteamGameServer.GetPublicIP().TryGetIPv4Address(out ipnum))
                    ipaddress = Parser.getIPFromUInt32(ipnum);
        }
        else
        {
            ipaddress = LOCAL_IP;
            ipnum = Parser.getUInt32FromIP(ipaddress);
        }

        string now = string.Format(TIME_FORMAT_SQL, DateTime.UtcNow);
        return NonQueryAsync(
            $"INSERT INTO `{TableLoginData}` " +
            $"(`{ColumnLoginDataSteam64}`,`{ColumnLoginDataUnpackedIP}`,`{ColumnLoginDataPackedIP}`,`{ColumnLoginDataLastLoggedIn}`,`{ColumnLoginDataFirstLoggedIn}`) " +
            "VALUES(@0, @1, @2, @3, @3) " +
            "ON DUPLICATE KEY UPDATE " +
            $"`{ColumnLoginDataUnpackedIP}`=@1,`{ColumnLoginDataPackedIP}`=@2,`{ColumnLoginDataLastLoggedIn}`=@3;",
            new object[] { player.channel.owner.playerID.steamID.m_SteamID, ipaddress, ipnum, now }, token);
    }
    public async Task<uint> TryGetPackedIPAsync(ulong id, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        uint ip = 0;
        await QueryAsync(
            $"SELECT `{ColumnLoginDataUnpackedIP}`, `{ColumnLoginDataPackedIP}` " +
            $"FROM `{TableLoginData}` " +
            $"WHERE `{ColumnLoginDataSteam64}` = @0 " +
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
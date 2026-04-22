using MySqlConnector;
using System;
using System.Linq;
using Uncreated.Warfare.Database.Manual;
using Uncreated.Warfare.Events.Models;
using Uncreated.Warfare.Events.Models.Players;
using Uncreated.Warfare.Util.List;
#pragma warning disable IDE0300

namespace Uncreated.Warfare.Kits;

/// <summary>
/// Performs specialized operations on player statistics relating to kits.
/// </summary>
public interface IKitStatisticService
{
    /// <summary>
    /// Gets the total damage done to each limb by a player with a gun while <paramref name="kit"/> was equipped.
    /// </summary>
    /// <param name="player">The player who's data to search for.</param>
    /// <param name="kitId">The kit to search for. If <c>0</c>, factors in all kit's data.</param>
    /// <param name="season">Season to query for. If <c>&lt;0</c>, uses the current build's season.</param>
    /// <param name="normalize">Whether or not to normalize all values so that they all add up to 1.</param>
    /// <param name="token">Cancellation token for the operation.</param>
    /// <returns>A new dictionary containing all the total damage done to each limb.</returns>
    Task<IReadOnlyDictionary<ELimb, double>> GetHeatmapDataForKit(CSteamID player, uint kitId, bool normalize = true, int season = -1, CancellationToken token = default);

    /// <summary>
    /// Query multiple statistics at once.
    /// </summary>
    /// <param name="player">The player who's data to search for.</param>
    /// <param name="kitId">The kit to search for. If <c>0</c>, searches for statistics of players with no kit.</param>
    /// <param name="season">Season to query for. If <c>&lt;0</c>, uses the current build's season.</param>
    /// <param name="types">Types of statistics to query. Order is preserved in the returned array.</param>
    /// <param name="token">Cancellation token for the operation.</param>
    /// <returns>List of value corresponding to each requested statistic. If <see cref="KnownStatType.KDR"/> is part of the array, the last two elements of this array will be the Kills and Deaths for the player.</returns>
    Task<IReadOnlyList<double>> BulkQueryStats(IReadOnlyList<KnownStatType> types, CSteamID player, uint kitId, int season = -1, CancellationToken token = default);

    /// <summary>
    /// Query multiple statistics at once for all kits.
    /// </summary>
    /// <param name="player">The player who's data to search for.</param>
    /// <param name="season">Season to query for. If <c>&lt;0</c>, uses the current build's season.</param>
    /// <param name="types">Types of statistics to query. Order is preserved in the returned array.</param>
    /// <param name="token">Cancellation token for the operation.</param>
    /// <returns>Dictionary List of value corresponding to each requested statistic. If <see cref="KnownStatType.KDR"/> is part of the array, the last two elements of this array will be the Kills and Deaths for the player.</returns>
    Task<IDictionary<uint, IReadOnlyList<double>>> BulkQueryAllKitsStats(IReadOnlyList<KnownStatType> types, CSteamID player, int season = -1, CancellationToken token = default);

    /// <summary>
    /// Query basic stats for all played kits in the given season.
    /// </summary>
    /// <param name="player">The player who's data to search for.</param>
    /// <param name="season">Season to query for. If <c>&lt;0</c>, uses the current build's season.</param>
    /// <param name="token">Cancellation token for the operation.</param>
    Task<IDictionary<uint, BasicKitStats>> QueryAllBasicKitStats(CSteamID player, int season = -1, CancellationToken token = default);
}

/// <summary>
/// Basic statistics displayed for all kits.
/// </summary>
public class BasicKitStats
{
    private int _kills;
    private int _deaths;
    private double _playtimeSeconds;
    public uint KitId { get; }

    public int Kills
    {
        get => _kills;
        set => _kills = value;
    }

    public int Deaths
    {
        get => _deaths;
        set => _deaths = value;
    }

    public double PlaytimeSeconds
    {
        get => _playtimeSeconds;
        set => _playtimeSeconds = value;
    }

    public BasicKitStats(uint kitId)
    {
        KitId = kitId;
    }

    public int IncrementKills()
    {
        return Interlocked.Increment(ref _kills);
    }

    public int IncrementDeaths()
    {
        return Interlocked.Increment(ref _deaths);
    }

    public double AddPlaytimeSeconds(double seconds)
    {
        // thread-safe add for doubles
        // https://stackoverflow.com/questions/1400465/why-is-there-no-overload-of-interlocked-add-that-accepts-doubles-as-parameters
        double newCurrentValue = _playtimeSeconds;
        while (true)
        {
            double currentValue = newCurrentValue;
            double newValue = currentValue + seconds;
            newCurrentValue = Interlocked.CompareExchange(ref _playtimeSeconds, newValue, currentValue);
            if (newCurrentValue.Equals(currentValue))
                return newValue;
        }
    }
}

/// <summary>
/// Queryable statistics for kit-specific data.
/// </summary>
public enum KnownStatType
{
    /// <summary>
    /// Kills, deaths, and K/D ratio.
    /// </summary>
    /// <remarks>Kills and Deaths will be appended to the end of the result set.</remarks>
    KDR,
    /// <summary>
    /// Seconds played.
    /// </summary>
    Playtime,
    /// <summary>
    /// Number of teamkills.
    /// </summary>
    Teamkills,
    /// <summary>
    /// Total amount of damage dealt.
    /// </summary>
    DamageDealt,

    /// <summary>
    /// Number of enemy vehicles destroyed.
    /// </summary>
    VehiclesDestroyed,

    /// <summary>
    /// Number of enemy FOBs destroyed.
    /// </summary>
    FOBsDestroyed,

    /// <summary>
    /// Number of friendly FOBs built.
    /// </summary>
    FOBsBuilt,

    /// <summary>
    /// Number of times reviving a friendly player from injured state.
    /// </summary>
    Revives,

    /// <summary>
    /// Number of kills with a melee weapon or punch.
    /// </summary>
    MeleeKills,

    /// <summary>
    /// Average kill distance for gunshots and splash kills.
    /// </summary>
    AverageKillDistance,

    /// <summary>
    /// Furthest kill distance for gunshots and splash kills.
    /// </summary>
    HighestKillDistance,

    /// <summary>
    /// Number of suicides.
    /// </summary>
    Suicides,

    /// <summary>
    /// Amount of health given to teammates with medical supplies.
    /// </summary>
    HealthAided,

    /// <summary>
    /// Number of kills while using a vehicle-mounted weapon or via roadkill, vehicle explosions, etc.
    /// </summary>
    KillsWithVehicle
}

internal class KitStatisticService : IKitStatisticService, IEventListener<PlayerDied>, IEventListener<SessionEnded>
{
    private readonly IManualMySqlProvider _manualSql;

    private const string SelectKitHeatmapData =
        """
        SELECT SUM(CAST(`Damage` AS DOUBLE)), `Limb` FROM `stats_damage` `d`
        JOIN `stats_sessions` `s` ON `d`.`InstigatorSession` = `s`.`SessionId`
        WHERE `d`.`Instigator`=@0 AND `s`.`Kit`=@1 AND `s`.`Season`=@2 AND `d`.`Cause`='GUN' AND `d`.`IsTeamkill`=0 AND `d`.`IsSuicide`=0
        GROUP BY d.Limb;
        """;

    private const string SelectGlobalHeatmapData =
        """
        SELECT SUM(CAST(`Damage` AS DOUBLE)), `Limb` FROM `stats_damage` `d`
        JOIN `stats_sessions` `s` ON `d`.`InstigatorSession` = `s`.`SessionId`
        WHERE `d`.`Instigator`=@0 AND `s`.`Season`=@1AND `d`.`Cause`='GUN' AND `d`.`IsTeamkill`=0 AND `d`.`IsSuicide`=0
        GROUP BY d.Limb;
        """;

    private const string SelectBasicKitStats =
        """
        SELECT SUM(`q`.`Playtime`) AS `Playtime`,SUM(`q`.`Kills`) AS `Kills`,SUM(`q`.`Deaths`) AS `Deaths`,`q`.`Kit` FROM (
        SELECT `LengthSeconds` AS `Playtime`,
        (SELECT COUNT(*) FROM `stats_deaths` AS `d` WHERE `d`.`InstigatorSession` = `s`.`SessionId` AND `d`.`IsTeamkill` = 0 AND `d`.`IsSuicide` = 0) `Kills`,
        (SELECT COUNT(*) FROM `stats_deaths` AS `d` WHERE `d`.`Session` = `s`.`SessionId` AND (`d`.`IsSuicide` = 1 OR `d`.`IsTeamkill` = 0)) `Deaths`,
        `Kit`
        FROM `stats_sessions` AS `s` WHERE `Steam64` = @0 AND `Season` = @1
        ) AS `q` GROUP BY `Kit` HAVING `Kit` IS NOT NULL;
        """;

    private const string SelectAdvancedOneKitStats = $"CALL `{Migrations.QueryKitStatsProcedure.ProcedureName}`(@0, @1, @2, @3);";
    private const string SelectAdvancedAllKitStats = $"CALL `{Migrations.QueryKitStatsProcedure.ProcedureName}`(@0, @1, @2, 0);";

    private const int StatCount = 14;

    // ReSharper disable RedundantExplicitArraySize

    private static readonly int[] KitStatMasks = new int[StatCount]
    {
        1,      // KDR,
        8192,   // Playtime,
        2,      // Teamkills,
        8,      // DamageDealt,
        16,     // VehiclesDestroyed,
        32,     // FOBsDestroyed,
        64,     // FOBsBuilt,
        128,    // Revives,
        512,    // MeleeKills,
        1024,   // AverageKillDistance,
        2048,   // HighestKillDistance
        4,      // Suicides
        256,    // HealthAided
        4096    // KillsWithVehicle
    };

    // 0 = int32, 1 = float64
    private static readonly int[] KitStatDataTypes = new int[StatCount]
    {
        1,      // KDR,
        1,      // Playtime,
        0,      // Teamkills,
        1,      // DamageDealt,
        0,      // VehiclesDestroyed,
        0,      // FOBsDestroyed,
        0,      // FOBsBuilt,
        0,      // Revives,
        0,      // MeleeKills,
        1,      // AverageKillDistance,
        1,      // HighestKillDistance
        0,      // Suicides
        1,      // HealthAided
        0       // KillsWithVehicle
    };

    private static readonly int[] KitStatOrdinals = new int[StatCount]
    {
        -1,     // KDR,
        15,     // Playtime,
        3,      // Teamkills,
        5,      // DamageDealt,
        6,      // VehiclesDestroyed,
        7,      // FOBsDestroyed,
        8,      // FOBsBuilt,
        9,      // Revives,
        11,     // MeleeKills,
        12,     // AverageKillDistance,
        13,     // HighestKillDistance
        4,      // Suicides
        10,     // HealthAided
        14      // KillsWithVehicle
    };

    // ReSharper restore RedundantExplicitArraySize

    public KitStatisticService(IManualMySqlProvider manualSql)
    {
        _manualSql = manualSql;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<ELimb, double>> GetHeatmapDataForKit(
        CSteamID player,
        uint kitId,
        bool normalize = true,
        int season = -1,
        CancellationToken token = default
    )
    {
        string query;
        object[] parameters;

        if (season < 0)
            season = WarfareModule.Season;

        if (kitId != 0)
        {
            query = SelectKitHeatmapData;
            parameters = [ player.m_SteamID, kitId, season ];
        }
        else
        {
            query = SelectGlobalHeatmapData;
            parameters = [ player.m_SteamID, season ];
        }

        LinearDictionary<ELimb, double> totals = new LinearDictionary<ELimb, double>();

        await _manualSql.QueryAsync(
            query,
            parameters,
            token,
            reader =>
            {
                double total = reader.GetDouble(0);
                string limbEnum = reader.GetString(1);
                if (!Enum.TryParse(limbEnum, true, out ELimb limb))
                    return;

                totals[limb] = total;
            }
        );

        if (normalize)
        {
            KeyValuePair<ELimb, double>[] newArray = new KeyValuePair<ELimb, double>[totals.Count];
            totals.CopyTo(newArray, 0);

            double total = 0;
            for (int i = 0; i < newArray.Length; ++i)
            {
                ref KeyValuePair<ELimb, double> pair = ref newArray[i];
                total += pair.Value;
            }

            for (int i = 0; i < newArray.Length; ++i)
            {
                ref KeyValuePair<ELimb, double> pair = ref newArray[i];
                totals[pair.Key] = pair.Value / total;
            }
        }

        return totals;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<double>> BulkQueryStats(
        IReadOnlyList<KnownStatType> types,
        CSteamID player,
        uint kitId,
        int season = -1,
        CancellationToken token = default)
    {
        KnownStatType[] statTypes = IntlInitResultSet(
            types,
            out int mask,
            out int resultSetLength,
            ref season
        );

        double[] results = new double[resultSetLength];

        await _manualSql.QueryAsync(
            SelectAdvancedOneKitStats,
            [ player.m_SteamID, mask, season, kitId ],
            token,
            reader =>
            {
                if (reader.IsDBNull(0))
                {
                    if (kitId != 0) return;
                }
                else if (reader.GetUInt32(0) != kitId)
                {
                    return;
                }

                ReadStatisticsResults(statTypes, results, reader);
            }
        );

        return results;
    }

    /// <inheritdoc />
    public async Task<IDictionary<uint, IReadOnlyList<double>>> BulkQueryAllKitsStats(
        IReadOnlyList<KnownStatType> types,
        CSteamID player,
        int season = -1,
        CancellationToken token = default)
    {
        KnownStatType[] statTypes = IntlInitResultSet(
            types,
            out int mask,
            out int resultSetLength,
            ref season
        );

        Dictionary<uint, IReadOnlyList<double>> kitStats = new Dictionary<uint, IReadOnlyList<double>>(32);

        await _manualSql.QueryAsync(
            SelectAdvancedAllKitStats,
            [ player.m_SteamID, mask, season ],
            token,
            reader =>
            {
                uint kitId = reader.IsDBNull(0) ? 0 : reader.GetUInt32(0);
                if (kitStats.ContainsKey(kitId))
                    return;

                double[] results = new double[resultSetLength];
                ReadStatisticsResults(statTypes, results, reader);
                kitStats[kitId] = results;
            });

        return kitStats;
    }

    private static void ReadStatisticsResults(KnownStatType[] statTypes, double[] result, MySqlDataReader reader)
    {
        for (int i = 0; i < statTypes.Length; ++i)
        {
            KnownStatType stat = statTypes[i];
            if (stat == KnownStatType.KDR)
            {
                double kills = reader.GetInt32(1);
                double deaths = reader.GetInt32(2);
                result[^2] = kills;
                result[^1] = deaths;
                result[i] = deaths > 0 ? kills / deaths : kills;
                continue;
            }

            if ((int)stat is < 0 or >= StatCount)
                continue;

            int dataType = KitStatDataTypes[(int)stat];
            int ordinal = KitStatOrdinals[(int)stat];
            double value = dataType == 0 ? reader.GetInt32(ordinal) : reader.GetDouble(ordinal);

            result[i] = value;
        }
    }

    private static KnownStatType[] IntlInitResultSet(IReadOnlyList<KnownStatType> types,
        out int mask,
        out int resultSetLength,
        ref int season)
    {
        if (season < 0)
            season = WarfareModule.Season;

        KnownStatType[] array = types as KnownStatType[] ?? types.ToArray();
        bool hasKdr = Array.IndexOf(array, KnownStatType.KDR) >= 0;

        int maskIntl = 0;
        foreach (KnownStatType stat in types)
        {
            int statIndex = (int)stat;
            if (statIndex >= 0 && statIndex < KitStatMasks.Length)
                maskIntl |= KitStatMasks[statIndex];
        }

        mask = maskIntl;
        resultSetLength = array.Length + (hasKdr ? 1 : 0) * 2;

        return array;
    }

    /// <inheritdoc />
    public async Task<IDictionary<uint, BasicKitStats>> QueryAllBasicKitStats(CSteamID player, int season = -1, CancellationToken token = default)
    {
        if (season < 0)
            season = WarfareModule.Season;

        Dictionary<uint, BasicKitStats> kitStats = new Dictionary<uint, BasicKitStats>(32);

        await _manualSql.QueryAsync(SelectBasicKitStats, [ player.m_SteamID, season ], token, read =>
        {
            uint kitId = read.GetUInt32(3);

            BasicKitStats stats = new BasicKitStats(kitId)
            {
                PlaytimeSeconds = read.GetDouble(0),
                Kills = read.GetInt32(1),
                Deaths = read.GetInt32(2)
            };

            kitStats[kitId] = stats;
        });

        return kitStats;
    }

    void IEventListener<PlayerDied>.HandleEvent(PlayerDied e, IServiceProvider serviceProvider)
    {
        if ((!e.WasTeamkill || e.WasSuicide) && e.Session != null)
        {
            e.Player.Component<KitPlayerComponent>()
                    .UpdateBasicStats(e.Session?.KitId ?? 0, k => k.IncrementDeaths());
        }

        if (e is { WasSuicide: false, WasTeamkill: false, KillerSession: not null })
        {
            e.Player.Component<KitPlayerComponent>()
                    .UpdateBasicStats(e.KillerSession.KitId ?? 0, k => k.IncrementKills());
        }
    }

    void IEventListener<SessionEnded>.HandleEvent(SessionEnded e, IServiceProvider serviceProvider)
    {
        if (e.Session.KitId.HasValue)
        {
            e.Player.Component<KitPlayerComponent>()
                    .UpdateBasicStats(e.Session.KitId.Value, k => k.AddPlaytimeSeconds(e.Session.LengthSeconds));
        }
    }
}

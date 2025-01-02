using System;
using Uncreated.Warfare.Database.Manual;
using Uncreated.Warfare.Players;
using Uncreated.Warfare.Players.Extensions;
using Uncreated.Warfare.Players.Management;

namespace Uncreated.Warfare.Stats;

/// <summary>
/// Handles storing and loading player's points and reputation.
/// </summary>
/// <remarks>Contains operations for read, add/subtract, and set, including bulk operations for both XP and credits.</remarks>
public interface IPointsStore
{
    /// <summary>
    /// Queries both XP and credits for a player.
    /// </summary>
    Task<PlayerPoints> GetPointsAsync(CSteamID player, uint factionId, int season, CancellationToken token = default);

    /// <summary>
    /// Queries both XP and credits for a player.
    /// </summary>
    Task<PlayerPoints> GetPointsAsync(CSteamID player, uint factionId, CancellationToken token = default)
    {
        return GetPointsAsync(player, factionId, WarfareModule.Season, token);
    }


    /// <summary>
    /// Queries credits for a player.
    /// </summary>
    async Task<double> GetCreditsAsync(CSteamID player, uint factionId, int season, CancellationToken token = default)
    {
        return (await GetPointsAsync(player, factionId, season, token).ConfigureAwait(false)).Credits;
    }

    /// <summary>
    /// Queries credits for a player.
    /// </summary>
    Task<double> GetCreditsAsync(CSteamID player, uint factionId, CancellationToken token = default)
    {
        return GetCreditsAsync(player, factionId, WarfareModule.Season, token);
    }


    /// <summary>
    /// Queries XP for a player.
    /// </summary>
    async Task<double> GetXPAsync(CSteamID player, uint factionId, int season, CancellationToken token = default)
    {
        return (await GetPointsAsync(player, factionId, season, token).ConfigureAwait(false)).XP;
    }

    /// <summary>
    /// Queries XP for a player.
    /// </summary>
    Task<double> GetXPAsync(CSteamID player, uint factionId, CancellationToken token = default)
    {
        return GetXPAsync(player, factionId, WarfareModule.Season, token);
    }


    /// <summary>
    /// Queries reputation for a player.
    /// </summary>
    Task<double> GetReputationAsync(CSteamID player, CancellationToken token = default);


    /// <summary>
    /// Adds to XP and credits for a player. Can be negative.
    /// </summary>
    /// <returns>The resulting point count.</returns>
    Task<PlayerPoints> AddToPointsAsync(CSteamID player, uint factionId, int season, double deltaXp, double deltaCredits, CancellationToken token = default);

    /// <summary>
    /// Adds to XP and credits for a player. Can be negative.
    /// </summary>
    /// <returns>The resulting point count.</returns>
    Task<PlayerPoints> AddToPointsAsync(CSteamID player, uint factionId, double deltaXp, double deltaCredits, CancellationToken token = default)
    {
        return AddToPointsAsync(player, factionId, WarfareModule.Season, deltaXp, deltaCredits, token);
    }


    /// <summary>
    /// Adds to XP for a player. Can be negative.
    /// </summary>
    /// <returns>The resulting point count.</returns>
    Task<PlayerPoints> AddToXPAsync(CSteamID player, uint factionId, int season, double deltaXp, CancellationToken token = default)
    {
        return AddToPointsAsync(player, factionId, season, deltaXp, deltaCredits: 0d, token);
    }

    /// <summary>
    /// Adds to XP for a player. Can be negative.
    /// </summary>
    /// <returns>The resulting point count.</returns>
    Task<PlayerPoints> AddToXPAsync(CSteamID player, uint factionId, double deltaXp, CancellationToken token = default)
    {
        return AddToXPAsync(player, factionId, WarfareModule.Season, deltaXp, token);
    }


    /// <summary>
    /// Adds to credits for a player. Can be negative.
    /// </summary>
    /// <returns>The resulting point count.</returns>
    Task<PlayerPoints> AddToCreditsAsync(CSteamID player, uint factionId, int season, double deltaCredits, CancellationToken token = default)
    {
        return AddToPointsAsync(player, factionId, season, deltaXp: 0d, deltaCredits, token);
    }

    /// <summary>
    /// Adds to credits for a player. Can be negative.
    /// </summary>
    /// <returns>The resulting point count.</returns>
    Task<PlayerPoints> AddToCreditsAsync(CSteamID player, uint factionId, double deltaCredits, CancellationToken token = default)
    {
        return AddToCreditsAsync(player, factionId, WarfareModule.Season, deltaCredits, token);
    }
    
    /// <summary>
    /// Adds to reputation for a player. Can be negative.
    /// </summary>
    /// <returns>The resulting reputation count.</returns>
    Task<double> AddToReputationAsync(CSteamID player, double deltaReputation, CancellationToken token = default);

    /// <summary>
    /// Directly sets the credits and XP of a player.
    /// </summary>
    Task SetPointsAsync(CSteamID player, uint factionId, int season, double xp, double credits, CancellationToken token = default);

    /// <summary>
    /// Directly sets the credits and XP of a player.
    /// </summary>
    Task SetPointsAsync(CSteamID player, uint factionId, double xp, double credits, CancellationToken token = default)
    {
        return SetPointsAsync(player, factionId, WarfareModule.Season, xp, credits, token);
    }

    /// <summary>
    /// Directly sets the XP of a player.
    /// </summary>
    Task SetXPAsync(CSteamID player, uint factionId, int season, double xp, CancellationToken token = default);

    /// <summary>
    /// Directly sets the XP of a player.
    /// </summary>
    Task SetXPAsync(CSteamID player, uint factionId, double xp, CancellationToken token = default)
    {
        return SetXPAsync(player, factionId, WarfareModule.Season, xp, token);
    }

    /// <summary>
    /// Directly sets the credits of a player.
    /// </summary>
    Task SetCreditsAsync(CSteamID player, uint factionId, int season, double credits, CancellationToken token = default);

    /// <summary>
    /// Directly sets the credits of a player.
    /// </summary>
    Task SetCreditsAsync(CSteamID player, uint factionId, double credits, CancellationToken token = default)
    {
        return SetCreditsAsync(player, factionId, WarfareModule.Season, credits, token);
    }


    /// <summary>
    /// Directly sets the reputation of a player.
    /// </summary>
    Task SetReputationAsync(CSteamID player, double reputation, CancellationToken token = default);


    /// <summary>
    /// Attempts to remove an amount of XP and credits from a player only if they have enough to spare.
    /// </summary>
    /// <returns>If the removal was successful.</returns>
    Task<bool> TryRemovePoints(CSteamID player, uint factionId, int season, double xpToRemove, double creditsToRemove, CancellationToken token = default);

    /// <summary>
    /// Attempts to remove an amount of XP and credits from a player only if they have enough to spare.
    /// </summary>
    /// <returns>If the removal was successful.</returns>
    Task<bool> TryRemovePoints(CSteamID player, uint factionId, double xpToRemove, double creditsToRemove, CancellationToken token = default)
    {
        return TryRemovePoints(player, factionId, WarfareModule.Season, xpToRemove, creditsToRemove, token);
    }

    /// <summary>
    /// Attempts to remove an amount of credits from a player only if they have enough to spare.
    /// </summary>
    /// <returns>If the removal was successful.</returns>
    Task<bool> TryRemoveCredits(CSteamID player, uint factionId, int season, double creditsToRemove, CancellationToken token = default);

    /// <summary>
    /// Attempts to remove an amount of credits from a player only if they have enough to spare.
    /// </summary>
    /// <returns>If the removal was successful.</returns>
    Task<bool> TryRemoveCredits(CSteamID player, uint factionId, double creditsToRemove, CancellationToken token = default)
    {
        return TryRemoveCredits(player, factionId, WarfareModule.Season, creditsToRemove, token);
    }

    /// <summary>
    /// Attempts to remove an amount of XP from a player only if they have enough to spare.
    /// </summary>
    /// <returns>If the removal was successful.</returns>
    Task<bool> TryRemoveXP(CSteamID player, uint factionId, int season, double xpToRemove, CancellationToken token = default);

    /// <summary>
    /// Attempts to remove an amount of XP from a player only if they have enough to spare.
    /// </summary>
    /// <returns>If the removal was successful.</returns>
    Task<bool> TryRemoveXP(CSteamID player, uint factionId, double xpToRemove, CancellationToken token = default)
    {
        return TryRemoveXP(player, factionId, WarfareModule.Season, xpToRemove, token);
    }
}

/// <summary>
/// Handles storing and loading player's points and reputation from a MySQL database.
/// </summary>
public class MySqlPointsStore : IPointsStore
{
    private readonly IManualMySqlProvider _sql;
    private readonly IPlayerService _playerService;

    // selects stats
    private const string GetPointsQuery = $"SELECT `{ColumnPointsXP}`,`{ColumnPointsCredits}` FROM `{TablePoints}` WHERE " +
                                          $"`{ColumnPointsSteam64}`=@0 AND " +
                                          $"`{ColumnPointsFaction}`=@1 AND " +
                                          $"`{ColumnPointsSeason}`=@2 LIMIT 1;";

    // selects credits only
    private const string GetCreditsQuery = $"SELECT `{ColumnPointsCredits}` FROM `{TablePoints}` WHERE " +
                                           $"`{ColumnPointsSteam64}`=@0 AND " +
                                           $"`{ColumnPointsFaction}`=@1 AND " +
                                           $"`{ColumnPointsSeason}`=@2 LIMIT 1;";

    // selects xp only
    private const string GetXPQuery = $"SELECT `{ColumnPointsCredits}` FROM `{TablePoints}` WHERE " +
                                      $"`{ColumnPointsSteam64}`=@0 AND " +
                                      $"`{ColumnPointsFaction}`=@1 AND " +
                                      $"`{ColumnPointsSeason}`=@2 LIMIT 1;";
    
    // selects reputation
    private const string GetReputationQuery = $"SELECT `{ColumnReputationValue}` FROM `{TableReputation}` WHERE " +
                                              $"`{ColumnReputationSteam64}`=@0 LIMIT 1;";

    private const string SetPointsQuery = $"INSERT INTO `{TablePoints}` (`{ColumnPointsSteam64}`,`{ColumnPointsFaction}`,`{ColumnPointsSeason}`,`{ColumnPointsXP}`,`{ColumnPointsCredits}`) " +
                                          $"VALUES (@0,@1,@2,@3,@4) AS `new` " +
                                          $"ON DUPLICATE KEY UPDATE " +
                                          $"`{TablePoints}`.`{ColumnPointsXP}`=`new`.`{ColumnPointsXP}`," +
                                          $"`{TablePoints}`.`{ColumnPointsCredits}`=`new`.`{ColumnPointsCredits}`;";

    private const string SetXPQuery = $"INSERT INTO `{TablePoints}` (`{ColumnPointsSteam64}`,`{ColumnPointsFaction}`,`{ColumnPointsSeason}`,`{ColumnPointsXP}`,`{ColumnPointsCredits}`) " +
                                      $"VALUES (@0,@1,@2,@3,0) AS `new` " +
                                      $"ON DUPLICATE KEY UPDATE " +
                                      $"`{TablePoints}`.`{ColumnPointsXP}`=`new`.`{ColumnPointsXP}`;";

    private const string SetCreditsQuery = $"INSERT INTO `{TablePoints}` (`{ColumnPointsSteam64}`,`{ColumnPointsFaction}`,`{ColumnPointsSeason}`,`{ColumnPointsXP}`,`{ColumnPointsCredits}`) " +
                                           $"VALUES (@0,@1,@2,0,@3) AS `new` " +
                                           $"ON DUPLICATE KEY UPDATE " +
                                           $"`{TablePoints}`.`{ColumnPointsCredits}`=`new`.`{ColumnPointsCredits}`;";
    
    private const string SetReputationQuery = $"INSERT INTO `{TableReputation}` (`{ColumnReputationSteam64}`,`{ColumnReputationValue}`) " +
                                              $"VALUES (@0,@1) AS `new` " +
                                              $"ON DUPLICATE KEY UPDATE " +
                                              $"`{TableReputation}`.`{ColumnReputationValue}`=`new`.`{ColumnReputationValue}`;";

    // adds XP and credits to the current value, clamping at 0. can be negative
    private const string AddOrUpdatePointsQuery = $"INSERT INTO `{TablePoints}` (`{ColumnPointsSteam64}`,`{ColumnPointsFaction}`,`{ColumnPointsSeason}`,`{ColumnPointsXP}`,`{ColumnPointsCredits}`) " +
                                                  $"VALUES (@0,@1,@2,GREATEST(0,@3),GREATEST(0,@4)) AS `new` " +
                                                  $"ON DUPLICATE KEY UPDATE " +
                                                  $"`{TablePoints}`.`{ColumnPointsXP}`=GREATEST(0,`{TablePoints}`.`{ColumnPointsXP}`+`new`.`{ColumnPointsXP}`)," +
                                                  $"`{TablePoints}`.`{ColumnPointsCredits}`=GREATEST(0,`{TablePoints}`.`{ColumnPointsCredits}`+`new`.`{ColumnPointsCredits}`);{GetPointsQuery}";

    // adds XP to the current value, clamping at 0. can be negative
    private const string AddOrUpdateXPQuery = $"INSERT INTO `{TablePoints}` (`{ColumnPointsSteam64}`,`{ColumnPointsFaction}`,`{ColumnPointsSeason}`,`{ColumnPointsXP}`,`{ColumnPointsCredits}`) " +
                                              $"VALUES (@0,@1,@2,GREATEST(0,@3),0) AS `new` " +
                                              $"ON DUPLICATE KEY UPDATE " +
                                              $"`{TablePoints}`.`{ColumnPointsXP}`=GREATEST(0,`{TablePoints}`.`{ColumnPointsXP}`+`new`.`{ColumnPointsXP}`);{GetPointsQuery}";

    // adds credits to the current value, clamping at 0. can be negative
    private const string AddOrUpdateCreditsQuery = $"INSERT INTO `{TablePoints}` (`{ColumnPointsSteam64}`,`{ColumnPointsFaction}`,`{ColumnPointsSeason}`,`{ColumnPointsXP}`,`{ColumnPointsCredits}`) " +
                                                   $"VALUES (@0,@1,@2,0,GREATEST(0,@3)) AS `new` " +
                                                   $"ON DUPLICATE KEY UPDATE " +
                                                   $"`{TablePoints}`.`{ColumnPointsCredits}`=GREATEST(0,`{TablePoints}`.`{ColumnPointsCredits}`+`new`.`{ColumnPointsCredits}`);{GetPointsQuery}";

    // adds reputation to the current value. can be negative
    private const string AddOrUpdateReputationQuery = $"INSERT INTO `{TableReputation}` (`{ColumnReputationSteam64}`,`{ColumnReputationValue}`) " +
                                                      $"VALUES (@0,@1) AS `new` " +
                                                      $"ON DUPLICATE KEY UPDATE " +
                                                      $"`{TableReputation}`.`{ColumnReputationValue}`=`{TableReputation}`.`{ColumnReputationValue}`+`new`.`{ColumnReputationValue}`;{GetReputationQuery}";

    // removes a certain amount of xp and credits only if theres enough of both
    private const string TryRemovePointsQuery = $"UPDATE `{TablePoints} " +
                                                $"SET `{ColumnPointsXP}` = GREATEST(0,`{ColumnPointsXP}`-@3) " +
                                                $"SET `{ColumnPointsCredits}` = GREATEST(0,`{ColumnPointsCredits}`-@4) " +
                                                $"WHERE `{ColumnPointsSteam64}`=@0 AND " +
                                                $"`{ColumnPointsFaction}`=@1 AND " +
                                                $"`{ColumnPointsSeason}`=@2 AND" +
                                                $"`{ColumnPointsXP}`>=@3 AND" +
                                                $"`{ColumnPointsCredits}`>=@4;";

    // removes a certain amount of xp only if theres enough
    private const string TryRemoveXPQuery = $"UPDATE `{TablePoints} " +
                                            $"SET `{ColumnPointsCredits}` = GREATEST(0,`{ColumnPointsCredits}`-@3) " +
                                            $"WHERE `{ColumnPointsSteam64}`=@0 AND " +
                                            $"`{ColumnPointsFaction}`=@1 AND " +
                                            $"`{ColumnPointsSeason}`=@2 AND" +
                                            $"`{ColumnPointsCredits}`>=@3;";

    // removes a certain amount of credits only if theres enough
    private const string TryRemoveCreditsQuery = $"UPDATE `{TablePoints} " +
                                                 $"SET `{ColumnPointsCredits}` = GREATEST(0,`{ColumnPointsCredits}`-@3) " +
                                                 $"WHERE `{ColumnPointsSteam64}`=@0 AND " +
                                                 $"`{ColumnPointsFaction}`=@1 AND " +
                                                 $"`{ColumnPointsSeason}`=@2 AND" +
                                                 $"`{ColumnPointsCredits}`>=@3;";

    public const string TablePoints = "user_points";
    public const string ColumnPointsSteam64 = "Steam64";
    public const string ColumnPointsSeason = "Season";
    public const string ColumnPointsFaction = "Faction";
    public const string ColumnPointsXP = "XP";
    public const string ColumnPointsCredits = "Credits";

    public const string TableReputation = "user_reputation";
    public const string ColumnReputationSteam64 = "Steam64";
    public const string ColumnReputationValue = "Reputation";

    public MySqlPointsStore(IManualMySqlProvider sql, IPlayerService playerService)
    {
        _sql = sql;
        _playerService = playerService;
    }


    /// <inheritdoc />
    public async Task<PlayerPoints> GetPointsAsync(CSteamID player, uint factionId, int season, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();

        if (season < 0)
            throw new ArgumentOutOfRangeException(nameof(season));

        object[] parameters = [ player.m_SteamID, factionId, season ];

        PlayerPoints pts = default;
        await _sql.QueryAsync(GetPointsQuery, parameters, token,
            reader =>
            {
                pts.XP = reader.GetDouble(0);
                pts.Credits = reader.GetDouble(1);
                pts.WasFound = true;
            }
        ).ConfigureAwait(false);

        if (season == WarfareModule.Season)
            OnPointsCacheable(player, factionId, pts.XP, pts.Credits, null);

        return pts;
    }

    /// <inheritdoc />
    public async Task<double> GetCreditsAsync(CSteamID player, uint factionId, int season, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();

        if (season < 0)
            throw new ArgumentOutOfRangeException(nameof(season));

        object[] parameters = [ player.m_SteamID, factionId, season ];

        double credits = 0d;
        await _sql.QueryAsync(GetCreditsQuery, parameters, token,
            reader =>
            {
                credits = reader.GetDouble(0);
            }
        ).ConfigureAwait(false);

        if (season == WarfareModule.Season)
            OnPointsCacheable(player, factionId, null, credits, null);

        return credits;
    }

    /// <inheritdoc />
    public async Task<double> GetXPAsync(CSteamID player, uint factionId, int season, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();

        if (season < 0)
            throw new ArgumentOutOfRangeException(nameof(season));

        object[] parameters = [ player.m_SteamID, factionId, season ];

        double xp = 0d;
        await _sql.QueryAsync(GetXPQuery, parameters, token,
            reader =>
            {
                xp = reader.GetDouble(0);
            }
        ).ConfigureAwait(false);

        if (season == WarfareModule.Season)
            OnPointsCacheable(player, factionId, xp, null, null);

        return xp;
    }

    /// <inheritdoc />
    public async Task<double> GetReputationAsync(CSteamID player, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();

        object[] parameters = [ player.m_SteamID ];

        double reputation = 0d;
        await _sql.QueryAsync(GetReputationQuery, parameters, token,
            reader =>
            {
                reputation = reader.GetDouble(0);
            }
        ).ConfigureAwait(false);

        OnPointsCacheable(player, 0, null, null, reputation);

        return reputation;
    }

    /// <inheritdoc />
    public async Task<PlayerPoints> AddToPointsAsync(CSteamID player, uint factionId, int season, double deltaXp, double deltaCredits, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();

        if (deltaCredits == 0)
        {
            return await AddToXPAsync(player, factionId, season, deltaXp, token);
        }
        if (deltaXp == 0)
        {
            return await AddToCreditsAsync(player, factionId, season, deltaCredits, token);
        }

        if (season < 0)
            throw new ArgumentOutOfRangeException(nameof(season));

        object[] parameters = [ player.m_SteamID, factionId, season, deltaXp, deltaCredits ];

        PlayerPoints pts = default;
        await _sql.QueryAsync(AddOrUpdatePointsQuery, parameters, token,
            reader =>
            {
                pts.XP = reader.GetDouble(0);
                pts.Credits = reader.GetDouble(1);
                pts.WasFound = true;
            }
        ).ConfigureAwait(false);

        if (season == WarfareModule.Season)
            OnPointsCacheable(player, factionId, pts.XP, pts.Credits, null);

        return pts;
    }

    /// <inheritdoc />
    public async Task<PlayerPoints> AddToXPAsync(CSteamID player, uint factionId, int season, double deltaXp, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();

        if (season < 0)
            throw new ArgumentOutOfRangeException(nameof(season));

        object[] parameters = [ player.m_SteamID, factionId, season, deltaXp ];

        PlayerPoints pts = default;
        await _sql.QueryAsync(AddOrUpdateXPQuery, parameters, token,
            reader =>
            {
                pts.XP = reader.GetDouble(0);
                pts.Credits = reader.GetDouble(1);
                pts.WasFound = true;
            }
        ).ConfigureAwait(false);

        if (season == WarfareModule.Season)
            OnPointsCacheable(player, factionId, pts.XP, pts.Credits, null);

        return pts;
    }

    /// <inheritdoc />
    public async Task<PlayerPoints> AddToCreditsAsync(CSteamID player, uint factionId, int season, double deltaCredits, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();

        if (season < 0)
            throw new ArgumentOutOfRangeException(nameof(season));

        object[] parameters = [ player.m_SteamID, factionId, season, deltaCredits ];

        PlayerPoints pts = default;
        await _sql.QueryAsync(AddOrUpdateCreditsQuery, parameters, token,
            reader =>
            {
                pts.XP = reader.GetDouble(0);
                pts.Credits = reader.GetDouble(1);
                pts.WasFound = true;
            }
        ).ConfigureAwait(false);

        if (season == WarfareModule.Season)
            OnPointsCacheable(player, factionId, pts.XP, pts.Credits, null);

        return pts;
    }

    /// <inheritdoc />
    public async Task<double> AddToReputationAsync(CSteamID player, double deltaReputation, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();

        object[] parameters = [ player.m_SteamID, deltaReputation ];

        double rep = 0;
        await _sql.QueryAsync(AddOrUpdateReputationQuery, parameters, token,
            reader =>
            {
                rep = reader.GetDouble(0);
            }
        ).ConfigureAwait(false);

        OnPointsCacheable(player, 0, null, null, rep);

        return rep;
    }

    /// <inheritdoc />
    public Task SetPointsAsync(CSteamID player, uint factionId, int season, double xp, double credits, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();

        if (season < 0)
            throw new ArgumentOutOfRangeException(nameof(season));

        object[] parameters = [ player.m_SteamID, factionId, season, xp, credits ];

        return _sql.NonQueryAsync(SetPointsQuery, parameters, token);
    }

    /// <inheritdoc />
    public async Task SetXPAsync(CSteamID player, uint factionId, int season, double xp, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();

        if (season < 0)
            throw new ArgumentOutOfRangeException(nameof(season));

        object[] parameters = [ player.m_SteamID, factionId, season, xp ];

        await _sql.NonQueryAsync(SetXPQuery, parameters, token).ConfigureAwait(false);

        if (season == WarfareModule.Season)
            OnPointsCacheable(player, factionId, xp, null, null);
    }

    /// <inheritdoc />
    public async Task SetCreditsAsync(CSteamID player, uint factionId, int season, double credits, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();

        if (season < 0)
            throw new ArgumentOutOfRangeException(nameof(season));

        object[] parameters = [ player.m_SteamID, factionId, season, credits ];

        await _sql.NonQueryAsync(SetCreditsQuery, parameters, token).ConfigureAwait(false);

        if (season == WarfareModule.Season)
            OnPointsCacheable(player, factionId, null, credits, null);
    }

    /// <inheritdoc />
    public async Task SetReputationAsync(CSteamID player, double reputation, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();

        object[] parameters = [ player.m_SteamID, reputation ];

        await _sql.NonQueryAsync(SetReputationQuery, parameters, token).ConfigureAwait(false);

        OnPointsCacheable(player, 0, null, null, reputation);
    }

    /// <inheritdoc />
    public async Task<bool> TryRemovePoints(CSteamID player, uint factionId, int season, double xpToRemove, double creditsToRemove, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();

        if (season < 0)
            throw new ArgumentOutOfRangeException(nameof(season));

        object[] parameters = [ player.m_SteamID, factionId, season, xpToRemove, creditsToRemove ];

        bool success = await _sql.NonQueryAsync(TryRemovePointsQuery, parameters, token).ConfigureAwait(false) > 0;
        if (!success)
            return false;

        if (season == WarfareModule.Season && WarfareModule.IsActive && _playerService.IsPlayerOnlineThreadSafe(player))
            await GetPointsAsync(player, factionId, season, token).ConfigureAwait(false);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> TryRemoveXP(CSteamID player, uint factionId, int season, double xpToRemove, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();

        if (season < 0)
            throw new ArgumentOutOfRangeException(nameof(season));
        
        object[] parameters = [ player.m_SteamID, factionId, season, xpToRemove ];

        bool success = await _sql.NonQueryAsync(TryRemoveXPQuery, parameters, token).ConfigureAwait(false) > 0;
        if (!success)
            return false;

        if (season == WarfareModule.Season && WarfareModule.IsActive && _playerService.IsPlayerOnlineThreadSafe(player))
            await GetPointsAsync(player, factionId, season, token).ConfigureAwait(false);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> TryRemoveCredits(CSteamID player, uint factionId, int season, double creditsToRemove, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();

        if (season < 0)
            throw new ArgumentOutOfRangeException(nameof(season));

        object[] parameters = [ player.m_SteamID, factionId, season, creditsToRemove ];

        bool success = await _sql.NonQueryAsync(TryRemoveCreditsQuery, parameters, token).ConfigureAwait(false) > 0;
        if (!success)
            return false;

        if (season == WarfareModule.Season && WarfareModule.IsActive && _playerService.IsPlayerOnlineThreadSafe(player))
            await GetPointsAsync(player, factionId, season, token).ConfigureAwait(false);
        return true;
    }

    protected virtual void OnPointsCacheable(CSteamID player, uint factionId, double? xp, double? creds, double? rep)
    {
        WarfarePlayer? pl = _playerService.GetOnlinePlayerOrNullThreadSafe(player);
        if (pl == null)
            return;

        if (rep.HasValue)
            pl.SetReputation((int)Math.Round(rep.Value));

        if (!xp.HasValue && !creds.HasValue || pl.Team.Faction.PrimaryKey != factionId)
            return;
        
        QueuePointsCacheChange(pl, factionId, xp, creds);
    }

    private static void QueuePointsCacheChange(WarfarePlayer pl, uint factionId, double? xp, double? creds)
    {
        UniTask.Create(async () =>
        {
            await UniTask.SwitchToMainThread();
            if (!pl.IsOnline || pl.Team.Faction.PrimaryKey != factionId)
                return;

            if (xp.HasValue)
                pl.CachedPoints.XP = xp.Value;
            if (creds.HasValue)
                pl.CachedPoints.Credits = creds.Value;
        });
    }
}

/// <summary>
/// Value pair of XP and credits for a player.
/// </summary>
public struct PlayerPoints
{
    public bool WasFound { get; set; }
    public double XP { get; set; }
    public double Credits { get; set; }
}
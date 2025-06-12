using Microsoft.EntityFrameworkCore.Migrations;

namespace Uncreated.Warfare.Migrations
{
    public partial class QueryStatsProcedure : Migration
    {
        public const string ProcedureName = "query_stats";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                $"""
                CREATE PROCEDURE `{ProcedureName}`(
                	IN `player` BIGINT,
                	IN `season` INT,
                	IN `winner_time_threshold` FLOAT
                )
                LANGUAGE SQL
                DETERMINISTIC
                READS SQL DATA
                SQL SECURITY INVOKER
                COMMENT ''
                BEGIN

                CREATE TEMPORARY TABLE IF NOT EXISTS output_tbl (
                	`Faction` INT,
                	`Kills` INT,
                	`Deaths` INT,
                	`Teamkills` INT,
                	`Suicides` INT,
                	`Revives` INT,
                	`Injures` INT,
                	`Damage` FLOAT,
                	`Wins` INT,
                	`Losses` INT,
                	`BuiltFOBs` INT,
                	`DestroyedFOBs` INT,
                	`XP` DOUBLE,
                	`Credits` DOUBLE,
                	`Reputation` DOUBLE,
                	`PlaytimeSeconds` DOUBLE);
                	
                DELETE FROM output_tbl;
                	
                INSERT INTO output_tbl
                	SELECT `Faction`, 0, 0, 0, 0, 0, 0, 0.0, 0, 0, 0, 0, 0.0, 0.0, 0.0, 0.0
                	FROM (
                		SELECT DISTINCT `Faction` FROM stats_sessions
                			WHERE `Steam64` = player AND `Season` = season
                		) f;

                # basically loops through each faction played by the player.
                UPDATE output_tbl o
                	# Kills
                	SET `Kills` = (
                		SELECT COUNT(*) FROM stats_deaths k
                		LEFT JOIN stats_sessions s ON `k`.`InstigatorSession` = `s`.`SessionId`
                		WHERE `IsTeamkill` = 0 AND `IsSuicide` = 0 AND `s`.`Steam64` = player AND `s`.`Season` = season AND `s`.`Faction` <=> `o`.`Faction`
                	),
                	# Deaths
                	`Deaths` = (
                		SELECT COUNT(*) FROM stats_deaths d
                		LEFT JOIN stats_sessions s ON `d`.`Session` = `s`.`SessionId`
                		WHERE `s`.`Steam64` = player AND `s`.`Season` = season AND `s`.`Faction` <=> `o`.`Faction`
                	),
                	# Teamkills
                	`Teamkills` = (
                		SELECT COUNT(*) FROM stats_deaths k
                		LEFT JOIN stats_sessions s ON `k`.`InstigatorSession` = `s`.`SessionId`
                		WHERE `IsTeamkill` = 1 AND `IsSuicide` = 0 AND `s`.`Steam64` = player AND `s`.`Season` = season AND `s`.`Faction` <=> `o`.`Faction`
                	),
                	# Suicides
                	`Suicides` = (
                		SELECT COUNT(*) FROM stats_deaths k
                		LEFT JOIN stats_sessions s ON `k`.`InstigatorSession` = `s`.`SessionId`
                		WHERE `IsSuicide` = 1 AND `s`.`Steam64` = player AND `s`.`Season` = season AND `s`.`Faction` <=> `o`.`Faction`
                	),
                	# Revives
                	`Revives` = (
                		SELECT COUNT(*) FROM stats_aid_records a
                		LEFT JOIN stats_sessions s ON `a`.`Session` = `s`.`SessionId`
                		WHERE `IsRevive` = 1 AND `s`.`Steam64` = player AND `s`.`Season` = season AND `s`.`Faction` <=> `o`.`Faction`
                	),
                	# Injures
                	`Injures` = (
                		SELECT COUNT(*) FROM stats_damage k
                		LEFT JOIN stats_sessions s ON `k`.`InstigatorSession` = `s`.`SessionId`
                		WHERE `IsInjure` = 1 AND `IsTeamkill` = 0 AND `IsSuicide` = 0 AND `s`.`Steam64` = player AND `s`.`Season` = season AND `s`.`Faction` <=> `o`.`Faction`
                	),
                	# Damage
                	`Damage` = (
                		SELECT SUM(`Damage`) FROM stats_damage k
                		LEFT JOIN stats_sessions s ON `k`.`InstigatorSession` = `s`.`SessionId`
                		WHERE `IsTeamkill` = 0 AND `IsSuicide` = 0 AND `s`.`Steam64` = player AND `s`.`Season` = season AND `s`.`Faction` <=> `o`.`Faction`
                	),
                	# Wins (note that this method has a small flaw, or maybe its not idk, but a single game can count towards both a win and a loss if a player played on both sides)
                	`Wins` = IF (
                		`o`.`Faction` IS NULL,
                		0,
                		(
                			SELECT COUNT(*) FROM (
                				SELECT `Game`, `Faction`, SUM(`LengthSeconds`) `Playtime` FROM stats_sessions s
                					WHERE `s`.`Steam64` = player AND `s`.`Season` = season AND `s`.`Faction` = `o`.`Faction`
                					GROUP BY `Game`, `Faction`
                				) r
                				LEFT JOIN stats_games g
                				ON `r`.`Game` = `g`.`GameId`
                				WHERE `g`.`Winner` = `r`.`Faction` AND `Playtime` / TIMESTAMPDIFF(SECOND, `g`.`StartTimestamp`, `g`.`EndTimestamp`) > winner_time_threshold
                		)
                	),
                	# Losses
                	`Losses` = IF (
                		`o`.`Faction` IS NULL,
                		0,
                		(
                			SELECT COUNT(*) FROM (
                				SELECT `Game`, `Faction`, SUM(`LengthSeconds`) `Playtime` FROM stats_sessions s
                					WHERE `s`.`Steam64` = player AND `s`.`Season` = season AND `s`.`Faction` = `o`.`Faction`
                					GROUP BY `Game`, `Faction`
                				) r
                				LEFT JOIN stats_games g
                				ON `r`.`Game` = `g`.`GameId`
                				WHERE `g`.`Winner` != `r`.`Faction` AND `Playtime` / TIMESTAMPDIFF(SECOND, `g`.`StartTimestamp`, `g`.`EndTimestamp`) > winner_time_threshold
                		)
                	),
                	# Built FOBs
                	`BuiltFOBs` = IF (
                		`o`.`Faction` IS NULL,
                		0,
                		(
                			SELECT COUNT(*) FROM stats_fobs f
                			LEFT JOIN stats_sessions s ON `f`.`Session` = `s`.`SessionId`
                			WHERE `s`.`Steam64` = player AND `s`.`Season` = season AND `s`.`Faction` = `o`.`Faction`
                		)
                	),
                	# Destroyed FOBs
                	`DestroyedFOBs` = IF (
                		`o`.`Faction` IS NULL,
                		0,
                		(
                			SELECT COUNT(*) FROM stats_fobs f
                			LEFT JOIN stats_sessions s ON `f`.`InstigatorSession` = `s`.`SessionId`
                			WHERE `s`.`Steam64` = player AND `s`.`Season` = season AND `s`.`Faction` = `o`.`Faction`
                		)
                	),
                	# Points
                	`XP` = IF (
                		`o`.`Faction` IS NULL,
                		0,
                		(
                			SELECT `XP` FROM user_points p
                			WHERE `p`.`Steam64` = player AND `p`.`Season` = season AND `p`.`Faction` = `o`.`Faction`
                			LIMIT 1
                		)
                	),
                	`Credits` = IF (
                		`o`.`Faction` IS NULL,
                		0,
                		(
                			SELECT `Credits` FROM user_points p
                			WHERE `p`.`Steam64` = player AND `p`.`Season` = season AND `p`.`Faction` = `o`.`Faction`
                			LIMIT 1
                		)
                	),
                	`Reputation` = IF (
                		`o`.`Faction` IS NOT NULL,
                		0,
                		(
                			SELECT `Reputation` FROM user_reputation p
                			WHERE `p`.`Steam64` = player
                			LIMIT 1
                		)
                	),
                	# Playtime
                	`PlaytimeSeconds` = (
                		SELECT SUM(`LengthSeconds`) FROM stats_sessions s
                			WHERE `s`.`Steam64` = player AND `s`.`Season` = season AND `s`.`Faction` <=> `o`.`Faction`
                	)
                ;

                SELECT * FROM output_tbl;
                END;
                """
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                                 DROP PROCEDURE {ProcedureName};
                                 """);
        }
    }
}
using Microsoft.EntityFrameworkCore.Migrations;

namespace Uncreated.Warfare.Migrations
{
    public partial class QueryKitStatsProcedure : Migration
    {
        public const string ProcedureName = "query_kit_stats";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Redirect",
                table: "kits_items",
                type: "enum('Shirt','Pants','Vest','Hat','Mask','Backpack','Glasses','AmmoSupply','BuildSupply','RallyPoint','Radio','AmmoBag','AmmoCrate','RepairStation','Bunker','VehicleBay','EntrenchingTool','UAV','RepairStationBuilt','AmmoCrateBuilt','BunkerBuilt','Cache','RadioDamaged','LaserDesignator','MapTackFlag','StandardBuildable')",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "enum('Shirt','Pants','Vest','Hat','Mask','Backpack','Glasses','AmmoSupply','BuildSupply','RallyPoint','Radio','AmmoBag','AmmoCrate','RepairStation','Bunker','VehicleBay','EntrenchingTool','UAV','RepairStationBuilt','AmmoCrateBuilt','BunkerBuilt','Cache','RadioDamaged','LaserDesignator','MapTackFlag')",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Redirect",
                table: "kits_hotkeys",
                type: "enum('Shirt','Pants','Vest','Hat','Mask','Backpack','Glasses','AmmoSupply','BuildSupply','RallyPoint','Radio','AmmoBag','AmmoCrate','RepairStation','Bunker','VehicleBay','EntrenchingTool','UAV','RepairStationBuilt','AmmoCrateBuilt','BunkerBuilt','Cache','RadioDamaged','LaserDesignator','MapTackFlag','StandardBuildable')",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "enum('Shirt','Pants','Vest','Hat','Mask','Backpack','Glasses','AmmoSupply','BuildSupply','RallyPoint','Radio','AmmoBag','AmmoCrate','RepairStation','Bunker','VehicleBay','EntrenchingTool','UAV','RepairStationBuilt','AmmoCrateBuilt','BunkerBuilt','Cache','RadioDamaged','LaserDesignator','MapTackFlag')",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Redirect",
                table: "faction_assets",
                type: "enum('Shirt','Pants','Vest','Hat','Mask','Backpack','Glasses','AmmoSupply','BuildSupply','RallyPoint','Radio','AmmoBag','AmmoCrate','RepairStation','Bunker','VehicleBay','EntrenchingTool','UAV','RepairStationBuilt','AmmoCrateBuilt','BunkerBuilt','Cache','RadioDamaged','LaserDesignator','MapTackFlag','StandardBuildable')",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "enum('Shirt','Pants','Vest','Hat','Mask','Backpack','Glasses','AmmoSupply','BuildSupply','RallyPoint','Radio','AmmoBag','AmmoCrate','RepairStation','Bunker','VehicleBay','EntrenchingTool','UAV','RepairStationBuilt','AmmoCrateBuilt','BunkerBuilt','Cache','RadioDamaged','LaserDesignator','MapTackFlag')")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql(
                $"""
                 CREATE PROCEDURE `{ProcedureName}`(
                 IN `player` BIGINT UNSIGNED,
                 IN `statMask` INT UNSIGNED,
                 IN `season` INT UNSIGNED,
                 IN `kit` INT UNSIGNED
                 )
                 LANGUAGE SQL
                 DETERMINISTIC
                 READS SQL DATA
                 SQL SECURITY INVOKER
                 COMMENT ''
                 BEGIN
                 
                 DROP TEMPORARY TABLE IF EXISTS temp_kit_query_output_tbl;
                 CREATE TEMPORARY TABLE temp_kit_query_output_tbl (
                 	`Kit` INT UNSIGNED,
                 	`Kills` INT,						# 1  (1)
                 	`Deaths` INT,						# 1
                 	`Teamkills` INT,					# 2  (2)
                 	`Suicides` INT,					# 3  (4)
                 	`Damage` DOUBLE,					# 4  (8)
                 	`VehicleKills` INT,				# 5  (16)
                 	`FOBKills` INT,					# 6  (32)
                 	`FOBsBuilt` INT,					# 7  (64)
                 	`Revives` INT,						# 8  (128)
                 	`HealthAided` DOUBLE,			# 9  (256)
                 	`MeleeKills` INT,					# 10 (512)
                 	`AverageKillDistance` DOUBLE,	# 11 (1024)
                 	`HighestKillDistance` DOUBLE, # 12 (2048)
                 	`KillsWithVehicle` INT,			# 13 (4096)
                 	`Playtime` DOUBLE 				# 14 (8192)
                 );
                 
                 IF (statMask & (1 | 2 | 4 | 512 | 1024 | 2048 | 4096)) != 0 THEN
                 	# store all kills in a table
                 	DROP TEMPORARY TABLE IF EXISTS temp_kit_query_kills;
                 	CREATE TEMPORARY TABLE temp_kit_query_kills
                 		SELECT
                 			`s`.`SessionId` AS `SessionId`,
                 			`d`.`DeathCause` AS `DeathCause`,
                 			`d`.`Distance` AS `Distance`,
                 			`d`.`IsTeamkill` AS `IsTeamkill`,
                 			`d`.`IsSuicide` AS `IsSuicide`,
                 			`d`.`SecondaryAsset` AS `SecondaryAsset`,
                 			`s`.`Kit` AS `Kit`
                 		FROM `stats_deaths` AS `d`
                 		JOIN `stats_sessions` AS `s` ON `d`.`InstigatorSession` = `s`.`SessionId`
                 		WHERE `s`.`Steam64` = player AND `s`.`Season` = season AND (kit = 0 OR `s`.`Kit` <=> kit);
                 END IF;
                 
                 IF kit IS NOT NULL AND kit != 0 THEN
                 
                 	INSERT INTO temp_kit_query_output_tbl
                 		(`Kit`, `Kills`, `Deaths`, `Teamkills`, `Suicides`, `Damage`, `VehicleKills`, `FOBKills`, `FOBsBuilt`, `Revives`, `HealthAided`, `MeleeKills`, `AverageKillDistance`, `HighestKillDistance`, `KillsWithVehicle`, `Playtime`)
                 		VALUES (kit, 0, 0, 0, 0, 0.0, 0, 0, 0, 0, 0.0, 0, 0.0, 0.0, 0, 0.0);
                 		
                 ELSE
                 
                 	INSERT INTO temp_kit_query_output_tbl
                 		SELECT `k`.`Kit`, 0, 0, 0, 0, 0.0, 0, 0, 0, 0, 0.0, 0, 0.0, 0.0, 0, 0.0
                 		FROM (
                 			SELECT DISTINCT `s`.`Kit` FROM `stats_sessions` AS `s`
                 				WHERE `Steam64` = player AND `Season` = season AND (kit IS NOT NULL OR `s`.`Kit` IS NULL)
                 			) AS `k`;
                 			
                 END IF;
                 		
                 IF (statMask & 1) = 1 THEN
                 	UPDATE `temp_kit_query_output_tbl` AS `o`
                 		SET `Kills` = COALESCE((
                 			SELECT COUNT(*) FROM `temp_kit_query_kills` AS `k`
                 			WHERE `k`.`Kit` <=> `o`.`Kit` AND `k`.`IsTeamkill` = 0 AND `k`.`IsSuicide` = 0
                 		), 0),
                 		`Deaths` = COALESCE((
                 			SELECT COUNT(*) FROM `stats_deaths` AS `d`
                 			JOIN `stats_sessions` AS `s` ON `d`.`Session` = `s`.`SessionId`
                 			WHERE `s`.`Steam64` = player AND `s`.`Season` = season AND `s`.`Kit` <=> `o`.`Kit` AND `d`.`IsTeamkill` = 0
                 		), 0);
                 END IF;
                 
                 IF (statMask & 2) = 2 THEN
                 	UPDATE `temp_kit_query_output_tbl` AS `o`
                 		SET `Teamkills` = COALESCE((
                 			SELECT COUNT(*) FROM `temp_kit_query_kills` AS `k`
                 			WHERE `k`.`Kit` <=> `o`.`Kit` AND `k`.`IsTeamkill` = 1 AND `k`.`IsSuicide` = 0
                 		), 0);
                 END IF;
                 
                 IF (statMask & 4) = 4 THEN
                 	UPDATE `temp_kit_query_output_tbl` AS `o`
                 		SET `Suicides` = COALESCE((
                 			SELECT COUNT(*) FROM `temp_kit_query_kills` AS `k`
                 			WHERE `k`.`Kit` <=> `o`.`Kit` AND `k`.`IsSuicide` = 1
                 		), 0);
                 END IF;
                 
                 IF (statMask & 8) = 8 THEN
                 	UPDATE `temp_kit_query_output_tbl` AS `o`
                 		SET `Damage` = COALESCE((
                 			SELECT SUM(CAST(`Damage` AS DOUBLE)) FROM `stats_damage` AS `d`
                 			JOIN `stats_sessions` AS `s` ON `d`.`Session` = `s`.`SessionId`
                 			WHERE `s`.`Steam64` = player AND `s`.`Season` = season AND `s`.`Kit` <=> `o`.`Kit` AND `d`.`IsTeamkill` = 0
                 		), 0);
                 END IF;
                 
                 # TODO: vehicle kills (16)
                 
                 IF (statMask & 32) = 32 THEN
                 	UPDATE `temp_kit_query_output_tbl` AS `o`
                 		SET `FOBKills` = COALESCE((
                 			SELECT COUNT(*) FROM `stats_fobs` AS `f`
                 			JOIN `stats_sessions` AS `s` ON `f`.`InstigatorSession` = `s`.`SessionId`
                 			WHERE `s`.`Steam64` = player AND `s`.`Season` = season AND `s`.`Kit` <=> `o`.`Kit` AND `f`.`Teamkilled` = 0 AND `f`.`DestroyedByRoundEnd` = 0
                 		), 0);
                 END IF;
                 
                 IF (statMask & 64) = 64 THEN
                 	UPDATE `temp_kit_query_output_tbl` AS `o`
                 		SET `FOBsBuilt` = COALESCE((
                 			SELECT COUNT(*) FROM `stats_fobs` AS `f`
                 			JOIN `stats_sessions` AS `s` ON `f`.`Session` = `s`.`SessionId`
                 			WHERE `s`.`Steam64` = player AND `s`.`Season` = season AND `s`.`Kit` <=> `o`.`Kit`
                 		), 0);
                 END IF;
                 
                 IF (statMask & 128) = 128 THEN
                 	UPDATE `temp_kit_query_output_tbl` AS `o`
                 		SET `Revives` = COALESCE((
                 			SELECT COUNT(*) FROM `stats_aid_records` AS `a`
                 			JOIN `stats_sessions` AS `s` ON `a`.`InstigatorSession` = `s`.`SessionId`
                 			WHERE `s`.`Steam64` = player AND `s`.`Season` = season AND `s`.`Kit` <=> `o`.`Kit` AND `a`.`IsRevive` = 1
                 		), 0);
                 END IF;
                 
                 IF (statMask & 256) = 256 THEN
                 	UPDATE `temp_kit_query_output_tbl` AS `o`
                 		SET `HealthAided` = COALESCE((
                 			SELECT SUM(ABS(CAST(`Damage` AS DOUBLE))) FROM `stats_aid_records` AS `a`
                 			JOIN `stats_sessions` AS `s` ON `a`.`InstigatorSession` = `s`.`SessionId`
                 			WHERE `s`.`Steam64` = player AND `s`.`Season` = season AND `s`.`Kit` <=> `o`.`Kit`
                 		), 0);
                 END IF;
                 
                 IF (statMask & 512) = 512 THEN
                 	UPDATE `temp_kit_query_output_tbl` AS `o`
                 		SET `MeleeKills` = COALESCE((
                 			SELECT COUNT(*) FROM `temp_kit_query_kills` AS `k`
                 			WHERE `k`.`Kit` <=> `o`.`Kit` AND `k`.`IsTeamkill` = 0 AND `k`.`IsSuicide` = 0 AND `k`.`DeathCause` IN ("MELEE", "PUNCH")
                 		), 0);
                 END IF;
                 
                 IF (statMask & 1024) = 1024 THEN
                 	UPDATE `temp_kit_query_output_tbl` AS `o`
                 		SET `AverageKillDistance` = COALESCE((
                 			SELECT AVG(`Distance`) FROM `temp_kit_query_kills` AS `k`
                 			WHERE `k`.`Kit` <=> `o`.`Kit` AND `k`.`IsTeamkill` = 0 AND `k`.`IsSuicide` = 0 AND `k`.`DeathCause` IN ("GUN", "SPLASH")
                 		), 0);
                 END IF;
                 
                 IF (statMask & 2048) = 2048 THEN
                 	UPDATE `temp_kit_query_output_tbl` AS `o`
                 		SET `HighestKillDistance` = COALESCE((
                 			SELECT MAX(`Distance`) FROM `temp_kit_query_kills` AS `k`
                 			WHERE `k`.`Kit` <=> `o`.`Kit` AND `k`.`IsTeamkill` = 0 AND `k`.`IsSuicide` = 0 AND `k`.`DeathCause` IN ("GUN", "SPLASH")
                 		), 0);
                 END IF;
                 
                 IF (statMask & 4096) = 4096 THEN
                 	UPDATE `temp_kit_query_output_tbl` AS `o`
                 		SET `KillsWithVehicle` = COALESCE((
                 			SELECT COUNT(*) FROM `temp_kit_query_kills` AS `k`
                 			WHERE `k`.`Kit` <=> `o`.`Kit` AND `k`.`IsTeamkill` = 0 AND `k`.`IsSuicide` = 0 AND (`k`.`DeathCause` IN ("VEHICLE", "ROADKILL") OR (`k`.`DeathCause` IN ("MISSILE", "SPLASH", "GUN") AND `k`.`SecondaryAsset` IS NOT NULL AND `k`.`SecondaryAsset` != ""))
                 		), 0);
                 END IF;
                 
                 IF (statMask & 8192) = 8192 THEN
                 	UPDATE `temp_kit_query_output_tbl` AS `o`
                 		SET `Playtime` = COALESCE((
                 			SELECT SUM(`LengthSeconds`) FROM `stats_sessions` AS `s`
                 			WHERE `s`.`Steam64` = player AND `s`.`Season` = season AND `s`.`Kit` <=> `o`.`Kit`
                 		), 0);
                 END IF;
                 
                 SELECT * FROM temp_kit_query_output_tbl;
                 DROP TEMPORARY TABLE IF EXISTS temp_kit_query_kills;
                 DROP TEMPORARY TABLE IF EXISTS temp_kit_query_output_tbl;
                 END
                 """
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                                  DROP PROCEDURE {ProcedureName};
                                  """);

            migrationBuilder.AlterColumn<string>(
                name: "Redirect",
                table: "kits_items",
                type: "enum('Shirt','Pants','Vest','Hat','Mask','Backpack','Glasses','AmmoSupply','BuildSupply','RallyPoint','Radio','AmmoBag','AmmoCrate','RepairStation','Bunker','VehicleBay','EntrenchingTool','UAV','RepairStationBuilt','AmmoCrateBuilt','BunkerBuilt','Cache','RadioDamaged','LaserDesignator','MapTackFlag')",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "enum('Shirt','Pants','Vest','Hat','Mask','Backpack','Glasses','AmmoSupply','BuildSupply','RallyPoint','Radio','AmmoBag','AmmoCrate','RepairStation','Bunker','VehicleBay','EntrenchingTool','UAV','RepairStationBuilt','AmmoCrateBuilt','BunkerBuilt','Cache','RadioDamaged','LaserDesignator','MapTackFlag','StandardBuildable')",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Redirect",
                table: "kits_hotkeys",
                type: "enum('Shirt','Pants','Vest','Hat','Mask','Backpack','Glasses','AmmoSupply','BuildSupply','RallyPoint','Radio','AmmoBag','AmmoCrate','RepairStation','Bunker','VehicleBay','EntrenchingTool','UAV','RepairStationBuilt','AmmoCrateBuilt','BunkerBuilt','Cache','RadioDamaged','LaserDesignator','MapTackFlag')",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "enum('Shirt','Pants','Vest','Hat','Mask','Backpack','Glasses','AmmoSupply','BuildSupply','RallyPoint','Radio','AmmoBag','AmmoCrate','RepairStation','Bunker','VehicleBay','EntrenchingTool','UAV','RepairStationBuilt','AmmoCrateBuilt','BunkerBuilt','Cache','RadioDamaged','LaserDesignator','MapTackFlag','StandardBuildable')",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Redirect",
                table: "faction_assets",
                type: "enum('Shirt','Pants','Vest','Hat','Mask','Backpack','Glasses','AmmoSupply','BuildSupply','RallyPoint','Radio','AmmoBag','AmmoCrate','RepairStation','Bunker','VehicleBay','EntrenchingTool','UAV','RepairStationBuilt','AmmoCrateBuilt','BunkerBuilt','Cache','RadioDamaged','LaserDesignator','MapTackFlag')",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "enum('Shirt','Pants','Vest','Hat','Mask','Backpack','Glasses','AmmoSupply','BuildSupply','RallyPoint','Radio','AmmoBag','AmmoCrate','RepairStation','Bunker','VehicleBay','EntrenchingTool','UAV','RepairStationBuilt','AmmoCrateBuilt','BunkerBuilt','Cache','RadioDamaged','LaserDesignator','MapTackFlag','StandardBuildable')")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}

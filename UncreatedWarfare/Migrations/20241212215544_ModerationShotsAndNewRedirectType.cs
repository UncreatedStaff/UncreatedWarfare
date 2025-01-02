using Microsoft.EntityFrameworkCore.Migrations;
using Uncreated.Warfare.Moderation;

namespace Uncreated.Warfare.Migrations
{
    public partial class ModerationShotsAndNewRedirectType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Redirect",
                table: "kits_items",
                type: "enum('Shirt','Pants','Vest','Hat','Mask','Backpack','Glasses','AmmoSupply','BuildSupply','RallyPoint','Radio','AmmoBag','AmmoCrate','RepairStation','Bunker','EntrenchingTool','UAV','RepairStationBuilt','AmmoCrateBuilt','BunkerBuilt','Cache','RadioDamaged','LaserDesignator','MapTackFlag')",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "enum('Shirt','Pants','Vest','Hat','Mask','Backpack','Glasses','AmmoSupply','BuildSupply','RallyPoint','Radio','AmmoBag','AmmoCrate','RepairStation','Bunker','EntrenchingTool','UAV','RepairStationBuilt','AmmoCrateBuilt','BunkerBuilt','Cache','RadioDamaged','LaserDesignator')",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Redirect",
                table: "kits_hotkeys",
                type: "enum('Shirt','Pants','Vest','Hat','Mask','Backpack','Glasses','AmmoSupply','BuildSupply','RallyPoint','Radio','AmmoBag','AmmoCrate','RepairStation','Bunker','EntrenchingTool','UAV','RepairStationBuilt','AmmoCrateBuilt','BunkerBuilt','Cache','RadioDamaged','LaserDesignator','MapTackFlag')",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "enum('Shirt','Pants','Vest','Hat','Mask','Backpack','Glasses','AmmoSupply','BuildSupply','RallyPoint','Radio','AmmoBag','AmmoCrate','RepairStation','Bunker','EntrenchingTool','UAV','RepairStationBuilt','AmmoCrateBuilt','BunkerBuilt','Cache','RadioDamaged','LaserDesignator')",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Redirect",
                table: "faction_assets",
                type: "enum('Shirt','Pants','Vest','Hat','Mask','Backpack','Glasses','AmmoSupply','BuildSupply','RallyPoint','Radio','AmmoBag','AmmoCrate','RepairStation','Bunker','EntrenchingTool','UAV','RepairStationBuilt','AmmoCrateBuilt','BunkerBuilt','Cache','RadioDamaged','LaserDesignator','MapTackFlag')",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "enum('Shirt','Pants','Vest','Hat','Mask','Backpack','Glasses','AmmoSupply','BuildSupply','RallyPoint','Radio','AmmoBag','AmmoCrate','RepairStation','Bunker','EntrenchingTool','UAV','RepairStationBuilt','AmmoCrateBuilt','BunkerBuilt','Cache','RadioDamaged','LaserDesignator')")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<byte[]>(
                name: DatabaseInterface.ColumnReportsScreenshotData,
                table: DatabaseInterface.TableReports,
                type: "BLOB",
                nullable: true,
                defaultValue: null);

            migrationBuilder.AlterColumn<string>(
                    name: DatabaseInterface.ColumnReportsShotRecordHitType,
                    table: DatabaseInterface.TableReportShotRecords,
                    type: "enum('NONE','SKIP','OBJECT','PLAYER','ZOMBIE','ANIMAL','VEHICLE','BARRICADE','STRUCTURE','RESOURCE')",
                    nullable: false,
                    oldClrType: typeof(string),
                    oldType: "enum('NONE','ENTITIY','CRITICAL','BUILD','GHOST')")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Redirect",
                table: "kits_items",
                type: "enum('Shirt','Pants','Vest','Hat','Mask','Backpack','Glasses','AmmoSupply','BuildSupply','RallyPoint','Radio','AmmoBag','AmmoCrate','RepairStation','Bunker','EntrenchingTool','UAV','RepairStationBuilt','AmmoCrateBuilt','BunkerBuilt','Cache','RadioDamaged','LaserDesignator')",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "enum('Shirt','Pants','Vest','Hat','Mask','Backpack','Glasses','AmmoSupply','BuildSupply','RallyPoint','Radio','AmmoBag','AmmoCrate','RepairStation','Bunker','EntrenchingTool','UAV','RepairStationBuilt','AmmoCrateBuilt','BunkerBuilt','Cache','RadioDamaged','LaserDesignator','MapTackFlag')",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Redirect",
                table: "kits_hotkeys",
                type: "enum('Shirt','Pants','Vest','Hat','Mask','Backpack','Glasses','AmmoSupply','BuildSupply','RallyPoint','Radio','AmmoBag','AmmoCrate','RepairStation','Bunker','EntrenchingTool','UAV','RepairStationBuilt','AmmoCrateBuilt','BunkerBuilt','Cache','RadioDamaged','LaserDesignator')",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "enum('Shirt','Pants','Vest','Hat','Mask','Backpack','Glasses','AmmoSupply','BuildSupply','RallyPoint','Radio','AmmoBag','AmmoCrate','RepairStation','Bunker','EntrenchingTool','UAV','RepairStationBuilt','AmmoCrateBuilt','BunkerBuilt','Cache','RadioDamaged','LaserDesignator','MapTackFlag')",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Redirect",
                table: "faction_assets",
                type: "enum('Shirt','Pants','Vest','Hat','Mask','Backpack','Glasses','AmmoSupply','BuildSupply','RallyPoint','Radio','AmmoBag','AmmoCrate','RepairStation','Bunker','EntrenchingTool','UAV','RepairStationBuilt','AmmoCrateBuilt','BunkerBuilt','Cache','RadioDamaged','LaserDesignator')",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "enum('Shirt','Pants','Vest','Hat','Mask','Backpack','Glasses','AmmoSupply','BuildSupply','RallyPoint','Radio','AmmoBag','AmmoCrate','RepairStation','Bunker','EntrenchingTool','UAV','RepairStationBuilt','AmmoCrateBuilt','BunkerBuilt','Cache','RadioDamaged','LaserDesignator','MapTackFlag')")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.DropColumn(
                name: DatabaseInterface.ColumnReportsScreenshotData,
                table: DatabaseInterface.TableReports);

            migrationBuilder.AlterColumn<string>(
                    name: DatabaseInterface.ColumnReportsShotRecordHitType,
                    table: DatabaseInterface.TableReportShotRecords,
                    type: "enum('NONE','ENTITIY','CRITICAL','BUILD','GHOST')",
                    nullable: false,
                    oldClrType: typeof(string),
                    oldType: "enum('NONE','SKIP','OBJECT','PLAYER','ZOMBIE','ANIMAL','VEHICLE','BARRICADE','STRUCTURE','RESOURCE')")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}

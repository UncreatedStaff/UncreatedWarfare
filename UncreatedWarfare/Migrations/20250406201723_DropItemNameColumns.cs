using Microsoft.EntityFrameworkCore.Migrations;

namespace Uncreated.Warfare.Migrations
{
    public partial class DropItemNameColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NearestLocation",
                table: "stats_fobs");

            migrationBuilder.DropColumn(
                name: "PrimaryAssetName",
                table: "stats_fobs");

            migrationBuilder.DropColumn(
                name: "SecondaryAssetName",
                table: "stats_fobs");

            migrationBuilder.DropColumn(
                name: "NearestLocation",
                table: "stats_fob_items_builders");

            migrationBuilder.DropColumn(
                name: "ItemName",
                table: "stats_fob_items");

            migrationBuilder.DropColumn(
                name: "NearestLocation",
                table: "stats_fob_items");

            migrationBuilder.DropColumn(
                name: "PrimaryAssetName",
                table: "stats_fob_items");

            migrationBuilder.DropColumn(
                name: "SecondaryAssetName",
                table: "stats_fob_items");

            migrationBuilder.DropColumn(
                name: "NearestLocation",
                table: "stats_deaths");

            migrationBuilder.DropColumn(
                name: "PrimaryAssetName",
                table: "stats_deaths");

            migrationBuilder.DropColumn(
                name: "SecondaryAssetName",
                table: "stats_deaths");

            migrationBuilder.DropColumn(
                name: "VehicleName",
                table: "stats_deaths");

            migrationBuilder.DropColumn(
                name: "NearestLocation",
                table: "stats_damage");

            migrationBuilder.DropColumn(
                name: "PrimaryAssetName",
                table: "stats_damage");

            migrationBuilder.DropColumn(
                name: "SecondaryAssetName",
                table: "stats_damage");

            migrationBuilder.DropColumn(
                name: "VehicleName",
                table: "stats_damage");

            migrationBuilder.DropColumn(
                name: "ItemName",
                table: "stats_aid_records");

            migrationBuilder.DropColumn(
                name: "NearestLocation",
                table: "stats_aid_records");

            migrationBuilder.AlterColumn<string>(
                name: "DeathMessage",
                table: "stats_deaths",
                type: "varchar(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(256)",
                oldMaxLength: 256)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NearestLocation",
                table: "stats_fobs",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PrimaryAssetName",
                table: "stats_fobs",
                type: "varchar(48)",
                maxLength: 48,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SecondaryAssetName",
                table: "stats_fobs",
                type: "varchar(48)",
                maxLength: 48,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "NearestLocation",
                table: "stats_fob_items_builders",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ItemName",
                table: "stats_fob_items",
                type: "varchar(48)",
                maxLength: 48,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "NearestLocation",
                table: "stats_fob_items",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PrimaryAssetName",
                table: "stats_fob_items",
                type: "varchar(48)",
                maxLength: 48,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SecondaryAssetName",
                table: "stats_fob_items",
                type: "varchar(48)",
                maxLength: 48,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "DeathMessage",
                table: "stats_deaths",
                type: "varchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(512)",
                oldMaxLength: 512)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "NearestLocation",
                table: "stats_deaths",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PrimaryAssetName",
                table: "stats_deaths",
                type: "varchar(48)",
                maxLength: 48,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SecondaryAssetName",
                table: "stats_deaths",
                type: "varchar(48)",
                maxLength: 48,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "VehicleName",
                table: "stats_deaths",
                type: "varchar(48)",
                maxLength: 48,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "NearestLocation",
                table: "stats_damage",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PrimaryAssetName",
                table: "stats_damage",
                type: "varchar(48)",
                maxLength: 48,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SecondaryAssetName",
                table: "stats_damage",
                type: "varchar(48)",
                maxLength: 48,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "VehicleName",
                table: "stats_damage",
                type: "varchar(48)",
                maxLength: 48,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ItemName",
                table: "stats_aid_records",
                type: "varchar(48)",
                maxLength: 48,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "NearestLocation",
                table: "stats_aid_records",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}

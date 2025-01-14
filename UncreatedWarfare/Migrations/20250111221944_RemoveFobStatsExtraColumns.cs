using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using Uncreated.Warfare.Moderation;

namespace Uncreated.Warfare.Migrations
{
    public partial class RemoveFobStatsExtraColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FobNumber",
                table: "stats_fobs");

            migrationBuilder.DropColumn(
                name: "FobPositionX",
                table: "stats_fobs");

            migrationBuilder.DropColumn(
                name: "FobPositionY",
                table: "stats_fobs");

            migrationBuilder.DropColumn(
                name: "FobPositionZ",
                table: "stats_fobs");

            migrationBuilder.AlterColumn<string>(
                name: "FobType",
                table: "stats_fobs",
                type: "enum('Other','BunkerFob','Cache')",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "enum('Other','RadioFob','SpecialFob','Cache')")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "FobType",
                table: "stats_fobs",
                type: "enum('Other','RadioFob','SpecialFob','Cache')",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "enum('Other','BunkerFob','Cache')")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "FobNumber",
                table: "stats_fobs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<float>(
                name: "FobPositionX",
                table: "stats_fobs",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "FobPositionY",
                table: "stats_fobs",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "FobPositionZ",
                table: "stats_fobs",
                type: "float",
                nullable: false,
                defaultValue: 0f);
        }
    }
}

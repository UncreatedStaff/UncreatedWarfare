using Microsoft.EntityFrameworkCore.Migrations;

namespace Uncreated.Warfare.Migrations
{
    public partial class SessionFixOnDeleteConstraints : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_stats_sessions_factions_Faction",
                table: "stats_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_stats_sessions_kits_Kit",
                table: "stats_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_stats_sessions_maps_Map",
                table: "stats_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_stats_sessions_seasons_Season",
                table: "stats_sessions");

            migrationBuilder.AddColumn<string>(
                name: "KitName",
                table: "stats_sessions",
                maxLength: 25,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_stats_sessions_factions_Faction",
                table: "stats_sessions",
                column: "Faction",
                principalTable: "factions",
                principalColumn: "pk");

            migrationBuilder.AddForeignKey(
                name: "FK_stats_sessions_kits_Kit",
                table: "stats_sessions",
                column: "Kit",
                principalTable: "kits",
                principalColumn: "pk");

            migrationBuilder.AddForeignKey(
                name: "FK_stats_sessions_maps_Map",
                table: "stats_sessions",
                column: "Map",
                principalTable: "maps",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_stats_sessions_seasons_Season",
                table: "stats_sessions",
                column: "Season",
                principalTable: "seasons",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_stats_sessions_factions_Faction",
                table: "stats_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_stats_sessions_kits_Kit",
                table: "stats_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_stats_sessions_maps_Map",
                table: "stats_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_stats_sessions_seasons_Season",
                table: "stats_sessions");

            migrationBuilder.DropColumn(
                name: "KitName",
                table: "stats_sessions");

            migrationBuilder.AddForeignKey(
                name: "FK_stats_sessions_factions_Faction",
                table: "stats_sessions",
                column: "Faction",
                principalTable: "factions",
                principalColumn: "pk",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stats_sessions_kits_Kit",
                table: "stats_sessions",
                column: "Kit",
                principalTable: "kits",
                principalColumn: "pk",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stats_sessions_maps_Map",
                table: "stats_sessions",
                column: "Map",
                principalTable: "maps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_stats_sessions_seasons_Season",
                table: "stats_sessions",
                column: "Season",
                principalTable: "seasons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

namespace Uncreated.Warfare.Migrations
{
    public partial class SessionFixOnDeleteConstraints2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_stats_sessions_factions_Faction",
                table: "stats_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_stats_sessions_stats_games_Game",
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

            migrationBuilder.DropForeignKey(
                name: "FK_stats_sessions_users_Steam64",
                table: "stats_sessions");

            migrationBuilder.AddForeignKey(
                name: "FK_stats_sessions_factions_Faction",
                table: "stats_sessions",
                column: "Faction",
                principalTable: "factions",
                principalColumn: "pk",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stats_sessions_stats_games_Game",
                table: "stats_sessions",
                column: "Game",
                principalTable: "stats_games",
                principalColumn: "GameId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stats_sessions_kits_Kit",
                table: "stats_sessions",
                column: "Kit",
                principalTable: "kits",
                principalColumn: "pk",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_stats_sessions_maps_Map",
                table: "stats_sessions",
                column: "Map",
                principalTable: "maps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stats_sessions_seasons_Season",
                table: "stats_sessions",
                column: "Season",
                principalTable: "seasons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stats_sessions_users_Steam64",
                table: "stats_sessions",
                column: "Steam64",
                principalTable: "users",
                principalColumn: "Steam64",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_stats_sessions_factions_Faction",
                table: "stats_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_stats_sessions_stats_games_Game",
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

            migrationBuilder.DropForeignKey(
                name: "FK_stats_sessions_users_Steam64",
                table: "stats_sessions");

            migrationBuilder.AddForeignKey(
                name: "FK_stats_sessions_factions_Faction",
                table: "stats_sessions",
                column: "Faction",
                principalTable: "factions",
                principalColumn: "pk");

            migrationBuilder.AddForeignKey(
                name: "FK_stats_sessions_stats_games_Game",
                table: "stats_sessions",
                column: "Game",
                principalTable: "stats_games",
                principalColumn: "GameId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_stats_sessions_users_Steam64",
                table: "stats_sessions",
                column: "Steam64",
                principalTable: "users",
                principalColumn: "Steam64");
        }
    }
}

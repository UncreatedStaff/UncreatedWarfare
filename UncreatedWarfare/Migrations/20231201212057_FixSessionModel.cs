using Microsoft.EntityFrameworkCore.Migrations;

namespace Uncreated.Warfare.Migrations
{
    public partial class FixSessionModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_stats_sessions_users_PlayerDataSteam64",
                table: "stats_sessions");

            migrationBuilder.DropIndex(
                name: "IX_stats_sessions_PlayerDataSteam64",
                table: "stats_sessions");

            migrationBuilder.DropColumn(
                name: "PlayerDataSteam64",
                table: "stats_sessions");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "PlayerDataSteam64",
                table: "stats_sessions",
                type: "bigint unsigned",
                nullable: false,
                defaultValue: 0ul);

            migrationBuilder.CreateIndex(
                name: "IX_stats_sessions_PlayerDataSteam64",
                table: "stats_sessions",
                column: "PlayerDataSteam64");

            migrationBuilder.AddForeignKey(
                name: "FK_stats_sessions_users_PlayerDataSteam64",
                table: "stats_sessions",
                column: "PlayerDataSteam64",
                principalTable: "users",
                principalColumn: "Steam64",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

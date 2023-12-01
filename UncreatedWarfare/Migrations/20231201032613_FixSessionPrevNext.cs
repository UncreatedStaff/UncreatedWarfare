using Microsoft.EntityFrameworkCore.Migrations;

namespace Uncreated.Warfare.Migrations
{
    public partial class FixSessionPrevNext : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "DiscordId",
                table: "users",
                nullable: false,
                defaultValue: 0ul);

            migrationBuilder.CreateIndex(
                name: "IX_stats_sessions_NextSession",
                table: "stats_sessions",
                column: "NextSession",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_stats_sessions_stats_sessions_NextSession",
                table: "stats_sessions",
                column: "NextSession",
                principalTable: "stats_sessions",
                principalColumn: "SessionId",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_stats_sessions_stats_sessions_NextSession",
                table: "stats_sessions");

            migrationBuilder.DropIndex(
                name: "IX_stats_sessions_NextSession",
                table: "stats_sessions");

            migrationBuilder.DropColumn(
                name: "DiscordId",
                table: "users");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

namespace Uncreated.Warfare.Migrations
{
    public partial class AddKillShot : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsBleedout",
                table: "stats_deaths",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<ulong>(
                name: "KillShot",
                table: "stats_deaths",
                nullable: true);

            migrationBuilder.AddColumn<ulong>(
                name: "DeathResult",
                table: "stats_damage",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_stats_deaths_KillShot",
                table: "stats_deaths",
                column: "KillShot",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_stats_deaths_stats_damage_KillShot",
                table: "stats_deaths",
                column: "KillShot",
                principalTable: "stats_damage",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_stats_deaths_stats_damage_KillShot",
                table: "stats_deaths");

            migrationBuilder.DropIndex(
                name: "IX_stats_deaths_KillShot",
                table: "stats_deaths");

            migrationBuilder.DropColumn(
                name: "IsBleedout",
                table: "stats_deaths");

            migrationBuilder.DropColumn(
                name: "KillShot",
                table: "stats_deaths");

            migrationBuilder.DropColumn(
                name: "DeathResult",
                table: "stats_damage");
        }
    }
}

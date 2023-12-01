using Microsoft.EntityFrameworkCore.Migrations;

namespace Uncreated.Warfare.Migrations
{
    public partial class AddGamemode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Gamemode",
                table: "stats_games",
                type: "enum('Undefined','TeamCTF','Invasion','Insurgency','Conquest','Hardpoint','Deathmatch')",
                nullable: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Gamemode",
                table: "stats_games");
        }
    }
}

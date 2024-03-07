using Microsoft.EntityFrameworkCore.Migrations;

namespace Uncreated.Warfare.Migrations
{
    public partial class RemoveDeathResultColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeathResult",
                table: "stats_damage");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "DeathResult",
                table: "stats_damage",
                type: "bigint unsigned",
                nullable: true);
        }
    }
}

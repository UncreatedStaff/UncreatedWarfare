using Microsoft.EntityFrameworkCore.Migrations;

namespace Uncreated.Warfare.Migrations
{
    public partial class AddStatsActually4 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PrimaryAssetName",
                table: "stats_damage",
                maxLength: 48,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondaryAssetName",
                table: "stats_damage",
                maxLength: 48,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ItemName",
                table: "stats_aid_records",
                maxLength: 48,
                nullable: false,
                defaultValue: "00000000000000000000000000000000");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrimaryAssetName",
                table: "stats_damage");

            migrationBuilder.DropColumn(
                name: "SecondaryAssetName",
                table: "stats_damage");

            migrationBuilder.DropColumn(
                name: "ItemName",
                table: "stats_aid_records");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

namespace Uncreated.Warfare.Migrations
{
    public partial class UpdateFactionNullability : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_kits_bundles_factions_Faction",
                table: "kits_bundles");

            migrationBuilder.AlterColumn<uint>(
                name: "Faction",
                table: "kits_bundles",
                nullable: true,
                oldClrType: typeof(uint),
                oldType: "int unsigned");

            migrationBuilder.AddForeignKey(
                name: "FK_kits_bundles_factions_Faction",
                table: "kits_bundles",
                column: "Faction",
                principalTable: "factions",
                principalColumn: "pk",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_kits_bundles_factions_Faction",
                table: "kits_bundles");

            migrationBuilder.AlterColumn<uint>(
                name: "Faction",
                table: "kits_bundles",
                type: "int unsigned",
                nullable: false,
                oldClrType: typeof(uint),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_kits_bundles_factions_Faction",
                table: "kits_bundles",
                column: "Faction",
                principalTable: "factions",
                principalColumn: "pk",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

namespace Uncreated.Warfare.Migrations
{
    public partial class UpdateFactionCascadeMode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_kits_bundles_factions_Faction",
                table: "kits_bundles");

            migrationBuilder.AddForeignKey(
                name: "FK_kits_bundles_factions_Faction",
                table: "kits_bundles",
                column: "Faction",
                principalTable: "factions",
                principalColumn: "pk",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_kits_bundles_factions_Faction",
                table: "kits_bundles");

            migrationBuilder.AddForeignKey(
                name: "FK_kits_bundles_factions_Faction",
                table: "kits_bundles",
                column: "Faction",
                principalTable: "factions",
                principalColumn: "pk",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

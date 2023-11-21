using Microsoft.EntityFrameworkCore.Migrations;

namespace Uncreated.Warfare.Migrations
{
    public partial class _1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_faction_translations_factions_FactionKey",
                table: "faction_translations");

            migrationBuilder.DropIndex(
                name: "IX_faction_translations_FactionKey",
                table: "faction_translations");

            migrationBuilder.DropColumn(
                name: "FactionKey",
                table: "faction_translations");

            migrationBuilder.AddColumn<uint>(
                name: "Faction",
                table: "faction_translations",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateIndex(
                name: "IX_faction_translations_Faction",
                table: "faction_translations",
                column: "Faction");

            migrationBuilder.AddForeignKey(
                name: "FK_faction_translations_factions_Faction",
                table: "faction_translations",
                column: "Faction",
                principalTable: "factions",
                principalColumn: "pk",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_faction_translations_factions_Faction",
                table: "faction_translations");

            migrationBuilder.DropIndex(
                name: "IX_faction_translations_Faction",
                table: "faction_translations");

            migrationBuilder.DropColumn(
                name: "Faction",
                table: "faction_translations");

            migrationBuilder.AddColumn<uint>(
                name: "FactionKey",
                table: "faction_translations",
                type: "int unsigned",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateIndex(
                name: "IX_faction_translations_FactionKey",
                table: "faction_translations",
                column: "FactionKey");

            migrationBuilder.AddForeignKey(
                name: "FK_faction_translations_factions_FactionKey",
                table: "faction_translations",
                column: "FactionKey",
                principalTable: "factions",
                principalColumn: "pk",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

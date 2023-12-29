using Microsoft.EntityFrameworkCore.Migrations;

namespace Uncreated.Warfare.Migrations
{
    public partial class FactionUnarmedKitFK : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnarmedKit",
                table: "factions");

            migrationBuilder.AddColumn<uint>(
                name: "UnarmedKitId",
                table: "factions",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_factions_UnarmedKitId",
                table: "factions",
                column: "UnarmedKitId");

            migrationBuilder.AddForeignKey(
                name: "FK_factions_kits_UnarmedKitId",
                table: "factions",
                column: "UnarmedKitId",
                principalTable: "kits",
                principalColumn: "pk",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_factions_kits_UnarmedKitId",
                table: "factions");

            migrationBuilder.DropIndex(
                name: "IX_factions_UnarmedKitId",
                table: "factions");

            migrationBuilder.DropColumn(
                name: "UnarmedKitId",
                table: "factions");

            migrationBuilder.AddColumn<string>(
                name: "UnarmedKit",
                table: "factions",
                type: "varchar(25) CHARACTER SET utf8mb4",
                maxLength: 25,
                nullable: true);
        }
    }
}

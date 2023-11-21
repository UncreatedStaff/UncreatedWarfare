using Microsoft.EntityFrameworkCore.Migrations;

namespace Uncreated.Warfare.Migrations
{
    public partial class _3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_kit_favorites_kits_Kit",
                table: "kit_favorites");

            migrationBuilder.DropForeignKey(
                name: "FK_kits_sign_text_kits_kit",
                table: "kits_sign_text");

            migrationBuilder.DropPrimaryKey(
                name: "PK_kit_favorites",
                table: "kit_favorites");

            migrationBuilder.RenameTable(
                name: "kit_favorites",
                newName: "kits_favorites");

            migrationBuilder.RenameColumn(
                name: "kit",
                table: "kits_sign_text",
                newName: "Kit");

            migrationBuilder.RenameIndex(
                name: "IX_kit_favorites_Steam64",
                table: "kits_favorites",
                newName: "IX_kits_favorites_Steam64");

            migrationBuilder.AddPrimaryKey(
                name: "PK_kits_favorites",
                table: "kits_favorites",
                columns: new[] { "Kit", "Steam64" });

            migrationBuilder.AddForeignKey(
                name: "FK_kits_favorites_kits_Kit",
                table: "kits_favorites",
                column: "Kit",
                principalTable: "kits",
                principalColumn: "pk",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_kits_sign_text_kits_Kit",
                table: "kits_sign_text",
                column: "Kit",
                principalTable: "kits",
                principalColumn: "pk",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_kits_favorites_kits_Kit",
                table: "kits_favorites");

            migrationBuilder.DropForeignKey(
                name: "FK_kits_sign_text_kits_Kit",
                table: "kits_sign_text");

            migrationBuilder.DropPrimaryKey(
                name: "PK_kits_favorites",
                table: "kits_favorites");

            migrationBuilder.RenameTable(
                name: "kits_favorites",
                newName: "kit_favorites");

            migrationBuilder.RenameColumn(
                name: "Kit",
                table: "kits_sign_text",
                newName: "kit");

            migrationBuilder.RenameIndex(
                name: "IX_kits_favorites_Steam64",
                table: "kit_favorites",
                newName: "IX_kit_favorites_Steam64");

            migrationBuilder.AddPrimaryKey(
                name: "PK_kit_favorites",
                table: "kit_favorites",
                columns: new[] { "Kit", "Steam64" });

            migrationBuilder.AddForeignKey(
                name: "FK_kit_favorites_kits_Kit",
                table: "kit_favorites",
                column: "Kit",
                principalTable: "kits",
                principalColumn: "pk",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_kits_sign_text_kits_kit",
                table: "kits_sign_text",
                column: "kit",
                principalTable: "kits",
                principalColumn: "pk",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

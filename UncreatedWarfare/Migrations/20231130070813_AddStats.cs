using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Uncreated.Warfare.Migrations
{
    public partial class AddStats : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<ulong>(
                name: "Steam64",
                table: "lang_preferences",
                nullable: false,
                oldClrType: typeof(ulong),
                oldType: "bigint unsigned")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.CreateIndex(
                name: "IX_lang_credits_Contributor",
                table: "lang_credits",
                column: "Contributor");

            migrationBuilder.CreateIndex(
                name: "IX_kits_hotkeys_Steam64",
                table: "kits_hotkeys",
                column: "Steam64");

            migrationBuilder.CreateIndex(
                name: "IX_kits_access_Steam64",
                table: "kits_access",
                column: "Steam64");

            migrationBuilder.AddForeignKey(
                name: "FK_kits_access_users_Steam64",
                table: "kits_access",
                column: "Steam64",
                principalTable: "users",
                principalColumn: "Steam64");

            migrationBuilder.AddForeignKey(
                name: "FK_kits_favorites_users_Steam64",
                table: "kits_favorites",
                column: "Steam64",
                principalTable: "users",
                principalColumn: "Steam64");

            migrationBuilder.AddForeignKey(
                name: "FK_kits_hotkeys_users_Steam64",
                table: "kits_hotkeys",
                column: "Steam64",
                principalTable: "users",
                principalColumn: "Steam64");

            migrationBuilder.AddForeignKey(
                name: "FK_kits_layouts_users_Steam64",
                table: "kits_layouts",
                column: "Steam64",
                principalTable: "users",
                principalColumn: "Steam64");

            migrationBuilder.AddForeignKey(
                name: "FK_lang_credits_users_Contributor",
                table: "lang_credits",
                column: "Contributor",
                principalTable: "users",
                principalColumn: "Steam64");

            migrationBuilder.AddForeignKey(
                name: "FK_lang_preferences_users_Steam64",
                table: "lang_preferences",
                column: "Steam64",
                principalTable: "users",
                principalColumn: "Steam64");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_kits_access_users_Steam64",
                table: "kits_access");

            migrationBuilder.DropForeignKey(
                name: "FK_kits_favorites_users_Steam64",
                table: "kits_favorites");

            migrationBuilder.DropForeignKey(
                name: "FK_kits_hotkeys_users_Steam64",
                table: "kits_hotkeys");

            migrationBuilder.DropForeignKey(
                name: "FK_kits_layouts_users_Steam64",
                table: "kits_layouts");

            migrationBuilder.DropForeignKey(
                name: "FK_lang_credits_users_Contributor",
                table: "lang_credits");

            migrationBuilder.DropForeignKey(
                name: "FK_lang_preferences_users_Steam64",
                table: "lang_preferences");

            migrationBuilder.DropIndex(
                name: "IX_lang_credits_Contributor",
                table: "lang_credits");

            migrationBuilder.DropIndex(
                name: "IX_kits_hotkeys_Steam64",
                table: "kits_hotkeys");

            migrationBuilder.DropIndex(
                name: "IX_kits_access_Steam64",
                table: "kits_access");

            migrationBuilder.AlterColumn<ulong>(
                name: "Steam64",
                table: "lang_preferences",
                type: "bigint unsigned",
                nullable: false,
                oldClrType: typeof(ulong))
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

namespace Uncreated.Warfare.Migrations
{
    public partial class _4 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_lang_aliases_lang_info_Langauge",
                table: "lang_aliases");

            migrationBuilder.DropForeignKey(
                name: "FK_lang_credits_lang_info_Langauge",
                table: "lang_credits");

            migrationBuilder.DropForeignKey(
                name: "FK_lang_cultures_lang_info_Langauge",
                table: "lang_cultures");

            migrationBuilder.DropForeignKey(
                name: "FK_lang_preferences_lang_info_Langauge",
                table: "lang_preferences");

            migrationBuilder.RenameColumn(
                name: "Langauge",
                table: "lang_preferences",
                newName: "Language");

            migrationBuilder.RenameIndex(
                name: "IX_lang_preferences_Langauge",
                table: "lang_preferences",
                newName: "IX_lang_preferences_Language");

            migrationBuilder.RenameColumn(
                name: "Langauge",
                table: "lang_cultures",
                newName: "Language");

            migrationBuilder.RenameIndex(
                name: "IX_lang_cultures_Langauge",
                table: "lang_cultures",
                newName: "IX_lang_cultures_Language");

            migrationBuilder.RenameColumn(
                name: "Langauge",
                table: "lang_credits",
                newName: "Language");

            migrationBuilder.RenameIndex(
                name: "IX_lang_credits_Langauge",
                table: "lang_credits",
                newName: "IX_lang_credits_Language");

            migrationBuilder.RenameColumn(
                name: "Langauge",
                table: "lang_aliases",
                newName: "Language");

            migrationBuilder.RenameIndex(
                name: "IX_lang_aliases_Langauge",
                table: "lang_aliases",
                newName: "IX_lang_aliases_Language");

            migrationBuilder.AddForeignKey(
                name: "FK_lang_aliases_lang_info_Language",
                table: "lang_aliases",
                column: "Language",
                principalTable: "lang_info",
                principalColumn: "pk",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_lang_credits_lang_info_Language",
                table: "lang_credits",
                column: "Language",
                principalTable: "lang_info",
                principalColumn: "pk",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_lang_cultures_lang_info_Language",
                table: "lang_cultures",
                column: "Language",
                principalTable: "lang_info",
                principalColumn: "pk",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_lang_preferences_lang_info_Language",
                table: "lang_preferences",
                column: "Language",
                principalTable: "lang_info",
                principalColumn: "pk",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_lang_aliases_lang_info_Language",
                table: "lang_aliases");

            migrationBuilder.DropForeignKey(
                name: "FK_lang_credits_lang_info_Language",
                table: "lang_credits");

            migrationBuilder.DropForeignKey(
                name: "FK_lang_cultures_lang_info_Language",
                table: "lang_cultures");

            migrationBuilder.DropForeignKey(
                name: "FK_lang_preferences_lang_info_Language",
                table: "lang_preferences");

            migrationBuilder.RenameColumn(
                name: "Language",
                table: "lang_preferences",
                newName: "Langauge");

            migrationBuilder.RenameIndex(
                name: "IX_lang_preferences_Language",
                table: "lang_preferences",
                newName: "IX_lang_preferences_Langauge");

            migrationBuilder.RenameColumn(
                name: "Language",
                table: "lang_cultures",
                newName: "Langauge");

            migrationBuilder.RenameIndex(
                name: "IX_lang_cultures_Language",
                table: "lang_cultures",
                newName: "IX_lang_cultures_Langauge");

            migrationBuilder.RenameColumn(
                name: "Language",
                table: "lang_credits",
                newName: "Langauge");

            migrationBuilder.RenameIndex(
                name: "IX_lang_credits_Language",
                table: "lang_credits",
                newName: "IX_lang_credits_Langauge");

            migrationBuilder.RenameColumn(
                name: "Language",
                table: "lang_aliases",
                newName: "Langauge");

            migrationBuilder.RenameIndex(
                name: "IX_lang_aliases_Language",
                table: "lang_aliases",
                newName: "IX_lang_aliases_Langauge");

            migrationBuilder.AddForeignKey(
                name: "FK_lang_aliases_lang_info_Langauge",
                table: "lang_aliases",
                column: "Langauge",
                principalTable: "lang_info",
                principalColumn: "pk",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_lang_credits_lang_info_Langauge",
                table: "lang_credits",
                column: "Langauge",
                principalTable: "lang_info",
                principalColumn: "pk",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_lang_cultures_lang_info_Langauge",
                table: "lang_cultures",
                column: "Langauge",
                principalTable: "lang_info",
                principalColumn: "pk",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_lang_preferences_lang_info_Langauge",
                table: "lang_preferences",
                column: "Langauge",
                principalTable: "lang_info",
                principalColumn: "pk",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

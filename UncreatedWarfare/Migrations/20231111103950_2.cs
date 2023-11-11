using Microsoft.EntityFrameworkCore.Migrations;

namespace Uncreated.Warfare.Migrations
{
    public partial class _2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PlayerName",
                table: "users",
                maxLength: 48,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(48) CHARACTER SET utf8",
                oldMaxLength: 48);

            migrationBuilder.AlterColumn<string>(
                name: "NickName",
                table: "users",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30) CHARACTER SET utf8",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "users",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(30) CHARACTER SET utf8",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CharacterName",
                table: "users",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30) CHARACTER SET utf8",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "Culture",
                table: "lang_preferences",
                maxLength: 16,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(16) CHARACTER SET utf8",
                oldMaxLength: 16,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SteamLanguageName",
                table: "lang_info",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(32) CHARACTER SET utf8",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NativeName",
                table: "lang_info",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(64) CHARACTER SET utf8",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "lang_info",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(64) CHARACTER SET utf8",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "DefaultCultureCode",
                table: "lang_info",
                maxLength: 16,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(16) CHARACTER SET utf8",
                oldMaxLength: 16,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CultureCode",
                table: "lang_cultures",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(16) CHARACTER SET utf8",
                oldMaxLength: 16);

            migrationBuilder.AlterColumn<string>(
                name: "Alias",
                table: "lang_aliases",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(64) CHARACTER SET utf8",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "UnarmedKit",
                table: "factions",
                maxLength: 25,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(25) CHARACTER SET utf8",
                oldMaxLength: 25,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ShortName",
                table: "factions",
                maxLength: 24,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(24) CHARACTER SET utf8",
                oldMaxLength: 24,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "factions",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(32) CHARACTER SET utf8",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "factions",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(16) CHARACTER SET utf8",
                oldMaxLength: 16);

            migrationBuilder.AlterColumn<string>(
                name: "FlagImageUrl",
                table: "factions",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(128) CHARACTER SET utf8",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "Emoji",
                table: "factions",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(64) CHARACTER SET utf8",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Abbreviation",
                table: "factions",
                maxLength: 8,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(8) CHARACTER SET utf8",
                oldMaxLength: 8);

            migrationBuilder.AlterColumn<string>(
                name: "ShortName",
                table: "faction_translations",
                maxLength: 24,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(24) CHARACTER SET utf8",
                oldMaxLength: 24,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "faction_translations",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(32) CHARACTER SET utf8",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Abbreviation",
                table: "faction_translations",
                maxLength: 8,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(8) CHARACTER SET utf8",
                oldMaxLength: 8,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "VariantKey",
                table: "faction_assets",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(32) CHARACTER SET utf8",
                oldMaxLength: 32,
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PlayerName",
                table: "users",
                type: "varchar(48) CHARACTER SET utf8",
                maxLength: 48,
                nullable: false,
                oldClrType: typeof(string),
                oldMaxLength: 48);

            migrationBuilder.AlterColumn<string>(
                name: "NickName",
                table: "users",
                type: "varchar(30) CHARACTER SET utf8",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "users",
                type: "varchar(30) CHARACTER SET utf8",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CharacterName",
                table: "users",
                type: "varchar(30) CHARACTER SET utf8",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "Culture",
                table: "lang_preferences",
                type: "varchar(16) CHARACTER SET utf8",
                maxLength: 16,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 16,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SteamLanguageName",
                table: "lang_info",
                type: "varchar(32) CHARACTER SET utf8",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NativeName",
                table: "lang_info",
                type: "varchar(64) CHARACTER SET utf8",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "lang_info",
                type: "varchar(64) CHARACTER SET utf8",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "DefaultCultureCode",
                table: "lang_info",
                type: "varchar(16) CHARACTER SET utf8",
                maxLength: 16,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 16,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CultureCode",
                table: "lang_cultures",
                type: "varchar(16) CHARACTER SET utf8",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(string),
                oldMaxLength: 16);

            migrationBuilder.AlterColumn<string>(
                name: "Alias",
                table: "lang_aliases",
                type: "varchar(64) CHARACTER SET utf8",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "UnarmedKit",
                table: "factions",
                type: "varchar(25) CHARACTER SET utf8",
                maxLength: 25,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 25,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ShortName",
                table: "factions",
                type: "varchar(24) CHARACTER SET utf8",
                maxLength: 24,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 24,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "factions",
                type: "varchar(32) CHARACTER SET utf8",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "factions",
                type: "varchar(16) CHARACTER SET utf8",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(string),
                oldMaxLength: 16);

            migrationBuilder.AlterColumn<string>(
                name: "FlagImageUrl",
                table: "factions",
                type: "varchar(128) CHARACTER SET utf8",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "Emoji",
                table: "factions",
                type: "varchar(64) CHARACTER SET utf8",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Abbreviation",
                table: "factions",
                type: "varchar(8) CHARACTER SET utf8",
                maxLength: 8,
                nullable: false,
                oldClrType: typeof(string),
                oldMaxLength: 8);

            migrationBuilder.AlterColumn<string>(
                name: "ShortName",
                table: "faction_translations",
                type: "varchar(24) CHARACTER SET utf8",
                maxLength: 24,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 24,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "faction_translations",
                type: "varchar(32) CHARACTER SET utf8",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Abbreviation",
                table: "faction_translations",
                type: "varchar(8) CHARACTER SET utf8",
                maxLength: 8,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 8,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "VariantKey",
                table: "faction_assets",
                type: "varchar(32) CHARACTER SET utf8",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldMaxLength: 32,
                oldNullable: true);
        }
    }
}

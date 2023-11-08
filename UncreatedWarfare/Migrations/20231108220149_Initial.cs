using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Uncreated.Warfare.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "factions",
                columns: table => new
                {
                    pk = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Id = table.Column<string>(maxLength: 16, nullable: false),
                    Name = table.Column<string>(maxLength: 32, nullable: true),
                    ShortName = table.Column<string>(maxLength: 24, nullable: true),
                    Abbreviation = table.Column<string>(maxLength: 8, nullable: true),
                    HexColor = table.Column<string>(type: "char(6)", nullable: true),
                    UnarmedKit = table.Column<string>(maxLength: 25, nullable: true),
                    FlagImageUrl = table.Column<string>(maxLength: 128, nullable: true),
                    SpriteIndex = table.Column<int>(nullable: true),
                    Emoji = table.Column<string>(maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_factions", x => x.pk);
                });

            migrationBuilder.CreateTable(
                name: "lang_info",
                columns: table => new
                {
                    pk = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "char(5)", nullable: false),
                    DisplayName = table.Column<string>(maxLength: 64, nullable: false),
                    NativeName = table.Column<string>(maxLength: 64, nullable: true),
                    DefaultCultureCode = table.Column<string>(maxLength: 16, nullable: true),
                    HasTranslationSupport = table.Column<bool>(nullable: false, defaultValue: false),
                    RequiresIMGUI = table.Column<bool>(nullable: false, defaultValue: false),
                    FallbackTranslationLanguageCode = table.Column<string>(type: "char(5)", nullable: true),
                    SteamLanguageName = table.Column<string>(maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lang_info", x => x.pk);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Steam64 = table.Column<ulong>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PlayerName = table.Column<string>(maxLength: 48, nullable: false),
                    CharacterName = table.Column<string>(maxLength: 30, nullable: false),
                    NickName = table.Column<string>(maxLength: 30, nullable: false),
                    PermissionLevel = table.Column<string>(nullable: false, defaultValue: "Member"),
                    FirstJoined = table.Column<DateTime>(nullable: true),
                    LastJoined = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Steam64);
                });

            migrationBuilder.CreateTable(
                name: "faction_assets",
                columns: table => new
                {
                    pk = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FactionKey = table.Column<int>(nullable: false),
                    Redirect = table.Column<string>(type: "enum('Shirt','Pants','Vest','Hat','Mask','Backpack','Glasses','AmmoSupply','BuildSupply','RallyPoint','Radio','AmmoBag','AmmoCrate','RepairStation','Bunker','VehicleBay','EntrenchingTool','UAV','RepairStationBuilt','AmmoCrateBuilt','BunkerBuilt','Cache','RadioDamaged','LaserDesignator','StandardAmmoIcon','StandardMeleeIcon','StandardGrenadeIcon','StandardSmokeGrenadeIcon')", nullable: false),
                    Asset = table.Column<string>(nullable: false),
                    VariantKey = table.Column<string>(maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_faction_assets", x => x.pk);
                    table.ForeignKey(
                        name: "FK_faction_assets_factions_FactionKey",
                        column: x => x.FactionKey,
                        principalTable: "factions",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "faction_translations",
                columns: table => new
                {
                    pk = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FactionKey = table.Column<int>(nullable: false),
                    LanguageKey = table.Column<int>(nullable: false),
                    Name = table.Column<string>(maxLength: 32, nullable: true),
                    ShortName = table.Column<string>(maxLength: 24, nullable: true),
                    Abbreviation = table.Column<string>(maxLength: 8, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_faction_translations", x => x.pk);
                    table.ForeignKey(
                        name: "FK_faction_translations_factions_FactionKey",
                        column: x => x.FactionKey,
                        principalTable: "factions",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_faction_translations_lang_info_LanguageKey",
                        column: x => x.LanguageKey,
                        principalTable: "lang_info",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lang_aliases",
                columns: table => new
                {
                    pk = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    LanguageKey = table.Column<int>(nullable: false),
                    Alias = table.Column<string>(maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lang_aliases", x => x.pk);
                    table.ForeignKey(
                        name: "FK_lang_aliases_lang_info_LanguageKey",
                        column: x => x.LanguageKey,
                        principalTable: "lang_info",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lang_credits",
                columns: table => new
                {
                    pk = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    LanguageKey = table.Column<int>(nullable: false),
                    Contributor = table.Column<ulong>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lang_credits", x => x.pk);
                    table.ForeignKey(
                        name: "FK_lang_credits_lang_info_LanguageKey",
                        column: x => x.LanguageKey,
                        principalTable: "lang_info",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lang_cultures",
                columns: table => new
                {
                    pk = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    LanguageKey = table.Column<int>(nullable: false),
                    CultureCode = table.Column<string>(maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lang_cultures", x => x.pk);
                    table.ForeignKey(
                        name: "FK_lang_cultures_lang_info_LanguageKey",
                        column: x => x.LanguageKey,
                        principalTable: "lang_info",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lang_preferences",
                columns: table => new
                {
                    Steam64 = table.Column<ulong>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    LanguageKey = table.Column<int>(nullable: true),
                    Culture = table.Column<string>(maxLength: 16, nullable: true),
                    UseCultureForCmdInput = table.Column<bool>(nullable: false, defaultValue: false),
                    LastUpdated = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lang_preferences", x => x.Steam64);
                    table.ForeignKey(
                        name: "FK_lang_preferences_lang_info_LanguageKey",
                        column: x => x.LanguageKey,
                        principalTable: "lang_info",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_faction_assets_FactionKey",
                table: "faction_assets",
                column: "FactionKey");

            migrationBuilder.CreateIndex(
                name: "IX_faction_translations_FactionKey",
                table: "faction_translations",
                column: "FactionKey");

            migrationBuilder.CreateIndex(
                name: "IX_faction_translations_LanguageKey",
                table: "faction_translations",
                column: "LanguageKey");

            migrationBuilder.CreateIndex(
                name: "IX_lang_aliases_LanguageKey",
                table: "lang_aliases",
                column: "LanguageKey");

            migrationBuilder.CreateIndex(
                name: "IX_lang_credits_LanguageKey",
                table: "lang_credits",
                column: "LanguageKey");

            migrationBuilder.CreateIndex(
                name: "IX_lang_cultures_LanguageKey",
                table: "lang_cultures",
                column: "LanguageKey");

            migrationBuilder.CreateIndex(
                name: "IX_lang_preferences_LanguageKey",
                table: "lang_preferences",
                column: "LanguageKey");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "faction_assets");

            migrationBuilder.DropTable(
                name: "faction_translations");

            migrationBuilder.DropTable(
                name: "lang_aliases");

            migrationBuilder.DropTable(
                name: "lang_credits");

            migrationBuilder.DropTable(
                name: "lang_cultures");

            migrationBuilder.DropTable(
                name: "lang_preferences");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "factions");

            migrationBuilder.DropTable(
                name: "lang_info");
        }
    }
}

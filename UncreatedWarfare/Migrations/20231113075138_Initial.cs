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
                    pk = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Id = table.Column<string>(maxLength: 16, nullable: false),
                    Name = table.Column<string>(maxLength: 32, nullable: false),
                    ShortName = table.Column<string>(maxLength: 24, nullable: true),
                    Abbreviation = table.Column<string>(maxLength: 8, nullable: false),
                    HexColor = table.Column<string>(type: "char(6)", nullable: false),
                    UnarmedKit = table.Column<string>(maxLength: 25, nullable: true),
                    FlagImageUrl = table.Column<string>(maxLength: 128, nullable: false),
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
                    pk = table.Column<uint>(nullable: false)
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
                    DisplayName = table.Column<string>(maxLength: 30, nullable: true),
                    PermissionLevel = table.Column<string>(type: "enum('Member','Helper','TrialAdmin','Admin','Superuser')", nullable: false),
                    FirstJoined = table.Column<DateTime>(nullable: true),
                    LastJoined = table.Column<DateTime>(nullable: true),
                    LastHWID = table.Column<byte[]>(type: "binary(20)", nullable: true),
                    LastIPAddress = table.Column<string>(type: "varchar(45)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Steam64);
                });

            migrationBuilder.CreateTable(
                name: "faction_assets",
                columns: table => new
                {
                    pk = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Faction = table.Column<uint>(nullable: false),
                    Redirect = table.Column<string>(type: "enum('Shirt','Pants','Vest','Hat','Mask','Backpack','Glasses','AmmoSupply','BuildSupply','RallyPoint','Radio','AmmoBag','AmmoCrate','RepairStation','Bunker','EntrenchingTool','UAV','RepairStationBuilt','AmmoCrateBuilt','BunkerBuilt','Cache','RadioDamaged','LaserDesignator')", nullable: false),
                    Asset = table.Column<string>(type: "char(32)", nullable: false),
                    VariantKey = table.Column<string>(maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_faction_assets", x => x.pk);
                    table.ForeignKey(
                        name: "FK_faction_assets_factions_Faction",
                        column: x => x.Faction,
                        principalTable: "factions",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "kits",
                columns: table => new
                {
                    pk = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Faction = table.Column<uint>(nullable: true),
                    Id = table.Column<string>(maxLength: 25, nullable: false),
                    Class = table.Column<string>(type: "enum('Unarmed','Squadleader','Rifleman','Medic','Breacher','AutomaticRifleman','Grenadier','MachineGunner','LAT','HAT','Marksman','Sniper','APRifleman','CombatEngineer','Crewman','Pilot','SpecOps')", nullable: false),
                    Branch = table.Column<string>(type: "enum('Infantry','Armor','Airforce','SpecOps','Navy')", nullable: false),
                    Type = table.Column<string>(type: "enum('Public','Elite','Special','Loadout')", nullable: false),
                    Disabled = table.Column<bool>(nullable: false),
                    RequiresNitro = table.Column<bool>(nullable: false),
                    MapFilterIsWhitelist = table.Column<bool>(nullable: false),
                    FactionFilterIsWhitelist = table.Column<bool>(nullable: false),
                    Season = table.Column<int>(nullable: false),
                    RequestCooldown = table.Column<float>(nullable: false),
                    TeamLimit = table.Column<float>(nullable: true),
                    CreditCost = table.Column<int>(nullable: false),
                    PremiumCost = table.Column<decimal>(nullable: false),
                    SquadLevel = table.Column<string>(type: "enum('Member','Commander')", nullable: false),
                    Weapons = table.Column<string>(maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                    Creator = table.Column<ulong>(nullable: false),
                    LastEditedAt = table.Column<DateTimeOffset>(nullable: false),
                    LastEditor = table.Column<ulong>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kits", x => x.pk);
                    table.ForeignKey(
                        name: "FK_kits_factions_Faction",
                        column: x => x.Faction,
                        principalTable: "factions",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "kits_bundles",
                columns: table => new
                {
                    pk = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Id = table.Column<string>(maxLength: 25, nullable: false),
                    DisplayName = table.Column<string>(maxLength: 50, nullable: false),
                    Description = table.Column<string>(maxLength: 255, nullable: false),
                    Faction = table.Column<uint>(nullable: false),
                    Cost = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kits_bundles", x => x.pk);
                    table.ForeignKey(
                        name: "FK_kits_bundles_factions_Faction",
                        column: x => x.Faction,
                        principalTable: "factions",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "faction_translations",
                columns: table => new
                {
                    pk = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FactionKey = table.Column<uint>(nullable: false),
                    Language = table.Column<uint>(nullable: false),
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
                        name: "FK_faction_translations_lang_info_Language",
                        column: x => x.Language,
                        principalTable: "lang_info",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lang_aliases",
                columns: table => new
                {
                    pk = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Langauge = table.Column<uint>(nullable: false),
                    Alias = table.Column<string>(maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lang_aliases", x => x.pk);
                    table.ForeignKey(
                        name: "FK_lang_aliases_lang_info_Langauge",
                        column: x => x.Langauge,
                        principalTable: "lang_info",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lang_credits",
                columns: table => new
                {
                    pk = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Langauge = table.Column<uint>(nullable: false),
                    Contributor = table.Column<ulong>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lang_credits", x => x.pk);
                    table.ForeignKey(
                        name: "FK_lang_credits_lang_info_Langauge",
                        column: x => x.Langauge,
                        principalTable: "lang_info",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lang_cultures",
                columns: table => new
                {
                    pk = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Langauge = table.Column<uint>(nullable: false),
                    CultureCode = table.Column<string>(maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lang_cultures", x => x.pk);
                    table.ForeignKey(
                        name: "FK_lang_cultures_lang_info_Langauge",
                        column: x => x.Langauge,
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
                    Langauge = table.Column<uint>(nullable: false),
                    Culture = table.Column<string>(maxLength: 16, nullable: true),
                    UseCultureForCmdInput = table.Column<bool>(nullable: false, defaultValue: false),
                    LastUpdated = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lang_preferences", x => x.Steam64);
                    table.ForeignKey(
                        name: "FK_lang_preferences_lang_info_Langauge",
                        column: x => x.Langauge,
                        principalTable: "lang_info",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "kit_favorites",
                columns: table => new
                {
                    Kit = table.Column<uint>(nullable: false),
                    Steam64 = table.Column<ulong>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kit_favorites", x => new { x.Kit, x.Steam64 });
                    table.ForeignKey(
                        name: "FK_kit_favorites_kits_Kit",
                        column: x => x.Kit,
                        principalTable: "kits",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "kits_access",
                columns: table => new
                {
                    Kit = table.Column<uint>(nullable: false),
                    Steam64 = table.Column<ulong>(nullable: false),
                    AccessType = table.Column<string>(type: "enum('Unknown','Credits','Event','Purchase','QuestReward')", nullable: false),
                    GivenAt = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kits_access", x => new { x.Kit, x.Steam64 });
                    table.ForeignKey(
                        name: "FK_kits_access_kits_Kit",
                        column: x => x.Kit,
                        principalTable: "kits",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "kits_faction_filters",
                columns: table => new
                {
                    Kit = table.Column<uint>(nullable: false),
                    Faction = table.Column<uint>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kits_faction_filters", x => new { x.Kit, x.Faction });
                    table.ForeignKey(
                        name: "FK_kits_faction_filters_factions_Faction",
                        column: x => x.Faction,
                        principalTable: "factions",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_kits_faction_filters_kits_Kit",
                        column: x => x.Kit,
                        principalTable: "kits",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "kits_hotkeys",
                columns: table => new
                {
                    Steam64 = table.Column<ulong>(nullable: false),
                    Kit = table.Column<uint>(nullable: false),
                    Slot = table.Column<byte>(nullable: false),
                    Page = table.Column<string>(type: "enum('Primary','Secondary','Hands','Backpack','Vest','Shirt','Pants','Storage','Area')", nullable: false),
                    X = table.Column<byte>(nullable: false),
                    Y = table.Column<byte>(nullable: false),
                    Item = table.Column<string>(type: "char(32)", nullable: true),
                    Redirect = table.Column<string>(type: "enum('Shirt','Pants','Vest','Hat','Mask','Backpack','Glasses','AmmoSupply','BuildSupply','RallyPoint','Radio','AmmoBag','AmmoCrate','RepairStation','Bunker','EntrenchingTool','UAV','RepairStationBuilt','AmmoCrateBuilt','BunkerBuilt','Cache','RadioDamaged','LaserDesignator')", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kits_hotkeys", x => new { x.Kit, x.Slot, x.Steam64 });
                    table.ForeignKey(
                        name: "FK_kits_hotkeys_kits_Kit",
                        column: x => x.Kit,
                        principalTable: "kits",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "kits_items",
                columns: table => new
                {
                    pk = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Kit = table.Column<uint>(nullable: false),
                    Item = table.Column<string>(type: "char(32)", nullable: true),
                    X = table.Column<byte>(nullable: true),
                    Y = table.Column<byte>(nullable: true),
                    Rotation = table.Column<byte>(nullable: true),
                    Page = table.Column<string>(type: "enum('Primary','Secondary','Hands','Backpack','Vest','Shirt','Pants','Storage','Area')", nullable: true),
                    ClothingSlot = table.Column<string>(type: "enum('Shirt','Pants','Vest','Hat','Mask','Backpack','Glasses')", nullable: true),
                    Redirect = table.Column<string>(type: "enum('Shirt','Pants','Vest','Hat','Mask','Backpack','Glasses','AmmoSupply','BuildSupply','RallyPoint','Radio','AmmoBag','AmmoCrate','RepairStation','Bunker','EntrenchingTool','UAV','RepairStationBuilt','AmmoCrateBuilt','BunkerBuilt','Cache','RadioDamaged','LaserDesignator')", nullable: true),
                    RedirectVariant = table.Column<string>(maxLength: 36, nullable: true),
                    Amount = table.Column<byte>(nullable: true),
                    Metadata = table.Column<byte[]>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kits_items", x => x.pk);
                    table.ForeignKey(
                        name: "FK_kits_items_kits_Kit",
                        column: x => x.Kit,
                        principalTable: "kits",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "kits_layouts",
                columns: table => new
                {
                    pk = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Steam64 = table.Column<ulong>(nullable: false),
                    Kit = table.Column<uint>(nullable: false),
                    OldPage = table.Column<string>(type: "enum('Primary','Secondary','Hands','Backpack','Vest','Shirt','Pants','Storage','Area')", nullable: false),
                    OldX = table.Column<byte>(nullable: false),
                    OldY = table.Column<byte>(nullable: false),
                    NewPage = table.Column<string>(type: "enum('Primary','Secondary','Hands','Backpack','Vest','Shirt','Pants','Storage','Area')", nullable: false),
                    NewX = table.Column<byte>(nullable: false),
                    NewY = table.Column<byte>(nullable: false),
                    NewRotation = table.Column<byte>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kits_layouts", x => x.pk);
                    table.ForeignKey(
                        name: "FK_kits_layouts_kits_Kit",
                        column: x => x.Kit,
                        principalTable: "kits",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "kits_map_filters",
                columns: table => new
                {
                    Kit = table.Column<uint>(nullable: false),
                    Map = table.Column<uint>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kits_map_filters", x => new { x.Kit, x.Map });
                    table.ForeignKey(
                        name: "FK_kits_map_filters_kits_Kit",
                        column: x => x.Kit,
                        principalTable: "kits",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "kits_sign_text",
                columns: table => new
                {
                    Language = table.Column<uint>(nullable: false),
                    kit = table.Column<uint>(nullable: false),
                    Value = table.Column<string>(maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kits_sign_text", x => new { x.kit, x.Language });
                    table.ForeignKey(
                        name: "FK_kits_sign_text_kits_kit",
                        column: x => x.kit,
                        principalTable: "kits",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_kits_sign_text_lang_info_Language",
                        column: x => x.Language,
                        principalTable: "lang_info",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "kits_skillsets",
                columns: table => new
                {
                    pk = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Skill = table.Column<string>(type: "enum('OVERKILL','SHARPSHOOTER','DEXTERITY','CARDIO','EXERCISE','DIVING','PARKOUR','SNEAKYBEAKY','VITALITY','IMMUNITY','TOUGHNESS','STRENGTH','WARMBLOODED','SURVIVAL','HEALING','CRAFTING','OUTDOORS','COOKING','FISHING','AGRICULTURE','MECHANIC','ENGINEER')", nullable: false),
                    Level = table.Column<byte>(nullable: false),
                    Kit = table.Column<uint>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kits_skillsets", x => x.pk);
                    table.ForeignKey(
                        name: "FK_kits_skillsets_kits_Kit",
                        column: x => x.Kit,
                        principalTable: "kits",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "kits_unlock_requirements",
                columns: table => new
                {
                    pk = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Json = table.Column<string>(maxLength: 255, nullable: false),
                    Kit = table.Column<uint>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kits_unlock_requirements", x => x.pk);
                    table.ForeignKey(
                        name: "FK_kits_unlock_requirements_kits_Kit",
                        column: x => x.Kit,
                        principalTable: "kits",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "kits_bundle_items",
                columns: table => new
                {
                    Bundle = table.Column<uint>(nullable: false),
                    Kit = table.Column<uint>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kits_bundle_items", x => new { x.Kit, x.Bundle });
                    table.ForeignKey(
                        name: "FK_kits_bundle_items_kits_bundles_Bundle",
                        column: x => x.Bundle,
                        principalTable: "kits_bundles",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_kits_bundle_items_kits_Kit",
                        column: x => x.Kit,
                        principalTable: "kits",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_faction_assets_Faction",
                table: "faction_assets",
                column: "Faction");

            migrationBuilder.CreateIndex(
                name: "IX_faction_translations_FactionKey",
                table: "faction_translations",
                column: "FactionKey");

            migrationBuilder.CreateIndex(
                name: "IX_faction_translations_Language",
                table: "faction_translations",
                column: "Language");

            migrationBuilder.CreateIndex(
                name: "IX_kit_favorites_Steam64",
                table: "kit_favorites",
                column: "Steam64");

            migrationBuilder.CreateIndex(
                name: "IX_kits_Faction",
                table: "kits",
                column: "Faction");

            migrationBuilder.CreateIndex(
                name: "IX_kits_bundle_items_Bundle",
                table: "kits_bundle_items",
                column: "Bundle");

            migrationBuilder.CreateIndex(
                name: "IX_kits_bundles_Faction",
                table: "kits_bundles",
                column: "Faction");

            migrationBuilder.CreateIndex(
                name: "IX_kits_faction_filters_Faction",
                table: "kits_faction_filters",
                column: "Faction");

            migrationBuilder.CreateIndex(
                name: "IX_kits_items_Kit",
                table: "kits_items",
                column: "Kit");

            migrationBuilder.CreateIndex(
                name: "IX_kits_layouts_Kit",
                table: "kits_layouts",
                column: "Kit");

            migrationBuilder.CreateIndex(
                name: "IX_kits_layouts_Steam64",
                table: "kits_layouts",
                column: "Steam64");

            migrationBuilder.CreateIndex(
                name: "IX_kits_sign_text_Language",
                table: "kits_sign_text",
                column: "Language");

            migrationBuilder.CreateIndex(
                name: "IX_kits_skillsets_Kit",
                table: "kits_skillsets",
                column: "Kit");

            migrationBuilder.CreateIndex(
                name: "IX_kits_unlock_requirements_Kit",
                table: "kits_unlock_requirements",
                column: "Kit");

            migrationBuilder.CreateIndex(
                name: "IX_lang_aliases_Langauge",
                table: "lang_aliases",
                column: "Langauge");

            migrationBuilder.CreateIndex(
                name: "IX_lang_credits_Langauge",
                table: "lang_credits",
                column: "Langauge");

            migrationBuilder.CreateIndex(
                name: "IX_lang_cultures_Langauge",
                table: "lang_cultures",
                column: "Langauge");

            migrationBuilder.CreateIndex(
                name: "IX_lang_preferences_Langauge",
                table: "lang_preferences",
                column: "Langauge");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "faction_assets");

            migrationBuilder.DropTable(
                name: "faction_translations");

            migrationBuilder.DropTable(
                name: "kit_favorites");

            migrationBuilder.DropTable(
                name: "kits_access");

            migrationBuilder.DropTable(
                name: "kits_bundle_items");

            migrationBuilder.DropTable(
                name: "kits_faction_filters");

            migrationBuilder.DropTable(
                name: "kits_hotkeys");

            migrationBuilder.DropTable(
                name: "kits_items");

            migrationBuilder.DropTable(
                name: "kits_layouts");

            migrationBuilder.DropTable(
                name: "kits_map_filters");

            migrationBuilder.DropTable(
                name: "kits_sign_text");

            migrationBuilder.DropTable(
                name: "kits_skillsets");

            migrationBuilder.DropTable(
                name: "kits_unlock_requirements");

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
                name: "kits_bundles");

            migrationBuilder.DropTable(
                name: "kits");

            migrationBuilder.DropTable(
                name: "lang_info");

            migrationBuilder.DropTable(
                name: "factions");
        }
    }
}

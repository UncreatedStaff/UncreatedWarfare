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
                name: "seasons",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ReleaseTimestampUTC = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seasons", x => x.Id);
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
                    FirstJoined = table.Column<DateTime>(type: "datetime", nullable: true),
                    LastJoined = table.Column<DateTime>(type: "datetime", nullable: true),
                    TotalSeconds = table.Column<uint>(nullable: false)
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
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    Creator = table.Column<ulong>(nullable: false),
                    LastEditedAt = table.Column<DateTime>(type: "datetime", nullable: false),
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
                    Faction = table.Column<uint>(nullable: true),
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
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "stats_games",
                columns: table => new
                {
                    GameId = table.Column<ulong>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Season = table.Column<int>(nullable: false),
                    Map = table.Column<int>(nullable: false),
                    StartTimestamp = table.Column<DateTime>(type: "datetime", nullable: false),
                    EndTimestamp = table.Column<DateTime>(type: "datetime", nullable: true),
                    Winner = table.Column<uint>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stats_games", x => x.GameId);
                    table.ForeignKey(
                        name: "FK_stats_games_factions_Winner",
                        column: x => x.Winner,
                        principalTable: "factions",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "faction_translations",
                columns: table => new
                {
                    pk = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Faction = table.Column<uint>(nullable: false),
                    Language = table.Column<uint>(nullable: false),
                    Name = table.Column<string>(maxLength: 32, nullable: true),
                    ShortName = table.Column<string>(maxLength: 24, nullable: true),
                    Abbreviation = table.Column<string>(maxLength: 8, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_faction_translations", x => x.pk);
                    table.ForeignKey(
                        name: "FK_faction_translations_factions_Faction",
                        column: x => x.Faction,
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
                    Language = table.Column<uint>(nullable: false),
                    Alias = table.Column<string>(maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lang_aliases", x => x.pk);
                    table.ForeignKey(
                        name: "FK_lang_aliases_lang_info_Language",
                        column: x => x.Language,
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
                    Language = table.Column<uint>(nullable: false),
                    CultureCode = table.Column<string>(maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lang_cultures", x => x.pk);
                    table.ForeignKey(
                        name: "FK_lang_cultures_lang_info_Language",
                        column: x => x.Language,
                        principalTable: "lang_info",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "maps",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DisplayName = table.Column<string>(maxLength: 50, nullable: false),
                    WorkshopId = table.Column<ulong>(nullable: true),
                    SeasonReleased = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_maps_seasons_SeasonReleased",
                        column: x => x.SeasonReleased,
                        principalTable: "seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "hwids",
                columns: table => new
                {
                    Id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Index = table.Column<int>(nullable: false),
                    Steam64 = table.Column<ulong>(nullable: false),
                    HWID = table.Column<byte[]>(type: "binary(20)", nullable: false),
                    LoginCount = table.Column<int>(nullable: false),
                    LastLogin = table.Column<DateTime>(type: "datetime", nullable: false),
                    FirstLogin = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hwids", x => x.Id);
                    table.ForeignKey(
                        name: "FK_hwids_users_Steam64",
                        column: x => x.Steam64,
                        principalTable: "users",
                        principalColumn: "Steam64");
                });

            migrationBuilder.CreateTable(
                name: "ip_addresses",
                columns: table => new
                {
                    Id = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    LoginCount = table.Column<int>(nullable: false),
                    Steam64 = table.Column<ulong>(nullable: false),
                    FirstLogin = table.Column<DateTime>(type: "datetime", nullable: true),
                    LastLogin = table.Column<DateTime>(type: "datetime", nullable: false),
                    Packed = table.Column<uint>(nullable: false),
                    Unpacked = table.Column<string>(type: "varchar(45)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ip_addresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ip_addresses_users_Steam64",
                        column: x => x.Steam64,
                        principalTable: "users",
                        principalColumn: "Steam64");
                });

            migrationBuilder.CreateTable(
                name: "lang_credits",
                columns: table => new
                {
                    pk = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Language = table.Column<uint>(nullable: false),
                    Contributor = table.Column<ulong>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lang_credits", x => x.pk);
                    table.ForeignKey(
                        name: "FK_lang_credits_users_Contributor",
                        column: x => x.Contributor,
                        principalTable: "users",
                        principalColumn: "Steam64");
                    table.ForeignKey(
                        name: "FK_lang_credits_lang_info_Language",
                        column: x => x.Language,
                        principalTable: "lang_info",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lang_preferences",
                columns: table => new
                {
                    Steam64 = table.Column<ulong>(nullable: false),
                    Language = table.Column<uint>(nullable: false),
                    Culture = table.Column<string>(maxLength: 16, nullable: true),
                    UseCultureForCmdInput = table.Column<bool>(nullable: false, defaultValue: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lang_preferences", x => x.Steam64);
                    table.ForeignKey(
                        name: "FK_lang_preferences_lang_info_Language",
                        column: x => x.Language,
                        principalTable: "lang_info",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_lang_preferences_users_Steam64",
                        column: x => x.Steam64,
                        principalTable: "users",
                        principalColumn: "Steam64");
                });

            migrationBuilder.CreateTable(
                name: "kits_access",
                columns: table => new
                {
                    Kit = table.Column<uint>(nullable: false),
                    Steam64 = table.Column<ulong>(nullable: false),
                    AccessType = table.Column<string>(type: "enum('Unknown','Credits','Event','Purchase','QuestReward')", nullable: false),
                    GivenAt = table.Column<DateTime>(type: "datetime", nullable: false)
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
                    table.ForeignKey(
                        name: "FK_kits_access_users_Steam64",
                        column: x => x.Steam64,
                        principalTable: "users",
                        principalColumn: "Steam64");
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
                name: "kits_favorites",
                columns: table => new
                {
                    Kit = table.Column<uint>(nullable: false),
                    Steam64 = table.Column<ulong>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kits_favorites", x => new { x.Kit, x.Steam64 });
                    table.ForeignKey(
                        name: "FK_kits_favorites_kits_Kit",
                        column: x => x.Kit,
                        principalTable: "kits",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_kits_favorites_users_Steam64",
                        column: x => x.Steam64,
                        principalTable: "users",
                        principalColumn: "Steam64");
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
                    table.ForeignKey(
                        name: "FK_kits_hotkeys_users_Steam64",
                        column: x => x.Steam64,
                        principalTable: "users",
                        principalColumn: "Steam64");
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
                    Metadata = table.Column<byte[]>(type: "varbinary(18)", maxLength: 18, nullable: true)
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
                    table.ForeignKey(
                        name: "FK_kits_layouts_users_Steam64",
                        column: x => x.Steam64,
                        principalTable: "users",
                        principalColumn: "Steam64");
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
                    Kit = table.Column<uint>(nullable: false),
                    Value = table.Column<string>(maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kits_sign_text", x => new { x.Kit, x.Language });
                    table.ForeignKey(
                        name: "FK_kits_sign_text_kits_Kit",
                        column: x => x.Kit,
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

            migrationBuilder.CreateTable(
                name: "maps_dependencies",
                columns: table => new
                {
                    WorkshopId = table.Column<ulong>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Map = table.Column<int>(nullable: false),
                    IsRemoved = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maps_dependencies", x => new { x.Map, x.WorkshopId });
                    table.ForeignKey(
                        name: "FK_maps_dependencies_maps_Map",
                        column: x => x.Map,
                        principalTable: "maps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stats_sessions",
                columns: table => new
                {
                    SessionId = table.Column<ulong>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Steam64 = table.Column<ulong>(nullable: false),
                    PlayerDataSteam64 = table.Column<ulong>(nullable: false),
                    Game = table.Column<ulong>(nullable: false),
                    Season = table.Column<int>(nullable: false),
                    Map = table.Column<int>(nullable: false),
                    Team = table.Column<byte>(nullable: false),
                    StartedTimestampUTC = table.Column<DateTime>(type: "datetime", nullable: false),
                    EndedTimestampUTC = table.Column<DateTime>(type: "datetime", nullable: true),
                    LengthSeconds = table.Column<double>(nullable: false),
                    PreviousSession = table.Column<ulong>(nullable: true),
                    NextSession = table.Column<ulong>(nullable: true),
                    Faction = table.Column<uint>(nullable: true),
                    Kit = table.Column<uint>(nullable: true),
                    FinishedGame = table.Column<bool>(nullable: false),
                    UnexpectedTermination = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stats_sessions", x => x.SessionId);
                    table.ForeignKey(
                        name: "FK_stats_sessions_factions_Faction",
                        column: x => x.Faction,
                        principalTable: "factions",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stats_sessions_stats_games_Game",
                        column: x => x.Game,
                        principalTable: "stats_games",
                        principalColumn: "GameId");
                    table.ForeignKey(
                        name: "FK_stats_sessions_kits_Kit",
                        column: x => x.Kit,
                        principalTable: "kits",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stats_sessions_maps_Map",
                        column: x => x.Map,
                        principalTable: "maps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_stats_sessions_users_PlayerDataSteam64",
                        column: x => x.PlayerDataSteam64,
                        principalTable: "users",
                        principalColumn: "Steam64",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_stats_sessions_stats_sessions_PreviousSession",
                        column: x => x.PreviousSession,
                        principalTable: "stats_sessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_stats_sessions_seasons_Season",
                        column: x => x.Season,
                        principalTable: "seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_stats_sessions_users_Steam64",
                        column: x => x.Steam64,
                        principalTable: "users",
                        principalColumn: "Steam64");
                });

            migrationBuilder.CreateTable(
                name: "stats_aid_records",
                columns: table => new
                {
                    Id = table.Column<ulong>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Team = table.Column<byte>(nullable: false),
                    Steam64 = table.Column<ulong>(nullable: false),
                    Session = table.Column<ulong>(nullable: false),
                    PositionX = table.Column<float>(nullable: false),
                    PositionY = table.Column<float>(nullable: false),
                    PositionZ = table.Column<float>(nullable: false),
                    TimestampUTC = table.Column<DateTime>(type: "datetime", nullable: false),
                    Instigator = table.Column<ulong>(nullable: true),
                    InstigatorSession = table.Column<ulong>(nullable: true),
                    InstigatorPositionX = table.Column<float>(nullable: true),
                    InstigatorPositionY = table.Column<float>(nullable: true),
                    InstigatorPositionZ = table.Column<float>(nullable: true),
                    Item = table.Column<string>(type: "char(32)", nullable: false),
                    Health = table.Column<float>(nullable: false),
                    IsRevive = table.Column<bool>(nullable: false),
                    ItemName = table.Column<string>(maxLength: 48, nullable: false, defaultValue: "00000000000000000000000000000000")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stats_aid_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stats_aid_records_users_Instigator",
                        column: x => x.Instigator,
                        principalTable: "users",
                        principalColumn: "Steam64");
                    table.ForeignKey(
                        name: "FK_stats_aid_records_stats_sessions_InstigatorSession",
                        column: x => x.InstigatorSession,
                        principalTable: "stats_sessions",
                        principalColumn: "SessionId");
                    table.ForeignKey(
                        name: "FK_stats_aid_records_stats_sessions_Session",
                        column: x => x.Session,
                        principalTable: "stats_sessions",
                        principalColumn: "SessionId");
                    table.ForeignKey(
                        name: "FK_stats_aid_records_users_Steam64",
                        column: x => x.Steam64,
                        principalTable: "users",
                        principalColumn: "Steam64");
                });

            migrationBuilder.CreateTable(
                name: "stats_damage",
                columns: table => new
                {
                    Id = table.Column<ulong>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Team = table.Column<byte>(nullable: false),
                    Steam64 = table.Column<ulong>(nullable: false),
                    Session = table.Column<ulong>(nullable: false),
                    PositionX = table.Column<float>(nullable: false),
                    PositionY = table.Column<float>(nullable: false),
                    PositionZ = table.Column<float>(nullable: false),
                    TimestampUTC = table.Column<DateTime>(type: "datetime", nullable: false),
                    Instigator = table.Column<ulong>(nullable: true),
                    InstigatorSession = table.Column<ulong>(nullable: true),
                    InstigatorPositionX = table.Column<float>(nullable: true),
                    InstigatorPositionY = table.Column<float>(nullable: true),
                    InstigatorPositionZ = table.Column<float>(nullable: true),
                    RelatedPlayer = table.Column<ulong>(nullable: true),
                    RelatedPlayerSession = table.Column<ulong>(nullable: true),
                    RelatedPlayerPositionX = table.Column<float>(nullable: true),
                    RelatedPlayerPositionY = table.Column<float>(nullable: true),
                    RelatedPlayerPositionZ = table.Column<float>(nullable: true),
                    PrimaryAsset = table.Column<string>(type: "char(32)", nullable: true),
                    SecondaryAsset = table.Column<string>(type: "char(32)", nullable: true),
                    Origin = table.Column<string>(type: "enum('Unknown','Mega_Zombie_Boulder','Vehicle_Bumper','Horde_Beacon_Self_Destruct','Trap_Wear_And_Tear','Carepackage_Timeout','Plant_Harvested','Charge_Self_Destruct','Zombie_Swipe','Grenade_Explosion','Rocket_Explosion','Food_Explosion','Vehicle_Explosion','Charge_Explosion','Trap_Explosion','Bullet_Explosion','Radioactive_Zombie_Explosion','Flamable_Zombie_Explosion','Zombie_Electric_Shock','Zombie_Stomp','Zombie_Fire_Breath','Sentry','Useable_Gun','Useable_Melee','Punch','Animal_Attack','Kill_Volume','Vehicle_Collision_Self_Damage','Lightning','VehicleDecay')", nullable: false),
                    Distance = table.Column<float>(nullable: true),
                    Damage = table.Column<float>(nullable: false),
                    IsTeamkill = table.Column<bool>(nullable: false),
                    IsSuicide = table.Column<bool>(nullable: false),
                    IsInjure = table.Column<bool>(nullable: false),
                    Limb = table.Column<string>(type: "enum('LEFT_FOOT','LEFT_LEG','RIGHT_FOOT','RIGHT_LEG','LEFT_HAND','LEFT_ARM','RIGHT_HAND','RIGHT_ARM','LEFT_BACK','RIGHT_BACK','LEFT_FRONT','RIGHT_FRONT','SPINE','SKULL')", nullable: false),
                    PrimaryAssetName = table.Column<string>(maxLength: 48, nullable: true),
                    SecondaryAssetName = table.Column<string>(maxLength: 48, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stats_damage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stats_damage_users_Instigator",
                        column: x => x.Instigator,
                        principalTable: "users",
                        principalColumn: "Steam64");
                    table.ForeignKey(
                        name: "FK_stats_damage_stats_sessions_InstigatorSession",
                        column: x => x.InstigatorSession,
                        principalTable: "stats_sessions",
                        principalColumn: "SessionId");
                    table.ForeignKey(
                        name: "FK_stats_damage_users_RelatedPlayer",
                        column: x => x.RelatedPlayer,
                        principalTable: "users",
                        principalColumn: "Steam64");
                    table.ForeignKey(
                        name: "FK_stats_damage_stats_sessions_RelatedPlayerSession",
                        column: x => x.RelatedPlayerSession,
                        principalTable: "stats_sessions",
                        principalColumn: "SessionId");
                    table.ForeignKey(
                        name: "FK_stats_damage_stats_sessions_Session",
                        column: x => x.Session,
                        principalTable: "stats_sessions",
                        principalColumn: "SessionId");
                    table.ForeignKey(
                        name: "FK_stats_damage_users_Steam64",
                        column: x => x.Steam64,
                        principalTable: "users",
                        principalColumn: "Steam64");
                });

            migrationBuilder.CreateTable(
                name: "stats_deaths",
                columns: table => new
                {
                    Id = table.Column<ulong>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Team = table.Column<byte>(nullable: false),
                    Steam64 = table.Column<ulong>(nullable: false),
                    Session = table.Column<ulong>(nullable: false),
                    PositionX = table.Column<float>(nullable: false),
                    PositionY = table.Column<float>(nullable: false),
                    PositionZ = table.Column<float>(nullable: false),
                    TimestampUTC = table.Column<DateTime>(type: "datetime", nullable: false),
                    Instigator = table.Column<ulong>(nullable: true),
                    InstigatorSession = table.Column<ulong>(nullable: true),
                    InstigatorPositionX = table.Column<float>(nullable: true),
                    InstigatorPositionY = table.Column<float>(nullable: true),
                    InstigatorPositionZ = table.Column<float>(nullable: true),
                    RelatedPlayer = table.Column<ulong>(nullable: true),
                    RelatedPlayerSession = table.Column<ulong>(nullable: true),
                    RelatedPlayerPositionX = table.Column<float>(nullable: true),
                    RelatedPlayerPositionY = table.Column<float>(nullable: true),
                    RelatedPlayerPositionZ = table.Column<float>(nullable: true),
                    DeathMessage = table.Column<string>(maxLength: 256, nullable: false),
                    DeathCause = table.Column<string>(type: "enum('BLEEDING','BONES','FREEZING','BURNING','FOOD','WATER','GUN','MELEE','ZOMBIE','ANIMAL','SUICIDE','KILL','INFECTION','PUNCH','BREATH','ROADKILL','VEHICLE','GRENADE','SHRED','LANDMINE','ARENA','MISSILE','CHARGE','SPLASH','SENTRY','ACID','BOULDER','BURNER','SPIT','SPARK')", nullable: false),
                    TimeDeployedSeconds = table.Column<float>(nullable: false),
                    Distance = table.Column<float>(nullable: true),
                    IsTeamkill = table.Column<bool>(nullable: false),
                    IsSuicide = table.Column<bool>(nullable: false),
                    PrimaryAsset = table.Column<string>(type: "char(32)", nullable: true),
                    SecondaryAsset = table.Column<string>(type: "char(32)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stats_deaths", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stats_deaths_users_Instigator",
                        column: x => x.Instigator,
                        principalTable: "users",
                        principalColumn: "Steam64");
                    table.ForeignKey(
                        name: "FK_stats_deaths_stats_sessions_InstigatorSession",
                        column: x => x.InstigatorSession,
                        principalTable: "stats_sessions",
                        principalColumn: "SessionId");
                    table.ForeignKey(
                        name: "FK_stats_deaths_users_RelatedPlayer",
                        column: x => x.RelatedPlayer,
                        principalTable: "users",
                        principalColumn: "Steam64");
                    table.ForeignKey(
                        name: "FK_stats_deaths_stats_sessions_RelatedPlayerSession",
                        column: x => x.RelatedPlayerSession,
                        principalTable: "stats_sessions",
                        principalColumn: "SessionId");
                    table.ForeignKey(
                        name: "FK_stats_deaths_stats_sessions_Session",
                        column: x => x.Session,
                        principalTable: "stats_sessions",
                        principalColumn: "SessionId");
                    table.ForeignKey(
                        name: "FK_stats_deaths_users_Steam64",
                        column: x => x.Steam64,
                        principalTable: "users",
                        principalColumn: "Steam64");
                });

            migrationBuilder.CreateIndex(
                name: "IX_faction_assets_Faction",
                table: "faction_assets",
                column: "Faction");

            migrationBuilder.CreateIndex(
                name: "IX_faction_translations_Faction",
                table: "faction_translations",
                column: "Faction");

            migrationBuilder.CreateIndex(
                name: "IX_faction_translations_Language",
                table: "faction_translations",
                column: "Language");

            migrationBuilder.CreateIndex(
                name: "IX_hwids_HWID",
                table: "hwids",
                column: "HWID");

            migrationBuilder.CreateIndex(
                name: "IX_hwids_Steam64",
                table: "hwids",
                column: "Steam64");

            migrationBuilder.CreateIndex(
                name: "IX_ip_addresses_Packed",
                table: "ip_addresses",
                column: "Packed");

            migrationBuilder.CreateIndex(
                name: "IX_ip_addresses_Steam64",
                table: "ip_addresses",
                column: "Steam64");

            migrationBuilder.CreateIndex(
                name: "IX_kits_Faction",
                table: "kits",
                column: "Faction");

            migrationBuilder.CreateIndex(
                name: "IX_kits_access_Steam64",
                table: "kits_access",
                column: "Steam64");

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
                name: "IX_kits_favorites_Steam64",
                table: "kits_favorites",
                column: "Steam64");

            migrationBuilder.CreateIndex(
                name: "IX_kits_hotkeys_Steam64",
                table: "kits_hotkeys",
                column: "Steam64");

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
                name: "IX_lang_aliases_Language",
                table: "lang_aliases",
                column: "Language");

            migrationBuilder.CreateIndex(
                name: "IX_lang_credits_Contributor",
                table: "lang_credits",
                column: "Contributor");

            migrationBuilder.CreateIndex(
                name: "IX_lang_credits_Language",
                table: "lang_credits",
                column: "Language");

            migrationBuilder.CreateIndex(
                name: "IX_lang_cultures_Language",
                table: "lang_cultures",
                column: "Language");

            migrationBuilder.CreateIndex(
                name: "IX_lang_preferences_Language",
                table: "lang_preferences",
                column: "Language");

            migrationBuilder.CreateIndex(
                name: "IX_maps_SeasonReleased",
                table: "maps",
                column: "SeasonReleased");

            migrationBuilder.CreateIndex(
                name: "IX_stats_aid_records_Instigator",
                table: "stats_aid_records",
                column: "Instigator");

            migrationBuilder.CreateIndex(
                name: "IX_stats_aid_records_InstigatorSession",
                table: "stats_aid_records",
                column: "InstigatorSession");

            migrationBuilder.CreateIndex(
                name: "IX_stats_aid_records_Session",
                table: "stats_aid_records",
                column: "Session");

            migrationBuilder.CreateIndex(
                name: "IX_stats_aid_records_Steam64",
                table: "stats_aid_records",
                column: "Steam64");

            migrationBuilder.CreateIndex(
                name: "IX_stats_aid_records_Team",
                table: "stats_aid_records",
                column: "Team");

            migrationBuilder.CreateIndex(
                name: "IX_stats_damage_Instigator",
                table: "stats_damage",
                column: "Instigator");

            migrationBuilder.CreateIndex(
                name: "IX_stats_damage_InstigatorSession",
                table: "stats_damage",
                column: "InstigatorSession");

            migrationBuilder.CreateIndex(
                name: "IX_stats_damage_RelatedPlayer",
                table: "stats_damage",
                column: "RelatedPlayer");

            migrationBuilder.CreateIndex(
                name: "IX_stats_damage_RelatedPlayerSession",
                table: "stats_damage",
                column: "RelatedPlayerSession");

            migrationBuilder.CreateIndex(
                name: "IX_stats_damage_Session",
                table: "stats_damage",
                column: "Session");

            migrationBuilder.CreateIndex(
                name: "IX_stats_damage_Steam64",
                table: "stats_damage",
                column: "Steam64");

            migrationBuilder.CreateIndex(
                name: "IX_stats_damage_Team",
                table: "stats_damage",
                column: "Team");

            migrationBuilder.CreateIndex(
                name: "IX_stats_deaths_Instigator",
                table: "stats_deaths",
                column: "Instigator");

            migrationBuilder.CreateIndex(
                name: "IX_stats_deaths_InstigatorSession",
                table: "stats_deaths",
                column: "InstigatorSession");

            migrationBuilder.CreateIndex(
                name: "IX_stats_deaths_RelatedPlayer",
                table: "stats_deaths",
                column: "RelatedPlayer");

            migrationBuilder.CreateIndex(
                name: "IX_stats_deaths_RelatedPlayerSession",
                table: "stats_deaths",
                column: "RelatedPlayerSession");

            migrationBuilder.CreateIndex(
                name: "IX_stats_deaths_Session",
                table: "stats_deaths",
                column: "Session");

            migrationBuilder.CreateIndex(
                name: "IX_stats_deaths_Steam64",
                table: "stats_deaths",
                column: "Steam64");

            migrationBuilder.CreateIndex(
                name: "IX_stats_deaths_Team",
                table: "stats_deaths",
                column: "Team");

            migrationBuilder.CreateIndex(
                name: "IX_stats_games_Winner",
                table: "stats_games",
                column: "Winner");

            migrationBuilder.CreateIndex(
                name: "IX_stats_sessions_Faction",
                table: "stats_sessions",
                column: "Faction");

            migrationBuilder.CreateIndex(
                name: "IX_stats_sessions_Game",
                table: "stats_sessions",
                column: "Game");

            migrationBuilder.CreateIndex(
                name: "IX_stats_sessions_Kit",
                table: "stats_sessions",
                column: "Kit");

            migrationBuilder.CreateIndex(
                name: "IX_stats_sessions_Map",
                table: "stats_sessions",
                column: "Map");

            migrationBuilder.CreateIndex(
                name: "IX_stats_sessions_PlayerDataSteam64",
                table: "stats_sessions",
                column: "PlayerDataSteam64");

            migrationBuilder.CreateIndex(
                name: "IX_stats_sessions_PreviousSession",
                table: "stats_sessions",
                column: "PreviousSession",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stats_sessions_Season",
                table: "stats_sessions",
                column: "Season");

            migrationBuilder.CreateIndex(
                name: "IX_stats_sessions_Steam64",
                table: "stats_sessions",
                column: "Steam64");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "faction_assets");

            migrationBuilder.DropTable(
                name: "faction_translations");

            migrationBuilder.DropTable(
                name: "hwids");

            migrationBuilder.DropTable(
                name: "ip_addresses");

            migrationBuilder.DropTable(
                name: "kits_access");

            migrationBuilder.DropTable(
                name: "kits_bundle_items");

            migrationBuilder.DropTable(
                name: "kits_faction_filters");

            migrationBuilder.DropTable(
                name: "kits_favorites");

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
                name: "maps_dependencies");

            migrationBuilder.DropTable(
                name: "stats_aid_records");

            migrationBuilder.DropTable(
                name: "stats_damage");

            migrationBuilder.DropTable(
                name: "stats_deaths");

            migrationBuilder.DropTable(
                name: "kits_bundles");

            migrationBuilder.DropTable(
                name: "lang_info");

            migrationBuilder.DropTable(
                name: "stats_sessions");

            migrationBuilder.DropTable(
                name: "stats_games");

            migrationBuilder.DropTable(
                name: "kits");

            migrationBuilder.DropTable(
                name: "maps");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "factions");

            migrationBuilder.DropTable(
                name: "seasons");
        }
    }
}

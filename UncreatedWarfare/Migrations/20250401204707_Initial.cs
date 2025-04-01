using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using Uncreated.Warfare.Moderation;

namespace Uncreated.Warfare.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "homebase_auth_keys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AuthKey = table.Column<string>(type: "char(32)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Identity = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Region = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    LastConnectTime = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_homebase_auth_keys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "item_whitelists",
                columns: table => new
                {
                    pk = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Item = table.Column<string>(type: "char(32)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Amount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_whitelists", x => x.pk);
                });

            migrationBuilder.CreateTable(
                name: "lang_info",
                columns: table => new
                {
                    pk = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "char(5)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayName = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NativeName = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DefaultCultureCode = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HasTranslationSupport = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    SupportsPluralization = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RequiresIMGUI = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    FallbackTranslationLanguageCode = table.Column<string>(type: "char(5)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SteamLanguageName = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lang_info", x => x.pk);
                });

            migrationBuilder.CreateTable(
                name: "moderation_global_ban_whitelist",
                columns: table => new
                {
                    Steam64 = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EffectiveTimeUTC = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moderation_global_ban_whitelist", x => x.Steam64);
                });

            migrationBuilder.CreateTable(
                name: "seasons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ReleaseTimestampUTC = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seasons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_permissions",
                columns: table => new
                {
                    pk = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Steam64 = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    IsGroup = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PermissionOrGroup = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_permissions", x => x.pk);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Steam64 = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PlayerName = table.Column<string>(type: "varchar(48)", maxLength: 48, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CharacterName = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NickName = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayName = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FirstJoined = table.Column<DateTime>(type: "datetime", nullable: true),
                    LastJoined = table.Column<DateTime>(type: "datetime", nullable: true),
                    DiscordId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    LastPrivacyPolicyAccepted = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Steam64);
                });

            migrationBuilder.CreateTable(
                name: "warfare_user_pending_accout_links",
                columns: table => new
                {
                    pk = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Steam64 = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    DiscordId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    Token = table.Column<string>(type: "char(9)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StartedTimestampUTC = table.Column<DateTime>(type: "datetime", nullable: false),
                    ExpiryTimestampUTC = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_warfare_user_pending_accout_links", x => x.pk);
                });

            migrationBuilder.CreateTable(
                name: "lang_aliases",
                columns: table => new
                {
                    pk = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Language = table.Column<uint>(type: "int unsigned", nullable: false),
                    Alias = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
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
                    pk = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Language = table.Column<uint>(type: "int unsigned", nullable: false),
                    CultureCode = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
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
                name: "hwids",
                columns: table => new
                {
                    Id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Index = table.Column<int>(type: "int", nullable: false),
                    Steam64 = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    HWID = table.Column<byte[]>(type: "binary(20)", nullable: false),
                    LoginCount = table.Column<int>(type: "int", nullable: false),
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
                    Id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    LoginCount = table.Column<int>(type: "int", nullable: false),
                    Steam64 = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    FirstLogin = table.Column<DateTime>(type: "datetime", nullable: true),
                    LastLogin = table.Column<DateTime>(type: "datetime", nullable: false),
                    Packed = table.Column<uint>(type: "int unsigned", nullable: false),
                    Unpacked = table.Column<string>(type: "varchar(45)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
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
                    pk = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Language = table.Column<uint>(type: "int unsigned", nullable: false),
                    Contributor = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lang_credits", x => x.pk);
                    table.ForeignKey(
                        name: "FK_lang_credits_lang_info_Language",
                        column: x => x.Language,
                        principalTable: "lang_info",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_lang_credits_users_Contributor",
                        column: x => x.Contributor,
                        principalTable: "users",
                        principalColumn: "Steam64");
                });

            migrationBuilder.CreateTable(
                name: "lang_preferences",
                columns: table => new
                {
                    Steam64 = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Language = table.Column<uint>(type: "int unsigned", nullable: false),
                    Culture = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TimeZone = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UseCultureForCmdInput = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
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
                name: "buildables_display_data",
                columns: table => new
                {
                    pk = table.Column<int>(type: "int", nullable: false),
                    Skin = table.Column<string>(type: "char(32)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Mythic = table.Column<string>(type: "char(32)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Rotation = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Tags = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DynamicProps = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_buildables_display_data", x => x.pk);
                });

            migrationBuilder.CreateTable(
                name: "buildables_instance_ids",
                columns: table => new
                {
                    pk = table.Column<int>(type: "int", nullable: false),
                    RegionId = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    InstanceId = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_buildables_instance_ids", x => new { x.pk, x.RegionId });
                });

            migrationBuilder.CreateTable(
                name: "buildables_stored_items",
                columns: table => new
                {
                    Save = table.Column<int>(type: "int", nullable: false),
                    PositionX = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    PositionY = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Item = table.Column<string>(type: "char(32)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Amount = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Quality = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Rotation = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    State = table.Column<byte[]>(type: "varbinary(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_buildables_stored_items", x => new { x.Save, x.PositionX, x.PositionY });
                });

            migrationBuilder.CreateTable(
                name: "faction_assets",
                columns: table => new
                {
                    pk = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Faction = table.Column<uint>(type: "int unsigned", nullable: false),
                    Redirect = table.Column<string>(type: "enum('Shirt','Pants','Vest','Hat','Mask','Backpack','Glasses','AmmoSupply','BuildSupply','RallyPoint','Radio','AmmoBag','AmmoCrate','RepairStation','Bunker','VehicleBay','EntrenchingTool','UAV','RepairStationBuilt','AmmoCrateBuilt','BunkerBuilt','Cache','RadioDamaged','LaserDesignator','MapTackFlag')", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Asset = table.Column<string>(type: "char(32)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VariantKey = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_faction_assets", x => x.pk);
                });

            migrationBuilder.CreateTable(
                name: "faction_translations",
                columns: table => new
                {
                    pk = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Faction = table.Column<uint>(type: "int unsigned", nullable: false),
                    Language = table.Column<uint>(type: "int unsigned", nullable: false),
                    Name = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ShortName = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Abbreviation = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_faction_translations", x => x.pk);
                    table.ForeignKey(
                        name: "FK_faction_translations_lang_info_Language",
                        column: x => x.Language,
                        principalTable: "lang_info",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "kits",
                columns: table => new
                {
                    pk = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Faction = table.Column<uint>(type: "int unsigned", nullable: true),
                    Id = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Class = table.Column<string>(type: "enum('Unarmed','Squadleader','Rifleman','Medic','Breacher','AutomaticRifleman','Grenadier','MachineGunner','LAT','HAT','Marksman','Sniper','APRifleman','CombatEngineer','Crewman','Pilot','SpecOps')", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Branch = table.Column<string>(type: "enum('Infantry','Armor','Airforce','SpecOps','Navy')", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<string>(type: "enum('Public','Elite','Special','Loadout','Template')", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Disabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RequiresNitro = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MapFilterIsWhitelist = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FactionFilterIsWhitelist = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Season = table.Column<int>(type: "int", nullable: false),
                    RequestCooldown = table.Column<float>(type: "float", nullable: false),
                    MinRequiredSquadMembers = table.Column<int>(type: "int", nullable: true),
                    RequiresSquad = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreditCost = table.Column<int>(type: "int", nullable: false),
                    PremiumCost = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    SquadLevel = table.Column<string>(type: "enum('Member','Commander')", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Weapons = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    Creator = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    LastEditedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    LastEditor = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kits", x => x.pk);
                });

            migrationBuilder.CreateTable(
                name: "factions",
                columns: table => new
                {
                    pk = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Id = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    KitPrefix = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ShortName = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Abbreviation = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HexColor = table.Column<string>(type: "char(6)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UnarmedKitId = table.Column<uint>(type: "int unsigned", nullable: true),
                    FlagImageUrl = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SpriteIndex = table.Column<int>(type: "int", nullable: true),
                    Emoji = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_factions", x => x.pk);
                    table.ForeignKey(
                        name: "FK_factions_kits_UnarmedKitId",
                        column: x => x.UnarmedKitId,
                        principalTable: "kits",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "kits_access",
                columns: table => new
                {
                    Kit = table.Column<uint>(type: "int unsigned", nullable: false),
                    Steam64 = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    AccessType = table.Column<string>(type: "enum('Unknown','Credits','Event','Purchase','QuestReward')", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                        principalColumn: "Steam64",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "kits_delays",
                columns: table => new
                {
                    pk = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Kit = table.Column<uint>(type: "int unsigned", nullable: false),
                    KitModelPrimaryKey = table.Column<uint>(type: "int unsigned", nullable: true),
                    Data = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kits_delays", x => x.pk);
                    table.ForeignKey(
                        name: "FK_kits_delays_kits_KitModelPrimaryKey",
                        column: x => x.KitModelPrimaryKey,
                        principalTable: "kits",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "kits_favorites",
                columns: table => new
                {
                    Kit = table.Column<uint>(type: "int unsigned", nullable: false),
                    Steam64 = table.Column<ulong>(type: "bigint unsigned", nullable: false)
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
                        principalColumn: "Steam64",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "kits_hotkeys",
                columns: table => new
                {
                    Steam64 = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Kit = table.Column<uint>(type: "int unsigned", nullable: false),
                    Slot = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Page = table.Column<string>(type: "enum('Primary','Secondary','Hands','Backpack','Vest','Shirt','Pants','Storage','Area')", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    X = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Y = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Item = table.Column<string>(type: "char(32)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Redirect = table.Column<string>(type: "enum('Shirt','Pants','Vest','Hat','Mask','Backpack','Glasses','AmmoSupply','BuildSupply','RallyPoint','Radio','AmmoBag','AmmoCrate','RepairStation','Bunker','VehicleBay','EntrenchingTool','UAV','RepairStationBuilt','AmmoCrateBuilt','BunkerBuilt','Cache','RadioDamaged','LaserDesignator','MapTackFlag')", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
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
                        principalColumn: "Steam64",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "kits_items",
                columns: table => new
                {
                    pk = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Kit = table.Column<uint>(type: "int unsigned", nullable: false),
                    Item = table.Column<string>(type: "char(32)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    X = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    Y = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    Rotation = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    Page = table.Column<string>(type: "enum('Primary','Secondary','Hands','Backpack','Vest','Shirt','Pants','Storage','Area')", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClothingSlot = table.Column<string>(type: "enum('Shirt','Pants','Vest','Hat','Mask','Backpack','Glasses')", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Redirect = table.Column<string>(type: "enum('Shirt','Pants','Vest','Hat','Mask','Backpack','Glasses','AmmoSupply','BuildSupply','RallyPoint','Radio','AmmoBag','AmmoCrate','RepairStation','Bunker','VehicleBay','EntrenchingTool','UAV','RepairStationBuilt','AmmoCrateBuilt','BunkerBuilt','Cache','RadioDamaged','LaserDesignator','MapTackFlag')", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RedirectVariant = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Amount = table.Column<byte>(type: "tinyint unsigned", nullable: true),
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
                    pk = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Steam64 = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Kit = table.Column<uint>(type: "int unsigned", nullable: false),
                    OldPage = table.Column<string>(type: "enum('Primary','Secondary','Hands','Backpack','Vest','Shirt','Pants','Storage','Area')", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OldX = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    OldY = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    NewPage = table.Column<string>(type: "enum('Primary','Secondary','Hands','Backpack','Vest','Shirt','Pants','Storage','Area')", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NewX = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    NewY = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    NewRotation = table.Column<byte>(type: "tinyint unsigned", nullable: false)
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
                        principalColumn: "Steam64",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "kits_map_filters",
                columns: table => new
                {
                    Kit = table.Column<uint>(type: "int unsigned", nullable: false),
                    Map = table.Column<uint>(type: "int unsigned", nullable: false)
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
                    Language = table.Column<uint>(type: "int unsigned", nullable: false),
                    Kit = table.Column<uint>(type: "int unsigned", nullable: false),
                    Value = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
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
                });

            migrationBuilder.CreateTable(
                name: "kits_skillsets",
                columns: table => new
                {
                    pk = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Kit = table.Column<uint>(type: "int unsigned", nullable: false),
                    Skill = table.Column<string>(type: "enum('OVERKILL','SHARPSHOOTER','DEXTERITY','CARDIO','EXERCISE','DIVING','PARKOUR','SNEAKYBEAKY','VITALITY','IMMUNITY','TOUGHNESS','STRENGTH','WARMBLOODED','SURVIVAL','HEALING','CRAFTING','OUTDOORS','COOKING','FISHING','AGRICULTURE','MECHANIC','ENGINEER')", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Level = table.Column<byte>(type: "tinyint unsigned", nullable: false)
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
                    pk = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Kit = table.Column<uint>(type: "int unsigned", nullable: false),
                    Type = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Data = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
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
                name: "loadout_purchases",
                columns: table => new
                {
                    CreatedKit = table.Column<uint>(type: "int unsigned", nullable: false),
                    LoadoutId = table.Column<int>(type: "int", nullable: false),
                    Steam64 = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Paid = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Status = table.Column<string>(type: "enum('AwaitingApproval','ChangesRequested','InProgress','Completed')", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Edit = table.Column<string>(type: "enum('None','EditRequested','EditAllowed','SeasonalUpdate')", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Created = table.Column<DateTime>(type: "datetime", nullable: false),
                    FormModified = table.Column<DateTime>(type: "datetime", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime", nullable: false),
                    Season = table.Column<int>(type: "int", nullable: false),
                    FormYaml = table.Column<string>(type: "TEXT", maxLength: 65535, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AdminChangeRequest = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AdminChangeRequester = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    AdminChangeRequestDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    PlayerChangeRequest = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PlayerChangeRequestDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    StripeSessionId = table.Column<string>(type: "varchar(70)", maxLength: 70, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loadout_purchases", x => x.CreatedKit);
                    table.ForeignKey(
                        name: "FK_loadout_purchases_kits_CreatedKit",
                        column: x => x.CreatedKit,
                        principalTable: "kits",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_loadout_purchases_users_AdminChangeRequester",
                        column: x => x.AdminChangeRequester,
                        principalTable: "users",
                        principalColumn: "Steam64",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_loadout_purchases_users_Steam64",
                        column: x => x.Steam64,
                        principalTable: "users",
                        principalColumn: "Steam64",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "kits_bundles",
                columns: table => new
                {
                    pk = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Id = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayName = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Faction = table.Column<uint>(type: "int unsigned", nullable: true),
                    Cost = table.Column<decimal>(type: "decimal(65,30)", nullable: false)
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
                name: "kits_faction_filters",
                columns: table => new
                {
                    Kit = table.Column<uint>(type: "int unsigned", nullable: false),
                    Faction = table.Column<uint>(type: "int unsigned", nullable: false)
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
                name: "maps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DisplayName = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WorkshopId = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    SeasonReleased = table.Column<int>(type: "int", nullable: false),
                    Team1Faction = table.Column<uint>(type: "int unsigned", nullable: false),
                    Team2Faction = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_maps_factions_Team1Faction",
                        column: x => x.Team1Faction,
                        principalTable: "factions",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_maps_factions_Team2Faction",
                        column: x => x.Team2Faction,
                        principalTable: "factions",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_maps_seasons_SeasonReleased",
                        column: x => x.SeasonReleased,
                        principalTable: "seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stats_games",
                columns: table => new
                {
                    GameId = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Season = table.Column<int>(type: "int", nullable: false),
                    Map = table.Column<int>(type: "int", nullable: false),
                    Region = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    StartTimestamp = table.Column<DateTime>(type: "datetime", nullable: false),
                    EndTimestamp = table.Column<DateTime>(type: "datetime", nullable: true),
                    Gamemode = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Winner = table.Column<uint>(type: "int unsigned", nullable: true)
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
                name: "kits_bundle_items",
                columns: table => new
                {
                    Bundle = table.Column<uint>(type: "int unsigned", nullable: false),
                    Kit = table.Column<uint>(type: "int unsigned", nullable: false)
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
                name: "buildables",
                columns: table => new
                {
                    pk = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Map = table.Column<int>(type: "int", nullable: true),
                    Item = table.Column<string>(type: "char(32)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsStructure = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PositionX = table.Column<float>(type: "float", nullable: false),
                    PositionY = table.Column<float>(type: "float", nullable: false),
                    PositionZ = table.Column<float>(type: "float", nullable: false),
                    RotationX = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    RotationY = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    RotationZ = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Owner = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Group = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    State = table.Column<byte[]>(type: "varbinary(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_buildables", x => x.pk);
                    table.ForeignKey(
                        name: "FK_buildables_maps_Map",
                        column: x => x.Map,
                        principalTable: "maps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "maps_dependencies",
                columns: table => new
                {
                    WorkshopId = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Map = table.Column<int>(type: "int", nullable: false),
                    IsRemoved = table.Column<bool>(type: "tinyint(1)", nullable: false)
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
                    SessionId = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Steam64 = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Game = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Season = table.Column<int>(type: "int", nullable: false),
                    Map = table.Column<int>(type: "int", nullable: false),
                    Team = table.Column<int>(type: "int", nullable: false),
                    StartedTimestampUTC = table.Column<DateTime>(type: "datetime", nullable: false),
                    EndedTimestampUTC = table.Column<DateTime>(type: "datetime", nullable: true),
                    LengthSeconds = table.Column<double>(type: "double", nullable: false),
                    SquadName = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SquadLeader = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    PreviousSession = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    NextSession = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    Faction = table.Column<uint>(type: "int unsigned", nullable: true),
                    Kit = table.Column<uint>(type: "int unsigned", nullable: true),
                    KitName = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StartedGame = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FinishedGame = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UnexpectedTermination = table.Column<bool>(type: "tinyint(1)", nullable: false)
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
                        name: "FK_stats_sessions_kits_Kit",
                        column: x => x.Kit,
                        principalTable: "kits",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_stats_sessions_maps_Map",
                        column: x => x.Map,
                        principalTable: "maps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stats_sessions_seasons_Season",
                        column: x => x.Season,
                        principalTable: "seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stats_sessions_stats_games_Game",
                        column: x => x.Game,
                        principalTable: "stats_games",
                        principalColumn: "GameId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stats_sessions_stats_sessions_NextSession",
                        column: x => x.NextSession,
                        principalTable: "stats_sessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_stats_sessions_stats_sessions_PreviousSession",
                        column: x => x.PreviousSession,
                        principalTable: "stats_sessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_stats_sessions_users_SquadLeader",
                        column: x => x.SquadLeader,
                        principalTable: "users",
                        principalColumn: "Steam64",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stats_sessions_users_Steam64",
                        column: x => x.Steam64,
                        principalTable: "users",
                        principalColumn: "Steam64",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stats_aid_records",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Item = table.Column<string>(type: "char(32)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ItemName = table.Column<string>(type: "varchar(48)", maxLength: 48, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Health = table.Column<float>(type: "float", nullable: false),
                    IsRevive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Team = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Steam64 = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    Session = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    PositionX = table.Column<float>(type: "float", nullable: false),
                    PositionY = table.Column<float>(type: "float", nullable: false),
                    PositionZ = table.Column<float>(type: "float", nullable: false),
                    NearestLocation = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TimestampUTC = table.Column<DateTime>(type: "datetime", nullable: false),
                    Instigator = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    InstigatorSession = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    InstigatorPositionX = table.Column<float>(type: "float", nullable: true),
                    InstigatorPositionY = table.Column<float>(type: "float", nullable: true),
                    InstigatorPositionZ = table.Column<float>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stats_aid_records", x => x.Id);
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
                        name: "FK_stats_aid_records_users_Instigator",
                        column: x => x.Instigator,
                        principalTable: "users",
                        principalColumn: "Steam64");
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
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PrimaryAsset = table.Column<string>(type: "char(32)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PrimaryAssetName = table.Column<string>(type: "varchar(48)", maxLength: 48, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SecondaryAsset = table.Column<string>(type: "char(32)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SecondaryAssetName = table.Column<string>(type: "varchar(48)", maxLength: 48, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Vehicle = table.Column<string>(type: "char(32)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VehicleName = table.Column<string>(type: "varchar(48)", maxLength: 48, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Cause = table.Column<string>(type: "enum('BLEEDING','BONES','FREEZING','BURNING','FOOD','WATER','GUN','MELEE','ZOMBIE','ANIMAL','SUICIDE','KILL','INFECTION','PUNCH','BREATH','ROADKILL','VEHICLE','GRENADE','SHRED','LANDMINE','ARENA','MISSILE','CHARGE','SPLASH','SENTRY','ACID','BOULDER','BURNER','SPIT','SPARK','37')", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Distance = table.Column<float>(type: "float", nullable: true),
                    Damage = table.Column<float>(type: "float", nullable: false),
                    TimeDeployedSeconds = table.Column<float>(type: "float", nullable: false),
                    IsTeamkill = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsSuicide = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsInjure = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsInjured = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Limb = table.Column<string>(type: "enum('LEFT_FOOT','LEFT_LEG','RIGHT_FOOT','RIGHT_LEG','LEFT_HAND','LEFT_ARM','RIGHT_HAND','RIGHT_ARM','LEFT_BACK','RIGHT_BACK','LEFT_FRONT','RIGHT_FRONT','SPINE','SKULL')", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Team = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Steam64 = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    Session = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    PositionX = table.Column<float>(type: "float", nullable: false),
                    PositionY = table.Column<float>(type: "float", nullable: false),
                    PositionZ = table.Column<float>(type: "float", nullable: false),
                    NearestLocation = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TimestampUTC = table.Column<DateTime>(type: "datetime", nullable: false),
                    Instigator = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    InstigatorSession = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    InstigatorPositionX = table.Column<float>(type: "float", nullable: true),
                    InstigatorPositionY = table.Column<float>(type: "float", nullable: true),
                    InstigatorPositionZ = table.Column<float>(type: "float", nullable: true),
                    RelatedPlayer = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    RelatedPlayerSession = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    RelatedPlayerPositionX = table.Column<float>(type: "float", nullable: true),
                    RelatedPlayerPositionY = table.Column<float>(type: "float", nullable: true),
                    RelatedPlayerPositionZ = table.Column<float>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stats_damage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stats_damage_stats_sessions_InstigatorSession",
                        column: x => x.InstigatorSession,
                        principalTable: "stats_sessions",
                        principalColumn: "SessionId");
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
                        name: "FK_stats_damage_users_Instigator",
                        column: x => x.Instigator,
                        principalTable: "users",
                        principalColumn: "Steam64");
                    table.ForeignKey(
                        name: "FK_stats_damage_users_RelatedPlayer",
                        column: x => x.RelatedPlayer,
                        principalTable: "users",
                        principalColumn: "Steam64");
                    table.ForeignKey(
                        name: "FK_stats_damage_users_Steam64",
                        column: x => x.Steam64,
                        principalTable: "users",
                        principalColumn: "Steam64");
                });

            migrationBuilder.CreateTable(
                name: "stats_fobs",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FobType = table.Column<string>(type: "enum('Other','BunkerFob','Cache')", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PrimaryAsset = table.Column<string>(type: "char(32)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PrimaryAssetName = table.Column<string>(type: "varchar(48)", maxLength: 48, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SecondaryAsset = table.Column<string>(type: "char(32)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SecondaryAssetName = table.Column<string>(type: "varchar(48)", maxLength: 48, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FobName = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeploymentCount = table.Column<int>(type: "int", nullable: false),
                    TeleportCount = table.Column<int>(type: "int", nullable: false),
                    FortificationsBuilt = table.Column<int>(type: "int", nullable: false),
                    FortificationsDestroyed = table.Column<int>(type: "int", nullable: false),
                    EmplacementsBuilt = table.Column<int>(type: "int", nullable: false),
                    EmplacementsDestroyed = table.Column<int>(type: "int", nullable: false),
                    BunkersBuilt = table.Column<int>(type: "int", nullable: false),
                    BunkersDestroyed = table.Column<int>(type: "int", nullable: false),
                    AmmoCratesBuilt = table.Column<int>(type: "int", nullable: false),
                    AmmoCratesDestroyed = table.Column<int>(type: "int", nullable: false),
                    RepairStationsBuilt = table.Column<int>(type: "int", nullable: false),
                    RepairStationsDestroyed = table.Column<int>(type: "int", nullable: false),
                    EmplacementPlayerKills = table.Column<int>(type: "int", nullable: false),
                    EmplacementVehicleKills = table.Column<int>(type: "int", nullable: false),
                    AmmoSpent = table.Column<int>(type: "int", nullable: false),
                    BuildSpent = table.Column<int>(type: "int", nullable: false),
                    AmmoLoaded = table.Column<int>(type: "int", nullable: false),
                    BuildLoaded = table.Column<int>(type: "int", nullable: false),
                    DestroyedByRoundEnd = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Teamkilled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DestroyedAtUTC = table.Column<DateTime>(type: "datetime", nullable: true),
                    FobAngleX = table.Column<float>(type: "float", nullable: false),
                    FobAngleY = table.Column<float>(type: "float", nullable: false),
                    FobAngleZ = table.Column<float>(type: "float", nullable: false),
                    Team = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Steam64 = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    Session = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    PositionX = table.Column<float>(type: "float", nullable: false),
                    PositionY = table.Column<float>(type: "float", nullable: false),
                    PositionZ = table.Column<float>(type: "float", nullable: false),
                    NearestLocation = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TimestampUTC = table.Column<DateTime>(type: "datetime", nullable: false),
                    Instigator = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    InstigatorSession = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    InstigatorPositionX = table.Column<float>(type: "float", nullable: true),
                    InstigatorPositionY = table.Column<float>(type: "float", nullable: true),
                    InstigatorPositionZ = table.Column<float>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stats_fobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stats_fobs_stats_sessions_InstigatorSession",
                        column: x => x.InstigatorSession,
                        principalTable: "stats_sessions",
                        principalColumn: "SessionId");
                    table.ForeignKey(
                        name: "FK_stats_fobs_stats_sessions_Session",
                        column: x => x.Session,
                        principalTable: "stats_sessions",
                        principalColumn: "SessionId");
                    table.ForeignKey(
                        name: "FK_stats_fobs_users_Instigator",
                        column: x => x.Instigator,
                        principalTable: "users",
                        principalColumn: "Steam64");
                    table.ForeignKey(
                        name: "FK_stats_fobs_users_Steam64",
                        column: x => x.Steam64,
                        principalTable: "users",
                        principalColumn: "Steam64");
                });

            migrationBuilder.CreateTable(
                name: "stats_deaths",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DeathMessage = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeathCause = table.Column<string>(type: "enum('BLEEDING','BONES','FREEZING','BURNING','FOOD','WATER','GUN','MELEE','ZOMBIE','ANIMAL','SUICIDE','KILL','INFECTION','PUNCH','BREATH','ROADKILL','VEHICLE','GRENADE','SHRED','LANDMINE','ARENA','MISSILE','CHARGE','SPLASH','SENTRY','ACID','BOULDER','BURNER','SPIT','SPARK','37')", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TimeDeployedSeconds = table.Column<float>(type: "float", nullable: false),
                    Distance = table.Column<float>(type: "float", nullable: true),
                    IsTeamkill = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsSuicide = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsBleedout = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    KillShot = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    PrimaryAsset = table.Column<string>(type: "char(32)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PrimaryAssetName = table.Column<string>(type: "varchar(48)", maxLength: 48, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SecondaryAsset = table.Column<string>(type: "char(32)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SecondaryAssetName = table.Column<string>(type: "varchar(48)", maxLength: 48, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Vehicle = table.Column<string>(type: "char(32)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VehicleName = table.Column<string>(type: "varchar(48)", maxLength: 48, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Team = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Steam64 = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    Session = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    PositionX = table.Column<float>(type: "float", nullable: false),
                    PositionY = table.Column<float>(type: "float", nullable: false),
                    PositionZ = table.Column<float>(type: "float", nullable: false),
                    NearestLocation = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TimestampUTC = table.Column<DateTime>(type: "datetime", nullable: false),
                    Instigator = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    InstigatorSession = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    InstigatorPositionX = table.Column<float>(type: "float", nullable: true),
                    InstigatorPositionY = table.Column<float>(type: "float", nullable: true),
                    InstigatorPositionZ = table.Column<float>(type: "float", nullable: true),
                    RelatedPlayer = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    RelatedPlayerSession = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    RelatedPlayerPositionX = table.Column<float>(type: "float", nullable: true),
                    RelatedPlayerPositionY = table.Column<float>(type: "float", nullable: true),
                    RelatedPlayerPositionZ = table.Column<float>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stats_deaths", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stats_deaths_stats_damage_KillShot",
                        column: x => x.KillShot,
                        principalTable: "stats_damage",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stats_deaths_stats_sessions_InstigatorSession",
                        column: x => x.InstigatorSession,
                        principalTable: "stats_sessions",
                        principalColumn: "SessionId");
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
                        name: "FK_stats_deaths_users_Instigator",
                        column: x => x.Instigator,
                        principalTable: "users",
                        principalColumn: "Steam64");
                    table.ForeignKey(
                        name: "FK_stats_deaths_users_RelatedPlayer",
                        column: x => x.RelatedPlayer,
                        principalTable: "users",
                        principalColumn: "Steam64");
                    table.ForeignKey(
                        name: "FK_stats_deaths_users_Steam64",
                        column: x => x.Steam64,
                        principalTable: "users",
                        principalColumn: "Steam64");
                });

            migrationBuilder.CreateTable(
                name: "stats_fob_items",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Fob = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Type = table.Column<string>(type: "enum('Fob','AmmoCrate','RepairStation','Fortification','Emplacement')", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PlayerKills = table.Column<int>(type: "int", nullable: false),
                    VehicleKills = table.Column<int>(type: "int", nullable: false),
                    UseTimeSeconds = table.Column<double>(type: "double", nullable: false),
                    BuiltAtUTC = table.Column<DateTime>(type: "datetime", nullable: true),
                    DestroyedAtUTC = table.Column<DateTime>(type: "datetime", nullable: true),
                    Item = table.Column<string>(type: "char(32)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ItemName = table.Column<string>(type: "varchar(48)", maxLength: 48, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PrimaryAsset = table.Column<string>(type: "char(32)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PrimaryAssetName = table.Column<string>(type: "varchar(48)", maxLength: 48, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SecondaryAsset = table.Column<string>(type: "char(32)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SecondaryAssetName = table.Column<string>(type: "varchar(48)", maxLength: 48, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DestroyedByRoundEnd = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Teamkilled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FobItemPositionX = table.Column<float>(type: "float", nullable: false),
                    FobItemPositionY = table.Column<float>(type: "float", nullable: false),
                    FobItemPositionZ = table.Column<float>(type: "float", nullable: false),
                    FobItemAngleX = table.Column<float>(type: "float", nullable: false),
                    FobItemAngleY = table.Column<float>(type: "float", nullable: false),
                    FobItemAngleZ = table.Column<float>(type: "float", nullable: false),
                    Team = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Steam64 = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    Session = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    PositionX = table.Column<float>(type: "float", nullable: false),
                    PositionY = table.Column<float>(type: "float", nullable: false),
                    PositionZ = table.Column<float>(type: "float", nullable: false),
                    NearestLocation = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TimestampUTC = table.Column<DateTime>(type: "datetime", nullable: false),
                    Instigator = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    InstigatorSession = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    InstigatorPositionX = table.Column<float>(type: "float", nullable: true),
                    InstigatorPositionY = table.Column<float>(type: "float", nullable: true),
                    InstigatorPositionZ = table.Column<float>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stats_fob_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stats_fob_items_stats_fobs_Fob",
                        column: x => x.Fob,
                        principalTable: "stats_fobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_stats_fob_items_stats_sessions_InstigatorSession",
                        column: x => x.InstigatorSession,
                        principalTable: "stats_sessions",
                        principalColumn: "SessionId");
                    table.ForeignKey(
                        name: "FK_stats_fob_items_stats_sessions_Session",
                        column: x => x.Session,
                        principalTable: "stats_sessions",
                        principalColumn: "SessionId");
                    table.ForeignKey(
                        name: "FK_stats_fob_items_users_Instigator",
                        column: x => x.Instigator,
                        principalTable: "users",
                        principalColumn: "Steam64");
                    table.ForeignKey(
                        name: "FK_stats_fob_items_users_Steam64",
                        column: x => x.Steam64,
                        principalTable: "users",
                        principalColumn: "Steam64");
                });

            migrationBuilder.CreateTable(
                name: "stats_fob_items_builders",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FobItem = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Hits = table.Column<float>(type: "float", nullable: false),
                    Responsibility = table.Column<double>(type: "double", nullable: false),
                    Team = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Steam64 = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    Session = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    PositionX = table.Column<float>(type: "float", nullable: false),
                    PositionY = table.Column<float>(type: "float", nullable: false),
                    PositionZ = table.Column<float>(type: "float", nullable: false),
                    NearestLocation = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TimestampUTC = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stats_fob_items_builders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stats_fob_items_builders_stats_fob_items_FobItem",
                        column: x => x.FobItem,
                        principalTable: "stats_fob_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_stats_fob_items_builders_stats_sessions_Session",
                        column: x => x.Session,
                        principalTable: "stats_sessions",
                        principalColumn: "SessionId");
                    table.ForeignKey(
                        name: "FK_stats_fob_items_builders_users_Steam64",
                        column: x => x.Steam64,
                        principalTable: "users",
                        principalColumn: "Steam64");
                });

            migrationBuilder.CreateIndex(
                name: "IX_buildables_Map",
                table: "buildables",
                column: "Map");

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
                name: "IX_factions_UnarmedKitId",
                table: "factions",
                column: "UnarmedKitId");

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
                name: "IX_kits_Id",
                table: "kits",
                column: "Id",
                unique: true);

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
                name: "IX_kits_bundles_Id",
                table: "kits_bundles",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_kits_delays_KitModelPrimaryKey",
                table: "kits_delays",
                column: "KitModelPrimaryKey");

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
                name: "IX_loadout_purchases_AdminChangeRequester",
                table: "loadout_purchases",
                column: "AdminChangeRequester");

            migrationBuilder.CreateIndex(
                name: "IX_loadout_purchases_Steam64",
                table: "loadout_purchases",
                column: "Steam64");

            migrationBuilder.CreateIndex(
                name: "IX_maps_SeasonReleased",
                table: "maps",
                column: "SeasonReleased");

            migrationBuilder.CreateIndex(
                name: "IX_maps_Team1Faction",
                table: "maps",
                column: "Team1Faction");

            migrationBuilder.CreateIndex(
                name: "IX_maps_Team2Faction",
                table: "maps",
                column: "Team2Faction");

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
                name: "IX_stats_deaths_KillShot",
                table: "stats_deaths",
                column: "KillShot",
                unique: true);

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
                name: "IX_stats_fob_items_Fob",
                table: "stats_fob_items",
                column: "Fob");

            migrationBuilder.CreateIndex(
                name: "IX_stats_fob_items_Instigator",
                table: "stats_fob_items",
                column: "Instigator");

            migrationBuilder.CreateIndex(
                name: "IX_stats_fob_items_InstigatorSession",
                table: "stats_fob_items",
                column: "InstigatorSession");

            migrationBuilder.CreateIndex(
                name: "IX_stats_fob_items_Session",
                table: "stats_fob_items",
                column: "Session");

            migrationBuilder.CreateIndex(
                name: "IX_stats_fob_items_Steam64",
                table: "stats_fob_items",
                column: "Steam64");

            migrationBuilder.CreateIndex(
                name: "IX_stats_fob_items_Team",
                table: "stats_fob_items",
                column: "Team");

            migrationBuilder.CreateIndex(
                name: "IX_stats_fob_items_builders_FobItem",
                table: "stats_fob_items_builders",
                column: "FobItem");

            migrationBuilder.CreateIndex(
                name: "IX_stats_fob_items_builders_Session",
                table: "stats_fob_items_builders",
                column: "Session");

            migrationBuilder.CreateIndex(
                name: "IX_stats_fob_items_builders_Steam64",
                table: "stats_fob_items_builders",
                column: "Steam64");

            migrationBuilder.CreateIndex(
                name: "IX_stats_fob_items_builders_Team",
                table: "stats_fob_items_builders",
                column: "Team");

            migrationBuilder.CreateIndex(
                name: "IX_stats_fobs_Instigator",
                table: "stats_fobs",
                column: "Instigator");

            migrationBuilder.CreateIndex(
                name: "IX_stats_fobs_InstigatorSession",
                table: "stats_fobs",
                column: "InstigatorSession");

            migrationBuilder.CreateIndex(
                name: "IX_stats_fobs_Session",
                table: "stats_fobs",
                column: "Session");

            migrationBuilder.CreateIndex(
                name: "IX_stats_fobs_Steam64",
                table: "stats_fobs",
                column: "Steam64");

            migrationBuilder.CreateIndex(
                name: "IX_stats_fobs_Team",
                table: "stats_fobs",
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
                name: "IX_stats_sessions_NextSession",
                table: "stats_sessions",
                column: "NextSession");

            migrationBuilder.CreateIndex(
                name: "IX_stats_sessions_PreviousSession",
                table: "stats_sessions",
                column: "PreviousSession");

            migrationBuilder.CreateIndex(
                name: "IX_stats_sessions_Season",
                table: "stats_sessions",
                column: "Season");

            migrationBuilder.CreateIndex(
                name: "IX_stats_sessions_SquadLeader",
                table: "stats_sessions",
                column: "SquadLeader");

            migrationBuilder.CreateIndex(
                name: "IX_stats_sessions_Steam64",
                table: "stats_sessions",
                column: "Steam64");

            migrationBuilder.CreateIndex(
                name: "IX_user_permissions_Steam64",
                table: "user_permissions",
                column: "Steam64");

            migrationBuilder.CreateIndex(
                name: "IX_warfare_user_pending_accout_links_DiscordId",
                table: "warfare_user_pending_accout_links",
                column: "DiscordId");

            migrationBuilder.CreateIndex(
                name: "IX_warfare_user_pending_accout_links_Steam64",
                table: "warfare_user_pending_accout_links",
                column: "Steam64");

            migrationBuilder.CreateIndex(
                name: "IX_warfare_user_pending_accout_links_Token",
                table: "warfare_user_pending_accout_links",
                column: "Token",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_buildables_display_data_buildables_pk",
                table: "buildables_display_data",
                column: "pk",
                principalTable: "buildables",
                principalColumn: "pk",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_buildables_instance_ids_buildables_pk",
                table: "buildables_instance_ids",
                column: "pk",
                principalTable: "buildables",
                principalColumn: "pk",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_buildables_stored_items_buildables_Save",
                table: "buildables_stored_items",
                column: "Save",
                principalTable: "buildables",
                principalColumn: "pk",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_faction_assets_factions_Faction",
                table: "faction_assets",
                column: "Faction",
                principalTable: "factions",
                principalColumn: "pk",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_faction_translations_factions_Faction",
                table: "faction_translations",
                column: "Faction",
                principalTable: "factions",
                principalColumn: "pk",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_kits_factions_Faction",
                table: "kits",
                column: "Faction",
                principalTable: "factions",
                principalColumn: "pk",
                onDelete: ReferentialAction.SetNull);


            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableEntries,
                columns: table => new
                {
                    Id = table.Column<int>(name: DatabaseInterface.ColumnEntriesPrimaryKey, nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Type = table.Column<string>(name: DatabaseInterface.ColumnEntriesType, type: "enum('Warning','Kick','Ban','Mute','AssetBan','Teamkill','VehicleTeamkill','BattlEyeKick','Appeal','Report','GriefingReport','ChatAbuseReport','CheatingReport','Note','Commendation','BugReportAccepted','PlayerReportAccepted')", nullable: false),
                    Steam64 = table.Column<ulong>(name: DatabaseInterface.ColumnEntriesSteam64, nullable: false),
                    Message = table.Column<string>(name: DatabaseInterface.ColumnEntriesMessage, maxLength: 1024, nullable: true, defaultValueSql: "NULL"),
                    IsLegacy = table.Column<bool>(name: DatabaseInterface.ColumnEntriesIsLegacy, nullable: false, defaultValue: false),
                    LegacyId = table.Column<uint>(name: DatabaseInterface.ColumnEntriesLegacyId, nullable: true, defaultValueSql: "NULL"),
                    StartTimeUTC = table.Column<DateTime>(name: DatabaseInterface.ColumnEntriesStartTimestamp, type: "datetime", nullable: false),
                    ResolvedTimeUTC = table.Column<DateTime>(name: DatabaseInterface.ColumnEntriesResolvedTimestamp, type: "datetime", nullable: true, defaultValueSql: "NULL"),
                    PendingReputation = table.Column<double>(name: DatabaseInterface.ColumnEntriesPendingReputation, nullable: true, defaultValue: 0d),
                    Reputation = table.Column<double>(name: DatabaseInterface.ColumnEntriesReputation, nullable: false, defaultValue: 0d),
                    RelavantLogsStartTimeUTC = table.Column<DateTime>(name: DatabaseInterface.ColumnEntriesRelavantLogsStartTimestamp, type: "datetime", nullable: true, defaultValueSql: "NULL"),
                    RelavantLogsEndTimeUTC = table.Column<DateTime>(name: DatabaseInterface.ColumnEntriesRelavantLogsEndTimestamp, type: "datetime", nullable: true, defaultValueSql: "NULL"),
                    Removed = table.Column<bool>(name: DatabaseInterface.ColumnEntriesRemoved, nullable: false, defaultValue: false),
                    RemovedBy = table.Column<ulong>(name: DatabaseInterface.ColumnEntriesRemovedBy, nullable: true, defaultValueSql: "NULL"),
                    RemovedTimeUTC = table.Column<DateTime>(name: DatabaseInterface.ColumnEntriesRemovedTimestamp, type: "datetime", nullable: true, defaultValueSql: "NULL"),
                    RemovedReason = table.Column<string>(name: DatabaseInterface.ColumnEntriesRemovedReason, maxLength: 1024, nullable: true, defaultValueSql: "NULL"),
                    DiscordMessageId = table.Column<ulong>(name: DatabaseInterface.ColumnEntriesDiscordMessageId, nullable: false, defaultValue: 0ul)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moderation_entries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_entries_Steam64",
                table: DatabaseInterface.TableEntries,
                column: DatabaseInterface.ColumnEntriesSteam64
            );

            migrationBuilder.CreateIndex(
                name: "IX_moderation_entries_RemovedBy",
                table: DatabaseInterface.TableEntries,
                column: DatabaseInterface.ColumnEntriesRemovedBy
            );

            migrationBuilder.CreateIndex(
                name: "IX_moderation_entries_LegacyId",
                table: DatabaseInterface.TableEntries,
                column: DatabaseInterface.ColumnEntriesLegacyId
            );

            migrationBuilder.CreateIndex(
                name: "IX_moderation_entries_Type",
                table: DatabaseInterface.TableEntries,
                column: DatabaseInterface.ColumnEntriesType
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableActors,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false),
                    ActorRole = table.Column<string>(name: DatabaseInterface.ColumnActorsRole, nullable: false, maxLength: 255),
                    ActorId = table.Column<ulong>(name: DatabaseInterface.ColumnActorsId, nullable: false),
                    ActorAsAdmin = table.Column<bool>(name: DatabaseInterface.ColumnActorsAsAdmin, nullable: false),
                    ActorIndex = table.Column<int>(name: DatabaseInterface.ColumnActorsIndex, nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_moderation_actors_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_actors_Entry",
                table: DatabaseInterface.TableActors,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateIndex(
                name: "IX_moderation_actors_ActorId",
                table: DatabaseInterface.TableActors,
                column: DatabaseInterface.ColumnActorsId
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableEvidence,
                columns: table => new
                {
                    Id = table.Column<int>(name: DatabaseInterface.ColumnEntriesPrimaryKey, nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: true, defaultValueSql: "NULL"),
                    EvidenceURL = table.Column<string>(name: DatabaseInterface.ColumnEvidenceLink, nullable: false, maxLength: 512),
                    EvidenceLocalSource = table.Column<string>(name: DatabaseInterface.ColumnEvidenceLocalSource, nullable: true, maxLength: 512, defaultValueSql: "NULL"),
                    EvidenceIsImage = table.Column<bool>(name: DatabaseInterface.ColumnEvidenceIsImage, nullable: false),
                    EvidenceTimestampUTC = table.Column<bool>(name: DatabaseInterface.ColumnEvidenceTimestamp, type: "datetime", nullable: false),
                    EvidenceActor = table.Column<ulong>(name: DatabaseInterface.ColumnEvidenceActorId, nullable: true, defaultValueSql: "NULL"),
                    EvidenceMessage = table.Column<string>(name: DatabaseInterface.ColumnEvidenceMessage, maxLength: 1024, nullable: true, defaultValueSql: "NULL")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moderation_evidence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_moderation_evidence_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_evidence_Entry",
                table: DatabaseInterface.TableEvidence,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateIndex(
                name: "IX_moderation_evidence_EvidenceActor",
                table: DatabaseInterface.TableEvidence,
                column: DatabaseInterface.ColumnEvidenceActorId
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableRelatedEntries,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false),
                    RelatedEntry = table.Column<int>(name: DatabaseInterface.ColumnRelatedEntry, nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_moderation_related_entries_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);

                    table.ForeignKey(
                        name: "FK_moderation_related_entries_moderation_entries_RelatedEntry",
                        column: x => x.RelatedEntry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_related_entries_Entry",
                table: DatabaseInterface.TableRelatedEntries,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateIndex(
                name: "IX_moderation_related_entries_RelatedEntry",
                table: DatabaseInterface.TableRelatedEntries,
                column: DatabaseInterface.ColumnRelatedEntry
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableAssetBanTypeFilters,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false),
                    VehicleType = table.Column<string>(name: DatabaseInterface.ColumnAssetBanFiltersType, type: "enum('None','Humvee','TransportGround','ScoutCar','LogisticsGround','APC','IFV','MBT','TransportAir','AttackHeli','Jet','Emplacement','AA','HMG','ATGM','Mortar')", nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_moderation_asset_ban_filters_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_asset_ban_filters_Entry",
                table: DatabaseInterface.TableAssetBanTypeFilters,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TablePunishments,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PresetType = table.Column<string>(name: DatabaseInterface.ColumnPunishmentsPresetType, type: "enum('None','Griefing','Toxicity','Soloing','AssetWaste','IntentionalTeamkilling','TargetedHarassment','Discrimination','Cheating','DisruptiveBehavior','InappropriateProfile','BypassingPunishment')", nullable: true, defaultValueSql: "NULL"),
                    PresetLevel = table.Column<int>(name: DatabaseInterface.ColumnPunishmentsPresetLevel, nullable: true, defaultValueSql: "NULL")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moderation_punishments_Entry", x => x.Entry);
                    table.ForeignKey(
                        name: "FK_moderation_punishments_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_punishments_Entry",
                table: DatabaseInterface.TablePunishments,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateIndex(
                name: "IX_moderation_punishments_PresetType",
                table: DatabaseInterface.TablePunishments,
                column: DatabaseInterface.ColumnPunishmentsPresetType
            );

            migrationBuilder.CreateIndex(
                name: "IX_moderation_punishments_PresetLevel",
                table: DatabaseInterface.TablePunishments,
                column: DatabaseInterface.ColumnPunishmentsPresetLevel
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableDurationPunishments,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Duration = table.Column<long>(name: DatabaseInterface.ColumnDurationsDurationSeconds, nullable: false),
                    Forgiven = table.Column<bool>(name: DatabaseInterface.ColumnDurationsForgiven, nullable: false, defaultValue: false),
                    ForgivenBy = table.Column<ulong>(name: DatabaseInterface.ColumnDurationsForgivenBy, nullable: true, defaultValueSql: "NULL"),
                    ForgivenTimeUTC = table.Column<DateTime>(name: DatabaseInterface.ColumnDurationsForgivenTimestamp, type: "datetime", nullable: true, defaultValueSql: "NULL"),
                    ForgivenReason = table.Column<string>(name: DatabaseInterface.ColumnDurationsForgivenReason, maxLength: 1024, nullable: true, defaultValueSql: "NULL")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moderation_durations_Entry", x => x.Entry);
                    table.ForeignKey(
                        name: "FK_moderation_durations_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_durations_Entry",
                table: DatabaseInterface.TableDurationPunishments,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateIndex(
                name: "IX_moderation_durations_ForgivenBy",
                table: DatabaseInterface.TableDurationPunishments,
                column: DatabaseInterface.ColumnDurationsForgivenBy
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableLinkedAppeals,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false),
                    LinkedAppeal = table.Column<int>(name: DatabaseInterface.ColumnLinkedAppealsAppeal, nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_moderation_linked_appeals_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);

                    table.ForeignKey(
                        name: "FK_moderation_linked_appeals_moderation_entries_LinkedAppeal",
                        column: x => x.LinkedAppeal,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_linked_appeals_Entry",
                table: DatabaseInterface.TableLinkedAppeals,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateIndex(
                name: "IX_moderation_linked_appeals_LinkedAppeal",
                table: DatabaseInterface.TableLinkedAppeals,
                column: DatabaseInterface.ColumnLinkedAppealsAppeal
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableLinkedReports,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false),
                    LinkedReport = table.Column<int>(name: DatabaseInterface.ColumnLinkedReportsReport, nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_moderation_linked_reports_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);

                    table.ForeignKey(
                        name: "FK_moderation_linked_reports_moderation_entries_LinkedReport",
                        column: x => x.LinkedReport,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_linked_reports_Entry",
                table: DatabaseInterface.TableLinkedReports,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateIndex(
                name: "IX_moderation_linked_reports_LinkedReport",
                table: DatabaseInterface.TableLinkedReports,
                column: DatabaseInterface.ColumnLinkedReportsReport
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableMutes,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MuteType = table.Column<string>(name: DatabaseInterface.ColumnMutesType, type: "enum('Voice','Text','Both')", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moderation_mutes_Entry", x => x.Entry);
                    table.ForeignKey(
                        name: "FK_moderation_mutes_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_mutes_Entry",
                table: DatabaseInterface.TableMutes,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableWarnings,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Displayed = table.Column<DateTime>(name: DatabaseInterface.ColumnWarningsDisplayedTimestamp, type: "datetime", nullable: true, defaultValueSql: "NULL")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moderation_warnings_Entry", x => x.Entry);
                    table.ForeignKey(
                        name: "FK_moderation_warnings_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_warnings_Entry",
                table: DatabaseInterface.TableWarnings,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TablePlayerReportAccepteds,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AcceptedReport = table.Column<int>(name: DatabaseInterface.ColumnPlayerReportAcceptedsReport, nullable: true, defaultValueSql: "NULL")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moderation_accepted_player_reports_Entry", x => x.Entry);
                    table.ForeignKey(
                        name: "FK_moderation_accepted_player_reports_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);

                    table.ForeignKey(
                        name: "FK_moderation_accepted_player_reports_moderation_entries_AcceptedReport",
                        column: x => x.AcceptedReport,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_accepted_player_reports_Entry",
                table: DatabaseInterface.TablePlayerReportAccepteds,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateIndex(
                name: "IX_moderation_accepted_player_reports_AcceptedReport",
                table: DatabaseInterface.TablePlayerReportAccepteds,
                column: DatabaseInterface.ColumnPlayerReportAcceptedsReport
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableBugReportAccepteds,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AcceptedCommit = table.Column<string>(name: DatabaseInterface.ColumnTableBugReportAcceptedsCommit, nullable: true, defaultValueSql: "NULL"),
                    AcceptedIssue = table.Column<int>(name: DatabaseInterface.ColumnTableBugReportAcceptedsIssue, nullable: true, defaultValueSql: "NULL")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moderation_accepted_bug_reports_Entry", x => x.Entry);
                    table.ForeignKey(
                        name: "FK_moderation_accepted_bug_reports_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_accepted_bug_reports_Entry",
                table: DatabaseInterface.TableBugReportAccepteds,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableTeamkills,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Asset = table.Column<string>(name: DatabaseInterface.ColumnTeamkillsAsset, type: "char(32)", nullable: true, defaultValueSql: "NULL"),
                    AssetName = table.Column<string>(name: DatabaseInterface.ColumnTeamkillsAssetName, maxLength: 48, nullable: true, defaultValueSql: "NULL"),
                    DeathCause = table.Column<string>(name: DatabaseInterface.ColumnTeamkillsDeathCause, type: "enum('BLEEDING','BONES','FREEZING','BURNING','FOOD','WATER','GUN','MELEE','ZOMBIE','ANIMAL','SUICIDE','KILL','INFECTION','PUNCH','BREATH','ROADKILL','VEHICLE','GRENADE','SHRED','LANDMINE','ARENA','MISSILE','CHARGE','SPLASH','SENTRY','ACID','BOULDER','BURNER','SPIT','SPARK')", nullable: true, defaultValueSql: "NULL"),
                    Distance = table.Column<float>(name: DatabaseInterface.ColumnTeamkillsDistance, nullable: true, defaultValueSql: "NULL"),
                    Limb = table.Column<string>(name: DatabaseInterface.ColumnTeamkillsLimb, type: "enum('LEFT_FOOT','LEFT_LEG','RIGHT_FOOT','RIGHT_LEG','LEFT_HAND','LEFT_ARM','RIGHT_HAND','RIGHT_ARM','LEFT_BACK','RIGHT_BACK','LEFT_FRONT','RIGHT_FRONT','SPINE','SKULL')", nullable: true, defaultValueSql: "NULL"),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moderation_teamkills_Entry", x => x.Entry);
                    table.ForeignKey(
                        name: "FK_moderation_teamkills_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_teamkills_Entry",
                table: DatabaseInterface.TableTeamkills,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableVehicleTeamkills,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    VehicleAsset = table.Column<string>(name: DatabaseInterface.ColumnVehicleTeamkillsVehicleAsset, type: "char(32)", nullable: true, defaultValueSql: "NULL"),
                    VehicleAssetName = table.Column<string>(name: DatabaseInterface.ColumnVehicleTeamkillsVehicleAssetName, maxLength: 48, nullable: true, defaultValueSql: "NULL"),
                    DamageOrigin = table.Column<string>(name: DatabaseInterface.ColumnVehicleTeamkillsDamageOrigin, type: "enum('Unknown','Mega_Zombie_Boulder','Vehicle_Bumper','Horde_Beacon_Self_Destruct','Trap_Wear_And_Tear','Carepackage_Timeout','Plant_Harvested','Charge_Self_Destruct','Zombie_Swipe','Grenade_Explosion','Rocket_Explosion','Food_Explosion','Vehicle_Explosion','Charge_Explosion','Trap_Explosion','Bullet_Explosion','Radioactive_Zombie_Explosion','Flamable_Zombie_Explosion','Zombie_Electric_Shock','Zombie_Stomp','Zombie_Fire_Breath','Sentry','Useable_Gun','Useable_Melee','Punch','Animal_Attack','Kill_Volume','Vehicle_Collision_Self_Damage','Lightning','VehicleDecay')", nullable: true, defaultValueSql: "NULL")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moderation_vehicle_teamkills_Entry", x => x.Entry);
                    table.ForeignKey(
                        name: "FK_moderation_vehicle_teamkills_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_vehicle_teamkills_Entry",
                table: DatabaseInterface.TableVehicleTeamkills,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableAppeals,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TicketId = table.Column<string>(name: DatabaseInterface.ColumnAppealsTicketId, type: "char(32)", nullable: false),
                    State = table.Column<bool>(name: DatabaseInterface.ColumnAppealsState, nullable: true, defaultValueSql: "NULL"),
                    DiscordId = table.Column<ulong>(name: DatabaseInterface.ColumnAppealsDiscordId, nullable: true, defaultValueSql: "NULL")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moderation_appeals_Entry", x => x.Entry);
                    table.ForeignKey(
                        name: "FK_moderation_appeals_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_appeals_Entry",
                table: DatabaseInterface.TableAppeals,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableAppealPunishments,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false),
                    Punishment = table.Column<int>(name: DatabaseInterface.ColumnAppealPunishmentsPunishment, nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_moderation_appeal_punishments_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);

                    table.ForeignKey(
                        name: "FK_moderation_appeal_punishments_moderation_entries_Punishment",
                        column: x => x.Punishment,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_appeal_punishments_Entry",
                table: DatabaseInterface.TableAppealPunishments,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateIndex(
                name: "IX_moderation_appeal_punishments_Punishment",
                table: DatabaseInterface.TableAppealPunishments,
                column: DatabaseInterface.ColumnAppealPunishmentsPunishment
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableAppealResponses,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false),
                    Question = table.Column<string>(name: DatabaseInterface.ColumnAppealResponsesQuestion, maxLength: 255, nullable: false),
                    Response = table.Column<string>(name: DatabaseInterface.ColumnAppealResponsesResponse, maxLength: 1024, nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_moderation_appeal_responses_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_appeal_responses_Entry",
                table: DatabaseInterface.TableAppealResponses,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableReports,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Type = table.Column<string>(name: DatabaseInterface.ColumnReportsType, type: "enum('Custom','Greifing','ChatAbuse','VoiceChatAbuse','Cheating')", nullable: false),
                    ScreenshotData = table.Column<byte[]>(name: DatabaseInterface.ColumnReportsScreenshotData, type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moderation_reports_Entry", x => x.Entry);
                    table.ForeignKey(
                        name: "FK_moderation_reports_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_reports_Entry",
                table: DatabaseInterface.TableReports,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableReportChatRecords,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false),
                    Message = table.Column<string>(name: DatabaseInterface.ColumnReportsChatRecordsMessage, maxLength: 512, nullable: false),
                    TimeUTC = table.Column<DateTime>(name: DatabaseInterface.ColumnReportsChatRecordsTimestamp, type: "datetime", nullable: false),
                    Index = table.Column<int>(name: DatabaseInterface.ColumnReportsChatRecordsIndex, nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_moderation_report_chat_records_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_report_chat_records_Entry",
                table: DatabaseInterface.TableReportChatRecords,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableReportStructureDamageRecords,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false),
                    Structure = table.Column<string>(name: DatabaseInterface.ColumnReportsStructureDamageStructure, type: "char(32)", nullable: false),
                    StructureName = table.Column<string>(name: DatabaseInterface.ColumnReportsStructureDamageStructureName, maxLength: 48, nullable: false),
                    StructureOwner = table.Column<ulong>(name: DatabaseInterface.ColumnReportsStructureDamageStructureOwner, nullable: false),
                    StructureType = table.Column<string>(name: DatabaseInterface.ColumnReportsStructureDamageStructureType, type: "enum('Structure','Barricade')", nullable: false),
                    Damage = table.Column<int>(name: DatabaseInterface.ColumnReportsStructureDamageDamage, nullable: false),
                    DamageOrigin = table.Column<string>(name: DatabaseInterface.ColumnReportsStructureDamageDamageOrigin, type: "enum('Unknown','Mega_Zombie_Boulder','Vehicle_Bumper','Horde_Beacon_Self_Destruct','Trap_Wear_And_Tear','Carepackage_Timeout','Plant_Harvested','Charge_Self_Destruct','Zombie_Swipe','Grenade_Explosion','Rocket_Explosion','Food_Explosion','Vehicle_Explosion','Charge_Explosion','Trap_Explosion','Bullet_Explosion','Radioactive_Zombie_Explosion','Flamable_Zombie_Explosion','Zombie_Electric_Shock','Zombie_Stomp','Zombie_Fire_Breath','Sentry','Useable_Gun','Useable_Melee','Punch','Animal_Attack','Kill_Volume','Vehicle_Collision_Self_Damage','Lightning','VehicleDecay')", nullable: false),
                    InstanceId = table.Column<uint>(name: DatabaseInterface.ColumnReportsStructureDamageInstanceId, nullable: false),
                    WasDestroyed = table.Column<bool>(name: DatabaseInterface.ColumnReportsStructureDamageWasDestroyed, nullable: false, defaultValue: false),
                    Timestamp = table.Column<DateTime>(name: DatabaseInterface.ColumnReportsStructureDamageTimestamp, type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_moderation_report_struct_dmg_records_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_report_struct_dmg_records_Entry",
                table: DatabaseInterface.TableReportStructureDamageRecords,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableReportTeamkillRecords,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false),
                    Teamkill = table.Column<int>(name: DatabaseInterface.ColumnReportsTeamkillRecordTeamkill, nullable: true, defaultValueSql: "NULL"),
                    Victim = table.Column<ulong>(name: DatabaseInterface.ColumnReportsTeamkillRecordVictim, nullable: false),
                    DeathCause = table.Column<string>(name: DatabaseInterface.ColumnReportsTeamkillRecordDeathCause, type: "enum('BLEEDING','BONES','FREEZING','BURNING','FOOD','WATER','GUN','MELEE','ZOMBIE','ANIMAL','SUICIDE','KILL','INFECTION','PUNCH','BREATH','ROADKILL','VEHICLE','GRENADE','SHRED','LANDMINE','ARENA','MISSILE','CHARGE','SPLASH','SENTRY','ACID','BOULDER','BURNER','SPIT','SPARK')", nullable: false),
                    WasIntentional = table.Column<bool>(name: DatabaseInterface.ColumnReportsTeamkillRecordWasIntentional, nullable: true, defaultValueSql: "NULL"),
                    Message = table.Column<string>(name: DatabaseInterface.ColumnReportsTeamkillRecordMessage, maxLength: 255, nullable: true, defaultValueSql: "NULL"),
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_moderation_report_tk_records_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);

                    table.ForeignKey(
                        name: "FK_moderation_report_tk_records_moderation_entries_Teamkill",
                        column: x => x.Teamkill,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_report_tk_records_Entry",
                table: DatabaseInterface.TableReportTeamkillRecords,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateIndex(
                name: "IX_moderation_report_tk_records_Teamkill",
                table: DatabaseInterface.TableReportTeamkillRecords,
                column: DatabaseInterface.ColumnReportsTeamkillRecordTeamkill
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableReportVehicleTeamkillRecords,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false),
                    Teamkill = table.Column<int>(name: DatabaseInterface.ColumnReportsVehicleTeamkillRecordTeamkill, nullable: true, defaultValueSql: "NULL"),
                    Victim = table.Column<ulong>(name: DatabaseInterface.ColumnReportsVehicleTeamkillRecordVictim, nullable: false),
                    DamageOrigin = table.Column<string>(name: DatabaseInterface.ColumnReportsVehicleTeamkillRecordDamageOrigin, type: "enum('Unknown','Mega_Zombie_Boulder','Vehicle_Bumper','Horde_Beacon_Self_Destruct','Trap_Wear_And_Tear','Carepackage_Timeout','Plant_Harvested','Charge_Self_Destruct','Zombie_Swipe','Grenade_Explosion','Rocket_Explosion','Food_Explosion','Vehicle_Explosion','Charge_Explosion','Trap_Explosion','Bullet_Explosion','Radioactive_Zombie_Explosion','Flamable_Zombie_Explosion','Zombie_Electric_Shock','Zombie_Stomp','Zombie_Fire_Breath','Sentry','Useable_Gun','Useable_Melee','Punch','Animal_Attack','Kill_Volume','Vehicle_Collision_Self_Damage','Lightning','VehicleDecay')", nullable: false),
                    Message = table.Column<string>(name: DatabaseInterface.ColumnReportsVehicleTeamkillRecordMessage, maxLength: 255, nullable: true, defaultValueSql: "NULL"),
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_moderation_report_veh_tk_records_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);

                    table.ForeignKey(
                        name: "FK_moderation_report_veh_tk_records_moderation_entries_Teamkill",
                        column: x => x.Teamkill,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_report_veh_tk_records_Entry",
                table: DatabaseInterface.TableReportVehicleTeamkillRecords,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateIndex(
                name: "IX_moderation_report_veh_tk_records_Teamkill",
                table: DatabaseInterface.TableReportVehicleTeamkillRecords,
                column: DatabaseInterface.ColumnReportsVehicleTeamkillRecordTeamkill
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableReportVehicleRequestRecords,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false),
                    Vehicle = table.Column<string>(name: DatabaseInterface.ColumnReportsVehicleRequestRecordVehicle, type: "char(32)", nullable: false),
                    VehicleName = table.Column<string>(name: DatabaseInterface.ColumnReportsVehicleRequestRecordVehicleName, maxLength: 48, nullable: false),
                    DamageOrigin = table.Column<string>(name: DatabaseInterface.ColumnReportsVehicleRequestRecordDamageOrigin, type: "enum('Unknown','Mega_Zombie_Boulder','Vehicle_Bumper','Horde_Beacon_Self_Destruct','Trap_Wear_And_Tear','Carepackage_Timeout','Plant_Harvested','Charge_Self_Destruct','Zombie_Swipe','Grenade_Explosion','Rocket_Explosion','Food_Explosion','Vehicle_Explosion','Charge_Explosion','Trap_Explosion','Bullet_Explosion','Radioactive_Zombie_Explosion','Flamable_Zombie_Explosion','Zombie_Electric_Shock','Zombie_Stomp','Zombie_Fire_Breath','Sentry','Useable_Gun','Useable_Melee','Punch','Animal_Attack','Kill_Volume','Vehicle_Collision_Self_Damage','Lightning','VehicleDecay')", nullable: false),
                    DamageInstigator = table.Column<ulong>(name: DatabaseInterface.ColumnReportsVehicleRequestRecordInstigator, nullable: false),
                    RequestTimeUTC = table.Column<DateTime>(name: DatabaseInterface.ColumnReportsVehicleRequestRecordRequestTimestamp, type: "datetime", nullable: false),
                    DestroyTimeUTC = table.Column<DateTime>(name: DatabaseInterface.ColumnReportsVehicleRequestRecordDestroyTimestamp, type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_moderation_report_veh_req_records_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_report_veh_req_records_Entry",
                table: DatabaseInterface.TableReportVehicleRequestRecords,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableReportShotRecords,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false),
                    Ammo = table.Column<string>(name: DatabaseInterface.ColumnReportsShotRecordAmmo, type: "char(32)", nullable: false),
                    AmmoName = table.Column<string>(name: DatabaseInterface.ColumnReportsShotRecordAmmoName, maxLength: 48, nullable: false),
                    Item = table.Column<string>(name: DatabaseInterface.ColumnReportsShotRecordItem, type: "char(32)", nullable: false),
                    ItemName = table.Column<string>(name: DatabaseInterface.ColumnReportsShotRecordItemName, maxLength: 48, nullable: false),
                    DamageDone = table.Column<int>(name: DatabaseInterface.ColumnReportsShotRecordDamageDone, nullable: false),
                    Limb = table.Column<string>(name: DatabaseInterface.ColumnReportsShotRecordLimb, type: "enum('LEFT_FOOT','LEFT_LEG','RIGHT_FOOT','RIGHT_LEG','LEFT_HAND','LEFT_ARM','RIGHT_HAND','RIGHT_ARM','LEFT_BACK','RIGHT_BACK','LEFT_FRONT','RIGHT_FRONT','SPINE','SKULL')", nullable: true, defaultValueSql: "NULL"),
                    IsProjectile = table.Column<bool>(name: DatabaseInterface.ColumnReportsShotRecordIsProjectile, nullable: false),
                    Distance = table.Column<double>(name: DatabaseInterface.ColumnReportsShotRecordDistance, nullable: true, defaultValueSql: "NULL"),
                    HitPointX = table.Column<float>(name: DatabaseInterface.ColumnReportsShotRecordHitPointX, nullable: true, defaultValueSql: "NULL"),
                    HitPointY = table.Column<float>(name: DatabaseInterface.ColumnReportsShotRecordHitPointY, nullable: true, defaultValueSql: "NULL"),
                    HitPointZ = table.Column<float>(name: DatabaseInterface.ColumnReportsShotRecordHitPointZ, nullable: true, defaultValueSql: "NULL"),
                    ShootFromPointX = table.Column<float>(name: DatabaseInterface.ColumnReportsShotRecordShootFromPointX, nullable: false),
                    ShootFromPointY = table.Column<float>(name: DatabaseInterface.ColumnReportsShotRecordShootFromPointY, nullable: false),
                    ShootFromPointZ = table.Column<float>(name: DatabaseInterface.ColumnReportsShotRecordShootFromPointZ, nullable: false),
                    ShootFromRotationX = table.Column<float>(name: DatabaseInterface.ColumnReportsShotRecordShootFromRotationX, nullable: false),
                    ShootFromRotationY = table.Column<float>(name: DatabaseInterface.ColumnReportsShotRecordShootFromRotationY, nullable: false),
                    ShootFromRotationZ = table.Column<float>(name: DatabaseInterface.ColumnReportsShotRecordShootFromRotationZ, nullable: false),
                    HitType = table.Column<string>(name: DatabaseInterface.ColumnReportsShotRecordHitType, type: "enum('NONE','SKIP','OBJECT','PLAYER','ZOMBIE','ANIMAL','VEHICLE','BARRICADE','STRUCTURE','RESOURCE')", nullable: true, defaultValueSql: "NULL"),
                    HitActor = table.Column<ulong>(name: DatabaseInterface.ColumnReportsShotRecordHitActor, nullable: true, defaultValueSql: "NULL"),
                    HitAsset = table.Column<string>(name: DatabaseInterface.ColumnReportsShotRecordHitAsset, type: "char(32)", nullable: true, defaultValueSql: "NULL"),
                    HitAssetName = table.Column<string>(name: DatabaseInterface.ColumnReportsShotRecordHitAssetName, maxLength: 48, nullable: true, defaultValueSql: "NULL"),
                    Timestamp = table.Column<DateTime>(name: DatabaseInterface.ColumnReportsShotRecordTimestamp, type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_moderation_report_shot_record_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_report_shot_record_Entry",
                table: DatabaseInterface.TableReportShotRecords,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableIPWhitelists,
                columns: table => new
                {
                    Steam64 = table.Column<ulong>(name: DatabaseInterface.ColumnIPWhitelistsSteam64, nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Admin = table.Column<ulong>(name: DatabaseInterface.ColumnIPWhitelistsAdmin, nullable: false),
                    IPRange = table.Column<string>(name: DatabaseInterface.ColumnIPWhitelistsIPRange, maxLength: 18, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ip_whitelists_Steam64", x => x.Steam64);
                });

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableVoiceChatReports,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    VoiceData = table.Column<byte[]>(name: DatabaseInterface.ColumnVoiceChatReportsData, type: "blob", nullable: true, defaultValueSql: "NULL")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moderation_voice_chat_reports_Entry", x => x.Entry);
                    table.ForeignKey(
                        name: "FK_moderation_voice_chat_reports_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_voice_chat_reports_Entry",
                table: DatabaseInterface.TableVoiceChatReports,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_kits_factions_Faction",
                table: "kits");

            migrationBuilder.DropTable(
                name: "buildables_display_data");

            migrationBuilder.DropTable(
                name: "buildables_instance_ids");

            migrationBuilder.DropTable(
                name: "buildables_stored_items");

            migrationBuilder.DropTable(
                name: "faction_assets");

            migrationBuilder.DropTable(
                name: "faction_translations");

            migrationBuilder.DropTable(
                name: "homebase_auth_keys");

            migrationBuilder.DropTable(
                name: "hwids");

            migrationBuilder.DropTable(
                name: "ip_addresses");

            migrationBuilder.DropTable(
                name: "item_whitelists");

            migrationBuilder.DropTable(
                name: "kits_access");

            migrationBuilder.DropTable(
                name: "kits_bundle_items");

            migrationBuilder.DropTable(
                name: "kits_delays");

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
                name: "loadout_purchases");

            migrationBuilder.DropTable(
                name: "maps_dependencies");

            migrationBuilder.DropTable(
                name: "moderation_global_ban_whitelist");

            migrationBuilder.DropTable(
                name: "stats_aid_records");

            migrationBuilder.DropTable(
                name: "stats_deaths");

            migrationBuilder.DropTable(
                name: "stats_fob_items_builders");

            migrationBuilder.DropTable(
                name: "user_permissions");

            migrationBuilder.DropTable(
                name: "warfare_user_pending_accout_links");

            migrationBuilder.DropTable(
                name: "buildables");

            migrationBuilder.DropTable(
                name: "kits_bundles");

            migrationBuilder.DropTable(
                name: "lang_info");

            migrationBuilder.DropTable(
                name: "stats_damage");

            migrationBuilder.DropTable(
                name: "stats_fob_items");

            migrationBuilder.DropTable(
                name: "stats_fobs");

            migrationBuilder.DropTable(
                name: "stats_sessions");

            migrationBuilder.DropTable(
                name: "maps");

            migrationBuilder.DropTable(
                name: "stats_games");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "seasons");

            migrationBuilder.DropTable(
                name: "factions");

            migrationBuilder.DropTable(
                name: "kits");

            migrationBuilder.DropTable(
                name: DatabaseInterface.TableVoiceChatReports);

            migrationBuilder.DropTable(
                name: DatabaseInterface.TableIPWhitelists);

            migrationBuilder.DropTable(
                name: DatabaseInterface.TableReportShotRecords);

            migrationBuilder.DropTable(
                name: DatabaseInterface.TableReportVehicleRequestRecords);

            migrationBuilder.DropTable(
                name: DatabaseInterface.TableReportVehicleTeamkillRecords);

            migrationBuilder.DropTable(
                name: DatabaseInterface.TableReportTeamkillRecords);

            migrationBuilder.DropTable(
                name: DatabaseInterface.TableReportStructureDamageRecords);

            migrationBuilder.DropTable(
                name: DatabaseInterface.TableReportChatRecords);

            migrationBuilder.DropTable(
                name: DatabaseInterface.TableReports);

            migrationBuilder.DropTable(
                name: DatabaseInterface.TableAppealResponses);

            migrationBuilder.DropTable(
                name: DatabaseInterface.TableAppealPunishments);

            migrationBuilder.DropTable(
                name: DatabaseInterface.TableAppeals);

            migrationBuilder.DropTable(
                name: DatabaseInterface.TableVehicleTeamkills);

            migrationBuilder.DropTable(
                name: DatabaseInterface.TableTeamkills);

            migrationBuilder.DropTable(
                name: DatabaseInterface.TableBugReportAccepteds);

            migrationBuilder.DropTable(
                name: DatabaseInterface.TablePlayerReportAccepteds);

            migrationBuilder.DropTable(
                name: DatabaseInterface.TableWarnings);

            migrationBuilder.DropTable(
                name: DatabaseInterface.TableMutes);

            migrationBuilder.DropTable(
                name: DatabaseInterface.TableLinkedReports);

            migrationBuilder.DropTable(
                name: DatabaseInterface.TableLinkedAppeals);

            migrationBuilder.DropTable(
                name: DatabaseInterface.TableDurationPunishments);

            migrationBuilder.DropTable(
                name: DatabaseInterface.TablePunishments);

            migrationBuilder.DropTable(
                name: DatabaseInterface.TableAssetBanTypeFilters);

            migrationBuilder.DropTable(
                name: DatabaseInterface.TableEvidence);

            migrationBuilder.DropTable(
                name: DatabaseInterface.TableActors);

            migrationBuilder.DropTable(
                name: DatabaseInterface.TableRelatedEntries);

            migrationBuilder.DropTable(
                name: DatabaseInterface.TableEntries);
        }
    }
}

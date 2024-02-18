using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Uncreated.Warfare.Migrations
{
    public partial class FobStats : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_kits_factions_Faction",
                table: "kits");

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

            migrationBuilder.DropColumn(
                name: "TotalSeconds",
                table: "users");

            migrationBuilder.AddColumn<ulong>(
                name: "SquadLeader",
                table: "stats_sessions",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SquadName",
                table: "stats_sessions",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AlterColumn<ulong>(
                name: "Steam64",
                table: "stats_deaths",
                nullable: true,
                oldClrType: typeof(ulong),
                oldType: "bigint unsigned");

            migrationBuilder.AlterColumn<ulong>(
                name: "Session",
                table: "stats_deaths",
                nullable: true,
                oldClrType: typeof(ulong),
                oldType: "bigint unsigned");

            migrationBuilder.AddColumn<string>(
                name: "NearestLocation",
                table: "stats_deaths",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<ulong>(
                name: "Steam64",
                table: "stats_damage",
                nullable: true,
                oldClrType: typeof(ulong),
                oldType: "bigint unsigned");

            migrationBuilder.AlterColumn<ulong>(
                name: "Session",
                table: "stats_damage",
                nullable: true,
                oldClrType: typeof(ulong),
                oldType: "bigint unsigned");

            migrationBuilder.AddColumn<string>(
                name: "NearestLocation",
                table: "stats_damage",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<ulong>(
                name: "Steam64",
                table: "stats_aid_records",
                nullable: true,
                oldClrType: typeof(ulong),
                oldType: "bigint unsigned");

            migrationBuilder.AlterColumn<ulong>(
                name: "Session",
                table: "stats_aid_records",
                nullable: true,
                oldClrType: typeof(ulong),
                oldType: "bigint unsigned");

            migrationBuilder.AddColumn<string>(
                name: "NearestLocation",
                table: "stats_aid_records",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<uint>(
                name: "Team1Faction",
                table: "maps",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "Team2Faction",
                table: "maps",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateTable(
                name: "stats_fobs",
                columns: table => new
                {
                    Id = table.Column<ulong>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Team = table.Column<byte>(nullable: false),
                    Steam64 = table.Column<ulong>(nullable: true),
                    Session = table.Column<ulong>(nullable: true),
                    PositionX = table.Column<float>(nullable: false),
                    PositionY = table.Column<float>(nullable: false),
                    PositionZ = table.Column<float>(nullable: false),
                    NearestLocation = table.Column<string>(maxLength: 255, nullable: false),
                    TimestampUTC = table.Column<DateTime>(type: "datetime", nullable: false),
                    Instigator = table.Column<ulong>(nullable: true),
                    InstigatorSession = table.Column<ulong>(nullable: true),
                    InstigatorPositionX = table.Column<float>(nullable: true),
                    InstigatorPositionY = table.Column<float>(nullable: true),
                    InstigatorPositionZ = table.Column<float>(nullable: true),
                    FobNumber = table.Column<int>(nullable: false),
                    FobType = table.Column<string>(type: "enum('Other','RadioFob','SpecialFob','Cache')", nullable: false),
                    PrimaryAsset = table.Column<string>(type: "char(32)", nullable: true),
                    SecondaryAsset = table.Column<string>(type: "char(32)", nullable: true),
                    FobName = table.Column<string>(maxLength: 32, nullable: false),
                    DeploymentCount = table.Column<int>(nullable: false),
                    TeleportCount = table.Column<int>(nullable: false),
                    FortificationsBuilt = table.Column<int>(nullable: false),
                    FortificationsDestroyed = table.Column<int>(nullable: false),
                    EmplacementsBuilt = table.Column<int>(nullable: false),
                    EmplacementsDestroyed = table.Column<int>(nullable: false),
                    BunkersBuilt = table.Column<int>(nullable: false),
                    BunkersDestroyed = table.Column<int>(nullable: false),
                    AmmoCratesBuilt = table.Column<int>(nullable: false),
                    AmmoCratesDestroyed = table.Column<int>(nullable: false),
                    RepairStationsBuilt = table.Column<int>(nullable: false),
                    RepairStationsDestroyed = table.Column<int>(nullable: false),
                    EmplacementPlayerKills = table.Column<int>(nullable: false),
                    EmplacementVehicleKills = table.Column<int>(nullable: false),
                    AmmoSpent = table.Column<int>(nullable: false),
                    BuildSpent = table.Column<int>(nullable: false),
                    AmmoLoaded = table.Column<int>(nullable: false),
                    BuildLoaded = table.Column<int>(nullable: false),
                    DestroyedByRoundEnd = table.Column<bool>(nullable: false),
                    Teamkilled = table.Column<bool>(nullable: false),
                    DestroyedAtUTC = table.Column<DateTime>(type: "datetime", nullable: true),
                    FobPositionX = table.Column<float>(nullable: false),
                    FobPositionY = table.Column<float>(nullable: false),
                    FobPositionZ = table.Column<float>(nullable: false),
                    FobAngleX = table.Column<float>(nullable: false),
                    FobAngleY = table.Column<float>(nullable: false),
                    FobAngleZ = table.Column<float>(nullable: false),
                    PrimaryAssetName = table.Column<string>(maxLength: 48, nullable: true),
                    SecondaryAssetName = table.Column<string>(maxLength: 48, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stats_fobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stats_fobs_users_Instigator",
                        column: x => x.Instigator,
                        principalTable: "users",
                        principalColumn: "Steam64");
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
                        name: "FK_stats_fobs_users_Steam64",
                        column: x => x.Steam64,
                        principalTable: "users",
                        principalColumn: "Steam64");
                });

            migrationBuilder.CreateTable(
                name: "stats_fob_items",
                columns: table => new
                {
                    Id = table.Column<ulong>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Team = table.Column<byte>(nullable: false),
                    Steam64 = table.Column<ulong>(nullable: true),
                    Session = table.Column<ulong>(nullable: true),
                    PositionX = table.Column<float>(nullable: false),
                    PositionY = table.Column<float>(nullable: false),
                    PositionZ = table.Column<float>(nullable: false),
                    NearestLocation = table.Column<string>(maxLength: 255, nullable: false),
                    TimestampUTC = table.Column<DateTime>(type: "datetime", nullable: false),
                    Instigator = table.Column<ulong>(nullable: true),
                    InstigatorSession = table.Column<ulong>(nullable: true),
                    InstigatorPositionX = table.Column<float>(nullable: true),
                    InstigatorPositionY = table.Column<float>(nullable: true),
                    InstigatorPositionZ = table.Column<float>(nullable: true),
                    Fob = table.Column<ulong>(nullable: true),
                    Type = table.Column<string>(type: "enum('Bunker','AmmoCrate','RepairStation','Fortification','Emplacement')", nullable: false),
                    PlayerKills = table.Column<int>(nullable: false),
                    VehicleKills = table.Column<int>(nullable: false),
                    UseTimeSeconds = table.Column<double>(nullable: false),
                    BuiltAtUTC = table.Column<DateTime>(type: "datetime", nullable: true),
                    DestroyedAtUTC = table.Column<DateTime>(type: "datetime", nullable: true),
                    Item = table.Column<string>(type: "char(32)", nullable: true),
                    PrimaryAsset = table.Column<string>(type: "char(32)", nullable: true),
                    SecondaryAsset = table.Column<string>(type: "char(32)", nullable: true),
                    DestroyedByRoundEnd = table.Column<bool>(nullable: false),
                    Teamkilled = table.Column<bool>(nullable: false),
                    FobItemPositionX = table.Column<float>(nullable: false),
                    FobItemPositionY = table.Column<float>(nullable: false),
                    FobItemPositionZ = table.Column<float>(nullable: false),
                    FobItemAngleX = table.Column<float>(nullable: false),
                    FobItemAngleY = table.Column<float>(nullable: false),
                    FobItemAngleZ = table.Column<float>(nullable: false),
                    ItemName = table.Column<string>(maxLength: 48, nullable: true),
                    PrimaryAssetName = table.Column<string>(maxLength: 48, nullable: true),
                    SecondaryAssetName = table.Column<string>(maxLength: 48, nullable: true)
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
                        name: "FK_stats_fob_items_users_Instigator",
                        column: x => x.Instigator,
                        principalTable: "users",
                        principalColumn: "Steam64");
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
                        name: "FK_stats_fob_items_users_Steam64",
                        column: x => x.Steam64,
                        principalTable: "users",
                        principalColumn: "Steam64");
                });

            migrationBuilder.CreateTable(
                name: "stats_fob_items_builders",
                columns: table => new
                {
                    Id = table.Column<ulong>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Team = table.Column<byte>(nullable: false),
                    Steam64 = table.Column<ulong>(nullable: true),
                    Session = table.Column<ulong>(nullable: true),
                    PositionX = table.Column<float>(nullable: false),
                    PositionY = table.Column<float>(nullable: false),
                    PositionZ = table.Column<float>(nullable: false),
                    NearestLocation = table.Column<string>(maxLength: 255, nullable: false),
                    TimestampUTC = table.Column<DateTime>(type: "datetime", nullable: false),
                    FobItem = table.Column<ulong>(nullable: false),
                    Hits = table.Column<float>(nullable: false),
                    Responsibility = table.Column<double>(nullable: false)
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
                name: "IX_stats_sessions_SquadLeader",
                table: "stats_sessions",
                column: "SquadLeader");

            migrationBuilder.CreateIndex(
                name: "IX_maps_Team1Faction",
                table: "maps",
                column: "Team1Faction");

            migrationBuilder.CreateIndex(
                name: "IX_maps_Team2Faction",
                table: "maps",
                column: "Team2Faction");

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

            migrationBuilder.AddForeignKey(
                name: "FK_kits_factions_Faction",
                table: "kits",
                column: "Faction",
                principalTable: "factions",
                principalColumn: "pk",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_kits_access_users_Steam64",
                table: "kits_access",
                column: "Steam64",
                principalTable: "users",
                principalColumn: "Steam64",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_kits_favorites_users_Steam64",
                table: "kits_favorites",
                column: "Steam64",
                principalTable: "users",
                principalColumn: "Steam64",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_kits_hotkeys_users_Steam64",
                table: "kits_hotkeys",
                column: "Steam64",
                principalTable: "users",
                principalColumn: "Steam64",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_kits_layouts_users_Steam64",
                table: "kits_layouts",
                column: "Steam64",
                principalTable: "users",
                principalColumn: "Steam64",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_maps_factions_Team1Faction",
                table: "maps",
                column: "Team1Faction",
                principalTable: "factions",
                principalColumn: "pk",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_maps_factions_Team2Faction",
                table: "maps",
                column: "Team2Faction",
                principalTable: "factions",
                principalColumn: "pk",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_stats_sessions_users_SquadLeader",
                table: "stats_sessions",
                column: "SquadLeader",
                principalTable: "users",
                principalColumn: "Steam64",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_kits_factions_Faction",
                table: "kits");

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
                name: "FK_maps_factions_Team1Faction",
                table: "maps");

            migrationBuilder.DropForeignKey(
                name: "FK_maps_factions_Team2Faction",
                table: "maps");

            migrationBuilder.DropForeignKey(
                name: "FK_stats_sessions_users_SquadLeader",
                table: "stats_sessions");

            migrationBuilder.DropTable(
                name: "stats_fob_items_builders");

            migrationBuilder.DropTable(
                name: "stats_fob_items");

            migrationBuilder.DropTable(
                name: "stats_fobs");

            migrationBuilder.DropIndex(
                name: "IX_stats_sessions_SquadLeader",
                table: "stats_sessions");

            migrationBuilder.DropIndex(
                name: "IX_maps_Team1Faction",
                table: "maps");

            migrationBuilder.DropIndex(
                name: "IX_maps_Team2Faction",
                table: "maps");

            migrationBuilder.DropColumn(
                name: "SquadLeader",
                table: "stats_sessions");

            migrationBuilder.DropColumn(
                name: "SquadName",
                table: "stats_sessions");

            migrationBuilder.DropColumn(
                name: "NearestLocation",
                table: "stats_deaths");

            migrationBuilder.DropColumn(
                name: "NearestLocation",
                table: "stats_damage");

            migrationBuilder.DropColumn(
                name: "NearestLocation",
                table: "stats_aid_records");

            migrationBuilder.DropColumn(
                name: "Team1Faction",
                table: "maps");

            migrationBuilder.DropColumn(
                name: "Team2Faction",
                table: "maps");

            migrationBuilder.AddColumn<uint>(
                name: "TotalSeconds",
                table: "users",
                type: "int unsigned",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AlterColumn<ulong>(
                name: "Steam64",
                table: "stats_deaths",
                type: "bigint unsigned",
                nullable: false,
                oldClrType: typeof(ulong),
                oldNullable: true);

            migrationBuilder.AlterColumn<ulong>(
                name: "Session",
                table: "stats_deaths",
                type: "bigint unsigned",
                nullable: false,
                oldClrType: typeof(ulong),
                oldNullable: true);

            migrationBuilder.AlterColumn<ulong>(
                name: "Steam64",
                table: "stats_damage",
                type: "bigint unsigned",
                nullable: false,
                oldClrType: typeof(ulong),
                oldNullable: true);

            migrationBuilder.AlterColumn<ulong>(
                name: "Session",
                table: "stats_damage",
                type: "bigint unsigned",
                nullable: false,
                oldClrType: typeof(ulong),
                oldNullable: true);

            migrationBuilder.AlterColumn<ulong>(
                name: "Steam64",
                table: "stats_aid_records",
                type: "bigint unsigned",
                nullable: false,
                oldClrType: typeof(ulong),
                oldNullable: true);

            migrationBuilder.AlterColumn<ulong>(
                name: "Session",
                table: "stats_aid_records",
                type: "bigint unsigned",
                nullable: false,
                oldClrType: typeof(ulong),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_kits_factions_Faction",
                table: "kits",
                column: "Faction",
                principalTable: "factions",
                principalColumn: "pk",
                onDelete: ReferentialAction.Restrict);

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
        }
    }
}

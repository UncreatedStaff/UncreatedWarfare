using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Uncreated.Warfare.Migrations
{
    public partial class AddStatsActually3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_maps_seasons_SeasonReleased",
                table: "maps");

            migrationBuilder.DropForeignKey(
                name: "FK_stats_sessions_stats_games_Game",
                table: "stats_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_stats_sessions_stats_sessions_NextSession",
                table: "stats_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_stats_sessions_stats_sessions_PreviousSession",
                table: "stats_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_stats_sessions_users_Steam64",
                table: "stats_sessions");

            migrationBuilder.DropIndex(
                name: "IX_stats_sessions_NextSession",
                table: "stats_sessions");

            migrationBuilder.DropIndex(
                name: "IX_stats_sessions_PreviousSession",
                table: "stats_sessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_maps_dependencies",
                table: "maps_dependencies");

            migrationBuilder.DropIndex(
                name: "IX_maps_dependencies_Map",
                table: "maps_dependencies");

            migrationBuilder.AddColumn<ulong>(
                name: "PlayerDataSteam64",
                table: "stats_sessions",
                nullable: false,
                defaultValue: 0ul);

            migrationBuilder.AddPrimaryKey(
                name: "PK_maps_dependencies",
                table: "maps_dependencies",
                columns: new[] { "Map", "WorkshopId" });

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
                    IsRevive = table.Column<bool>(nullable: false)
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
                    Limb = table.Column<string>(type: "enum('LEFT_FOOT','LEFT_LEG','RIGHT_FOOT','RIGHT_LEG','LEFT_HAND','LEFT_ARM','RIGHT_HAND','RIGHT_ARM','LEFT_BACK','RIGHT_BACK','LEFT_FRONT','RIGHT_FRONT','SPINE','SKULL')", nullable: false)
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
                name: "IX_stats_sessions_PlayerDataSteam64",
                table: "stats_sessions",
                column: "PlayerDataSteam64");

            migrationBuilder.CreateIndex(
                name: "IX_stats_sessions_PreviousSession",
                table: "stats_sessions",
                column: "PreviousSession",
                unique: true);

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

            migrationBuilder.AddForeignKey(
                name: "FK_maps_seasons_SeasonReleased",
                table: "maps",
                column: "SeasonReleased",
                principalTable: "seasons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stats_sessions_stats_games_Game",
                table: "stats_sessions",
                column: "Game",
                principalTable: "stats_games",
                principalColumn: "GameId");

            migrationBuilder.AddForeignKey(
                name: "FK_stats_sessions_users_PlayerDataSteam64",
                table: "stats_sessions",
                column: "PlayerDataSteam64",
                principalTable: "users",
                principalColumn: "Steam64",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_stats_sessions_stats_sessions_PreviousSession",
                table: "stats_sessions",
                column: "PreviousSession",
                principalTable: "stats_sessions",
                principalColumn: "SessionId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_stats_sessions_users_Steam64",
                table: "stats_sessions",
                column: "Steam64",
                principalTable: "users",
                principalColumn: "Steam64");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_maps_seasons_SeasonReleased",
                table: "maps");

            migrationBuilder.DropForeignKey(
                name: "FK_stats_sessions_stats_games_Game",
                table: "stats_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_stats_sessions_users_PlayerDataSteam64",
                table: "stats_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_stats_sessions_stats_sessions_PreviousSession",
                table: "stats_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_stats_sessions_users_Steam64",
                table: "stats_sessions");

            migrationBuilder.DropTable(
                name: "stats_aid_records");

            migrationBuilder.DropTable(
                name: "stats_damage");

            migrationBuilder.DropTable(
                name: "stats_deaths");

            migrationBuilder.DropIndex(
                name: "IX_stats_sessions_PlayerDataSteam64",
                table: "stats_sessions");

            migrationBuilder.DropIndex(
                name: "IX_stats_sessions_PreviousSession",
                table: "stats_sessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_maps_dependencies",
                table: "maps_dependencies");

            migrationBuilder.DropColumn(
                name: "PlayerDataSteam64",
                table: "stats_sessions");

            migrationBuilder.AddPrimaryKey(
                name: "PK_maps_dependencies",
                table: "maps_dependencies",
                column: "WorkshopId");

            migrationBuilder.CreateIndex(
                name: "IX_stats_sessions_NextSession",
                table: "stats_sessions",
                column: "NextSession");

            migrationBuilder.CreateIndex(
                name: "IX_stats_sessions_PreviousSession",
                table: "stats_sessions",
                column: "PreviousSession");

            migrationBuilder.CreateIndex(
                name: "IX_maps_dependencies_Map",
                table: "maps_dependencies",
                column: "Map");

            migrationBuilder.AddForeignKey(
                name: "FK_maps_seasons_SeasonReleased",
                table: "maps",
                column: "SeasonReleased",
                principalTable: "seasons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_stats_sessions_stats_games_Game",
                table: "stats_sessions",
                column: "Game",
                principalTable: "stats_games",
                principalColumn: "GameId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_stats_sessions_stats_sessions_NextSession",
                table: "stats_sessions",
                column: "NextSession",
                principalTable: "stats_sessions",
                principalColumn: "SessionId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stats_sessions_stats_sessions_PreviousSession",
                table: "stats_sessions",
                column: "PreviousSession",
                principalTable: "stats_sessions",
                principalColumn: "SessionId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stats_sessions_users_Steam64",
                table: "stats_sessions",
                column: "Steam64",
                principalTable: "users",
                principalColumn: "Steam64",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

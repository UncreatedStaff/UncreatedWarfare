using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Uncreated.Warfare.Migrations
{
    public partial class AddStatsActually : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    table.PrimaryKey("PK_maps_dependencies", x => x.WorkshopId);
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
                        principalColumn: "GameId",
                        onDelete: ReferentialAction.Cascade);
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
                        name: "FK_stats_sessions_stats_sessions_NextSession",
                        column: x => x.NextSession,
                        principalTable: "stats_sessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stats_sessions_stats_sessions_PreviousSession",
                        column: x => x.PreviousSession,
                        principalTable: "stats_sessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Restrict);
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
                        principalColumn: "Steam64",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_maps_SeasonReleased",
                table: "maps",
                column: "SeasonReleased");

            migrationBuilder.CreateIndex(
                name: "IX_maps_dependencies_Map",
                table: "maps_dependencies",
                column: "Map");

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
                name: "IX_stats_sessions_Steam64",
                table: "stats_sessions",
                column: "Steam64");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "maps_dependencies");

            migrationBuilder.DropTable(
                name: "stats_sessions");

            migrationBuilder.DropTable(
                name: "stats_games");

            migrationBuilder.DropTable(
                name: "maps");

            migrationBuilder.DropTable(
                name: "seasons");
        }
    }
}

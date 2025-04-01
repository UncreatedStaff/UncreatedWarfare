using Microsoft.EntityFrameworkCore.Migrations;
using Uncreated.Warfare.Stats;

namespace Uncreated.Warfare.Migrations
{
    public partial class AddPointsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: MySqlPointsStore.TablePoints,
                columns: table => new
                {
                    Steam64 = table.Column<ulong>(name: MySqlPointsStore.ColumnPointsSteam64, nullable: false),
                    Faction = table.Column<uint>(name: MySqlPointsStore.ColumnPointsFaction, nullable: false),
                    Season = table.Column<int>(name: MySqlPointsStore.ColumnPointsSeason, nullable: false),
                    XP = table.Column<double>(name: MySqlPointsStore.ColumnPointsXP, nullable: false, defaultValue: 0d),
                    Credits = table.Column<double>(name: MySqlPointsStore.ColumnPointsCredits, nullable: false, defaultValue: 0d)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_points", x => new { x.Steam64, x.Faction, x.Season });
                    table.ForeignKey(
                        name: "FK_user_points_seasons_Season",
                        column: x => x.Season,
                        principalTable: "seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_points_factions_Faction",
                        column: x => x.Faction,
                        principalTable: "factions",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: MySqlPointsStore.TableReputation,
                columns: table => new
                {
                    Steam64 = table.Column<ulong>(name: MySqlPointsStore.ColumnReputationSteam64, nullable: false),
                    Reputation = table.Column<double>(name: MySqlPointsStore.ColumnReputationValue, nullable: false, defaultValue: 0d)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_reputation", x => new { x.Steam64 });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: MySqlPointsStore.TableReputation
            );

            migrationBuilder.DropTable(
                name: MySqlPointsStore.TablePoints
            );
        }
    }
}

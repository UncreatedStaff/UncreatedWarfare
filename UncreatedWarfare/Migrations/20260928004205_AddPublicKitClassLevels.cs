using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Uncreated.Warfare.Migrations
{
    public partial class AddPublicKitClassLevels : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreditCost",
                table: "kits");

            migrationBuilder.CreateTable(
                name: "kits_level_access",
                columns: table => new
                {
                    Steam64 = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Class = table.Column<string>(type: "enum('Squadleader','Rifleman','Medic','Breacher','AutomaticRifleman','Grenadier','MachineGunner','LAT','HAT','Marksman','Sniper','APRifleman','CombatEngineer','Crewman','Pilot','SpecOps')", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Level = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Season = table.Column<int>(type: "int", nullable: false),
                    GivenAt = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kits_level_access", x => new { x.Season, x.Steam64, x.Class, x.Level });
                    table.ForeignKey(
                        name: "FK_kits_level_access_seasons_Season",
                        column: x => x.Season,
                        principalTable: "seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_kits_level_access_users_Steam64",
                        column: x => x.Steam64,
                        principalTable: "users",
                        principalColumn: "Steam64",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_kits_level_access_Steam64",
                table: "kits_level_access",
                column: "Steam64");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "kits_level_access");

            migrationBuilder.AddColumn<int>(
                name: "CreditCost",
                table: "kits",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}

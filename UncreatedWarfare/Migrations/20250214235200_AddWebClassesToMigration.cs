using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Uncreated.Warfare.Migrations
{
    public partial class AddWebClassesToMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "loadout_purchases",
                columns: table => new
                {
                    CreatedKit = table.Column<uint>(type: "int unsigned", nullable: false),
                    LoadoutId = table.Column<int>(type: "int", nullable: false),
                    Steam64 = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Paid = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Status = table.Column<string>(type: "enum('AwaitingPayment','AwaitingApproval','ChangesRequested','InProgress','Completed')", nullable: false)
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
                    PlayerChangeRequest = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_loadout_purchases_AdminChangeRequester",
                table: "loadout_purchases",
                column: "AdminChangeRequester");

            migrationBuilder.CreateIndex(
                name: "IX_loadout_purchases_Steam64",
                table: "loadout_purchases",
                column: "Steam64");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "loadout_purchases");
        }
    }
}

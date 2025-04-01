using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using Uncreated.Warfare.Moderation;

namespace Uncreated.Warfare.Migrations
{
    public partial class AddBanListWhitelist : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableBanListWhitelists,
                columns: table => new
                {
                    Steam64 = table.Column<ulong>(name: DatabaseInterface.ColumnBanListWhitelistSteam64, type: "bigint unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Admin = table.Column<ulong>(name: DatabaseInterface.ColumnBanListWhitelistAdmin, type: "bigint unsigned", nullable: false, defaultValue: 0ul),
                    Reason = table.Column<string>(name: DatabaseInterface.ColumnBanListWhitelistReason, type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TimeAddedUTC = table.Column<DateTime>(name: DatabaseInterface.ColumnBanListWhitelistTimestamp, type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moderation_ban_list_whitelists", x => x.Steam64);
                });

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: DatabaseInterface.TableBanListWhitelists);
        }
    }
}

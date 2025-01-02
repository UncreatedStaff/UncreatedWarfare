using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Uncreated.Warfare.Moderation;

namespace Uncreated.Warfare.Migrations
{
    public partial class AddCheatingReportType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.AlterColumn<string>(
                name: DatabaseInterface.ColumnReportsType,
                table: DatabaseInterface.TableReports,
                type: "enum('Custom','Greifing','ChatAbuse','VoiceChatAbuse','Cheating')",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "enum('Custom','Greifing','ChatAbuse')",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.RenameColumn(
                name: DatabaseInterface.ColumnReportsVehicleRequestRecordDamageOrigin,
                table: DatabaseInterface.TableReportShotRecords,
                newName: DatabaseInterface.ColumnReportsShotRecordLimb);

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "warfare_user_pending_accout_links");

            migrationBuilder.AlterColumn<string>(
                name: DatabaseInterface.ColumnReportsType,
                table: DatabaseInterface.TableReports,
                type: "enum('Custom','Greifing','ChatAbuse')",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "enum('Custom','Greifing','ChatAbuse','VoiceChatAbuse','Cheating')",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.RenameColumn(
                name: DatabaseInterface.ColumnReportsShotRecordLimb,
                table: DatabaseInterface.TableReportShotRecords,
                newName: DatabaseInterface.ColumnReportsVehicleRequestRecordDamageOrigin);
        }
    }
}

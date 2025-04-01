using Microsoft.EntityFrameworkCore.Migrations;

namespace Uncreated.Warfare.Migrations
{
    public partial class AddNewKitLimitColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TeamLimit",
                table: "kits");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "loadout_purchases",
                type: "enum('AwaitingApproval','ChangesRequested','InProgress','Completed')",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "enum('AwaitingPayment','AwaitingApproval','ChangesRequested','InProgress','Completed')")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "PlayerChangeRequest",
                table: "loadout_purchases",
                type: "varchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "MinRequiredSquadMembers",
                table: "kits",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresSquad",
                table: "kits",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_kits_bundles_Id",
                table: "kits_bundles",
                column: "Id",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_kits_bundles_Id",
                table: "kits_bundles");

            migrationBuilder.DropColumn(
                name: "MinRequiredSquadMembers",
                table: "kits");

            migrationBuilder.DropColumn(
                name: "RequiresSquad",
                table: "kits");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "loadout_purchases",
                type: "enum('AwaitingPayment','AwaitingApproval','ChangesRequested','InProgress','Completed')",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "enum('AwaitingApproval','ChangesRequested','InProgress','Completed')")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "PlayerChangeRequest",
                table: "loadout_purchases",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(256)",
                oldMaxLength: 256,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<float>(
                name: "TeamLimit",
                table: "kits",
                type: "float",
                nullable: true);
        }
    }
}

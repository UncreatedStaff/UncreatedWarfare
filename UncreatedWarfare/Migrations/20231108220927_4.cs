using Microsoft.EntityFrameworkCore.Migrations;

namespace Uncreated.Warfare.Migrations
{
    public partial class _4 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PermissionLevel",
                table: "users",
                type: "enum('Member','Helper','TrialAdmin','Admin','Superuser')",
                nullable: false,
                defaultValue: "Member",
                oldClrType: typeof(string),
                oldType: "longtext CHARACTER SET utf8mb4",
                oldDefaultValue: "Member");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PermissionLevel",
                table: "users",
                type: "longtext CHARACTER SET utf8mb4",
                nullable: false,
                defaultValue: "Member",
                oldClrType: typeof(string),
                oldType: "enum('Member','Helper','TrialAdmin','Admin','Superuser')",
                oldDefaultValue: "Member");
        }
    }
}

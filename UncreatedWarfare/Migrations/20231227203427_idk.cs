using Microsoft.EntityFrameworkCore.Migrations;

namespace Uncreated.Warfare.Migrations
{
    public partial class idk : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "kits",
                type: "enum('Public','Elite','Special','Loadout','Template')",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "enum('Public','Elite','Special','Loadout')");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "kits",
                type: "enum('Public','Elite','Special','Loadout')",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "enum('Public','Elite','Special','Loadout','Template')");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Uncreated.Warfare.Migrations
{
    public partial class AddRotationToMap : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "stats_fob_items",
                type: "enum('Fob','FobAmmoVendor','RepairStation','Fortification','Emplacement','AmmoCrate')",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "enum('Fob','AmmoCrate','RepairStation','Fortification','Emplacement')")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "RotationEnabled",
                table: "maps",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RotationEnabled",
                table: "maps");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "stats_fob_items",
                type: "enum('Fob','AmmoCrate','RepairStation','Fortification','Emplacement')",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "enum('Fob','FobAmmoVendor','RepairStation','Fortification','Emplacement','AmmoCrate')")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Uncreated.Warfare.Migrations
{
    public partial class RemoveBuildableSaves : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "buildables_display_data");

            migrationBuilder.DropTable(
                name: "buildables_instance_ids");

            migrationBuilder.DropTable(
                name: "buildables_stored_items");

            migrationBuilder.DropTable(
                name: "buildables");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "buildables",
                columns: table => new
                {
                    pk = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Group = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    IsStructure = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Item = table.Column<string>(type: "char(32)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Map = table.Column<int>(type: "int", nullable: true),
                    Owner = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    PositionX = table.Column<float>(type: "float", nullable: false),
                    PositionY = table.Column<float>(type: "float", nullable: false),
                    PositionZ = table.Column<float>(type: "float", nullable: false),
                    RotationX = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    RotationY = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    RotationZ = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    State = table.Column<byte[]>(type: "varbinary(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_buildables", x => x.pk);
                    table.ForeignKey(
                        name: "FK_buildables_maps_Map",
                        column: x => x.Map,
                        principalTable: "maps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "buildables_display_data",
                columns: table => new
                {
                    pk = table.Column<int>(type: "int", nullable: false),
                    DynamicProps = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Mythic = table.Column<string>(type: "char(32)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Rotation = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Skin = table.Column<string>(type: "char(32)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tags = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_buildables_display_data", x => x.pk);
                    table.ForeignKey(
                        name: "FK_buildables_display_data_buildables_pk",
                        column: x => x.pk,
                        principalTable: "buildables",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "buildables_instance_ids",
                columns: table => new
                {
                    pk = table.Column<int>(type: "int", nullable: false),
                    RegionId = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    InstanceId = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_buildables_instance_ids", x => new { x.pk, x.RegionId });
                    table.ForeignKey(
                        name: "FK_buildables_instance_ids_buildables_pk",
                        column: x => x.pk,
                        principalTable: "buildables",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "buildables_stored_items",
                columns: table => new
                {
                    Save = table.Column<int>(type: "int", nullable: false),
                    PositionX = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    PositionY = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Amount = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Item = table.Column<string>(type: "char(32)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Quality = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Rotation = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    State = table.Column<byte[]>(type: "varbinary(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_buildables_stored_items", x => new { x.Save, x.PositionX, x.PositionY });
                    table.ForeignKey(
                        name: "FK_buildables_stored_items_buildables_Save",
                        column: x => x.Save,
                        principalTable: "buildables",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_buildables_Map",
                table: "buildables",
                column: "Map");
        }
    }
}

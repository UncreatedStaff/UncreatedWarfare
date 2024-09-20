using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Uncreated.Warfare.Migrations
{
    public partial class S4first : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PermissionLevel",
                table: "users");
            
            migrationBuilder.AlterColumn<int>(
                name: "Team",
                table: "stats_sessions",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint unsigned");
            
            migrationBuilder.AlterColumn<string>(
                name: "Gamemode",
                table: "stats_games",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "enum('Undefined','TeamCTF','Invasion','Insurgency','Conquest','Hardpoint','Deathmatch')");

            migrationBuilder.AddColumn<bool>(
                name: "SupportsPluralization",
                table: "lang_info",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "buildables",
                columns: table => new
                {
                    pk = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Map = table.Column<int>(nullable: true),
                    Item = table.Column<string>(type: "char(32)", nullable: false),
                    IsStructure = table.Column<bool>(nullable: false),
                    PositionX = table.Column<float>(nullable: false),
                    PositionY = table.Column<float>(nullable: false),
                    PositionZ = table.Column<float>(nullable: false),
                    RotationX = table.Column<byte>(nullable: false),
                    RotationY = table.Column<byte>(nullable: false),
                    RotationZ = table.Column<byte>(nullable: false),
                    Owner = table.Column<ulong>(nullable: false),
                    Group = table.Column<ulong>(nullable: false),
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
                name: "item_whitelists",
                columns: table => new
                {
                    pk = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Item = table.Column<string>(type: "char(32)", nullable: false),
                    Amount = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_whitelists", x => x.pk);
                });

            migrationBuilder.CreateTable(
                name: "user_permissions",
                columns: table => new
                {
                    pk = table.Column<uint>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Steam64 = table.Column<ulong>(nullable: false),
                    IsGroup = table.Column<bool>(nullable: false),
                    PermissionOrGroup = table.Column<string>(maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_permissions", x => x.pk);
                });

            migrationBuilder.CreateTable(
                name: "buildables_display_data",
                columns: table => new
                {
                    pk = table.Column<int>(nullable: false),
                    Skin = table.Column<string>(type: "char(32)", nullable: false),
                    Mythic = table.Column<string>(type: "char(32)", nullable: false),
                    Rotation = table.Column<byte>(nullable: false),
                    Tags = table.Column<string>(maxLength: 255, nullable: true),
                    DynamicProps = table.Column<string>(maxLength: 255, nullable: true)
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
                    pk = table.Column<int>(nullable: false),
                    RegionId = table.Column<byte>(nullable: false),
                    InstanceId = table.Column<uint>(nullable: false)
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
                    Save = table.Column<int>(nullable: false),
                    PositionX = table.Column<byte>(nullable: false),
                    PositionY = table.Column<byte>(nullable: false),
                    Item = table.Column<string>(type: "char(32)", nullable: false),
                    Amount = table.Column<byte>(nullable: false),
                    Quality = table.Column<byte>(nullable: false),
                    Rotation = table.Column<byte>(nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_user_permissions_Steam64",
                table: "user_permissions",
                column: "Steam64");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "buildables_display_data");

            migrationBuilder.DropTable(
                name: "buildables_instance_ids");

            migrationBuilder.DropTable(
                name: "buildables_stored_items");

            migrationBuilder.DropTable(
                name: "item_whitelists");

            migrationBuilder.DropTable(
                name: "user_permissions");

            migrationBuilder.DropTable(
                name: "buildables");

            migrationBuilder.DropColumn(
                name: "SupportsPluralization",
                table: "lang_info");

            migrationBuilder.AddColumn<string>(
                name: "PermissionLevel",
                table: "users",
                type: "enum('Member','Helper','TrialAdmin','Admin','Superuser')",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<byte>(
                name: "Team",
                table: "stats_sessions",
                type: "tinyint unsigned",
                nullable: false,
                oldClrType: typeof(int));

            migrationBuilder.AlterColumn<string>(
                name: "Gamemode",
                table: "stats_games",
                type: "enum('Undefined','TeamCTF','Invasion','Insurgency','Conquest','Hardpoint','Deathmatch')",
                nullable: false,
                oldClrType: typeof(string));
        }
    }
}

using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Uncreated.Warfare.Migrations
{
    public partial class KitRewrite : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_kits_sign_text_lang_info_Language",
                table: "kits_sign_text");

            migrationBuilder.DropIndex(
                name: "IX_kits_sign_text_Language",
                table: "kits_sign_text");

            migrationBuilder.DropColumn(
                name: "Json",
                table: "kits_unlock_requirements");

            migrationBuilder.AddColumn<string>(
                name: "Data",
                table: "kits_unlock_requirements",
                type: "varchar(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "kits_unlock_requirements",
                type: "varchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<uint>(
                name: "KitModelPrimaryKey",
                table: "kits_favorites",
                type: "int unsigned",
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "KitModelPrimaryKey",
                table: "kits_access",
                type: "int unsigned",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "kits_delays",
                columns: table => new
                {
                    Id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Kit = table.Column<uint>(type: "int unsigned", nullable: false),
                    KitModelPrimaryKey = table.Column<uint>(type: "int unsigned", nullable: true),
                    Data = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kits_delays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_kits_delays_kits_KitModelPrimaryKey",
                        column: x => x.KitModelPrimaryKey,
                        principalTable: "kits",
                        principalColumn: "pk",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_kits_favorites_KitModelPrimaryKey",
                table: "kits_favorites",
                column: "KitModelPrimaryKey");

            migrationBuilder.CreateIndex(
                name: "IX_kits_access_KitModelPrimaryKey",
                table: "kits_access",
                column: "KitModelPrimaryKey");

            migrationBuilder.CreateIndex(
                name: "IX_kits_Id",
                table: "kits",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_kits_delays_KitModelPrimaryKey",
                table: "kits_delays",
                column: "KitModelPrimaryKey");

            migrationBuilder.AddForeignKey(
                name: "FK_kits_access_kits_KitModelPrimaryKey",
                table: "kits_access",
                column: "KitModelPrimaryKey",
                principalTable: "kits",
                principalColumn: "pk",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_kits_favorites_kits_KitModelPrimaryKey",
                table: "kits_favorites",
                column: "KitModelPrimaryKey",
                principalTable: "kits",
                principalColumn: "pk",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_kits_access_kits_KitModelPrimaryKey",
                table: "kits_access");

            migrationBuilder.DropForeignKey(
                name: "FK_kits_favorites_kits_KitModelPrimaryKey",
                table: "kits_favorites");

            migrationBuilder.DropTable(
                name: "kits_delays");

            migrationBuilder.DropIndex(
                name: "IX_kits_favorites_KitModelPrimaryKey",
                table: "kits_favorites");

            migrationBuilder.DropIndex(
                name: "IX_kits_access_KitModelPrimaryKey",
                table: "kits_access");

            migrationBuilder.DropIndex(
                name: "IX_kits_Id",
                table: "kits");

            migrationBuilder.DropColumn(
                name: "Data",
                table: "kits_unlock_requirements");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "kits_unlock_requirements");

            migrationBuilder.DropColumn(
                name: "KitModelPrimaryKey",
                table: "kits_favorites");

            migrationBuilder.DropColumn(
                name: "KitModelPrimaryKey",
                table: "kits_access");

            migrationBuilder.AddColumn<string>(
                name: "Json",
                table: "kits_unlock_requirements",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_kits_sign_text_Language",
                table: "kits_sign_text",
                column: "Language");

            migrationBuilder.AddForeignKey(
                name: "FK_kits_sign_text_lang_info_Language",
                table: "kits_sign_text",
                column: "Language",
                principalTable: "lang_info",
                principalColumn: "pk",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

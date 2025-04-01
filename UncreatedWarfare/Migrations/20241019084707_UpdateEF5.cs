using Microsoft.EntityFrameworkCore.Migrations;

namespace Uncreated.Warfare.Migrations
{
    public partial class UpdateEF5 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "users")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "user_permissions")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "stats_sessions")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "stats_games")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "stats_fobs")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "stats_fob_items_builders")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "stats_fob_items")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "stats_deaths")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "stats_damage")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "stats_aid_records")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "seasons")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "maps_dependencies")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "maps")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "lang_preferences")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "lang_info")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "lang_cultures")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "lang_credits")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "lang_aliases")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "kits_unlock_requirements")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "kits_skillsets")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "kits_sign_text")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "kits_map_filters")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "kits_layouts")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "kits_items")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "kits_hotkeys")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "kits_favorites")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "kits_faction_filters")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "kits_bundles")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "kits_bundle_items")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "kits_access")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "kits")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "item_whitelists")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "ip_addresses")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "hwids")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "homebase_auth_keys")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "factions")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "faction_translations")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "faction_assets")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "buildables_stored_items")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "buildables_instance_ids")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "buildables_display_data")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "buildables")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "stats_fob_items",
                type: "enum('Fob','AmmoCrate','RepairStation','Fortification','Emplacement')",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "enum('Bunker','AmmoCrate','RepairStation','Fortification','Emplacement')")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<ulong>(
                name: "Fob",
                table: "stats_fob_items",
                type: "bigint unsigned",
                nullable: false,
                defaultValue: 0ul,
                oldClrType: typeof(ulong),
                oldType: "bigint unsigned",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "users")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "user_permissions")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "stats_sessions")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "stats_games")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "stats_fobs")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "stats_fob_items_builders")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "stats_fob_items")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "stats_deaths")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "stats_damage")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "stats_aid_records")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "seasons")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "maps_dependencies")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "maps")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "lang_preferences")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "lang_info")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "lang_cultures")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "lang_credits")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "lang_aliases")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "kits_unlock_requirements")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "kits_skillsets")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "kits_sign_text")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "kits_map_filters")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "kits_layouts")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "kits_items")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "kits_hotkeys")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "kits_favorites")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "kits_faction_filters")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "kits_bundles")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "kits_bundle_items")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "kits_access")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "kits")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "item_whitelists")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "ip_addresses")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "hwids")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "homebase_auth_keys")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "factions")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "faction_translations")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "faction_assets")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "buildables_stored_items")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "buildables_instance_ids")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "buildables_display_data")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterTable(
                name: "buildables")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Gamemode",
                table: "stats_games",
                type: "longtext CHARACTER SET utf8mb4",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "stats_fob_items",
                type: "enum('Bunker','AmmoCrate','RepairStation','Fortification','Emplacement')",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "enum('Fob','AmmoCrate','RepairStation','Fortification','Emplacement')")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<ulong>(
                name: "Fob",
                table: "stats_fob_items",
                type: "bigint unsigned",
                nullable: true,
                oldClrType: typeof(ulong),
                oldType: "bigint unsigned");
        }
    }
}

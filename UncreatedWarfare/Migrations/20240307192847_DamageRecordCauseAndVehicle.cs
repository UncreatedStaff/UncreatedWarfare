using Microsoft.EntityFrameworkCore.Migrations;

namespace Uncreated.Warfare.Migrations
{
    public partial class DamageRecordCauseAndVehicle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Origin",
                table: "stats_damage");

            migrationBuilder.AddColumn<string>(
                name: "PrimaryAssetName",
                table: "stats_deaths",
                maxLength: 48,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondaryAssetName",
                table: "stats_deaths",
                maxLength: 48,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Vehicle",
                table: "stats_deaths",
                type: "char(32)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleName",
                table: "stats_deaths",
                maxLength: 48,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cause",
                table: "stats_damage",
                type: "enum('BLEEDING','BONES','FREEZING','BURNING','FOOD','WATER','GUN','MELEE','ZOMBIE','ANIMAL','SUICIDE','KILL','INFECTION','PUNCH','BREATH','ROADKILL','VEHICLE','GRENADE','SHRED','LANDMINE','ARENA','MISSILE','CHARGE','SPLASH','SENTRY','ACID','BOULDER','BURNER','SPIT','SPARK')",
                nullable: false,
                defaultValue: "KILL");

            migrationBuilder.AddColumn<bool>(
                name: "IsInjured",
                table: "stats_damage",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<float>(
                name: "TimeDeployedSeconds",
                table: "stats_damage",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<string>(
                name: "Vehicle",
                table: "stats_damage",
                type: "char(32)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleName",
                table: "stats_damage",
                maxLength: 48,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrimaryAssetName",
                table: "stats_deaths");

            migrationBuilder.DropColumn(
                name: "SecondaryAssetName",
                table: "stats_deaths");

            migrationBuilder.DropColumn(
                name: "Vehicle",
                table: "stats_deaths");

            migrationBuilder.DropColumn(
                name: "VehicleName",
                table: "stats_deaths");

            migrationBuilder.DropColumn(
                name: "Cause",
                table: "stats_damage");

            migrationBuilder.DropColumn(
                name: "IsInjured",
                table: "stats_damage");

            migrationBuilder.DropColumn(
                name: "TimeDeployedSeconds",
                table: "stats_damage");

            migrationBuilder.DropColumn(
                name: "Vehicle",
                table: "stats_damage");

            migrationBuilder.DropColumn(
                name: "VehicleName",
                table: "stats_damage");

            migrationBuilder.AddColumn<string>(
                name: "Origin",
                table: "stats_damage",
                type: "enum('Unknown','Mega_Zombie_Boulder','Vehicle_Bumper','Horde_Beacon_Self_Destruct','Trap_Wear_And_Tear','Carepackage_Timeout','Plant_Harvested','Charge_Self_Destruct','Zombie_Swipe','Grenade_Explosion','Rocket_Explosion','Food_Explosion','Vehicle_Explosion','Charge_Explosion','Trap_Explosion','Bullet_Explosion','Radioactive_Zombie_Explosion','Flamable_Zombie_Explosion','Zombie_Electric_Shock','Zombie_Stomp','Zombie_Fire_Breath','Sentry','Useable_Gun','Useable_Melee','Punch','Animal_Attack','Kill_Volume','Vehicle_Collision_Self_Damage','Lightning','VehicleDecay')",
                nullable: false,
                defaultValue: "");
        }
    }
}

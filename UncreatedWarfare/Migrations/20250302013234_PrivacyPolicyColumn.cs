using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Uncreated.Warfare.Migrations
{
    public partial class PrivacyPolicyColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastPrivacyPolicyAccepted",
                table: "users",
                type: "datetime",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastPrivacyPolicyAccepted",
                table: "users");
        }
    }
}

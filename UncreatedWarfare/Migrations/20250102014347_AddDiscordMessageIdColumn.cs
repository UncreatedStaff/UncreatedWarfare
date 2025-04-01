using Microsoft.EntityFrameworkCore.Migrations;
using Uncreated.Warfare.Moderation;

namespace Uncreated.Warfare.Migrations
{
    public partial class AddDiscordMessageIdColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: DatabaseInterface.ColumnEntriesDiscordMessageId,
                table: DatabaseInterface.TableEntries,
                nullable: false,
                defaultValue: 0ul);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: DatabaseInterface.ColumnEntriesDiscordMessageId,
                table: DatabaseInterface.TableEntries);
        }
    }
}

using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Uncreated.Warfare.Moderation;

namespace Uncreated.Warfare.Migrations
{
    public partial class AddVoiceChatReportsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableVoiceChatReports,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    VoiceData = table.Column<byte[]>(name: DatabaseInterface.ColumnVoiceChatReportsData, type: "blob", nullable: true, defaultValueSql: "NULL")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moderation_voice_chat_reports_Entry", x => x.Entry);
                    table.ForeignKey(
                        name: "FK_moderation_voice_chat_reports_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_voice_chat_reports_Entry",
                table: DatabaseInterface.TableVoiceChatReports,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: DatabaseInterface.TableVoiceChatReports);
        }
    }
}

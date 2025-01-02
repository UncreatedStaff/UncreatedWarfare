using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using Uncreated.Warfare.Moderation;
using Migration = Microsoft.EntityFrameworkCore.Migrations.Migration;

namespace Uncreated.Warfare.Migrations
{
    public partial class AddMigrationTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableEntries,
                columns: table => new
                {
                    Id = table.Column<int>(name: DatabaseInterface.ColumnEntriesPrimaryKey, nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Type = table.Column<string>(name: DatabaseInterface.ColumnEntriesType, type: "enum('Warning','Kick','Ban','Mute','AssetBan','Teamkill','VehicleTeamkill','BattlEyeKick','Appeal','Report','GriefingReport','ChatAbuseReport','CheatingReport','Note','Commendation','BugReportAccepted','PlayerReportAccepted')", nullable: false),
                    Steam64 = table.Column<ulong>(name: DatabaseInterface.ColumnEntriesSteam64, nullable: false),
                    Message = table.Column<string>(name: DatabaseInterface.ColumnEntriesMessage, maxLength: 1024, nullable: true, defaultValueSql: "NULL"),
                    IsLegacy = table.Column<bool>(name: DatabaseInterface.ColumnEntriesIsLegacy, nullable: false, defaultValue: false),
                    LegacyId = table.Column<uint>(name: DatabaseInterface.ColumnEntriesLegacyId, nullable: true, defaultValueSql: "NULL"),
                    StartTimeUTC = table.Column<DateTime>(name: DatabaseInterface.ColumnEntriesStartTimestamp, type: "datetime", nullable: false),
                    ResolvedTimeUTC = table.Column<DateTime>(name: DatabaseInterface.ColumnEntriesResolvedTimestamp, type: "datetime", nullable: true, defaultValueSql: "NULL"),
                    PendingReputation = table.Column<double>(name: DatabaseInterface.ColumnEntriesPendingReputation, nullable: true, defaultValue: 0d),
                    Reputation = table.Column<double>(name: DatabaseInterface.ColumnEntriesReputation, nullable: false, defaultValue: 0d),
                    RelavantLogsStartTimeUTC = table.Column<DateTime>(name: DatabaseInterface.ColumnEntriesRelavantLogsStartTimestamp, type: "datetime", nullable: true, defaultValueSql: "NULL"),
                    RelavantLogsEndTimeUTC = table.Column<DateTime>(name: DatabaseInterface.ColumnEntriesRelavantLogsEndTimestamp, type: "datetime", nullable: true, defaultValueSql: "NULL"),
                    Removed = table.Column<bool>(name: DatabaseInterface.ColumnEntriesRemoved, nullable: false, defaultValue: false),
                    RemovedBy = table.Column<ulong>(name: DatabaseInterface.ColumnEntriesRemovedBy, nullable: true, defaultValueSql: "NULL"),
                    RemovedTimeUTC = table.Column<DateTime>(name: DatabaseInterface.ColumnEntriesRemovedTimestamp, type: "datetime", nullable: true, defaultValueSql: "NULL"),
                    RemovedReason = table.Column<string>(name: DatabaseInterface.ColumnEntriesRemovedReason, maxLength: 1024, nullable: true, defaultValueSql: "NULL")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moderation_entries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_entries_Steam64",
                table: DatabaseInterface.TableEntries,
                column: DatabaseInterface.ColumnEntriesSteam64
            );

            migrationBuilder.CreateIndex(
                name: "IX_moderation_entries_RemovedBy",
                table: DatabaseInterface.TableEntries,
                column: DatabaseInterface.ColumnEntriesRemovedBy
            );

            migrationBuilder.CreateIndex(
                name: "IX_moderation_entries_LegacyId",
                table: DatabaseInterface.TableEntries,
                column: DatabaseInterface.ColumnEntriesLegacyId
            );

            migrationBuilder.CreateIndex(
                name: "IX_moderation_entries_Type",
                table: DatabaseInterface.TableEntries,
                column: DatabaseInterface.ColumnEntriesType
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableActors,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false),
                    ActorRole = table.Column<string>(name: DatabaseInterface.ColumnActorsRole, nullable: false, maxLength: 255),
                    ActorId = table.Column<ulong>(name: DatabaseInterface.ColumnActorsId, nullable: false),
                    ActorAsAdmin = table.Column<bool>(name: DatabaseInterface.ColumnActorsAsAdmin, nullable: false),
                    ActorIndex = table.Column<int>(name: DatabaseInterface.ColumnActorsIndex, nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_moderation_actors_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_actors_Entry",
                table: DatabaseInterface.TableActors,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateIndex(
                name: "IX_moderation_actors_ActorId",
                table: DatabaseInterface.TableActors,
                column: DatabaseInterface.ColumnActorsId
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableEvidence,
                columns: table => new
                {
                    Id = table.Column<int>(name: DatabaseInterface.ColumnEntriesPrimaryKey, nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: true, defaultValueSql: "NULL"),
                    EvidenceURL = table.Column<string>(name: DatabaseInterface.ColumnEvidenceLink, nullable: false, maxLength: 512),
                    EvidenceLocalSource = table.Column<string>(name: DatabaseInterface.ColumnEvidenceLocalSource, nullable: true, maxLength: 512, defaultValueSql: "NULL"),
                    EvidenceIsImage = table.Column<bool>(name: DatabaseInterface.ColumnEvidenceIsImage, nullable: false),
                    EvidenceTimestampUTC = table.Column<bool>(name: DatabaseInterface.ColumnEvidenceTimestamp, type: "datetime", nullable: false),
                    EvidenceActor = table.Column<ulong>(name: DatabaseInterface.ColumnEvidenceActorId, nullable: true, defaultValueSql: "NULL"),
                    EvidenceMessage = table.Column<string>(name: DatabaseInterface.ColumnEvidenceMessage, maxLength: 1024, nullable: true, defaultValueSql: "NULL")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moderation_evidence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_moderation_evidence_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_evidence_Entry",
                table: DatabaseInterface.TableEvidence,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateIndex(
                name: "IX_moderation_evidence_EvidenceActor",
                table: DatabaseInterface.TableEvidence,
                column: DatabaseInterface.ColumnEvidenceActorId
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableRelatedEntries,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false),
                    RelatedEntry = table.Column<int>(name: DatabaseInterface.ColumnRelatedEntry, nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_moderation_related_entries_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);

                    table.ForeignKey(
                        name: "FK_moderation_related_entries_moderation_entries_RelatedEntry",
                        column: x => x.RelatedEntry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_related_entries_Entry",
                table: DatabaseInterface.TableRelatedEntries,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateIndex(
                name: "IX_moderation_related_entries_RelatedEntry",
                table: DatabaseInterface.TableRelatedEntries,
                column: DatabaseInterface.ColumnRelatedEntry
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableAssetBanTypeFilters,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false),
                    VehicleType = table.Column<string>(name: DatabaseInterface.ColumnAssetBanFiltersType, type: "enum('None','Humvee','TransportGround','ScoutCar','LogisticsGround','APC','IFV','MBT','TransportAir','AttackHeli','Jet','Emplacement','AA','HMG','ATGM','Mortar')", nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_moderation_asset_ban_filters_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_asset_ban_filters_Entry",
                table: DatabaseInterface.TableAssetBanTypeFilters,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TablePunishments,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PresetType = table.Column<string>(name: DatabaseInterface.ColumnPunishmentsPresetType, type: "enum('None','Griefing','Toxicity','Soloing','AssetWaste','IntentionalTeamkilling','TargetedHarassment','Discrimination','Cheating','DisruptiveBehavior','InappropriateProfile','BypassingPunishment')", nullable: true, defaultValueSql: "NULL"),
                    PresetLevel = table.Column<int>(name: DatabaseInterface.ColumnPunishmentsPresetLevel, nullable: true, defaultValueSql: "NULL")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moderation_punishments_Entry", x => x.Entry);
                    table.ForeignKey(
                        name: "FK_moderation_punishments_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_punishments_Entry",
                table: DatabaseInterface.TablePunishments,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateIndex(
                name: "IX_moderation_punishments_PresetType",
                table: DatabaseInterface.TablePunishments,
                column: DatabaseInterface.ColumnPunishmentsPresetType
            );

            migrationBuilder.CreateIndex(
                name: "IX_moderation_punishments_PresetLevel",
                table: DatabaseInterface.TablePunishments,
                column: DatabaseInterface.ColumnPunishmentsPresetLevel
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableDurationPunishments,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Duration = table.Column<long>(name: DatabaseInterface.ColumnDurationsDurationSeconds, nullable: false),
                    Forgiven = table.Column<bool>(name: DatabaseInterface.ColumnDurationsForgiven, nullable: false, defaultValue: false),
                    ForgivenBy = table.Column<ulong>(name: DatabaseInterface.ColumnDurationsForgivenBy, nullable: true, defaultValueSql: "NULL"),
                    ForgivenTimeUTC = table.Column<DateTime>(name: DatabaseInterface.ColumnDurationsForgivenTimestamp, type: "datetime", nullable: true, defaultValueSql: "NULL"),
                    ForgivenReason = table.Column<string>(name: DatabaseInterface.ColumnDurationsForgivenReason, maxLength: 1024, nullable: true, defaultValueSql: "NULL")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moderation_durations_Entry", x => x.Entry);
                    table.ForeignKey(
                        name: "FK_moderation_durations_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_durations_Entry",
                table: DatabaseInterface.TableDurationPunishments,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateIndex(
                name: "IX_moderation_durations_ForgivenBy",
                table: DatabaseInterface.TableDurationPunishments,
                column: DatabaseInterface.ColumnDurationsForgivenBy
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableLinkedAppeals,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false),
                    LinkedAppeal = table.Column<int>(name: DatabaseInterface.ColumnLinkedAppealsAppeal, nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_moderation_linked_appeals_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);

                    table.ForeignKey(
                        name: "FK_moderation_linked_appeals_moderation_entries_LinkedAppeal",
                        column: x => x.LinkedAppeal,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_linked_appeals_Entry",
                table: DatabaseInterface.TableLinkedAppeals,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateIndex(
                name: "IX_moderation_linked_appeals_LinkedAppeal",
                table: DatabaseInterface.TableLinkedAppeals,
                column: DatabaseInterface.ColumnLinkedAppealsAppeal
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableLinkedReports,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false),
                    LinkedReport = table.Column<int>(name: DatabaseInterface.ColumnLinkedReportsReport, nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_moderation_linked_reports_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);

                    table.ForeignKey(
                        name: "FK_moderation_linked_reports_moderation_entries_LinkedReport",
                        column: x => x.LinkedReport,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_linked_reports_Entry",
                table: DatabaseInterface.TableLinkedReports,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateIndex(
                name: "IX_moderation_linked_reports_LinkedReport",
                table: DatabaseInterface.TableLinkedReports,
                column: DatabaseInterface.ColumnLinkedReportsReport
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableMutes,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MuteType = table.Column<string>(name: DatabaseInterface.ColumnMutesType, type: "enum('Voice','Text','Both')", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moderation_mutes_Entry", x => x.Entry);
                    table.ForeignKey(
                        name: "FK_moderation_mutes_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_mutes_Entry",
                table: DatabaseInterface.TableMutes,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableWarnings,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Displayed = table.Column<DateTime>(name: DatabaseInterface.ColumnWarningsDisplayedTimestamp, type: "datetime", nullable: true, defaultValueSql: "NULL")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moderation_warnings_Entry", x => x.Entry);
                    table.ForeignKey(
                        name: "FK_moderation_warnings_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_warnings_Entry",
                table: DatabaseInterface.TableWarnings,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TablePlayerReportAccepteds,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AcceptedReport = table.Column<int>(name: DatabaseInterface.ColumnPlayerReportAcceptedsReport, nullable: true, defaultValueSql: "NULL")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moderation_accepted_player_reports_Entry", x => x.Entry);
                    table.ForeignKey(
                        name: "FK_moderation_accepted_player_reports_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);

                    table.ForeignKey(
                        name: "FK_moderation_accepted_player_reports_moderation_entries_AcceptedReport",
                        column: x => x.AcceptedReport,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_accepted_player_reports_Entry",
                table: DatabaseInterface.TablePlayerReportAccepteds,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateIndex(
                name: "IX_moderation_accepted_player_reports_AcceptedReport",
                table: DatabaseInterface.TablePlayerReportAccepteds,
                column: DatabaseInterface.ColumnPlayerReportAcceptedsReport
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableBugReportAccepteds,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AcceptedCommit = table.Column<string>(name: DatabaseInterface.ColumnTableBugReportAcceptedsCommit, nullable: true, defaultValueSql: "NULL"),
                    AcceptedIssue = table.Column<int>(name: DatabaseInterface.ColumnTableBugReportAcceptedsIssue, nullable: true, defaultValueSql: "NULL")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moderation_accepted_bug_reports_Entry", x => x.Entry);
                    table.ForeignKey(
                        name: "FK_moderation_accepted_bug_reports_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_accepted_bug_reports_Entry",
                table: DatabaseInterface.TableBugReportAccepteds,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableTeamkills,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Asset = table.Column<string>(name: DatabaseInterface.ColumnTeamkillsAsset, type: "char(32)", nullable: true, defaultValueSql: "NULL"),
                    AssetName = table.Column<string>(name: DatabaseInterface.ColumnTeamkillsAssetName, maxLength: 48, nullable: true, defaultValueSql: "NULL"),
                    DeathCause = table.Column<string>(name: DatabaseInterface.ColumnTeamkillsDeathCause, type: "enum('BLEEDING','BONES','FREEZING','BURNING','FOOD','WATER','GUN','MELEE','ZOMBIE','ANIMAL','SUICIDE','KILL','INFECTION','PUNCH','BREATH','ROADKILL','VEHICLE','GRENADE','SHRED','LANDMINE','ARENA','MISSILE','CHARGE','SPLASH','SENTRY','ACID','BOULDER','BURNER','SPIT','SPARK')", nullable: true, defaultValueSql: "NULL"),
                    Distance = table.Column<float>(name: DatabaseInterface.ColumnTeamkillsDistance, nullable: true, defaultValueSql: "NULL"),
                    Limb = table.Column<string>(name: DatabaseInterface.ColumnTeamkillsLimb, type: "enum('LEFT_FOOT','LEFT_LEG','RIGHT_FOOT','RIGHT_LEG','LEFT_HAND','LEFT_ARM','RIGHT_HAND','RIGHT_ARM','LEFT_BACK','RIGHT_BACK','LEFT_FRONT','RIGHT_FRONT','SPINE','SKULL')", nullable: true, defaultValueSql: "NULL"),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moderation_teamkills_Entry", x => x.Entry);
                    table.ForeignKey(
                        name: "FK_moderation_teamkills_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_teamkills_Entry",
                table: DatabaseInterface.TableTeamkills,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableVehicleTeamkills,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    VehicleAsset = table.Column<string>(name: DatabaseInterface.ColumnVehicleTeamkillsVehicleAsset, type: "char(32)", nullable: true, defaultValueSql: "NULL"),
                    VehicleAssetName = table.Column<string>(name: DatabaseInterface.ColumnVehicleTeamkillsVehicleAssetName, maxLength: 48, nullable: true, defaultValueSql: "NULL"),
                    DamageOrigin = table.Column<string>(name: DatabaseInterface.ColumnVehicleTeamkillsDamageOrigin, type: "enum('Unknown','Mega_Zombie_Boulder','Vehicle_Bumper','Horde_Beacon_Self_Destruct','Trap_Wear_And_Tear','Carepackage_Timeout','Plant_Harvested','Charge_Self_Destruct','Zombie_Swipe','Grenade_Explosion','Rocket_Explosion','Food_Explosion','Vehicle_Explosion','Charge_Explosion','Trap_Explosion','Bullet_Explosion','Radioactive_Zombie_Explosion','Flamable_Zombie_Explosion','Zombie_Electric_Shock','Zombie_Stomp','Zombie_Fire_Breath','Sentry','Useable_Gun','Useable_Melee','Punch','Animal_Attack','Kill_Volume','Vehicle_Collision_Self_Damage','Lightning','VehicleDecay')", nullable: true, defaultValueSql: "NULL")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moderation_vehicle_teamkills_Entry", x => x.Entry);
                    table.ForeignKey(
                        name: "FK_moderation_vehicle_teamkills_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_vehicle_teamkills_Entry",
                table: DatabaseInterface.TableVehicleTeamkills,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableAppeals,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TicketId = table.Column<string>(name: DatabaseInterface.ColumnAppealsTicketId, type: "char(32)", nullable: false),
                    State = table.Column<bool>(name: DatabaseInterface.ColumnAppealsState, nullable: true, defaultValueSql: "NULL"),
                    DiscordId = table.Column<ulong>(name: DatabaseInterface.ColumnAppealsDiscordId, nullable: true, defaultValueSql: "NULL")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moderation_appeals_Entry", x => x.Entry);
                    table.ForeignKey(
                        name: "FK_moderation_appeals_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_appeals_Entry",
                table: DatabaseInterface.TableAppeals,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableAppealPunishments,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false),
                    Punishment = table.Column<int>(name: DatabaseInterface.ColumnAppealPunishmentsPunishment, nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_moderation_appeal_punishments_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);

                    table.ForeignKey(
                        name: "FK_moderation_appeal_punishments_moderation_entries_Punishment",
                        column: x => x.Punishment,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_appeal_punishments_Entry",
                table: DatabaseInterface.TableAppealPunishments,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateIndex(
                name: "IX_moderation_appeal_punishments_Punishment",
                table: DatabaseInterface.TableAppealPunishments,
                column: DatabaseInterface.ColumnAppealPunishmentsPunishment
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableAppealResponses,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false),
                    Question = table.Column<string>(name: DatabaseInterface.ColumnAppealResponsesQuestion, maxLength: 255, nullable: false),
                    Response = table.Column<string>(name: DatabaseInterface.ColumnAppealResponsesResponse, maxLength: 1024, nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_moderation_appeal_responses_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_appeal_responses_Entry",
                table: DatabaseInterface.TableAppealResponses,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableReports,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Type = table.Column<string>(name: DatabaseInterface.ColumnReportsType, type: "enum('Custom','Greifing','ChatAbuse')", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moderation_reports_Entry", x => x.Entry);
                    table.ForeignKey(
                        name: "FK_moderation_reports_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_reports_Entry",
                table: DatabaseInterface.TableReports,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableReportChatRecords,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false),
                    Message = table.Column<string>(name: DatabaseInterface.ColumnReportsChatRecordsMessage, maxLength: 512, nullable: false),
                    TimeUTC = table.Column<DateTime>(name: DatabaseInterface.ColumnReportsChatRecordsTimestamp, type: "datetime", nullable: false),
                    Index = table.Column<int>(name: DatabaseInterface.ColumnReportsChatRecordsIndex, nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_moderation_report_chat_records_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_report_chat_records_Entry",
                table: DatabaseInterface.TableReportChatRecords,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableReportStructureDamageRecords,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false),
                    Structure = table.Column<string>(name: DatabaseInterface.ColumnReportsStructureDamageStructure, type: "char(32)", nullable: false),
                    StructureName = table.Column<string>(name: DatabaseInterface.ColumnReportsStructureDamageStructureName, maxLength: 48, nullable: false),
                    StructureOwner = table.Column<ulong>(name: DatabaseInterface.ColumnReportsStructureDamageStructureOwner, nullable: false),
                    StructureType = table.Column<string>(name: DatabaseInterface.ColumnReportsStructureDamageStructureType, type: "enum('Structure','Barricade')", nullable: false),
                    Damage = table.Column<int>(name: DatabaseInterface.ColumnReportsStructureDamageDamage, nullable: false),
                    DamageOrigin = table.Column<string>(name: DatabaseInterface.ColumnReportsStructureDamageDamageOrigin, type: "enum('Unknown','Mega_Zombie_Boulder','Vehicle_Bumper','Horde_Beacon_Self_Destruct','Trap_Wear_And_Tear','Carepackage_Timeout','Plant_Harvested','Charge_Self_Destruct','Zombie_Swipe','Grenade_Explosion','Rocket_Explosion','Food_Explosion','Vehicle_Explosion','Charge_Explosion','Trap_Explosion','Bullet_Explosion','Radioactive_Zombie_Explosion','Flamable_Zombie_Explosion','Zombie_Electric_Shock','Zombie_Stomp','Zombie_Fire_Breath','Sentry','Useable_Gun','Useable_Melee','Punch','Animal_Attack','Kill_Volume','Vehicle_Collision_Self_Damage','Lightning','VehicleDecay')", nullable: false),
                    InstanceId = table.Column<uint>(name: DatabaseInterface.ColumnReportsStructureDamageInstanceId, nullable: false),
                    WasDestroyed = table.Column<bool>(name: DatabaseInterface.ColumnReportsStructureDamageWasDestroyed, nullable: false, defaultValue: false),
                    Timestamp = table.Column<DateTime>(name: DatabaseInterface.ColumnReportsStructureDamageTimestamp, type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_moderation_report_struct_dmg_records_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_report_struct_dmg_records_Entry",
                table: DatabaseInterface.TableReportStructureDamageRecords,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableReportTeamkillRecords,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false),
                    Teamkill = table.Column<int>(name: DatabaseInterface.ColumnReportsTeamkillRecordTeamkill, nullable: true, defaultValueSql: "NULL"),
                    Victim = table.Column<ulong>(name: DatabaseInterface.ColumnReportsTeamkillRecordVictim, nullable: false),
                    DeathCause = table.Column<string>(name: DatabaseInterface.ColumnReportsTeamkillRecordDeathCause, type: "enum('BLEEDING','BONES','FREEZING','BURNING','FOOD','WATER','GUN','MELEE','ZOMBIE','ANIMAL','SUICIDE','KILL','INFECTION','PUNCH','BREATH','ROADKILL','VEHICLE','GRENADE','SHRED','LANDMINE','ARENA','MISSILE','CHARGE','SPLASH','SENTRY','ACID','BOULDER','BURNER','SPIT','SPARK')", nullable: false),
                    WasIntentional = table.Column<bool>(name: DatabaseInterface.ColumnReportsTeamkillRecordWasIntentional, nullable: true, defaultValueSql: "NULL"),
                    Message = table.Column<string>(name: DatabaseInterface.ColumnReportsTeamkillRecordMessage, maxLength: 255, nullable: true, defaultValueSql: "NULL"),
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_moderation_report_tk_records_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);

                    table.ForeignKey(
                        name: "FK_moderation_report_tk_records_moderation_entries_Teamkill",
                        column: x => x.Teamkill,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_report_tk_records_Entry",
                table: DatabaseInterface.TableReportTeamkillRecords,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateIndex(
                name: "IX_moderation_report_tk_records_Teamkill",
                table: DatabaseInterface.TableReportTeamkillRecords,
                column: DatabaseInterface.ColumnReportsTeamkillRecordTeamkill
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableReportVehicleTeamkillRecords,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false),
                    Teamkill = table.Column<int>(name: DatabaseInterface.ColumnReportsVehicleTeamkillRecordTeamkill, nullable: true, defaultValueSql: "NULL"),
                    Victim = table.Column<ulong>(name: DatabaseInterface.ColumnReportsVehicleTeamkillRecordVictim, nullable: false),
                    DamageOrigin = table.Column<string>(name: DatabaseInterface.ColumnReportsVehicleTeamkillRecordDamageOrigin, type: "enum('Unknown','Mega_Zombie_Boulder','Vehicle_Bumper','Horde_Beacon_Self_Destruct','Trap_Wear_And_Tear','Carepackage_Timeout','Plant_Harvested','Charge_Self_Destruct','Zombie_Swipe','Grenade_Explosion','Rocket_Explosion','Food_Explosion','Vehicle_Explosion','Charge_Explosion','Trap_Explosion','Bullet_Explosion','Radioactive_Zombie_Explosion','Flamable_Zombie_Explosion','Zombie_Electric_Shock','Zombie_Stomp','Zombie_Fire_Breath','Sentry','Useable_Gun','Useable_Melee','Punch','Animal_Attack','Kill_Volume','Vehicle_Collision_Self_Damage','Lightning','VehicleDecay')", nullable: false),
                    Message = table.Column<string>(name: DatabaseInterface.ColumnReportsVehicleTeamkillRecordMessage, maxLength: 255, nullable: true, defaultValueSql: "NULL"),
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_moderation_report_veh_tk_records_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);

                    table.ForeignKey(
                        name: "FK_moderation_report_veh_tk_records_moderation_entries_Teamkill",
                        column: x => x.Teamkill,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_report_veh_tk_records_Entry",
                table: DatabaseInterface.TableReportVehicleTeamkillRecords,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateIndex(
                name: "IX_moderation_report_veh_tk_records_Teamkill",
                table: DatabaseInterface.TableReportVehicleTeamkillRecords,
                column: DatabaseInterface.ColumnReportsVehicleTeamkillRecordTeamkill
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableReportVehicleRequestRecords,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false),
                    Vehicle = table.Column<string>(name: DatabaseInterface.ColumnReportsVehicleRequestRecordVehicle, type: "char(32)", nullable: false),
                    VehicleName = table.Column<string>(name: DatabaseInterface.ColumnReportsVehicleRequestRecordVehicleName, maxLength: 48, nullable: false),
                    DamageOrigin = table.Column<string>(name: DatabaseInterface.ColumnReportsVehicleRequestRecordDamageOrigin, type: "enum('Unknown','Mega_Zombie_Boulder','Vehicle_Bumper','Horde_Beacon_Self_Destruct','Trap_Wear_And_Tear','Carepackage_Timeout','Plant_Harvested','Charge_Self_Destruct','Zombie_Swipe','Grenade_Explosion','Rocket_Explosion','Food_Explosion','Vehicle_Explosion','Charge_Explosion','Trap_Explosion','Bullet_Explosion','Radioactive_Zombie_Explosion','Flamable_Zombie_Explosion','Zombie_Electric_Shock','Zombie_Stomp','Zombie_Fire_Breath','Sentry','Useable_Gun','Useable_Melee','Punch','Animal_Attack','Kill_Volume','Vehicle_Collision_Self_Damage','Lightning','VehicleDecay')", nullable: false),
                    DamageInstigator = table.Column<ulong>(name: DatabaseInterface.ColumnReportsVehicleRequestRecordInstigator, nullable: false),
                    RequestTimeUTC = table.Column<DateTime>(name: DatabaseInterface.ColumnReportsVehicleRequestRecordRequestTimestamp, type: "datetime", nullable: false),
                    DestroyTimeUTC = table.Column<DateTime>(name: DatabaseInterface.ColumnReportsVehicleRequestRecordDestroyTimestamp, type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_moderation_report_veh_req_records_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_report_veh_req_records_Entry",
                table: DatabaseInterface.TableReportVehicleRequestRecords,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableReportShotRecords,
                columns: table => new
                {
                    Entry = table.Column<int>(name: DatabaseInterface.ColumnExternalPrimaryKey, nullable: false),
                    Ammo = table.Column<string>(name: DatabaseInterface.ColumnReportsShotRecordAmmo, type: "char(32)", nullable: false),
                    AmmoName = table.Column<string>(name: DatabaseInterface.ColumnReportsShotRecordAmmoName, maxLength: 48, nullable: false),
                    Item = table.Column<string>(name: DatabaseInterface.ColumnReportsShotRecordItem, type: "char(32)", nullable: false),
                    ItemName = table.Column<string>(name: DatabaseInterface.ColumnReportsShotRecordItemName, maxLength: 48, nullable: false),
                    DamageDone = table.Column<int>(name: DatabaseInterface.ColumnReportsShotRecordDamageDone, nullable: false),
                                                                        // TODO: THIS IS WRONG but can't be changed since its still in a migration, it should be ColumnReportsShotRecordLimb
                    Limb = table.Column<string>(name: DatabaseInterface.ColumnReportsVehicleRequestRecordDamageOrigin, type: "enum('LEFT_FOOT','LEFT_LEG','RIGHT_FOOT','RIGHT_LEG','LEFT_HAND','LEFT_ARM','RIGHT_HAND','RIGHT_ARM','LEFT_BACK','RIGHT_BACK','LEFT_FRONT','RIGHT_FRONT','SPINE','SKULL')", nullable: true, defaultValueSql: "NULL"),
                    IsProjectile = table.Column<bool>(name: DatabaseInterface.ColumnReportsShotRecordIsProjectile, nullable: false),
                    Distance = table.Column<double>(name: DatabaseInterface.ColumnReportsShotRecordDistance, nullable: true, defaultValueSql: "NULL"),
                    HitPointX = table.Column<float>(name: DatabaseInterface.ColumnReportsShotRecordHitPointX, nullable: true, defaultValueSql: "NULL"),
                    HitPointY = table.Column<float>(name: DatabaseInterface.ColumnReportsShotRecordHitPointY, nullable: true, defaultValueSql: "NULL"),
                    HitPointZ = table.Column<float>(name: DatabaseInterface.ColumnReportsShotRecordHitPointZ, nullable: true, defaultValueSql: "NULL"),
                    ShootFromPointX = table.Column<float>(name: DatabaseInterface.ColumnReportsShotRecordShootFromPointX, nullable: false),
                    ShootFromPointY = table.Column<float>(name: DatabaseInterface.ColumnReportsShotRecordShootFromPointY, nullable: false),
                    ShootFromPointZ = table.Column<float>(name: DatabaseInterface.ColumnReportsShotRecordShootFromPointZ, nullable: false),
                    ShootFromRotationX = table.Column<float>(name: DatabaseInterface.ColumnReportsShotRecordShootFromRotationX, nullable: false),
                    ShootFromRotationY = table.Column<float>(name: DatabaseInterface.ColumnReportsShotRecordShootFromRotationY, nullable: false),
                    ShootFromRotationZ = table.Column<float>(name: DatabaseInterface.ColumnReportsShotRecordShootFromRotationZ, nullable: false),
                    HitType = table.Column<string>(name: DatabaseInterface.ColumnReportsShotRecordHitType, type: "enum('NONE','ENTITIY','CRITICAL','BUILD','GHOST')", nullable: true, defaultValueSql: "NULL"),
                    HitActor = table.Column<ulong>(name: DatabaseInterface.ColumnReportsShotRecordHitActor, nullable: true, defaultValueSql: "NULL"),
                    HitAsset = table.Column<string>(name: DatabaseInterface.ColumnReportsShotRecordHitAsset, type: "char(32)", nullable: true, defaultValueSql: "NULL"),
                    HitAssetName = table.Column<string>(name: DatabaseInterface.ColumnReportsShotRecordHitAssetName, maxLength: 48, nullable: true, defaultValueSql: "NULL"),
                    Timestamp = table.Column<DateTime>(name: DatabaseInterface.ColumnReportsShotRecordTimestamp, type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_moderation_report_shot_record_moderation_entries_Entry",
                        column: x => x.Entry,
                        principalTable: DatabaseInterface.TableEntries,
                        principalColumn: DatabaseInterface.ColumnEntriesPrimaryKey,
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_report_shot_record_Entry",
                table: DatabaseInterface.TableReportShotRecords,
                column: DatabaseInterface.ColumnExternalPrimaryKey
            );

            migrationBuilder.CreateTable(
                name: DatabaseInterface.TableIPWhitelists,
                columns: table => new
                {
                    Steam64 = table.Column<ulong>(name: DatabaseInterface.ColumnIPWhitelistsSteam64, nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Admin = table.Column<ulong>(name: DatabaseInterface.ColumnIPWhitelistsAdmin, nullable: false),
                    IPRange = table.Column<string>(name: DatabaseInterface.ColumnIPWhitelistsIPRange, maxLength: 18, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ip_whitelists_Steam64", x => x.Steam64);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: DatabaseInterface.TableIPWhitelists);
            
            migrationBuilder.DropTable(
                name: DatabaseInterface.TableReportShotRecords);
            
            migrationBuilder.DropTable(
                name: DatabaseInterface.TableReportVehicleRequestRecords);
            
            migrationBuilder.DropTable(
                name: DatabaseInterface.TableReportVehicleTeamkillRecords);
            
            migrationBuilder.DropTable(
                name: DatabaseInterface.TableReportTeamkillRecords);
            
            migrationBuilder.DropTable(
                name: DatabaseInterface.TableReportStructureDamageRecords);
            
            migrationBuilder.DropTable(
                name: DatabaseInterface.TableReportChatRecords);
            
            migrationBuilder.DropTable(
                name: DatabaseInterface.TableReports);
            
            migrationBuilder.DropTable(
                name: DatabaseInterface.TableAppealResponses);
            
            migrationBuilder.DropTable(
                name: DatabaseInterface.TableAppealPunishments);
            
            migrationBuilder.DropTable(
                name: DatabaseInterface.TableAppeals);
            
            migrationBuilder.DropTable(
                name: DatabaseInterface.TableVehicleTeamkills);
            
            migrationBuilder.DropTable(
                name: DatabaseInterface.TableTeamkills);
            
            migrationBuilder.DropTable(
                name: DatabaseInterface.TableBugReportAccepteds);
            
            migrationBuilder.DropTable(
                name: DatabaseInterface.TablePlayerReportAccepteds);
            
            migrationBuilder.DropTable(
                name: DatabaseInterface.TableWarnings);
            
            migrationBuilder.DropTable(
                name: DatabaseInterface.TableMutes);
            
            migrationBuilder.DropTable(
                name: DatabaseInterface.TableLinkedReports);
            
            migrationBuilder.DropTable(
                name: DatabaseInterface.TableLinkedAppeals);
            
            migrationBuilder.DropTable(
                name: DatabaseInterface.TableDurationPunishments);
            
            migrationBuilder.DropTable(
                name: DatabaseInterface.TablePunishments);
            
            migrationBuilder.DropTable(
                name: DatabaseInterface.TableAssetBanTypeFilters);

            migrationBuilder.DropTable(
                name: DatabaseInterface.TableEvidence);
            
            migrationBuilder.DropTable(
                name: DatabaseInterface.TableActors);

            migrationBuilder.DropTable(
                name: DatabaseInterface.TableRelatedEntries);

            migrationBuilder.DropTable(
                name: DatabaseInterface.TableEntries);
        }
    }
}

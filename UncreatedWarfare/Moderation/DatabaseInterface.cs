using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Networking;
using Uncreated.Players;
using Uncreated.SQL;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Moderation.Appeals;
using Uncreated.Warfare.Moderation.Commendation;
using Uncreated.Warfare.Moderation.Punishments;
using Uncreated.Warfare.Moderation.Reports;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Moderation;
public abstract class DatabaseInterface
{
    public static readonly TimeSpan DefaultInvalidateDuration = TimeSpan.FromSeconds(3);
    public ModerationCache Cache { get; } = new ModerationCache(64);
    public abstract IWarfareSql Sql { get; }
    public Task VerifyTables() => Sql.VerifyTables(Schema);
    public async Task<PlayerNames> GetUsernames(ulong id, CancellationToken token = default)
    {
        if (UCWarfare.IsLoaded)
            return await F.GetPlayerOriginalNamesAsync(id, token).ConfigureAwait(false);

        return await Sql.GetUsernamesAsync(id, token).ConfigureAwait(false);
    }
    public async Task<T?> ReadOne<T>(PrimaryKey id, CancellationToken token = default) where T : ModerationEntry
    {
        string query = $"SELECT {SqlTypes.ColumnList(ColumnEntriesType, ColumnEntriesSteam64, ColumnEntriesMessage,
            ColumnEntriesIsLegacy, ColumnEntriesStartTimestamp, ColumnEntriesResolvedTimestamp, ColumnEntriesReputation,
            ColumnEntriesReputationApplied, ColumnEntriesLegacyId,
            ColumnEntriesRelavantLogsStartTimestamp, ColumnEntriesRelavantLogsEndTimestamp)} " +
                       $"FROM `{TableEntries}` WHERE `{ColumnEntriesPrimaryKey}`=@0 LIMIT 1;";
        object[] pkArgs = { id.Key };
        ModerationEntry? entry = null;
        await Sql.QueryAsync(query, pkArgs, reader =>
        {
            entry = ReadEntry(reader, 0);
            if (entry != null)
                entry.Id = id;
            return true;
        }, token).ConfigureAwait(false);

        if (entry == null)
        {
            Cache.Remove(id.Key);
            return null;
        }

        await Fill(new ModerationEntry[] { entry }, token).ConfigureAwait(false);
        
        return entry as T;
    }
    public async Task<T[]> ReadAll<T>(ulong actor, ActorRelationType type, DateTimeOffset? start = null, DateTimeOffset? end = null, CancellationToken token = default) where T : ModerationEntry
    {
        string query = $"SELECT {SqlTypes.ColumnList(ColumnEntriesPrimaryKey, ColumnEntriesType, ColumnEntriesSteam64, ColumnEntriesMessage,
            ColumnEntriesIsLegacy, ColumnEntriesStartTimestamp, ColumnEntriesResolvedTimestamp, ColumnEntriesReputation,
            ColumnEntriesReputationApplied, ColumnEntriesLegacyId,
            ColumnEntriesRelavantLogsStartTimestamp, ColumnEntriesRelavantLogsEndTimestamp)} " +
                       $"FROM `{TableEntries}` WHERE";


        switch (type)
        {
            default:
            case ActorRelationType.IsTarget:
                query += $" `{ColumnEntriesSteam64}`=@0;";
                break;
            case ActorRelationType.IsActor:
                query += $" (SELECT COUNT(*) FROM `{TableActors}` AS `a` WHERE `a`.`{ColumnExternalPrimaryKey}` = `{ColumnEntriesPrimaryKey}` AND `a`.`{ColumnActorsId}`=@0) > 0";
                break;
            case ActorRelationType.IsAdminActor:
                query += $" (SELECT COUNT(*) FROM `{TableActors}` AS `a` WHERE `a`.`{ColumnExternalPrimaryKey}` = `{ColumnEntriesPrimaryKey}` AND `a`.`{ColumnActorsId}`=@0 AND `a`.`{ColumnActorsAsAdmin}` != 0) > 0";
                break;
            case ActorRelationType.IsNonAdminActor:
                query += $" (SELECT COUNT(*) FROM `{TableActors}` AS `a` WHERE `a`.`{ColumnExternalPrimaryKey}` = `{ColumnEntriesPrimaryKey}` AND `a`.`{ColumnActorsId}`=@0 AND `a`.`{ColumnActorsAsAdmin}` == 0) > 0";
                break;
        }
        
        ModerationEntryType[]? types = null;
        if (typeof(T) != typeof(ModerationEntry))
            ModerationReflection.TypeInheritance.TryGetValue(typeof(T), out types);
        object[] actorArgs;
        int offset;
        if (start.HasValue && end.HasValue)
        {
            offset = 3;
            actorArgs = new object[3 + (types == null ? 0 : types.Length)];
            actorArgs[1] = start.Value.UtcDateTime;
            actorArgs[2] = end.Value.UtcDateTime;
            query += $" AND `{ColumnEntriesStartTimestamp}` >= @1 AND `{ColumnEntriesStartTimestamp}` <= @2";
        }
        else if (start.HasValue)
        {
            offset = 2;
            actorArgs = new object[2 + (types == null ? 0 : types.Length)];
            actorArgs[1] = start.Value.UtcDateTime;
            query += $" AND `{ColumnEntriesStartTimestamp}` >= @1";
        }
        else if (end.HasValue)
        {
            offset = 2;
            actorArgs = new object[2 + (types == null ? 0 : types.Length)];
            actorArgs[1] = end.Value.UtcDateTime;
            query += $" AND `{ColumnEntriesStartTimestamp}` <= @1";
        }
        else
        {
            offset = 1;
            actorArgs = new object[1 + (types == null ? 0 : types.Length)];
        }

        actorArgs[0] = actor;
        if (types is { Length: > 0 })
        {
            for (int i = 0; i < types.Length; ++i)
                actorArgs[offset + i] = types[i].ToString();
            query += $" AND `{ColumnEntriesType} IN (";
            if (types.Length == 1)
            {
                query += "@" + offset.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                StringBuilder sb = new StringBuilder(types.Length * 12);
                for (int i = 0; i < types.Length; ++i)
                {
                    if (i != 0)
                        sb.Append(',');
                    sb.Append('@').Append((i + offset).ToString(CultureInfo.InvariantCulture));
                }
            }

            query += ");";
        }
        else query += ";";


        List<T> entries = new List<T>(4);
        await Sql.QueryAsync(query, actorArgs, reader =>
        {
            ModerationEntry? entry = ReadEntry(reader, 1);
            if (entry is T tsEntry)
            {
                tsEntry.Id = reader.GetInt32(0);
                entries.Add(tsEntry);
            }
        }, token).ConfigureAwait(false);

        T[] rtn = entries.ToArrayFast();

        // ReSharper disable once CoVariantArrayConversion
        await Fill(rtn, token).ConfigureAwait(false);
        
        return rtn;
    }
    public async Task<T[]> ReadAll<T>(DateTimeOffset? start = null, DateTimeOffset? end = null, CancellationToken token = default) where T : ModerationEntry
    {
        string query = $"SELECT {SqlTypes.ColumnList(ColumnEntriesPrimaryKey, ColumnEntriesType, ColumnEntriesSteam64, ColumnEntriesMessage,
            ColumnEntriesIsLegacy, ColumnEntriesStartTimestamp, ColumnEntriesResolvedTimestamp, ColumnEntriesReputation,
            ColumnEntriesReputationApplied, ColumnEntriesLegacyId,
            ColumnEntriesRelavantLogsStartTimestamp, ColumnEntriesRelavantLogsEndTimestamp)} " +
                       $"FROM `{TableEntries}` WHERE";

        ModerationEntryType[]? types = null;
        if (typeof(T) != typeof(ModerationEntry))
            ModerationReflection.TypeInheritance.TryGetValue(typeof(T), out types);

        object[] args;
        int offset;
        if (start.HasValue && end.HasValue)
        {
            offset = 2;
            args = new object[2 + (types == null ? 0 : types.Length)];
            args[0] = start.Value.UtcDateTime;
            args[1] = end.Value.UtcDateTime;
            query += $" AND `{ColumnEntriesStartTimestamp}` >= @1 AND `{ColumnEntriesStartTimestamp}` <= @2";
        }
        else if (start.HasValue)
        {
            offset = 1;
            args = new object[1 + (types == null ? 0 : types.Length)];
            args[0] = start.Value.UtcDateTime;
            query += $" AND `{ColumnEntriesStartTimestamp}` >= @1";
        }
        else if (end.HasValue)
        {
            offset = 1;
            args = new object[1 + (types == null ? 0 : types.Length)];
            args[0] = end.Value.UtcDateTime;
            query += $" AND `{ColumnEntriesStartTimestamp}` <= @1";
        }
        else
        {
            offset = 0;
            args = new object[types == null ? 0 : types.Length];
        }
        
        if (types is { Length: > 0 })
        {
            for (int i = 0; i < types.Length; ++i)
                args[offset + i] = types[i].ToString();
            query += $" AND `{ColumnEntriesType} IN (";
            if (types.Length == 1)
            {
                query += "@" + offset.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                StringBuilder sb = new StringBuilder(types.Length * 12);
                for (int i = 0; i < types.Length; ++i)
                {
                    if (i != 0)
                        sb.Append(',');
                    sb.Append('@').Append((i + offset).ToString(CultureInfo.InvariantCulture));
                }
            }

            query += ");";
        }
        else query += ";";


        List<T> entries = new List<T>(4);
        await Sql.QueryAsync(query, args, reader =>
        {
            ModerationEntry? entry = ReadEntry(reader, 1);
            if (entry is T tsEntry)
            {
                tsEntry.Id = reader.GetInt32(0);
                entries.Add(tsEntry);
            }
        }, token).ConfigureAwait(false);

        T[] rtn = entries.ToArrayFast();

        // ReSharper disable once CoVariantArrayConversion
        await Fill(rtn, token).ConfigureAwait(false);
        
        return rtn;
    }
    private async Task Fill(ModerationEntry[] entries, CancellationToken token = default)
    {
        if (entries.Length == 0) return;
        string inArg;
        if (entries.Length == 1)
            inArg = " IN (" + entries[0].Id.Key.ToString(CultureInfo.InvariantCulture) + ")";
        else
        {
            StringBuilder sb = new StringBuilder("IN (", entries.Length * 4 + 6);
            for (int i = 0; i < entries.Length; ++i)
            {
                if (i != 0)
                    sb.Append(',');
                sb.Append(entries[i].Id.Key.ToString(CultureInfo.InvariantCulture));
            }

            inArg = sb.Append(')').ToString();
        }

        bool anyPunishments = false;
        bool anyDurationPunishments = false;
        for (int i = 0; i < entries.Length; ++i)
        {
            if (entries[i] is Punishment p)
            {
                anyPunishments = true;
                if (p is DurationPunishment)
                {
                    anyDurationPunishments = true;
                    break;
                }
            }
        }
        string query = $"SELECT {SqlTypes.ColumnList(ColumnExternalPrimaryKey, ColumnActorsId, ColumnActorsRole, ColumnActorsAsAdmin, ColumnActorsIndex)} " +
                $"FROM `{TableActors}` WHERE `{ColumnExternalPrimaryKey}` {inArg};";

        List<(int, List<RelatedActor>)> actors = new List<(int, List<RelatedActor>)>();
        await Sql.QueryAsync(query, null, reader =>
        {
            int pk = reader.GetInt32(0);
            int index = reader.GetInt32(4);
            int arrIndex = actors.FindIndex(x => x.Item1 == pk);
            RelatedActor actor = ReadActor(reader, 1);
            List<RelatedActor> list;
            if (arrIndex == -1)
                actors.Add((pk, list = new List<RelatedActor>()));
            else list = actors[arrIndex].Item2;

            if (actors.Count <= index)
                list.Add(actor);
            else
                list.Insert(index, actor);
        }, token).ConfigureAwait(false);

        for (int i = 0; i < actors.Count; ++i)
        {
            int pk = actors[i].Item1;
            for (int j = 0; j < entries.Length; ++j)
            {
                if (entries[j].Id.Key == pk)
                {
                    entries[j].Actors = actors[i].Item2.ToArrayFast();
                    break;
                }
            }
        }

        query = $"SELECT {SqlTypes.ColumnList(ColumnExternalPrimaryKey, ColumnEvidenceLink, ColumnEvidenceMessage, ColumnEvidenceIsImage, ColumnEvidenceTimestamp, ColumnEvidenceActorId)} " +
                $"FROM `{TableEvidence}` WHERE `{ColumnExternalPrimaryKey}` {inArg};";

        List<(int, List<Evidence>)> evidence = new List<(int, List<Evidence>)>();
        await Sql.QueryAsync(query, null, reader =>
        {
            int pk = reader.GetInt32(0);
            int arrIndex = evidence.FindIndex(x => x.Item1 == pk);
            Evidence piece = ReadEvidence(reader, 1);
            List<Evidence> list;
            if (arrIndex == -1)
                evidence.Add((pk, list = new List<Evidence>()));
            else list = evidence[arrIndex].Item2;
            list.Add(piece);
        }, token).ConfigureAwait(false);

        for (int i = 0; i < evidence.Count; ++i)
        {
            int pk = evidence[i].Item1;
            for (int j = 0; j < entries.Length; ++j)
            {
                if (entries[j].Id.Key == pk)
                {
                    entries[j].Evidence = evidence[i].Item2.ToArrayFast();
                    break;
                }
            }
        }

        if (anyDurationPunishments)
        {
            query = $"SELECT {SqlTypes.ColumnList(ColumnExternalPrimaryKey, ColumnDuationsDurationSeconds)} FROM `{TableDurationPunishments}` WHERE `{ColumnExternalPrimaryKey}` {inArg};";
            await Sql.QueryAsync(query, null, reader =>
            {
                int pk = reader.GetInt32(0);
                long sec = reader.GetInt64(1);
                if (sec < 0)
                    sec = -1;
                for (int i = 0; i < entries.Length; ++i)
                {
                    if (entries[i].Id.Key == pk && entries[i] is DurationPunishment duration)
                    {
                        duration.Duration = TimeSpan.FromSeconds(sec);
                        break;
                    }
                }
            }, token).ConfigureAwait(false);
        }

        if (anyPunishments)
        {
            List<(int, List<PrimaryKey>)> links = new List<(int, List<PrimaryKey>)>();

            query = $"SELECT {SqlTypes.ColumnList(ColumnExternalPrimaryKey, ColumnLinkedAppealsAppeal)} FROM `{TableLinkedAppeals}` WHERE `{ColumnExternalPrimaryKey}` {inArg};";
            await Sql.QueryAsync(query, null, reader =>
            {
                int pk = reader.GetInt32(0);
                int arrIndex = links.FindIndex(x => x.Item1 == pk);
                PrimaryKey val = reader.GetInt32(1);
                List<PrimaryKey> list;
                if (arrIndex == -1)
                    links.Add((pk, list = new List<PrimaryKey>()));
                else list = links[arrIndex].Item2;
                list.Add(val);
            }, token).ConfigureAwait(false);
            for (int i = 0; i < links.Count; ++i)
            {
                int pk = links[i].Item1;
                List<PrimaryKey> vals = links[i].Item2;
                for (int j = 0; j < entries.Length; ++j)
                {
                    if (entries[j].Id.Key == pk && entries[j] is Punishment p)
                    {
                        p.AppealKeys = vals.ToArrayFast();
                        break;
                    }
                }
                vals.Clear();
            }

            query = $"SELECT {SqlTypes.ColumnList(ColumnExternalPrimaryKey, ColumnLinkedReportsReport)} FROM `{TableLinkedReports}` WHERE `{ColumnExternalPrimaryKey}` {inArg};";
            await Sql.QueryAsync(query, null, reader =>
            {
                int pk = reader.GetInt32(0);
                int arrIndex = links.FindIndex(x => x.Item1 == pk);
                PrimaryKey val = reader.GetInt32(1);
                List<PrimaryKey> list;
                if (arrIndex == -1)
                    links.Add((pk, list = new List<PrimaryKey>()));
                else list = links[arrIndex].Item2;
                list.Add(val);
            }, token).ConfigureAwait(false);
            for (int i = 0; i < links.Count; ++i)
            {
                int pk = links[i].Item1;
                List<PrimaryKey> vals = links[i].Item2;
                for (int j = 0; j < entries.Length; ++j)
                {
                    if (entries[j].Id.Key == pk && entries[j] is Punishment p)
                    {
                        p.ReportKeys = vals.ToArrayFast();
                        break;
                    }
                }
            }
        }

        for (int i = 0; i < entries.Length; ++i)
        {
            Cache.AddOrUpdate(entries[i]);
            await entries[i].FillDetail(this).ConfigureAwait(false);
        }
    }
    private static ModerationEntry? ReadEntry(MySqlDataReader reader, int offset)
    {
        ModerationEntryType? type = reader.ReadStringEnum<ModerationEntryType>(offset);
        Type? csType = type.HasValue ? ModerationReflection.GetType(type.Value) : null;
        if (csType == null)
        {
            Logging.LogWarning($"Invalid type while reading moderation entry: {reader.GetString(offset)}.");
            return null;
        }

        ModerationEntry entry = (ModerationEntry)Activator.CreateInstance(csType);
        entry.Player = reader.GetUInt64(1 + offset);
        entry.Message = reader.IsDBNull(2 + offset) ? null : reader.GetString(2 + offset);
        entry.IsLegacy = reader.GetBoolean(3 + offset);
        entry.StartedTimestamp = DateTime.SpecifyKind(reader.GetDateTime(4 + offset), DateTimeKind.Utc);
        entry.ResolvedTimestamp = reader.IsDBNull(5 + offset) ? null : DateTime.SpecifyKind(reader.GetDateTime(5 + offset), DateTimeKind.Utc);
        entry.Reputation = reader.GetDouble(6 + offset);
        entry.ReputationApplied = reader.GetBoolean(7 + offset);
        entry.LegacyId = reader.IsDBNull(8 + offset) ? null : reader.GetUInt32(8 + offset);
        entry.RelevantLogsStart = reader.IsDBNull(9 + offset) ? null : DateTime.SpecifyKind(reader.GetDateTime(9 + offset), DateTimeKind.Utc);
        entry.RelevantLogsEnd = reader.IsDBNull(10 + offset) ? null : DateTime.SpecifyKind(reader.GetDateTime(10 + offset), DateTimeKind.Utc);
        return entry;
    }

    private static Evidence ReadEvidence(MySqlDataReader reader, int offset)
    {
        return new Evidence(
            reader.GetString(offset),
            reader.IsDBNull(1 + offset) ? null : reader.GetString(1 + offset),
            reader.GetBoolean(2 + offset),
            Actors.GetActor(reader.GetUInt64(4 + offset)),
            DateTime.SpecifyKind(reader.GetDateTime(3 + offset), DateTimeKind.Utc));
    }
    private static RelatedActor ReadActor(MySqlDataReader reader, int offset)
    {
        return new RelatedActor(
            reader.GetString(1 + offset),
            reader.GetBoolean(2 + offset),
            Actors.GetActor(reader.GetUInt64(offset)));
    }

    public const string TableEntries = "moderation_entries";
    public const string TableActors = "moderation_actors";
    public const string TableEvidence = "moderation_evidence";
    public const string TableAssetBanFilters = "moderation_asset_ban_filters";
    public const string TableDurationPunishments = "moderation_durations";
    public const string TableLinkedAppeals = "moderation_linked_appeals";
    public const string TableLinkedReports = "moderation_linked_reports";
    public const string TableMutes = "moderation_mutes";
    public const string TableWarnings = "moderation_warnings";
    public const string TablePlayerReportAccepteds = "moderation_accepted_player_reports";
    public const string TableBugReportAccepteds = "moderation_accepted_bug_reports";

    public const string ColumnExternalPrimaryKey = "Entry";
    
    public const string ColumnEntriesPrimaryKey = "Id";
    public const string ColumnEntriesType = "Type";
    public const string ColumnEntriesSteam64 = "Steam64";
    public const string ColumnEntriesMessage = "Message";
    public const string ColumnEntriesIsLegacy = "IsLegacy";
    public const string ColumnEntriesStartTimestamp = "StartTimeUTC";
    public const string ColumnEntriesResolvedTimestamp = "ResolvedTimeUTC";
    public const string ColumnEntriesReputation = "Reputation";
    public const string ColumnEntriesReputationApplied = "ReputationApplied";
    public const string ColumnEntriesLegacyId = "LegacyId";
    public const string ColumnEntriesRelavantLogsStartTimestamp = "RelavantLogsStartTimeUTC";
    public const string ColumnEntriesRelavantLogsEndTimestamp = "RelavantLogsEndTimeUTC";

    public const string ColumnActorsId = "ActorId";
    public const string ColumnActorsRole = "ActorRole";
    public const string ColumnActorsAsAdmin = "ActorAsAdmin";
    public const string ColumnActorsIndex = "ActorIndex";

    public const string ColumnEvidenceLink = "EvidenceURL";
    public const string ColumnEvidenceMessage = "EvidenceMessage";
    public const string ColumnEvidenceIsImage = "EvidenceIsImage";
    public const string ColumnEvidenceActorId = "EvidenceActor";
    public const string ColumnEvidenceTimestamp = "EvidenceTimestampUTC";

    public const string ColumnAssetBanFiltersAsset = "AssetBanAsset";

    public const string ColumnDuationsDurationSeconds = "Duration";

    public const string ColumnLinkedAppealsAppeal = "LinkedAppeal";
    public const string ColumnLinkedReportsReport = "LinkedReport";

    public const string ColumnMutesType = "MuteType";

    public const string ColumnWarningsHasBeenDisplayed = "WarningHasBeenDisplayed";

    public const string ColumnPlayerReportAcceptedsReport = "AcceptedReport";

    public const string ColumnTableBugReportAcceptedsCommit = "AcceptedCommit";

    public static Schema[] Schema =
    {
        new Schema(TableEntries, new Schema.Column[]
        {
            new Schema.Column(ColumnEntriesPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true
            },
            new Schema.Column(ColumnEntriesType, SqlTypes.Enum(exclude: ModerationEntryType.None)),
            new Schema.Column(ColumnEntriesSteam64, SqlTypes.STEAM_64)
            {
                Indexed = true
            },
            new Schema.Column(ColumnEntriesMessage, SqlTypes.String(1024))
            {
                Nullable = true
            },
            new Schema.Column(ColumnEntriesIsLegacy, SqlTypes.BOOLEAN)
            {
                Default = "b'0'"
            },
            new Schema.Column(ColumnEntriesLegacyId, SqlTypes.UINT)
            {
                Nullable = true
            },
            new Schema.Column(ColumnEntriesStartTimestamp, SqlTypes.DATETIME),
            new Schema.Column(ColumnEntriesResolvedTimestamp, SqlTypes.DATETIME)
            {
                Nullable = true
            },
            new Schema.Column(ColumnEntriesReputationApplied, SqlTypes.BOOLEAN)
            {
                Nullable = true,
                Default = "b'0'"
            },
            new Schema.Column(ColumnEntriesReputation, SqlTypes.DOUBLE)
            {
                Default = "'0'"
            },
            new Schema.Column(ColumnEntriesRelavantLogsStartTimestamp, SqlTypes.DATETIME)
            {
                Nullable = true
            },
            new Schema.Column(ColumnEntriesRelavantLogsEndTimestamp, SqlTypes.DATETIME)
            {
                Nullable = true
            },
        }, true, typeof(ModerationEntry)),
        new Schema(TableActors, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnActorsRole, SqlTypes.STRING_255),
            new Schema.Column(ColumnActorsId, SqlTypes.STEAM_64),
            new Schema.Column(ColumnActorsAsAdmin, SqlTypes.BOOLEAN),
            new Schema.Column(ColumnActorsIndex, SqlTypes.INT)
        }, true, typeof(RelatedActor)),
        new Schema(TableEvidence, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnEvidenceLink, SqlTypes.String(512)),
            new Schema.Column(ColumnEvidenceIsImage, SqlTypes.BOOLEAN),
            new Schema.Column(ColumnEvidenceTimestamp, SqlTypes.DATETIME),
            new Schema.Column(ColumnEvidenceActorId, SqlTypes.STEAM_64)
            {
                Nullable = true
            },
            new Schema.Column(ColumnEvidenceMessage, SqlTypes.String(1024))
            {
                Nullable = true
            }

        }, false, typeof(Evidence)),
        new Schema(TableAssetBanFilters, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnAssetBanFiltersAsset, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyTable = VehicleBay.TABLE_MAIN,
                ForeignKeyColumn = VehicleBay.COLUMN_PK
            }
        }, false, typeof(VehicleData)),
        new Schema(TableDurationPunishments, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true,
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnDuationsDurationSeconds, SqlTypes.LONG)
        }, false, typeof(TimeSpan)),
        new Schema(TableLinkedAppeals, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnLinkedAppealsAppeal, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
        }, false, typeof(Appeal)),
        new Schema(TableLinkedReports, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnLinkedReportsReport, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
        }, false, typeof(Report)),
        new Schema(TableMutes, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnMutesType, SqlTypes.Enum(exclude: MuteType.None))
            {
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
        }, false, typeof(Mute)),
        new Schema(TableWarnings, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnWarningsHasBeenDisplayed, SqlTypes.BOOLEAN)
            {
                Default = "b'0'"
            },
        }, false, typeof(Warning)),
        new Schema(TablePlayerReportAccepteds, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true,
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnPlayerReportAcceptedsReport, SqlTypes.INCREMENT_KEY)
            {
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
        }, false, typeof(PlayerReportAccepted)),
        new Schema(TableBugReportAccepteds, new Schema.Column[]
        {
            new Schema.Column(ColumnExternalPrimaryKey, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true,
                ForeignKey = true,
                ForeignKeyColumn = ColumnEntriesPrimaryKey,
                ForeignKeyTable = TableEntries
            },
            new Schema.Column(ColumnTableBugReportAcceptedsCommit, "char(7)")
            {
                Nullable = true
            }
        }, false, typeof(PlayerReportAccepted)),
    };
}

public enum ActorRelationType
{
    IsTarget,
    IsAdminActor,
    IsNonAdminActor,
    IsActor
}

internal class WarfareDatabaseInterface : DatabaseInterface
{
    public override IWarfareSql Sql => Data.AdminSql;
}
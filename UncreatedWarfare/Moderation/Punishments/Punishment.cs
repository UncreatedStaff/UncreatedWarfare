using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Encoding;
using Uncreated.SQL;
using Uncreated.Warfare.Moderation.Appeals;
using Uncreated.Warfare.Moderation.Reports;

namespace Uncreated.Warfare.Moderation.Punishments;

[JsonConverter(typeof(ModerationEntryConverter))]
public abstract class Punishment : ModerationEntry
{
    /// <summary>
    /// Keys for all related appeals.
    /// </summary>
    [JsonPropertyName("appeals")]
    public PrimaryKey[] AppealKeys { get; set; } = Array.Empty<PrimaryKey>();

    /// <summary>
    /// Keys for all related reports.
    /// </summary>
    [JsonPropertyName("reports")]
    public PrimaryKey[] ReportKeys { get; set; } = Array.Empty<PrimaryKey>();

    /// <summary>
    /// All related appeals.
    /// </summary>
    [JsonPropertyName("appeals_detail")]
    public Appeal?[] Appeals { get; set; } = Array.Empty<Appeal>();

    /// <summary>
    /// All related reports.
    /// </summary>
    [JsonPropertyName("reports_detail")]
    public Report?[] Reports { get; set; } = Array.Empty<Report>();

    /// <summary>
    /// Try to find a resolved appeal with a state matching the value for <paramref name="state"/> in <see cref="Appeals"/>.
    /// </summary>
    /// <param name="appeal">The first matching appeal found.</param>
    /// <param name="state">Which state to look for, defaults to accepted.</param>
    /// <returns><see langword="true"/> if an appeal is found.</returns>
    public bool TryFindAppeal(out Appeal appeal, bool state = true)
    {
        for (int i = 0; i < Appeals.Length; ++i)
        {
            Appeal? appeal2 = Appeals[i];
            if (appeal2 is { AppealState: not null } && appeal2.AppealState.Value == state)
            {
                appeal = appeal2;
                return true;
            }
        }

        appeal = null!;
        return false;
    }
    internal override async Task FillDetail(DatabaseInterface db, CancellationToken token = default)
    {
        if (Appeals.Length != AppealKeys.Length)
            Appeals = new Appeal?[AppealKeys.Length];
        if (Reports.Length != ReportKeys.Length)
            Reports = new Report?[ReportKeys.Length];

        await db.ReadAll(Appeals, AppealKeys, true, token).ConfigureAwait(false);
        await db.ReadAll(Reports, ReportKeys, true, token).ConfigureAwait(false);
    }

    protected override void ReadIntl(ByteReader reader, ushort version)
    {
        base.ReadIntl(reader, version);
        
        AppealKeys = new PrimaryKey[reader.ReadInt32()];
        for (int i = 0; i < AppealKeys.Length; ++i)
            AppealKeys[i] = reader.ReadInt32();
        ReportKeys = new PrimaryKey[reader.ReadInt32()];
        for (int i = 0; i < ReportKeys.Length; ++i)
            ReportKeys[i] = reader.ReadInt32();
    }

    protected override void WriteIntl(ByteWriter writer)
    {
        base.WriteIntl(writer);
        
        writer.Write(AppealKeys.Length);
        for (int i = 0; i < AppealKeys.Length; ++i)
            writer.Write(AppealKeys[i].Key);
        writer.Write(ReportKeys.Length);
        for (int i = 0; i < ReportKeys.Length; ++i)
            writer.Write(ReportKeys[i].Key);
    }

    public override void Write(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        base.Write(writer, options);

        if (Appeals.Length > 0 && Appeals.Length == AppealKeys.Length && Appeals.All(x => x != null))
        {
            writer.WritePropertyName("appeals_detail");
            JsonSerializer.Serialize(writer, Appeals, options);
        }
        writer.WritePropertyName("appeals");
        JsonSerializer.Serialize(writer, AppealKeys, options);

        if (Reports.Length > 0 && Reports.Length == ReportKeys.Length && Reports.All(x => x != null))
        {
            writer.WritePropertyName("reports_detail");
            JsonSerializer.Serialize(writer, Reports, options);
        }
        writer.WritePropertyName("reports");
        JsonSerializer.Serialize(writer, ReportKeys, options);
    }

    internal override int EstimateColumnCount() => base.EstimateColumnCount() + AppealKeys.Length + ReportKeys.Length;
    internal override bool AppendWriteCall(StringBuilder builder, List<object> args)
    {
        bool hasEvidenceCalls = base.AppendWriteCall(builder, args);

        builder.Append($"DELETE FROM `{DatabaseInterface.TableLinkedReports}` WHERE `{DatabaseInterface.ColumnExternalPrimaryKey}` = @0;");

        if (ReportKeys.Length > 0)
        {
            builder.Append($" INSERT INTO `{DatabaseInterface.TableLinkedReports}` ({SqlTypes.ColumnList(
                DatabaseInterface.ColumnExternalPrimaryKey, DatabaseInterface.ColumnLinkedReportsReport)}) VALUES ");
            for (int i = 0; i < ReportKeys.Length; ++i)
            {
                F.AppendPropertyList(builder, args.Count, 1, i, 1);
                args.Add(ReportKeys[i].Key);
            }
            builder.Append(';');
        }
        builder.Append($"DELETE FROM `{DatabaseInterface.TableLinkedReports}` WHERE `{DatabaseInterface.ColumnExternalPrimaryKey}` = @0;");

        if (AppealKeys.Length > 0)
        {
            builder.Append($" INSERT INTO `{DatabaseInterface.TableLinkedAppeals}` ({SqlTypes.ColumnList(
                DatabaseInterface.ColumnExternalPrimaryKey, DatabaseInterface.ColumnLinkedAppealsAppeal)}) VALUES ");
            for (int i = 0; i < AppealKeys.Length; ++i)
            {
                F.AppendPropertyList(builder, args.Count, 1, i, 1);
                args.Add(AppealKeys[i].Key);
            }
            builder.Append(';');
        }

        return hasEvidenceCalls;
    }
}

public abstract class DurationPunishment : Punishment
{
    /// <summary>
    /// Length of the punishment, negative implies permanent.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Returns <see langword="true"/> if the punishment will never expire.
    /// </summary>
    /// <remarks>This is indicated by a negative <see cref="Duration"/>.</remarks>
    public bool IsPermanent => Duration.Ticks < 0L;

    /// <summary>
    /// Gets the time at which the punishment expires, assuming it isn't appealed.
    /// </summary>
    /// <exception cref="InvalidOperationException">This punishment hasn't been resolved (<see cref="ModerationEntry.ResolvedTimestamp"/> is <see langword="null"/>).</exception>
    public DateTimeOffset GetExpiryTimestamp()
    {
        if (!ResolvedTimestamp.HasValue)
            throw new InvalidOperationException(GetType().Name + " has not been resolved.");

        return IsPermanent ? DateTimeOffset.MaxValue : ResolvedTimestamp.Value.Add(Duration);
    }

    /// <summary>
    /// Checks if the punishment is still active, assuming it isn't appealed.
    /// </summary>
    /// <exception cref="InvalidOperationException">This punishment hasn't been resolved (<see cref="ModerationEntry.ResolvedTimestamp"/> is <see langword="null"/>).</exception>
    public bool IsApplied()
    {
        if (!ResolvedTimestamp.HasValue)
            throw new InvalidOperationException(GetType().Name + " has not been resolved.");

        return IsPermanent || DateTime.UtcNow > ResolvedTimestamp.Value.UtcDateTime.Add(Duration);
    }

    internal override int EstimateColumnCount() => base.EstimateColumnCount() + 1;
    internal override bool AppendWriteCall(StringBuilder builder, List<object> args)
    {
        bool hasEvidenceCalls = base.AppendWriteCall(builder, args);

        builder.Append($" INSERT INTO `{DatabaseInterface.TableDurationPunishments}` ({SqlTypes.ColumnList(
            DatabaseInterface.ColumnExternalPrimaryKey, DatabaseInterface.ColumnDuationsDurationSeconds)}) VALUES ");

        F.AppendPropertyList(builder, args.Count, 1, 0, 1);
        builder.Append(" AS `t` " +
                       $"ON DUPLICATE KEY UPDATE `{DatabaseInterface.ColumnDuationsDurationSeconds}` = " +
                       $"`t`.`{DatabaseInterface.ColumnDuationsDurationSeconds}`;");

        args.Add((long)Math.Round(Duration.TotalSeconds));

        return hasEvidenceCalls;
    }
}
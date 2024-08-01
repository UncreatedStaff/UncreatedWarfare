using DanielWillett.SpeedBytes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Moderation.Reports;

namespace Uncreated.Warfare.Moderation.Commendation;

[ModerationEntry(ModerationEntryType.PlayerReportAccepted)]
[JsonConverter(typeof(ModerationEntryConverter))]
public class PlayerReportAccepted : ModerationEntry
{
    [JsonPropertyName("report_id")]
    public uint ReportKey { get; set; }
    public Report? Report { get; set; }

    public override string GetDisplayName() => "Player Report Accepted";
    protected override void ReadIntl(ByteReader reader, ushort version)
    {
        base.ReadIntl(reader, version);

        ReportKey = reader.ReadUInt32();
    }
    protected override void WriteIntl(ByteWriter writer)
    {
        base.WriteIntl(writer);

        writer.Write(ReportKey);
    }
    public override void ReadProperty(ref Utf8JsonReader reader, string propertyName, JsonSerializerOptions options)
    {
        if (propertyName.Equals("report_id", StringComparison.InvariantCultureIgnoreCase))
            ReportKey = reader.GetUInt32();
        else
            base.ReadProperty(ref reader, propertyName, options);
    }
    public override void Write(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        base.Write(writer, options);
        writer.WriteNumber("report_id", ReportKey);
    }
    internal override int EstimateParameterCount() => base.EstimateParameterCount() + 1;
    public override async Task AddExtraInfo(DatabaseInterface db, List<string> workingList, IFormatProvider formatter, CancellationToken token = default)
    {
        await base.AddExtraInfo(db, workingList, formatter, token);
        if (ReportKey != 0u)
            workingList.Add($"Report ID: \"{ReportKey.ToString(formatter)}\"");
    }
    internal override async Task FillDetail(DatabaseInterface db, CancellationToken token = default)
    {
        Report = ReportKey != 0u ? await db.ReadOne<Report>(ReportKey, true, true, false, token).ConfigureAwait(false) : null;
        await base.FillDetail(db, token).ConfigureAwait(false);
    }
    internal override bool AppendWriteCall(StringBuilder builder, List<object> args)
    {
        bool hasEvidenceCalls = base.AppendWriteCall(builder, args);

        builder.Append($" INSERT INTO `{DatabaseInterface.TablePlayerReportAccepteds}` ({SqlTypes.ColumnList(
            DatabaseInterface.ColumnExternalPrimaryKey, DatabaseInterface.ColumnPlayerReportAcceptedsReport)}) VALUES " +
                       $"(@0, @{args.Count.ToString(CultureInfo.InvariantCulture)}) AS `t` " +
                       $"ON DUPLICATE KEY UPDATE `{DatabaseInterface.ColumnPlayerReportAcceptedsReport}` = `t`.`{DatabaseInterface.ColumnPlayerReportAcceptedsReport}`;");

        args.Add(ReportKey != 0u ? ReportKey : DBNull.Value);

        return hasEvidenceCalls;
    }
}
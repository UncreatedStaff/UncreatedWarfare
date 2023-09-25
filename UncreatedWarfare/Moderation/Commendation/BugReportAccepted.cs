using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Uncreated.Encoding;
using Uncreated.Framework;
using Uncreated.SQL;

namespace Uncreated.Warfare.Moderation.Commendation;

[ModerationEntry(ModerationEntryType.BugReportAccepted)]
[JsonConverter(typeof(ModerationEntryConverter))]
public class BugReportAccepted : ModerationEntry
{
    [JsonPropertyName("commit")]
    public string? Commit { get; set; }

    [JsonPropertyName("issue")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int? Issue { get; set; }
    public override string GetDisplayName() => "Bug Report Accepted";
    protected override void ReadIntl(ByteReader reader, ushort version)
    {
        base.ReadIntl(reader, version);

        Commit = reader.ReadNullableString();
        Issue = reader.ReadNullableInt32();
    }

    protected override void WriteIntl(ByteWriter writer)
    {
        base.WriteIntl(writer);

        writer.WriteNullable(Commit);
        writer.WriteNullable(Issue);
    }

    public override void ReadProperty(ref Utf8JsonReader reader, string propertyName, JsonSerializerOptions options)
    {
        if (propertyName.Equals("commit", StringComparison.InvariantCultureIgnoreCase))
            Commit = reader.GetString();
        else if (propertyName.Equals("issue", StringComparison.InvariantCultureIgnoreCase))
            Issue = reader.TokenType == JsonTokenType.Null ? null : reader.GetInt32();
        else
            base.ReadProperty(ref reader, propertyName, options);
    }
    public override void Write(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        base.Write(writer, options);
        writer.WriteString("commit", Commit);
        if (Issue.HasValue)
            writer.WriteNumber("issue", Issue.Value);
        else
            writer.WriteNull("issue");
    }

    internal override int EstimateParameterCount() => base.EstimateParameterCount() + 2;
    public override async Task AddExtraInfo(DatabaseInterface db, List<string> workingList, IFormatProvider formatter, CancellationToken token = default)
    {
        await base.AddExtraInfo(db, workingList, formatter, token);
        if (Commit != null)
            workingList.Add($"Commit ID: \"{Commit}\"");
        if (Issue.HasValue)
            workingList.Add($"Issue ID: {Issue.Value.ToString(formatter)}");
    }

    internal override bool AppendWriteCall(StringBuilder builder, List<object> args)
    {
        bool hasEvidenceCalls = base.AppendWriteCall(builder, args);

        builder.Append($" INSERT INTO `{DatabaseInterface.TableBugReportAccepteds}` ({SqlTypes.ColumnList(
            DatabaseInterface.ColumnExternalPrimaryKey, DatabaseInterface.ColumnTableBugReportAcceptedsCommit, DatabaseInterface.ColumnTableBugReportAcceptedsIssue)}) VALUES " +
                       $"(@0, @{args.Count.ToString(CultureInfo.InvariantCulture)}) AS `t` " +
                       $"ON DUPLICATE KEY UPDATE `{DatabaseInterface.ColumnTableBugReportAcceptedsCommit}` = `t`.`{DatabaseInterface.ColumnTableBugReportAcceptedsCommit}`," +
                       $"`{DatabaseInterface.ColumnTableBugReportAcceptedsIssue}` = `t`.`{DatabaseInterface.ColumnTableBugReportAcceptedsIssue}`;");

        args.Add((object?)Commit.MaxLength(7) ?? DBNull.Value);
        args.Add(Issue.HasValue ? Issue.Value : DBNull.Value);

        return hasEvidenceCalls;
    }
}
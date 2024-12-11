using DanielWillett.SpeedBytes;
using DanielWillett.SpeedBytes.Formatting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Database.Manual;

namespace Uncreated.Warfare.Moderation.Reports;

[ModerationEntry(ModerationEntryType.VoiceChatAbuseReport)]
[JsonConverter(typeof(ModerationEntryConverter))]
public class VoiceChatAbuseReport : Report
{
    [JsonIgnore]
    public byte[]? PreviousVoiceData { get; set; }

    public override bool ShouldScreenshot => false;

    public override string GetDisplayName() => "Voice Chat Abuse Report";

    protected override void ReadIntl(ByteReader reader, ushort version)
    {
        base.ReadIntl(reader, version);
        PreviousVoiceData = reader.ReadBool() ? reader.ReadLongUInt8Array() : null;
    }

    protected override void WriteIntl(ByteWriter writer)
    {
        base.WriteIntl(writer);
        if (PreviousVoiceData != null)
        {
            writer.Write(true);
            writer.WriteLong(PreviousVoiceData);
        }
        else
        {
            writer.Write(false);
        }
    }

    internal override int EstimateParameterCount() => base.EstimateParameterCount() + 1;

    public override async Task AddExtraInfo(DatabaseInterface db, List<string> workingList, IFormatProvider formatter, CancellationToken token = default)
    {
        await base.AddExtraInfo(db, workingList, formatter, token);

        if (PreviousVoiceData is { Length: > 0 })
        {
            workingList.Add($"Voice data: {ByteFormatter.FormatCapacity(PreviousVoiceData.Length)}");
        }
    }

    internal override bool AppendWriteCall(StringBuilder builder, List<object> args)
    {
        bool hasEvidenceCalls = base.AppendWriteCall(builder, args);

        builder.Append($" INSERT INTO `{DatabaseInterface.TableVoiceChatReports}` ({MySqlSnippets.ColumnList(
            DatabaseInterface.ColumnExternalPrimaryKey, DatabaseInterface.ColumnVoiceChatReportsData)}) VALUES " +
                       $"(@0, @{args.Count.ToString(CultureInfo.InvariantCulture)}) AS `t` " +
                       $"ON DUPLICATE KEY UPDATE `{DatabaseInterface.ColumnVoiceChatReportsData}` = `t`.`{DatabaseInterface.ColumnVoiceChatReportsData}`;");

        args.Add((object?)PreviousVoiceData ?? DBNull.Value);

        return hasEvidenceCalls;
    }
}
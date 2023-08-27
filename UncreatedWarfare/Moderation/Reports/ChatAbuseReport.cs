using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Encoding;
using Uncreated.SQL;
using Uncreated.Warfare.Moderation.Appeals;

namespace Uncreated.Warfare.Moderation.Reports;
[ModerationEntry(ModerationEntryType.ChatAbuseReport)]
[JsonConverter(typeof(ModerationEntryConverter))]
public class ChatAbuseReport : Report
{
    [JsonPropertyName("messages")]
    public ReportChatRecord[] Messages { get; set; } = Array.Empty<ReportChatRecord>();
    public override string GetDisplayName() => "Chat Abuse Report";
    protected override void ReadIntl(ByteReader reader, ushort version)
    {
        base.ReadIntl(reader, version);

        Messages = new ReportChatRecord[reader.ReadInt32()];
        for (int i = 0; i < Messages.Length; ++i)
            Messages[i] = new ReportChatRecord(reader);
    }

    protected override void WriteIntl(ByteWriter writer)
    {
        base.WriteIntl(writer);

        writer.Write(Messages.Length);
        for (int i = 0; i < Messages.Length; ++i)
            Messages[i].Write(writer);
    }
    public override void ReadProperty(ref Utf8JsonReader reader, string propertyName, JsonSerializerOptions options)
    {
        if (propertyName.Equals("messages", StringComparison.InvariantCultureIgnoreCase))
            Messages = JsonSerializer.Deserialize<ReportChatRecord[]>(ref reader, options) ?? Array.Empty<ReportChatRecord>();
        else
            base.ReadProperty(ref reader, propertyName, options);
    }
    public override void Write(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        base.Write(writer, options);

        writer.WritePropertyName("messages");
        JsonSerializer.Serialize(writer, Messages, options);
    }

    internal override int EstimateColumnCount() => base.EstimateColumnCount() + Messages.Length * 3;
    internal override bool AppendWriteCall(StringBuilder builder, List<object> args)
    {
        bool hasEvidenceCalls = base.AppendWriteCall(builder, args);

        builder.Append($"DELETE FROM `{DatabaseInterface.TableReportChatRecords}` WHERE `{DatabaseInterface.ColumnExternalPrimaryKey}` = @0;");

        if (Messages.Length > 0)
        {
            builder.Append($" INSERT INTO `{DatabaseInterface.TableReportChatRecords}` ({SqlTypes.ColumnList(
                DatabaseInterface.ColumnExternalPrimaryKey, DatabaseInterface.ColumnReportsChatRecordsTimestamp,
                DatabaseInterface.ColumnReportsChatRecordsCount, DatabaseInterface.ColumnReportsChatRecordsMessage)}) VALUES ");

            for (int i = 0; i < Messages.Length; ++i)
            {
                ref ReportChatRecord record = ref Messages[i];
                F.AppendPropertyList(builder, args.Count, 3, i, 1);
                args.Add(record.Timestamp.UtcDateTime);
                args.Add(record.Count);
                args.Add(record.Message.MaxLength(512) ?? string.Empty);
            }

            builder.Append(';');
        }

        return hasEvidenceCalls;
    }
}

public readonly struct ReportChatRecord
{
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; }
    [JsonPropertyName("count")]
    public int Count { get; }
    [JsonPropertyName("message")]
    public string Message { get; }

    [JsonConstructor]
    public ReportChatRecord(string message, int count, DateTimeOffset timestamp)
    {
        Timestamp = timestamp;
        Count = count;
        Message = message;
    }
    public ReportChatRecord(ByteReader reader)
    {
        Timestamp = reader.ReadDateTimeOffset();
        Count = reader.ReadInt32();
        Message = reader.ReadString();
    }
    public void Write(ByteWriter writer)
    {
        writer.Write(Timestamp);
        writer.Write(Count);
        writer.Write(Message);
    }
}
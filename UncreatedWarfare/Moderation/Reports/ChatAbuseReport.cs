using DanielWillett.SpeedBytes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Database.Manual;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Moderation.Reports;
[ModerationEntry(ModerationEntryType.ChatAbuseReport)]
[JsonConverter(typeof(ModerationEntryConverter))]
public class ChatAbuseReport : Report
{
    [JsonPropertyName("messages")]
    public AbusiveChatRecord[] Messages { get; set; } = Array.Empty<AbusiveChatRecord>();
    public override string GetDisplayName() => "Chat Abuse Report";
    protected override void ReadIntl(ByteReader reader, ushort version)
    {
        base.ReadIntl(reader, version);

        Messages = new AbusiveChatRecord[reader.ReadInt32()];
        for (int i = 0; i < Messages.Length; ++i)
            Messages[i] = new AbusiveChatRecord(reader);
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
            Messages = JsonSerializer.Deserialize<AbusiveChatRecord[]>(ref reader, options) ?? Array.Empty<AbusiveChatRecord>();
        else
            base.ReadProperty(ref reader, propertyName, options);
    }
    public override void Write(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        base.Write(writer, options);

        writer.WritePropertyName("messages");
        JsonSerializer.Serialize(writer, Messages, options);
    }

    internal override int EstimateParameterCount() => base.EstimateParameterCount() + Messages.Length * 3;
    public override async Task AddExtraInfo(DatabaseInterface db, List<string> workingList, IFormatProvider formatter, CancellationToken token = default)
    {
        await base.AddExtraInfo(db, workingList, formatter, token);
        
        workingList.Add($"Recorded Messages: {Messages.Length.ToString(formatter)}");
    }
    internal override bool AppendWriteCall(StringBuilder builder, List<object> args)
    {
        bool hasEvidenceCalls = base.AppendWriteCall(builder, args);

        builder.Append($"DELETE FROM `{DatabaseInterface.TableReportChatRecords}` WHERE `{DatabaseInterface.ColumnExternalPrimaryKey}` = @0;");

        if (Messages.Length > 0)
        {
            builder.Append($" INSERT INTO `{DatabaseInterface.TableReportChatRecords}` ({MySqlSnippets.ColumnList(
                DatabaseInterface.ColumnExternalPrimaryKey, DatabaseInterface.ColumnReportsChatRecordsTimestamp,
                DatabaseInterface.ColumnReportsChatRecordsIndex, DatabaseInterface.ColumnReportsChatRecordsMessage)}) VALUES ");

            for (int i = 0; i < Messages.Length; ++i)
            {
                ref AbusiveChatRecord record = ref Messages[i];
                MySqlSnippets.AppendPropertyList(builder, args.Count, 3, i, 1);
                args.Add(record.Timestamp.UtcDateTime);
                args.Add(i);
                args.Add(record.Message.Truncate(512) ?? string.Empty);
            }

            builder.Append(';');
        }

        return hasEvidenceCalls;
    }
}

public struct AbusiveChatRecord
{
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }
    [JsonPropertyName("message")]
    public string Message { get; set; }

    public AbusiveChatRecord() { }
    public AbusiveChatRecord(string message, DateTimeOffset timestamp)
    {
        Timestamp = timestamp;
        Message = message;
    }
    public AbusiveChatRecord(ByteReader reader)
    {
        Timestamp = reader.ReadDateTimeOffset();
        Message = reader.ReadString();
    }
    public void Write(ByteWriter writer)
    {
        writer.Write(Timestamp);
        writer.Write(Message);
    }
}
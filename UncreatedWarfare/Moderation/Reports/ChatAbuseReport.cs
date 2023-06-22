using System;

namespace Uncreated.Warfare.Moderation.Reports;
[ModerationEntry(ModerationEntryType.ChatAbuseReport)]
public class ChatAbuseReport : Report
{
    public ReportChatRecord[] Messages { get; set; } = Array.Empty<ReportChatRecord>();
}

public readonly struct ReportChatRecord
{
    public DateTimeOffset Timestamp { get; }
    public int Count { get; }
    public string Message { get; }

    public ReportChatRecord(string message, int count, DateTimeOffset timestamp)
    {
        Timestamp = timestamp;
        Count = count;
        Message = message;
    }
}
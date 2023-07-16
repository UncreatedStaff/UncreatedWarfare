using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Encoding;
using Uncreated.SQL;
using Uncreated.Warfare.Moderation.Appeals;

namespace Uncreated.Warfare.Moderation.Commendation;

[ModerationEntry(ModerationEntryType.BugReportAccepted)]
[JsonConverter(typeof(ModerationEntryConverter))]
public class BugReportAccepted : ModerationEntry
{
    [JsonPropertyName("commit")]
    public string? Commit { get; set; }
    public override string GetDisplayName() => "Bug Report Accepted";
    protected override void ReadIntl(ByteReader reader, ushort version)
    {
        base.ReadIntl(reader, version);

        Commit = reader.ReadNullableString();
    }

    protected override void WriteIntl(ByteWriter writer)
    {
        base.WriteIntl(writer);

        writer.WriteNullable(Commit);
    }

    public override void ReadProperty(ref Utf8JsonReader reader, string propertyName, JsonSerializerOptions options)
    {
        if (propertyName.Equals("commit", StringComparison.InvariantCultureIgnoreCase))
            Commit = reader.GetString();
        else
            base.ReadProperty(ref reader, propertyName, options);
    }
    public override void Write(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        base.Write(writer, options);
        writer.WriteString("commit", Commit);
    }
}
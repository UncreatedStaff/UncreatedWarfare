using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Encoding;
using Uncreated.SQL;
using Uncreated.Warfare.Moderation.Appeals;

namespace Uncreated.Warfare.Moderation.Commendation;

[ModerationEntry(ModerationEntryType.PlayerReportAccepted)]
[JsonConverter(typeof(ModerationEntryConverter))]
public class PlayerReportAccepted : ModerationEntry
{
    [JsonPropertyName("report_id")]
    public PrimaryKey Report { get; set; }
    public override string GetDisplayName() => "Player Report Accepted";
    protected override void ReadIntl(ByteReader reader, ushort version)
    {
        base.ReadIntl(reader, version);

        Report = reader.ReadInt32();
    }

    protected override void WriteIntl(ByteWriter writer)
    {
        base.WriteIntl(writer);

        writer.Write(Report.Key);
    }

    public override void ReadProperty(ref Utf8JsonReader reader, string propertyName, JsonSerializerOptions options)
    {
        if (propertyName.Equals("report_id", StringComparison.InvariantCultureIgnoreCase))
            Report = reader.GetInt32();
        else
            base.ReadProperty(ref reader, propertyName, options);
    }
    public override void Write(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        base.Write(writer, options);
        writer.WriteNumber("report_id", Report.Key);
    }
}
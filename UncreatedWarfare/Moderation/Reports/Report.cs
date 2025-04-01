using DanielWillett.SpeedBytes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Configuration.JsonConverters;
using Uncreated.Warfare.Database.Manual;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Moderation.Reports;

[ModerationEntry(ModerationEntryType.Report)]
[JsonConverter(typeof(ModerationEntryConverter))]
public class Report : ModerationEntry
{
    [JsonPropertyName("report_type")]
    [JsonConverter(typeof(ReportTypeLegacyConverter))]
    public ReportType Type { get; set; }

    [JsonPropertyName("screenshot_data")]
    [JsonConverter(typeof(ByteArrayJsonConverter))]
    public byte[]? ScreenshotJpgData { get; set; }

    [JsonIgnore]
    public virtual bool ShouldScreenshot => true;

    public override string GetDisplayName() => "Report";
    protected override void ReadIntl(ByteReader reader, ushort version)
    {
        base.ReadIntl(reader, version);

        Type = (ReportType)reader.ReadUInt16();
        ScreenshotJpgData = reader.ReadBool() ? reader.ReadLongUInt8Array() : null;
    }

    protected override void WriteIntl(ByteWriter writer)
    {
        base.WriteIntl(writer);

        writer.Write((ushort)Type);
        if (ScreenshotJpgData != null)
        {
            writer.Write(true);
            writer.WriteLong(ScreenshotJpgData);
        }
        else writer.Write(false);
    }

    public override bool ReadProperty(ref Utf8JsonReader reader, string propertyName, JsonSerializerOptions options)
    {
        if (propertyName.Equals("report_type", StringComparison.InvariantCultureIgnoreCase))
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                int num = reader.GetInt32();
                if (num >= 0)
                {
                    Type = (ReportType)num;
                    return true;
                }

                throw new JsonException($"Invalid integer for ReportType: {num}.");
            }

            string str = reader.GetString()!;
            if (!Enum.TryParse(str, true, out ReportType type))
            {
                // parse legacy report type
                type = str.ToUpperInvariant() switch
                {
                    "CHAT_ABUSE" or "VOICE_CHAT_ABUSE" => ReportType.ChatAbuse,
                    "GREIFING_FOBS" or "INTENTIONAL_TEAMKILL" or "SOLOING_VEHICLE" or "WASTING_ASSETS" => ReportType.Griefing,
                    "CUSTOM" => ReportType.Custom,
                    "CHEATING" => ReportType.Cheating,
                    _ => throw new JsonException("Invalid string value for ReportType.")
                };
            }
            Type = type;
        }
        else
            return base.ReadProperty(ref reader, propertyName, options);

        return true;
    }
    public override void Write(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        base.Write(writer, options);

        writer.WriteString("report_type", Type.ToString());
    }

    internal override int EstimateParameterCount() => base.EstimateParameterCount() + 2;
    internal override bool AppendWriteCall(StringBuilder builder, List<object> args)
    {
        bool hasEvidenceCalls = base.AppendWriteCall(builder, args);

        builder.Append($" INSERT INTO `{DatabaseInterface.TableReports}` ({MySqlSnippets.ColumnList(
            DatabaseInterface.ColumnExternalPrimaryKey, DatabaseInterface.ColumnReportsType, DatabaseInterface.ColumnReportsScreenshotData)}) VALUES ");

        MySqlSnippets.AppendPropertyList(builder, args.Count, 2, 0, 1);
        builder.Append(" AS `t` " +
                       $"ON DUPLICATE KEY UPDATE `{DatabaseInterface.ColumnReportsType}`=`t`.`{DatabaseInterface.ColumnReportsType}`," +
                       $"`{DatabaseInterface.ColumnReportsScreenshotData}`=`t`.`{DatabaseInterface.ColumnReportsScreenshotData}`;");

        args.Add(Type.ToString());
        args.Add((object?)ScreenshotJpgData ?? DBNull.Value);
        
        return hasEvidenceCalls;
    }
}

[Translatable("Player Report Type")]
public enum ReportType
{
    Custom,
    Griefing,
    ChatAbuse,
    VoiceChatAbuse,
    Cheating
}
public sealed class ReportTypeLegacyConverter : JsonConverter<ReportType>
{
    public override ReportType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            int num = reader.GetInt32();
            return (ReportType)num;
        }
        
        if (reader.TokenType == JsonTokenType.String)
        {
            string str = reader.GetString()!;
            if (Enum.TryParse(str, true, out ReportType reportType))
                return reportType;

            return str.ToUpperInvariant() switch
            {
                "CHAT_ABUSE" => ReportType.ChatAbuse,
                "VOICE_CHAT_ABUSE" => ReportType.VoiceChatAbuse,
                "GREIFING_FOBS" or "INTENTIONAL_TEAMKILL" or "SOLOING_VEHICLE" or "WASTING_ASSETS" => ReportType.Griefing,
                "CUSTOM" => ReportType.Custom,
                "CHEATING" => ReportType.Cheating,
                _ => throw new JsonException("Invalid string value for ReportType.")
            };
        }

        if (reader.TokenType is not JsonTokenType.String and not JsonTokenType.Null)
            throw new JsonException($"Unexpected token type for: ReportType, {reader.TokenType}.");

        return ReportType.Custom;
    }

    public override void Write(Utf8JsonWriter writer, ReportType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
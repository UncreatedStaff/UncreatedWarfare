using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Encoding;
using Uncreated.Framework;
using Uncreated.SQL;

namespace Uncreated.Warfare.Moderation.Reports;

[ModerationEntry(ModerationEntryType.Report)]
[JsonConverter(typeof(ModerationEntryConverter))]
public class Report : ModerationEntry
{
    [JsonPropertyName("report_type")]
    [JsonConverter(typeof(ReportTypeLegacyConverter))]
    public ReportType Type { get; set; }

    [JsonPropertyName("screenshot_data")]
    [JsonConverter(typeof(Base64Converter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public byte[]? ScreenshotJpgData { get; set; }
    public override string GetDisplayName() => "Report";
    protected override void ReadIntl(ByteReader reader, ushort version)
    {
        base.ReadIntl(reader, version);

        Type = (ReportType)reader.ReadUInt16();
        ScreenshotJpgData = reader.ReadBool() ? reader.ReadLongBytes() : null;
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

    public override void ReadProperty(ref Utf8JsonReader reader, string propertyName, JsonSerializerOptions options)
    {
        if (propertyName.Equals("report_type", StringComparison.InvariantCultureIgnoreCase))
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                int num = reader.GetInt32();
                if (num >= 0)
                {
                    Type = (ReportType)num;
                    return;
                }

                throw new JsonException($"Invalid integer for ReportType: {num}.");
            }
            string str = reader.GetString()!;
            if (!Enum.TryParse(str, true, out ReportType type))
            {
                if (Enum.TryParse(str, true, out EReportType legacyReportType))
                {
                    Type = legacyReportType switch
                    {
                        EReportType.CHAT_ABUSE => ReportType.ChatAbuse,
                        EReportType.GREIFING_FOBS or EReportType.INTENTIONAL_TEAMKILL or EReportType.SOLOING_VEHICLE or EReportType.WASTING_ASSETS => ReportType.Greifing,
                        _ => ReportType.Custom
                    };
                    return;
                }

                throw new JsonException("Invalid string value for ReportType.");
            }
            Type = type;
        }
        else if (propertyName.Equals("screenshot_data", StringComparison.InvariantCultureIgnoreCase))
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string b64 = reader.GetString()!;
                ScreenshotJpgData = Convert.FromBase64String(b64);
                return;
            }
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                List<byte> bytes = new List<byte>(short.MaxValue);
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                        break;
                    if (reader.TokenType == JsonTokenType.Number)
                    {
                        if (reader.TryGetByte(out byte b))
                        {
                            bytes.Add(b);
                            continue;
                        }
                    }
                    else if (reader.TokenType == JsonTokenType.String)
                    {
                        string str = reader.GetString()!;
                        if (byte.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out byte b))
                        {
                            bytes.Add(b);
                            continue;
                        }
                    }
                    throw new JsonException("Failed to get byte reading ScreenshotJpgData.");
                }
                ScreenshotJpgData = bytes.ToArray();
                return;
            }
            if (reader.TokenType == JsonTokenType.Null)
                ScreenshotJpgData = null;
            else
                throw new JsonException("Unexpected token " + reader.TokenType + " while reading ScreenshotJpgData.");
        }
        else
            base.ReadProperty(ref reader, propertyName, options);
    }
    public override void Write(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        base.Write(writer, options);

        writer.WriteString("report_type", Type.ToString());

        if (ScreenshotJpgData != null)
            writer.WriteString("screenshot_data", Convert.ToBase64String(ScreenshotJpgData));
    }

    internal override int EstimateColumnCount() => base.EstimateColumnCount() + 2;
    internal override bool AppendWriteCall(StringBuilder builder, List<object> args)
    {
        bool hasEvidenceCalls = base.AppendWriteCall(builder, args);

        builder.Append($" INSERT INTO `{DatabaseInterface.TableReports}` ({SqlTypes.ColumnList(
            DatabaseInterface.ColumnExternalPrimaryKey, DatabaseInterface.ColumnReportsType)}) VALUES ");

        F.AppendPropertyList(builder, args.Count, 1, 0, 1);
        builder.Append(" AS `t` " +
                       $"ON DUPLICATE KEY UPDATE `{DatabaseInterface.ColumnReportsType}` = " +
                       $"`t`.`{DatabaseInterface.ColumnReportsType}`;");

        args.Add(Type.ToString());
        
        return hasEvidenceCalls;
    }
}

public enum ReportType
{
    Custom,
    Greifing,
    ChatAbuse
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
            if (Enum.TryParse(reader.GetString()!, true, out ReportType reportType))
                return reportType;
            if (Enum.TryParse(reader.GetString()!, true, out EReportType reportTypeOld))
                return reportTypeOld switch
                {
                    EReportType.CHAT_ABUSE => ReportType.ChatAbuse,
                    EReportType.GREIFING_FOBS or EReportType.INTENTIONAL_TEAMKILL or EReportType.SOLOING_VEHICLE or EReportType.WASTING_ASSETS => ReportType.Greifing,
                    _ => ReportType.Custom
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
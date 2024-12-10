using DanielWillett.SpeedBytes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Commands;
using Uncreated.Warfare.Database.Manual;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Languages;

namespace Uncreated.Warfare.Moderation.Punishments;
[ModerationEntry(ModerationEntryType.Mute)]
[JsonConverter(typeof(ModerationEntryConverter))]
public class Mute : DurationPunishment
{
    [JsonIgnore]
    public override bool IsAppealable => true;

    /// <summary>
    /// Which areas of communication the mute applies to.
    /// </summary>
    [JsonPropertyName("mute_type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MuteType Type { get; set; }
    public override string GetDisplayName() => Type switch
    {
        MuteType.Text => "Text Chat Mute",
        MuteType.Voice => "Voice Chat Mute",
        MuteType.Both => "Chat Mute",
        _ => "Mute"
    };
    protected override void ReadIntl(ByteReader reader, ushort version)
    {
        base.ReadIntl(reader, version);

        Type = (MuteType)reader.ReadUInt8();
    }

    protected override void WriteIntl(ByteWriter writer)
    {
        base.WriteIntl(writer);

        writer.Write((byte)Type);
    }

    public override void ReadProperty(ref Utf8JsonReader reader, string propertyName, JsonSerializerOptions options)
    {
        if (propertyName.Equals("mute_type", StringComparison.InvariantCultureIgnoreCase))
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                int num = reader.GetInt32();
                if (num <= (int)MuteType.Both && num >= 0)
                {
                    Type = (MuteType)num;
                    return;
                }

                throw new JsonException($"Invalid integer for MuteType: {num}.");
            }
            string str = reader.GetString()!;
            if (!Enum.TryParse(str, true, out MuteType type))
                throw new JsonException("Invalid string value for MuteType.");
            Type = type;
        }
        else
            base.ReadProperty(ref reader, propertyName, options);
    }
    public override void Write(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        base.Write(writer, options);
        writer.WriteString("mute_type", Type.ToString());
    }

    internal override int EstimateParameterCount() => base.EstimateParameterCount() + 1;
    internal override bool AppendWriteCall(StringBuilder builder, List<object> args)
    {
        bool hasEvidenceCalls = base.AppendWriteCall(builder, args);

        builder.Append($" INSERT INTO `{DatabaseInterface.TableMutes}` ({MySqlSnippets.ColumnList(
            DatabaseInterface.ColumnExternalPrimaryKey, DatabaseInterface.ColumnMutesType)}) VALUES " +
            $"(@0, @{args.Count.ToString(CultureInfo.InvariantCulture)}) AS `t` " +
            $"ON DUPLICATE KEY UPDATE `{DatabaseInterface.ColumnMutesType}` = `t`.`{DatabaseInterface.ColumnMutesType}`;");

        args.Add(Type.ToString());

        return hasEvidenceCalls;
    }
}


[Translatable("Mute Severity")]
[Flags]
public enum MuteType : byte
{
    None = 0,

    [Translatable(Languages.ChineseSimplified, "语音交流")]
    [Translatable("Voice Chat")]
    Voice = 1,

    [Translatable(Languages.ChineseSimplified, "文字交流")]
    [Translatable("Text Chat")]
    Text = 2,

    [Translatable(Languages.ChineseSimplified, "语音和文字交流")]
    [Translatable("Voice and Text Chat")]
    Both = Voice | Text
}
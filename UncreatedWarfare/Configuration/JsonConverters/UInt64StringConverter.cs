using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Uncreated.Warfare.Configuration.JsonConverters;
public class UInt64StringConverter : JsonConverter<ulong>
{
    public override ulong Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
            return reader.GetUInt64();

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException($"Unexpected token {reader.TokenType} while reading UInt64.");

        string str = reader.GetString()!;
        return ulong.Parse(str, NumberStyles.Any, CultureInfo.InvariantCulture);
    }
    public override void Write(Utf8JsonWriter writer, ulong value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}

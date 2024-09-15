using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Uncreated.Warfare.Configuration.JsonConverters;

public class ByteArrayJsonConverter : JsonConverter<byte[]>
{
    public override byte[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string b64 = reader.GetString()!;
            return Convert.FromBase64String(b64);
        }
        
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            List<byte> bytes = new List<byte>(16);
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
                throw new JsonException("Failed to get byte reading byte[].");
            }
            return bytes.ToArray();
        }
        
        if (reader.TokenType == JsonTokenType.Null)
            return null!;

        throw new JsonException("Unexpected token " + reader.TokenType + " while reading byte[].");
    }
    public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
    {
        if (value is null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(Convert.ToBase64String(value));
    }
}
public class ByteArraySegmentJsonConverter : JsonConverter<ArraySegment<byte>>
{
    public override ArraySegment<byte> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string b64 = reader.GetString()!;
            return Convert.FromBase64String(b64);
        }
        
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            List<byte> bytes = new List<byte>(16);
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
                throw new JsonException("Failed to get byte reading byte[].");
            }
            return bytes.ToArray();
        }
        
        if (reader.TokenType == JsonTokenType.Null)
            return null!;

        throw new JsonException("Unexpected token " + reader.TokenType + " while reading byte[].");
    }
    public override void Write(Utf8JsonWriter writer, ArraySegment<byte> value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(Convert.ToBase64String(value));
    }
}
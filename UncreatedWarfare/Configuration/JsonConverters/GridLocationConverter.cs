using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Locations;

namespace Uncreated.Warfare.Configuration.JsonConverters;
public class GridLocationConverter : JsonConverter<GridLocation>
{
    public override GridLocation Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return default;

            case JsonTokenType.String:
                string str = reader.GetString()!;
                if (GridLocation.TryParse(str, out GridLocation gridLocation))
                    return gridLocation;

                throw new JsonException("Invalid string representation of 'GridLocation'.");

            case JsonTokenType.StartObject:
                byte? x = null, y = null, index = null;
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    if (reader.TokenType != JsonTokenType.PropertyName)
                        continue;

                    string property = reader.GetString()!;
                    if (!reader.Read())
                        break;
                    if (reader.TokenType != JsonTokenType.Number)
                        continue;

                    if (property.Equals("x", StringComparison.Ordinal))
                    {
                        x = reader.GetByte();
                    }
                    else if (property.Equals("y", StringComparison.Ordinal))
                    {
                        y = reader.GetByte();
                    }
                    else if (property.Equals("index", StringComparison.Ordinal))
                    {
                        index = reader.GetByte();
                    }
                }

                if (x.HasValue && y.HasValue)
                    return new GridLocation(x.Value, y.Value, index.GetValueOrDefault());

                throw new JsonException("Invalid object representation of 'GridLocation'. Expected: { \"x\": number, \"y\": number, \"index\": number }.");
            
            default:
                throw new JsonException($"Unexpected token '{reader.TokenType}' while parsing 'GridLocation'.");
        }
    }
    public override void Write(Utf8JsonWriter writer, GridLocation value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Configuration.JsonConverters;
public sealed class TypeJsonConverter : JsonConverter<Type>
{
    public override Type? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.String => Type.GetType(reader.GetString()!, true, ignoreCase: true),
            _ => throw new JsonException($"Unexpected token: {reader.TokenType} while reading System.Type object.")
        };
    }
    public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
    {
        if (value == null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(ContextualTypeResolver.TypeToKeywordOrAssemblyQualifiedString(value));
    }
}
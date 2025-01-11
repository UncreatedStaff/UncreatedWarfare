using DanielWillett.ReflectionTools;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Uncreated.Warfare.Configuration.JsonConverters;
public class ArrayConverter<TElementType, TConverterType> : JsonConverter<TElementType?[]?> where TConverterType : JsonConverter<TElementType>, new()
{
    private readonly TConverterType _converter = new TConverterType();
    public override TElementType?[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;

            case JsonTokenType.StartArray:
                List<TElementType?> list = new List<TElementType?>();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                        break;

                    list.Add(_converter.Read(ref reader, typeof(TElementType), options));
                }

                return list.ToArray();

            default:
                throw new JsonException($"Unexpected token reading array of {Accessor.ExceptionFormatter.Format(typeof(TElementType))}.");
        }
    }

    public override void Write(Utf8JsonWriter writer, TElementType?[]? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartArray();

        for (int i = 0; i < value.Length; ++i)
        {
            TElementType? element = value[i];
            if (element == null)
                writer.WriteNullValue();
            else
                _converter.Write(writer, element, options);
        }

        writer.WriteEndArray();
    }
}

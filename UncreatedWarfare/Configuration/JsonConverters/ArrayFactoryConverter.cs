using DanielWillett.ReflectionTools;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Uncreated.Warfare.Configuration.JsonConverters;
public class ArrayFactoryConverter<TElementType, TFactoryType> : JsonConverter<TElementType?[]?> where TFactoryType : JsonConverterFactory, new()
{
    private readonly TFactoryType _factory = new TFactoryType();
    public override TElementType?[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;

            case JsonTokenType.StartArray:
                if (_factory.CreateConverter(typeof(TElementType), options) is not JsonConverter<TElementType> converter)
                    throw new JsonException($"Unable to create converter from factory of type {Accessor.ExceptionFormatter.Format(typeof(TFactoryType))}.");

                List<TElementType?> list = new List<TElementType?>();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                        break;

                    list.Add(converter.Read(ref reader, typeof(TElementType), options));
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

        if (_factory.CreateConverter(typeof(TElementType), options) is not JsonConverter<TElementType> converter)
            throw new JsonException($"Unable to create converter from factory of type {Accessor.ExceptionFormatter.Format(typeof(TFactoryType))}.");

        writer.WriteStartArray();

        for (int i = 0; i < value.Length; ++i)
        {
            TElementType? element = value[i];
            if (element == null)
                writer.WriteNullValue();
            else
                converter.Write(writer, element, options);
        }

        writer.WriteEndArray();
    }
}

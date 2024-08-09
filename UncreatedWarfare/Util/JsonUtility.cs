using System;
using System.Text.Json;
using Uncreated.Warfare.Configuration;

namespace Uncreated.Warfare.Util;
public static class JsonUtility
{
    public delegate void ReadTopLevelPropertiesHandler<TState>(ref Utf8JsonReader reader, string propertyName, ref TState state);

    /// <summary>
    /// Find the first, next property matching the given the <paramref name="propertyName"/>.
    /// </summary>
    /// <remarks>After calling this method, you can use .GetValue() on the reader.</remarks>
    /// <returns><see langword="true"/> if the value was found, otherwise <see langword="false"/>.</returns>
    public static bool SkipToProperty(ref Utf8JsonReader reader, string propertyName, bool ignoreCase = true)
    {
        int objectLevel = 0;
        int arrayLevel = 0;
        bool hasHitOneProperty = false;

        // read all tokens in json file, looping for each token
        // token is section of json, like '{', '"property"', '"value"', '[', etc
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName && objectLevel <= 0 && arrayLevel <= 0)
            {
                hasHitOneProperty = true;

                string property = reader.GetString()!;
                if (propertyName.Equals(property, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                {
                    return reader.Read();
                }
            }

            if (!hasHitOneProperty)
                continue;

            switch (reader.TokenType)
            {
                case JsonTokenType.StartObject:
                    ++objectLevel;
                    break;

                case JsonTokenType.StartArray:
                    ++arrayLevel;
                    break;

                case JsonTokenType.EndObject:
                    --objectLevel;
                    break;

                case JsonTokenType.EndArray:
                    --arrayLevel;
                    break;
            }
        }

        return false;
    }

    /// <summary>
    /// Find all top-level properties on the current object.
    /// </summary>
    public static int ReadTopLevelProperties(ref Utf8JsonReader reader, ReadTopLevelPropertiesHandler<object?> action)
    {
        object? state = null;
        return ReadTopLevelProperties(ref reader, ref state, action);
    }

    /// <summary>
    /// Find all top-level properties on the current object.
    /// </summary>
    public static int ReadTopLevelProperties<TState>(ref Utf8JsonReader reader, ref TState state, ReadTopLevelPropertiesHandler<TState> action)
    {
        int objectLevel = 0;
        int arrayLevel = 0;
        int propCount = 0;

        if (reader.TokenType != JsonTokenType.PropertyName && !reader.Read())
            return 0;

        do
        {
            if (reader.TokenType == JsonTokenType.PropertyName && objectLevel <= 0 && arrayLevel <= 0)
            {
                string property = reader.GetString()!;
                if (!reader.Read())
                    return propCount;

                ++propCount;
                action(ref reader, property, ref state);
            }

            if (propCount == 0)
                continue;

            switch (reader.TokenType)
            {
                case JsonTokenType.StartObject:
                    ++objectLevel;
                    break;

                case JsonTokenType.StartArray:
                    ++arrayLevel;
                    break;

                case JsonTokenType.EndObject:
                    --objectLevel;
                    break;

                case JsonTokenType.EndArray:
                    --arrayLevel;
                    break;
            }

            if (objectLevel < 0)
                break;
        }
        while (reader.Read());

        return propCount;
    }

    /// <summary>
    /// Stops indenting until disposed. Use this with a <see langword="using"/> statement.
    /// </summary>
    public static JsonIndent StopIndenting(this Utf8JsonWriter writer)
    {
        return JsonIndent.SetOptions == null || !writer.Options.Indented ? default : new JsonIndent(writer, false);
    }

    /// <summary>
    /// Starts indenting until disposed. Use this with a <see langword="using"/> statement.
    /// </summary>
    public static JsonIndent StartIndenting(this Utf8JsonWriter writer)
    {
        return JsonIndent.SetOptions == null || writer.Options.Indented ? default : new JsonIndent(writer, true);
    }
}
using DanielWillett.ReflectionTools;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Translations;

namespace Uncreated.Warfare.Quests.Parameters;

[JsonConverter(typeof(QuestParameterConverter))]
public abstract class QuestParameterValue<TValue> : IFormattable, IEquatable<QuestParameterValue<TValue>>
{
    /// <summary>
    /// Type of value set. Constant, list, wildcard, and sometimes range.
    /// </summary>
    public ParameterValueType ValueType { get; protected set; }

    /// <summary>
    /// Selection style of the parameter. Inclusive allows *any* value in the set, where selective selects *one* value in the set.
    /// </summary>
    public ParameterSelectionType SelectionType { get; protected set; }

    /// <summary>
    /// Compare a value against the current value of this parameter. Best used with inclusive selection.
    /// </summary>
    public abstract bool IsMatch(TValue otherValue);

    /// <summary>
    /// Get one value to use. Must either be a constant or selective.
    /// </summary>
    /// <exception cref="InvalidOperationException">Inclusive selection is not supported when getting a single value.</exception>
    public abstract TValue GetSingleValue();

    /// <summary>
    /// Get one value to use. If this is a range or list it'll return the maximum of the set.
    /// </summary>
    /// <exception cref="InvalidOperationException">Inclusive selection is not supported when getting a single value for this type.</exception>
    public virtual TValue GetSingleValueOrMaximum()
    {
        return GetSingleValue();
    }

    /// <summary>
    /// Get one value to use. If this is a range or list it'll return the minimum of the set.
    /// </summary>
    /// <exception cref="InvalidOperationException">Inclusive selection is not supported when getting a single value for this type.</exception>
    public virtual TValue GetSingleValueOrMinimum()
    {
        return GetSingleValue();
    }

    /// <inheritdoc />
    public abstract bool Equals(QuestParameterValue<TValue>? other);

    /// <summary>
    /// Creates a string for use in translations.
    /// </summary>
    public abstract object GetDisplayString(ITranslationValueFormatter formatter);

    /// <summary>
    /// Convert a parameter value back to a string.
    /// </summary>
    public abstract override string ToString();

    /// <summary>
    /// Write to a JSON writer.
    /// </summary>
    public virtual void WriteJson(Utf8JsonWriter writer)
    {
        writer.WriteStringValue(ToString());
    }

    string IFormattable.ToString(string format, IFormatProvider formatProvider) => ToString();
}

public class QuestParameterConverter : JsonConverterFactory
{
    private static readonly KeyValuePair<Type, JsonConverter>[] Converters =
    [
        new KeyValuePair<Type, JsonConverter>(typeof(QuestParameterValue<int>), new Int32QuestParameterConverter()),
        new KeyValuePair<Type, JsonConverter>(typeof(QuestParameterValue<float>), new SingleQuestParameterConverter()),
        new KeyValuePair<Type, JsonConverter>(typeof(QuestParameterValue<string>), new StringQuestParameterConverter()),
        new KeyValuePair<Type, JsonConverter>(typeof(QuestParameterValue<Guid>), new GuidQuestParameterConverter())
    ];

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        for (int i = 0; i < Converters.Length; ++i)
        {
            if (Converters[i].Key == typeToConvert)
                return true;
        }

        return typeToConvert.IsConstructedGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(QuestParameterValue<>);
    }

    /// <inheritdoc />
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        for (int i = 0; i < Converters.Length; ++i)
        {
            ref KeyValuePair<Type, JsonConverter> converter = ref Converters[i];
            if (converter.Key == typeToConvert)
                return converter.Value;
        }

        Type genType = typeToConvert.GetGenericArguments()[0];
        if (genType.IsEnum)
        {
            return (JsonConverter)Activator.CreateInstance(typeof(EnumQuestParameterConverter<>).MakeGenericType(genType));
        }

        throw new JsonException($"Unable to convert {Accessor.ExceptionFormatter.Format(typeToConvert)}.");
    }

    private class Int32QuestParameterConverter : JsonConverter<QuestParameterValue<int>>
    {
        /// <inheritdoc />
        public override QuestParameterValue<int>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return Int32ParameterTemplate.ReadValueJson(ref reader);
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, QuestParameterValue<int> value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    private class SingleQuestParameterConverter : JsonConverter<QuestParameterValue<float>>
    {
        /// <inheritdoc />
        public override QuestParameterValue<float>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return SingleParameterTemplate.ReadValueJson(ref reader);
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, QuestParameterValue<float> value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    private class StringQuestParameterConverter : JsonConverter<QuestParameterValue<string>>
    {
        /// <inheritdoc />
        public override QuestParameterValue<string>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return StringParameterTemplate.ReadValueJson(ref reader);
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, QuestParameterValue<string> value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    private class GuidQuestParameterConverter : JsonConverter<QuestParameterValue<Guid>>
    {
        /// <inheritdoc />
        public override QuestParameterValue<Guid>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return AssetParameterTemplate<Asset>.ReadValueJson(ref reader);
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, QuestParameterValue<Guid> value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    private class EnumQuestParameterConverter<TEnum> : JsonConverter<QuestParameterValue<TEnum>> where TEnum : unmanaged, Enum
    {
        /// <inheritdoc />
        public override QuestParameterValue<TEnum>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return EnumParameterTemplate<TEnum>.ReadValueJson(ref reader);
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, QuestParameterValue<TEnum> value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Models.Localization;

namespace Uncreated.Warfare;

/// <summary>
/// Wrapper for a <see cref="Dictionary{TKey, TValue}"/>, has custom JSON reading to take a string or dictionary of translations.<br/><see langword="null"/> = empty list.
/// </summary>
[JsonConverter(typeof(TranslationListConverter))]
[TypeConverter(typeof(TranslationListTypeConverter))]
public sealed class TranslationList : List<KeyValuePair<string, string>>, ICloneable, IDictionary<string, string>
{
    public const int DefaultCharLength = 255;

    public TranslationList() : this(0) { }
    public TranslationList(int capacity) : base(capacity) { }
    public TranslationList(IDictionary<string, string> dictionary) : base(dictionary) { }
    public TranslationList(string @default) : base(2)
    {
        Add(string.Empty, @default);
    }
    public TranslationList(int capacity, string @default) : base(capacity)
    {
        Add(string.Empty, @default);
    }
    public TranslationList(TranslationList copy) : base(copy.Count)
    {
        foreach (KeyValuePair<string, string> pair in copy)
        {
            Add(pair.Key, pair.Value);
        }
    }

    /// <summary>
    /// Use language overload if possible.
    /// </summary>
    public void Add(string? code, string value)
    {
        code ??= string.Empty;

        int index = FindIndex(code);
        if (index == -1)
            Add(new KeyValuePair<string, string>(code, value));
        else
            this[index] = new KeyValuePair<string, string>(code, value);
    }

    public void Add(LanguageInfo? language, string value)
    {
        if (language == null || language.IsDefault)
            Add(string.Empty, value);
        else
            Add(language.Code, value);
    }

    [return: NotNullIfNotNull(nameof(@default))]
    public string? Translate(LanguageInfo? language, string? @default)
    {
        return Translate(language) ?? @default;
    }

    public string? Translate(LanguageInfo? language)
    {
        string code = language == null || language.IsDefault ? string.Empty : language.Code;
        if (TryGetValue(code, out string? value) && value != null)
            return value;
        
        if (code.Length == 0 && language != null && TryGetValue(language.Code, out value) && value != null)
            return value;

        if (language != null)
        {
            if (language.FallbackTranslationLanguageCode != null && TryGetValue(language.FallbackTranslationLanguageCode, out value) && value != null)
                return value;

            if (!language.IsDefault && TryGetValue(string.Empty, out value) && value != null)
                return value;
        }

        return Count > 0 ? this[0].Value : null;
    }

    public TranslationList Clone() => new TranslationList(this);
    object ICloneable.Clone() => Clone();

    internal bool TryGetValue(string code, [MaybeNullWhen(false)] out string value)
    {
        for (int i = 0; i < Count; ++i)
        {
            KeyValuePair<string, string> kvp = this[i];
            if (!string.Equals(kvp.Key, code, StringComparison.OrdinalIgnoreCase))
                continue;

            value = kvp.Value;
            return true;
        }

        value = null;
        return false;
    }

    internal int FindIndex(string code)
    {
        for (int i = 0; i < Count; ++i)
        {
            KeyValuePair<string, string> kvp = this[i];
            if (!string.Equals(kvp.Key, code, StringComparison.OrdinalIgnoreCase))
                continue;

            return i;
        }

        return -1;
    }
    
    bool IDictionary<string, string>.ContainsKey(string key)
    {
        return FindIndex(key) != -1;
    }

    bool IDictionary<string, string>.Remove(string key)
    {
        int index = FindIndex(key);
        if (index == -1)
            return false;

        RemoveAt(index);
        return true;
    }

    bool IDictionary<string, string>.TryGetValue(string key, out string value)
    {
        if (TryGetValue(key, out string? v))
        {
            value = v;
            return true;
        }

        value = null!;
        return false;
    }

    string IDictionary<string, string>.this[string key]
    {
        get
        {
            int index = FindIndex(key);
            if (index == -1)
                throw new KeyNotFoundException();
            
            return this[index].Value;
        }
        set
        {
            int index = FindIndex(key);
            if (index == -1)
                Add(key, value);

            this[index] = new KeyValuePair<string, string>(this[index].Key, value);
        }
    }

    ICollection<string> IDictionary<string, string>.Keys => this.Select(x => x.Key).ToList();

    ICollection<string> IDictionary<string, string>.Values => this.Select(x => x.Value).ToList();
}

public sealed class TranslationListConverter : JsonConverter<TranslationList>
{
    public override TranslationList Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        JsonTokenType token = reader.TokenType;
        switch (token)
        {
            case JsonTokenType.Null:
                return new TranslationList();

            case JsonTokenType.String:
                return new TranslationList(reader.GetString()!.Replace("\\n", "\n"));

            case JsonTokenType.StartObject:
                TranslationList list = new TranslationList(2);
                while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
                {
                    string? key = reader.GetString();
                    if (!string.IsNullOrWhiteSpace(key) && reader.Read() && reader.TokenType == JsonTokenType.String)
                    {
                        string? val = reader.GetString();
                        if (val is not null)
                            list.Add(key, val.Replace("\\n", "\n"));
                    }
                    else throw new JsonException("Invalid token type for TranslationList at key \"" + (key ?? "null") + "\".");
                }
                return list;

            default:
                throw new JsonException("Invalid token type for TranslationList.");
        }
    }

    public override void Write(Utf8JsonWriter writer, TranslationList value, JsonSerializerOptions options)
    {
        if (value == null || value.Count == 0)
        {
            writer.WriteNullValue();
        }
        else if (value.Count == 1 && value.TryGetValue(string.Empty, out string v))
        {
            writer.WriteStringValue(v.Replace("\n", "\\n"));
        }
        else
        {
            writer.WriteStartObject();

            foreach (KeyValuePair<string, string> kvp in value)
            {
                writer.WritePropertyName(kvp.Key);
                writer.WriteStringValue(kvp.Value.Replace("\n", "\\n"));
            }

            writer.WriteEndObject();
        }
    }
}

public class TranslationListTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
        return destinationType == typeof(IDictionary<string, string>) || destinationType == typeof(Dictionary<string, string>) || destinationType != typeof(string) && base.CanConvertTo(context, destinationType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object? value)
    {
        if (value is string str)
        {
            return new TranslationList(str);
        }

        return base.ConvertFrom(context, culture, value);
    }

    public override object? ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        TranslationList? list = (TranslationList?)value;
        if (destinationType == typeof(IDictionary<string, string>) || destinationType == typeof(Dictionary<string, string>))
            return list;
        
        if (destinationType != typeof(string))
            return base.ConvertTo(context, culture, value, destinationType);

        if (list is not { Count: 1 })
            throw GetConvertToException(value, destinationType);

        return list.Translate(null, null);

    }
}
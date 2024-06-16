using SDG.Framework.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Encoding;
using Uncreated.Framework;
using Uncreated.Json;
using Uncreated.SQL;
using Uncreated.Warfare.Models.Localization;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare;

public struct Point3D
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("x")]
    public float X { get; set; }
    [JsonPropertyName("y")]
    public float Y { get; set; }
    [JsonPropertyName("z")]
    public float Z { get; set; }
    [JsonIgnore]
    public readonly Vector3 Vector3 { get => new Vector3(X, Y, Z); }
    public Point3D() { }
    public Point3D(string name, float x, float y, float z)
    {
        this.Name = name;
        this.X = x;
        this.Y = y;
        this.Z = z;
    }
}
public struct SerializableVector3 : IJsonReadWrite
{
    public static readonly SerializableVector3 Zero = new SerializableVector3(0, 0, 0);
    [JsonPropertyName("x")]
    public float X { get; set; }
    [JsonPropertyName("y")]
    public float Y { get; set; }
    [JsonPropertyName("z")]
    public float Z { get; set; }
    [JsonIgnore]
    public Vector3 Vector3
    {
        readonly get => new Vector3(X, Y, Z);
        set
        {
            X = value.x; Y = value.y; Z = value.z;
        }
    }
    public static bool operator ==(SerializableVector3 a, SerializableVector3 b) => a.X == b.X && a.Y == b.Y && a.Z == b.Z;
    public static bool operator ==(SerializableVector3 a, Vector3 b) => a.X == b.x && a.Y == b.y && a.Z == b.z;
    public static bool operator !=(SerializableVector3 a, SerializableVector3 b) => a.X != b.X || a.Y != b.Y || a.Z != b.Z;
    public static bool operator !=(SerializableVector3 a, Vector3 b) => a.X != b.x || a.Y != b.y || a.Z != b.z;
    public readonly override bool Equals(object obj)
    {
        if (obj == default) return false;
        if (obj is SerializableVector3 v3)
            return X == v3.X && Y == v3.Y && Z == v3.Z;
        else if (obj is Vector3 uv3)
            return X == uv3.x && Y == uv3.y && Z == uv3.z;
        else return false;
    }
    public readonly override int GetHashCode()
    {
        int hashCode = 373119288;
        hashCode = hashCode * -1521134295 + X.GetHashCode();
        hashCode = hashCode * -1521134295 + Y.GetHashCode();
        hashCode = hashCode * -1521134295 + Z.GetHashCode();
        return hashCode;
    }
    public readonly override string ToString() => $"({Mathf.RoundToInt(X).ToString(Data.LocalLocale)}, {Mathf.RoundToInt(Y).ToString(Data.LocalLocale)}, {Mathf.RoundToInt(Z).ToString(Data.LocalLocale)})";
    public SerializableVector3(Vector3 v)
    {
        X = v.x;
        Y = v.y;
        Z = v.z;
    }
    public SerializableVector3() { }
    public SerializableVector3(float x, float y, float z)
    {
        this.X = x;
        this.Y = y;
        this.Z = z;
    }
    public readonly void WriteJson(Utf8JsonWriter writer)
    {
        writer.WriteProperty("x", X);
        writer.WriteProperty("y", Y);
        writer.WriteProperty("z", Z);
    }
    public void ReadJson(ref Utf8JsonReader reader)
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string val = reader.GetString()!;
                if (val != null && reader.Read())
                {
                    switch (val)
                    {
                        case "x":
                            X = (float)reader.GetDouble();
                            break;
                        case "y":
                            Y = (float)reader.GetDouble();
                            break;
                        case "z":
                            Z = (float)reader.GetDouble();
                            break;
                    }
                }
            }
            else if (reader.TokenType == JsonTokenType.EndObject) return;
        }
    }
}
public struct SerializableTransform : IJsonReadWrite
{
    public static readonly SerializableTransform Zero = new SerializableTransform(SerializableVector3.Zero, SerializableVector3.Zero);
    [JsonPropertyName("position")]
    public SerializableVector3 SerializablePosition { get; set; }
    [JsonPropertyName("euler_angles")]
    public SerializableVector3 SerializableRotation { get; set; }
    [JsonIgnore]
    public readonly Quaternion Rotation { get => Quaternion.Euler(SerializableRotation.Vector3); }
    [JsonIgnore]
    public readonly Vector3 Position { get => SerializablePosition.Vector3; }
    public static bool operator ==(SerializableTransform a, SerializableTransform b) => a.SerializablePosition == b.SerializablePosition && a.SerializableRotation == b.SerializableRotation;
    public static bool operator !=(SerializableTransform a, SerializableTransform b) => a.SerializablePosition != b.SerializablePosition || a.SerializableRotation != b.SerializableRotation;
    public static bool operator ==(SerializableTransform a, Transform b) => a.SerializablePosition == b.position && a.SerializableRotation == b.rotation.eulerAngles;
    public static bool operator !=(SerializableTransform a, Transform b) => a.SerializablePosition != b.position || a.SerializableRotation != b.rotation.eulerAngles;
    public readonly override bool Equals(object obj)
    {
        if (obj == default) return false;
        if (obj is SerializableTransform t)
            return SerializablePosition == t.SerializablePosition && SerializableRotation == t.SerializableRotation;
        else if (obj is Transform ut)
            return SerializablePosition == ut.position && SerializableRotation == ut.eulerAngles;
        else return false;
    }
    public readonly override string ToString() => SerializablePosition.ToString();
    public readonly override int GetHashCode()
    {
        int hashCode = -1079335343;
        hashCode = hashCode * -1521134295 + SerializablePosition.GetHashCode();
        hashCode = hashCode * -1521134295 + SerializableRotation.GetHashCode();
        return hashCode;
    }
    public SerializableTransform() { }
    public SerializableTransform(SerializableVector3 serializablePosition, SerializableVector3 euler_angles)
    {
        this.SerializablePosition = serializablePosition;
        this.SerializableRotation = euler_angles;
    }
    public SerializableTransform(Transform transform)
    {
        SerializablePosition = new SerializableVector3(transform.position);
        SerializableRotation = new SerializableVector3(transform.rotation.eulerAngles);
    }
    public SerializableTransform(Vector3 position, Vector3 eulerAngles)
    {
        this.SerializablePosition = new SerializableVector3(position);
        SerializableRotation = new SerializableVector3(eulerAngles);
    }
    public SerializableTransform(float posx, float posy, float posz, float rotx, float roty, float rotz)
    {
        SerializablePosition = new SerializableVector3(posx, posy, posz);
        SerializableRotation = new SerializableVector3(rotx, roty, rotz);
    }
    public SerializableTransform(Vector3 position, Quaternion rotation)
    {
        this.SerializablePosition = new SerializableVector3(position);
        SerializableRotation = new SerializableVector3(rotation.eulerAngles);
    }
    public readonly void WriteJson(Utf8JsonWriter writer)
    {
        writer.WriteProperty("position", SerializablePosition);
        writer.WriteProperty("euler_angles", SerializableRotation);
    }
    public void ReadJson(ref Utf8JsonReader reader)
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string val = reader.GetString()!;
                if (reader.Read() && reader.TokenType == JsonTokenType.StartObject)
                {
                    switch (val)
                    {
                        case "position":
                            SerializablePosition = new SerializableVector3();
                            SerializablePosition.ReadJson(ref reader);
                            break;
                        case "euler_angles":
                            SerializableRotation = new SerializableVector3();
                            SerializableRotation.ReadJson(ref reader);
                            break;
                    }
                }
                else if (reader.TokenType == JsonTokenType.EndObject) return;
            }
            else if (reader.TokenType == JsonTokenType.EndObject) return;
        }
    }
}

/// <summary>Wrapper for a <see cref="Dictionary{TKey, TValue}"/>, has custom JSON reading to take a string or dictionary of translations.<br/><see langword="null"/> = empty list.</summary>
/// <remarks>Extension methods located in <see cref="T"/>.</remarks>
[JsonConverter(typeof(TranslationListConverter))]
public sealed class TranslationList : Dictionary<string, string>, ICloneable, IReadWrite
{
    public const int DefaultCharLength = 255;
    public TranslationList() : this(0) { }
    public TranslationList(int capacity) : base(capacity, StringComparer.Ordinal) { }
    public TranslationList(IDictionary<string, string> dictionary) : base(dictionary, StringComparer.Ordinal) { }
    public TranslationList(string @default)
    {
        Add(L.Default, @default);
    }
    public TranslationList(int capacity, string @default) : base(capacity)
    {
        Add(L.Default, @default);
    }
    public TranslationList(TranslationList copy) : base(copy.Count)
    {
        foreach (KeyValuePair<string, string> pair in copy)
        {
            Add(pair.Key, pair.Value);
        }
    }
    public static Schema GetDefaultSchema(string tableName, string fkColumn, string mainTable, string mainPkColumn, bool oneToOne = false, bool hasPk = false)
    {
        if (!oneToOne)
            throw new NotSupportedException("One-to-one only rn");
        return F.GetTranslationListSchema(tableName, fkColumn, mainTable, mainPkColumn, DefaultCharLength);
    }

    [return: NotNullIfNotNull(nameof(@default))]
    public string? Translate(LanguageInfo? language, string? @default) => Translate(language) ?? @default;
    public string? Translate(LanguageInfo? language)
    {
        language ??= Localization.GetDefaultLanguage();
        if (TryGetValue(language.Code, out string value))
            return value;

        if (language.FallbackTranslationLanguageCode != null && TryGetValue(language.FallbackTranslationLanguageCode, out value))
            return value;

        if (!language.IsDefault && TryGetValue(L.Default, out value))
            return value;

        return Count > 0 ? Values.ElementAt(0) : null;
    }

    public TranslationList Clone() => new TranslationList(this);
    object ICloneable.Clone() => Clone();
    public void Read(ByteReader reader)
    {
        Clear();
        int len = reader.ReadUInt16();
        for (int i = 0; i < len; ++i)
        {
            Add(reader.ReadShortString(), reader.ReadNullableString()!);
        }
    }

    public void Write(ByteWriter writer)
    {
        writer.Write((ushort)Count);
        foreach (KeyValuePair<string, string> vals in this)
        {
            writer.WriteShort(vals.Key);
            writer.WriteNullable(vals.Value);
        }
    }
}

public sealed class ArrayConverter<TElement, TConverter> : JsonConverter<TElement?[]> where TConverter : JsonConverter, new()
{
    private readonly TConverter _converterFactory = new TConverter();
    private JsonConverter<TElement>? _converter = null;
    private void CheckConverter(JsonSerializerOptions options)
    {
        JsonConverterFactory? factory = _converterFactory as JsonConverterFactory;
        if (_converter != null && factory == null)
            return;


        if (factory != null)
            _converter = factory.CreateConverter(typeof(TElement), options) as JsonConverter<TElement>;
        else
            _converter = _converterFactory as JsonConverter<TElement>;

        if (_converter == null)
            throw new JsonException($"Invalid converter for type: {typeof(TElement)} (using factory: {typeof(TConverter)}).");
    }
    public override TElement?[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        CheckConverter(options);

        if (reader.TokenType == JsonTokenType.Null)
            return null!;

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            List<TElement?> list;
            bool pooled = false;
            if (UCWarfare.IsLoaded && UCWarfare.IsMainThread)
            {
                pooled = true;
                list = ListPool<TElement?>.claim();
            }
            else list = new List<TElement?>(16);

            try
            {
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                        break;
                    list.Add(_converter!.Read(ref reader, typeof(TElement), options));
                }

                return list.Count == 0 ? Array.Empty<TElement?>() : list.ToArray();
            }
            finally
            {
                if (pooled)
                    ListPool<TElement?>.release(list);
            }
        }

        return new TElement?[] { _converter!.Read(ref reader, typeToConvert, options) };
    }

    public override void Write(Utf8JsonWriter writer, TElement?[] value, JsonSerializerOptions options)
    {
        CheckConverter(options);

        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartArray();
        for (int i = 0; i < value.Length; ++i)
        {
            _converter!.Write(writer, value[i]!, options);
        }
        writer.WriteEndArray();
    }
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
            writer.WriteNullValue();
        else if (value.Count == 1 && value.TryGetValue(L.Default, out string v))
            writer.WriteStringValue(v.Replace("\n", "\\n"));
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

[Obsolete]
public class LanguageAliasSet : ITranslationArgument
{
    public string key { get; set; }
    public string display_name { get; set; }
    public string[] values { get; set; }
    [JsonPropertyName("requires_imgui")]
    public bool RequiresIMGUI { get; set; }
    public LanguageAliasSet(string key, string display_name, string[] values)
    {
        this.key = key;
        this.display_name = display_name;
        this.values = values;
    }
    [FormatDisplay("Display Name")]
    public const string FormatDisplayName = "d";
    [FormatDisplay("Key Code")]
    public const string FormatKey = "k";
    public string Translate(LanguageInfo language, string? format, UCPlayer? target, CultureInfo? culture,
        ref TranslationFlags flags)
    {
        if (format is not null && format.Equals(FormatKey, StringComparison.Ordinal))
            return key;
        return display_name;
    }
}
// todo sqlify
public static partial class JSONMethods
{
    public static Dictionary<string, Color> LoadColors(out Dictionary<string, string> hexValues)
    {
        F.CheckDir(Data.Paths.BaseDirectory, out bool fileExists);
        string chatColors = Path.Combine(Data.Paths.BaseDirectory, "chat_colors.json");
        if (fileExists)
        {
            if (!File.Exists(chatColors))
            {
                Dictionary<string, Color> defaultColors2 = new Dictionary<string, Color>(DefaultColors.Count);
                hexValues = new Dictionary<string, string>(DefaultColors.Count);
                using FileStream stream = new FileStream(chatColors, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
                Utf8JsonWriter writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
                writer.WriteStartObject();
                foreach (KeyValuePair<string, string> color in DefaultColors)
                {
                    string y = color.Value;
                    if (y.Equals(Team1ColorPlaceholder, StringComparison.Ordinal))
                        y = TeamManager.Team1ColorHex;
                    else if (y.Equals(Team2ColorPlaceholder, StringComparison.Ordinal))
                        y = TeamManager.Team2ColorHex;
                    else if (y.Equals(Team3ColorPlaceholder, StringComparison.Ordinal))
                        y = TeamManager.AdminColorHex;
                    writer.WritePropertyName(color.Key);
                    writer.WriteStringValue(color.Value);
                    defaultColors2.Add(color.Key, y.Hex());
                    hexValues.Add(color.Key, y);
                }
                writer.WriteEndObject();
                writer.Dispose();
                stream.Close();
                stream.Dispose();
                return defaultColors2;
            }
            using (FileStream stream = new FileStream(chatColors, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                long len = stream.Length;
                if (len > int.MaxValue)
                {
                    L.LogError("chat_colors.json is too long to read.");
                    goto def;
                }
                Dictionary<string, Color> converted = new Dictionary<string, Color>(DefaultColors.Count);
                hexValues = new Dictionary<string, string>(DefaultColors.Count);
                byte[] bytes = new byte[len];
                stream.Read(bytes, 0, (int)len);
                try
                {
                    Utf8JsonReader reader = new Utf8JsonReader(bytes, JsonEx.readerOptions);
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.StartObject) continue;
                        if (reader.TokenType == JsonTokenType.EndObject) break;
                        if (reader.TokenType == JsonTokenType.PropertyName)
                        {
                            string key = reader.GetString()!;
                            if (reader.Read() && reader.TokenType == JsonTokenType.String)
                            {
                                string color = reader.GetString()!;
                                if (hexValues.ContainsKey(key))
                                    L.LogWarning("Duplicate color key \"" + key + "\" in chat_colors.json");
                                else
                                {
                                    if (color.Equals(Team1ColorPlaceholder, StringComparison.Ordinal))
                                        color = TeamManager.Team1ColorHex;
                                    else if (color.Equals(Team2ColorPlaceholder, StringComparison.Ordinal))
                                        color = TeamManager.Team2ColorHex;
                                    else if (color.Equals(Team3ColorPlaceholder, StringComparison.Ordinal))
                                        color = TeamManager.AdminColorHex;
                                    hexValues.Add(key, color);
                                    converted.Add(key, color.Hex());
                                }
                            }
                        }
                    }
                    return converted;
                }
                catch (Exception e)
                {
                    L.LogError("Failed to read chat_colors.json");
                    L.LogError(e);
                }
            }
        }
        else
        {
            L.LogError("Failed to create chat_colors.json, read above.");
        }

        def:
        Dictionary<string, Color> newDefaults = new Dictionary<string, Color>(DefaultColors.Count);
        foreach (KeyValuePair<string, string> color in DefaultColors)
        {
            newDefaults.Add(color.Key, color.Value.Hex());
        }
        hexValues = DefaultColors;
        return newDefaults;
    }
    public static Dictionary<string, Vector3> LoadExtraPoints()
    {
        F.CheckDir(Data.Paths.FlagStorage, out bool dirExists);
        if (dirExists)
        {
            string xtraPts = Path.Combine(Data.Paths.FlagStorage, "extra_points.json");
            if (!File.Exists(xtraPts))
            {
                Dictionary<string, Vector3> defaultXtraPoints2 = new Dictionary<string, Vector3>(DefaultExtraPoints.Count);
                using FileStream stream = new FileStream(xtraPts, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
                Utf8JsonWriter writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
                writer.WriteStartObject();
                for (int i = 0; i < DefaultExtraPoints.Count; i++)
                {
                    Point3D point = DefaultExtraPoints[i];
                    writer.WritePropertyName(point.Name);
                    writer.WriteStartObject();
                    writer.WriteProperty("x", point.X);
                    writer.WriteProperty("y", point.Y);
                    writer.WriteProperty("z", point.Z);
                    writer.WriteEndObject();
                    defaultXtraPoints2.Add(point.Name, point.Vector3);
                }
                writer.WriteEndObject();
                writer.Dispose();
                stream.Close();
                stream.Dispose();
                return defaultXtraPoints2;
            }
            using (FileStream stream = new FileStream(xtraPts, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                long len = stream.Length;
                if (len > int.MaxValue)
                {
                    L.LogError("extra_points.json is too long to read.");
                    goto def;
                }
                Dictionary<string, Vector3> xtraPoints = new Dictionary<string, Vector3>(DefaultExtraPoints.Count);
                byte[] bytes = new byte[len];
                stream.Read(bytes, 0, (int)len);
                try
                {
                    Utf8JsonReader reader = new Utf8JsonReader(bytes, JsonEx.readerOptions);
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.StartObject) continue;
                        if (reader.TokenType == JsonTokenType.EndObject) break;
                        if (reader.TokenType == JsonTokenType.PropertyName)
                        {
                            string key = reader.GetString()!;
                            if (reader.Read() && reader.TokenType == JsonTokenType.StartObject)
                            {
                                float x = 0f;
                                float y = 0f;
                                float z = 0f;
                                while (reader.Read())
                                {
                                    if (reader.TokenType == JsonTokenType.EndObject) break;
                                    else if (reader.TokenType == JsonTokenType.PropertyName)
                                    {
                                        string prop = reader.GetString()!;
                                        if (reader.Read())
                                        {
                                            switch (prop)
                                            {
                                                case "x":
                                                    x = (float)reader.GetDouble();
                                                    break;
                                                case "y":
                                                    y = (float)reader.GetDouble();
                                                    break;
                                                case "z":
                                                    z = (float)reader.GetDouble();
                                                    break;
                                            }
                                        }
                                    }
                                }
                                xtraPoints.Add(key, new Vector3(x, y, z));
                            }
                        }
                    }

                    return xtraPoints;
                }
                catch (Exception e)
                {
                    L.LogError("Failed to read " + xtraPts);
                    L.LogError(e);
                }
            }
        }
        else
        {
            L.LogError("Failed to load extra points, see above. Loading default points.");
        }

        def:
        Dictionary<string, Vector3> defaultXtraPoints = new Dictionary<string, Vector3>(DefaultExtraPoints.Count);
        for (int i = 0; i < DefaultExtraPoints.Count; i++)
        {
            Point3D point = DefaultExtraPoints[i];
            defaultXtraPoints.Add(point.Name, point.Vector3);
        }
        return defaultXtraPoints;
    }
    public static Dictionary<ulong, string> LoadLanguagePreferences()
    {
        F.CheckDir(Data.Paths.LangStorage, out bool dirExists);
        string langPrefs = Path.Combine(Data.Paths.LangStorage, "preferences.json");
        if (dirExists)
        {
            if (!File.Exists(langPrefs))
            {
                using (FileStream stream = new FileStream(langPrefs, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                {
                    byte[] utf8 = System.Text.Encoding.UTF8.GetBytes("[]");
                    stream.Write(utf8, 0, utf8.Length);
                    stream.Close();
                    stream.Dispose();
                }
                return new Dictionary<ulong, string>();
            }
            using (FileStream stream = new FileStream(langPrefs, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                long len = stream.Length;
                if (len > int.MaxValue)
                {
                    L.LogError("Language preferences at preferences.json is too long to read.");
                    return new Dictionary<ulong, string>();
                }
                Dictionary<ulong, string> prefs = new Dictionary<ulong, string>(48);
                byte[] bytes = new byte[len];
                stream.Read(bytes, 0, (int)len);
                try
                {
                    Utf8JsonReader reader = new Utf8JsonReader(bytes, JsonEx.readerOptions);
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.StartObject) continue;
                        else if (reader.TokenType == JsonTokenType.EndObject) break;
                        else if (reader.TokenType == JsonTokenType.PropertyName)
                        {
                            string input = reader.GetString()!;
                            if (!ulong.TryParse(input, NumberStyles.Any, Data.AdminLocale, out ulong steam64))
                            {
                                L.LogWarning("Invalid Steam64 ID: \"" + input + "\" in Lang\\preferences.json");
                            }
                            else if (reader.Read() && reader.TokenType == JsonTokenType.String)
                            {
                                string language = reader.GetString()!;
                                prefs.Add(steam64, language);
                            }
                        }
                    }

                    return prefs;
                }
                catch (Exception ex)
                {
                    L.LogError("Failed to read language preferences at " + langPrefs);
                    L.LogError(ex);
                    return new Dictionary<ulong, string>();
                }
            }
        }
        L.LogError("Failed to load language preferences, see above.");
        return new Dictionary<ulong, string>();
    }
}

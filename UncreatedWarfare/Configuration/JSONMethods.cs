using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Framework;
using Uncreated.Json;
using Uncreated.SQL;
using Uncreated.Warfare.Teams;
using UnityEngine;

namespace Uncreated.Warfare;

public readonly struct TranslationData
{
    public static readonly TranslationData Nil = new TranslationData("default", "<color=#ffffff>default</color>", true, Color.white);
    public readonly string Message;
    public readonly string Original;
    public readonly Color Color;
    public readonly bool UseColor;
    public TranslationData(string message, string original, bool useColor, Color color)
    {
        this.Message = message;
        this.Original = original;
        this.UseColor = useColor;
        this.Color = color;
    }
    public TranslationData(string Original)
    {
        this.Original = Original;
        this.Color = GetColorFromMessage(Original, out Message, out UseColor);
    }
    public static TranslationData GetPlaceholder(string key) => new TranslationData(key, key, false, Color.white);
    public static Color GetColorFromMessage(string Original, out string InnerText, out bool found)
    {
        if (Original.Length < 23)
        {
            InnerText = Original;
            found = false;
            return UCWarfare.GetColor("default");
        }
        if (Original.StartsWith("<color=#") && Original[8] != '{' && Original.EndsWith("</color>"))
        {
            IEnumerator<char> characters = Original.Skip(8).GetEnumerator();
            int start = 8;
            int length = 0;
            while (characters.MoveNext())
            {
                if (characters.Current == '>') break; // keep moving until the ending > is found.
                length++;
            }
            characters.Dispose();
            int msgStart = start + length + 1;
            InnerText = Original.Substring(msgStart, Original.Length - msgStart - 8);
            found = true;
            return Original.Substring(start, length).Hex();
        }
        else
        {
            InnerText = Original;
            found = false;
            return UCWarfare.GetColor("default");
        }
    }
    public override readonly string ToString() =>
        $"Original: {Original}, Inner text: {Message}, {(UseColor ? $"Color: {Color} ({ColorUtility.ToHtmlStringRGBA(Color)}." : "Unable to find color.")}";
}
public struct Point3D
{
    public string name;
    public float x;
    public float y;
    public float z;
    [JsonIgnore]
    public readonly Vector3 Vector3 { get => new Vector3(x, y, z); }
    [JsonConstructor]
    public Point3D(string name, float x, float y, float z)
    {
        this.name = name;
        this.x = x;
        this.y = y;
        this.z = z;
    }
}
public struct SerializableVector3 : IJsonReadWrite
{
    public static readonly SerializableVector3 Zero = new SerializableVector3(0, 0, 0);
    public float x;
    public float y;
    public float z;
    [JsonIgnore]
    public Vector3 Vector3
    {
        readonly get => new Vector3(x, y, z);
        set
        {
            x = value.x; y = value.y; z = value.z;
        }
    }
    public static bool operator ==(SerializableVector3 a, SerializableVector3 b) => a.x == b.x && a.y == b.y && a.z == b.z;
    public static bool operator ==(SerializableVector3 a, Vector3 b) => a.x == b.x && a.y == b.y && a.z == b.z;
    public static bool operator !=(SerializableVector3 a, SerializableVector3 b) => a.x != b.x || a.y != b.y || a.z != b.z;
    public static bool operator !=(SerializableVector3 a, Vector3 b) => a.x != b.x || a.y != b.y || a.z != b.z;
    public override readonly bool Equals(object obj)
    {
        if (obj == default) return false;
        if (obj is SerializableVector3 v3)
            return x == v3.x && y == v3.y && z == v3.z;
        else if (obj is Vector3 uv3)
            return x == uv3.x && y == uv3.y && z == uv3.z;
        else return false;
    }
    public override readonly int GetHashCode()
    {
        int hashCode = 373119288;
        hashCode = hashCode * -1521134295 + x.GetHashCode();
        hashCode = hashCode * -1521134295 + y.GetHashCode();
        hashCode = hashCode * -1521134295 + z.GetHashCode();
        return hashCode;
    }
    public override readonly string ToString() => $"({Mathf.RoundToInt(x).ToString(Data.Locale)}, {Mathf.RoundToInt(y).ToString(Data.Locale)}, {Mathf.RoundToInt(z).ToString(Data.Locale)})";
    public SerializableVector3(Vector3 v)
    {
        x = v.x;
        y = v.y;
        z = v.z;
    }
    [JsonConstructor]
    public SerializableVector3(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
    public readonly void WriteJson(Utf8JsonWriter writer)
    {
        writer.WriteProperty(nameof(x), this.x);
        writer.WriteProperty(nameof(y), this.y);
        writer.WriteProperty(nameof(z), this.z);
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
                        case nameof(x):
                            x = (float)reader.GetDouble();
                            break;
                        case nameof(y):
                            y = (float)reader.GetDouble();
                            break;
                        case nameof(z):
                            z = (float)reader.GetDouble();
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
    public SerializableVector3 position;
    public SerializableVector3 euler_angles;
    [JsonIgnore]
    public readonly Quaternion Rotation { get => Quaternion.Euler(euler_angles.Vector3); }
    [JsonIgnore]
    public readonly Vector3 Position { get => position.Vector3; }
    public static bool operator ==(SerializableTransform a, SerializableTransform b) => a.position == b.position && a.euler_angles == b.euler_angles;
    public static bool operator !=(SerializableTransform a, SerializableTransform b) => a.position != b.position || a.euler_angles != b.euler_angles;
    public static bool operator ==(SerializableTransform a, Transform b) => a.position == b.position && a.euler_angles == b.rotation.eulerAngles;
    public static bool operator !=(SerializableTransform a, Transform b) => a.position != b.position || a.euler_angles != b.rotation.eulerAngles;
    public override readonly bool Equals(object obj)
    {
        if (obj == default) return false;
        if (obj is SerializableTransform t)
            return position == t.position && euler_angles == t.euler_angles;
        else if (obj is Transform ut)
            return position == ut.position && euler_angles == ut.eulerAngles;
        else return false;
    }
    public override readonly string ToString() => position.ToString();
    public override readonly int GetHashCode()
    {
        int hashCode = -1079335343;
        hashCode = hashCode * -1521134295 + position.GetHashCode();
        hashCode = hashCode * -1521134295 + euler_angles.GetHashCode();
        return hashCode;
    }
    [JsonConstructor]
    public SerializableTransform(SerializableVector3 position, SerializableVector3 euler_angles)
    {
        this.position = position;
        this.euler_angles = euler_angles;
    }
    public SerializableTransform(Transform transform)
    {
        this.position = new SerializableVector3(transform.position);
        this.euler_angles = new SerializableVector3(transform.rotation.eulerAngles);
    }
    public SerializableTransform(Vector3 position, Vector3 eulerAngles)
    {
        this.position = new SerializableVector3(position);
        this.euler_angles = new SerializableVector3(eulerAngles);
    }
    public SerializableTransform(float posx, float posy, float posz, float rotx, float roty, float rotz)
    {
        this.position = new SerializableVector3(posx, posy, posz);
        this.euler_angles = new SerializableVector3(rotx, roty, rotz);
    }
    public SerializableTransform(Vector3 position, Quaternion rotation)
    {
        this.position = new SerializableVector3(position);
        this.euler_angles = new SerializableVector3(rotation.eulerAngles);
    }
    public readonly void WriteJson(Utf8JsonWriter writer)
    {
        writer.WriteProperty(nameof(position), position);
        writer.WriteProperty(nameof(euler_angles), euler_angles);
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
                        case nameof(position):
                            position = new SerializableVector3();
                            position.ReadJson(ref reader);
                            break;
                        case nameof(euler_angles):
                            euler_angles = new SerializableVector3();
                            euler_angles.ReadJson(ref reader);
                            break;
                    }
                }
                else if (reader.TokenType == JsonTokenType.EndObject) return;
            }
            else if (reader.TokenType == JsonTokenType.EndObject) return;
        }
    }
}

/// <summary>Wrapper for a <see cref="Dictionary{string, string}"/>, has custom JSON reading to take a string or dictionary of translations.<br/><see langword="null"/> = empty list.</summary>
/// <remarks>Extension methods located in <see cref="T"/>.</remarks>
[JsonConverter(typeof(TranslationListConverter))]
public sealed class TranslationList : Dictionary<string, string>, ICloneable
{
    public const int DEFAULT_CHAR_LENGTH = 255;
    public TranslationList() { }
    public TranslationList(int capacity) : base(capacity) { }
    public TranslationList(string @default)
    {
        Add(L.DEFAULT, @default);
    }
    public TranslationList(int capacity, string @default) : base(capacity)
    {
        Add(L.DEFAULT, @default);
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
        return F.GetTranslationListSchema(tableName, fkColumn, mainTable, mainPkColumn, DEFAULT_CHAR_LENGTH);
    }

    public object Clone() => new TranslationList(this);
}
public sealed class TranslationListConverter : JsonConverter<TranslationList>
{
    public override TranslationList? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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
                            list.Add(key!, val.Replace("\\n", "\n"));
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
        else if (value.Count == 1 && value.TryGetValue(L.DEFAULT, out string v))
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

public class LanguageAliasSet : IJsonReadWrite, ITranslationArgument
{
    public const string ENGLISH = "en-us";
    public const string RUSSIAN = "ru-ru";
    public const string SPANISH = "es-es";
    public const string GERMAN = "de-de";
    public const string ARABIC = "ar-sa";
    public const string FRENCH = "fr-fr";
    public const string POLISH = "pl-pl";
    public const string PORTUGUESE = "pt-pt";
    public const string FILIPINO = "fil";
    public const string NORWEGIAN = "nb-no";
    public const string ROMANIAN = "ro-ro";
    public const string DUTCH = "nl-nl";
    public const string CHINESE_SIMPLIFIED = "zh-cn";
    public const string CHINESE_TRADITIONAL = "zh-tw";
    public static readonly CultureInfo ENGLISH_C = new CultureInfo("en-US");
    public static readonly CultureInfo RUSSIAN_C = new CultureInfo("ru-RU");
    public static readonly CultureInfo SPANISH_C = new CultureInfo("es-ES");
    public static readonly CultureInfo GERMAN_C = new CultureInfo("de-DE");
    public static readonly CultureInfo ARABIC_C = new CultureInfo("ar-SA");
    public static readonly CultureInfo FRENCH_C = new CultureInfo("fr-FR");
    public static readonly CultureInfo POLISH_C = new CultureInfo("pl-PL");
    public static readonly CultureInfo PORTUGUESE_C = new CultureInfo("pt-PT");
    public static readonly CultureInfo FILIPINO_C = new CultureInfo("fil-PH");
    public static readonly CultureInfo NORWEGIAN_C = new CultureInfo("nb-NO");
    public static readonly CultureInfo ROMANIAN_C = new CultureInfo("ro-RO");
    public static readonly CultureInfo DUTCH_C = new CultureInfo("nl-NL");
    public static readonly CultureInfo CHINESE_C = new CultureInfo("zh-CN");

    public static CultureInfo GetCultureInfo(string? language)
    {
        if (language is not null)
        {
            if (language.Equals(ENGLISH, StringComparison.Ordinal))
                return ENGLISH_C;
            if (language.Equals(RUSSIAN, StringComparison.Ordinal))
                return RUSSIAN_C;
            if (language.Equals(SPANISH, StringComparison.Ordinal))
                return SPANISH_C;
            if (language.Equals(GERMAN, StringComparison.Ordinal))
                return GERMAN_C;
            if (language.Equals(ARABIC, StringComparison.Ordinal))
                return ARABIC_C;
            if (language.Equals(FRENCH, StringComparison.Ordinal))
                return FRENCH_C;
            if (language.Equals(POLISH, StringComparison.Ordinal))
                return POLISH_C;
            if (language.Equals(PORTUGUESE, StringComparison.Ordinal))
                return PORTUGUESE_C;
            if (language.Equals(NORWEGIAN, StringComparison.Ordinal))
                return NORWEGIAN_C;
            if (language.Equals(ROMANIAN, StringComparison.Ordinal))
                return ROMANIAN_C;
            if (language.Equals(DUTCH, StringComparison.Ordinal))
                return DUTCH_C;
            if (language.Equals(CHINESE_SIMPLIFIED, StringComparison.Ordinal) ||
                language.Equals(CHINESE_TRADITIONAL, StringComparison.Ordinal))
                return CHINESE_C;
        }

        return Data.LocalLocale;
    }
    public string key;
    public string display_name;
    public string[] values;
    [JsonConstructor]
    public LanguageAliasSet(string key, string display_name, string[] values)
    {
        this.key = key;
        this.display_name = display_name;
        this.values = values;
    }

    public LanguageAliasSet(ref Utf8JsonReader reader) => ReadJson(ref reader);
    public void ReadJson(ref Utf8JsonReader reader)
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) return;
            else if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string prop = reader.GetString()!;
                if (!reader.Read()) continue;
                if (prop == nameof(key))
                    this.key = reader.GetString()!;
                else if (prop == nameof(display_name))
                    this.display_name = reader.GetString()!;
                else if (prop == nameof(values) && reader.TokenType == JsonTokenType.StartArray)
                {
                    List<string> tlist = new List<string>(24);
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    {
                        if (reader.TokenType == JsonTokenType.String)
                        {
                            tlist.Add(reader.GetString()!);
                        }
                    }
                    this.values = tlist.ToArray();
                }
            }
        }
    }

    [FormatDisplay("Display Name")]
    public const string DISPLAY_NAME_FORMAT = "d";
    [FormatDisplay("Key Code")]
    public const string KEY_FORMAT = "k";
    public string Translate(string language, string? format, UCPlayer? target, ref TranslationFlags flags)
    {
        if (format is not null && format.Equals(KEY_FORMAT, StringComparison.Ordinal))
            return key;
        return display_name;
    }

    public void WriteJson(Utf8JsonWriter writer)
    {
        writer.WriteProperty(nameof(key), key);
        writer.WriteProperty(nameof(display_name), display_name);
        writer.WritePropertyName(nameof(values));
        writer.WriteStartArray();
        for (int i = 0; i < values.Length; i++)
        {
            writer.WriteStringValue(values[i]);
        }
        writer.WriteEndArray();
    }
}
public static partial class JSONMethods
{
    public static Dictionary<string, Color> LoadColors(out Dictionary<string, string> HexValues)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        F.CheckDir(Data.Paths.BaseDirectory, out bool fileExists);
        string chatColors = Path.Combine(Data.Paths.BaseDirectory, "chat_colors.json");
        if (fileExists)
        {
            if (!File.Exists(chatColors))
            {
                Dictionary<string, Color> defaultColors2 = new Dictionary<string, Color>(DefaultColors.Count);
                HexValues = new Dictionary<string, string>(DefaultColors.Count);
                using (FileStream stream = new FileStream(chatColors, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                {
                    Utf8JsonWriter writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
                    writer.WriteStartObject();
                    foreach (KeyValuePair<string, string> color in DefaultColors)
                    {
                        string y = color.Value;
                        if (y.Equals(T1_COLOR_PH, StringComparison.Ordinal))
                            y = TeamManager.Team1ColorHex;
                        else if (y.Equals(T2_COLOR_PH, StringComparison.Ordinal))
                            y = TeamManager.Team2ColorHex;
                        else if (y.Equals(T3_COLOR_PH, StringComparison.Ordinal))
                            y = TeamManager.AdminColorHex;
                        writer.WritePropertyName(color.Key);
                        writer.WriteStringValue(color.Value);
                        defaultColors2.Add(color.Key, y.Hex());
                        HexValues.Add(color.Key, y);
                    }
                    writer.WriteEndObject();
                    writer.Dispose();
                    stream.Close();
                    stream.Dispose();
                }
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
                else
                {
                    Dictionary<string, Color> converted = new Dictionary<string, Color>(DefaultColors.Count);
                    HexValues = new Dictionary<string, string>(DefaultColors.Count);
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
                                string key = reader.GetString()!;
                                if (reader.Read() && reader.TokenType == JsonTokenType.String)
                                {
                                    string color = reader.GetString()!;
                                    if (HexValues.ContainsKey(key))
                                        L.LogWarning("Duplicate color key \"" + key + "\" in chat_colors.json");
                                    else
                                    {
                                        if (color.Equals(T1_COLOR_PH, StringComparison.Ordinal))
                                            color = TeamManager.Team1ColorHex;
                                        else if (color.Equals(T2_COLOR_PH, StringComparison.Ordinal))
                                            color = TeamManager.Team2ColorHex;
                                        else if (color.Equals(T3_COLOR_PH, StringComparison.Ordinal))
                                            color = TeamManager.AdminColorHex;
                                        HexValues.Add(key, color);
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
                        goto def;
                    }
                }
            }
        }
        else
        {
            L.LogError("Failed to create chat_colors.json, read above.");
            goto def;
        }

    def:
        Dictionary<string, Color> NewDefaults = new Dictionary<string, Color>(DefaultColors.Count);
        foreach (KeyValuePair<string, string> color in DefaultColors)
        {
            NewDefaults.Add(color.Key, color.Value.Hex());
        }
        HexValues = DefaultColors;
        return NewDefaults;
    }
    public static Dictionary<string, Vector3> LoadExtraPoints()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        F.CheckDir(Data.Paths.FlagStorage, out bool dirExists);
        if (dirExists)
        {
            string xtraPts = Path.Combine(Data.Paths.FlagStorage, "extra_points.json");
            if (!File.Exists(xtraPts))
            {
                Dictionary<string, Vector3> defaultXtraPoints2 = new Dictionary<string, Vector3>(DefaultExtraPoints.Count);
                using (FileStream stream = new FileStream(xtraPts, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                {
                    Utf8JsonWriter writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
                    writer.WriteStartObject();
                    for (int i = 0; i < DefaultExtraPoints.Count; i++)
                    {
                        Point3D point = DefaultExtraPoints[i];
                        writer.WritePropertyName(point.name);
                        writer.WriteStartObject();
                        writer.WriteProperty("x", point.x);
                        writer.WriteProperty("y", point.y);
                        writer.WriteProperty("z", point.z);
                        writer.WriteEndObject();
                        defaultXtraPoints2.Add(point.name, point.Vector3);
                    }
                    writer.WriteEndObject();
                    writer.Dispose();
                    stream.Close();
                    stream.Dispose();
                }
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
                else
                {
                    Dictionary<string, Vector3> xtraPoints = new Dictionary<string, Vector3>(DefaultExtraPoints.Count);
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
                        goto def;
                    }
                }
            }
        }
        else
        {
            L.LogError("Failed to load extra points, see above. Loading default points.");
            goto def;
        }


    def:
        Dictionary<string, Vector3> defaultXtraPoints = new Dictionary<string, Vector3>(DefaultExtraPoints.Count);
        for (int i = 0; i < DefaultExtraPoints.Count; i++)
        {
            Point3D point = DefaultExtraPoints[i];
            defaultXtraPoints.Add(point.name, point.Vector3);
        }
        return defaultXtraPoints;
    }
    public static Dictionary<ulong, string> LoadLanguagePreferences()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
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
                else
                {
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
                                if (!ulong.TryParse(input, System.Globalization.NumberStyles.Any, Data.Locale, out ulong steam64))
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
        }
        else
        {
            L.LogError("Failed to load language preferences, see above.");
            return new Dictionary<ulong, string>();
        }
    }
    public static void SaveLangs(Dictionary<ulong, string> languages)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (languages == null) return;
        F.CheckDir(Data.Paths.LangStorage, out bool dirExists);
        if (dirExists)
        {
            using (FileStream stream = new FileStream(Path.Combine(Data.Paths.LangStorage, "preferences.json"), FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            {
                Utf8JsonWriter writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
                writer.WriteStartObject();
                foreach (KeyValuePair<ulong, string> languagePref in languages)
                {
                    writer.WritePropertyName(languagePref.Key.ToString(Data.Locale));
                    writer.WriteStringValue(languagePref.Value);
                }
                writer.WriteEndObject();
                writer.Dispose();
                stream.Close();
                stream.Dispose();
            }
        }
    }
    public static void SetLanguage(ulong player, string language)
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        if (Data.Languages.ContainsKey(player))
        {
            Data.Languages[player] = language;
            SaveLangs(Data.Languages);
        }
        else
        {
            Data.Languages.Add(player, language);
            SaveLangs(Data.Languages);
        }
    }
    public static List<LanguageAliasSet> LoadLangAliases()
    {
#if DEBUG
        using IDisposable profiler = ProfilingUtils.StartTracking();
#endif
        F.CheckDir(Data.Paths.LangStorage, out bool dirExists);
        string langAliases = Path.Combine(Data.Paths.LangStorage, "aliases.json");
        if (dirExists)
        {
            if (!File.Exists(langAliases))
            {
                List<LanguageAliasSet> defaultLanguageAliasSets2 = new List<LanguageAliasSet>(DefaultLanguageAliasSets.Count);
                using FileStream stream = new FileStream(langAliases, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
                Utf8JsonWriter writer = new Utf8JsonWriter(stream, JsonEx.writerOptions);
                writer.WriteStartArray();
                for (int i = 0; i < DefaultLanguageAliasSets.Count; i++)
                {
                    LanguageAliasSet set = DefaultLanguageAliasSets[i];
                    writer.WriteStartObject();
                    set.WriteJson(writer);
                    writer.WriteEndObject();;
                }
                writer.WriteEndArray();
                writer.Dispose();
                stream.Close();
                stream.Dispose();
                return defaultLanguageAliasSets2;
            }
            using (FileStream stream = new FileStream(langAliases, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                long len = stream.Length;
                if (len > int.MaxValue)
                {
                    L.LogError("Language alias sets at aliases.json is too long to read.");
                    goto def;
                }
                List<LanguageAliasSet> languageAliasSets = new List<LanguageAliasSet>(DefaultLanguageAliasSets.Count);
                byte[] bytes = new byte[len];
                stream.Read(bytes, 0, (int)len);
                try
                {
                    Utf8JsonReader reader = new Utf8JsonReader(bytes, JsonEx.readerOptions);
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.StartArray) continue;
                        if (reader.TokenType == JsonTokenType.EndArray) break;
                        if (reader.TokenType == JsonTokenType.StartObject)
                        {
                            LanguageAliasSet set = new LanguageAliasSet(ref reader);
                            if (set.key != null)
                                languageAliasSets.Add(set);
                        }
                    }

                    return languageAliasSets;
                }
                catch (Exception e)
                {
                    L.LogError("Failed to read language aliases at aliases.json.");
                    L.LogError(e);
                }
            }
        }
        else
        {
            L.LogError("Failed to load language aliases, see above. Loading default language aliases.");
        }
        def:
        List<LanguageAliasSet> defaultLanguageAliasSets = new List<LanguageAliasSet>(DefaultLanguageAliasSets.Count);
        for (int i = 0; i < DefaultLanguageAliasSets.Count; i++)
        {
            LanguageAliasSet set = DefaultLanguageAliasSets[i];
            defaultLanguageAliasSets.Add(set);
        }
        return defaultLanguageAliasSets;
    }
}

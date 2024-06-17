using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Configuration.JsonConverters;

namespace Uncreated.Warfare.Configuration;
public static class JsonSettings
{
    private static readonly JavaScriptEncoder TextEncoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

    public static readonly JsonSerializerOptions SerializerSettings = new JsonSerializerOptions
    {
        WriteIndented = true,
        AllowTrailingCommas = true,
        Encoder = TextEncoder,
        ReadCommentHandling = JsonCommentHandling.Skip
    };
    public static readonly JsonSerializerOptions CondensedSerializerSettings = new JsonSerializerOptions
    {
        WriteIndented = false,
        AllowTrailingCommas = true,
        Encoder = TextEncoder,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public static readonly JsonConverter[] Converters =
    {
        new QuaternionJsonConverter(),
        new Vector4JsonConverter(),
        new Vector3JsonConverter(),
        new Vector2JsonConverter(),
        new ColorJsonConverter(),
        new Color32JsonConverter(),
        new CSteamIDJsonConverter(),
    };

    public static readonly JsonWriterOptions WriterOptions = new JsonWriterOptions { Indented = true, Encoder = TextEncoder };
    public static readonly JsonWriterOptions CondensedWriterOptions = new JsonWriterOptions { Indented = false, Encoder = TextEncoder };
    public static readonly JsonReaderOptions ReaderOptions = new JsonReaderOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip };
    static JsonSettings()
    {
        for (int i = 0; i < Converters.Length; ++i)
        {
            JsonConverter converter = Converters[i];
            SerializerSettings.Converters.Add(converter);
            CondensedSerializerSettings.Converters.Add(converter);
        }
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

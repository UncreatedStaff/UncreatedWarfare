using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Configuration.JsonConverters;
using Uncreated.Warfare.Configuration.TypeConverters;
using Uncreated.Warfare.Vehicles;

namespace Uncreated.Warfare.Configuration;
public static class ConfigurationSettings
{
    private static readonly JavaScriptEncoder TextEncoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
    
    public static readonly JsonWriterOptions JsonWriterOptions = new JsonWriterOptions { Indented = true, Encoder = TextEncoder };
    
    public static readonly JsonWriterOptions JsonCondensedWriterOptions = new JsonWriterOptions { Indented = false, Encoder = TextEncoder };

    public static readonly JsonReaderOptions JsonReaderOptions = new JsonReaderOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip };
    
    public static readonly JsonSerializerOptions JsonSerializerSettings = new JsonSerializerOptions
    {
        WriteIndented = true,
        AllowTrailingCommas = true,
        Encoder = TextEncoder,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public static readonly JsonSerializerOptions JsonCondensedSerializerSettings = new JsonSerializerOptions
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
        new AssetLinkJsonFactory(),
        new TranslationListConverter(),
        new ByteArraySegmentJsonConverter(),
        new ByteArrayConverter()
    };

    static ConfigurationSettings()
    {
        for (int i = 0; i < Converters.Length; ++i)
        {
            JsonConverter converter = Converters[i];
            JsonSerializerSettings.Converters.Add(converter);
            JsonCondensedSerializerSettings.Converters.Add(converter);
        }
    }

    /// <summary>
    /// Add converters used for binding to third-party types.
    /// </summary>
    internal static void SetupTypeConverters()
    {
        TimeSpanConverterWithTimeString.Setup();
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

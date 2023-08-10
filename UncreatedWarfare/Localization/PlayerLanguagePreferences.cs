using System;
using System.Text.Json.Serialization;
using Uncreated.SQL;

namespace Uncreated.Warfare;
public sealed class PlayerLanguagePreferences
{
    [JsonPropertyName("steam64")]
    public ulong Steam64 { get; }

    [JsonPropertyName("language")]
    public PrimaryKey Language { get; set; }

    [JsonPropertyName("culture_code")]
    public string? CultureCode { get; set; }

    [JsonPropertyName("use_culture_for_command_input")]
    public bool UseCultureForCommandInput { get; set; }

    [JsonPropertyName("last_updated")]
    public DateTimeOffset LastUpdated { get; set; }

    public PlayerLanguagePreferences(ulong steam64)
    {
        Steam64 = steam64;
        UseCultureForCommandInput = true;
    }

    [JsonConstructor]
    public PlayerLanguagePreferences(ulong steam64, PrimaryKey language, string? cultureCode, bool useCultureForCommandInput, DateTimeOffset lastUpdated)
    {
        Steam64 = steam64;
        Language = language;
        CultureCode = cultureCode;
        UseCultureForCommandInput = useCultureForCommandInput;
        LastUpdated = lastUpdated;
    }
}

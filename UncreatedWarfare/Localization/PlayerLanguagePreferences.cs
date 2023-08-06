using System;
using System.Globalization;
using Uncreated.SQL;

namespace Uncreated.Warfare;
public sealed class PlayerLanguagePreferences
{
    public ulong Steam64 { get; }
    public PrimaryKey Language { get; set; }
    public string? CultureCode { get; set; }
    public DateTimeOffset LastUpdated { get; set; }

    public PlayerLanguagePreferences(ulong steam64)
    {
        Steam64 = steam64;
    }
    public PlayerLanguagePreferences(ulong steam64, PrimaryKey language, string? cultureCode, DateTimeOffset lastUpdated)
    {
        Steam64 = steam64;
        Language = language;
        CultureCode = cultureCode;
        LastUpdated = lastUpdated;
    }
}

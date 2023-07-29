using System;
using Uncreated.SQL;

namespace Uncreated.Warfare;
public class LanguageInfo : IListItem
{
    public PrimaryKey PrimaryKey { get; set; }
    public string DisplayName { get; set; }
    public string LanguageCode { get; set; }
    public bool HasTranslationSupport { get; set; }
    public string? DefaultCultureCode { get; set; }
    public string[] Aliases { get; set; } = Array.Empty<string>();
    public string[] AvailableCultureCodes { get; set; } = Array.Empty<string>();
    public ulong[] Credits { get; set; } = Array.Empty<ulong>();
}

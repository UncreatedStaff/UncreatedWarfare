using System.Globalization;
using Uncreated.SQL;

namespace Uncreated.Warfare.Sync;
public static class ServerRegion
{
    public static byte Key => UCWarfare.IsLoaded ? UCWarfare.Config.RegionKey : byte.MaxValue;
    public static RegionData Current;
    public static readonly Schema REGION_TABLE = UCWarfare.IsLoaded ? new Schema("region_data", new Schema.Column[]
    {
        new Schema.Column("RegionKey", SqlTypes.REGION_KEY)
        {
            PrimaryKey = true,
            AutoIncrement = true
        },
        new Schema.Column("DisplayName", SqlTypes.STRING_255),
        new Schema.Column("Currency", "char(3)")
        {
            Default = "'USD'"
        },
        new Schema.Column("DefaultTranslationLanguageCode", "char(5)")
        {
            Default = "'" + LanguageAliasSet.ENGLISH + "'"
        },
        new Schema.Column("DefaultCultureCode", "varchar(16)")
        {
            Default = "'" +LanguageAliasSet.ENGLISH_C + "'"
        },
        new Schema.Column("HostCountryCode", "char(2)")
        {
            Default = "'US'"
        }
    }, true, typeof(RegionData)) : null!;
}

public class RegionData : IListItem
{
    public PrimaryKey PrimaryKey { get => RegionKey; set => RegionKey = checked((byte)value.Key); }
    public byte RegionKey { get; private set; }
    public string DisplayName { get; private set; }

    /// <summary>ISO 4217 Code (3 characters)</summary>
    public string Currency { get; private set; }

    /// <summary>Uncreated Language Code (5 characters)</summary>
    public string DefaultTranslationLanguageCode { get; private set; }

    /// <summary>.NET <see cref="CultureInfo"/> code (5-11 characters, case in-sensitive)</summary>
    public string DefaultCultureCode { get; private set; }

    /// <summary>ISO 3166 Code (2 characters)</summary>
    public string HostCountryCode { get; private set; }

    internal RegionData(byte regionKey, string displayName, string currency, string defaultTranslationLanguageCode, string defaultCultureCode, string hostCountryCode)
    {
        RegionKey = regionKey;
        DisplayName = displayName;
        Currency = currency;
        DefaultTranslationLanguageCode = defaultTranslationLanguageCode;
        DefaultCultureCode = defaultCultureCode;
        HostCountryCode = hostCountryCode;
    }

    public RegionData() { }
}
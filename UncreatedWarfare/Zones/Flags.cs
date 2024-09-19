using Uncreated.Warfare.Translations.Util;

namespace Uncreated.Warfare.Zones;
public static class Flags
{
    public static readonly SpecialFormat ColorNameFormat = new SpecialFormat("Colored Name", "nc");

    public static readonly SpecialFormat NameFormat = new SpecialFormat("Name", "n");

    public static readonly SpecialFormat ColorShortNameFormat = new SpecialFormat("Colored Short Name", "sc");

    public static readonly SpecialFormat ShortNameFormat = new SpecialFormat("Short Name", "s");

    public static readonly SpecialFormat ColorNameDiscoverFormat = new SpecialFormat("Colored Name (Discovered Check)", "ncd");

    public static readonly SpecialFormat NameDiscoverFormat = new SpecialFormat("Name (Discovered Check)", "nd");

    public static readonly SpecialFormat ColorShortNameDiscoverFormat = new SpecialFormat("Colored Short Name (Discovered Check)", "scd");

    public static readonly SpecialFormat ShortNameDiscoverFormat = new SpecialFormat("Short Name (Discovered Check)", "sd");

    public static readonly SpecialFormat LocationNameFormat = new SpecialFormat("Nearest Location Name", "lc");
}
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Database.Automation;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Kits;

[JsonConverter(typeof(ClassConverter))]
[Translatable("Kit Class")]
[ExcludedEnum(None)]
public enum Class : byte
{
    [TranslatableValue(IsPrioritizedTranslation = false)]
    None = 0,
    Unarmed = 1,
    [TranslatableValue("Squad Leader")]
    Squadleader = 2,
    Rifleman = 3,
    Medic = 4,
    Breacher = 5,
    [TranslatableValue("Automatic Rifleman")]
    AutomaticRifleman = 6,
    Grenadier = 7,
    [TranslatableValue("Machine Gunner")]
    MachineGunner = 8,
    LAT = 9,
    HAT = 10,
    Marksman = 11,
    Sniper = 12,
    [TranslatableValue("Anti-personnel Rifleman")]
    APRifleman = 13,
    [TranslatableValue("Combat Engineer")]
    CombatEngineer = 14,
    Crewman = 15,
    Pilot = 16,
    [TranslatableValue("Special Ops")]
    SpecOps = 17
}

public static class ClassExtensions
{
    private static readonly char[] Icons = new char[(int)EnumUtility.GetMaximumValue<Class>() + 1];
    private static readonly string[] IconStrings = new string[(int)EnumUtility.GetMaximumValue<Class>() + 1];
    private static readonly string?[] KitIdAbbreviations = new string?[(int)EnumUtility.GetMaximumValue<Class>() + 1];
    static ClassExtensions()
    {
        Array.Fill(Icons, '±');
        Array.Fill(IconStrings, "±");

        Icons[(int)Class.Squadleader] = '¦';
        IconStrings[(int)Class.Squadleader] = "¦";
        KitIdAbbreviations[(int)Class.Squadleader] = "sql";
        Icons[(int)Class.Rifleman] = '¡';
        IconStrings[(int)Class.Rifleman] = "¡";
        KitIdAbbreviations[(int)Class.Rifleman] = "rif";
        Icons[(int)Class.Medic] = '¢';
        IconStrings[(int)Class.Medic] = "¢";
        KitIdAbbreviations[(int)Class.Medic] = "med";
        Icons[(int)Class.Breacher] = '¤';
        IconStrings[(int)Class.Breacher] = "¤";
        KitIdAbbreviations[(int)Class.Breacher] = "bre";
        Icons[(int)Class.AutomaticRifleman] = '¥';
        IconStrings[(int)Class.AutomaticRifleman] = "¥";
        KitIdAbbreviations[(int)Class.AutomaticRifleman] = "ar";
        Icons[(int)Class.Grenadier] = '¬';
        IconStrings[(int)Class.Grenadier] = "¬";
        KitIdAbbreviations[(int)Class.Grenadier] = "gre";
        Icons[(int)Class.MachineGunner] = '«';
        IconStrings[(int)Class.MachineGunner] = "«";
        KitIdAbbreviations[(int)Class.MachineGunner] = "mg";
        Icons[(int)Class.LAT] = '®';
        IconStrings[(int)Class.LAT] = "®";
        KitIdAbbreviations[(int)Class.LAT] = "lat";
        Icons[(int)Class.HAT] = '¯';
        IconStrings[(int)Class.HAT] = "¯";
        KitIdAbbreviations[(int)Class.HAT] = "hat";
        Icons[(int)Class.Marksman] = '¨';
        IconStrings[(int)Class.Marksman] = "¨";
        KitIdAbbreviations[(int)Class.Marksman] = "mar";
        Icons[(int)Class.Sniper] = '£';
        IconStrings[(int)Class.Sniper] = "£";
        KitIdAbbreviations[(int)Class.Sniper] = "sni";
        Icons[(int)Class.APRifleman] = '©';
        IconStrings[(int)Class.APRifleman] = "©";
        KitIdAbbreviations[(int)Class.APRifleman] = "apr";
        Icons[(int)Class.CombatEngineer] = 'ª';
        IconStrings[(int)Class.CombatEngineer] = "ª";
        KitIdAbbreviations[(int)Class.CombatEngineer] = "com";
        Icons[(int)Class.Crewman] = '§';
        IconStrings[(int)Class.Crewman] = "§";
        KitIdAbbreviations[(int)Class.Crewman] = "cre";
        Icons[(int)Class.Pilot] = '°';
        IconStrings[(int)Class.Pilot] = "°";
        KitIdAbbreviations[(int)Class.Pilot] = "pil";
        Icons[(int)Class.SpecOps] = '×';
        IconStrings[(int)Class.SpecOps] = "×";
        KitIdAbbreviations[(int)Class.SpecOps] = "spec";
    }

    public static char GetIcon(this Class @class)
    {
        return (int)@class < Icons.Length ? Icons[(int)@class] : Icons[0];
    }

    public static string GetIconString(this Class @class)
    {
        return (int)@class < Icons.Length ? IconStrings[(int)@class] : IconStrings[0];
    }

    public static string? GetKitIdAbbreviation(this Class @class)
    {
        return (int)@class < Icons.Length ? KitIdAbbreviations[(int)@class] : null;
    }

    /// <summary>
    /// Try parse the class from a class abbreviation.
    /// </summary>
    /// <remarks>Ignores any characters before the abbreviation, so it can parse strings like 'usrif' and the 'us' will be ignored.</remarks>
    /// <returns>Whether or not a class could be determined.</returns>
    public static bool TryGetClassFromKitIdAbbreviation(ReadOnlySpan<char> abbreviation, out Class @class)
    {
        @class = Class.None;
        abbreviation = abbreviation.TrimEnd();
        if (abbreviation.Length < 2)
            return false;

        const StringComparison comparison = StringComparison.OrdinalIgnoreCase;

        // switch on last letter
        switch (abbreviation[^1])
        {
            case 'l':
            case 'L':
                if (abbreviation.EndsWith("sql", comparison))
                    @class = Class.Squadleader;
                else if (abbreviation.EndsWith("pil", comparison))
                    @class = Class.Pilot;
                else return false;
                return true;
            case 'f':
            case 'F':
                if (abbreviation.EndsWith("rif", comparison))
                    @class = Class.Rifleman;
                else return false;
                return true;
            case 'd':
            case 'D':
                if (abbreviation.EndsWith("med", comparison))
                    @class = Class.Medic;
                else return false;
                return true;
            case 'e':
            case 'E':
                if (abbreviation.EndsWith("bre", comparison))
                    @class = Class.Breacher;
                else if (abbreviation.EndsWith("gre", comparison))
                    @class = Class.Grenadier;
                else if (abbreviation.EndsWith("cre", comparison))
                    @class = Class.Crewman;
                else return false;
                return true;
            case 'r':
            case 'R':
                if (abbreviation.EndsWith("mar", comparison))
                    @class = Class.Marksman; // needs to go before 'ar' since 'mar' ends with 'ar'
                else if (abbreviation.EndsWith("ar", comparison))
                    @class = Class.AutomaticRifleman;
                else if (abbreviation.EndsWith("apr", comparison))
                    @class = Class.APRifleman;
                else return false;
                return true;
            case 'g':
            case 'G':
                if (abbreviation.EndsWith("mg", comparison))
                    @class = Class.MachineGunner;
                else return false;
                return true;
            case 't':
            case 'T':
                if (abbreviation.EndsWith("lat", comparison))
                    @class = Class.LAT;
                else if (abbreviation.EndsWith("hat", comparison))
                    @class = Class.HAT;
                else return false;
                return true;
            case 'i':
            case 'I':
                if (abbreviation.EndsWith("sni", comparison))
                    @class = Class.Sniper;
                else return false;
                return true;
            case 'm':
            case 'M':
                if (abbreviation.EndsWith("com", comparison))
                    @class = Class.CombatEngineer;
                else return false;
                return true;
            case 'c':
            case 'C':
                if (abbreviation.EndsWith("spec", comparison))
                    @class = Class.SpecOps;
                else return false;
                return true;
            default:
                return false;
        }
    }

    public static bool TryParseClass(string val, out Class @class)
    {
        if (Enum.TryParse(val, true, out @class))
            return EnumUtility.ValidateValidField(@class);
        // checks old values for the enum before renaming.
        if (val.Equals("AUTOMATIC_RIFLEMAN", StringComparison.OrdinalIgnoreCase))
            @class = Class.AutomaticRifleman;
        else if (val.Equals("MACHINE_GUNNER", StringComparison.OrdinalIgnoreCase))
            @class = Class.MachineGunner;
        else if (val.Equals("AP_RIFLEMAN", StringComparison.OrdinalIgnoreCase))
            @class = Class.APRifleman;
        else if (val.Equals("COMBAT_ENGINEER", StringComparison.OrdinalIgnoreCase))
            @class = Class.CombatEngineer;
        else if (val.Equals("SPEC_OPS", StringComparison.OrdinalIgnoreCase))
            @class = Class.SpecOps;
        else
        {
            @class = default;
            return false;
        }

        return true;
    }
}

public sealed class ClassConverter : JsonConverter<Class>
{
    internal static readonly Class MaxClass = EnumUtility.GetMaximumValue<Class>();
    public override Class Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return Class.None;
            case JsonTokenType.Number:
                if (reader.TryGetByte(out byte b))
                    return (Class)b;
                throw new JsonException("Invalid Class value.");
            case JsonTokenType.String:
                string val = reader.GetString()!;
                if (ClassExtensions.TryParseClass(val, out Class @class))
                    return @class;
                throw new JsonException("Invalid Class value.");
            default:
                throw new JsonException("Invalid token for Class parameter.");
        }
    }
    public override void Write(Utf8JsonWriter writer, Class value, JsonSerializerOptions options)
    {
        if (value <= MaxClass)
            writer.WriteStringValue(EnumUtility.GetName(value));
        else
            writer.WriteNumberValue((byte)value);
    }
}
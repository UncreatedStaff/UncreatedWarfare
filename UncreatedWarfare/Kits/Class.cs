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
    static ClassExtensions()
    {
        Array.Fill(Icons, '±');
        Array.Fill(IconStrings, "±");

        Icons[(int)Class.Squadleader] = '¦';
        IconStrings[(int)Class.Squadleader] = "¦";
        Icons[(int)Class.Rifleman] = '¡';
        IconStrings[(int)Class.Rifleman] = "¡";
        Icons[(int)Class.Medic] = '¢';
        IconStrings[(int)Class.Medic] = "¢";
        Icons[(int)Class.Breacher] = '¤';
        IconStrings[(int)Class.Breacher] = "¤";
        Icons[(int)Class.AutomaticRifleman] = '¥';
        IconStrings[(int)Class.AutomaticRifleman] = "¥";
        Icons[(int)Class.Grenadier] = '¬';
        IconStrings[(int)Class.Grenadier] = "¬";
        Icons[(int)Class.MachineGunner] = '«';
        IconStrings[(int)Class.MachineGunner] = "«";
        Icons[(int)Class.LAT] = '®';
        IconStrings[(int)Class.LAT] = "®";
        Icons[(int)Class.HAT] = '¯';
        IconStrings[(int)Class.HAT] = "¯";
        Icons[(int)Class.Marksman] = '¨';
        IconStrings[(int)Class.Marksman] = "¨";
        Icons[(int)Class.Sniper] = '£';
        IconStrings[(int)Class.Sniper] = "£";
        Icons[(int)Class.APRifleman] = '©';
        IconStrings[(int)Class.APRifleman] = "©";
        Icons[(int)Class.CombatEngineer] = 'ª';
        IconStrings[(int)Class.CombatEngineer] = "ª";
        Icons[(int)Class.Crewman] = '§';
        IconStrings[(int)Class.Crewman] = "§";
        Icons[(int)Class.Pilot] = '°';
        IconStrings[(int)Class.Pilot] = "°";
        Icons[(int)Class.SpecOps] = '×';
        IconStrings[(int)Class.SpecOps] = "×";
    }

    public static char GetIcon(this Class @class)
    {
        return (int)@class < Icons.Length ? Icons[(int)@class] : Icons[0];
    }

    public static string GetIconString(this Class @class)
    {
        return (int)@class < Icons.Length ? IconStrings[(int)@class] : IconStrings[0];
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
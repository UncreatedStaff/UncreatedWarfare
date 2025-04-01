using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Database.Automation;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Languages;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Kits;

[JsonConverter(typeof(ClassConverter))]
[Translatable("Kit Class")]
[ExcludedEnum(None)]
public enum Class : byte
{
    None = 0,
    [Translatable(Languages.Russian, "Безоружный")]
    [Translatable(Languages.Spanish, "Desarmado")]
    [Translatable(Languages.Romanian, "Neinarmat")]
    [Translatable(Languages.PortugueseBrazil, "Desarmado")]
    [Translatable(Languages.Polish, "Nieuzbrojony")]
    [Translatable(Languages.ChineseSimplified, "无武装")]
    Unarmed = 1,
    [Translatable("Squad Leader")]
    [Translatable(Languages.Russian, "Лидер отряда")]
    [Translatable(Languages.Spanish, "Líder De Escuadrón")]
    [Translatable(Languages.Romanian, "Lider de Echipa")]
    [Translatable(Languages.PortugueseBrazil, "Líder de Esquadrão")]
    [Translatable(Languages.Polish, "Dowódca Oddziału")]
    [Translatable(Languages.ChineseSimplified, "小队长")]
    Squadleader = 2,
    [Translatable(Languages.Russian, "Стрелок")]
    [Translatable(Languages.Spanish, "Fusilero")]
    [Translatable(Languages.Romanian, "Puscas")]
    [Translatable(Languages.Polish, "Strzelec")]
    [Translatable(Languages.ChineseSimplified, "步枪兵")]
    Rifleman = 3,
    [Translatable(Languages.Russian, "Медик")]
    [Translatable(Languages.Spanish, "Médico")]
    [Translatable(Languages.Romanian, "Medic")]
    [Translatable(Languages.Polish, "Medyk")]
    [Translatable(Languages.ChineseSimplified, "卫生员")]
    Medic = 4,
    [Translatable(Languages.Russian, "Нарушитель")]
    [Translatable(Languages.Spanish, "Brechador")]
    [Translatable(Languages.Romanian, "Breacher")]
    [Translatable(Languages.Polish, "Wyłamywacz")]
    [Translatable(Languages.ChineseSimplified, "突破手")]
    Breacher = 5,
    [Translatable(Languages.Russian, "Солдат с автоматом")]
    [Translatable(Languages.Spanish, "Fusilero Automático")]
    [Translatable(Languages.Spanish, "Puscas Automat")]
    [Translatable(Languages.PortugueseBrazil, "Fuzileiro Automobilístico")]
    [Translatable(Languages.Polish, "Strzelec Automatyczny")]
    [Translatable(Languages.EnglishUS, "Automatic Rifleman")]
    [Translatable(Languages.ChineseSimplified, "自动步枪兵")]
    AutomaticRifleman = 6,
    [Translatable(Languages.Russian, "Гренадёр")]
    [Translatable(Languages.Spanish, "Granadero")]
    [Translatable(Languages.Romanian, "Grenadier")]
    [Translatable(Languages.PortugueseBrazil, "Granadeiro")]
    [Translatable(Languages.Polish, "Grenadier")]
    [Translatable(Languages.ChineseSimplified, "掷弹兵")]
    Grenadier = 7,
    [Translatable(Languages.Romanian, "Mitralior")]
    [Translatable(Languages.EnglishUS, "Machine Gunner")]
    [Translatable(Languages.ChineseSimplified, "机枪手")]
    MachineGunner = 8,
    [Translatable("LAT")]
    [Translatable(Languages.Russian, "Лёгкий противотанк")]
    [Translatable(Languages.Spanish, "Anti-Tanque Ligero")]
    [Translatable(Languages.Romanian, "Anti-Tanc Usor")]
    [Translatable(Languages.PortugueseBrazil, "Anti-Tanque Leve")]
    [Translatable(Languages.Polish, "Lekka Piechota Przeciwpancerna")]
    [Translatable(Languages.ChineseSimplified, "轻型反坦克兵")]
    LAT = 9,
    [Translatable("HAT")]
    [Translatable(Languages.ChineseSimplified, "重型反坦克兵")]
    HAT = 10,
    [Translatable(Languages.Russian, "Марксман")]
    [Translatable(Languages.Spanish, "Tirador Designado")]
    [Translatable(Languages.Romanian, "Lunetist-Usor")]
    [Translatable(Languages.Polish, "Zwiadowca")]
    [Translatable(Languages.ChineseSimplified, "精确射手")]
    Marksman = 11,
    [Translatable(Languages.Russian, "Снайпер")]
    [Translatable(Languages.Spanish, "Francotirador")]
    [Translatable(Languages.Romanian, "Lunetist")]
    [Translatable(Languages.PortugueseBrazil, "Franco-Atirador")]
    [Translatable(Languages.Polish, "Snajper")]
    [Translatable(Languages.ChineseSimplified, "狙击手")]
    Sniper = 12,
    [Translatable("Anti-personnel Rifleman")]
    [Translatable(Languages.Russian, "Противопехотный")]
    [Translatable(Languages.Spanish, "Fusilero Anti-Personal")]
    [Translatable(Languages.Romanian, "Puscas Anti-Personal")]
    [Translatable(Languages.PortugueseBrazil, "Antipessoal")]
    [Translatable(Languages.Polish, "Strzelec Przeciw-Piechotny")]
    [Translatable(Languages.ChineseSimplified, "反人员步枪兵")]
    APRifleman = 13,
    [Translatable(Languages.Russian, "Инженер")]
    [Translatable(Languages.Spanish, "Ingeniero")]
    [Translatable(Languages.Romanian, "Inginer")]
    [Translatable(Languages.PortugueseBrazil, "Engenheiro")]
    [Translatable(Languages.Polish, "Inżynier")]
    [Translatable(Languages.EnglishUS, "Combat Engineer")]
    [Translatable(Languages.ChineseSimplified, "战斗工兵")]
    CombatEngineer = 14,
    [Translatable(Languages.Russian, "Механик-водитель")]
    [Translatable(Languages.Spanish, "Tripulante")]
    [Translatable(Languages.Romanian, "Echipaj")]
    [Translatable(Languages.PortugueseBrazil, "Tripulante")]
    [Translatable(Languages.Polish, "Załogant")]
    [Translatable(Languages.ChineseSimplified, "车组成员")]
    Crewman = 15,
    [Translatable(Languages.Russian, "Пилот")]
    [Translatable(Languages.Spanish, "Piloto")]
    [Translatable(Languages.Romanian, "Pilot")]
    [Translatable(Languages.PortugueseBrazil, "Piloto")]
    [Translatable(Languages.Polish, "Pilot")]
    [Translatable(Languages.ChineseSimplified, "飞行员")]
    Pilot = 16,
    [Translatable("Special Ops")]
    [Translatable(Languages.Spanish, "Op. Esp.")]
    [Translatable(Languages.Romanian, "Trupe Speciale")]
    [Translatable(Languages.PortugueseBrazil, "Op. Esp.")]
    [Translatable(Languages.Polish, "Specjalista")]
    [Translatable(Languages.ChineseSimplified, "特种部队")]
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
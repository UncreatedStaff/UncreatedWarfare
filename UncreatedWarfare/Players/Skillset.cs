using MySql.Data.MySqlClient;
using SDG.Unturned;
using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Encoding;
using Uncreated.SQL;
using Uncreated.Warfare.Kits;
using Uncreated.Warfare.Models.Localization;

namespace Uncreated.Warfare.Players;

[JsonConverter(typeof(SkillsetConverter))]
public readonly struct Skillset : IEquatable<Skillset>, ITranslationArgument
{
    public static readonly Skillset[] DefaultSkillsets =
    {
        new Skillset(EPlayerOffense.SHARPSHOOTER, 7),
        new Skillset(EPlayerOffense.PARKOUR, 1),
        new Skillset(EPlayerOffense.EXERCISE, 0),
        new Skillset(EPlayerOffense.CARDIO, 5),
        new Skillset(EPlayerOffense.DIVING, 0),
        new Skillset(EPlayerDefense.VITALITY, 5),
    };

    public readonly EPlayerSpeciality Speciality;
    public readonly byte Level;
    public readonly byte SkillIndex;
    public EPlayerOffense Offense => (EPlayerOffense)SkillIndex;
    public EPlayerDefense Defense => (EPlayerDefense)SkillIndex;
    public EPlayerSupport Support => (EPlayerSupport)SkillIndex;
    public byte SpecialityIndex => (byte)Speciality;
    public Skillset(EPlayerOffense skill, byte level)
    {
        Speciality = EPlayerSpeciality.OFFENSE;
        SkillIndex = (byte)skill;
        Level = level;
    }
    public Skillset(EPlayerDefense skill, byte level)
    {
        Speciality = EPlayerSpeciality.DEFENSE;
        SkillIndex = (byte)skill;
        Level = level;
    }
    public Skillset(EPlayerSupport skill, byte level)
    {
        Speciality = EPlayerSpeciality.SUPPORT;
        SkillIndex = (byte)skill;
        Level = level;
    }
    internal Skillset(EPlayerSpeciality specialty, byte skill, byte level)
    {
        Speciality = specialty;
        SkillIndex = skill;
        Level = level;
    }
    public static Skillset Read(ByteReader reader)
    {
        EPlayerSpeciality speciality = (EPlayerSpeciality)reader.ReadUInt8();
        byte val = reader.ReadUInt8();
        byte level = reader.ReadUInt8();
        return speciality switch
        {
            EPlayerSpeciality.SUPPORT or EPlayerSpeciality.DEFENSE or EPlayerSpeciality.OFFENSE => new Skillset(speciality, val, level),
            _ => throw new Exception("Invalid value of specialty while reading skillset.")
        };
    }
    public static void Write(ByteWriter writer, Skillset skillset)
    {
        writer.Write((byte)skillset.Speciality);
        writer.Write(skillset.SkillIndex);
        writer.Write(skillset.Level);
    }
    public void ServerSet(UCPlayer player)
    {
        ThreadUtil.assertIsGameThread();
        if (player.IsOnline)
            player.Player.skills.ServerSetSkillLevel(SpecialityIndex, SkillIndex, Level);
    }
    /// <exception cref="FormatException"/>
    public static Skillset Read(MySqlDataReader reader, int colOffset = 0)
    {
        string type = reader.GetString(colOffset + 1);
        byte level = reader.GetByte(colOffset + 2);
        if (Enum.TryParse(type, true, out EPlayerOffense offense))
            return new Skillset(offense, level);
        if (Enum.TryParse(type, true, out EPlayerDefense defense))
            return new Skillset(defense, level);
        if (Enum.TryParse(type, true, out EPlayerSupport support))
            return new Skillset(support, level);
        throw new FormatException("Unable to find valid skill for skillset: \"" + type + "\" at level " + level + ".");
    }
    public static Skillset Read(ref Utf8JsonReader reader)
    {
        bool valFound = false;
        bool lvlFound = false;
        EPlayerSpeciality spec = default;
        EPlayerOffense offense = default;
        EPlayerDefense defense = default;
        EPlayerSupport support = default;
        byte level = 255;
        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            string? property = reader.GetString();
            if (reader.Read() && property != null)
            {
                switch (property)
                {
                    case "offense":
                        spec = EPlayerSpeciality.OFFENSE;
                        string? value2 = reader.GetString();
                        if (value2 != null)
                        {
                            Enum.TryParse(value2, true, out offense);
                            valFound = true;
                        }
                        break;
                    case "defense":
                        spec = EPlayerSpeciality.DEFENSE;
                        string? value3 = reader.GetString();
                        if (value3 != null)
                        {
                            Enum.TryParse(value3, true, out defense);
                            valFound = true;
                        }
                        break;
                    case "support":
                        spec = EPlayerSpeciality.SUPPORT;
                        string? value4 = reader.GetString();
                        if (value4 != null)
                        {
                            Enum.TryParse(value4, true, out support);
                            valFound = true;
                        }
                        break;
                    case "level":
                        if (reader.TryGetByte(out level))
                        {
                            lvlFound = true;
                        }
                        break;
                }
            }
        }
        if (valFound && lvlFound)
        {
            switch (spec)
            {
                case EPlayerSpeciality.OFFENSE:
                    return new Skillset(offense, level);
                case EPlayerSpeciality.DEFENSE:
                    return new Skillset(defense, level);
                case EPlayerSpeciality.SUPPORT:
                    return new Skillset(support, level);
            }
        }
        L.Log("Error parsing skillset.");
        return default;
    }
    public static void Write(Utf8JsonWriter writer, in Skillset skillset)
    {
        switch (skillset.Speciality)
        {
            case EPlayerSpeciality.OFFENSE:
                writer.WriteString("offense", skillset.Offense.ToString());
                break;
            case EPlayerSpeciality.DEFENSE:
                writer.WriteString("defense", skillset.Defense.ToString());
                break;
            case EPlayerSpeciality.SUPPORT:
                writer.WriteString("support", skillset.Support.ToString());
                break;
        }
        writer.WriteNumber("level", skillset.Level);
    }
    public override bool Equals(object? obj) => obj is Skillset skillset && EqualsHelper(in skillset, true);
    private bool EqualsHelper(in Skillset skillset, bool compareLevel)
    {
        if (compareLevel && skillset.Level != Level) return false;
        return skillset.Speciality == Speciality && skillset.SkillIndex == SkillIndex;
    }
    public override string ToString()
    {
        return Speciality switch
        {
            EPlayerSpeciality.OFFENSE => "Offense: " + Offense,
            EPlayerSpeciality.DEFENSE => "Defense: " + Defense,
            EPlayerSpeciality.SUPPORT => "Support: " + Support,
            _ => "Invalid speciality #" + SkillIndex.ToString(Data.AdminLocale)
        } + " at level " + Level.ToString(Data.AdminLocale) + ".";
    }
    public override int GetHashCode()
    {
        int hashCode = 1232939970;
        hashCode *= -1521134295 + Speciality.GetHashCode();
        hashCode *= -1521134295 + Level.GetHashCode();
        hashCode *= -1521134295 + SkillIndex;
        return hashCode;
    }

    [FormatDisplay("No Level")]
    public const string FormatNoLevel = "nl";
    string ITranslationArgument.Translate(LanguageInfo language, string? format, UCPlayer? target, CultureInfo? culture,
        ref TranslationFlags flags)
    {
        string b = Speciality switch
        {
            EPlayerSpeciality.DEFENSE => Localization.TranslateEnum(Defense, language),
            EPlayerSpeciality.OFFENSE => Localization.TranslateEnum(Offense, language),
            EPlayerSpeciality.SUPPORT => Localization.TranslateEnum(Support, language),
            _ => SpecialityIndex.ToString(culture) + "." + SkillIndex.ToString(culture)
        };
        if (format != null && format.Equals(FormatNoLevel, StringComparison.Ordinal))
            return b;
        return b + " Level " + Level.ToString(culture);
    }

    public bool Equals(Skillset other) => EqualsHelper(in other, true);
    public bool TypeEquals(in Skillset skillset) => EqualsHelper(in skillset, false);
    /// <returns>-1 if parse failure.</returns>
    public static int GetSkillsetFromEnglishName(string name, out EPlayerSpeciality speciality)
    {
        if (Enum.TryParse(name, true, out EPlayerOffense offense))
        {
            speciality = EPlayerSpeciality.OFFENSE;
            return (int)offense;
        }
        if (Enum.TryParse(name, true, out EPlayerDefense defense))
        {
            speciality = EPlayerSpeciality.DEFENSE;
            return (int)defense;
        }
        if (Enum.TryParse(name, true, out EPlayerSupport support))
        {
            speciality = EPlayerSpeciality.SUPPORT;
            return (int)support;
        }
        speciality = (EPlayerSpeciality)(-1);
        return -1;
    }
    public static bool operator ==(Skillset a, Skillset b) => a.EqualsHelper(in b, true);
    public static bool operator !=(Skillset a, Skillset b) => !a.EqualsHelper(in b, true);
    // ReSharper disable InconsistentNaming
    public const string COLUMN_PK = "pk";
    public const string COLUMN_SKILL = "Skill";
    public const string COLUMN_LEVEL = "Level";
    // ReSharper restore InconsistentNaming

    public static readonly string SkillSqlEnumType = "enum('" + string.Join("','",
        typeof(EPlayerOffense).GetEnumNames().Concat(typeof(EPlayerDefense).GetEnumNames())
            .Concat(typeof(EPlayerSupport).GetEnumNames())) + "')";
    public static Schema GetDefaultSchema(string tableName, string fkColumn, string mainTable, string mainPkColumn, bool oneToOne = false, bool hasPk = false)
    {
        if (!oneToOne && fkColumn.Equals(COLUMN_PK, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Foreign key column may not be the same as \"" + COLUMN_PK + "\".", nameof(fkColumn));
        int ct = 3;
        if (!oneToOne && hasPk)
            ++ct;
        Schema.Column[] columns = new Schema.Column[ct];
        int index = 0;
        if (!oneToOne && hasPk)
        {
            columns[0] = new Schema.Column(COLUMN_PK, SqlTypes.INCREMENT_KEY)
            {
                PrimaryKey = true,
                AutoIncrement = true
            };
        }
        else index = -1;
        columns[++index] = new Schema.Column(fkColumn, SqlTypes.INCREMENT_KEY)
        {
            PrimaryKey = oneToOne,
            AutoIncrement = oneToOne,
            ForeignKey = true,
            ForeignKeyColumn = mainPkColumn,
            ForeignKeyTable = mainTable
        };
        columns[++index] = new Schema.Column(COLUMN_SKILL, SkillSqlEnumType);
        columns[++index] = new Schema.Column(COLUMN_LEVEL, SqlTypes.BYTE);
        return new Schema(tableName, columns, false, typeof(Skillset));
    }
}
public sealed class SkillsetConverter : JsonConverter<Skillset>
{
    public override Skillset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => Skillset.Read(ref reader);
    public override void Write(Utf8JsonWriter writer, Skillset value, JsonSerializerOptions options) => Skillset.Write(writer, in value);
}
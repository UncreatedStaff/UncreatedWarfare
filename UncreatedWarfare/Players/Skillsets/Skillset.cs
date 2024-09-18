using DanielWillett.SpeedBytes;
using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Uncreated.Warfare.Translations;
using Uncreated.Warfare.Translations.Util;
using Uncreated.Warfare.Translations.ValueFormatters;
using Uncreated.Warfare.Util;

namespace Uncreated.Warfare.Players.Skillsets;

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

    public static readonly string SkillSqlEnumType = "enum('" + string.Join("','",
        typeof(EPlayerOffense).GetEnumNames().Concat(typeof(EPlayerDefense).GetEnumNames())
            .Concat(typeof(EPlayerSupport).GetEnumNames())) + "')";

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
    public void ServerSet(WarfarePlayer player)
    {
        GameThread.AssertCurrent();
        if (player.IsOnline)
            player.UnturnedPlayer.skills.ServerSetSkillLevel(SpecialityIndex, SkillIndex, Level);
    }

    public static Skillset Read(ref Utf8JsonReader reader)
    {
        SkillsetReadState state = default;
        state.Level = byte.MaxValue;
        JsonUtility.ReadTopLevelProperties(ref reader, ref state, action: (ref Utf8JsonReader reader, string propertyName, ref SkillsetReadState state) =>
        {
            if (propertyName.Equals("offsense", StringComparison.Ordinal))
            {
                state.Spec = EPlayerSpeciality.OFFENSE;
                string? value = reader.GetString();
                if (value != null && Enum.TryParse(value, true, out state.Offense))
                {
                    state.ValFound = true;
                }
                else throw new JsonException($"Invalid offense value: \"{value}\".");
            }
            else if (propertyName.Equals("defense", StringComparison.Ordinal))
            {
                state.Spec = EPlayerSpeciality.DEFENSE;
                string? value = reader.GetString();
                if (value != null && Enum.TryParse(value, true, out state.Defense))
                {
                    state.ValFound = true;
                }
                else throw new JsonException($"Invalid defense value: \"{value}\".");
            }
            else if (propertyName.Equals("support", StringComparison.Ordinal))
            {
                state.Spec = EPlayerSpeciality.SUPPORT;
                string? value = reader.GetString();
                if (value != null && Enum.TryParse(value, true, out state.Support))
                {
                    state.ValFound = true;
                }
                else throw new JsonException($"Invalid support value: \"{value}\".");
            }
            else
            {
                if (reader.TryGetByte(out state.Level))
                {
                    state.LvlFound = true;
                }
                else throw new JsonException($"Invalid level value: \"{reader.GetString()}\".");
            }
        });

        if (!state.ValFound || !state.LvlFound)
            throw new JsonException("Invalid skillset JSON format. Skill and/or level not found.");

        return state.Spec switch
        {
            EPlayerSpeciality.OFFENSE => new Skillset(state.Offense, state.Level),
            EPlayerSpeciality.DEFENSE => new Skillset(state.Defense, state.Level),
            _ => new Skillset(state.Support, state.Level)
        };
    }

    private struct SkillsetReadState
    {
        public bool ValFound;
        public bool LvlFound;
        public EPlayerSpeciality Spec;
        public EPlayerOffense Offense;
        public EPlayerDefense Defense;
        public EPlayerSupport Support;
        public byte Level;
    }

    public static void Write(Utf8JsonWriter writer, in Skillset skillset)
    {
        writer.WriteStartObject();
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
        writer.WriteEndObject();
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
            _ => "Invalid speciality #" + SkillIndex.ToString(CultureInfo.InvariantCulture)
        } + " at level " + Level.ToString(CultureInfo.InvariantCulture) + ".";
    }
    public override int GetHashCode()
    {
        int hashCode = 1232939970;
        hashCode *= -1521134295 + Speciality.GetHashCode();
        hashCode *= -1521134295 + Level.GetHashCode();
        hashCode *= -1521134295 + SkillIndex;
        return hashCode;
    }

    public static readonly SpecialFormat FormatNoLevel = new SpecialFormat("No Level", "nl");
    string ITranslationArgument.Translate(ITranslationValueFormatter formatter, in ValueFormatParameters parameters)
    {
        string? format = parameters.Format.Format;
        string b = Speciality switch
        {
            EPlayerSpeciality.DEFENSE => formatter.FormatEnum(Defense, parameters.Language),
            EPlayerSpeciality.OFFENSE => formatter.FormatEnum(Offense, parameters.Language),
            EPlayerSpeciality.SUPPORT => formatter.FormatEnum(Support, parameters.Language),
            _ => SpecialityIndex.ToString(parameters.Culture) + "." + SkillIndex.ToString(parameters.Culture)
        };
        if (format != null && FormatNoLevel.Match(in parameters))
            return b;
        return b + " Level " + Level.ToString(parameters.Culture);
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
}
public sealed class SkillsetConverter : JsonConverter<Skillset>
{
    public override Skillset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => Skillset.Read(ref reader);
    public override void Write(Utf8JsonWriter writer, Skillset value, JsonSerializerOptions options) => Skillset.Write(writer, in value);
}